using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Codecs;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class BlockTransferTests
    {
        [Test]
        public void NativeShaMatchesKnownVectorsOffsetsAndReset()
        {
            using var native = TransferCodec.CreateHash();
            using var reference = SHA256.Create();
            foreach (int length in new[] { 0, 3, 55, 64, 65537, 1048576 })
            {
                var bytes = new byte[length + 7];
                new Random(715).NextBytes(bytes);
                Assert.That(native.ComputeHash(bytes, 7, length), Is.EqualTo(reference.ComputeHash(bytes, 7, length)));
                native.Initialize();
                int first = length / 2;
                native.TransformBlock(bytes, 7, first, null, 0);
                native.TransformFinalBlock(bytes, 7 + first, length - first);
                Assert.That(native.Hash, Is.EqualTo(reference.ComputeHash(bytes, 7, length)));
            }

            Assert.That(BitConverter.ToString(native.ComputeHash(Encoding.ASCII.GetBytes("abc"))).Replace("-", "").ToLowerInvariant(), Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void NativeCodecRetainsEveryByteAndRejectsWrongSize(byte codec)
        {
            var raw = Enumerable.Range(0, TransferCodec.MaximumBlockBytes).Select(i => (byte)(i % 17)).ToArray();
            var compressed = new byte[raw.Length + 65536];
            int size = TransferCodec.Encode(codec, 3, raw, raw.Length, compressed);
            var restored = new byte[raw.Length];
            TransferCodec.Decode(codec, compressed, size, restored, raw.Length);
            Assert.That(restored, Is.EqualTo(raw));
            Assert.Throws<InvalidDataException>(() => TransferCodec.Decode(codec, compressed, size, restored, raw.Length - 1));
        }

        [TestCase("none")]
        [TestCase("payload")]
        [TestCase("sequence")]
        [TestCase("offset")]
        [TestCase("codec")]
        [TestCase("oversize")]
        [TestCase("reserved")]
        [TestCase("endHash")]
        [TestCase("truncate")]
        [TestCase("manifest")]
        [Timeout(10000)]
        public async Task OrderedBlocksMustAllVerifyBeforeCompletion(string corruption)
        {
            var data = new byte[TransferCodec.BlockBytes + 171];
            new Random(17).NextBytes(data); // Incompressible fallback plus partial last block.
            var packets = new List<byte[]>();
            byte[] expected = BlockContainer.Produce(new[] { new BlockResource("a.bin", data.Length, (s, t) => s.Write(data, 0, data.Length)), new BlockResource("empty.bin", 0, (s, t) => { }) }, packets.Add, CancellationToken.None);
            if (corruption == "payload") packets[1][64] ^= 1;
            if (corruption == "sequence") packets[1][4] = 5;
            if (corruption == "offset") packets[1][12] = 1;
            if (corruption == "codec") packets[1][1] = 254;
            if (corruption == "oversize") Buffer.BlockCopy(BitConverter.GetBytes(int.MaxValue), 0, packets[1], 20, 4);
            if (corruption == "reserved") packets[1][63] = 1;
            if (corruption == "endHash") packets.Last()[28] ^= 1;
            if (corruption == "truncate") packets.RemoveAt(packets.Count - 1);
            if (corruption == "manifest") packets[0][13] = (byte)'b';
            using var input = new MemoryStream(packets.SelectMany(p => p).ToArray());
            input.Position = 4;
            var sink = new MemorySink();
            Exception error = null;
            byte[] hash = null;
            try
            {
                hash = await BlockContainer.ReceiveAsync(input, sink, CancellationToken.None);
            }
            catch (Exception exception)
            {
                error = exception;
            }

            if (corruption == "none")
            {
                Assert.That(error, Is.Null);
                Assert.That(hash, Is.EqualTo(expected));
                Assert.That(sink.Files[0].ToArray(), Is.EqualTo(data));
                Assert.That(sink.Completed, Is.True);
            }
            else
            {
                Assert.That(error, Is.Not.Null);
                Assert.That(sink.Completed, Is.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Timeout(10000)]
        public async Task PackedBlockSceneKeepsScientificValuesAndCanonicalGlobals(bool cancelAfterReceive)
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-blocks-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var source = new SceneArchive(Path.Combine(root, "source"), deferResourceWrites: true);
                var payload = PreparedSceneArchiveTests.Fixture(source);
                byte[] metadata = source.CaptureMetadata(payload);
                var packets = new List<byte[]>();
                BlockContainer.Produce(source.CaptureBlockResources(metadata), packets.Add, CancellationToken.None);
                using var input = new MemoryStream(packets.SelectMany(p => p).ToArray());
                input.Position = 4;
                using var target = new SceneArchive(Path.Combine(root, "target"), true, source.Globals);
                using var stop = new CancellationTokenSource();
                await BlockContainer.ReceiveAsync(input, target, stop.Token);
                if (cancelAfterReceive)
                {
                    stop.Cancel();
                    Exception error = null;
                    try
                    {
                        target.ReadPrepared();
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    Assert.That(error, Is.Not.Null);
                    return;
                }

                var restored = target.ReadPrepared();
                var ieeg = (HBP.Core.Data.IEEGColumn)restored.Visualization.Columns[1];
                Assert.That(ieeg.Data.ProcessedValuesByChannel["patient_A1"], Is.EqualTo(new[] { 1f, 2f, 3f, 4f }));
                Assert.That(ieeg.Bloc, Is.SameAs(source.Globals.Data.Protocols.Single().Blocs.Single()));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        [Timeout(10000)]
        public async Task ValidTransportCannotHideCorruptLogicalPack()
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-pack-corrupt-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var source = new SceneArchive(Path.Combine(root, "source"), deferResourceWrites: true);
                var payload = PreparedSceneArchiveTests.Fixture(source);
                var resources = source.CaptureBlockResources(source.CaptureMetadata(payload)).ToArray();
                var original = resources[3];
                resources[3] = new BlockResource(original.Name, original.Length, (output, token) =>
                {
                    using var raw = new MemoryStream();
                    original.Write(raw, token);
                    byte[] bytes = raw.ToArray();
                    bytes[0] ^= 1;
                    output.Write(bytes, 0, bytes.Length);
                });
                var packets = new List<byte[]>();
                BlockContainer.Produce(resources, packets.Add, CancellationToken.None);
                using var wire = new MemoryStream(packets.SelectMany(p => p).ToArray());
                wire.Position = 4;
                using var target = new SceneArchive(Path.Combine(root, "target"), true, source.Globals);
                Exception failure = null;
                try
                {
                    await BlockContainer.ReceiveAsync(wire, target, CancellationToken.None);
                }
                catch (Exception e)
                {
                    failure = e;
                }

                Assert.That(failure, Is.Not.Null);
                Assert.Throws<InvalidOperationException>(() => target.ReadPrepared());
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        [Timeout(10000)]
        public async Task FailedNetworkCancelsProducerAndReleasesFullQueue()
        {
            string file = Path.Combine(Path.GetTempPath(), "hibop-block-spool-" + Guid.NewGuid().ToString("N"));
            bool released = false;
            var bytes = new byte[TransferCodec.BlockBytes * 8];
            var delivery = new BlockDelivery(file, () => new[] { new BlockResource("large.bin", bytes.Length, (s, t) => s.Write(bytes, 0, bytes.Length)) }, () => released = true, CancellationToken.None);
            try
            {
                Exception failure = null;
                try
                {
                    await delivery.SendAsync(new FailingStream(), CancellationToken.None, null);
                }
                catch (Exception error)
                {
                    failure = error;
                }

                Assert.That(failure, Is.Not.Null);
                Assert.That(released, Is.True);
                Assert.That(delivery.IsPrepared, Is.False);
            }
            finally
            {
                await delivery.CloseAsync();
                File.Delete(file);
            }
        }

        [Test]
        [Timeout(10000)]
        public async Task ConnectionFailureBeforeSendCanReleaseUnconsumedProducer()
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-connect-failure-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            bool freed = false;
            var bytes = new byte[TransferCodec.BlockBytes * 8];
            var delivery = new SceneDelivery(Path.Combine(root, "spool"), "id", "session", "fixture", () => new[] { new BlockResource("large.bin", bytes.Length, (s, t) => s.Write(bytes, 0, bytes.Length)) }, () => freed = true, CancellationToken.None);
            await delivery.DisposeAsync(); // Outer connection/auth failed, so SendAsync was never called.
            Assert.That(freed, Is.True);
            Assert.That(Directory.Exists(root), Is.False);
        }

        [Test]
        [Timeout(10000)]
        public async Task AckLossRetriesExactlyTheFinalizedSpool()
        {
            string path = Path.Combine(Path.GetTempPath(), "hibop-retry-" + Guid.NewGuid().ToString("N"));
            var bytes = new byte[TransferCodec.BlockBytes + 10];
            new Random(42).NextBytes(bytes);
            var delivery = new BlockDelivery(path, () => new[] { new BlockResource("data.bin", bytes.Length, (s, t) => s.Write(bytes, 0, bytes.Length)) }, () => { }, CancellationToken.None);
            try
            {
                using var first = new ReceiptStream { LoseAck = true };
                Exception error = null;
                try
                {
                    await delivery.SendAsync(first, CancellationToken.None, null);
                }
                catch (Exception e)
                {
                    error = e;
                }

                Assert.That(error, Is.TypeOf<IOException>());
                Assert.That(delivery.IsPrepared, Is.True);
                bytes[0] ^= 1; // Replay must not re-read source data.
                using var second = new ReceiptStream();
                var result = await delivery.SendAsync(second, CancellationToken.None, null);
                Assert.That(result.Status, Is.EqualTo(DeliveryStatus.AlreadyPublished));
                Assert.That(second.ToArray(), Is.EqualTo(first.ToArray()));
            }
            finally
            {
                await delivery.CloseAsync();
                File.Delete(path);
            }
        }

        private sealed class ReceiptStream : MemoryStream
        {
            internal bool LoseAck;

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                if (LoseAck) return Task.FromException<int>(new IOException("ACK lost"));
                Assert.That(count, Is.EqualTo(33));
                byte[] sent = ToArray();
                buffer[offset] = (byte)DeliveryStatus.AlreadyPublished;
                Buffer.BlockCopy(sent, sent.Length - 64 + 28, buffer, offset + 1, 32);
                return Task.FromResult(33);
            }
        }

        private sealed class MemorySink : IBlockSink
        {
            internal readonly List<MemoryStream> Files = new();
            internal bool Completed;

            public void Begin(IReadOnlyList<BlockResource> resources, CancellationToken token)
            {
            }

            public Stream Open(int index)
            {
                var file = new MemoryStream();
                Files.Add(file);
                return file;
            }

            public void Complete() => Completed = true;
        }

        private sealed class FailingStream : MemoryStream
        {
            public override Task WriteAsync(byte[] b, int o, int c, CancellationToken token) => Task.FromException(new IOException("Disconnected"));
        }
    }
}
