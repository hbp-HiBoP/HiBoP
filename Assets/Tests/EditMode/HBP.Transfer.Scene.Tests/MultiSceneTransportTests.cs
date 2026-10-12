using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class MultiSceneTransportTests
    {
        [Test]
        public async Task RoutePreservesAllThreeIdentitiesBeforeUnchangedProtocolMagic()
        {
            var session = new SessionId(Guid.NewGuid());
            var scene = new SceneId(Guid.NewGuid());
            var incarnation = new IncarnationId(Guid.NewGuid());
            using var stream = new MemoryStream();
            await V2ReplicaRoute.WriteAsync(stream, session, scene, incarnation, CancellationToken.None);
            byte[] inner = System.Text.Encoding.ASCII.GetBytes("HBT2");
            await stream.WriteAsync(inner, 0, inner.Length);
            stream.Position = 4;
            var routed = await V2ReplicaRoute.ReadBodyAsync(stream, CancellationToken.None);
            Assert.That(routed.Session, Is.EqualTo(session));
            Assert.That(routed.Scene, Is.EqualTo(scene));
            Assert.That(routed.Incarnation, Is.EqualTo(incarnation));
            Assert.That(stream.ReadByte(), Is.EqualTo((byte)'H'));
        }

        [Test]
        public void PatientActionRetainsCapturedContextAndOperationAndRejectsTrailingBytes()
        {
            var original = new V2SceneAction(new SceneId(Guid.NewGuid()), new IncarnationId(Guid.NewGuid()), V2SceneActionKind.OpenPatient, "column-A", "patient-site");
            Assert.That(V2SceneAction.TryDecode(original.Encode(), out var decoded), Is.True);
            Assert.That(decoded.Scene, Is.EqualTo(original.Scene));
            Assert.That(decoded.Incarnation, Is.EqualTo(original.Incarnation));
            Assert.That(decoded.Operation, Is.EqualTo(original.Operation));
            Assert.That(decoded.Column, Is.EqualTo("column-A"));
            Assert.That(decoded.Site, Is.EqualTo("patient-site"));
            Assert.Throws<InvalidDataException>(() => V2SceneAction.TryDecode(original.Encode().Concat(new byte[] { 1 }).ToArray(), out _));
        }

        [Test]
        public async Task HeartbeatAndControlsRemainAvailableWithSixPersistentScenes()
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var pairing = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var watchdog = stop.Token.Register(() => ready.TrySetCanceled());
            int count = 0;
            Task serving = pairing.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { }, receiveReplica: async (_, token) =>
            {
                if (Interlocked.Increment(ref count) == 6) ready.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, token);
            }, receiveSessionControl: (request, _) => Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied)));
            Task[] replicas = Array.Empty<Task>();
            try
            {
                byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
                byte[] credential = await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
                replicas = Enumerable.Range(0, 6).Select(_ => QuestPairing.OpenReplicaAsync("127.0.0.1", pin, credential, stop.Token, async (stream, token) => await Task.Delay(Timeout.Infinite, token))).ToArray();
                await ready.Task;
                Assert.That(await QuestPairing.PingAsync("127.0.0.1", pin, credential, stop.Token), Is.True);
                var request = new SessionControlRequest(Guid.NewGuid(), 1, Guid.NewGuid(), SessionControlKind.SceneInventory);
                Assert.That((await QuestPairing.SendSessionControlAsync("127.0.0.1", pin, credential, request, stop.Token)).Applied, Is.True);
            }
            finally
            {
                stop.Cancel();
                foreach (var replica in replicas)
                {
                    try
                    {
                        await replica;
                    }
                    catch (Exception)
                    {
                    }
                }

                await serving;
            }
        }
    }
}
