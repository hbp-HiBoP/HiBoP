using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync;

namespace HBP.Transfer.Transport
{
    public enum PersistentTransportState : byte
    {
        Disconnected,
        Connected,
        DisconnectedGrace,
        Faulted,
        Disposed
    }

    /// <summary>
    /// Runs the T03 scheduler over one already-authenticated persistent stream.
    /// The scheduler has one owner here; incoming records are receipt-ACKed only
    /// after bounded queue admission and are read independently by the application.
    /// </summary>
    public sealed class PersistentTransport : IDisposable
    {
        private const int MaximumDirectControls = 64;
        private const int MaximumOutOfOrderRecords = 128;
        private const int MaximumIncomingRecords = 256;
        private const int MaximumIncomingBytes = 4 * 1024 * 1024;
        private const int MaximumPendingPingSamples = 32;
        private const int MaximumPayloadFromPeerBytes = TransportFrameCodec.MaximumPayloadBytes;

        private readonly object m_Gate = new object();
        private readonly OutgoingScheduler m_Scheduler;
        private readonly TimeSpan m_LivenessInterval;
        private readonly TimeSpan m_ClockProbeInterval;
        private readonly Func<bool> m_ShouldProbeClock;
        private readonly Func<CancellationToken, Task> m_PreviewFrameWaiter;
        private readonly CancellationTokenSource m_DisposeSource = new CancellationTokenSource();
        private readonly SemaphoreSlim m_WriterSignal = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim m_IncomingAvailable = new SemaphoreSlim(0);
        private readonly Queue<TransportRecord> m_DirectControls = new Queue<TransportRecord>();
        private readonly Dictionary<Guid, TransportRecord> m_PendingAcknowledgements = new Dictionary<Guid, TransportRecord>();
        private readonly Dictionary<Guid, ulong> m_LastSentSequences = new Dictionary<Guid, ulong>();
        private readonly Dictionary<Guid, InboundStreamState> m_InboundStreams = new Dictionary<Guid, InboundStreamState>();
        private readonly Queue<Guid> m_PendingPingOrder = new Queue<Guid>();
        private readonly Dictionary<Guid, PendingPing> m_PendingPings = new Dictionary<Guid, PendingPing>();
        private readonly Queue<TransportRecord> m_Incoming = new Queue<TransportRecord>();
        private TransportRecord m_IncomingProgressPlaceholder;
        private TransportRecord m_LatestIncomingProgress;
        private RetentionProgress m_LastReceivedProgress;
        private readonly Func<Guid> m_GuidFactory;
        private ulong m_LastOriginSequence;
        private int m_OutOfOrderRecordCount;
        private int m_OutOfOrderBytes;
        private int m_IncomingBytes;
        private int m_IncomingReaderWaiters;
        private ulong m_RemoteBulkStreamRetiredThrough;
        private ulong m_LastWrittenBulkStreamRetiredThrough;
        private ulong? m_PendingBulkStreamRetiredThrough;
        private int m_ConnectionRunning;
        private bool m_HandshakeComplete;
        private bool m_IncomingCompleted;
        private bool m_Disposed;
        private bool m_Faulted;
        private bool m_FaultShutdownStarted;
        private Exception m_FaultException;
        private Stream m_CurrentStream;
        private PersistentTransportState m_State = PersistentTransportState.Disconnected;
        private TimeSpan? m_LastRoundTrip;
        private long m_LastLivenessProbeTicks;

        public event Action<ClockProbeSample> ClockProbeSampleReceived;

        public PersistentTransportState State
        {
            get
            {
                lock (m_Gate)
                {
                    if (m_State == PersistentTransportState.DisconnectedGrace)
                    {
                        m_State = m_Scheduler.State switch
                        {
                            SchedulerState.DisconnectedGrace => PersistentTransportState.DisconnectedGrace,
                            SchedulerState.Online => PersistentTransportState.Connected,
                            SchedulerState.Offline => PersistentTransportState.Disconnected,
                            SchedulerState.Faulted => PersistentTransportState.Faulted,
                            _ => m_State
                        };
                    }

                    return m_State;
                }
            }
        }

        /// <summary>True only after the peer has confirmed this connection's resume handshake.</summary>
        public bool IsConnectionReady
        {
            get
            {
                lock (m_Gate) return m_State == PersistentTransportState.Connected && m_HandshakeComplete;
            }
        }

        public TimeSpan? LastRoundTrip
        {
            get
            {
                lock (m_Gate) return m_LastRoundTrip;
            }
        }

        public PersistentTransport(OutgoingScheduler scheduler, TimeSpan? livenessInterval = null, Func<Guid> guidFactory = null, Func<bool> shouldProbeClock = null, TimeSpan? clockProbeInterval = null, Func<CancellationToken, Task> previewFrameWaiter = null)
        {
            m_Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            m_PreviewFrameWaiter = previewFrameWaiter;
            m_Scheduler.PreviewFramePacingEnabled = previewFrameWaiter != null;
            m_LivenessInterval = livenessInterval ?? TimeSpan.FromSeconds(15);
            if (m_LivenessInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(livenessInterval));
            m_ClockProbeInterval = clockProbeInterval ?? TimeSpan.FromMilliseconds(250);
            if (m_ClockProbeInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(clockProbeInterval));
            m_ShouldProbeClock = shouldProbeClock;
            if (scheduler.Limits.BulkChunkBytes > TransportFrameCodec.MaximumPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(scheduler), "The scheduler bulk chunk exceeds the transport frame bound.");
            m_GuidFactory = guidFactory ?? Guid.NewGuid;
        }

        public EnqueueResult EnqueueMutation(Mutation mutation, ulong? canonicalSequence, ulong? observedCanonicalSequence, bool coalesciblePreview = true, OperationId operationId = null)
        {
            ValidateMutationSequences(canonicalSequence, observedCanonicalSequence);
            EnqueueResult result;
            bool schedulerFaulted;
            lock (m_Gate)
            {
                ThrowIfUnavailable();
                result = m_Scheduler.EnqueueMutation(mutation, coalesciblePreview, operationId, canonicalSequence, observedCanonicalSequence);
                schedulerFaulted = MarkSchedulerFaultedLocked();
            }

            if (schedulerFaulted)
                StopFaultedSession();
            if (result.Accepted)
                SignalWriter();
            return result;
        }

