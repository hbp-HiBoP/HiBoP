using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using CRNL.HiBoP.Spikes.P06;

var options = ClientOptions.Parse(args);
var verifier = new ServerIdentityVerifier(options.PairingCode, options.ExpectedPinHex);
using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = verifier.ValidateHttp };
using var http = new HttpClient(handler) { BaseAddress = options.Endpoint, Timeout = TimeSpan.FromSeconds(30) };

using var pairRequest = new HttpRequestMessage(HttpMethod.Post, "/pair");
pairRequest.Headers.Add("X-P06-Pair-Code", options.PairingCode);
using var pairResponse = await http.SendAsync(pairRequest);
pairResponse.EnsureSuccessStatusCode();
var pairing = JsonSerializer.Deserialize<PairingResponse>(await pairResponse.Content.ReadAsStringAsync(), JsonDefaults.Options)
    ?? throw new InvalidDataException("Pairing response was empty.");
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pairing.AccessToken);

var codecs = RunCodecBenchmarks(options.CodecIterations);
var buffers = RunBufferBenchmarks();
var malformedFrameRejected = await VerifyMalformedFrameRejectedAsync(options, verifier, pairing.AccessToken);
var allocatedBefore = GC.GetTotalAllocatedBytes(true);
var stopwatch = Stopwatch.StartNew();
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(options.DurationSeconds + 30));
var commandTask = RunCommandsAsync(options, verifier, pairing.AccessToken, cancellation.Token);
var assetTask = DownloadAssetAsync(http, cancellation.Token);
await Task.WhenAll(commandTask, assetTask);
stopwatch.Stop();

var report = new ClientReport(
    DateTimeOffset.UtcNow,
    Environment.OSVersion.ToString(),
    System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
    options.Endpoint.ToString(),
    Convert.ToHexStringLower(verifier.Pin ?? throw new InvalidOperationException("No server pin was captured.")),
    codecs,
    buffers,
    malformedFrameRejected,
    commandTask.Result,
    assetTask.Result,
    GC.GetTotalAllocatedBytes(true) - allocatedBefore,
    Process.GetCurrentProcess().PeakWorkingSet64,
    stopwatch.Elapsed.TotalSeconds);

var json = JsonSerializer.Serialize(report, JsonDefaults.Options);
Console.WriteLine(json);
if (!string.IsNullOrWhiteSpace(options.Output))
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.Output))!);
    await File.WriteAllTextAsync(options.Output, json);
}

static IReadOnlyList<CodecReport> RunCodecBenchmarks(int iterations)
{
    var sample = new ControlSample(
        1,
        0x0102030405060708,
        0x1112131415161718,
        42,
        638923456789012345,
        new string('x', 256),
        Enumerable.Range(-16, 33).ToArray());
    var reports = new List<CodecReport>();

    foreach (var codec in ControlCodecs.All)
    {
        for (var index = 0; index < 10_000; index++)
        {
            _ = codec.Decode(codec.Encode(sample));
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        byte[]? encoded = null;
        for (var index = 0; index < iterations; index++)
        {
            encoded = codec.Encode(sample);
            _ = codec.Decode(encoded);
        }

        timer.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var vector = WireFrame.Encode(codec.Id, 1, 0, sample.MessageId, encoded!);
        reports.Add(
            new CodecReport(
                codec.Id.ToString(),
                codec.Name,
                encoded!.Length,
                timer.Elapsed.TotalMilliseconds,
                allocated,
                Convert.ToHexStringLower(SHA256.HashData(vector))));
    }

    return reports;
}

static IReadOnlyList<BufferReport> RunBufferBenchmarks()
{
    const int elementCount = 138_208;
    var values = Enumerable.Range(0, elementCount)
        .Select(index => (MathF.Sin(index * 0.013f) * 0.8f) + (MathF.Cos(index * 0.001f) * 0.2f))
        .ToArray();
    var reports = new List<BufferReport>();

    var raw = Measure("float32-le", () => BufferEncoding.EncodeFloat32(values), bytes => BufferEncoding.DecodeFloat32(bytes));
    reports.Add(raw with { MaximumAbsoluteError = 0, RootMeanSquareError = 0 });

    var half = Measure("float16-le", () => BufferEncoding.EncodeFloat16(values), bytes => BufferEncoding.DecodeFloat16(bytes));
    var halfDecoded = BufferEncoding.DecodeFloat16(BufferEncoding.EncodeFloat16(values));
    reports.Add(half with { MaximumAbsoluteError = MaximumError(values, halfDecoded), RootMeanSquareError = RootMeanSquareError(values, halfDecoded) });

    var rawBytes = BufferEncoding.EncodeFloat32(values);
    var lz4 = Measure("float32-le+lz4", () => BufferEncoding.CompressLz4(rawBytes), bytes => BufferEncoding.DecompressLz4(bytes, rawBytes.Length));
    reports.Add(lz4 with { MaximumAbsoluteError = 0, RootMeanSquareError = 0 });
    return reports;

    static BufferReport Measure(string name, Func<byte[]> encode, Func<byte[], Array> decode)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        var bytes = encode();
        var encodedMilliseconds = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        _ = decode(bytes);
        timer.Stop();
        return new BufferReport(
            name,
            bytes.Length,
            encodedMilliseconds,
            timer.Elapsed.TotalMilliseconds,
            GC.GetAllocatedBytesForCurrentThread() - before,
            0,
            0);
    }

    static double MaximumError(IReadOnlyList<float> expected, IReadOnlyList<float> actual) =>
        expected.Zip(actual).Max(pair => Math.Abs((double)pair.First - pair.Second));

    static double RootMeanSquareError(IReadOnlyList<float> expected, IReadOnlyList<float> actual) =>
        Math.Sqrt(expected.Zip(actual).Average(pair => Math.Pow((double)pair.First - pair.Second, 2)));
}

