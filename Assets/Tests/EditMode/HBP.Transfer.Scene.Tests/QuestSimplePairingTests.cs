using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class QuestSimplePairingTests
    {
        private CancellationTokenSource stop;
        private QuestPairing receiver;
        private Task serving;
        private string folder, statePath;
        private int installations;
        private TaskCompletionSource<bool> replicaEntered, releaseReplica;

        [SetUp]
        public void SetUp()
        {
            installations = 0;
            replicaEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            releaseReplica = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            folder = Path.Combine(Path.GetTempPath(), "hibop-quest030-" + Guid.NewGuid().ToString("N"));
            statePath = Path.Combine(folder, "identity.pair");
            Start();
        }

        private void Start()
        {
            receiver = new QuestPairing(statePath, "Test Quest");
            Listen();
        }

        private void Listen()
        {
            stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            serving = receiver.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException("Unexpected scene"), _ => { }, async (stream, token) =>
            {
                var marker = new byte[1];
                await PinnedTlsTransfer.ReadExactAsync(stream, marker, 0, 1, token);
                if (marker[0] != 42) throw new InvalidDataException("Interrupted global installation");
                Interlocked.Increment(ref installations);
                await stream.WriteAsync(new byte[] { 1 }, 0, 1, token);
                return null;
            }, async (_, token) =>
            {
                replicaEntered.TrySetResult(true);
                using var cancelled = token.Register(() => releaseReplica.TrySetCanceled());
                await releaseReplica.Task;
            });
        }

        private async Task CloseAsync()
        {
            stop.Cancel();
            await serving;
            receiver.Dispose();
            stop.Dispose();
        }

        [TearDown]
        public async Task TearDown()
        {
            await CloseAsync();
            Directory.Delete(folder, true);
        }

        private static async Task<DeliveryReceipt> Globals(Stream stream, CancellationToken token)
        {
            await stream.WriteAsync(new byte[] { 42 }, 0, 1, token);
            await PinnedTlsTransfer.ReadExactAsync(stream, new byte[1], 0, 1, token);
            return null;
        }

        private Task<byte[]> Pair(string code = null) => QuestPairing.AuthenticateAsync("127.0.0.1", receiver.Pin, code ?? receiver.Code, true, stop.Token);
        private Task Resume(byte[] secret, string id) => QuestPairing.ResumeAsync("127.0.0.1", receiver.Pin, secret, id, stop.Token, Globals);

        [Test]
        public async Task CorrectCodeThenRepeatedResume_PreservesGlobals()
        {
            byte[] credential = await Pair();
            string id = Guid.NewGuid().ToString("N");
            await Resume(credential, id);
            await Resume(credential, id);
            Assert.That(await QuestPairing.PingAsync("127.0.0.1", receiver.Pin, credential, stop.Token), Is.True);
            Assert.That(installations, Is.EqualTo(1));
            Assert.That(receiver.IsConnected, Is.True);
        }

        [Test]
        public async Task PersistentReplicaDoesNotBlockAuthenticatedHeartbeat()
        {
            byte[] credential = await Pair();
            await Resume(credential, Guid.NewGuid().ToString("N"));
            Task control = QuestPairing.OpenReplicaAsync("127.0.0.1", receiver.Pin, credential, stop.Token, (_, __) => releaseReplica.Task);
            await replicaEntered.Task;
            Assert.That(await QuestPairing.PingAsync("127.0.0.1", receiver.Pin, credential, stop.Token), Is.True);
            releaseReplica.TrySetResult(true);
            await control;
        }

        [Test]
        public async Task RestartListener_PreservesPairingAndGlobals()
        {
            byte[] credential = await Pair();
            string id = Guid.NewGuid().ToString("N");
            await Resume(credential, id);
            stop.Cancel();
            await serving;
            stop.Dispose();
            Listen();
            Assert.That(await QuestPairing.PingAsync("127.0.0.1", receiver.Pin, credential, stop.Token), Is.True);
            await Resume(credential, id);
            Assert.That(installations, Is.EqualTo(1));
        }

        [Test]
        public async Task RestartReceiver_PreservesIdentityAndCredential_ReinstallsGlobalsOnce()
        {
            byte[] credential = await Pair(), pin = receiver.Pin;
            string id = Guid.NewGuid().ToString("N");
            await Resume(credential, id);
            await CloseAsync();
            Start();
            Assert.That(receiver.Pin, Is.EqualTo(pin));
            Assert.That(await QuestPairing.PingAsync("127.0.0.1", pin, credential, stop.Token), Is.False);
            await Resume(credential, id);
            await Resume(credential, id);
            Assert.That(installations, Is.EqualTo(2));
        }

        [Test]
        public async Task WrongCodeThenCorrectCode_OnlyAuthenticatedClientInstallsGlobals()
        {
            string wrong = receiver.Code == "000000" ? "000001" : "000000";
            Assert.That(await Capture(() => Pair(wrong)), Is.Not.Null);
            Assert.That(receiver.IsPaired, Is.False);
            Assert.That(installations, Is.Zero);
            byte[] credential = await Pair();
            await Resume(credential, Guid.NewGuid().ToString("N"));
            Assert.That(installations, Is.EqualTo(1));
        }

        [Test]
        public async Task FiveBadCodes_LockAcrossConnections()
        {
            string wrong = receiver.Code == "000000" ? "000001" : "000000";
            for (int i = 0; i < 5; i++) Assert.That(await Capture(() => Pair(wrong)), Is.Not.Null);
            Assert.That(receiver.IsLocked, Is.True);
            Assert.That(await Capture(() => Pair()), Is.TypeOf<AuthenticationException>());
            Assert.That(installations, Is.Zero);
        }

        [Test]
        public async Task AbandonedPairingAttempts_CountTowardLimit()
        {
            for (int i = 0; i < 5; i++)
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, QuestPairing.Port);
                using var tls = new SslStream(client.GetStream(), false, (_, __, ___, ____) => true);
                await tls.AuthenticateAsClientAsync("HiBoP-Quest", null, SslProtocols.Tls12, false);
                await tls.WriteAsync(new byte[] { 11 }, 0, 1, stop.Token);
                var accepted = new byte[1];
                await PinnedTlsTransfer.ReadExactAsync(tls, accepted, 0, 1, stop.Token);
                Assert.That(accepted[0], Is.EqualTo(1));
            }

            Assert.That(await Capture(() => Pair()), Is.TypeOf<AuthenticationException>());
            Assert.That(receiver.IsLocked, Is.True);
        }

        [Test]
        public async Task MalformedIdentity_DoesNotStopReceiver()
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, QuestPairing.Port);
                using var tls = new SslStream(client.GetStream(), false, (_, __, ___, ____) => true);
                await tls.AuthenticateAsClientAsync("HiBoP-Quest", null, SslProtocols.Tls12, false);
                await tls.WriteAsync(new byte[] { 11 }, 0, 1, stop.Token);
                await PinnedTlsTransfer.ReadExactAsync(tls, new byte[1], 0, 1, stop.Token);
                byte[] malformed = { 5, 0, 0, 0, 255, 255, 255, 255, 255 };
                await tls.WriteAsync(malformed, 0, malformed.Length, stop.Token);
            }

            byte[] credential = await Pair();
            await Resume(credential, Guid.NewGuid().ToString("N"));
            Assert.That(installations, Is.EqualTo(1));
        }

        [Test]
        public async Task WrongCredentialAndWrongCertificate_CannotResume()
        {
            byte[] credential = await Pair();
            Assert.That(await Capture(() => Resume(new byte[32], Guid.NewGuid().ToString("N"))), Is.TypeOf<AuthenticationException>());
            Assert.That(await Capture(() => QuestPairing.PingAsync("127.0.0.1", new byte[32], credential, stop.Token)), Is.Not.Null);
            Assert.That(installations, Is.Zero);
        }

        [Test]
        public async Task InterruptedGlobals_RetriesWithSameCredential()
        {
            byte[] credential = await Pair();
            string id = Guid.NewGuid().ToString("N");
            Assert.That(await Capture(() => QuestPairing.ResumeAsync("127.0.0.1", receiver.Pin, credential, id, stop.Token, async (stream, token) =>
            {
                await stream.WriteAsync(new byte[] { 0 }, 0, 1, token);
                await PinnedTlsTransfer.ReadExactAsync(stream, new byte[1], 0, 1, token);
                return null;
            })), Is.Not.Null);
            await Resume(credential, id);
            Assert.That(installations, Is.EqualTo(1));
        }

        [Test]
        public async Task ConcurrentPairing_OnlyOneOwnerObtainsCredential()
        {
            Task<byte[]> first = Pair(), second = Pair();
            Exception a = await Capture(() => first), b = await Capture(() => second);
            Assert.That((a == null ? 1 : 0) + (b == null ? 1 : 0), Is.EqualTo(1));
        }

        [Test]
        public async Task LegacyRawCodeCommand_IsRefused()
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, QuestPairing.Port);
            using var tls = new SslStream(client.GetStream(), false, (_, __, ___, ____) => true);
            await tls.AuthenticateAsClientAsync("HiBoP-Quest", null, SslProtocols.Tls12, false);
            await tls.WriteAsync(new byte[] { 3 }, 0, 1, stop.Token);
            byte[] accepted = new byte[1];
            await PinnedTlsTransfer.ReadExactAsync(tls, accepted, 0, 1, stop.Token);
            Assert.That(accepted[0], Is.Zero);
        }

        [Test]
        public async Task DiscoveryMetadata_DoesNotPairOrExposeCode()
        {
            var device = await QuestPairing.DescribeAsync("127.0.0.1", stop.Token);
            Assert.That(device.Name, Is.EqualTo("Test Quest"));
            Assert.That(device.Pin, Is.EqualTo(receiver.Pin));
            Assert.That(receiver.IsPaired, Is.False);
            Assert.That(receiver.Announcement, Does.Not.Contain(receiver.Code));
            Assert.That(QuestDiscovery.Decode("HiBoP-Quest-v1|bad|spoof", "127.0.0.1"), Is.Null);
        }

        [Test]
        public async Task PakeBoundToDifferentTlsCertificates_IsRejected()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                using var client = new TcpClient();
                Task connect = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                using var server = await listener.AcceptTcpClientAsync();
                await connect;
                var otherPin = (byte[])receiver.Pin.Clone();
                otherPin[0] ^= 1;
                Task first = Task.Run(() => PairingPake.AuthenticateAsync(client.GetStream(), "123456", receiver.Pin, false, stop.Token));
                Task second = Task.Run(() => PairingPake.AuthenticateAsync(server.GetStream(), "123456", otherPin, true, stop.Token));
                Assert.That(await Capture(() => first), Is.TypeOf<AuthenticationException>());
                Assert.That(await Capture(() => second), Is.TypeOf<AuthenticationException>());
            }
            finally
            {
                listener.Stop();
            }
        }

        [Test]
        public void CredentialFile_RoundTripsAndReplacesAtomically()
        {
            string path = Path.Combine(folder, "desktop.pair");
            byte[] credential = new byte[32];
            credential[0] = 42;
            PairingStorage.Write(path, credential);
            Assert.That(PairingStorage.Read(path), Is.EqualTo(credential));
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) Assert.That(File.ReadAllBytes(path), Is.Not.EqualTo(credential));
            credential[0] = 43;
            PairingStorage.Write(path, credential);
            Assert.That(PairingStorage.Read(path), Is.EqualTo(credential));
        }

        private static async Task<Exception> Capture(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
