using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class PairingGlobalsHandshakeTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task PairingWaitsForGlobalInstallationAndLocksAfterFailure(bool fail)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var pairing = new QuestPairing();
            string folder = Path.Combine(Path.GetTempPath(), "hibop-pair-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string source = Path.Combine(folder, "source"), target = Path.Combine(folder, "target");
            File.WriteAllText(source, "global snapshot");
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var install = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = stop.Token.Register(() =>
            {
                entered.TrySetCanceled();
                install.TrySetCanceled();
                finished.TrySetCanceled();
            });
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            Task serving = pairing.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException("Unexpected scene"), state =>
            {
                if (state.StartsWith("Paired.", StringComparison.Ordinal) || state.StartsWith("Connection interrupted.", StringComparison.Ordinal)) finished.TrySetResult(true);
            }, (stream, token) => PinnedTlsTransfer.ReceiveFileAsync(stream, target, token, async (_, __, ___) =>
            {
                entered.TrySetResult(true);
                await install.Task;
                if (fail) throw new InvalidDataException("Global setup refused");
                return DeliveryStatus.Published;
            }));
            Task<byte[]> client = null;
            try
            {
                byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
                // A legacy pairing must never bypass the global snapshot requirement.
                Exception legacy = null;
                try
                {
                    await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
                }
                catch (Exception exception)
                {
                    legacy = exception;
                }

                Assert.That(legacy, Is.TypeOf<AuthenticationException>());
                client = QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token, (stream, token) => PinnedTlsTransfer.SendFileAsync(stream, source, token));
                await entered.Task;
                Assert.That(pairing.IsPaired, Is.False);
                Assert.That(client.IsCompleted, Is.False);
                install.TrySetResult(true);
                Exception failure = null;
                byte[] credential = null;
                try
                {
                    credential = await client;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                await finished.Task;
                if (credential != null) Array.Clear(credential, 0, credential.Length);
                Assert.That(failure == null, Is.EqualTo(!fail));
                Assert.That(pairing.IsPaired, Is.EqualTo(!fail));
                if (fail) Assert.That(pairing.IsLocked, Is.True);
            }
            finally
            {
                stop.Cancel();
                if (client != null)
                {
                    try
                    {
                        await client;
                    }
                    catch (Exception)
                    {
                    }
                }

                await serving;
                Directory.Delete(folder, true);
            }
        }
    }
}
