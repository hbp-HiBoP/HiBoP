using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.StaticRendering;
using NUnit.Framework;
using UnityEngine;

namespace CRNL.HiBoP.XR.RemoteAssets.Tests
{
    public class P08RemoteSurfaceIntegrationTests
    {
        [Test]
        public void TwoRenderersShareValidatedPayloadAndMeshUntilExplicitLifecycleRelease()
        {
            SurfaceAsset source = CreateSurface();
            byte[] payload = SurfaceAssetPayloadCodec.Encode(source);
            AssetHash hash = SurfaceAssetPayloadCodec.ComputeHash(payload);
            var descriptor = new RemoteAssetDescriptor(new AssetReference(new ContractId(8, 1), hash, SurfaceAssetPayloadCodec.SchemaVersion), RemoteAssetKind.Surface, RemoteAssetVariant.Anatomical, payload.Length, source.Positions.Count, source.Indices.Count, source.StaticUvs.Count, 64, Array.Empty<RemoteAssetDependency>());
            using var cache = new InMemoryRemoteAssetCache(1024);
            RemoteAssetTransfer transfer = cache.StartTransfer(descriptor).Transfer;
            for (int chunk = 0; chunk < descriptor.ChunkCount; chunk++)
            {
                int offset = chunk * descriptor.ChunkBytes;
                int count = Math.Min(descriptor.ChunkBytes, payload.Length - offset);
                var bytes = new byte[count];
                Buffer.BlockCopy(payload, offset, bytes, 0, count);
                transfer.WriteChunk(chunk, bytes);
            }

            Assert.That(transfer.Complete(), Is.EqualTo(AssetTransferCompletion.Published));
            var surfaceStore = new RemoteSurfaceAssetStore(cache);
            GameObject firstObject = CreateRendererObject("P08 renderer one", out P05StaticSurfaceRenderer firstRenderer, out MeshFilter firstFilter);
            GameObject secondObject = CreateRendererObject("P08 renderer two", out P05StaticSurfaceRenderer secondRenderer, out MeshFilter secondFilter);
            try
            {
                using var first = new RemoteSurfaceRendererBinding(surfaceStore, firstRenderer);
                using var second = new RemoteSurfaceRendererBinding(surfaceStore, secondRenderer);
                Assert.That(first.TryActivate(hash, SurfaceTransparency.Opaque), Is.True);
                Assert.That(second.TryActivate(hash, SurfaceTransparency.Opaque), Is.True);
                Assert.That(firstFilter.sharedMesh, Is.SameAs(secondFilter.sharedMesh));
                Assert.That(first.ActiveSurfaceAsset, Is.SameAs(second.ActiveSurfaceAsset));
                Assert.That(cache.StoredAssetCount, Is.EqualTo(1));

                byte[] replacementPayload = (byte[])payload.Clone();
                replacementPayload[^1] ^= 0xff;
                AssetHash replacementHash = SurfaceAssetPayloadCodec.ComputeHash(replacementPayload);
                var replacementDescriptor = new RemoteAssetDescriptor(new AssetReference(new ContractId(8, 2), replacementHash, SurfaceAssetPayloadCodec.SchemaVersion), RemoteAssetKind.Surface, RemoteAssetVariant.Anatomical, replacementPayload.Length, source.Positions.Count, source.Indices.Count, source.StaticUvs.Count, 64, Array.Empty<RemoteAssetDependency>());
                RemoteAssetTransfer incomplete = cache.StartTransfer(replacementDescriptor).Transfer;
                var firstChunk = new byte[64];
                Buffer.BlockCopy(replacementPayload, 0, firstChunk, 0, firstChunk.Length);
                incomplete.WriteChunk(0, firstChunk);
                Mesh visibleMesh = firstFilter.sharedMesh;
                Assert.That(first.TryActivate(replacementHash, SurfaceTransparency.Opaque), Is.False);
                Assert.That(firstFilter.sharedMesh, Is.SameAs(visibleMesh), "Incomplete replacement cannot disturb active content.");

                AssetCacheLifecycleResult lifecycle = cache.ApplyLifecycle(AssetCacheLifecycleEvent.Backgrounded);
                Assert.That(lifecycle.CancelledTransfers, Is.EqualTo(1));
                Assert.That(lifecycle.ActiveAssetsAwaitingExplicitRelease, Is.EqualTo(1));
                Assert.That(firstFilter.sharedMesh, Is.Not.Null, "Background purge cannot silently remove visible content.");
                Assert.That(secondFilter.sharedMesh, Is.Not.Null);
                Assert.That(cache.TryAcquire(hash, out _), Is.False, "Pending-purge content cannot get a new consumer.");

                first.ReleaseActiveContent();
                Assert.That(cache.StoredAssetCount, Is.EqualTo(1));
                second.ReleaseActiveContent();
                Assert.That(cache.StoredAssetCount, Is.Zero);
                Assert.That(cache.ResidentBytes, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstObject.GetComponent<MeshRenderer>().sharedMaterial);
                UnityEngine.Object.DestroyImmediate(secondObject.GetComponent<MeshRenderer>().sharedMaterial);
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }

        private static GameObject CreateRendererObject(string name, out P05StaticSurfaceRenderer presenter, out MeshFilter filter)
        {
            var gameObject = new GameObject(name);
            filter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("HiBoP XR/P05/Surface Opaque");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            presenter = gameObject.AddComponent<P05StaticSurfaceRenderer>();
            presenter.Configure(filter, meshRenderer, material, material, null, null, null, Color.gray, 1f, 0);
            return gameObject;
        }

        private static SurfaceAsset CreateSurface()
        {
            return new SurfaceAsset(new AssetHash(8, 8, 8, 8), SurfaceRepresentation.Anatomical, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0, 0, 0), new Float3(1, 1, 0)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 0), new Float3(1, 0, 0), new Float3(0, 1, 0) }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 1), new Float3(0, 0, 1), new Float3(0, 0, 1) }), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2 }), RenderBuffer<Float2>.TakeOwnership(Array.Empty<Float2>()));
        }
    }
}
