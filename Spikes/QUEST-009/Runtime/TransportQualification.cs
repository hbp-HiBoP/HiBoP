using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HBP.Transfer.Transport.Probe
{
    public static class TransportQualification
    {
        [Serializable]
        public sealed class Config
        {
            public string runId;
            public string mode;
            public string host;
            public int port = 5449;
            public string pin;
            public string secret;
            public string payloadPath;
            public string expectedHash;
            public string outputPath;
            public string readyPath;
            public string stopPath;
        }

        [Serializable]
        public sealed class Result
        {
            public string scenario;
            public bool passed;
            public string detail;
            public double milliseconds;
            public int bytes;
            public long gcAllocatedFrameBytes;
            public long managedRetainedDeltaBytes;
        }

        public static long AllocatedFrameBytes;

        public static async Task RunAsync(CancellationToken cancellation)
        {
            var arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-probeConfig");
            string path = index >= 0 ? arguments[index + 1] : Path.Combine(Application.persistentDataPath, "quest009-config.json");
            if (File.Exists(path))
            {
                var config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
                File.Delete(path); // One-time trusted test bootstrap, never a saved pairing.
                Debug.Log("QUEST009_RUN " + config.runId);
                if (config.mode == "client")
                {
                    await ClientAsync(config, cancellation);
                    return;
                }

                if (config.mode != "server") throw new InvalidDataException("Unknown probe mode.");
                using var identity = TransportIdentity.Create();
                byte[] secret = Convert.FromBase64String(config.secret);
                config.secret = null;
                byte[] payload = File.ReadAllBytes(config.payloadPath);
                var listener = new TcpListener(IPAddress.Parse(config.host), config.port);
                listener.Start(1);
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                Task server = PinnedTlsTransfer.ServeAsync(listener, identity, secret, payload, stop.Token, Record);
                try
                {
                    File.WriteAllText(config.readyPath, JsonUtility.ToJson(new Config { pin = Convert.ToBase64String(TransportIdentity.Hash(identity.RawData)), port = config.port }));
                    while (!File.Exists(config.stopPath) && !server.IsCompleted) await Task.Delay(100, cancellation);
                }
                finally
                {
                    var watch = Stopwatch.StartNew();
                    stop.Cancel();
                    await server;
                    Array.Clear(secret, 0, secret.Length);
                    Report("server-stop", watch.Elapsed.TotalSeconds < 5, "owned server task awaited", watch.Elapsed.TotalMilliseconds);
                }

                return;
            }

            using var testIdentity = TransportIdentity.Create();
            byte[] testSecret = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(testSecret);
            byte[] testPayload = new byte[3 * 1024 * 1024 + 17];
            for (int i = 0; i < testPayload.Length; i++) testPayload[i] = (byte)(i * 31);
            for (int variant = 0; variant < 2; variant++)
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start(1);
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                Task server = PinnedTlsTransfer.ServeAsync(listener, testIdentity, testSecret, testPayload, stop.Token, Record, variant == 1);
                var config = new Config { host = "127.0.0.1", port = port, pin = Convert.ToBase64String(TransportIdentity.Hash(testIdentity.RawData)), secret = Convert.ToBase64String(testSecret), expectedHash = Hex(TransportIdentity.Hash(testPayload)) };
                try
                {
                    if (variant == 0) await ClientAsync(config, cancellation);
                    else await ExpectFailureAsync("corrupt-chunk", () => PinnedTlsTransfer.ReceiveAsync(config.host, port, Convert.FromBase64String(config.pin), testSecret, cancellation), exception => exception is InvalidDataException && exception.Message == "Chunk hash mismatch.");
                }
                finally
                {
                    var watch = Stopwatch.StartNew();
                    stop.Cancel();
                    await server;
                    Report("stop-listener", watch.Elapsed.TotalSeconds < 5, "owned server task awaited", watch.Elapsed.TotalMilliseconds);
                    var rebound = new TcpListener(IPAddress.Loopback, port);
                    rebound.Start(1);
                    rebound.Stop();
                    Report("rebind-port", true, "same port immediately reusable", 0);
                }
            }

            var stalledListener = new TcpListener(IPAddress.Loopback, 0);
            stalledListener.Start(1);
            int stalledPort = ((IPEndPoint)stalledListener.LocalEndpoint).Port;
            using (var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            using (var stalledClient = new TcpClient())
            {
                var accepted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task server = PinnedTlsTransfer.ServeAsync(stalledListener, testIdentity, testSecret, testPayload, stop.Token, record =>
                {
                    Record(record);
                    if (record == "accepted") accepted.TrySetResult(true);
                });
                try
                {
                    await stalledClient.ConnectAsync(IPAddress.Loopback, stalledPort);
                    await accepted.Task;
                }
                finally
                {
                    var watch = Stopwatch.StartNew();
                    stop.Cancel();
                    await server;
                    Report("stop-during-handshake", watch.Elapsed.TotalSeconds < 5, "accepted socket closed and task awaited", watch.Elapsed.TotalMilliseconds);
                    var rebound = new TcpListener(IPAddress.Loopback, stalledPort);
                    rebound.Start(1);
                    rebound.Stop();
                }
            }

            Array.Clear(testSecret, 0, testSecret.Length);
            foreach (string name in new[] { "truncated", "unknown-version", "oversized" })
            {
                var header = new byte[name == "truncated" ? 12 : 40];
                header[0] = (byte)'H';
                header[1] = (byte)'B';
                header[2] = (byte)'T';
                header[3] = (byte)(name == "unknown-version" ? 2 : 1);
                header[7] = 0x7f;
                using var stream = new MemoryStream(header);
                await ExpectFailureAsync(name, () => PinnedTlsTransfer.ReceivePayloadAsync(stream, cancellation), exception => name == "truncated" ? exception is EndOfStreamException : exception is InvalidDataException && exception.Message == (name == "unknown-version" ? "Unknown transport version." : "Payload size out of bounds."));
            }
        }

        private static async Task ClientAsync(Config config, CancellationToken cancellation)
        {
            byte[] pin = Convert.FromBase64String(config.pin);
            byte[] secret = Convert.FromBase64String(config.secret);
            config.secret = null;
            try
            {
                await ReceiveAndMeasureAsync("nominal", config, pin, secret, cancellation);
                byte[] wrongPin = (byte[])pin.Clone();
                wrongPin[0] ^= 1;
                await ExpectFailureAsync("wrong-identity", () => PinnedTlsTransfer.ReceiveAsync(config.host, config.port, wrongPin, secret, cancellation), exception => exception is AuthenticationException && exception.Message == "Certificate pin rejected.");
                byte[] wrongSecret = (byte[])secret.Clone();
                wrongSecret[0] ^= 1;
                await ExpectFailureAsync("wrong-pairing", () => PinnedTlsTransfer.ReceiveAsync(config.host, config.port, pin, wrongSecret, cancellation), exception => exception is AuthenticationException && exception.Message == "Pairing secret rejected.");
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    using var interrupt = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                    bool reachedPayload = false;
                    await ExpectFailureAsync("interrupt-" + cycle, () => PinnedTlsTransfer.ReceiveAsync(config.host, config.port, pin, secret, interrupt.Token, bytes =>
                    {
                        if (bytes >= 2 * PinnedTlsTransfer.ChunkBytes)
                        {
                            reachedPayload = true;
                            interrupt.Cancel();
                        }
                    }), exception => reachedPayload && interrupt.IsCancellationRequested && (exception is OperationCanceledException || exception is IOException || exception is ObjectDisposedException));
                    await ReceiveAndMeasureAsync("reconnect-" + cycle, config, pin, secret, cancellation);
                }
            }
            finally
            {
                Array.Clear(secret, 0, secret.Length);
            }
        }

        private static async Task ReceiveAndMeasureAsync(string name, Config config, byte[] pin, byte[] secret, CancellationToken cancellation)
        {
            long allocatedBefore = AllocatedFrameBytes;
            long retainedBefore = GC.GetTotalMemory(false);
            var watch = Stopwatch.StartNew();
            byte[] payload = await PinnedTlsTransfer.ReceiveAsync(config.host, config.port, pin, secret, cancellation);
            string hash = Hex(TransportIdentity.Hash(payload));
            if (!string.Equals(hash, config.expectedHash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Fixture hash mismatch.");
            if (!string.IsNullOrEmpty(config.outputPath)) File.WriteAllBytes(config.outputPath, payload);
            Report(name, true, "sha256=" + hash, watch.Elapsed.TotalMilliseconds, payload.Length, AllocatedFrameBytes - allocatedBefore, GC.GetTotalMemory(false) - retainedBefore);
        }

        private static async Task ExpectFailureAsync(string name, Func<Task<byte[]>> action, Func<Exception, bool> expected)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                await action();
            }
            catch (Exception exception) when (expected(exception))
            {
                Report(name, true, exception.GetType().Name + ": " + exception.Message, watch.Elapsed.TotalMilliseconds);
                return;
            }

            throw new InvalidOperationException("Expected rejection was not observed: " + name);
        }

        private static void Report(string name, bool passed, string detail, double milliseconds, int bytes = 0, long allocated = 0, long retained = 0)
        {
            Debug.Log("QUEST009_RESULT " + JsonUtility.ToJson(new Result { scenario = name, passed = passed, detail = detail, milliseconds = milliseconds, bytes = bytes, gcAllocatedFrameBytes = allocated, managedRetainedDeltaBytes = retained }));
            if (!passed) throw new InvalidOperationException("Probe failed: " + name);
        }

        private static void Record(string record) => Debug.Log("QUEST009_SERVER " + record);
        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
