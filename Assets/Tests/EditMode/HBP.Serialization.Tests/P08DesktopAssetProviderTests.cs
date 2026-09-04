using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using HBP.RenderModelAdapters;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class P08DesktopAssetProviderTests
    {
        [Test]
        public void DesktopProviderPublishesImmutableHashedSurfaceRangesOnlyInMemory()
        {
            SurfaceAsset surface = CreateSurface(SurfaceRepresentation.Anatomical);
            using var desktop = new DesktopSurfaceAssetProvider();
            RemoteAssetDescriptor descriptor = desktop.Publish(surface, new ContractId(8, 1), 64);
            byte[] firstRange = desktop.Provider.ReadRange(descriptor.Asset.Hash, 0, 64);
            byte original = firstRange[0];
            firstRange[0] ^= 0xff;

            Assert.That(descriptor.Kind, Is.EqualTo(RemoteAssetKind.Surface));
            Assert.That(descriptor.Variant, Is.EqualTo(RemoteAssetVariant.Anatomical));
            Assert.That(descriptor.Asset.Hash, Is.Not.EqualTo(surface.Hash), "The wire content hash is authoritative, not the capture placeholder.");
            Assert.That(desktop.Provider.ReadRange(descriptor.Asset.Hash, 0, 1)[0], Is.EqualTo(original), "Ranges are defensive copies of immutable provider memory.");
            Assert.That(desktop.Provider.Count, Is.EqualTo(1));
        }

        [Test]
        public void InflatedDescriptorRequiresAndRecordsAnatomicalDependency()
        {
            using var desktop = new DesktopSurfaceAssetProvider();
            Assert.Throws<ArgumentException>(() => desktop.Publish(CreateSurface(SurfaceRepresentation.Inflated), new ContractId(8, 2), 64));
            RemoteAssetDescriptor anatomical = desktop.Publish(CreateSurface(SurfaceRepresentation.Anatomical), new ContractId(8, 1), 64);
            RemoteAssetDescriptor inflated = desktop.Publish(CreateSurface(SurfaceRepresentation.Inflated), new ContractId(8, 2), 64, anatomical.Asset.Hash);
            var variants = new SurfaceVariantSetDescriptor(anatomical, inflated);

            Assert.That(inflated.Dependencies[0].Hash, Is.EqualTo(anatomical.Asset.Hash));
            Assert.That(variants.ManifestHash.IsValid, Is.True);
        }

        private static SurfaceAsset CreateSurface(SurfaceRepresentation representation)
        {
            var positions = new[] { new Float3(0, 0, 0), new Float3(1, 0, 0), new Float3(0, 1, 0) };
            var normals = new[] { new Float3(0, 0, 1), new Float3(0, 0, 1), new Float3(0, 0, 1) };
            return new SurfaceAsset(new AssetHash(8, 1, 1, (ulong)representation), representation, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0, 0, 0), new Float3(1, 1, 0)), RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Float3>.TakeOwnership(normals), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2 }), RenderBuffer<Float2>.TakeOwnership(Array.Empty<Float2>()));
        }
    }
}
