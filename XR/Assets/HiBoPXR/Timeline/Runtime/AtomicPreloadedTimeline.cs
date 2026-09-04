using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;

namespace CRNL.HiBoP.XR.Timeline
{
    public enum PreloadedTimelineApplyResult : byte
    {
        Ready = 1,
        Selected = 2,
        Stale = 3,
        Rejected = 4,
    }

    public readonly struct PreloadedTimelineSelection<TPrepared> where TPrepared : class
    {
        internal PreloadedTimelineSelection(PreloadedDynamicTimeline timeline, TPrepared prepared, int index, ScopeRevision playbackRevision, ulong commandSequence)
        {
            Timeline = timeline;
            Prepared = prepared;
            Index = index;
            PlaybackRevision = playbackRevision;
            CommandSequence = commandSequence;
        }

        public PreloadedDynamicTimeline Timeline { get; }
        public TPrepared Prepared { get; }
        public int Index { get; }
        public ScopeRevision PlaybackRevision { get; }
        public ulong CommandSequence { get; }
        public PreloadedTimelineIndex Metadata => Timeline.Indices[Index];
    }

    public sealed class AtomicPreloadedTimeline<TPrepared> where TPrepared : class
    {
        private readonly object m_Gate = new();
        private readonly Func<PreloadedDynamicTimeline, TPrepared> m_Prepare;
        private readonly Action<TPrepared> m_Release;
        private SessionEpoch m_Session;
        private ContractId m_TimelineId;
        private PreloadedDynamicTimeline m_Timeline;
        private TPrepared m_Prepared;
        private int m_Index;
        private ScopeRevision m_PlaybackRevision;
        private ulong m_CommandSequence;

        public AtomicPreloadedTimeline(SessionEpoch session, ContractId timelineId, Func<PreloadedDynamicTimeline, TPrepared> prepare, Action<TPrepared> release = null)
        {
            m_Prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
            m_Release = release;
            Reset(session, timelineId);
        }

        public void Reset(SessionEpoch session, ContractId timelineId)
        {
            if (!session.IsValid || !timelineId.IsValid)
                throw new ArgumentException("A valid session and timeline are required.");

            TPrepared previous;
            lock (m_Gate)
            {
                previous = m_Prepared;
                m_Session = session;
                m_TimelineId = timelineId;
                m_Timeline = null;
                m_Prepared = null;
                m_Index = 0;
                m_PlaybackRevision = default;
                m_CommandSequence = 0;
            }

            Release(previous);
        }

        public PreloadedTimelineApplyResult TryPrepareAndCommit(PreloadedDynamicTimeline timeline, int initialIndex, ScopeRevision playbackRevision, ulong commandSequence, out Exception error)
        {
            if (timeline == null)
            {
                error = new ArgumentNullException(nameof(timeline));
                return PreloadedTimelineApplyResult.Rejected;
            }

            if (initialIndex < 0 || initialIndex >= timeline.IndexCount || commandSequence == 0)
            {
                error = commandSequence == 0 ? new ArgumentOutOfRangeException(nameof(commandSequence)) : new ArgumentOutOfRangeException(nameof(initialIndex));
                return PreloadedTimelineApplyResult.Rejected;
            }

            lock (m_Gate)
            {
                if (timeline.Session != m_Session || timeline.TimelineId != m_TimelineId || IsStale(playbackRevision, commandSequence))
                {
                    error = null;
                    return timeline.Session == m_Session && timeline.TimelineId == m_TimelineId ? PreloadedTimelineApplyResult.Stale : PreloadedTimelineApplyResult.Rejected;
                }
            }

            TPrepared prepared;
            try
            {
                prepared = m_Prepare(timeline) ?? throw new InvalidOperationException("Timeline preparation returned null.");
            }
            catch (Exception exception)
            {
                error = exception;
                return PreloadedTimelineApplyResult.Rejected;
            }

            TPrepared previous = null;
            bool accepted;
            bool sameIdentity;
            lock (m_Gate)
            {
                sameIdentity = timeline.Session == m_Session && timeline.TimelineId == m_TimelineId;
                accepted = sameIdentity && !IsStale(playbackRevision, commandSequence);
                if (accepted)
                {
                    previous = m_Prepared;
                    m_Timeline = timeline;
                    m_Prepared = prepared;
                    m_Index = initialIndex;
                    m_PlaybackRevision = playbackRevision;
                    m_CommandSequence = commandSequence;
                }
            }

            if (!accepted)
            {
                Release(prepared);
                error = null;
                return sameIdentity ? PreloadedTimelineApplyResult.Stale : PreloadedTimelineApplyResult.Rejected;
            }

            Release(previous);
            error = null;
            return PreloadedTimelineApplyResult.Ready;
        }

        public PreloadedTimelineApplyResult TrySelect(int index, ScopeRevision playbackRevision, ulong commandSequence)
        {
            lock (m_Gate)
            {
                if (m_Timeline == null || index < 0 || index >= m_Timeline.IndexCount)
                    return PreloadedTimelineApplyResult.Rejected;
                if (IsStale(playbackRevision, commandSequence))
                    return PreloadedTimelineApplyResult.Stale;

                m_Index = index;
                m_PlaybackRevision = playbackRevision;
                m_CommandSequence = commandSequence;
                return PreloadedTimelineApplyResult.Selected;
            }
        }

        public bool TryRead(out PreloadedTimelineSelection<TPrepared> selection)
        {
            lock (m_Gate)
            {
                if (m_Timeline == null)
                {
                    selection = default;
                    return false;
                }

                selection = new PreloadedTimelineSelection<TPrepared>(m_Timeline, m_Prepared, m_Index, m_PlaybackRevision, m_CommandSequence);
                return true;
            }
        }

        private bool IsStale(ScopeRevision playbackRevision, ulong commandSequence)
        {
            return m_Timeline != null && (commandSequence <= m_CommandSequence || playbackRevision < m_PlaybackRevision);
        }

        private void Release(TPrepared prepared)
        {
            if (prepared != null)
                m_Release?.Invoke(prepared);
        }
    }
}
