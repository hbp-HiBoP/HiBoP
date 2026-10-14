using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Preferences;
using HBP.Transfer.Transport;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public class PairingAutomationTests
    {
        private static QuestDevice Device(byte identity) => new() { Pin = Enumerable.Repeat(identity, 32).ToArray(), Name = "Quest", Host = "127.0.0.1" };

        [Test]
        public void OperatingSystemFailuresUseEnglishProductMessages()
        {
            var socket = new SocketException((int)SocketError.OperationAborted);
            Assert.That(TransferError.Describe(new IOException("Une erreur réseau est survenue", socket)), Is.EqualTo("The Quest transfer was interrupted. Check USB or Wi-Fi, then retry."));
            Assert.That(TransferError.Describe(new TimeoutException("Délai dépassé")), Does.StartWith("The Quest operation timed out."));
            Assert.That(TransferError.Describe(new InvalidOperationException("The Quest connection changed.")), Is.EqualTo("The Quest connection changed."));
        }

        [TestCase("read"), TestCase("aborted-write"), TestCase("failure")]
        public async Task CancelledTlsIoIsCancellationWhileUncancelledIoRemainsAFailure(string mode)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            using var receiver = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var watchdog = stop.Token.Register(() => entered.TrySetCanceled());
            var serving = receiver.ServeAsync(listener, stop.Token, async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException();
            }, _ => { });
            byte[] credential = null;
            try
            {
                credential = await QuestPairing.PairAsync("127.0.0.1", receiver.Pin, receiver.Code, stop.Token);
                var sending = QuestPairing.SendAsync("127.0.0.1", receiver.Pin, credential, attempt.Token, async (stream, token) =>
                {
                    entered.TrySetResult(true);
                    if (mode == "read") await stream.ReadAsync(new byte[1], 0, 1, token);
                    if (mode == "aborted-write") attempt.Cancel();
                    throw new IOException("Une erreur réseau est survenue", new SocketException((int)SocketError.OperationAborted));
                });
                await entered.Task;
                if (mode == "read") attempt.Cancel();
                Exception failure = null;
                try
                {
                    await sending;
                }
                catch (Exception error)
                {
                    failure = error;
                }

                Assert.That(failure, mode == "failure" ? Is.InstanceOf<IOException>() : Is.InstanceOf<OperationCanceledException>());
                if (failure is OperationCanceledException cancelled) Assert.That(cancelled.CancellationToken, Is.EqualTo(attempt.Token));
            }
            finally
            {
                if (credential != null) Array.Clear(credential, 0, credential.Length);
                stop.Cancel();
                await serving;
            }
        }

        [Test]
        public void PreferencesDefaultAndClonePreserveExplicitChoice()
        {
            Assert.That(JsonConvert.DeserializeObject<QuestPreferences>("{\"AutomaticallySynchronizeScenes\":false}").AutomaticallyConnect, Is.True);
            var preferences = JsonConvert.DeserializeObject<QuestPreferences>("{\"AutomaticallyConnect\":false}");
            Assert.That(((QuestPreferences)preferences.Clone()).AutomaticallyConnect, Is.False);
            Assert.That(JsonConvert.DeserializeObject<QuestPreferences>(JsonConvert.SerializeObject(preferences)).AutomaticallyConnect, Is.False);
        }

        [Test]
        public void LatestAvailableRememberedConnectionWinsAndExclusionsAreRespected()
        {
            var first = Device(1);
            var second = Device(2);
            var unknown = Device(3);
            var history = new Dictionary<string, long> { [first.Id] = 10, [second.Id] = 20 };
            var excluded = new HashSet<string>();
            Assert.That(PairingAssociations.Choose(new[] { first, second, unknown }, history, excluded), Is.SameAs(second));
            Assert.That(PairingAssociations.Choose(new[] { first, unknown }, history, excluded), Is.SameAs(first));
            history[first.Id] = 30;
            Assert.That(PairingAssociations.Choose(new[] { first, second }, history, excluded), Is.SameAs(first));
            excluded.Add(first.Id);
            Assert.That(PairingAssociations.Choose(new[] { first, second }, history, excluded), Is.SameAs(second));
            Assert.That(PairingAssociations.Choose(new[] { unknown }, history, excluded), Is.Null);
        }

        [Test]
        public void UndatedAssociationsUseStableOrderingAndDoNotBlockConnection()
        {
            var devices = new[] { Device(2), Device(1) };
            var history = devices.ToDictionary(device => device.Id, _ => 0L);
            var excluded = new HashSet<string>();
            Assert.That(PairingAssociations.Choose(devices, history, excluded).Id, Is.EqualTo(PairingAssociations.Choose(devices.Reverse(), history, excluded).Id));
        }

        [Test]
        public async Task ConnectionHistoryDoesNotChangeCredentialFormat()
        {
            string directory = Path.Combine(Path.GetTempPath(), "hibop-pairing-history-" + Guid.NewGuid().ToString("N"));
            var associations = new PairingAssociations(directory);
            var device = Device(1);
            try
            {
                await Task.Run(() => PairingStorage.Write(associations.CredentialPath(device.Pin), new byte[32]));
                Assert.That((await Task.Run(associations.ReadHistory))[device.Id], Is.Zero);
                await Task.Run(() => associations.RecordSuccess(device.Pin, 123));
                Assert.That((await Task.Run(associations.ReadHistory))[device.Id], Is.EqualTo(123));
                Assert.That(PairingStorage.Read(associations.CredentialPath(device.Pin)).Length, Is.EqualTo(32));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public async Task DiscoveryDoesNotDisplayCodeAndDesktopRequestDoesNotResetAttempts()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var receiver = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            var serving = receiver.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { });
            try
            {
                await QuestPairing.DescribeAsync("127.0.0.1", stop.Token);
                Assert.That(receiver.IsCodeRequested, Is.False);
                int attempts = receiver.GetStatus().AttemptsRemaining;
                Guid request = await QuestPairing.RequestCodeAsync("127.0.0.1", receiver.Pin, stop.Token);
                Assert.That(receiver.IsCodeRequested, Is.True);
                Assert.That(receiver.GetStatus().AttemptsRemaining, Is.EqualTo(attempts));
                await QuestPairing.RequestCodeAsync("127.0.0.1", receiver.Pin, stop.Token, request);
                Assert.That(receiver.IsCodeRequested, Is.False);
            }
            finally
            {
                stop.Cancel();
                await serving;
            }
        }

        [Test]
        public async Task IncompatibleActiveContextIsReportedBeforeSendingGlobals()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var receiver = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            string active = Guid.NewGuid().ToString("N");
            var serving = receiver.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { }, (_, __) => throw new InvalidOperationException("Globals must not be replaced"), activeGlobals: _ => Task.FromResult(active));
            byte[] credential = null;
            try
            {
                credential = await QuestPairing.AuthenticateAsync("127.0.0.1", receiver.Pin, receiver.Code, true, stop.Token);
                Exception failure = null;
                try
                {
                    await QuestPairing.ResumeAsync("127.0.0.1", receiver.Pin, credential, Guid.NewGuid().ToString("N"), stop.Token, (_, __) => throw new InvalidOperationException("Must not send globals"));
                }
                catch (Exception error)
                {
                    failure = error;
                }

                Assert.That(failure, Is.TypeOf<QuestContextException>());
                await QuestPairing.ResumeAsync("127.0.0.1", receiver.Pin, credential, active, stop.Token, (_, __) => throw new InvalidOperationException("Retained context must be reused"));
                Assert.That(receiver.IsPaired, Is.True);
            }
            finally
            {
                if (credential != null) Array.Clear(credential, 0, credential.Length);
                stop.Cancel();
                await serving;
            }
        }
    }
}