static async Task<bool> VerifyMalformedFrameRejectedAsync(
    ClientOptions options,
    ServerIdentityVerifier verifier,
    string token)
{
    using var socket = new ClientWebSocket();
    socket.Options.RemoteCertificateValidationCallback = verifier.ValidateSocket;
    socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
    socket.Options.SetRequestHeader("Origin", "hibop://xr");
    var endpoint = new UriBuilder(options.Endpoint) { Scheme = "wss", Path = "/control" }.Uri;
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await socket.ConnectAsync(endpoint, timeout.Token);
    await socket.SendAsync(new byte[WireFrame.HeaderLength], WebSocketMessageType.Binary, true, timeout.Token);
    var response = new byte[128];
    var result = await socket.ReceiveAsync(response, timeout.Token);
    return result.MessageType == WebSocketMessageType.Close
        && result.CloseStatus == WebSocketCloseStatus.InvalidPayloadData;
}

static async Task<CommandReport> RunCommandsAsync(
    ClientOptions options,
    ServerIdentityVerifier verifier,
    string token,
    CancellationToken cancellationToken)
{
    using var socket = new ClientWebSocket();
    socket.Options.RemoteCertificateValidationCallback = verifier.ValidateSocket;
    socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
    socket.Options.SetRequestHeader("Origin", "hibop://xr");
    var endpoint = new UriBuilder(options.Endpoint) { Scheme = "wss", Path = "/control" }.Uri;
    await socket.ConnectAsync(endpoint, cancellationToken);

    var codec = ControlCodecs.Get(ControlCodecId.Protobuf);
    var latencies = new List<double>();
    var failures = 0;
    var interval = TimeSpan.FromSeconds(1d / options.CommandsPerSecond);
    var deadline = Stopwatch.StartNew();
    ulong messageId = 1;

    while (deadline.Elapsed < TimeSpan.FromSeconds(options.DurationSeconds))
    {
        var started = Stopwatch.GetTimestamp();
        var sample = new ControlSample(1, messageId, messageId, (uint)messageId, DateTime.UtcNow.Ticks, "echo", [1, 2, 3]);
        var frame = WireFrame.Encode(codec.Id, 1, 0, messageId, codec.Encode(sample));
        try
        {
            await socket.SendAsync(frame, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, cancellationToken);
            var response = new byte[WireFrame.MaximumFrameLength];
            var received = await socket.ReceiveAsync(response, cancellationToken);
            if (!received.EndOfMessage || received.MessageType != WebSocketMessageType.Binary)
            {
                failures++;
            }
            else
            {
                var decoded = WireFrame.Decode(response.AsMemory(0, received.Count));
                var echoed = codec.Decode(decoded.Payload);
                if (echoed.MessageId != messageId)
                {
                    failures++;
                }
                else
                {
                    latencies.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                }
            }
        }
        catch (WebSocketException)
        {
            failures++;
        }

        messageId++;
        var remaining = interval - Stopwatch.GetElapsedTime(started);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, cancellationToken);
        }
    }

    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "complete", cancellationToken);
    latencies.Sort();
    return new CommandReport(
        latencies.Count,
        failures,
        Percentile(latencies, 0.50),
        Percentile(latencies, 0.95),
        latencies.Count == 0 ? double.NaN : latencies[^1]);
}

