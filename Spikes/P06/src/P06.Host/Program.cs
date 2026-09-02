using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text.Json;
using CRNL.HiBoP.Spikes.P06;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Net.Http.Headers;

var options = HostOptions.Parse(args);
using var certificate = CertificateIdentity.CreateEphemeral(options.AdvertisedAddress);
var pairingCode = CertificateIdentity.ComputeShortAuthenticationString(certificate);
var assetHash = SyntheticAsset.ComputeSha256Hex(options.AssetLength);
var bearerTokens = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
var pairingAttempts = new Queue<DateTimeOffset>();
var tokenLock = new object();
var tokenTtl = TimeSpan.FromMinutes(30);

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(settings => settings.SingleLine = true);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestBodySize = ControlCodecs.MaximumEncodedBytes;
    kestrel.Limits.MaxConcurrentConnections = 16;
    kestrel.Limits.MaxConcurrentUpgradedConnections = 2;
    kestrel.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
    kestrel.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
    kestrel.Listen(options.ListenAddress, options.Port, endpoint =>
    {
        endpoint.Protocols = HttpProtocols.Http1AndHttp2;
        endpoint.UseHttps(
            https =>
            {
                https.ServerCertificate = certificate;
                https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            });
    });
});

var app = builder.Build();
app.UseWebSockets(
    new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(15),
        AllowedOrigins = { "hibop://xr" },
    });

app.MapGet("/health", () => Results.Json(new { status = "ok", protocol = 1 }));

app.MapPost("/pair", (HttpContext context) =>
{
    if (!TryConsumePairingAttempt())
    {
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    var suppliedCode = context.Request.Headers["X-P06-Pair-Code"].ToString();
    if (!string.Equals(suppliedCode, pairingCode, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    lock (tokenLock)
    {
        PruneExpiredTokens(DateTimeOffset.UtcNow);
        if (bearerTokens.Count >= 4)
        {
            var oldest = bearerTokens.MinBy(pair => pair.Value).Key;
            bearerTokens.Remove(oldest);
        }

        bearerTokens.Add(token, DateTimeOffset.UtcNow.Add(tokenTtl));
    }

    return Results.Json(new { accessToken = token, pin = Convert.ToHexStringLower(CertificateIdentity.ComputeSpkiPin(certificate)) });
});

app.MapGet("/asset/manifest", (HttpContext context) =>
{
    return IsAuthorized(context)
        ? Results.Json(new { length = options.AssetLength, sha256 = assetHash, chunkLength = SyntheticAsset.DefaultChunkLength })
        : Results.Unauthorized();
});

app.MapGet("/asset/data", async (HttpContext context) =>
{
    if (!IsAuthorized(context))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    if (!TryParseRange(context.Request.Headers.Range.ToString(), options.AssetLength, out var start, out var end))
    {
        context.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
        context.Response.Headers.ContentRange = $"bytes */{options.AssetLength}";
        return;
    }

    var count = checked((int)(end - start + 1));
    if (count > SyntheticAsset.DefaultChunkLength)
    {
        context.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
        return;
    }

    var bytes = new byte[count];
    SyntheticAsset.Fill(bytes, start);
    context.Response.StatusCode = StatusCodes.Status206PartialContent;
    context.Response.ContentType = "application/octet-stream";
    context.Response.ContentLength = count;
    context.Response.Headers.ContentRange = $"bytes {start}-{end}/{options.AssetLength}";
    context.Response.Headers["X-P06-Chunk-SHA256"] = Convert.ToHexStringLower(SHA256.HashData(bytes));
    if (options.CorruptChunkAt >= start && options.CorruptChunkAt <= end)
    {
        bytes[options.CorruptChunkAt - start] ^= 0xff;
    }

    if (options.BulkMegabitsPerSecond > 0)
    {
        var delaySeconds = (count * 8d) / (options.BulkMegabitsPerSecond * 1_000_000d);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), context.RequestAborted);
    }

    await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
});

app.MapGet("/control", async (HttpContext context) =>
{
    if (!IsAuthorized(context))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    try
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[WireFrame.MaximumFrameLength];
        var rateWindowStarted = Stopwatch.GetTimestamp();
        var framesInRateWindow = 0;
        while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
        {
            var count = 0;
            ValueWebSocketReceiveResult result;
            do
            {
                if (count == buffer.Length)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "frame limit", context.RequestAborted);
                    return;
                }

                result = await socket.ReceiveAsync(buffer.AsMemory(count), context.RequestAborted);
                count += result.Count;
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", context.RequestAborted);
                return;
            }

            if (result.MessageType != WebSocketMessageType.Binary)
            {
                await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "binary only", context.RequestAborted);
                return;
            }

            if (Stopwatch.GetElapsedTime(rateWindowStarted) >= TimeSpan.FromSeconds(1))
            {
                rateWindowStarted = Stopwatch.GetTimestamp();
                framesInRateWindow = 0;
            }

            framesInRateWindow++;
            if (framesInRateWindow > 100)
            {
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "rate limit", context.RequestAborted);
                return;
            }

            try
            {
                var decoded = WireFrame.Decode(buffer.AsMemory(0, count));
                _ = ControlCodecs.Get(decoded.Codec).Decode(decoded.Payload);
            }
            catch (Exception exception) when (exception is InvalidDataException or ArgumentException or Google.Protobuf.InvalidProtocolBufferException or MemoryPack.MemoryPackSerializationException or MessagePack.MessagePackSerializationException)
            {
                await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "invalid frame", context.RequestAborted);
                return;
            }

            await socket.SendAsync(
                buffer.AsMemory(0, count),
                WebSocketMessageType.Binary,
                WebSocketMessageFlags.EndOfMessage | WebSocketMessageFlags.DisableCompression,
                context.RequestAborted);
        }
    }
    catch (WebSocketException)
    {
        // A peer may drop the connection without a close handshake. The session is already discarded.
    }
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    var startup = new
    {
        status = "ready",
        endpoint = $"https://{options.DisplayAddress}:{options.Port}",
        pairingCode,
        spkiSha256 = Convert.ToHexStringLower(CertificateIdentity.ComputeSpkiPin(certificate)),
        processId = Environment.ProcessId,
    };
    var json = JsonSerializer.Serialize(startup);
    Console.WriteLine($"P06_READY {json}");
    if (!string.IsNullOrWhiteSpace(options.ReadyFile))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.ReadyFile))!);
        File.WriteAllText(options.ReadyFile, json);
    }
});

