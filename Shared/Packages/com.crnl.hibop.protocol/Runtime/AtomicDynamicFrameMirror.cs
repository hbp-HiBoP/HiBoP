using System;
using System.Threading;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.Protocol
{
    public enum DynamicFrameCommitResult : byte
    {
        Committed = 1,
        Stale = 2,
        WrongSession = 3,
        WrongTimeline = 4,
    }

    public sealed class AtomicDynamicFrameMirror
    {
        private readonly object m_Gate = new();
        private DynamicFrameBundle m_Current;
        private SessionEpoch m_Session;
        private ContractId m_TimelineId;
        private long m_Committed;
        private long m_Rejected;
        private long m_Stale;

        public AtomicDynamicFrameMirror(SessionEpoch session, ContractId timelineId)
        {
            Reset(session, timelineId);
        }

        public long CommittedCount => Interlocked.Read(ref m_Committed);
        public long RejectedCount => Interlocked.Read(ref m_Rejected);
        public long StaleCount => Interlocked.Read(ref m_Stale);

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

        public DynamicFrameCommitResult TryCommit(DynamicFrameBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            lock (m_Gate)
            {
                if (bundle.Session != m_Session)
                {
                    Interlocked.Increment(ref m_Rejected);
                    return DynamicFrameCommitResult.WrongSession;
                }

                if (bundle.TimelineId != m_TimelineId)
                {
                    Interlocked.Increment(ref m_Rejected);
                    return DynamicFrameCommitResult.WrongTimeline;
                }

                if (m_Current != null && (bundle.FrameSequence <= m_Current.FrameSequence || bundle.PlaybackRevision < m_Current.PlaybackRevision || bundle.SourceStateRevision < m_Current.SourceStateRevision))
                {
                    Interlocked.Increment(ref m_Stale);
                    return DynamicFrameCommitResult.Stale;
                }

                m_Current = bundle;
                Interlocked.Increment(ref m_Committed);
                return DynamicFrameCommitResult.Committed;
            }
        }

        public bool TryRead(out DynamicFrameBundle bundle)
        {
            lock (m_Gate)
            {
                bundle = m_Current;
                return bundle != null;
            }
        }
    }
}
