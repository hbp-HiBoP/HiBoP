using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.XR.Timeline.Tests
{
    public class AtomicPreloadedTimelineTests
    {
        [Test]
        public void RandomReverseSelection_ReusesPreparedTimelineAndUsesCommandOrder()
        {
            PreloadedDynamicTimeline timeline = BuildTimeline();
            int prepareCount = 0;
            var atomic = new AtomicPreloadedTimeline<object>(timeline.Session, timeline.TimelineId, _ =>
            {
                prepareCount++;
                return new object();
            });

            Assert.That(atomic.TryPrepareAndCommit(timeline, 0, new ScopeRevision(1), 1, out Exception error), Is.EqualTo(PreloadedTimelineApplyResult.Ready), error?.ToString());
            Assert.That(atomic.TrySelect(96, new ScopeRevision(2), 2), Is.EqualTo(PreloadedTimelineApplyResult.Selected));
            Assert.That(atomic.TrySelect(2, new ScopeRevision(3), 3), Is.EqualTo(PreloadedTimelineApplyResult.Selected));
            Assert.That(atomic.TrySelect(80, new ScopeRevision(4), 3), Is.EqualTo(PreloadedTimelineApplyResult.Stale));

            Assert.That(prepareCount, Is.EqualTo(1));
            Assert.That(atomic.TryRead(out PreloadedTimelineSelection<object> selection), Is.True);
            Assert.That(selection.Index, Is.EqualTo(2));
            Assert.That(selection.CommandSequence, Is.EqualTo(3));
        }

        private static PreloadedDynamicTimeline BuildTimeline()
        {
            SessionEpoch session = new(Id(1), 1);
            ContractId timelineId = Id(2);
            ContractId columnId = Id(10);
            AssetHash surfaceHash = Hash(20);
            StateRevision stateRevision = new(5);
            var expectation = new DynamicColumnExpectation(columnId, DynamicColumnContent.Surface, Array.Empty<ContractId>());
            var builder = new PreloadedDynamicTimelineBuilder(1024 * 1024);
            for (int index = 0; index < 97; index++)
            {
                RenderTemporalSample sample = new(index, 0f);
                var surface = new SurfaceFrame(surfaceHash, stateRevision, sample, TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(new[] { (float)index }), RenderBuffer<float>.TakeOwnership(new[] { 1f }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1 }));
                var column = new ColumnFrame(columnId, surfaceHash, new ScopeRevision(4), Optional<SurfaceFrame>.Some(surface), Optional<SiteRenderFrame>.None, Array.Empty<CutOverlayFrame>());
                builder.AddFrame(new DynamicFrameBundle(session, timelineId, new ScopeRevision((ulong)index + 1), (ulong)index + 1, index, sample, stateRevision, new[] { expectation }, new[] { column }));
            }

            return builder.Build();
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
