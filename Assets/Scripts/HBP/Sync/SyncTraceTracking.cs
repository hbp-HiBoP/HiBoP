using System;

namespace HBP.Sync
{
    /// <summary>Stores the first raw setter point for the active capture generation.</summary>
    public struct SyncSetterOrigin
    {
        private SyncTelemetryPoint m_Point;

        public void CaptureFirst()
        {
            long generation = SyncTelemetry.ActiveCaptureGeneration;
            if (generation == 0)
            {
                m_Point = default;
                return;
            }

            if (!m_Point.IsValid || m_Point.ContextGeneration != generation)
                m_Point = SyncTelemetry.CapturePoint();
        }

        public bool TryTake(out SyncTelemetryPoint point)
        {
            point = m_Point;
            m_Point = default;
            return point.IsValid;
        }

        public void Clear() => m_Point = default;
    }

    public readonly struct SyncLogicalTrace
    {
        public SyncProfile Profile { get; }
        public long LogicalTraceId { get; }
        public long CaptureGeneration { get; }
        public SyncTelemetryPoint Setter { get; }
        public SyncTelemetryPoint CaptureStart { get; }
        public SyncTelemetryPoint CaptureEnd { get; }
        public SyncTelemetryPoint Queued { get; }
        public bool IsValid => Setter.IsValid && Queued.IsValid;

        internal SyncLogicalTrace(SyncProfile profile, long logicalTraceId, long captureGeneration, SyncTelemetryPoint setter, SyncTelemetryPoint captureStart, SyncTelemetryPoint captureEnd, SyncTelemetryPoint queued)
        {
            Profile = profile;
            LogicalTraceId = logicalTraceId;
            CaptureGeneration = captureGeneration;
            Setter = setter;
            CaptureStart = captureStart;
            CaptureEnd = captureEnd;
            Queued = queued;
        }

        public SyncTelemetryIdentity Identity(string scopeId, long attemptId = 0) => new(scopeId, LogicalTraceId, CaptureGeneration, attemptId);
    }

