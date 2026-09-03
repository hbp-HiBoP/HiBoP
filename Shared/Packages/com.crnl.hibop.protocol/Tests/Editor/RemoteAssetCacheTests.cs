using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class RemoteAssetCacheTests
    {
        private const int PayloadBytes = 121;

        [Test]
        public void CorruptAndIncompleteTransfersAreNeverPublished()
        {
            byte[] valid = Payload(1);
            RemoteAssetDescriptor descriptor = Descriptor(valid, chunkBytes: 32);
            using var cache = new InMemoryRemoteAssetCache(1024);
            RemoteAssetTransfer transfer = cache.StartTransfer(descriptor).Transfer;
            transfer.WriteChunk(0, valid.Take(32).ToArray());

            Assert.That(transfer.Complete(), Is.EqualTo(AssetTransferCompletion.Incomplete));
            Assert.That(cache.TryAcquire(descriptor.Asset.Hash, out _), Is.False);
            transfer.Cancel();

            valid[^1] ^= 0xff;
            transfer = cache.StartTransfer(Descriptor(valid, chunkBytes: 32)).Transfer;
            byte[] corrupted = (byte[])valid.Clone();
            corrupted[5] ^= 0xff;
            for (int chunk = 0; chunk < transfer.Descriptor.ChunkCount; chunk++)
                transfer.WriteChunk(chunk, Slice(corrupted, chunk, transfer.Descriptor.ChunkBytes));

            Assert.That(transfer.Complete(), Is.EqualTo(AssetTransferCompletion.Corrupt));
            Assert.That(cache.TryAcquire(transfer.Descriptor.Asset.Hash, out _), Is.False);
            Assert.That(cache.StagingTransferCount, Is.Zero);
        }

        [Test]
        public void InterruptedTransferResumesOnlyMissingRangesAndAcceptsIdenticalDuplicates()
        {
            byte[] payload = Payload(2);
            RemoteAssetDescriptor descriptor = Descriptor(payload, chunkBytes: 32);
            using var cache = new InMemoryRemoteAssetCache(1024);
            RemoteAssetTransfer first = cache.StartTransfer(descriptor).Transfer;
            first.WriteChunk(0, Slice(payload, 0, descriptor.ChunkBytes));
            first.WriteChunk(2, Slice(payload, 2, descriptor.ChunkBytes));
            first.WriteChunk(2, Slice(payload, 2, descriptor.ChunkBytes));

            AssetCacheLifecycleResult interrupted = cache.ApplyLifecycle(AssetCacheLifecycleEvent.ConnectionInterrupted);
            AssetTransferStartResult resumed = cache.StartTransfer(descriptor);
            Assert.That(interrupted.CancelledTransfers, Is.Zero);
            Assert.That(resumed.Status, Is.EqualTo(AssetTransferStartStatus.JoinedExisting));
            Assert.That(resumed.Transfer, Is.SameAs(first));
            Assert.That(first.DuplicateChunkCount, Is.EqualTo(1));
            Assert.That(first.GetMissingRanges().Select(range => range.Offset), Is.EqualTo(new[] { 32, 96 }));

            first.WriteChunk(1, Slice(payload, 1, descriptor.ChunkBytes));
            first.WriteChunk(3, Slice(payload, 3, descriptor.ChunkBytes));
            Assert.That(first.Complete(), Is.EqualTo(AssetTransferCompletion.Published));
            Assert.That(cache.TryAcquire(descriptor.Asset.Hash, out RemoteAssetLease lease), Is.True);
            lease.Dispose();
        }

        [Test]
        public void ConflictingDuplicateCancelsStaging()
        {
            byte[] payload = Payload(3);
            RemoteAssetDescriptor descriptor = Descriptor(payload, chunkBytes: 32);
            using var cache = new InMemoryRemoteAssetCache(1024);
            RemoteAssetTransfer transfer = cache.StartTransfer(descriptor).Transfer;
            byte[] first = Slice(payload, 0, descriptor.ChunkBytes);
            transfer.WriteChunk(0, first);
            first[0] ^= 0xff;

            Assert.Throws<InvalidDataException>(() => transfer.WriteChunk(0, first));
            Assert.That(transfer.Result, Is.EqualTo(AssetTransferCompletion.Cancelled));
            Assert.That(cache.StagingTransferCount, Is.Zero);
            Assert.That(cache.ResidentBytes, Is.Zero);
        }

        [Test]
        public void OneStagingTransferAndOnePhysicalPayloadServeTwoConsumers()
        {
            byte[] payload = Payload(4);
            RemoteAssetDescriptor descriptor = Descriptor(payload, chunkBytes: 32);
            using var cache = new InMemoryRemoteAssetCache(1024);
            AssetTransferStartResult firstStart = cache.StartTransfer(descriptor);
            AssetTransferStartResult secondStart = cache.StartTransfer(descriptor);
            Assert.That(secondStart.Status, Is.EqualTo(AssetTransferStartStatus.JoinedExisting));
            Assert.That(secondStart.Transfer, Is.SameAs(firstStart.Transfer));

            Fill(firstStart.Transfer, payload);
            Assert.That(firstStart.Transfer.Complete(), Is.EqualTo(AssetTransferCompletion.Published));
            Assert.That(cache.TryAcquire(descriptor.Asset.Hash, out RemoteAssetLease first), Is.True);
            Assert.That(cache.TryAcquire(descriptor.Asset.Hash, out RemoteAssetLease second), Is.True);
            Assert.That(cache.StoredAssetCount, Is.EqualTo(1));
            Assert.That(cache.ResidentBytes, Is.EqualTo(payload.Length));
            using Stream firstStream = first.OpenRead();
            using Stream secondStream = second.OpenRead();
            Assert.That(firstStream.ReadByte(), Is.EqualTo(secondStream.ReadByte()));
            first.Dispose();
            second.Dispose();
        }

        [Test]
        public void LifecycleRetainsResumeStateButNeverSilentlyRemovesActiveContent()
        {
            byte[] activePayload = Payload(5);
            byte[] stagingPayload = Payload(6);
            RemoteAssetDescriptor activeDescriptor = Descriptor(activePayload, chunkBytes: 32);
            RemoteAssetDescriptor stagingDescriptor = Descriptor(stagingPayload, chunkBytes: 32);
            using var cache = new InMemoryRemoteAssetCache(1024);
            Publish(cache, activeDescriptor, activePayload);
            Assert.That(cache.TryAcquire(activeDescriptor.Asset.Hash, out RemoteAssetLease activeLease), Is.True);
            RemoteAssetTransfer staging = cache.StartTransfer(stagingDescriptor).Transfer;
            staging.WriteChunk(0, Slice(stagingPayload, 0, stagingDescriptor.ChunkBytes));

            cache.ApplyLifecycle(AssetCacheLifecycleEvent.ConnectionInterrupted);
            Assert.That(cache.StagingTransferCount, Is.EqualTo(1), "The P07 resume lease keeps staging in memory.");
            AssetCacheLifecycleResult epoch = cache.ApplyLifecycle(AssetCacheLifecycleEvent.NewEpoch);
            Assert.That(epoch.CancelledTransfers, Is.EqualTo(1));
            Assert.That(epoch.ActiveAssetsAwaitingExplicitRelease, Is.EqualTo(1));
            Assert.That(cache.StoredAssetCount, Is.EqualTo(1));
            Assert.That(cache.TryAcquire(activeDescriptor.Asset.Hash, out _), Is.False, "Old-epoch content cannot gain a new consumer.");
            using Stream stillVisible = activeLease.OpenRead();
            Assert.That(stillVisible.Length, Is.EqualTo(activePayload.Length), "The active lease remains valid until its owner releases it explicitly.");

            activeLease.Dispose();
            Assert.That(cache.StoredAssetCount, Is.Zero);
            Assert.That(cache.ResidentBytes, Is.Zero);
        }

        [TestCase(AssetCacheLifecycleEvent.ResumeLeaseExpired)]
        [TestCase(AssetCacheLifecycleEvent.NewEpoch)]
        [TestCase(AssetCacheLifecycleEvent.Backgrounded)]
        [TestCase(AssetCacheLifecycleEvent.Closed)]
        public void DestructiveLifecycleEventsCancelStagingAndPurgeInactive(AssetCacheLifecycleEvent lifecycleEvent)
        {
            byte[] committedPayload = Payload(15);
            byte[] stagingPayload = Payload(16);
            using var cache = new InMemoryRemoteAssetCache(1024);
            Publish(cache, Descriptor(committedPayload, chunkBytes: 32), committedPayload);
            RemoteAssetTransfer staging = cache.StartTransfer(Descriptor(stagingPayload, chunkBytes: 32)).Transfer;
            staging.WriteChunk(0, Slice(stagingPayload, 0, staging.Descriptor.ChunkBytes));

            AssetCacheLifecycleResult result = cache.ApplyLifecycle(lifecycleEvent);

            Assert.That(result.CancelledTransfers, Is.EqualTo(1));
            Assert.That(result.PurgedInactiveAssets, Is.EqualTo(1));
            Assert.That(result.ActiveAssetsAwaitingExplicitRelease, Is.Zero);
            Assert.That(cache.ResidentBytes, Is.Zero);
        }

        [Test]
        public void RepeatedOpenReleaseAndCloseCyclesLeaveNoResidentPayload()
        {
            byte[] payload = Payload(17);
            RemoteAssetDescriptor descriptor = Descriptor(payload, chunkBytes: 32);
            for (int cycle = 0; cycle < 256; cycle++)
            {
                using var cache = new InMemoryRemoteAssetCache(1024);
                Publish(cache, descriptor, payload);
                Assert.That(cache.TryAcquire(descriptor.Asset.Hash, out RemoteAssetLease lease), Is.True);
                lease.Dispose();
                cache.ApplyLifecycle(AssetCacheLifecycleEvent.Closed);
                Assert.That(cache.ResidentBytes, Is.Zero, $"cycle {cycle}");
            }
        }

        [Test]
        public void PressureEvictsOnlyInactiveLruAndReturnsExplicitBlockWhenActiveContentFillsBudget()
        {
            byte[] firstPayload = Payload(7);
            byte[] secondPayload = Payload(8);
            byte[] thirdPayload = Payload(9);
            RemoteAssetDescriptor firstDescriptor = Descriptor(firstPayload, chunkBytes: 32);
            RemoteAssetDescriptor secondDescriptor = Descriptor(secondPayload, chunkBytes: 32);
            RemoteAssetDescriptor thirdDescriptor = Descriptor(thirdPayload, chunkBytes: 32);
            using var cache = new InMemoryRemoteAssetCache(PayloadBytes * 2L);
            Publish(cache, firstDescriptor, firstPayload);
            Publish(cache, secondDescriptor, secondPayload);
            Assert.That(cache.TryAcquire(firstDescriptor.Asset.Hash, out RemoteAssetLease touch), Is.True);
            touch.Dispose();
            Publish(cache, thirdDescriptor, thirdPayload);

            Assert.That(cache.TryAcquire(secondDescriptor.Asset.Hash, out _), Is.False, "The least-recently-used inactive asset is evicted.");
            Assert.That(cache.TryAcquire(firstDescriptor.Asset.Hash, out RemoteAssetLease firstActive), Is.True);
            Assert.That(cache.TryAcquire(thirdDescriptor.Asset.Hash, out RemoteAssetLease thirdActive), Is.True);
            AssetTransferStartResult blocked = cache.StartTransfer(Descriptor(Payload(10), chunkBytes: 32));
            Assert.That(blocked.Status, Is.EqualTo(AssetTransferStartStatus.MemoryPressureRequiresUserAction));
            Assert.That(blocked.RequiresUserAction, Is.True);
            Assert.That(cache.StoredAssetCount, Is.EqualTo(2), "Pressure must not remove active content.");
            firstActive.Dispose();
            thirdActive.Dispose();
        }

        [Test]
        public void VariantManifestBindsInflatedContentToItsAnatomicalHashWithoutAssumingSharedTopology()
        {
            byte[] anatomicalPayload = Payload(11);
            byte[] inflatedPayload = Payload(12, 157);
            RemoteAssetDescriptor anatomical = Descriptor(anatomicalPayload, RemoteAssetVariant.Anatomical, Array.Empty<RemoteAssetDependency>());
            RemoteAssetDescriptor inflated = Descriptor(inflatedPayload, RemoteAssetVariant.Inflated, new[] { new RemoteAssetDependency(RemoteAssetDependencyKind.VariantBase, anatomical.Asset.Hash) }, primaryCount: 4, secondaryCount: 6);
            var manifest = new SurfaceVariantSetDescriptor(anatomical, inflated);

            Assert.That(manifest.ManifestHash.IsValid, Is.True);
            Assert.That(manifest.Anatomical.PrimaryCount, Is.Not.EqualTo(manifest.Inflated.PrimaryCount));
            Assert.Throws<ArgumentException>(() => new SurfaceVariantSetDescriptor(inflated, anatomical));
        }

        [Test]
        public void DescriptorRejectsAllocationBombBeforeCacheAllocation()
        {
            byte[] payload = Payload(13);
            AssetHash hash = Hash(payload);
            var asset = new AssetReference(new ContractId(1, 13), hash, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RemoteAssetDescriptor(asset, RemoteAssetKind.Surface, RemoteAssetVariant.Anatomical, RemoteAssetDescriptor.MaximumSurfaceBytes, RemoteAssetDescriptor.MaximumSurfaceVertices + 1, 3, 0, 32, Array.Empty<RemoteAssetDependency>()));

            using var cache = new InMemoryRemoteAssetCache(PayloadBytes - 1);
            AssetTransferStartResult result = cache.StartTransfer(Descriptor(payload, chunkBytes: 32));
            Assert.That(result.Status, Is.EqualTo(AssetTransferStartStatus.AssetExceedsBudget));
            Assert.That(cache.ResidentBytes, Is.Zero);
        }

        [Test]
        public void SafetyCapabilitiesNegotiateTheMinimumAndRejectBeforeStaging()
        {
            var local = new RemoteAssetCapabilities(64, 1024, 1024, 1024, 1024);
            var remote = new RemoteAssetCapabilities(32, 512, 512, 512, 512);
            RemoteAssetCapabilities negotiated = RemoteAssetCapabilities.Negotiate(local, remote);
            byte[] payload = Payload(14);
            RemoteAssetDescriptor descriptor = Descriptor(payload, chunkBytes: 64);
            using var cache = new InMemoryRemoteAssetCache(1024, negotiated);

            Assert.That(negotiated.MaximumChunkBytes, Is.EqualTo(32));
            Assert.Throws<InvalidOperationException>(() => cache.StartTransfer(descriptor));
            Assert.That(cache.StagingBytes, Is.Zero);
        }

        [Test]
        public void RuntimeCacheContainsNoFilesystemPersistenceApi()
        {
            string source = File.ReadAllText(Path.GetFullPath("Shared/Packages/com.crnl.hibop.protocol/Runtime/RemoteAssets.cs"));
            Assert.That(source, Does.Not.Contain("File."));
            Assert.That(source, Does.Not.Contain("Directory."));
            Assert.That(source, Does.Not.Contain("FileStream"));
            Assert.That(source, Does.Not.Contain("persistentDataPath"));
        }

        private static void Publish(InMemoryRemoteAssetCache cache, RemoteAssetDescriptor descriptor, byte[] payload)
        {
            RemoteAssetTransfer transfer = cache.StartTransfer(descriptor).Transfer;
            Fill(transfer, payload);
            Assert.That(transfer.Complete(), Is.EqualTo(AssetTransferCompletion.Published));
        }

        private static void Fill(RemoteAssetTransfer transfer, byte[] payload)
        {
            for (int chunk = 0; chunk < transfer.Descriptor.ChunkCount; chunk++)
                transfer.WriteChunk(chunk, Slice(payload, chunk, transfer.Descriptor.ChunkBytes));
        }

        private static byte[] Slice(byte[] payload, int chunk, int chunkBytes)
        {
            int offset = chunk * chunkBytes;
            int count = Math.Min(chunkBytes, payload.Length - offset);
            var result = new byte[count];
            Buffer.BlockCopy(payload, offset, result, 0, count);
            return result;
        }

        private static RemoteAssetDescriptor Descriptor(byte[] payload, int chunkBytes = 64)
        {
            return Descriptor(payload, RemoteAssetVariant.Anatomical, Array.Empty<RemoteAssetDependency>(), chunkBytes);
        }

        private static RemoteAssetDescriptor Descriptor(byte[] payload, RemoteAssetVariant variant, RemoteAssetDependency[] dependencies, int chunkBytes = 64, int primaryCount = 3, int secondaryCount = 3)
        {
            AssetHash hash = Hash(payload);
            return new RemoteAssetDescriptor(new AssetReference(new ContractId(1, payload[0]), hash, 1), RemoteAssetKind.Surface, variant, payload.Length, primaryCount, secondaryCount, 0, chunkBytes, dependencies);
        }

        private static byte[] Payload(byte seed, int length = PayloadBytes)
        {
            var payload = new byte[length];
            for (int index = 0; index < payload.Length; index++)
                payload[index] = unchecked((byte)(seed + index));
            return payload;
        }

        private static AssetHash Hash(byte[] payload)
        {
            using SHA256 sha256 = SHA256.Create();
            return AssetHash.FromBytes(sha256.ComputeHash(payload));
        }
    }
}
