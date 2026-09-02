using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CRNL.HiBoP.Spikes.P06;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;
using Debug = UnityEngine.Debug;

namespace CRNL.HiBoP.Spikes.P06.UnityClient
{
    public sealed class P06TransportProbe : MonoBehaviour
    {
        [SerializeField] private string hostAddress = "192.168.1.2";
        [SerializeField] private string pairingCode = string.Empty;

        private string status = "Saisir l’adresse du PC et le code affiché sur le Desktop.";
        private string automatedMode = "smoke";
        private bool running;

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = activity.Call<AndroidJavaObject>("getIntent");
            var automatedHost = intent.Call<string>("getStringExtra", "p06Host");
            var automatedPairingCode = intent.Call<string>("getStringExtra", "p06PairCode");
            automatedMode = intent.Call<string>("getStringExtra", "p06Mode") ?? "smoke";
            if (!string.IsNullOrWhiteSpace(automatedHost) && !string.IsNullOrWhiteSpace(automatedPairingCode))
            {
                hostAddress = automatedHost;
                pairingCode = automatedPairingCode;
                _ = RunAndReportAsync();
            }
#endif
        }

        private void OnGUI()
        {
            const float width = 760f;
            GUILayout.BeginArea(new Rect(40, 40, width, Screen.height - 80), GUI.skin.box);
            GUILayout.Label("HiBoP P06 — transport Quest IL2CPP");
            GUILayout.Label("Adresse IPv4 du Desktop");
            hostAddress = GUILayout.TextField(hostAddress, 64);
            GUILayout.Label("Code d’appairage (6 chiffres)");
            pairingCode = GUILayout.TextField(pairingCode, 6);
            GUI.enabled = !running;
            if (GUILayout.Button("Tester HTTPS + WSS + codecs"))
            {
                _ = RunAndReportAsync();
            }

            GUI.enabled = true;
            GUILayout.Space(20);
            GUILayout.TextArea(status, GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
        }

        private async Task RunAndReportAsync()
        {
            running = true;
            try
            {
                if (pairingCode.Length != 6 || pairingCode.Any(character => character < '0' || character > '9'))
                {
                    throw new InvalidOperationException("Le code doit contenir exactement six chiffres.");
                }

                var verifier = new P06CertificateVerifier(pairingCode);
                var endpoint = $"https://{hostAddress}:5443";
                if (string.Equals(automatedMode, "identity-reject", StringComparison.Ordinal))
                {
                    await VerifyIdentityRejectedAsync(endpoint, pairingCode, verifier);
                    return;
                }

                status = "Appairage TLS…";
                var pairing = await PairAsync(endpoint, pairingCode, verifier);
                status = "Golden vectors codecs…";
                var codecResults = VerifyCodecs();
                if (string.Equals(automatedMode, "corruption-reject", StringComparison.Ordinal))
                {
                    await VerifyCorruptionRejectedAsync(endpoint, pairing.accessToken, verifier);
                    return;
                }

                if (string.Equals(automatedMode, "load", StringComparison.Ordinal))
                {
                    status = "Charge nominale 120 s : contrôle WSS + asset HTTPS 100 Mio…";
                    var load = await RunNominalLoadAsync(endpoint, hostAddress, pairing.accessToken, verifier);
                    var loadReport = new P06QuestReport
                    {
                        platform = Application.platform.ToString(),
                        unity = Application.unityVersion,
                        scriptingBackend = "IL2CPP-required-by-build",
                        pin = verifier.PinHex,
                        codecs = codecResults.ToArray(),
                        load = load,
                        result = "PASS",
                    };
                    Debug.Log("P06_QUEST_LOAD " + JsonUtility.ToJson(load));
                    status = JsonUtility.ToJson(loadReport, true);
                    Debug.Log("P06_QUEST_REPORT " + status);
                    return;
                }

                status = "Echo WSS épinglé…";
                var wssMilliseconds = await EchoAsync(hostAddress, pairing.accessToken, verifier);
                status = "Range HTTPS épinglé…";
                var chunkBytes = await DownloadFirstChunkAsync(endpoint, pairing.accessToken, verifier);
                var report = new P06QuestReport
                {
                    platform = Application.platform.ToString(),
                    unity = Application.unityVersion,
                    scriptingBackend = "IL2CPP-required-by-build",
                    pin = verifier.PinHex,
                    codecs = codecResults.ToArray(),
                    wssRoundTripMilliseconds = wssMilliseconds,
                    httpsChunkBytes = chunkBytes,
                    result = "PASS",
                };
                status = JsonUtility.ToJson(report, true);
                Debug.Log("P06_QUEST_REPORT " + status);
            }
            catch (Exception exception)
            {
                status = "FAIL\n" + exception.GetType().Name + ": " + exception.Message;
                Debug.LogError("P06_QUEST_REPORT " + status);
            }
            finally
            {
                running = false;
            }
        }

        private static async Task<PairingResponse> PairAsync(string endpoint, string code, P06CertificateVerifier verifier)
        {
            using var request = new UnityWebRequest(endpoint + "/pair", UnityWebRequest.kHttpVerbPOST)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new P06CertificateHandler(verifier),
            };
            request.SetRequestHeader("X-P06-Pair-Code", code);
            await SendAsync(request);
            EnsureSuccess(request);
            return JsonUtility.FromJson<PairingResponse>(request.downloadHandler.text) ?? throw new InvalidOperationException("Réponse d’appairage vide.");
        }

