using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Sync
{
    public interface IMonotonicClock
    {
        long Frequency { get; }
        long GetTimestamp();
    }

    public sealed class StopwatchMonotonicClock : IMonotonicClock
    {
        public static StopwatchMonotonicClock Instance { get; } = new();

        private StopwatchMonotonicClock()
        {
        }

        public long Frequency => Stopwatch.Frequency;
        public long GetTimestamp() => Stopwatch.GetTimestamp();
    }

    public enum SyncProfile : byte
    {
        InitialTransfer,
        SiteColor,
        CutDefinition,
        TimelineAnchor
    }

    public enum SyncMilestone : byte
    {
        UserRequest,
        Setter,
        CaptureStart,
        CaptureEnd,
        Queued,
        Replaced,
        Encoded,
        FirstByteWritten,
        LastByteWritten,
        FirstByteReceived,
        LastByteReceived,
        ApplyStart,
        ApplyEnd,
        NextVisible,
        ScientificStable,
        PublicationReceipt,
        ReceivedAck,
        AppliedAck,
        VisibleAck
    }

    public enum SyncTelemetryIdentityKind : byte
    {
        Unavailable,
        Exact,
        LegacyProxy
    }

    public readonly struct SyncTelemetryIdentity : IEquatable<SyncTelemetryIdentity>
    {
        public string ScopeId { get; }
        public long LogicalTraceId { get; }
        public long CaptureGeneration { get; }
        public long AttemptId { get; }
        public SyncTelemetryIdentityKind LogicalTraceIdKind { get; }
        public SyncTelemetryIdentityKind CaptureGenerationKind { get; }
        public SyncTelemetryIdentityKind AttemptIdKind { get; }
        public bool IsValid => !string.IsNullOrEmpty(ScopeId);

        public SyncTelemetryIdentity(string scopeId, long logicalTraceId, long captureGeneration, long attemptId = 0) : this(scopeId, logicalTraceId, captureGeneration, attemptId, logicalTraceId == 0 ? SyncTelemetryIdentityKind.Unavailable : SyncTelemetryIdentityKind.Exact, captureGeneration == 0 ? SyncTelemetryIdentityKind.Unavailable : SyncTelemetryIdentityKind.Exact, attemptId == 0 ? SyncTelemetryIdentityKind.Unavailable : SyncTelemetryIdentityKind.Exact)
        {
        }

        private SyncTelemetryIdentity(string scopeId, long logicalTraceId, long captureGeneration, long attemptId, SyncTelemetryIdentityKind logicalTraceIdKind, SyncTelemetryIdentityKind captureGenerationKind, SyncTelemetryIdentityKind attemptIdKind)
        {
            ScopeId = scopeId ?? string.Empty;
            LogicalTraceId = logicalTraceId;
            CaptureGeneration = captureGeneration;
            AttemptId = attemptId;
            LogicalTraceIdKind = logicalTraceIdKind;
            CaptureGenerationKind = captureGenerationKind;
            AttemptIdKind = attemptIdKind;
        }

        public static SyncTelemetryIdentity LegacyRevisionProxy(string scopeId, long revision)
        {
            return new SyncTelemetryIdentity(scopeId, revision, revision, 0, SyncTelemetryIdentityKind.LegacyProxy, SyncTelemetryIdentityKind.LegacyProxy, SyncTelemetryIdentityKind.Unavailable);
        }

        public static SyncTelemetryIdentity Unknown(string scopeId)
        {
            return new SyncTelemetryIdentity(scopeId, 0, 0, 0, SyncTelemetryIdentityKind.Unavailable, SyncTelemetryIdentityKind.Unavailable, SyncTelemetryIdentityKind.Unavailable);
        }

        public SyncTelemetryIdentity WithAttempt(long attemptId) => new(ScopeId, LogicalTraceId, CaptureGeneration, attemptId, LogicalTraceIdKind, CaptureGenerationKind, attemptId == 0 ? SyncTelemetryIdentityKind.Unavailable : SyncTelemetryIdentityKind.Exact);

        public bool Equals(SyncTelemetryIdentity other)
        {
            return ScopeId == other.ScopeId && LogicalTraceId == other.LogicalTraceId && CaptureGeneration == other.CaptureGeneration && AttemptId == other.AttemptId && LogicalTraceIdKind == other.LogicalTraceIdKind && CaptureGenerationKind == other.CaptureGenerationKind && AttemptIdKind == other.AttemptIdKind;
        }

        public override bool Equals(object obj) => obj is SyncTelemetryIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ScopeId, LogicalTraceId, CaptureGeneration, AttemptId, LogicalTraceIdKind, CaptureGenerationKind, AttemptIdKind);
    }

    public readonly struct SyncTelemetryPoint
    {
        public bool IsValid { get; }
        public long Timestamp { get; }
        public long AllocatedBytes { get; }
        public int ManagedThreadId { get; }
        internal long ContextGeneration { get; }
        public long RecordingGeneration => ContextGeneration;
        internal long ClockFrequency { get; }
        internal int MainThreadId { get; }

        internal SyncTelemetryPoint(long timestamp, long allocatedBytes, int managedThreadId, long contextGeneration, long clockFrequency, int mainThreadId)
        {
            IsValid = true;
            Timestamp = timestamp;
            AllocatedBytes = allocatedBytes;
            ManagedThreadId = managedThreadId;
            ContextGeneration = contextGeneration;
            ClockFrequency = clockFrequency;
            MainThreadId = mainThreadId;
        }
    }

    public readonly struct SyncTelemetrySample
    {
        public SyncProfile Profile { get; }
        public SyncMilestone Milestone { get; }
        public SyncTelemetryIdentity Identity { get; }
        public string CorrelationId => Identity.ScopeId;
        public long Timestamp { get; }
        public long ClockFrequency { get; }
        public long AllocatedBytes { get; }
        public int ManagedThreadId { get; }
        public bool IsMainThread { get; }
        public long PayloadBytes { get; }
        public long RecordingGeneration { get; }

        internal SyncTelemetrySample(SyncProfile profile, SyncMilestone milestone, SyncTelemetryIdentity identity, SyncTelemetryPoint point, long payloadBytes)
        {
            Profile = profile;
            Milestone = milestone;
            Identity = identity;
            Timestamp = point.Timestamp;
            ClockFrequency = point.ClockFrequency;
            AllocatedBytes = point.AllocatedBytes;
            ManagedThreadId = point.ManagedThreadId;
            IsMainThread = point.ManagedThreadId == point.MainThreadId;
            PayloadBytes = payloadBytes;
            RecordingGeneration = point.ContextGeneration;
        }
    }

    public interface ISyncTelemetrySink
    {
        void Record(SyncTelemetrySample sample);
    }

    /// <summary>CloseAsync stops admission synchronously, then asynchronously drains admitted writers.</summary>
    public interface ISyncTelemetryCapture : IDisposable
    {
        Task CloseAsync();
    }

    /// <summary>A fixed-capacity sink that retains the oldest admitted samples and counts overflow.</summary>
    public sealed class BoundedSyncTelemetrySink : ISyncTelemetrySink
    {
        private readonly object m_Gate = new();
        private readonly SyncTelemetrySample[] m_Samples;
        private int m_Count;
        private long m_Dropped;

        public BoundedSyncTelemetrySink(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Samples = new SyncTelemetrySample[capacity];
        }

        public int Capacity => m_Samples.Length;

        public int Count
        {
            get
            {
                lock (m_Gate)
                    return m_Count;
            }
        }

        public long Dropped
        {
            get
            {
                lock (m_Gate)
                    return m_Dropped;
            }
        }

        public void Record(SyncTelemetrySample sample)
        {
            lock (m_Gate)
            {
                if (m_Count == m_Samples.Length)
                {
                    if (m_Dropped != long.MaxValue) ++m_Dropped;
                    return;
                }

                m_Samples[m_Count++] = sample;
            }
        }

        public bool TryGet(int index, out SyncTelemetrySample sample)
        {
            lock (m_Gate)
            {
                if ((uint)index >= (uint)m_Count)
                {
                    sample = default;
                    return false;
                }

                sample = m_Samples[index];
                return true;
            }
        }
    }

    /// <summary>Opt-in, process-local sync measurements. With no context installed, probes only take a null branch.</summary>
    public static class SyncTelemetry
    {
        private static readonly object s_Gate = new();
        private static CaptureContext s_Context;
        private static long s_NextGeneration;

        public static bool Enabled => Volatile.Read(ref s_Context) != null;
        public static long ActiveCaptureGeneration => Volatile.Read(ref s_Context)?.Generation ?? 0;

        public static SyncTelemetryPoint CapturePoint()
        {
            CaptureContext context = Volatile.Read(ref s_Context);
            if (context == null)
                return default;
            return new SyncTelemetryPoint(context.Clock.GetTimestamp(), GC.GetAllocatedBytesForCurrentThread(), Thread.CurrentThread.ManagedThreadId, context.Generation, context.Frequency, context.MainThreadId);
        }

        public static void Mark(SyncProfile profile, string correlationId, SyncMilestone milestone, long payloadBytes = 0)
        {
            Mark(profile, new SyncTelemetryIdentity(correlationId, 0, 0), milestone, payloadBytes);
        }

        public static void Mark(SyncProfile profile, SyncTelemetryIdentity identity, SyncMilestone milestone, long payloadBytes = 0)
        {
            SyncTelemetryPoint point = CapturePoint();
            MarkAt(profile, identity, milestone, point, payloadBytes);
        }

        public static void MarkAt(SyncProfile profile, string correlationId, SyncMilestone milestone, SyncTelemetryPoint point, long payloadBytes = 0)
        {
            MarkAt(profile, new SyncTelemetryIdentity(correlationId, 0, 0), milestone, point, payloadBytes);
        }

        public static void MarkAt(SyncProfile profile, SyncTelemetryIdentity identity, SyncMilestone milestone, SyncTelemetryPoint point, long payloadBytes = 0)
        {
            CaptureContext context = Volatile.Read(ref s_Context);
            if (context == null || !point.IsValid || point.ContextGeneration != context.Generation || !context.TryEnterWriter())
                return;
            try
            {
                context.Sink.Record(new SyncTelemetrySample(profile, milestone, identity, point, payloadBytes));
            }
            catch
            {
                // Diagnostics must never change application behavior.
            }
            finally
            {
                context.ExitWriter();
            }
        }

        public static ISyncTelemetryCapture BeginCapture(ISyncTelemetrySink sink, IMonotonicClock clock = null, int mainThreadId = 0)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));
            lock (s_Gate)
            {
                if (s_Context != null)
                    throw new InvalidOperationException("Sync telemetry capture is already active.");
                var context = new CaptureContext(sink, clock ?? StopwatchMonotonicClock.Instance, mainThreadId == 0 ? Thread.CurrentThread.ManagedThreadId : mainThreadId, Interlocked.Increment(ref s_NextGeneration));
                Volatile.Write(ref s_Context, context);
                return new CaptureSession(context);
            }
        }

        private sealed class CaptureContext
        {
            private readonly object m_Gate = new();
            private bool m_Accepting = true;
            private int m_Writers;
            private readonly TaskCompletionSource<bool> m_Drained = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ISyncTelemetrySink Sink { get; }
            public IMonotonicClock Clock { get; }
            public long Frequency { get; }
            public int MainThreadId { get; }
            public long Generation { get; }

            public CaptureContext(ISyncTelemetrySink sink, IMonotonicClock clock, int mainThreadId, long generation)
            {
                Sink = sink;
                Clock = clock;
                Frequency = clock.Frequency;
                if (Frequency <= 0) throw new ArgumentOutOfRangeException(nameof(clock), "The monotonic frequency must be positive.");
                MainThreadId = mainThreadId;
                Generation = generation;
            }

            public bool TryEnterWriter()
            {
                lock (m_Gate)
                {
                    if (!m_Accepting)
                        return false;
                    ++m_Writers;
                    return true;
                }
            }

            public void ExitWriter()
            {
                lock (m_Gate)
                {
                    if (--m_Writers == 0 && !m_Accepting)
                        m_Drained.TrySetResult(true);
                }
            }

            public Task CloseAsync()
            {
                lock (m_Gate)
                {
                    m_Accepting = false;
                    if (m_Writers == 0) m_Drained.TrySetResult(true);
                    return m_Drained.Task;
                }
            }
        }

        private sealed class CaptureSession : ISyncTelemetryCapture
        {
            private readonly object m_Gate = new();
            private readonly CaptureContext m_Owner;
            private Task m_Close;

            public CaptureSession(CaptureContext owner) => m_Owner = owner;

            public Task CloseAsync()
            {
                lock (m_Gate)
                {
                    if (m_Close != null) return m_Close;
                    lock (s_Gate)
                    {
                        if (ReferenceEquals(s_Context, m_Owner))
                            Volatile.Write(ref s_Context, null);
                        m_Close = m_Owner.CloseAsync();
                    }

                    return m_Close;
                }
            }

            // For short, synchronous sinks only. Async/test owners should await CloseAsync.
            // A sink must not synchronously dispose its own capture from inside Record.
            public void Dispose() => CloseAsync().GetAwaiter().GetResult();
        }
    }
}