static async Task<AssetReport> DownloadAssetAsync(HttpClient http, CancellationToken cancellationToken)
{
    var manifest = JsonSerializer.Deserialize<AssetManifest>(await http.GetStringAsync("/asset/manifest", cancellationToken), JsonDefaults.Options)
        ?? throw new InvalidDataException("Asset manifest was empty.");
    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    var timer = Stopwatch.StartNew();
    long received = 0;

    while (received < manifest.Length)
    {
        var end = Math.Min(received + manifest.ChunkLength, manifest.Length) - 1;
        using var request = new HttpRequestMessage(HttpMethod.Get, "/asset/data");
        request.Headers.Range = new RangeHeaderValue(received, end);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode != HttpStatusCode.PartialContent)
        {
            throw new InvalidDataException($"Expected HTTP 206, received {(int)response.StatusCode}.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var expectedChunkHash = response.Headers.GetValues("X-P06-Chunk-SHA256").Single();
        var actualChunkHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (!string.Equals(expectedChunkHash, actualChunkHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Asset chunk hash mismatch.");
        }

        hash.AppendData(bytes);
        received += bytes.Length;
    }

    timer.Stop();
    var actualHash = Convert.ToHexStringLower(hash.GetHashAndReset());
    if (!string.Equals(manifest.Sha256, actualHash, StringComparison.Ordinal))
    {
        throw new InvalidDataException("Final asset hash mismatch.");
    }

    return new AssetReport(
        received,
        manifest.ChunkLength,
        actualHash,
        timer.Elapsed.TotalSeconds,
        (received * 8d) / timer.Elapsed.TotalSeconds / 1_000_000d);
}

static double Percentile(IReadOnlyList<double> sorted, double percentile)
{
    if (sorted.Count == 0)
    {
        return double.NaN;
    }

    var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
    return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
}

internal sealed class ServerIdentityVerifier
{
    private readonly string pairingCode;

    public ServerIdentityVerifier(string pairingCode, string? expectedPinHex)
    {
        this.pairingCode = pairingCode;
        if (!string.IsNullOrWhiteSpace(expectedPinHex))
        {
            Pin = Convert.FromHexString(expectedPinHex);
            if (Pin.Length != SHA256.HashSizeInBytes)
            {
                throw new ArgumentException("--expected-pin must be a 32-byte SHA-256 value.");
            }
        }
    }

    public byte[]? Pin { get; private set; }

    public bool ValidateHttp(
        HttpRequestMessage? request,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors errors) => ValidateCertificate(certificate);

    public bool ValidateSocket(
        object? sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors) => ValidateCertificate(certificate);

    private bool ValidateCertificate(X509Certificate? certificate)
    {
        if (certificate is null)
        {
            return false;
        }

        using var certificate2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
        var candidatePin = CertificateIdentity.ComputeSpkiPin(certificate2);
        if (Pin is null)
        {
            if (!string.Equals(
                    pairingCode,
                    CertificateIdentity.ComputeShortAuthenticationString(candidatePin),
                    StringComparison.Ordinal))
            {
                return false;
            }

            Pin = candidatePin;
            return true;
        }

        return CertificateIdentity.PinsMatch(Pin, candidatePin);
    }
}

internal static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
}

internal sealed record PairingResponse(string AccessToken, string Pin);

internal sealed record AssetManifest(long Length, string Sha256, int ChunkLength);

internal sealed record CodecReport(string Id, string Name, int EncodedBytes, double RoundTripMilliseconds, long AllocatedBytes, string WireVectorSha256);

internal sealed record BufferReport(
    string Name,
    int EncodedBytes,
    double EncodeMilliseconds,
    double DecodeMilliseconds,
    long AllocatedBytes,
    double MaximumAbsoluteError,
    double RootMeanSquareError);

internal sealed record CommandReport(int Successes, int Failures, double P50Milliseconds, double P95Milliseconds, double MaximumMilliseconds);

internal sealed record AssetReport(long Bytes, int ChunkBytes, string Sha256, double Seconds, double MegabitsPerSecond);

internal sealed record ClientReport(
    DateTimeOffset Timestamp,
    string OperatingSystem,
    string RuntimeIdentifier,
    string Endpoint,
    string SpkiSha256,
    IReadOnlyList<CodecReport> Codecs,
    IReadOnlyList<BufferReport> Buffers,
    bool MalformedFrameRejected,
    CommandReport Commands,
    AssetReport Asset,
    long AllocatedBytes,
    long PeakWorkingSetBytes,
    double TotalSeconds);

internal sealed record ClientOptions(
    Uri Endpoint,
    string PairingCode,
    int DurationSeconds,
    int CommandsPerSecond,
    int CodecIterations,
    string? ExpectedPinHex,
    string? Output)
{
    public static ClientOptions Parse(string[] args)
    {
        var values = args
            .Chunk(2)
            .Where(pair => pair.Length == 2 && pair[0].StartsWith("--", StringComparison.Ordinal))
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);
        var endpoint = new Uri(values.GetValueOrDefault("--endpoint", "https://127.0.0.1:5443"), UriKind.Absolute);
        var pairingCode = values.GetValueOrDefault("--pair-code")
            ?? throw new ArgumentException("--pair-code is required.");
        if (pairingCode.Length != 6 || pairingCode.Any(character => !char.IsAsciiDigit(character)))
        {
            throw new ArgumentException("--pair-code must contain exactly six digits.");
        }

        return new ClientOptions(
            endpoint,
            pairingCode,
            int.Parse(values.GetValueOrDefault("--duration-seconds", "30"), CultureInfo.InvariantCulture),
            int.Parse(values.GetValueOrDefault("--commands-per-second", "20"), CultureInfo.InvariantCulture),
            int.Parse(values.GetValueOrDefault("--codec-iterations", "100000"), CultureInfo.InvariantCulture),
            values.GetValueOrDefault("--expected-pin"),
            values.GetValueOrDefault("--output"));
    }
}