        /// <summary>Wakes the transport writer after an attached driver enqueues directly on its scheduler.</summary>
        public void NotifySchedulerChanged()
        {
            bool schedulerFaulted;
            lock (m_Gate)
            {
                ThrowIfUnavailable();
                schedulerFaulted = MarkSchedulerFaultedLocked();
            }

            if (schedulerFaulted)
                StopFaultedSession();
            else
                SignalWriter();
        }

        public EnqueueResult EnqueueSceneOperation(byte[] encodedBody, ScheduleDescriptor descriptor, bool coalesciblePreview = false, bool structural = false, bool final = false, ushort bodySchema = 1, OperationId operationId = null, ulong? canonicalSequence = null, ulong? observedCanonicalSequence = null)
        {
            EnqueueResult result;
            bool schedulerFaulted;
            bool retirementPending;
            lock (m_Gate)
            {
                ThrowIfUnavailable();
                result = m_Scheduler.EnqueueSceneOperation(encodedBody, descriptor, coalesciblePreview, structural, final, bodySchema, operationId, canonicalSequence, observedCanonicalSequence);
                schedulerFaulted = MarkSchedulerFaultedLocked();
                RetireSentBulkWatermarksLocked();
                retirementPending = m_PendingBulkStreamRetiredThrough.HasValue;
            }

            if (schedulerFaulted)
                StopFaultedSession();
            if (result.Accepted || retirementPending)
                SignalWriter();
            return result;
        }

        public EnqueueResult EnqueueSessionControl(byte[] payload, DeliveryReliability reliability)
        {
            EnqueueResult result;
            bool schedulerFaulted;
            lock (m_Gate)
            {
                ThrowIfUnavailable();
                result = m_Scheduler.EnqueueSessionControl(payload, reliability);
                schedulerFaulted = MarkSchedulerFaultedLocked();
            }

            if (schedulerFaulted)
                StopFaultedSession();
            if (result.Accepted)
                SignalWriter();
            return result;
        }

        public EnqueueResult EnqueueRetentionProgress(RetentionProgress progress)
        {
            EnqueueResult result;
            bool faulted;
            lock (m_Gate)
            {
                ThrowIfUnavailable();
                result = m_Scheduler.EnqueueRetentionProgress(progress.Encode());
                faulted = MarkSchedulerFaultedLocked();
            }

            if (faulted) StopFaultedSession();
            if (result.Accepted) SignalWriter();
            return result;
        }

        public IReadOnlyList<OperationId> RetireAppliedSceneOperations(ulong throughOriginSequence)
        {
            lock (m_Gate) return m_Scheduler.RetireAppliedSceneOperations(throughOriginSequence);
        }

        public bool CancelBulk(OperationId operationId)
        {
            bool cancelled;
            lock (m_Gate)
            {
                ThrowIfUnavailable();
                cancelled = m_Scheduler.CancelBulk(operationId);
                RetireSentBulkWatermarksLocked();
            }

            if (cancelled)
                SignalWriter();
            return cancelled;
        }

        public SchedulerMetrics SnapshotMetrics()
        {
            lock (m_Gate)
                return m_Scheduler.SnapshotMetrics();
        }

        /// <summary>Enqueues a reliable, operation-scoped rejection without ending the session.</summary>
        public EnqueueResult EnqueueScopedRejection(OperationId operationId, string code, string diagnostic)
        {
            byte[] payload = ScopedRejectionCodec.Encode(operationId, code, diagnostic);
            return EnqueueSessionControl(payload, DeliveryReliability.Reliable);
        }

        /// <summary>Requests an ephemeral ping on the current connection; it is never sequenced or replayed.</summary>
        public Guid SendLivenessProbe()
        {
            Guid id;
            lock (m_Gate)
            {
                ThrowIfUnavailable();
                if (!m_HandshakeComplete)
                    return Guid.Empty;
                id = NextGuid();
                long timestamp = Stopwatch.GetTimestamp();
                if (m_PendingPings.Count == MaximumPendingPingSamples)
                {
                    while (m_PendingPingOrder.Count > 0)
                    {
                        Guid oldest = m_PendingPingOrder.Dequeue();
                        if (m_PendingPings.Remove(oldest))
                            break;
                    }
                }

                while (m_PendingPingOrder.Count > 0 && !m_PendingPings.ContainsKey(m_PendingPingOrder.Peek()))
                    m_PendingPingOrder.Dequeue();
                m_PendingPings.Add(id, new PendingPing(timestamp, checked((ulong)Stopwatch.Frequency)));
                m_PendingPingOrder.Enqueue(id);
                if (!QueueDirectControl(CreatePingRecord(id, timestamp)))
                {
                    m_PendingPings.Remove(id);
                    return Guid.Empty;
                }
            }

            SignalWriter();
            return id;
        }

