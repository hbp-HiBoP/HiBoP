using System;
using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.RenderModel.Tests
{
    public class RenderModelValidationTests
    {
        [Test]
        public void DesktopUnityCoordinateSpace_IsExplicitAndCanonical()
        {
            CoordinateSpace space = CoordinateSpace.DesktopUnityMillimetersV1;

            Assert.That(space.Handedness, Is.EqualTo(CoordinateHandedness.Left));
            Assert.That(space.AxisOrder, Is.EqualTo(CoordinateAxisOrder.Xyz));
            Assert.That(space.Unit, Is.EqualTo(LengthUnit.Millimeter));
            Assert.That(space.MetersPerUnit, Is.EqualTo(0.001f));
            Assert.That(space.AssetToBrain, Is.EqualTo(Matrix4x4F.Identity));
            Assert.That(space.MappingVersion, Is.EqualTo(1));
        }

        [Test]
        public void SurfaceAsset_RejectsOutOfRangeTriangleIndices()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SurfaceAsset(Hash(1), SurfaceRepresentation.Anatomical, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0, 0, 0), new Float3(1, 1, 0)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 0), new Float3(1, 0, 0), new Float3(0, 1, 0) }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 1), new Float3(0, 0, 1), new Float3(0, 0, 1) }), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 3 }), RenderBuffer<Float2>.TakeOwnership(Array.Empty<Float2>())));
        }

        [Test]
        public void SiteAsset_RejectsDuplicateOpaqueIds()
        {
            ContractId id = Id(1);
            Assert.Throws<ArgumentException>(() => new SiteAsset(Hash(2), CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0, 0, 0), new Float3(1, 0, 0)), RenderBuffer<ContractId>.TakeOwnership(new[] { id, id }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 0), new Float3(1, 0, 0) })));
        }

        internal static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
        internal static ContractId Id(ulong value) => new(value, value + 1);
    }
}
