using System;
using System.IO;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class P11PreloadedTimelineTests
    {
        private const long TestBudgetBytes = 64 * 1024 * 1024;

        [Test]
        public void Codec_RoundTripsNinetySevenIndicesLosslesslyAndDeduplicatesInvariantSlices()
        {
            PreloadedDynamicTimeline source = BuildTimeline(97);

            Assert.That(source.IndexCount, Is.EqualTo(97));
            Assert.That(source.Columns[0].SurfaceActivity.UniqueSliceCount, Is.EqualTo(97));
            Assert.That(source.Columns[0].SurfaceOpacity.UniqueSliceCount, Is.EqualTo(1));
            Assert.That(source.Columns[0].SitePositions.UniqueSliceCount, Is.EqualTo(1));
            Assert.That(source.UniquePayloadBytes, Is.LessThan(source.NaivePayloadBytes));

            using var stream = new MemoryStream();
            PreloadedTimelineDescriptor descriptor = PreloadedDynamicTimelineCodec.Write(stream, source);
            stream.Position = 0;
            PreloadedDynamicTimeline decoded = PreloadedDynamicTimelineCodec.Read(stream, descriptor, TestBudgetBytes);

            Assert.That(decoded.IndexCount, Is.EqualTo(97));
            Assert.That(decoded.Columns, Has.Count.EqualTo(1));
            foreach (int index in new[] { 0, 48, 96 })
            {
                AssertFloatBits(decoded.Columns[0].SurfaceActivity.GetSlice(index)[0], index + 0.125f);
                Assert.That(decoded.Columns[0].SiteColors.GetSlice(index)[0], Is.EqualTo(new Rgba32((byte)index, 64, 192, 255)));
                Assert.That(decoded.Columns[0].Cuts[0].Pixels.GetSlice(index)[0], Is.EqualTo(new Rgba32((byte)index, 32, 224, 255)));
                AssertFloatBits((float)decoded.Indices[index].LogicalTime, index / 10f);
            }
        }

        [Test]
        public void Codec_RejectsCorruptionBeforeReturningATimeline()
        {
            PreloadedDynamicTimeline timeline = BuildTimeline(3);
            using var destination = new MemoryStream();
            PreloadedTimelineDescriptor descriptor = PreloadedDynamicTimelineCodec.Write(destination, timeline);
            byte[] payload = destination.ToArray();
            payload[^1] ^= 0xff;
            using var corrupted = new MemoryStream(payload, false);

            Assert.Throws<InvalidDataException>(() => PreloadedDynamicTimelineCodec.Read(corrupted, descriptor, TestBudgetBytes));
        }

        [Test]
        public void Builder_RejectsMemoryBudgetOverflowWithoutPublishingAPartialArchive()
        {
            var builder = new PreloadedDynamicTimelineBuilder(1);

            Assert.Throws<NotSupportedException>(() => builder.AddFrame(CreateFrame(0)));
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        private static PreloadedDynamicTimeline BuildTimeline(int indexCount)
        {
            var builder = new PreloadedDynamicTimelineBuilder(TestBudgetBytes);
            for (int index = 0; index < indexCount; index++)
                builder.AddFrame(CreateFrame(index));
            return builder.Build();
        }

        private static DynamicFrameBundle CreateFrame(int timelineIndex)
        {
            SessionEpoch session = new(Id(1), 1);
            ContractId timelineId = Id(2);
            ContractId columnId = Id(10);
            ContractId cutId = Id(100);
            AssetHash surfaceHash = Hash(20);
            StateRevision stateRevision = new(5);
            RenderTemporalSample sample = new(timelineIndex, 0.25f);
            var expectation = new DynamicColumnExpectation(columnId, DynamicColumnContent.Surface | DynamicColumnContent.Sites, new[] { cutId });
            var surface = new SurfaceFrame(surfaceHash, stateRevision, sample, TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(new[] { timelineIndex + 0.125f, timelineIndex + 0.25f, timelineIndex + 0.5f, timelineIndex + 0.75f }), RenderBuffer<float>.TakeOwnership(new[] { 1f, 0.75f, 0.5f, 0.25f }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1, 1, 1, 1 }));
            var sites = new SiteRenderFrame(Hash(40), stateRevision, sample, TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0, 1, 2), new Float3(3, 4, 5), new Float3(6, 7, 8) }), RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32((byte)timelineIndex, 64, 192, 255), new Rgba32(1, 2, 3, 255), new Rgba32(4, 5, 6, 255) }), RenderBuffer<float>.TakeOwnership(new[] { 1f, 2f, 3f }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1, 1, 1 }), RenderBuffer<SiteRenderFlags>.TakeOwnership(new[] { SiteRenderFlags.None, SiteRenderFlags.None, SiteRenderFlags.None }));
            var overlay = new CutOverlayFrame(cutId, columnId, stateRevision, 2, 2, sample, TemporalApplication.SampleAndHold, new ScopeRevision(3), RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32((byte)timelineIndex, 32, 224, 255), new Rgba32(1, 2, 3, 4), new Rgba32(5, 6, 7, 8), new Rgba32(9, 10, 11, 12) }));
            var column = new ColumnFrame(columnId, surfaceHash, new ScopeRevision(4), Optional<SurfaceFrame>.Some(surface), Optional<SiteRenderFrame>.Some(sites), new[] { overlay });
            return new DynamicFrameBundle(session, timelineId, new ScopeRevision((ulong)timelineIndex + 1), (ulong)timelineIndex + 1, timelineIndex / 10f, sample, stateRevision, new[] { expectation }, new[] { column });
        }

        private static void AssertFloatBits(float actual, float expected)
        {
            Assert.That(BitConverter.SingleToInt32Bits(actual), Is.EqualTo(BitConverter.SingleToInt32Bits(expected)));
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