await app.RunAsync();

bool IsAuthorized(HttpContext context)
{
    var token = context.Request.Headers["X-P06-Access-Token"].ToString();
    var authorization = context.Request.Headers.Authorization.ToString();
    const string prefix = "Bearer ";
    if (string.IsNullOrEmpty(token) && authorization.StartsWith(prefix, StringComparison.Ordinal))
    {
        token = authorization[prefix.Length..];
    }

    if (string.IsNullOrEmpty(token))
    {
        return false;
    }

    lock (tokenLock)
    {
        var now = DateTimeOffset.UtcNow;
        PruneExpiredTokens(now);
        return bearerTokens.TryGetValue(token, out var expiresAt)
            && expiresAt > now;
    }
}

bool TryConsumePairingAttempt()
{
    lock (tokenLock)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        while (pairingAttempts.TryPeek(out var attempt) && attempt < cutoff)
        {
            pairingAttempts.Dequeue();
        }

        if (pairingAttempts.Count >= 10)
        {
            return false;
        }

        pairingAttempts.Enqueue(DateTimeOffset.UtcNow);
        return true;
    }
}

void PruneExpiredTokens(DateTimeOffset now)
{
    foreach (var token in bearerTokens.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray())
    {
        bearerTokens.Remove(token);
    }
}

static bool TryParseRange(string rangeHeader, long totalLength, out long start, out long end)
{
    start = 0;
    end = 0;
    if (!rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var parts = rangeHeader[6..].Split('-', 2);
    if (parts.Length != 2
        || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out start)
        || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out end))
    {
        return false;
    }

    return start >= 0 && end >= start && end < totalLength;
}

internal sealed record HostOptions(
    IPAddress ListenAddress,
    string DisplayAddress,
    string? AdvertisedAddress,
    int Port,
    long AssetLength,
    double BulkMegabitsPerSecond,
    long CorruptChunkAt,
    string? ReadyFile)
{
    public static HostOptions Parse(string[] args)
    {
        var values = args
            .Chunk(2)
            .Where(pair => pair.Length == 2 && pair[0].StartsWith("--", StringComparison.Ordinal))
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);
        var listenText = values.GetValueOrDefault("--listen", "127.0.0.1");
        if (!IPAddress.TryParse(listenText, out var listenAddress))
        {
            throw new ArgumentException("--listen must be an IP address.");
        }

        return new HostOptions(
            listenAddress,
            values.GetValueOrDefault("--display", listenText),
            values.GetValueOrDefault("--advertise", listenText),
            int.Parse(values.GetValueOrDefault("--port", "5443"), CultureInfo.InvariantCulture),
            long.Parse(values.GetValueOrDefault("--asset-bytes", SyntheticAsset.DefaultLength.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
            double.Parse(values.GetValueOrDefault("--bulk-mbps", "100"), CultureInfo.InvariantCulture),
            long.Parse(values.GetValueOrDefault("--corrupt-chunk-at", "-1"), CultureInfo.InvariantCulture),
            values.GetValueOrDefault("--ready-file"));
    }
}
