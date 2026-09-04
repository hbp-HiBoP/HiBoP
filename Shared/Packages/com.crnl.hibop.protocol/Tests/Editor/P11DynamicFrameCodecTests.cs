using System;
using System.IO;
using System.Runtime.InteropServices;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class P11DynamicFrameCodecTests
    {
        [Test]
        public void WireStructLayouts_AreContiguousAndStable()
        {
            Assert.That(Marshal.SizeOf<Float3>(), Is.EqualTo(12));
            Assert.That(Marshal.SizeOf<Rgba32>(), Is.EqualTo(4));
        }

        [Test]
        public void Codec_RoundTripsCompleteFloat32BundleBitExactly()
        {
            DynamicFrameBundle source = Bundle(3, 7, 11);

            EncodedDynamicFrameBundle encoded = DynamicFrameBundleCodec.Encode(source);
            DynamicFrameBundle decoded = DynamicFrameBundleCodec.Decode(encoded.Descriptor, encoded.CopyPayload());

            Assert.That(encoded.Descriptor.Encoding, Is.EqualTo(DynamicFrameEncoding.Float32LittleEndian));
            Assert.That(decoded.FrameSequence, Is.EqualTo(11));
            Assert.That(decoded.ColumnFrames, Has.Count.EqualTo(3));
            for (int column = 0; column < decoded.ColumnFrames.Count; column++)
            {
                ColumnFrame expected = source.ColumnFrames[column];
                ColumnFrame actual = decoded.ColumnFrames[column];
                Assert.That(actual.Surface.Value.TemporalApplication, Is.EqualTo(TemporalApplication.SampleAndHold));
                Assert.That(actual.Sites.Value.TemporalApplication, Is.EqualTo(TemporalApplication.Linear));
                Assert.That(actual.CutOverlays[0].TemporalApplication, Is.EqualTo(TemporalApplication.SampleAndHold));
                for (int index = 0; index < expected.Surface.Value.VertexCount; index++)
                {
                    Assert.That(BitConverter.SingleToInt32Bits(actual.Surface.Value.ActivityValues[index]), Is.EqualTo(BitConverter.SingleToInt32Bits(expected.Surface.Value.ActivityValues[index])));
                    Assert.That(BitConverter.SingleToInt32Bits(actual.Surface.Value.OpacityValues[index]), Is.EqualTo(BitConverter.SingleToInt32Bits(expected.Surface.Value.OpacityValues[index])));
                    Assert.That(actual.Surface.Value.ActiveMask[index], Is.EqualTo(expected.Surface.Value.ActiveMask[index]));
                }

                for (int index = 0; index < expected.Sites.Value.SiteCount; index++)
                {
                    Assert.That(BitConverter.SingleToInt32Bits(actual.Sites.Value.Positions[index].X), Is.EqualTo(BitConverter.SingleToInt32Bits(expected.Sites.Value.Positions[index].X)));
                    Assert.That(BitConverter.SingleToInt32Bits(actual.Sites.Value.Positions[index].Y), Is.EqualTo(BitConverter.SingleToInt32Bits(expected.Sites.Value.Positions[index].Y)));
                    Assert.That(BitConverter.SingleToInt32Bits(actual.Sites.Value.Positions[index].Z), Is.EqualTo(BitConverter.SingleToInt32Bits(expected.Sites.Value.Positions[index].Z)));
                    Assert.That(actual.Sites.Value.Colors[index], Is.EqualTo(expected.Sites.Value.Colors[index]));
                    Assert.That(BitConverter.SingleToInt32Bits(actual.Sites.Value.Sizes[index]), Is.EqualTo(BitConverter.SingleToInt32Bits(expected.Sites.Value.Sizes[index])));
                    Assert.That(actual.Sites.Value.Visibility[index], Is.EqualTo(expected.Sites.Value.Visibility[index]));
                    Assert.That(actual.Sites.Value.Flags[index], Is.EqualTo(expected.Sites.Value.Flags[index]));
                }

                for (int index = 0; index < expected.CutOverlays[0].Pixels.Count; index++)
                    Assert.That(actual.CutOverlays[0].Pixels[index], Is.EqualTo(expected.CutOverlays[0].Pixels[index]));
            }
        }

        [Test]
        public void Codec_RejectsCorruptionBeforeReturningAnyBundle()
        {
            EncodedDynamicFrameBundle encoded = DynamicFrameBundleCodec.Encode(Bundle(1, 3, 1));
            byte[] payload = encoded.CopyPayload();
            payload[payload.Length - 1] ^= 0xff;

            Assert.Throws<InvalidDataException>(() => DynamicFrameBundleCodec.Decode(encoded.Descriptor, payload));
        }

        internal static DynamicFrameBundle Bundle(int columnCount, int siteCount, ulong sequence)
        {
            SessionEpoch session = new(Id(1), 1);
            ContractId timelineId = Id(2);
            var expectations = new DynamicColumnExpectation[columnCount];
            var frames = new ColumnFrame[columnCount];
            RenderTemporalSample sample = new(2, 0.75f);
            StateRevision stateRevision = new(5);
            for (int column = 0; column < columnCount; column++)
            {
                ContractId columnId = Id((ulong)(10 + column));
                ContractId cutId = Id((ulong)(100 + column));
                AssetHash surfaceHash = Hash((ulong)(20 + column));
                expectations[column] = new DynamicColumnExpectation(columnId, DynamicColumnContent.Surface | DynamicColumnContent.Sites, new[] { cutId });
                SurfaceFrame surface = Surface(surfaceHash, stateRevision, sample, column);
                SiteRenderFrame sites = Sites(Hash((ulong)(40 + column)), stateRevision, sample, siteCount, column);
                CutOverlayFrame overlay = new(cutId, columnId, stateRevision, 2, 2, sample, TemporalApplication.SampleAndHold, new ScopeRevision(3), RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32(1, 2, 3, 4), new Rgba32(5, 6, 7, 8), new Rgba32(9, 10, 11, 12), new Rgba32(13, 14, 15, 16) }));
                frames[column] = new ColumnFrame(columnId, surfaceHash, new ScopeRevision(4), Optional<SurfaceFrame>.Some(surface), Optional<SiteRenderFrame>.Some(sites), new[] { overlay });
            }

            return new DynamicFrameBundle(session, timelineId, new ScopeRevision(6), sequence, 1.25, sample, stateRevision, expectations, frames);
        }

        private static SurfaceFrame Surface(AssetHash hash, StateRevision revision, RenderTemporalSample sample, int seed)
        {
            float[] activity = { seed + 0.125f, seed + 0.25f, seed + 0.5f, seed + 0.75f };
            float[] opacity = { 1f, 0.75f, 0.5f, 0.25f };
            return new SurfaceFrame(hash, revision, sample, TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(activity), RenderBuffer<float>.TakeOwnership(opacity), RenderBuffer<byte>.TakeOwnership(new byte[] { 1, 1, 1, 1 }));
        }

        private static SiteRenderFrame Sites(AssetHash hash, StateRevision revision, RenderTemporalSample sample, int count, int seed)
        {
            Float3[] positions = new Float3[count];
            Rgba32[] colors = new Rgba32[count];
            float[] sizes = new float[count];
            byte[] visibility = new byte[count];
            SiteRenderFlags[] flags = new SiteRenderFlags[count];
            for (int index = 0; index < count; index++)
            {
                positions[index] = new Float3(index, seed, index + seed);
                colors[index] = new Rgba32((byte)index, 2, 3, 255);
                sizes[index] = index + 0.375f;
                visibility[index] = 1;
                flags[index] = SiteRenderFlags.None;
            }

            return new SiteRenderFrame(hash, revision, sample, TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Rgba32>.TakeOwnership(colors), RenderBuffer<float>.TakeOwnership(sizes), RenderBuffer<byte>.TakeOwnership(visibility), RenderBuffer<SiteRenderFlags>.TakeOwnership(flags));
        }

        internal static ContractId Id(ulong value) => new(value, value + 1);
        internal static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