        /// <summary>
        /// Attaches the transport to a stream returned by the existing authenticated
        /// Quest pairing path. Reuse this object for reconnects inside the grace window.
        /// </summary>
        public async Task RunConnectionAsync(Stream authenticatedStream, CancellationToken cancellationToken)
        {
            if (authenticatedStream == null)
                throw new ArgumentNullException(nameof(authenticatedStream));
            ThrowIfUnavailable();
            if (Interlocked.CompareExchange(ref m_ConnectionRunning, 1, 0) != 0)
                throw new InvalidOperationException("A sync transport session can own only one active connection.");

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_DisposeSource.Token))
            {
                CancellationToken token = linked.Token;
                TaskCompletionSource<bool> handshakeReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    lock (m_Gate)
                    {
                        ThrowIfUnavailable();
                        if (m_Scheduler.State == SchedulerState.DisconnectedGrace && !m_Scheduler.TryResume())
                            throw new InvalidOperationException("The reconnect grace expired before the authenticated stream resumed.");
                        ResetEphemeralConnectionState();
                        m_HandshakeComplete = false;
                        m_CurrentStream = authenticatedStream;
                        m_State = PersistentTransportState.Connected;
                        QueueDirectControl(CreateResumeHello());
                    }

                    using (token.Register(CloseCurrentStream))
                    {
                        Task reader = ReadLoopAsync(authenticatedStream, handshakeReady, token);
                        Task writer = WriteLoopAsync(authenticatedStream, token);
                        Task liveness = LivenessLoopAsync(handshakeReady.Task, token);
                        Task completed = await Task.WhenAny(reader, writer, liveness).ConfigureAwait(false);
                        Exception failure = null;
                        try
                        {
                            await completed.ConfigureAwait(false);
                            if (!token.IsCancellationRequested)
                                failure = new EndOfStreamException("A sync transport loop ended unexpectedly.");
                        }
                        catch (Exception exception)
                        {
                            failure = exception;
                        }

                        bool sessionFaulted;
                        lock (m_Gate)
                        {
                            MarkSchedulerFaultedLocked();
                            sessionFaulted = m_Faulted;
                            if (sessionFaulted)
                                failure = m_FaultException ?? failure;
                        }

                        if (sessionFaulted)
                            StopFaultedSession();

                        bool callerCancelled = cancellationToken.IsCancellationRequested || m_DisposeSource.IsCancellationRequested;
                        linked.Cancel();
                        CloseCurrentStream();
                        await ObserveLoopShutdownAsync(reader).ConfigureAwait(false);
                        await ObserveLoopShutdownAsync(writer).ConfigureAwait(false);
                        await ObserveLoopShutdownAsync(liveness).ConfigureAwait(false);

                        if (sessionFaulted)
                        {
                            lock (m_Gate)
                                m_CurrentStream = null;
                            throw failure ?? new TransportProtocolException("The sync transport session faulted.");
                        }

                        if (callerCancelled)
                        {
                            lock (m_Gate)
                            {
                                m_CurrentStream = null;
                                if (!m_Disposed && !m_Faulted)
                                {
                                    m_Scheduler.BeginDisconnectGrace();
                                    m_State = m_Scheduler.State == SchedulerState.DisconnectedGrace ? PersistentTransportState.DisconnectedGrace : PersistentTransportState.Disconnected;
                                }
                            }

                            return;
                        }

                        if (failure is InvalidDataException || failure is TransportProtocolException)
                        {
                            lock (m_Gate)
                            {
                                MarkFaultedLocked(failure);
                                m_CurrentStream = null;
                            }

                            StopFaultedSession();
                            throw failure;
                        }

                        if (failure is IOException || failure is SocketException || failure is ObjectDisposedException)
                        {
                            lock (m_Gate)
                            {
                                if (!m_Disposed && !m_Faulted)
                                {
                                    m_Scheduler.BeginDisconnectGrace();
                                    m_State = PersistentTransportState.DisconnectedGrace;
                                }

                                m_CurrentStream = null;
                            }

                            throw failure;
                        }

                        Exception terminalFailure = failure ?? new InvalidOperationException("A sync transport loop failed without an exception.");
                        lock (m_Gate)
                        {
                            MarkFaultedLocked(terminalFailure);
                            m_CurrentStream = null;
                        }

                        StopFaultedSession();
                        throw terminalFailure;
                    }
                }
                finally
                {
                    lock (m_Gate)
                    {
                        if (ReferenceEquals(m_CurrentStream, authenticatedStream))
                            m_CurrentStream = null;
                    }

                    Interlocked.Exchange(ref m_ConnectionRunning, 0);
                }
            }
        }

        /// <summary>Reads the next bounded, in-order application record from the session queue.</summary>
        public async Task<TransportRecord> ReadIncomingAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                lock (m_Gate)
                {
                    if (m_IncomingCompleted && m_Incoming.Count == 0)
                        ThrowIncomingCompletionLocked();
                    m_IncomingReaderWaiters++;
                }

                try
                {
                    await m_IncomingAvailable.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    lock (m_Gate)
                        m_IncomingReaderWaiters--;
                }

                lock (m_Gate)
                {
                    if (m_Incoming.Count > 0)
                    {
                        TransportRecord record = DequeueIncoming();
                        m_IncomingBytes -= checked(TransportFrameCodec.HeaderLength + record.PayloadLength);
                        return record;
                    }

                    if (m_IncomingCompleted)
                        ThrowIncomingCompletionLocked();
                }
            }
        }

        /// <summary>Drains an already retained record without waiting or changing reliable transport history.</summary>
        public bool TryReadIncoming(out TransportRecord record)
        {
            lock (m_Gate)
            {
                record = null;
                if (m_Incoming.Count == 0 || !m_IncomingAvailable.Wait(0)) return false;
                record = DequeueIncoming();
                m_IncomingBytes -= checked(TransportFrameCodec.HeaderLength + record.PayloadLength);
                return true;
            }
        }

        private async Task ReadLoopAsync(Stream stream, TaskCompletionSource<bool> handshakeReady, CancellationToken cancellationToken)
        {
            bool receivedHello = false;
            while (true)
            {
                TransportRecord record = await TransportFrameCodec.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
                if (record == null)
                    throw new EndOfStreamException("The authenticated sync stream closed.");
                if (!record.SessionId.Equals(m_Scheduler.SessionId))
                    throw new InvalidDataException("The transport record belongs to another session.");

                if (record.Kind == TransportMessageKind.ResumeHello)
                {
                    if (receivedHello)
                        throw new InvalidDataException("A connection sent more than one resume handshake.");
                    ProcessResumeHello(record);
                    receivedHello = true;
                    handshakeReady.TrySetResult(true);
                    continue;
                }

                if (!receivedHello)
                    throw new InvalidDataException("The peer sent application traffic before resume negotiation completed.");

                switch (record.Kind)
                {
                    case TransportMessageKind.Application:
                        ProcessApplicationRecord(record);
                        break;
                    case TransportMessageKind.Acknowledgement:
                        ProcessAcknowledgement(record);
                        break;
                    case TransportMessageKind.Ping:
                        ProcessPing(record);
                        break;
                    case TransportMessageKind.Pong:
                        ProcessPong(record);
                        break;
                    case TransportMessageKind.BulkStreamRetirement:
                        ProcessBulkStreamRetirement(record);
                        break;
                    default:
                        throw new InvalidDataException("Unsupported sync transport record kind.");
                }
            }
        }

        private async Task WriteLoopAsync(Stream stream, CancellationToken cancellationToken)
        {
            Task previewFrame = null;
            Task writerWake = null;
            while (true)
            {
                if (previewFrame?.IsCompleted == true)
                {
                    await previewFrame.ConfigureAwait(false);
                    lock (m_Gate) m_Scheduler.BeginPreviewFrame();
                    previewFrame = null;
                }

                bool wroteAny = false;
                bool needsPreviewFrame = false;
                while (true)
                {
                    TransportRecord record;
                    lock (m_Gate)
                    {
                        if (m_Scheduler.State == SchedulerState.Faulted)
                        {
                            MarkFaultedLocked(CreateSchedulerFaultException());
                            throw m_FaultException;
                        }

                        if (m_DirectControls.Count > 0)
                        {
                            record = m_DirectControls.Dequeue();
                            if (record.Kind == TransportMessageKind.Acknowledgement)
                                m_PendingAcknowledgements.Remove(record.StreamId.Value);
                        }
                        else if (m_HandshakeComplete && m_PendingBulkStreamRetiredThrough.HasValue)
                        {
                            ulong retiredThrough = m_PendingBulkStreamRetiredThrough.Value;
                            m_PendingBulkStreamRetiredThrough = null;
                            if (retiredThrough > m_LastWrittenBulkStreamRetiredThrough)
                                m_LastWrittenBulkStreamRetiredThrough = retiredThrough;
                            record = CreateBulkStreamRetirementRecord(retiredThrough);
                        }
                        else if (m_HandshakeComplete && m_Scheduler.TryGetNextTransmission(out TransmissionAttempt transmission))
                        {
                            record = CreateApplicationRecord(transmission.Frame);
                            if (record.ReliableFrameSequence > 0)
                                TrackSentSequence(record.StreamId, record.ReliableFrameSequence);
                        }
                        else
                            record = null;

                        if (record == null && m_Scheduler.State == SchedulerState.Faulted)
                        {
                            MarkFaultedLocked(CreateSchedulerFaultException());
                            throw m_FaultException;
                        }
                    }

                    if (record == null)
                    {
                        lock (m_Gate) needsPreviewFrame = m_Scheduler.NeedsPreviewFrame;
                        break;
                    }

                    await TransportFrameCodec.WriteAsync(stream, record, cancellationToken).ConfigureAwait(false);
                    wroteAny = true;
                }

                if (needsPreviewFrame && previewFrame == null)
                    previewFrame = m_PreviewFrameWaiter(cancellationToken);
                if (!wroteAny)
                {
                    writerWake ??= m_WriterSignal.WaitAsync(cancellationToken);
                    if (previewFrame == null) await writerWake.ConfigureAwait(false);
                    else await Task.WhenAny(previewFrame, writerWake).ConfigureAwait(false);
                    if (writerWake.IsCompleted)
                    {
                        await writerWake.ConfigureAwait(false);
                        writerWake = null;
                    }
                }
            }
        }

        private async Task LivenessLoopAsync(Task handshakeReady, CancellationToken cancellationToken)
        {
            await AwaitWithCancellationAsync(handshakeReady, cancellationToken).ConfigureAwait(false);
            m_LastLivenessProbeTicks = Stopwatch.GetTimestamp();
            while (true)
            {
                await Task.Delay(m_ClockProbeInterval, cancellationToken).ConfigureAwait(false);
                long now = Stopwatch.GetTimestamp();
                bool shouldProbeClock = false;
                try
                {
                    shouldProbeClock = m_ShouldProbeClock?.Invoke() == true;
                }
                catch (ObjectDisposedException)
                {
                }

                double elapsed = (double)(now - m_LastLivenessProbeTicks) / Stopwatch.Frequency;
                if (shouldProbeClock || elapsed >= m_LivenessInterval.TotalSeconds)
                {
                    SendLivenessProbe();
                    m_LastLivenessProbeTicks = now;
                }
            }
        }

        private void ProcessResumeHello(TransportRecord record)
        {
            Dictionary<ReliableStreamId, ulong> peerWatermarks = ResumeWatermarkCodec.Decode(record.GetPayloadCopy());
            lock (m_Gate)
            {
                if (record.OriginDevice == m_Scheduler.OriginDevice)
                    throw new InvalidDataException("Resume state claims the local origin device.");
                foreach (KeyValuePair<ReliableStreamId, ulong> watermark in peerWatermarks)
                {
                    if (!m_LastSentSequences.TryGetValue(watermark.Key.Value, out ulong highestSent))
                    {
                        if (IsLocallyRetiredBulkStream(watermark.Key))
                            continue;
                        throw new InvalidDataException("Resume state acknowledges an unknown or unsent reliable frame.");
                    }

                    if (watermark.Value > highestSent)
                        throw new InvalidDataException("Resume state acknowledges an unknown or unsent reliable frame.");
                    m_Scheduler.Acknowledge(watermark.Key, watermark.Value);
                }

                RetireSentBulkWatermarksLocked();
                ApplyRemoteBulkStreamRetirementLocked(record.BulkStreamRetiredThrough);
                m_HandshakeComplete = true;
                QueueLocalBulkStreamRetirementLocked();
            }

            SignalWriter();
        }

        private void ProcessAcknowledgement(TransportRecord record)
        {
            lock (m_Gate)
            {
                if (!m_LastSentSequences.TryGetValue(record.StreamId.Value, out ulong highestSent))
                {
                    if (IsLocallyRetiredBulkStream(record.StreamId))
                        return;
                    throw new InvalidDataException("Acknowledgement exceeds the sent reliable stream watermark.");
                }

                if (record.ReliableFrameSequence > highestSent)
                    throw new InvalidDataException("Acknowledgement exceeds the sent reliable stream watermark.");
                m_Scheduler.Acknowledge(record.StreamId, record.ReliableFrameSequence);
                RetireSentBulkWatermarksLocked();
            }

            SignalWriter();
        }

        private void ProcessBulkStreamRetirement(TransportRecord record)
        {
            lock (m_Gate)
            {
                if (record.OriginDevice == m_Scheduler.OriginDevice)
                    throw new InvalidDataException("Bulk retirement claims the local origin device.");
                ApplyRemoteBulkStreamRetirementLocked(record.BulkStreamRetiredThrough);
            }
        }

        private void ProcessPing(TransportRecord record)
        {
            byte[] request = record.GetPayloadCopy();
            long remoteReceiveTicks = Stopwatch.GetTimestamp();
            long requesterSendTicks = unchecked((long)ReadUInt64(request, 0));
            byte[] response = new byte[32];
            WriteUInt64(response, 0, unchecked((ulong)remoteReceiveTicks));
            WriteUInt64(response, 8, unchecked((ulong)Stopwatch.GetTimestamp()));
            WriteUInt64(response, 16, checked((ulong)Stopwatch.Frequency));
            WriteUInt64(response, 24, unchecked((ulong)requesterSendTicks));
            var pong = new TransportRecord(TransportMessageKind.Pong, m_Scheduler.SessionId, messageId: record.MessageId, originDevice: m_Scheduler.OriginDevice, payload: response);
            lock (m_Gate)
                QueueDirectControl(pong);
            SignalWriter();
        }

        private void ProcessPong(TransportRecord record)
        {
            byte[] payload = record.GetPayloadCopy();
            ClockProbeSample sample = null;
            lock (m_Gate)
            {
                if (!m_PendingPings.TryGetValue(record.MessageId.Value, out PendingPing pending))
                    return;
                m_PendingPings.Remove(record.MessageId.Value);
                long receivedAt = Stopwatch.GetTimestamp();
                long elapsed = receivedAt - pending.SentTicks;
                if (elapsed >= 0)
                    m_LastRoundTrip = TimeSpan.FromSeconds((double)elapsed / Stopwatch.Frequency);

                if (ReadUInt64(payload, 24) == unchecked((ulong)pending.SentTicks))
                {
                    long remoteReceiveTicks = unchecked((long)ReadUInt64(payload, 0));
                    long remoteSendTicks = unchecked((long)ReadUInt64(payload, 8));
                    ulong remoteFrequency = ReadUInt64(payload, 16);
                    if (remoteReceiveTicks >= 0 && remoteSendTicks >= remoteReceiveTicks && remoteFrequency > 0 && remoteFrequency <= 1000000000000UL)
                        sample = new ClockProbeSample(pending.SentTicks, receivedAt, pending.TickFrequency, remoteReceiveTicks, remoteSendTicks, remoteFrequency);
                }
            }

            if (sample != null) ClockProbeSampleReceived?.Invoke(sample);
        }

        private void ProcessApplicationRecord(TransportRecord record)
        {
            if (record.OriginDevice == m_Scheduler.OriginDevice)
                throw new TransportProtocolException("An application record claims the local origin device.");
            if (record.ReliableFrameSequence == 0)
            {
                lock (m_Gate)
                    EnqueueIncoming(record);
                return;
            }

            ulong bulkOrdinal = 0;
            if (record.Lane == ScheduleLane.Bulk && !BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, record.OriginDevice, record.StreamId, out bulkOrdinal))
                throw new TransportProtocolException("A bulk frame has an invalid session-direction stream identity.");

            ulong acknowledgedThrough;
            lock (m_Gate)
            {
                if (record.Lane == ScheduleLane.Bulk && bulkOrdinal <= m_RemoteBulkStreamRetiredThrough)
                {
                    // The peer has proved it retired this stream. A late duplicate
                    // is suppressed and acknowledged without recreating its state.
                    acknowledgedThrough = record.ReliableFrameSequence;
                }
                else
                {
                    if (!m_InboundStreams.TryGetValue(record.StreamId.Value, out InboundStreamState state))
                    {
                        if (m_InboundStreams.Count >= ResumeWatermarkCodec.MaximumStreams)
                            throw new TransportBackpressureException("The bounded reliable receive-stream table is full.");
                        state = new InboundStreamState(record.StreamId);
                        m_InboundStreams.Add(record.StreamId.Value, state);
                    }

                    if (record.ReliableFrameSequence <= state.ReceivedThrough)
                    {
                        acknowledgedThrough = state.ReceivedThrough;
                    }
                    else if (record.ReliableFrameSequence > state.ReceivedThrough + 1)
                    {
                        ulong gap = record.ReliableFrameSequence - state.ReceivedThrough;
                        if (gap > MaximumOutOfOrderRecords || m_OutOfOrderRecordCount >= MaximumOutOfOrderRecords || m_OutOfOrderBytes > MaximumIncomingBytes - record.PayloadLength)
                            throw new TransportProtocolException("Reliable frame gap exceeds the bounded replay window.");
                        if (!state.Pending.ContainsKey(record.ReliableFrameSequence))
                        {
                            state.Pending.Add(record.ReliableFrameSequence, record);
                            m_OutOfOrderRecordCount++;
                            m_OutOfOrderBytes += record.PayloadLength;
                        }

                        acknowledgedThrough = state.ReceivedThrough;
                    }
                    else
                    {
                        TransportRecord current = record;
                        while (current != null)
                        {
                            bool advancesOriginSequence = ValidateOriginContinuity(current);
                            if (current.SceneId != null && (!current.SceneId.Equals(m_Scheduler.SceneId) || !current.IncarnationId.Equals(m_Scheduler.IncarnationId)))
                            {
                                EnqueueResult rejection = EnqueueScopedRejection(current.MessageId, "wrong-scene", "The record belongs to another scene incarnation.");
                                if (!rejection.Accepted)
                                {
                                    var exception = new TransportProtocolException("The session-control scheduler could not retain a required scoped rejection.");
                                    MarkFaultedLocked(exception);
                                    throw exception;
                                }
                            }
                            else
                                EnqueueIncoming(current);

                            if (advancesOriginSequence)
                                m_LastOriginSequence = current.OriginSequence;
                            state.ReceivedThrough = current.ReliableFrameSequence;
                            ulong next = checked(state.ReceivedThrough + 1);
                            if (!state.Pending.TryGetValue(next, out current))
                                current = null;
                            else
                            {
                                state.Pending.Remove(next);
                                m_OutOfOrderRecordCount--;
                                m_OutOfOrderBytes -= current.PayloadLength;
                            }
                        }

                        acknowledgedThrough = state.ReceivedThrough;
                    }
                }

                if (acknowledgedThrough > 0)
                    QueueAcknowledgement(record.StreamId, acknowledgedThrough);
            }

            SignalWriter();
        }

        private bool ValidateOriginContinuity(TransportRecord record)
        {
            if (record.Lane != ScheduleLane.Interactive && record.Lane != ScheduleLane.SceneControl)
                return false;
            if (!record.SceneId.Equals(m_Scheduler.SceneId) || !record.IncarnationId.Equals(m_Scheduler.IncarnationId))
                return false;
            if (record.OriginDevice == m_Scheduler.OriginDevice)
                throw new TransportProtocolException("A scene operation claims the local device as its origin.");
            if (record.OriginSequence != checked(m_LastOriginSequence + 1))
                throw new TransportProtocolException("The scene operation origin sequence contains a gap or duplicate.");
            return true;
        }

        private void ResetEphemeralConnectionState()
        {
            // ACK watermarks are carried by ResumeHello; pings and pongs have no
            // meaning on a replacement stream. Always put the handshake first.
            m_DirectControls.Clear();
            m_PendingAcknowledgements.Clear();
            m_PendingPings.Clear();
            m_PendingPingOrder.Clear();
            m_PendingBulkStreamRetiredThrough = null;
            m_LastWrittenBulkStreamRetiredThrough = 0;
        }

        private void EnqueueIncoming(TransportRecord record)
        {
            RetentionProgress progress = null;
            if (record.Lane == ScheduleLane.SessionControl && RetentionProgress.TryDecode(record.GetPayloadCopy(), out progress))
            {
                ulong knownOrigin = m_Scheduler.OriginDevice == OriginDevice.Desktop ? m_Scheduler.LastCommittedOriginSequence : m_LastOriginSequence;
                if (record.ReliableFrameSequence == 0 || progress.AppliedOriginThrough > knownOrigin)
                    throw new InvalidDataException("Application retention progress acknowledges an unknown origin or is unreliable.");
                progress.ValidateScope(m_Scheduler.SessionId, m_Scheduler.SceneId, m_Scheduler.IncarnationId, m_Scheduler.OriginDevice == OriginDevice.Desktop ? OriginDevice.Quest : OriginDevice.Desktop);
                if (m_LastReceivedProgress != null && (progress.AppliedOriginThrough < m_LastReceivedProgress.AppliedOriginThrough || progress.MinimumObservedCanonicalSequence < m_LastReceivedProgress.MinimumObservedCanonicalSequence || progress.RetiredCanonicalThrough < m_LastReceivedProgress.RetiredCanonicalThrough || progress.CurrentCanonicalSequence < m_LastReceivedProgress.CurrentCanonicalSequence))
                    throw new InvalidDataException("Application retention progress regressed on the receive stream.");
                // Network ACKs can outrun main-thread consumption. Keep one queued
                // notification with the latest cumulative value, validating every frame.
                if (m_IncomingProgressPlaceholder != null)
                {
                    m_LastReceivedProgress = progress;
                    m_LatestIncomingProgress = record;
                    return;
                }
            }

            int bytes = checked(TransportFrameCodec.HeaderLength + record.PayloadLength);
            if (m_Incoming.Count >= MaximumIncomingRecords || bytes > MaximumIncomingBytes - m_IncomingBytes)
                throw new TransportBackpressureException("The bounded incoming-record queue is full.");
            if (progress != null)
            {
                m_LastReceivedProgress = progress;
                m_LatestIncomingProgress = record;
                m_IncomingProgressPlaceholder = record;
            }

            m_Incoming.Enqueue(record);
            m_IncomingBytes += bytes;
            m_IncomingAvailable.Release();
        }

        private TransportRecord DequeueIncoming()
        {
            TransportRecord record = m_Incoming.Dequeue();
            if (!ReferenceEquals(record, m_IncomingProgressPlaceholder)) return record;
            record = m_LatestIncomingProgress;
            m_IncomingProgressPlaceholder = null;
            m_LatestIncomingProgress = null;
            return record;
        }

        private void QueueAcknowledgement(ReliableStreamId streamId, ulong throughSequence)
        {
            if (m_PendingAcknowledgements.TryGetValue(streamId.Value, out TransportRecord pending))
            {
                if (throughSequence > pending.ReliableFrameSequence)
                {
                    var updated = CreateAcknowledgement(streamId, throughSequence);
                    ReplacePendingControl(pending, updated);
                    m_PendingAcknowledgements[streamId.Value] = updated;
                }

                return;
            }

            TransportRecord acknowledgement = CreateAcknowledgement(streamId, throughSequence);
            if (QueueDirectControl(acknowledgement))
                m_PendingAcknowledgements.Add(streamId.Value, acknowledgement);
        }

        private bool QueueDirectControl(TransportRecord record)
        {
            if (m_DirectControls.Count >= MaximumDirectControls)
                return false;
            m_DirectControls.Enqueue(record);
            return true;
        }

        private void ReplacePendingControl(TransportRecord previous, TransportRecord replacement)
        {
            int count = m_DirectControls.Count;
            for (int i = 0; i < count; i++)
            {
                TransportRecord current = m_DirectControls.Dequeue();
                m_DirectControls.Enqueue(ReferenceEquals(current, previous) ? replacement : current);
            }
        }

        private TransportRecord CreateApplicationRecord(ReliableFrame frame)
        {
            SceneId scene = frame.Lane == ScheduleLane.SessionControl ? null : m_Scheduler.SceneId;
            IncarnationId incarnation = frame.Lane == ScheduleLane.SessionControl ? null : m_Scheduler.IncarnationId;
            ReliableStreamId stream = frame.StreamId ?? m_Scheduler.SessionControlStreamId;
            int? chunkIndex = frame.ChunkIndex;
            ushort bodySchema = frame.BodySchema;
            ulong? canonicalSequence = frame.CanonicalSequence;
            ulong? observedCanonicalSequence = frame.ObservedCanonicalSequence;
            var record = new TransportRecord(TransportMessageKind.Application, m_Scheduler.SessionId, scene, incarnation, frame.OperationId, stream, frame.ReliableFrameSequence.GetValueOrDefault(), frame.OriginSequence.GetValueOrDefault(), m_Scheduler.OriginDevice, frame.Lane, bodySchema, chunkIndex, frame.GetPayloadCopy(), canonicalSequence, observedCanonicalSequence);
            return record;
        }

        private void ValidateMutationSequences(ulong? canonicalSequence, ulong? observedCanonicalSequence)
        {
            if (m_Scheduler.OriginDevice == OriginDevice.Desktop)
            {
                if (!canonicalSequence.HasValue || canonicalSequence.Value == 0)
                    throw new ArgumentException("Desktop mutations require their authoritative nonzero canonical sequence.", nameof(canonicalSequence));
                if (observedCanonicalSequence.HasValue)
                    throw new ArgumentException("Desktop mutations cannot carry an observed canonical sequence.", nameof(observedCanonicalSequence));
                return;
            }

            if (canonicalSequence.HasValue)
                throw new ArgumentException("Quest proposals cannot carry a canonical sequence.", nameof(canonicalSequence));
            if (!observedCanonicalSequence.HasValue)
                throw new ArgumentException("Quest proposals require the latest observed canonical sequence (zero is valid before the first canonical operation).", nameof(observedCanonicalSequence));
        }

        private TransportRecord CreateResumeHello()
        {
            var watermarks = new Dictionary<ReliableStreamId, ulong>();
            foreach (InboundStreamState stream in m_InboundStreams.Values)
            {
                if (stream.ReceivedThrough > 0)
                    watermarks.Add(stream.StreamId, stream.ReceivedThrough);
            }

            return new TransportRecord(TransportMessageKind.ResumeHello, m_Scheduler.SessionId, originDevice: m_Scheduler.OriginDevice, payload: ResumeWatermarkCodec.Encode(watermarks), bulkStreamRetiredThrough: m_Scheduler.BulkStreamRetiredThrough);
        }

        private TransportRecord CreatePingRecord(Guid id, long timestamp)
        {
            byte[] payload = new byte[16];
            WriteUInt64(payload, 0, unchecked((ulong)timestamp));
            WriteUInt64(payload, 8, checked((ulong)Stopwatch.Frequency));
            return new TransportRecord(TransportMessageKind.Ping, m_Scheduler.SessionId, messageId: new OperationId(id), originDevice: m_Scheduler.OriginDevice, payload: payload);
        }

        private sealed class PendingPing
        {
            public long SentTicks { get; }
            public ulong TickFrequency { get; }

            public PendingPing(long sentTicks, ulong tickFrequency)
            {
                SentTicks = sentTicks;
                TickFrequency = tickFrequency;
            }
        }

        private TransportRecord CreateAcknowledgement(ReliableStreamId streamId, ulong throughSequence)
        {
            return new TransportRecord(TransportMessageKind.Acknowledgement, m_Scheduler.SessionId, streamId: streamId, reliableFrameSequence: throughSequence, originDevice: m_Scheduler.OriginDevice);
        }

        private TransportRecord CreateBulkStreamRetirementRecord(ulong retiredThrough)
        {
            return new TransportRecord(TransportMessageKind.BulkStreamRetirement, m_Scheduler.SessionId, originDevice: m_Scheduler.OriginDevice, bulkStreamRetiredThrough: retiredThrough);
        }

        private void RetireSentBulkWatermarksLocked()
        {
            ulong retiredThrough = m_Scheduler.BulkStreamRetiredThrough;
            if (retiredThrough > 0)
            {
                var retiredStreams = new List<Guid>();
                foreach (Guid streamValue in m_LastSentSequences.Keys)
                {
                    var streamId = new ReliableStreamId(streamValue);
                    if (BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, m_Scheduler.OriginDevice, streamId, out ulong ordinal) && ordinal <= retiredThrough)
                        retiredStreams.Add(streamValue);
                }

                for (int i = 0; i < retiredStreams.Count; i++)
                    m_LastSentSequences.Remove(retiredStreams[i]);
            }

            QueueLocalBulkStreamRetirementLocked();
        }

        private void QueueLocalBulkStreamRetirementLocked()
        {
            if (!m_HandshakeComplete)
                return;
            ulong retiredThrough = m_Scheduler.BulkStreamRetiredThrough;
            if (retiredThrough <= m_LastWrittenBulkStreamRetiredThrough)
                return;
            if (!m_PendingBulkStreamRetiredThrough.HasValue || retiredThrough > m_PendingBulkStreamRetiredThrough.Value)
                m_PendingBulkStreamRetiredThrough = retiredThrough;
        }

        private void ApplyRemoteBulkStreamRetirementLocked(ulong retiredThrough)
        {
            if (retiredThrough < m_RemoteBulkStreamRetiredThrough)
                throw new InvalidDataException("The peer bulk retirement watermark moved backwards.");
            if (retiredThrough == m_RemoteBulkStreamRetiredThrough)
                return;
            OriginDevice remoteOrigin = m_Scheduler.OriginDevice == OriginDevice.Desktop ? OriginDevice.Quest : OriginDevice.Desktop;
            var retiredStreams = new List<Guid>();
            foreach (KeyValuePair<Guid, InboundStreamState> entry in m_InboundStreams)
            {
                if (!BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, remoteOrigin, entry.Value.StreamId, out ulong ordinal) || ordinal > retiredThrough)
                    continue;
                if (entry.Value.Pending.Count > 0)
                    throw new InvalidDataException("The peer retired a bulk stream while a reliable frame gap remained.");
                retiredStreams.Add(entry.Key);
            }

            for (int i = 0; i < retiredStreams.Count; i++)
                m_InboundStreams.Remove(retiredStreams[i]);
            m_RemoteBulkStreamRetiredThrough = retiredThrough;
        }

        private bool IsLocallyRetiredBulkStream(ReliableStreamId streamId)
        {
            return BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, m_Scheduler.OriginDevice, streamId, out ulong ordinal) && ordinal <= m_Scheduler.BulkStreamRetiredThrough;
        }

        private void TrackSentSequence(ReliableStreamId streamId, ulong sequence)
        {
            if (!m_LastSentSequences.TryGetValue(streamId.Value, out ulong previous))
            {
                if (m_LastSentSequences.Count >= ResumeWatermarkCodec.MaximumStreams)
                    throw new TransportBackpressureException("The bounded sent-stream table is full.");
                m_LastSentSequences.Add(streamId.Value, sequence);
            }
            else if (sequence > previous)
                m_LastSentSequences[streamId.Value] = sequence;
        }

        private void SendScheduledPing()
        {
            if (State == PersistentTransportState.Connected)
                SendLivenessProbe();
        }

        private Guid NextGuid()
        {
            Guid value = m_GuidFactory();
            if (value == Guid.Empty)
                throw new InvalidOperationException("The transport identity factory returned an empty identity.");
            return value;
        }

        private void SignalWriter()
        {
            try
            {
                m_WriterSignal.Release();
            }
            catch (SemaphoreFullException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void CloseCurrentStream()
        {
            Stream stream;
            lock (m_Gate)
                stream = m_CurrentStream;
            try
            {
                stream?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void ThrowIfUnavailable()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(PersistentTransport));
            if (m_Faulted)
                throw new InvalidOperationException("The sync transport session is faulted.");
        }

        private bool MarkSchedulerFaultedLocked()
        {
            if (m_Scheduler.State != SchedulerState.Faulted || m_Faulted)
                return false;
            MarkFaultedLocked(CreateSchedulerFaultException());
            return true;
        }

        private TransportProtocolException CreateSchedulerFaultException() => new TransportProtocolException("The sync outgoing scheduler faulted after required work could not be retained.");

        private void MarkFaultedLocked(Exception exception)
        {
            if (m_Faulted)
                return;
            m_Faulted = true;
            m_State = PersistentTransportState.Faulted;
            m_IncomingCompleted = true;
            m_FaultException = exception ?? CreateSchedulerFaultException();
        }

        private void StopFaultedSession()
        {
            int readersToWake;
            lock (m_Gate)
            {
                if (m_FaultShutdownStarted || !m_Faulted)
                    return;
                m_FaultShutdownStarted = true;
                readersToWake = m_IncomingReaderWaiters;
            }

            m_DisposeSource.Cancel();
            CloseCurrentStream();
            SignalWriter();
            WakeIncomingReaders(readersToWake);
        }

        private void WakeIncomingReaders(int count)
        {
            if (count <= 0)
                return;
            try
            {
                m_IncomingAvailable.Release(count);
            }
            catch (SemaphoreFullException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void ThrowIncomingCompletionLocked()
        {
            if (m_Faulted)
                throw m_FaultException ?? CreateSchedulerFaultException();
            throw new ObjectDisposedException(nameof(PersistentTransport));
        }

        private static async Task ObserveLoopShutdownAsync(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            // The first completed loop's exception is captured before shutdown starts.
            // Other loop failures during stream disposal are secondary to that result.
            catch (Exception)
            {
            }
        }

        private static async Task AwaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
        {
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                Task completed = await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false);
                if (completed != task)
                    cancellationToken.ThrowIfCancellationRequested();
                await task.ConfigureAwait(false);
            }
        }

        private static void WriteUInt64(byte[] bytes, int offset, ulong value)
        {
            for (int i = 0; i < 8; i++)
                bytes[offset + i] = (byte)(value >> (8 * i));
        }

        private static ulong ReadUInt64(byte[] bytes, int offset)
        {
            ulong value = 0;
            for (int i = 0; i < 8; i++)
                value |= (ulong)bytes[offset + i] << (8 * i);
            return value;
        }

        public void Dispose()
        {
            int readersToWake;
            lock (m_Gate)
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_State = PersistentTransportState.Disposed;
                m_IncomingCompleted = true;
                readersToWake = m_IncomingReaderWaiters;
            }

            m_DisposeSource.Cancel();
            CloseCurrentStream();
            SignalWriter();
            WakeIncomingReaders(readersToWake);
        }

        private sealed class InboundStreamState
        {
            public ReliableStreamId StreamId { get; }
            public ulong ReceivedThrough;
            public SortedDictionary<ulong, TransportRecord> Pending { get; } = new SortedDictionary<ulong, TransportRecord>();

            public InboundStreamState(ReliableStreamId streamId) => StreamId = streamId;
        }
    }

    public sealed class TransportProtocolException : Exception
    {
        public TransportProtocolException(string message) : base(message)
        {
        }
    }

    public sealed class TransportBackpressureException : IOException
    {
        public TransportBackpressureException(string message) : base(message)
        {
        }
    }
}
