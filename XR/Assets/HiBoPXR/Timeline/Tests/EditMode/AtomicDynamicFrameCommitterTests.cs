using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using NUnit.Framework;

namespace CRNL.HiBoP.XR.Timeline.Tests
{
    public class AtomicDynamicFrameCommitterTests
    {
        [Test]
        public void PrepareFailure_LeavesPreviouslyCommittedBundleVisible()
        {
            DynamicFrameBundle first = Bundle(1);
            bool fail = false;
            var committer = new AtomicDynamicFrameCommitter<int[]>(first.Session, first.TimelineId, bundle =>
            {
                if (fail)
                    throw new InvalidOperationException("synthetic column failure");
                return new[] { bundle.ColumnFrames.Count, checked((int)bundle.FrameSequence) };
            });

            Assert.That(Apply(committer, first), Is.EqualTo(DynamicFrameApplyResult.Committed));
            fail = true;
            Assert.That(Apply(committer, Bundle(2)), Is.EqualTo(DynamicFrameApplyResult.Rejected));
            Assert.That(committer.TryRead(out CommittedDynamicFrame<int[]> current), Is.True);
            Assert.That(current.Bundle.FrameSequence, Is.EqualTo(1));
            Assert.That(current.Prepared, Is.EqualTo(new[] { 3, 1 }));
        }

        [Test]
        public void DelayedBundle_IsRejectedBeforePrepare()
        {
            DynamicFrameBundle latest = Bundle(3);
            int prepares = 0;
            var committer = new AtomicDynamicFrameCommitter<int>(latest.Session, latest.TimelineId, bundle =>
            {
                prepares++;
                return bundle.ColumnFrames.Count;
            });

            Assert.That(Apply(committer, latest), Is.EqualTo(DynamicFrameApplyResult.Committed));
            Assert.That(Apply(committer, Bundle(2)), Is.EqualTo(DynamicFrameApplyResult.Stale));
            Assert.That(prepares, Is.EqualTo(1));
        }

        private static DynamicFrameApplyResult Apply(AtomicDynamicFrameCommitter<int[]> committer, DynamicFrameBundle bundle)
        {
            EncodedDynamicFrameBundle encoded = DynamicFrameBundleCodec.Encode(bundle);
            return committer.TryDecodePrepareAndCommit(encoded.Descriptor, encoded.CopyPayload(), out _, out _);
        }

        private static DynamicFrameApplyResult Apply(AtomicDynamicFrameCommitter<int> committer, DynamicFrameBundle bundle)
        {
            EncodedDynamicFrameBundle encoded = DynamicFrameBundleCodec.Encode(bundle);
            return committer.TryDecodePrepareAndCommit(encoded.Descriptor, encoded.CopyPayload(), out _, out _);
        }

        private static DynamicFrameBundle Bundle(ulong sequence)
        {
            SessionEpoch session = new(Id(1), 1);
            ContractId timelineId = Id(2);
            var expectations = new DynamicColumnExpectation[3];
            var frames = new ColumnFrame[3];
            for (int index = 0; index < 3; index++)
            {
                ContractId columnId = Id((ulong)(10 + index));
                AssetHash hash = Hash((ulong)(20 + index));
                expectations[index] = new DynamicColumnExpectation(columnId, DynamicColumnContent.None, Array.Empty<ContractId>());
                frames[index] = new ColumnFrame(columnId, hash, new ScopeRevision(1), Optional<SurfaceFrame>.None, Optional<SiteRenderFrame>.None, Array.Empty<CutOverlayFrame>());
            }

            return new DynamicFrameBundle(session, timelineId, new ScopeRevision(1), sequence, sequence, new RenderTemporalSample((int)sequence, 0f), new StateRevision(1), expectations, frames);
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
        private static AssetHash Hash(ulong value) => new(value, value + 1, value + 2, value + 3);
    }
}
