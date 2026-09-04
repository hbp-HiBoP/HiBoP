using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;

namespace CRNL.HiBoP.XR.Timeline.Rendering
{
    /// <summary>
    /// Main-thread controller that publishes a fully prepared GPU timeline once, then changes
    /// timeline index by updating only the four-byte selection buffer.
    /// </summary>
    public sealed class PreloadedTimelineGpuController : IDisposable
    {
        private readonly object m_Gate = new();
        private readonly AtomicPreloadedTimeline<PreloadedTimelineGpuResources> m_Atomic;
        private readonly long m_MaximumGpuBytes;
        private int m_PrepareIndex;
        private bool m_Disposed;

        public PreloadedTimelineGpuController(SessionEpoch session, ContractId timelineId, long maximumGpuBytes)
        {
            if (maximumGpuBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumGpuBytes));
            m_MaximumGpuBytes = maximumGpuBytes;
            m_Atomic = new AtomicPreloadedTimeline<PreloadedTimelineGpuResources>(session, timelineId, Prepare, prepared => prepared.Dispose());
        }

        public PreloadedTimelineApplyResult TryPrepareAndCommit(PreloadedDynamicTimeline timeline, int initialIndex, ScopeRevision playbackRevision, ulong commandSequence, out Exception error)
        {
            lock (m_Gate)
            {
                ThrowIfDisposed();
                m_PrepareIndex = initialIndex;
                return m_Atomic.TryPrepareAndCommit(timeline, initialIndex, playbackRevision, commandSequence, out error);
            }
        }

        public PreloadedTimelineApplyResult TrySelect(int index, ScopeRevision playbackRevision, ulong commandSequence, out Exception error)
        {
            lock (m_Gate)
            {
                ThrowIfDisposed();
                if (!m_Atomic.TryRead(out PreloadedTimelineSelection<PreloadedTimelineGpuResources> current) || index < 0 || index >= current.Timeline.IndexCount)
                {
                    error = new ArgumentOutOfRangeException(nameof(index));
                    return PreloadedTimelineApplyResult.Rejected;
                }

                if (commandSequence <= current.CommandSequence || playbackRevision < current.PlaybackRevision)
                {
                    error = null;
                    return PreloadedTimelineApplyResult.Stale;
                }

                try
                {
                    current.Prepared.SelectIndex(index);
                }
                catch (Exception exception)
                {
                    error = exception;
                    return PreloadedTimelineApplyResult.Rejected;
                }

                PreloadedTimelineApplyResult result = m_Atomic.TrySelect(index, playbackRevision, commandSequence);
                if (result != PreloadedTimelineApplyResult.Selected)
                    throw new InvalidOperationException("The serialized GPU timeline selector changed unexpectedly.");
                error = null;
                return result;
            }
        }

        public bool TryRead(out PreloadedTimelineSelection<PreloadedTimelineGpuResources> selection)
        {
            lock (m_Gate)
            {
                ThrowIfDisposed();
                return m_Atomic.TryRead(out selection);
            }
        }

        public void Dispose()
        {
            lock (m_Gate)
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                if (m_Atomic.TryRead(out PreloadedTimelineSelection<PreloadedTimelineGpuResources> current))
                    current.Prepared.Dispose();
            }
        }

        private PreloadedTimelineGpuResources Prepare(PreloadedDynamicTimeline timeline)
        {
            var prepared = new PreloadedTimelineGpuResources(timeline, m_MaximumGpuBytes);
            try
            {
                if (m_PrepareIndex != 0)
                    prepared.SelectIndex(m_PrepareIndex);
                return prepared;
            }
            catch
            {
                prepared.Dispose();
                throw;
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(PreloadedTimelineGpuController));
        }
    }
}
