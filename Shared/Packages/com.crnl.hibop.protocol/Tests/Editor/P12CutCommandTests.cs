using System.IO;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class P12CutCommandTests
    {
        [Test]
        public void SetCut_RoundTripsNormalizedDesktopOwnedPlane()
        {
            SessionEpoch session = new(Id(1), 1);
            var scope = new ScopeKey(ScopeType.Cut, Id(2));
            var intent = new CutPlaneIntent(new Plane3F(new Float3(0f, 0f, 2f), -40f));

            Command command = CutCommands.Create(session, Id(3), Id(4), scope, new ScopeRevision(5), intent, Id(6), new InteractionSequence(7));

            Assert.That(CutCommands.TryRead(command, out CutPlaneIntent decoded), Is.True);
            Assert.That(decoded.Plane.Normal, Is.EqualTo(new Float3(0f, 0f, 1f)));
            Assert.That(decoded.Plane.Distance, Is.EqualTo(-20f));
            Assert.That(command.Scope.Owner, Is.EqualTo(ScopeOwner.Desktop));
            Assert.That(command.InteractionId.Value, Is.EqualTo(Id(6)));
            Assert.That(command.Sequence.Value, Is.EqualTo(new InteractionSequence(7)));
        }

        [Test]
        public void CutResultCodec_RoundTripsExactDesktopResult()
        {
            SessionEpoch session = new(Id(1), 2);
            ContractId cutId = Id(2);
            ContractId columnId = Id(3);
            CutRenderResult result = Result(cutId, columnId);
            CutResultManifest manifest = new(new[] { columnId }, 2, 2);

            EncodedCutResult encoded = CutResultCodec.Encode(session, result, manifest);
            DecodedCutResult decoded = CutResultCodec.Decode(encoded.Descriptor, encoded.Payload, encoded.Payload.Length);

            Assert.That(decoded.Session, Is.EqualTo(session));
            Assert.That(decoded.Result.CutId, Is.EqualTo(result.CutId));
            Assert.That(decoded.Result.InteractionId, Is.EqualTo(result.InteractionId));
            Assert.That(decoded.Result.Sequence, Is.EqualTo(result.Sequence));
            AssertBufferEqual(decoded.Result.Geometry.Value.Positions, result.Geometry.Value.Positions);
            AssertBufferEqual(decoded.Result.BaseTexture.Value.Pixels, result.BaseTexture.Value.Pixels);
            AssertBufferEqual(decoded.Result.Overlays[0].Pixels, result.Overlays[0].Pixels);
        }

        [Test]
        public void CutResultCodec_RejectsCorruptionAndPayloadsAboveBudget()
        {
            SessionEpoch session = new(Id(1), 2);
            ContractId columnId = Id(3);
            EncodedCutResult encoded = CutResultCodec.Encode(session, Result(Id(2), columnId), new CutResultManifest(new[] { columnId }, 2, 2));
            byte[] corrupted = (byte[])encoded.Payload.Clone();
            corrupted[corrupted.Length - 1] ^= 0xff;

            Assert.Throws<InvalidDataException>(() => CutResultCodec.Decode(encoded.Descriptor, corrupted, corrupted.Length));
            Assert.Throws<InvalidDataException>(() => CutResultCodec.Decode(encoded.Descriptor, encoded.Payload, encoded.Payload.Length - 1));
        }

        private static CutRenderResult Result(ContractId cutId, ContractId columnId)
        {
            AssetHash geometryHash = Hash(10);
            AssetHash textureHash = Hash(20);
            RenderBuffer<Rgba32> pixels = Pixels(9);
            var geometry = new CutGeometryAsset(geometryHash, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0, 0, 0), new Float3(1, 1, 0)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 0), new Float3(1, 0, 0), new Float3(0, 1, 0) }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 0, 1), new Float3(0, 0, 1), new Float3(0, 0, 1) }), RenderBuffer<Float2>.TakeOwnership(new[] { new Float2(0, 0), new Float2(1, 0), new Float2(0, 1) }), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2 }));
            var texture = new TextureAsset(textureHash, 2, 2, TextureColorSpace.Srgb, pixels);
            var overlay = new CutOverlayFrame(cutId, columnId, new StateRevision(4), 2, 2, new RenderTemporalSample(1, 0.25f), TemporalApplication.SampleAndHold, new ScopeRevision(7), Pixels(5));
            return new CutRenderResult(cutId, Id(4), new InteractionSequence(3), new ScopeRevision(8), new ScopeRevision(9), new StateRevision(4), new RenderTemporalSample(1, 0.25f), new Plane3F(new Float3(0, 0, 1), -2), geometryHash, Optional<CutGeometryAsset>.Some(geometry), textureHash, Optional<TextureAsset>.Some(texture), new[] { overlay });
        }

        private static RenderBuffer<Rgba32> Pixels(byte value) => RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32(value, 0, 0, 255), new Rgba32(0, value, 0, 255), new Rgba32(0, 0, value, 255), new Rgba32(value, value, value, 255) });

        private static void AssertBufferEqual<T>(RenderBuffer<T> actual, RenderBuffer<T> expected) where T : struct
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int index = 0; index < actual.Count; index++)
                Assert.That(actual[index], Is.EqualTo(expected[index]));
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
