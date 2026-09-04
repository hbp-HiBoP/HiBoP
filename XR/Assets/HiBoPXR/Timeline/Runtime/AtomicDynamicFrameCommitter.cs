using System;
using System.Diagnostics;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.XR.Timeline
{
    public enum DynamicFrameApplyResult : byte
    {
        Committed = 1,
        Stale = 2,
        Rejected = 3,
    }

    public readonly struct DynamicFramePipelineTiming
    {
        public DynamicFramePipelineTiming(double decodeMilliseconds, double prepareMilliseconds, double commitMilliseconds)
        {
            DecodeMilliseconds = decodeMilliseconds;
            PrepareMilliseconds = prepareMilliseconds;
            CommitMilliseconds = commitMilliseconds;
        }

        public double DecodeMilliseconds { get; }
        public double PrepareMilliseconds { get; }
        public double CommitMilliseconds { get; }
        public double TotalMilliseconds => DecodeMilliseconds + PrepareMilliseconds + CommitMilliseconds;
    }

    public sealed class CommittedDynamicFrame<TPrepared>
    {
        internal CommittedDynamicFrame(DynamicFrameBundle bundle, TPrepared prepared)
        {
            Bundle = bundle;
            Prepared = prepared;
        }

        public DynamicFrameBundle Bundle { get; }
        public TPrepared Prepared { get; }
    }

    public sealed class AtomicDynamicFrameCommitter<TPrepared>
    {
        private readonly object m_Gate = new();
        private readonly Func<DynamicFrameBundle, TPrepared> m_Prepare;
        private SessionEpoch m_Session;
        private ContractId m_TimelineId;
        private CommittedDynamicFrame<TPrepared> m_Current;

        public AtomicDynamicFrameCommitter(SessionEpoch session, ContractId timelineId, Func<DynamicFrameBundle, TPrepared> prepare)
        {
            m_Prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
            Reset(session, timelineId);
        }

        public void Reset(SessionEpoch session, ContractId timelineId)
        {
            if (!session.IsValid || !timelineId.IsValid)
                throw new ArgumentException("A valid session and timeline are required.");
            lock (m_Gate)
            {
                m_Session = session;
                m_TimelineId = timelineId;
                m_Current = null;
            }
        }

        public DynamicFrameApplyResult TryDecodePrepareAndCommit(DynamicFrameDescriptor descriptor, byte[] payload, out DynamicFramePipelineTiming timing, out Exception error)
        {
            var watch = Stopwatch.StartNew();
            DynamicFrameBundle bundle;
            try
            {
                bundle = DynamicFrameBundleCodec.Decode(descriptor, payload);
            }
            catch (Exception exception)
            {
                timing = new DynamicFramePipelineTiming(watch.Elapsed.TotalMilliseconds, 0d, 0d);
                error = exception;
                return DynamicFrameApplyResult.Rejected;
            }

            double decodedAt = watch.Elapsed.TotalMilliseconds;
            lock (m_Gate)
            {
                if (bundle.Session != m_Session || bundle.TimelineId != m_TimelineId)
                {
                    timing = new DynamicFramePipelineTiming(decodedAt, 0d, 0d);
                    error = null;
                    return DynamicFrameApplyResult.Rejected;
                }

                if (IsStale(bundle))
                {
                    timing = new DynamicFramePipelineTiming(decodedAt, 0d, 0d);
                    error = null;
                    return DynamicFrameApplyResult.Stale;
                }
            }

            TPrepared prepared;
            try
            {
                prepared = m_Prepare(bundle);
            }
            catch (Exception exception)
            {
                timing = new DynamicFramePipelineTiming(decodedAt, watch.Elapsed.TotalMilliseconds - decodedAt, 0d);
                error = exception;
                return DynamicFrameApplyResult.Rejected;
            }

            double preparedAt = watch.Elapsed.TotalMilliseconds;
            lock (m_Gate)
            {
                if (bundle.Session != m_Session || bundle.TimelineId != m_TimelineId)
                {
                    timing = new DynamicFramePipelineTiming(decodedAt, preparedAt - decodedAt, watch.Elapsed.TotalMilliseconds - preparedAt);
                    error = null;
                    return DynamicFrameApplyResult.Rejected;
                }

                if (IsStale(bundle))
                {
                    timing = new DynamicFramePipelineTiming(decodedAt, preparedAt - decodedAt, watch.Elapsed.TotalMilliseconds - preparedAt);
                    error = null;
                    return DynamicFrameApplyResult.Stale;
                }

                m_Current = new CommittedDynamicFrame<TPrepared>(bundle, prepared);
                double committedAt = watch.Elapsed.TotalMilliseconds;
                timing = new DynamicFramePipelineTiming(decodedAt, preparedAt - decodedAt, committedAt - preparedAt);
                error = null;
                return DynamicFrameApplyResult.Committed;
            }
        }

        public bool TryRead(out CommittedDynamicFrame<TPrepared> frame)
        {
            lock (m_Gate)
            {
                frame = m_Current;
                return frame != null;
            }
        }

        private bool IsStale(DynamicFrameBundle bundle)
        {
            return m_Current != null && (bundle.FrameSequence <= m_Current.Bundle.FrameSequence || bundle.PlaybackRevision < m_Current.Bundle.PlaybackRevision || bundle.SourceStateRevision < m_Current.Bundle.SourceStateRevision);
        }
    }
}
