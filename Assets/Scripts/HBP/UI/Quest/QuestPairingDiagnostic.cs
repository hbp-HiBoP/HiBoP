#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Transport;
using UnityEngine;

namespace HBP.UI.Quest
{
    /// <summary>Explicit command-line smoke check in the real Windows IL2CPP player. No user pairing is read or written.</summary>
    public static class QuestPairingDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-questPairingSmoke");
            if (index >= 0 && index + 1 < args.Length) RunAsync(Path.GetFullPath(args[index + 1])).Forget(Debug.LogException);
            index = Array.IndexOf(args, "-questUsbSmoke");
            if (index >= 0 && index + 1 < args.Length) RunUsbAsync(Path.GetFullPath(args[index + 1])).Forget(Debug.LogException);
        }

        private static async UniTask RunUsbAsync(string directory)
        {
            Directory.CreateDirectory(directory);
            var result = new Result { utc = DateTime.UtcNow.ToString("O"), platform = Application.platform.ToString() };
            string adb = Path.Combine(Application.streamingAssetsPath, "QuestUsb", "adb.exe");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            int exit = 0;
            try
            {
                Check((await QuestAdbProcess.RunAsync(adb, "version", timeout.Token)).Contains("Android Debug Bridge"), "Bundled ADB native launch/output");
                result.checks++;
                bool rejected = false;
                try
                {
                    await QuestAdbProcess.RunAsync(adb, "hibop-invalid-command", timeout.Token);
                }
                catch (IOException)
                {
                    rejected = true;
                }

                Check(rejected, "Nonzero client exit");
                result.checks++;
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                rejected = false;
                try
                {
                    await QuestAdbProcess.RunAsync(adb, "version", cancelled.Token);
                }
                catch (OperationCanceledException)
                {
                    rejected = true;
                }

                Check(rejected, "Cancelled client launch");
                result.checks++;
                var discovery = new QuestUsbDiscovery(adb);
                var found = await discovery.FindAsync(timeout.Token);
                Check(found.Count > 0, "Physical Quest USB discovery with TLS metadata");
                result.checks++;
                var repeated = await discovery.FindAsync(timeout.Token);
                Check(repeated.Exists(x => x.Id == found[0].Id && x.Host == found[0].Host), "Stable identity and USB forward on repeat scan");
                result.checks++;
                result.passed = true;
            }
            catch (Exception exception)
            {
                result.error = exception.ToString();
                exit = 1;
            }
            finally
            {
                File.WriteAllText(Path.Combine(directory, "result.json"), JsonUtility.ToJson(result, true));
                Application.Quit(exit);
            }
        }

        private static async UniTask RunAsync(string directory)
        {
            Directory.CreateDirectory(directory);
            string identityPath = Path.Combine(directory, "smoke-identity.pair");
            QuestPairing receiver = null;
            CancellationTokenSource serverStop = null;
            Task serving = Task.CompletedTask;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            int installations = 0, exit = 0;
            var result = new Result { utc = DateTime.UtcNow.ToString("O"), platform = Application.platform.ToString() };

            async Task<DeliveryReceipt> Receive(Stream stream, CancellationToken token)
            {
                await PinnedTlsTransfer.ReadExactAsync(stream, new byte[1], 0, 1, token);
                Interlocked.Increment(ref installations);
                await stream.WriteAsync(new byte[] { 1 }, 0, 1, token);
                return null;
            }

            async Task<DeliveryReceipt> Send(Stream stream, CancellationToken token)
            {
                await stream.WriteAsync(new byte[] { 1 }, 0, 1, token);
                await PinnedTlsTransfer.ReadExactAsync(stream, new byte[1], 0, 1, token);
                return null;
            }

            async Task Start()
            {
                receiver = await Task.Run(() => new QuestPairing(identityPath, "IL2CPP smoke"));
                serverStop = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
                listener.Start();
                serving = receiver.ServeAsync(listener, serverStop.Token, (_, __) => throw new InvalidOperationException(), _ => { }, Receive);
            }

            async Task Close()
            {
                serverStop?.Cancel();
                await serving;
                receiver?.Dispose();
                receiver = null;
                serverStop?.Dispose();
                serverStop = null;
            }

            try
            {
                await Start();
                var info = await QuestPairing.DescribeAsync("127.0.0.1", timeout.Token);
                Check(TransportIdentity.Equal(info.Pin, receiver.Pin), "Receiver discovery");
                bool rejected = false;
                try
                {
                    await QuestPairing.AuthenticateAsync("127.0.0.1", receiver.Pin, receiver.Code == "000000" ? "000001" : "000000", true, timeout.Token);
                }
                catch (System.Security.Authentication.AuthenticationException)
                {
                    rejected = true;
                }

                Check(rejected, "Wrong code rejection");
                byte[] credential = await QuestPairing.AuthenticateAsync("127.0.0.1", receiver.Pin, receiver.Code, true, timeout.Token);
                byte[] pin = receiver.Pin;
                string context = Guid.NewGuid().ToString("N");
                await QuestPairing.ResumeAsync("127.0.0.1", pin, credential, context, timeout.Token, Send);
                await QuestPairing.ResumeAsync("127.0.0.1", pin, credential, context, timeout.Token, Send);
                Check(await QuestPairing.PingAsync("127.0.0.1", pin, credential, timeout.Token), "Heartbeat");
                Check(installations == 1, "Globals kept on reconnect");
                await Close();
                await Start();
                Check(TransportIdentity.Equal(pin, receiver.Pin), "Persistent certificate");
                Check(!await QuestPairing.PingAsync("127.0.0.1", pin, credential, timeout.Token), "Restart requires globals");
                await QuestPairing.ResumeAsync("127.0.0.1", pin, credential, context, timeout.Token, Send);
                Check(installations == 2, "Remembered credential restores after restart");
                Array.Clear(credential, 0, credential.Length);
                result.passed = true;
                result.checks = 8;
            }
            catch (Exception exception)
            {
                result.error = exception.ToString();
                exit = 1;
            }
            finally
            {
                await Close();
                if (File.Exists(identityPath)) File.Delete(identityPath);
                File.WriteAllText(Path.Combine(directory, "result.json"), JsonUtility.ToJson(result, true));
                Application.Quit(exit);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable]
        private sealed class Result
        {
            public string utc, platform, error;
            public bool passed;
            public int checks;
        }
    }
}
#endif
