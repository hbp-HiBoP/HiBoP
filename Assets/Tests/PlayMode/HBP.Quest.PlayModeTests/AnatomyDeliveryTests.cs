#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HBP.Quest;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Anatomy.Delivery;
using HBP.Transfer.Transport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class AnatomyDeliveryTests
    {
        private GameObject root;
        private QuestAnatomyView view;
        private QuestAnatomySession session;
        private X509Certificate2 identity;
        private readonly byte[] secret = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

        [OneTimeSetUp]
        public void CreateIdentity() => identity = TransportIdentity.Create();

        [OneTimeTearDown]
        public void ReleaseIdentity() => identity?.Dispose();

        [SetUp]
        public void SetUp()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"));
            view = root.GetComponent<QuestAnatomyView>();
            session = root.GetComponent<QuestAnatomySession>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        private static AnatomySnapshot Snapshot(string id, float alpha = 1) => AnatomySnapshot.Create(id, "session", "visualization", "column", 1, new AnatomyCoordinateSpace(AnatomyMeshUploader.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, true, new[] { 0.2f, 0.4f, 0.7f, alpha }, new float[] { 0, 0, 0, 100, 0, 0, 0, 100, 0 }, new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1 }, new uint[] { 0, 1, 2 }, Array.Empty<float>());

        [Test]
        public async Task Offer_RealTlsPublishesAndAcknowledgesOnlyOnce()
        {
            var offer = new AnatomyDelivery(Snapshot("offer"));
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var receipts = new System.Collections.Concurrent.ConcurrentQueue<DeliveryReceipt>();
            Task serving = offer.ServeAsync(listener, identity, secret, stop.Token, _ => { }, receipts.Enqueue);
            try
            {
                root.transform.position = new Vector3(1, 2, 3);
                DeliveryReceipt first = await session.ReceiveAsync("127.0.0.1", port, TransportIdentity.Hash(identity.RawData), secret);
                Mesh mesh = view.SharedMesh;
                DeliveryReceipt second = await session.ReceiveAsync("127.0.0.1", port, TransportIdentity.Hash(identity.RawData), secret);
                Assert.That(first.Status, Is.EqualTo(DeliveryStatus.Published));
                Assert.That(second.Status, Is.EqualTo(DeliveryStatus.AlreadyPublished));
                Assert.That(second.ContentHash, Is.EqualTo(offer.ContentHash));
                Assert.That(session.TransferId, Is.EqualTo(offer.TransferId));
                Assert.That(session.SessionId, Is.EqualTo(offer.SessionId));
                Assert.That(view.SharedMesh, Is.SameAs(mesh));
                Assert.That(view.UploadCount, Is.EqualTo(1));
                Assert.That(session.IsReady, Is.True);
                Assert.That(session.IsConnected, Is.False);
                Assert.That(root.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
                while (receipts.Count < 2)
                {
                    stop.Token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                Assert.That(receipts.Select(r => r.Status), Is.EqualTo(new[] { DeliveryStatus.Published, DeliveryStatus.AlreadyPublished }));
            }
            finally
            {
                stop.Cancel();
                await serving;
            }
        }

        [Test]
        public async Task LostAck_RetryReturnsExistingPublication_ThenOldRetryCannotReplaceNewContent()
        {
            byte[] first = AnatomySnapshotCodec.Encode(Snapshot("first"));
            var result = await Transfer(first, "lost-ack");
            Assert.That(result.ServerError, Is.TypeOf<EndOfStreamException>());
            Assert.That(session.IsReady, Is.True);
            Mesh firstMesh = view.SharedMesh;
            Assert.That((await Transfer(first)).Receipt.Status, Is.EqualTo(DeliveryStatus.AlreadyPublished));
            Assert.That(view.SharedMesh, Is.SameAs(firstMesh));
            Assert.That(view.UploadCount, Is.EqualTo(1));
            await Transfer(AnatomySnapshotCodec.Encode(Snapshot("second")));
            Mesh secondMesh = view.SharedMesh;
            await NextFrame();
            Assert.That(firstMesh == null, Is.True);
            Assert.That((await Transfer(first)).Receipt.Status, Is.EqualTo(DeliveryStatus.Superseded));
            Assert.That(view.SharedMesh, Is.SameAs(secondMesh));
            Assert.That(view.UploadCount, Is.EqualTo(2));
            session.enabled = false;
            await NextFrame();
            Assert.That(view.SharedMesh, Is.SameAs(secondMesh));
            Assert.That(session.IsReady, Is.True);
            session.enabled = true;
            session.CloseSession();
            await NextFrame();
            Assert.That(secondMesh == null, Is.True);
            Assert.That(session.IsReady, Is.False);
            Assert.That((await Transfer(AnatomySnapshotCodec.Encode(Snapshot("second")))).Receipt.Status, Is.EqualTo(DeliveryStatus.Closed));
            Assert.That(view.SharedMesh, Is.Null);
        }

        [TestCase("chunk")]
        [TestCase("truncated")]
        [TestCase("transport-version")]
        [TestCase("schema-version")]
        [TestCase("incomplete-hbna")]
        [TestCase("prepare")]
        [TestCase("identity-conflict")]
        public async Task InvalidReplacement_PreservesPublishedMeshAndPresentation(string failure)
        {
            await Transfer(AnatomySnapshotCodec.Encode(Snapshot("previous")));
            Mesh previous = view.SharedMesh;
            int meshes = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "Quest Anatomy");
            root.transform.localScale = Vector3.one * 2;
            byte[] payload = AnatomySnapshotCodec.Encode(Snapshot(failure == "identity-conflict" ? "previous" : "new", failure == "prepare" ? 0.5f : 1));
            if (failure == "schema-version") payload[4] = 99;
            if (failure == "identity-conflict") payload = AnatomySnapshotCodec.Encode(AnatomySnapshot.Create("previous", "changed-session", "v", "c", 1, Snapshot("x").Coordinates, AnatomyWinding.Clockwise, true, new float[] { 1, 0, 0, 1 }, Snapshot("x").Positions.ToArray(), Snapshot("x").Normals.ToArray(), new uint[] { 0, 1, 2 }, Array.Empty<float>()));
            if (failure == "incomplete-hbna") Array.Resize(ref payload, payload.Length - 10);
            var failed = await Transfer(payload, failure);
            Assert.That(failed.ClientError, Is.Not.Null);
            Assert.That(failed.ServerError, Is.Not.Null, "Invalid deliveries must not acknowledge publication.");
            Assert.That(view.SharedMesh, Is.SameAs(previous));
            Assert.That(view.UploadCount, Is.EqualTo(1));
            Assert.That(session.TransferId, Is.EqualTo("previous"));
            Assert.That(session.IsReady, Is.True);
            Assert.That(session.IsConnected, Is.False);
            Assert.That(session.LastError, Is.Not.Empty);
            Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one * 2));
            await NextFrame();
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "Quest Anatomy"), Is.EqualTo(meshes));
            await Transfer(AnatomySnapshotCodec.Encode(Snapshot("recovery")));
            Assert.That(session.TransferId, Is.EqualTo("recovery"));
            Assert.That(view.UploadCount, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CancellationDuringReception_DoesNotPublishLate(bool close)
        {
            await Transfer(AnatomySnapshotCodec.Encode(Snapshot("previous")));
            var offered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<Outcome> receiving = Transfer(AnatomySnapshotCodec.Encode(Snapshot("late")), "pause", offered, resume);
            await offered.Task;
            Exception concurrent = await Capture(() => session.ReceiveAsync("127.0.0.1", 1, new byte[32], secret));
            Assert.That(concurrent, Is.TypeOf<InvalidOperationException>());
            if (close) session.CloseSession();
            else session.Disconnect();
            resume.SetResult(true);
            var result = await receiving;
            Assert.That(result.ClientError, Is.Not.Null);
            await NextFrame();
            Assert.That(view.UploadCount, Is.EqualTo(1));
            Assert.That(session.IsReady, Is.EqualTo(!close));
            Assert.That(session.TransferId, Is.EqualTo(close ? null : "previous"));
        }

        [Test]
        public async Task RealMniCaptureFixture_TravelsThroughTlsToRenderer()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-questAnatomyFixture");
            if (index < 0) Assert.Ignore("Pass -questAnatomyFixture with the captured MNI HBNA.");
            byte[] bytes = File.ReadAllBytes(args[index + 1]);
            Assert.That(new DeliveryReceipt(TransportIdentity.Hash(bytes), DeliveryStatus.Published).ContentHash, Is.EqualTo("065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12"));
            var result = await Transfer(bytes);
            Assert.That(result.ClientError, Is.Null);
            Assert.That(result.ServerError, Is.Null);
            Assert.That(view.SharedMesh.vertexCount, Is.EqualTo(69104));
            Assert.That(view.BufferBytes, Is.EqualTo(3869920));
            Assert.That(session.ContentHash, Is.EqualTo(result.Receipt.ContentHash));
            session.Disconnect();
            await NextFrame();
            Assert.That(session.IsReady, Is.True);
        }

        private sealed class Outcome
        {
            public DeliveryReceipt Receipt;
            public Exception ClientError;
            public Exception ServerError;
        }

        [Test]
        public async Task RealMni_ManipulatesForSixtySecondsDisconnected_ThenRetriesAndReplacesWithoutChangingPose()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-questAnatomyFixture");
            if (index < 0) Assert.Ignore("Pass -questAnatomyFixture with the captured MNI HBNA.");
            byte[] bytes = File.ReadAllBytes(arguments[index + 1]);
            var first = await Transfer(bytes);
            Assert.That(first.ClientError, Is.Null);
            Assert.That(first.ServerError, Is.Null);
            Mesh mesh = view.SharedMesh;
            string hash = session.ContentHash;
            session.Disconnect();
            var manipulator = root.GetComponent<QuestAnatomyManipulator>();
            Vector3 center = root.transform.TransformPoint(mesh.bounds.center * 0.001f);
            var left = new Pose(center - Vector3.right * 0.06f, Quaternion.identity);
            var right = new Pose(center + Vector3.right * 0.06f, Quaternion.identity);
            manipulator.Step(left, true, false, right, true, false);
            manipulator.Step(left, true, true, right, true, true);
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            int firstFrame = Time.frameCount;
            while (elapsed.Elapsed.TotalSeconds < 60)
            {
                float seconds = (float)elapsed.Elapsed.TotalSeconds;
                Vector3 offset = new Vector3(Mathf.Sin(seconds) * 0.1f, 0.05f, 0.02f);
                Quaternion rotation = Quaternion.Euler(0, seconds * 5, seconds * 2);
                float scale = 1.5f + 0.25f * Mathf.Sin(seconds);
                manipulator.Step(new Pose(center + offset + rotation * (Vector3.left * 0.06f * scale), rotation), true, true, new Pose(center + offset + rotation * (Vector3.right * 0.06f * scale), rotation), true, true);
                Assert.That(session.IsReady, Is.True);
                Assert.That(view.SharedMesh, Is.SameAs(mesh));
                await Task.Delay(20); // Never blocks the PlayerLoop; this is logical disconnection, not a radio test.
            }

            manipulator.CancelGrab();
            Assert.That(Time.frameCount, Is.GreaterThan(firstFrame + 60));
            Assert.That(root.transform.localScale.x, Is.GreaterThan(1.2f));
            Assert.That(session.ContentHash, Is.EqualTo(hash));
            Vector3 position = root.transform.position;
            Quaternion finalRotation = root.transform.rotation;
            Vector3 finalScale = root.transform.localScale;
            Assert.That((await Transfer(bytes)).Receipt.Status, Is.EqualTo(DeliveryStatus.AlreadyPublished));
            Assert.That(view.SharedMesh, Is.SameAs(mesh));
            Assert.That(view.UploadCount, Is.EqualTo(1));
            AnatomySnapshot original = AnatomySnapshotCodec.Decode(bytes);
            var replacement = AnatomySnapshot.Create("quest012-replacement", original.SessionId, original.VisualizationId, original.ColumnId, original.ContentRevision, original.Coordinates, original.Winding, original.Visible, original.Color.ToArray(), original.Positions.ToArray(), original.Normals.ToArray(), original.Indices.ToArray(), original.Uvs.ToArray());
            byte[] replacementBytes = AnatomySnapshotCodec.Encode(replacement);
            var next = await Transfer(replacementBytes);
            Assert.That(next.Receipt.Status, Is.EqualTo(DeliveryStatus.Published));
            Assert.That(session.ContentHash, Is.EqualTo(new DeliveryReceipt(TransportIdentity.Hash(replacementBytes), DeliveryStatus.Published).ContentHash));
            Assert.That(view.UploadCount, Is.EqualTo(2));
            Assert.That(view.SharedMesh.vertexCount, Is.EqualTo(69104));
            Assert.That(root.transform.position, Is.EqualTo(position));
            Assert.That(root.transform.rotation, Is.EqualTo(finalRotation));
            Assert.That(root.transform.localScale, Is.EqualTo(finalScale));
            await NextFrame();
            Assert.That(mesh == null, Is.True, "Replacement releases the old mesh.");
            TestContext.Out.WriteLine($"Offline manipulation seconds={elapsed.Elapsed.TotalSeconds}; PlayerLoop frames={Time.frameCount - firstFrame}; exact retry then replacement preserved pose/scale.");
        }

        // A real loopback TLS peer with deterministic faults below the production frame reader.
        private async Task<Outcome> Transfer(byte[] payload, string fault = null, TaskCompletionSource<bool> offered = null, TaskCompletionSource<bool> resume = null)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var cancelled = stop.Token.Register(listener.Stop);
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var outcome = new Outcome();
            Task server = Task.Run(async () =>
            {
                try
                {
                    using TcpClient peer = await listener.AcceptTcpClientAsync();
                    using var abort = stop.Token.Register(peer.Close);
                    using var tls = new SslStream(peer.GetStream(), false);
                    await tls.AuthenticateAsServerAsync(identity, false, SslProtocols.Tls12, false);
                    byte[] receivedSecret = new byte[32];
                    await PinnedTlsTransfer.ReadExactAsync(tls, receivedSecret, 0, 32, stop.Token);
                    if (!TransportIdentity.Equal(receivedSecret, secret)) throw new AuthenticationException();
                    await tls.WriteAsync(new byte[] { 1 }, 0, 1, stop.Token);
                    using var framed = new FaultStream(tls, fault, offered, resume);
                    await PinnedTlsTransfer.SendPayloadAsync(framed, payload, stop.Token, fault == "chunk");
                }
                catch (Exception exception)
                {
                    outcome.ServerError = exception;
                }
            });
            try
            {
                try
                {
                    outcome.Receipt = await session.ReceiveAsync("127.0.0.1", port, TransportIdentity.Hash(identity.RawData), secret, stop.Token);
                }
                catch (Exception exception)
                {
                    outcome.ClientError = exception;
                }
            }
            finally
            {
                await server;
                listener.Stop();
            }

            return outcome;
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

        private static async Task NextFrame()
        {
            int frame = Time.frameCount;
            while (Time.frameCount == frame) await Task.Yield();
        }

        private sealed class FaultStream : Stream
        {
            private readonly Stream inner;
            private readonly string fault;
            private readonly TaskCompletionSource<bool> offered;
            private readonly TaskCompletionSource<bool> resume;
            private int writes;

            public FaultStream(Stream inner, string fault, TaskCompletionSource<bool> offered, TaskCompletionSource<bool> resume)
            {
                this.inner = inner;
                this.fault = fault;
                this.offered = offered;
                this.resume = resume;
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                writes++;
                if (fault == "transport-version" && writes == 1) buffer[3] = 99;
                if (fault == "truncated" && writes == 3)
                {
                    await inner.WriteAsync(buffer, offset, count / 2, token);
                    throw new EndOfStreamException("Injected truncation inside a chunk.");
                }

                if (fault == "pause" && writes == 3)
                {
                    offered.SetResult(true);
                    await resume.Task;
                }

                await inner.WriteAsync(buffer, offset, count, token);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                int read = await inner.ReadAsync(buffer, offset, count, token);
                if (fault == "lost-ack") throw new EndOfStreamException("Injected loss of the publication ACK before sender observes it.");
                return read;
            }

            public override bool CanRead => true;
            public override bool CanWrite => true;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }
    }
}
#endif