    /// <summary>A fixed-capacity batch for the three T00 live profiles.</summary>
    public readonly struct SyncTraceBatch
    {
        private readonly SyncLogicalTrace m_First;
        private readonly SyncLogicalTrace m_Second;
        private readonly SyncLogicalTrace m_Third;

        public int Count { get; }
        public bool IsEmpty => Count == 0;

        internal SyncTraceBatch(SyncLogicalTrace first, SyncLogicalTrace second = default, SyncLogicalTrace third = default)
        {
            m_First = first;
            m_Second = second;
            m_Third = third;
            Count = third.IsValid ? 3 : second.IsValid ? 2 : first.IsValid ? 1 : 0;
        }

        internal static SyncTraceBatch FromSlots(SyncLogicalTrace first, SyncLogicalTrace second, SyncLogicalTrace third)
        {
            SyncLogicalTrace resultFirst = default;
            SyncLogicalTrace resultSecond = default;
            SyncLogicalTrace resultThird = default;
            int count = 0;
            Add(first, ref resultFirst, ref resultSecond, ref resultThird, ref count);
            Add(second, ref resultFirst, ref resultSecond, ref resultThird, ref count);
            Add(third, ref resultFirst, ref resultSecond, ref resultThird, ref count);
            return new SyncTraceBatch(resultFirst, resultSecond, resultThird);
        }

        public SyncLogicalTrace this[int index] => index switch
        {
            0 when Count > 0 => m_First,
            1 when Count > 1 => m_Second,
            2 when Count > 2 => m_Third,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        private static void Add(SyncLogicalTrace trace, ref SyncLogicalTrace first, ref SyncLogicalTrace second, ref SyncLogicalTrace third, ref int count)
        {
            if (!trace.IsValid) return;
            switch (count++)
            {
                case 0:
                    first = trace;
                    break;
                case 1:
                    second = trace;
                    break;
                case 2:
                    third = trace;
                    break;
                default:
                    throw new InvalidOperationException("A T00 trace batch cannot exceed three profiles.");
            }
        }
    }

    public readonly struct SyncTraceCapture
    {
        public SyncTraceBatch Captured { get; }
        public SyncTraceBatch Replaced { get; }

        internal SyncTraceCapture(SyncTraceBatch captured, SyncTraceBatch replaced)
        {
            Captured = captured;
            Replaced = replaced;
        }
    }

    /// <summary>Three bounded live-profile slots: first dirty setter, one pending capture per profile.</summary>
    public sealed class SyncTraceTracker
    {
        private readonly DirtyTrace[] m_Dirty = new DirtyTrace[3];
        private readonly SyncLogicalTrace[] m_Pending = new SyncLogicalTrace[3];

        public void Begin(SyncProfile profile, long logicalTraceId, SyncTelemetryPoint setter)
        {
            int index = Index(profile);
            if (!setter.IsValid || m_Dirty[index].Setter.IsValid)
                return;
            m_Dirty[index] = new DirtyTrace(logicalTraceId, setter);
        }

        public SyncTraceCapture Capture(long generation, SyncTelemetryPoint captureStart, SyncTelemetryPoint captureEnd, SyncTelemetryPoint queued)
        {
            SyncLogicalTrace capturedFirst = default;
            SyncLogicalTrace capturedSecond = default;
            SyncLogicalTrace capturedThird = default;
            SyncLogicalTrace replacedFirst = default;
            SyncLogicalTrace replacedSecond = default;
            SyncLogicalTrace replacedThird = default;
            int capturedCount = 0;
            int replacedCount = 0;
            for (int i = 0; i < m_Pending.Length; i++)
            {
                DirtyTrace dirty = m_Dirty[i];
                if (!dirty.Setter.IsValid) continue;
                if (m_Pending[i].IsValid)
                    Add(m_Pending[i], ref replacedFirst, ref replacedSecond, ref replacedThird, ref replacedCount);
                SyncLogicalTrace current = new(Profile(i), dirty.LogicalTraceId, generation, dirty.Setter, captureStart, captureEnd, queued);
                m_Pending[i] = current;
                m_Dirty[i] = default;
                Add(current, ref capturedFirst, ref capturedSecond, ref capturedThird, ref capturedCount);
            }

            return new SyncTraceCapture(new SyncTraceBatch(capturedFirst, capturedSecond, capturedThird), new SyncTraceBatch(replacedFirst, replacedSecond, replacedThird));
        }

        public SyncTraceBatch SnapshotPending() => SyncTraceBatch.FromSlots(m_Pending[0], m_Pending[1], m_Pending[2]);

        public void ClearPending(SyncTraceBatch accepted)
        {
            for (int i = 0; i < accepted.Count; i++)
            {
                SyncLogicalTrace trace = accepted[i];
                int index = Index(trace.Profile);
                if (m_Pending[index].LogicalTraceId == trace.LogicalTraceId && m_Pending[index].CaptureGeneration == trace.CaptureGeneration)
                    m_Pending[index] = default;
            }
        }

        public void DiscardDirty()
        {
            Array.Clear(m_Dirty, 0, m_Dirty.Length);
        }

        private static void Add(SyncLogicalTrace trace, ref SyncLogicalTrace first, ref SyncLogicalTrace second, ref SyncLogicalTrace third, ref int count)
        {
            switch (count++)
            {
                case 0:
                    first = trace;
                    break;
                case 1:
                    second = trace;
                    break;
                case 2:
                    third = trace;
                    break;
                default:
                    throw new InvalidOperationException("A T00 trace batch cannot exceed three profiles.");
            }
        }

        private static int Index(SyncProfile profile) =>
            profile switch
            {
                SyncProfile.SiteColor => 0,
                SyncProfile.CutDefinition => 1,
                SyncProfile.TimelineAnchor => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(profile))
            };

        private static SyncProfile Profile(int index) =>
            index switch
            {
                0 => SyncProfile.SiteColor,
                1 => SyncProfile.CutDefinition,
                2 => SyncProfile.TimelineAnchor,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };

        private readonly struct DirtyTrace
        {
            public long LogicalTraceId { get; }
            public SyncTelemetryPoint Setter { get; }

            public DirtyTrace(long logicalTraceId, SyncTelemetryPoint setter)
            {
                LogicalTraceId = logicalTraceId;
                Setter = setter;
            }
        }
    }
}