        private static IReadOnlyList<CodecResult> VerifyCodecs()
        {
            var sample = new ControlSample(1, 0x0102030405060708, 0x1112131415161718, 42, 638923456789012345, new string('x', 256), Enumerable.Range(-16, 33).ToArray());
            var results = new List<CodecResult>();
            foreach (var codec in ControlCodecs.All)
            {
                var timer = Stopwatch.StartNew();
                var encoded = codec.Encode(sample);
                var decoded = codec.Decode(encoded);
                var frame = WireFrame.Encode(codec.Id, 1, 0, sample.MessageId, encoded);
                timer.Stop();
                if (decoded.MessageId != sample.MessageId || decoded.Payload != sample.Payload)
                {
                    throw new InvalidOperationException(codec.Name + " diverge sous IL2CPP.");
                }

                var expectedHash = codec.Id switch
                {
                    ControlCodecId.Protobuf => "5eea415f5b350eca305a1684c8b5235139b08ef189835a8d1ce4942d4c223db8",
                    ControlCodecId.MessagePack => "7dc75b5d0bc6f0558ff7cbf9c9c1e06b8faa2de65eb3dd2ffabb0254a696d12b",
                    ControlCodecId.MemoryPack => "13d13f3919452733e77733a04bcd95b26ad03037aa2c5fda885b3189dac9bfd3",
                    _ => throw new InvalidOperationException("Codec sans golden vector."),
                };
                if (!string.Equals(P06TransportProbe.Hex(Sha256(frame)), expectedHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(codec.Name + " diverge du golden vector sous IL2CPP.");
                }

                results.Add(new CodecResult
                {
                    name = codec.Name,
                    encodedBytes = encoded.Length,
                    roundTripMilliseconds = timer.Elapsed.TotalMilliseconds,
                });
            }

            return results;
        }

        private static async Task VerifyIdentityRejectedAsync(string endpoint, string code, P06CertificateVerifier verifier)
        {
            try
            {
                await PairAsync(endpoint, code, verifier);
            }
            catch (Exception) when (verifier.PairingCodeMismatchObserved)
            {
                Debug.Log("P06_QUEST_NEGATIVE {\"case\":\"identity_changed\",\"result\":\"PASS\"}");
                return;
            }

            throw new InvalidOperationException("Une identité TLS modifiée a été acceptée.");
        }

        private static async Task VerifyCorruptionRejectedAsync(string endpoint, string token, P06CertificateVerifier verifier)
        {
            try
            {
                await DownloadFirstChunkAsync(endpoint, token, verifier);
            }
            catch (InvalidOperationException exception) when (exception.Message == "Hash de chunk invalide.")
            {
                Debug.Log("P06_QUEST_NEGATIVE {\"case\":\"corrupted_chunk\",\"result\":\"PASS\"}");
                return;
            }

            throw new InvalidOperationException("Un chunk corrompu a été accepté.");
        }

        private static async Task<double> EchoAsync(string host, string token, P06CertificateVerifier verifier)
        {
            var completion = new TaskCompletionSource<double>();
            using var socket = new WebSocket("wss://" + host + ":5443/control");
            socket.Origin = "hibop://xr";
            socket.SetUserHeader("X-P06-Access-Token", token);
            socket.SslConfiguration.ServerCertificateValidationCallback = verifier.ValidateSocket;
            var codec = ControlCodecs.Get(ControlCodecId.Protobuf);
            var frame = WireFrame.Encode(codec.Id, 1, 0, 1, codec.Encode(new ControlSample(1, 1, 1, 1, DateTime.UtcNow.Ticks, "echo", Array.Empty<int>())));
            long started = 0;
            socket.OnOpen += (_, _) =>
            {
                started = Stopwatch.GetTimestamp();
                socket.Send(frame);
            };
            socket.OnMessage += (_, eventArgs) =>
            {
                try
                {
                    var decodedFrame = WireFrame.Decode(eventArgs.RawData);
                    var decoded = codec.Decode(decodedFrame.Payload);
                    if (decoded.MessageId != 1)
                    {
                        throw new InvalidOperationException("L’écho WSS a changé l’identifiant du message.");
                    }

                    completion.TrySetResult(((Stopwatch.GetTimestamp() - started) * 1000d) / Stopwatch.Frequency);
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            };
            socket.OnError += (_, eventArgs) => completion.TrySetException(new InvalidOperationException(eventArgs.Message));
            socket.OnClose += (_, eventArgs) =>
            {
                if (!completion.Task.IsCompleted)
                {
                    completion.TrySetException(new InvalidOperationException("WSS fermé: " + eventArgs.Reason));
                }
            };
            socket.ConnectAsync();
            var finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            if (finished != completion.Task)
            {
                throw new TimeoutException("Timeout WSS.");
            }

            var elapsed = await completion.Task;
            socket.Close();
            return elapsed;
        }

        private static async Task<LoadResult> RunNominalLoadAsync(string endpoint, string host, string token, P06CertificateVerifier verifier)
        {
            var controlTask = RunControlLoadAsync(host, token, verifier, TimeSpan.FromSeconds(120));
            var bulkTask = DownloadAssetAsync(endpoint, token, verifier);
            await Task.WhenAll(controlTask, bulkTask);
            return new LoadResult
            {
                control = await controlTask,
                bulk = await bulkTask,
            };
        }

        private static async Task<ControlLoadResult> RunControlLoadAsync(string host, string token, P06CertificateVerifier verifier, TimeSpan duration)
        {
            var opened = new TaskCompletionSource<bool>();
            var timestamps = new Dictionary<ulong, long>();
            var roundTrips = new List<double>();
            var synchronization = new object();
            string socketError = null;
            using var socket = new WebSocket("wss://" + host + ":5443/control");
            socket.Origin = "hibop://xr";
            socket.SetUserHeader("X-P06-Access-Token", token);
            socket.SslConfiguration.ServerCertificateValidationCallback = verifier.ValidateSocket;
            var codec = ControlCodecs.Get(ControlCodecId.Protobuf);
            socket.OnOpen += (_, _) => opened.TrySetResult(true);
            socket.OnMessage += (_, eventArgs) =>
            {
                try
                {
                    var frame = WireFrame.Decode(eventArgs.RawData);
                    _ = codec.Decode(frame.Payload);
                    lock (synchronization)
                    {
                        if (timestamps.Remove(frame.MessageId, out var started))
                        {
                            roundTrips.Add(((Stopwatch.GetTimestamp() - started) * 1000d) / Stopwatch.Frequency);
                        }
                    }
                }
                catch (Exception exception)
                {
                    socketError = exception.Message;
                }
            };
            socket.OnError += (_, eventArgs) =>
            {
                socketError = eventArgs.Message;
                opened.TrySetException(new InvalidOperationException(eventArgs.Message));
            };
            socket.OnClose += (_, eventArgs) =>
            {
                if (eventArgs.Code != 1000)
                {
                    socketError = "WSS fermé: " + eventArgs.Reason;
                }
            };
            socket.ConnectAsync();
            var connected = await Task.WhenAny(opened.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            if (connected != opened.Task)
            {
                throw new TimeoutException("Timeout connexion WSS sous charge.");
            }

            await opened.Task;
            var timer = Stopwatch.StartNew();
            ulong messageId = 0;
            var nextSend = (double)Stopwatch.GetTimestamp();
            while (timer.Elapsed < duration)
            {
                if (!string.IsNullOrEmpty(socketError))
                {
                    throw new InvalidOperationException(socketError);
                }

                messageId++;
                var payload = codec.Encode(new ControlSample(1, messageId, 0, (uint)messageId, DateTime.UtcNow.Ticks, new string('x', 256), Array.Empty<int>()));
                var frame = WireFrame.Encode(codec.Id, 1, 0, messageId, payload);
                lock (synchronization)
                {
                    timestamps[messageId] = Stopwatch.GetTimestamp();
                }

                socket.Send(frame);
                nextSend += Stopwatch.Frequency / 20d;
                var remainingSeconds = (nextSend - Stopwatch.GetTimestamp()) / Stopwatch.Frequency;
                if (remainingSeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(remainingSeconds));
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
            socket.Close();
            double[] samples;
            int pending;
            lock (synchronization)
            {
                samples = roundTrips.ToArray();
                pending = timestamps.Count;
            }

            if (samples.Length == 0)
            {
                throw new InvalidOperationException("Aucun écho WSS reçu sous charge.");
            }

            Array.Sort(samples);
            return new ControlLoadResult
            {
                sent = checked((int)messageId),
                received = samples.Length,
                failures = pending,
                p50Milliseconds = Percentile(samples, 0.50),
                p95Milliseconds = Percentile(samples, 0.95),
                maxMilliseconds = samples[samples.Length - 1],
            };
        }

        private static async Task<BulkLoadResult> DownloadAssetAsync(string endpoint, string token, P06CertificateVerifier verifier)
        {
            using var manifestRequest = UnityWebRequest.Get(endpoint + "/asset/manifest");
            manifestRequest.certificateHandler = new P06CertificateHandler(verifier);
            manifestRequest.SetRequestHeader("Authorization", "Bearer " + token);
            await SendAsync(manifestRequest);
            EnsureSuccess(manifestRequest);
            var manifest = JsonUtility.FromJson<AssetManifest>(manifestRequest.downloadHandler.text) ?? throw new InvalidOperationException("Manifest asset vide.");
            using var hash = SHA256.Create();
            var timer = Stopwatch.StartNew();
            long downloaded = 0;
            while (downloaded < manifest.length)
            {
                var end = Math.Min(downloaded + manifest.chunkLength, manifest.length) - 1;
                using var request = UnityWebRequest.Get(endpoint + "/asset/data");
                request.certificateHandler = new P06CertificateHandler(verifier);
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("Range", $"bytes={downloaded}-{end}");
                await SendAsync(request);
                EnsureSuccess(request, 206);
                var bytes = request.downloadHandler.data;
                var expectedChunk = request.GetResponseHeader("X-P06-Chunk-SHA256");
                if (!string.Equals(expectedChunk, Hex(Sha256(bytes)), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Hash de chunk invalide sous charge.");
                }

                hash.TransformBlock(bytes, 0, bytes.Length, null, 0);
                downloaded += bytes.Length;
            }

            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            timer.Stop();
            if (!string.Equals(manifest.sha256, Hex(hash.Hash), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Hash final asset invalide sous charge.");
            }

            return new BulkLoadResult
            {
                bytes = downloaded,
                durationMilliseconds = timer.Elapsed.TotalMilliseconds,
                usefulMegabitsPerSecond = (downloaded * 8d) / (timer.Elapsed.TotalSeconds * 1_000_000d),
            };
        }

        private static double Percentile(double[] sorted, double percentile)
        {
            var index = (int)Math.Ceiling(sorted.Length * percentile) - 1;
            return sorted[Math.Max(0, Math.Min(index, sorted.Length - 1))];
        }

        private static async Task<int> DownloadFirstChunkAsync(string endpoint, string token, P06CertificateVerifier verifier)
        {
            using var request = UnityWebRequest.Get(endpoint + "/asset/data");
            request.certificateHandler = new P06CertificateHandler(verifier);
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.SetRequestHeader("Range", "bytes=0-1048575");
            await SendAsync(request);
            EnsureSuccess(request, 206);
            var bytes = request.downloadHandler.data;
            var expected = request.GetResponseHeader("X-P06-Chunk-SHA256");
            var actual = Hex(Sha256(bytes));
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Hash de chunk invalide.");
            }

            return bytes.Length;
        }

        private static void EnsureSuccess(UnityWebRequest request, long expectedCode = 200)
        {
            if (request.result != UnityWebRequest.Result.Success || request.responseCode != expectedCode)
            {
                throw new InvalidOperationException($"HTTP {request.responseCode}: {request.error}");
            }
        }

        private static Task SendAsync(UnityWebRequest request)
        {
            var completion = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();
            operation.completed += _ => completion.TrySetResult(true);
            return completion.Task;
        }

        internal static byte[] Sha256(byte[] bytes)
        {
            using var algorithm = SHA256.Create();
            return algorithm.ComputeHash(bytes);
        }

        internal static string Hex(byte[] bytes) => string.Concat(bytes.Select(value => value.ToString("x2")));
    }

    internal sealed class P06CertificateHandler : CertificateHandler
    {
        private readonly P06CertificateVerifier verifier;

        public P06CertificateHandler(P06CertificateVerifier verifier)
        {
            this.verifier = verifier;
        }

        protected override bool ValidateCertificate(byte[] certificateData) => verifier.Validate(certificateData);
    }

    internal sealed class P06CertificateVerifier
    {
        private readonly string pairingCode;
        private byte[] pin;

        public P06CertificateVerifier(string pairingCode)
        {
            this.pairingCode = pairingCode;
        }

        public string PinHex => pin == null ? string.Empty : P06TransportProbe.Hex(pin);

        public bool PairingCodeMismatchObserved { get; private set; }

        public bool ValidateSocket(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors errors) => certificate != null && Validate(certificate.GetRawCertData());

        public bool Validate(byte[] certificateData)
        {
            try
            {
                var candidate = P06TransportProbe.Sha256(ExtractSubjectPublicKeyInfo(certificateData));
                if (pin == null)
                {
                    var firstTwentyBits = ((candidate[0] << 12) | (candidate[1] << 4) | (candidate[2] >> 4)) % 1_000_000;
                    if (!string.Equals(firstTwentyBits.ToString("D6"), pairingCode, StringComparison.Ordinal))
                    {
                        PairingCodeMismatchObserved = true;
                        return false;
                    }

                    pin = candidate;
                    return true;
                }

                if (pin.Length != candidate.Length)
                {
                    return false;
                }

                var difference = 0;
                for (var index = 0; index < pin.Length; index++)
                {
                    difference |= pin[index] ^ candidate[index];
                }

                return difference == 0;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static byte[] ExtractSubjectPublicKeyInfo(byte[] certificateData)
        {
            var certificateOffset = 0;
            ReadDerElement(certificateData, ref certificateOffset, 0x30, out var certificateContent, out _);

            var tbsOffset = certificateContent;
            ReadDerElement(certificateData, ref tbsOffset, 0x30, out var tbsContent, out _);

            var fieldOffset = tbsContent;
            if (fieldOffset < certificateData.Length && certificateData[fieldOffset] == 0xA0)
            {
                ReadDerElement(certificateData, ref fieldOffset, 0xA0, out _, out _);
            }

            ReadDerElement(certificateData, ref fieldOffset, 0x02, out _, out _); // serialNumber
            ReadDerElement(certificateData, ref fieldOffset, 0x30, out _, out _); // signature
            ReadDerElement(certificateData, ref fieldOffset, 0x30, out _, out _); // issuer
            ReadDerElement(certificateData, ref fieldOffset, 0x30, out _, out _); // validity
            ReadDerElement(certificateData, ref fieldOffset, 0x30, out _, out _); // subject

            var spkiOffset = fieldOffset;
            ReadDerElement(certificateData, ref fieldOffset, 0x30, out _, out _);
            var spki = new byte[fieldOffset - spkiOffset];
            Buffer.BlockCopy(certificateData, spkiOffset, spki, 0, spki.Length);
            return spki;
        }

        private static void ReadDerElement(byte[] data, ref int offset, byte expectedTag, out int contentOffset, out int contentLength)
        {
            if (data == null || offset < 0 || offset >= data.Length || data[offset++] != expectedTag || offset >= data.Length)
            {
                throw new CryptographicException("Invalid DER certificate structure.");
            }

            var lengthByte = data[offset++];
            if ((lengthByte & 0x80) == 0)
            {
                contentLength = lengthByte;
            }
            else
            {
                var lengthBytes = lengthByte & 0x7F;
                if (lengthBytes == 0 || lengthBytes > 4 || offset > data.Length - lengthBytes)
                {
                    throw new CryptographicException("Invalid DER certificate length.");
                }

                contentLength = 0;
                for (var index = 0; index < lengthBytes; index++)
                {
                    contentLength = checked((contentLength << 8) | data[offset++]);
                }
            }

            contentOffset = offset;
            if (contentLength < 0 || contentLength > data.Length - contentOffset)
            {
                throw new CryptographicException("Truncated DER certificate.");
            }

            offset = contentOffset + contentLength;
        }
    }

    [Serializable]
    internal sealed class PairingResponse
    {
        public string accessToken;
        public string pin;
    }

    [Serializable]
    internal sealed class CodecResult
    {
        public string name;
        public int encodedBytes;
        public double roundTripMilliseconds;
    }

    [Serializable]
    internal sealed class P06QuestReport
    {
        public string platform;
        public string unity;
        public string scriptingBackend;
        public string pin;
        public CodecResult[] codecs;
        public double wssRoundTripMilliseconds;
        public int httpsChunkBytes;
        public LoadResult load;
        public string result;
    }

    [Serializable]
    internal sealed class AssetManifest
    {
        public long length;
        public string sha256;
        public int chunkLength;
    }

    [Serializable]
    internal sealed class LoadResult
    {
        public ControlLoadResult control;
        public BulkLoadResult bulk;
    }

    [Serializable]
    internal sealed class ControlLoadResult
    {
        public int sent;
        public int received;
        public int failures;
        public double p50Milliseconds;
        public double p95Milliseconds;
        public double maxMilliseconds;
    }

    [Serializable]
    internal sealed class BulkLoadResult
    {
        public long bytes;
        public double durationMilliseconds;
        public double usefulMegabitsPerSecond;
    }
}
