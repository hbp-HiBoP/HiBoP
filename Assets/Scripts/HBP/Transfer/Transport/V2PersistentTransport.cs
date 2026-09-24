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
    public enum V2PersistentTransportState : byte
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
    public sealed class V2PersistentTransport : IDisposable
    {
        private const int MaximumDirectControls = 64;
        private const int MaximumOutOfOrderRecords = 128;
        private const int MaximumIncomingRecords = 256;
        private const int MaximumIncomingBytes = 4 * 1024 * 1024;
        private const int MaximumPendingPingSamples = 32;
        private const int MaximumPayloadFromPeerBytes = V2TransportFrameCodec.MaximumPayloadBytes;

        private readonly object m_Gate = new object();
        private readonly V2OutgoingScheduler m_Scheduler;
        private readonly TimeSpan m_LivenessInterval;
        private readonly CancellationTokenSource m_DisposeSource = new CancellationTokenSource();
        private readonly SemaphoreSlim m_WriterSignal = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim m_IncomingAvailable = new SemaphoreSlim(0);
        private readonly Queue<V2TransportRecord> m_DirectControls = new Queue<V2TransportRecord>();
        private readonly Dictionary<Guid, V2TransportRecord> m_PendingAcknowledgements = new Dictionary<Guid, V2TransportRecord>();
        private readonly Dictionary<Guid, ulong> m_LastSentSequences = new Dictionary<Guid, ulong>();
        private readonly Dictionary<Guid, InboundStreamState> m_InboundStreams = new Dictionary<Guid, InboundStreamState>();
        private readonly Queue<Guid> m_PendingPingOrder = new Queue<Guid>();
        private readonly Dictionary<Guid, long> m_PendingPings = new Dictionary<Guid, long>();
        private readonly Queue<V2TransportRecord> m_Incoming = new Queue<V2TransportRecord>();
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
        private V2PersistentTransportState m_State = V2PersistentTransportState.Disconnected;
        private TimeSpan? m_LastRoundTrip;

        public V2PersistentTransportState State
        {
            get
            {
                lock (m_Gate) return m_State;
            }
        }

        public TimeSpan? LastRoundTrip
        {
            get
            {
                lock (m_Gate) return m_LastRoundTrip;
            }
        }

        public V2PersistentTransport(V2OutgoingScheduler scheduler, TimeSpan? livenessInterval = null, Func<Guid> guidFactory = null)
        {
            m_Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            m_LivenessInterval = livenessInterval ?? TimeSpan.FromSeconds(15);
            if (m_LivenessInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(livenessInterval));
            if (scheduler.Limits.BulkChunkBytes > V2TransportFrameCodec.MaximumPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(scheduler), "The scheduler bulk chunk exceeds the transport frame bound.");
            m_GuidFactory = guidFactory ?? Guid.NewGuid;
        }

        public V2EnqueueResult EnqueueMutation(V2Mutation mutation, ulong? canonicalSequence, ulong? observedCanonicalSequence, bool coalesciblePreview = true, OperationId operationId = null)
        {
            ValidateMutationSequences(canonicalSequence, observedCanonicalSequence);
            V2EnqueueResult result;
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

        public V2EnqueueResult EnqueueSceneOperation(byte[] encodedBody, V2ScheduleDescriptor descriptor, bool coalesciblePreview = false, bool structural = false, bool final = false, ushort bodySchema = 1, OperationId operationId = null, ulong? canonicalSequence = null, ulong? observedCanonicalSequence = null)
        {
            V2EnqueueResult result;
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

        public V2EnqueueResult EnqueueSessionControl(byte[] payload, V2DeliveryReliability reliability)
        {
            V2EnqueueResult result;
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

        public V2SchedulerMetrics SnapshotMetrics()
        {
            lock (m_Gate)
                return m_Scheduler.SnapshotMetrics();
        }

        /// <summary>Enqueues a reliable, operation-scoped rejection without ending the session.</summary>
        public V2EnqueueResult EnqueueScopedRejection(OperationId operationId, string code, string diagnostic)
        {
            byte[] payload = V2ScopedRejectionCodec.Encode(operationId, code, diagnostic);
            return EnqueueSessionControl(payload, V2DeliveryReliability.Reliable);
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
                m_PendingPings.Add(id, timestamp);
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
                throw new InvalidOperationException("A v2 transport session can own only one active connection.");

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_DisposeSource.Token))
            {
                CancellationToken token = linked.Token;
                TaskCompletionSource<bool> handshakeReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    lock (m_Gate)
                    {
                        ThrowIfUnavailable();
                        if (m_Scheduler.State == V2SchedulerState.DisconnectedGrace && !m_Scheduler.TryResume())
                            throw new InvalidOperationException("The reconnect grace expired before the authenticated stream resumed.");
                        ResetEphemeralConnectionState();
                        m_HandshakeComplete = false;
                        m_CurrentStream = authenticatedStream;
                        m_State = V2PersistentTransportState.Connected;
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
                                failure = new EndOfStreamException("A v2 transport loop ended unexpectedly.");
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
                            throw failure ?? new V2TransportProtocolException("The v2 transport session faulted.");
                        }

                        if (callerCancelled)
                        {
                            lock (m_Gate)
                            {
                                m_CurrentStream = null;
                                if (!m_Disposed && !m_Faulted)
                                {
                                    m_Scheduler.BeginDisconnectGrace();
                                    m_State = m_Scheduler.State == V2SchedulerState.DisconnectedGrace ? V2PersistentTransportState.DisconnectedGrace : V2PersistentTransportState.Disconnected;
                                }
                            }

                            return;
                        }

                        if (failure is InvalidDataException || failure is V2TransportProtocolException)
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
                                    m_State = V2PersistentTransportState.DisconnectedGrace;
                                }

                                m_CurrentStream = null;
                            }

                            throw failure;
                        }

                        Exception terminalFailure = failure ?? new InvalidOperationException("A v2 transport loop failed without an exception.");
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
        public async Task<V2TransportRecord> ReadIncomingAsync(CancellationToken cancellationToken)
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
                        V2TransportRecord record = m_Incoming.Dequeue();
                        m_IncomingBytes -= checked(V2TransportFrameCodec.HeaderLength + record.PayloadLength);
                        return record;
                    }

                    if (m_IncomingCompleted)
                        ThrowIncomingCompletionLocked();
                }
            }
        }

        private async Task ReadLoopAsync(Stream stream, TaskCompletionSource<bool> handshakeReady, CancellationToken cancellationToken)
        {
            bool receivedHello = false;
            while (true)
            {
                V2TransportRecord record = await V2TransportFrameCodec.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
                if (record == null)
                    throw new EndOfStreamException("The authenticated v2 stream closed.");
                if (!record.SessionId.Equals(m_Scheduler.SessionId))
                    throw new InvalidDataException("The transport record belongs to another session.");

                if (record.Kind == V2TransportMessageKind.ResumeHello)
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
                    case V2TransportMessageKind.Application:
                        ProcessApplicationRecord(record);
                        break;
                    case V2TransportMessageKind.Acknowledgement:
                        ProcessAcknowledgement(record);
                        break;
                    case V2TransportMessageKind.Ping:
                        ProcessPing(record);
                        break;
                    case V2TransportMessageKind.Pong:
                        ProcessPong(record);
                        break;
                    case V2TransportMessageKind.BulkStreamRetirement:
                        ProcessBulkStreamRetirement(record);
                        break;
                    default:
                        throw new InvalidDataException("Unsupported v2 transport record kind.");
                }
            }
        }

        private async Task WriteLoopAsync(Stream stream, CancellationToken cancellationToken)
        {
            while (true)
            {
                bool wroteAny = false;
                while (true)
                {
                    V2TransportRecord record;
                    lock (m_Gate)
                    {
                        if (m_Scheduler.State == V2SchedulerState.Faulted)
                        {
                            MarkFaultedLocked(CreateSchedulerFaultException());
                            throw m_FaultException;
                        }

                        if (m_DirectControls.Count > 0)
                        {
                            record = m_DirectControls.Dequeue();
                            if (record.Kind == V2TransportMessageKind.Acknowledgement)
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
                        else if (m_HandshakeComplete && m_Scheduler.TryGetNextTransmission(out V2TransmissionAttempt transmission))
                        {
                            record = CreateApplicationRecord(transmission.Frame);
                            if (record.ReliableFrameSequence > 0)
                                TrackSentSequence(record.StreamId, record.ReliableFrameSequence);
                        }
                        else
                            record = null;

                        if (record == null && m_Scheduler.State == V2SchedulerState.Faulted)
                        {
                            MarkFaultedLocked(CreateSchedulerFaultException());
                            throw m_FaultException;
                        }
                    }

                    if (record == null)
                        break;
                    await V2TransportFrameCodec.WriteAsync(stream, record, cancellationToken).ConfigureAwait(false);
                    wroteAny = true;
                }

                if (!wroteAny)
                    await m_WriterSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task LivenessLoopAsync(Task handshakeReady, CancellationToken cancellationToken)
        {
            await AwaitWithCancellationAsync(handshakeReady, cancellationToken).ConfigureAwait(false);
            while (true)
            {
                await Task.Delay(m_LivenessInterval, cancellationToken).ConfigureAwait(false);
                SendLivenessProbe();
            }
        }

        private void ProcessResumeHello(V2TransportRecord record)
        {
            Dictionary<ReliableStreamId, ulong> peerWatermarks = V2ResumeWatermarkCodec.Decode(record.GetPayloadCopy());
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

        private void ProcessAcknowledgement(V2TransportRecord record)
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

        private void ProcessBulkStreamRetirement(V2TransportRecord record)
        {
            lock (m_Gate)
            {
                if (record.OriginDevice == m_Scheduler.OriginDevice)
                    throw new InvalidDataException("Bulk retirement claims the local origin device.");
                ApplyRemoteBulkStreamRetirementLocked(record.BulkStreamRetiredThrough);
            }
        }

        private void ProcessPing(V2TransportRecord record)
        {
            byte[] request = record.GetPayloadCopy();
            byte[] response = new byte[32];
            Buffer.BlockCopy(request, 0, response, 0, 16);
            WriteUInt64(response, 16, unchecked((ulong)Stopwatch.GetTimestamp()));
            WriteUInt64(response, 24, checked((ulong)Stopwatch.Frequency));
            var pong = new V2TransportRecord(V2TransportMessageKind.Pong, m_Scheduler.SessionId, messageId: record.MessageId, originDevice: m_Scheduler.OriginDevice, payload: response);
            lock (m_Gate)
                QueueDirectControl(pong);
            SignalWriter();
        }

        private void ProcessPong(V2TransportRecord record)
        {
            byte[] payload = record.GetPayloadCopy();
            long sentAt = unchecked((long)ReadUInt64(payload, 0));
            ulong sentFrequency = ReadUInt64(payload, 8);
            if (sentFrequency != checked((ulong)Stopwatch.Frequency))
                return;
            lock (m_Gate)
            {
                if (!m_PendingPings.TryGetValue(record.MessageId.Value, out long expectedSentAt) || expectedSentAt != sentAt)
                    return;
                m_PendingPings.Remove(record.MessageId.Value);
                long elapsed = Stopwatch.GetTimestamp() - sentAt;
                if (elapsed >= 0)
                    m_LastRoundTrip = TimeSpan.FromSeconds((double)elapsed / Stopwatch.Frequency);
            }
        }

        private void ProcessApplicationRecord(V2TransportRecord record)
        {
            if (record.OriginDevice == m_Scheduler.OriginDevice)
                throw new V2TransportProtocolException("An application record claims the local origin device.");
            if (record.ReliableFrameSequence == 0)
            {
                lock (m_Gate)
                    EnqueueIncoming(record);
                return;
            }

            ulong bulkOrdinal = 0;
            if (record.Lane == V2ScheduleLane.Bulk && !V2BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, record.OriginDevice, record.StreamId, out bulkOrdinal))
                throw new V2TransportProtocolException("A bulk frame has an invalid session-direction stream identity.");

            ulong acknowledgedThrough;
            lock (m_Gate)
            {
                if (record.Lane == V2ScheduleLane.Bulk && bulkOrdinal <= m_RemoteBulkStreamRetiredThrough)
                {
                    // The peer has proved it retired this stream. A late duplicate
                    // is suppressed and acknowledged without recreating its state.
                    acknowledgedThrough = record.ReliableFrameSequence;
                }
                else
                {
                    if (!m_InboundStreams.TryGetValue(record.StreamId.Value, out InboundStreamState state))
                    {
                        if (m_InboundStreams.Count >= V2ResumeWatermarkCodec.MaximumStreams)
                            throw new V2TransportBackpressureException("The bounded reliable receive-stream table is full.");
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
                            throw new V2TransportProtocolException("Reliable frame gap exceeds the bounded replay window.");
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
                        V2TransportRecord current = record;
                        while (current != null)
                        {
                            bool advancesOriginSequence = ValidateOriginContinuity(current);
                            if (current.SceneId != null && (!current.SceneId.Equals(m_Scheduler.SceneId) || !current.IncarnationId.Equals(m_Scheduler.IncarnationId)))
                            {
                                V2EnqueueResult rejection = EnqueueScopedRejection(current.MessageId, "wrong-scene", "The record belongs to another scene incarnation.");
                                if (!rejection.Accepted)
                                {
                                    var exception = new V2TransportProtocolException("The session-control scheduler could not retain a required scoped rejection.");
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

        private bool ValidateOriginContinuity(V2TransportRecord record)
        {
            if (record.Lane != V2ScheduleLane.Interactive && record.Lane != V2ScheduleLane.SceneControl)
                return false;
            if (!record.SceneId.Equals(m_Scheduler.SceneId) || !record.IncarnationId.Equals(m_Scheduler.IncarnationId))
                return false;
            if (record.OriginDevice == m_Scheduler.OriginDevice)
                throw new V2TransportProtocolException("A scene operation claims the local device as its origin.");
            if (record.OriginSequence != checked(m_LastOriginSequence + 1))
                throw new V2TransportProtocolException("The scene operation origin sequence contains a gap or duplicate.");
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

        private void EnqueueIncoming(V2TransportRecord record)
        {
            int bytes = checked(V2TransportFrameCodec.HeaderLength + record.PayloadLength);
            if (m_Incoming.Count >= MaximumIncomingRecords || bytes > MaximumIncomingBytes - m_IncomingBytes)
                throw new V2TransportBackpressureException("The bounded incoming-record queue is full.");
            m_Incoming.Enqueue(record);
            m_IncomingBytes += bytes;
            m_IncomingAvailable.Release();
        }

        private void QueueAcknowledgement(ReliableStreamId streamId, ulong throughSequence)
        {
            if (m_PendingAcknowledgements.TryGetValue(streamId.Value, out V2TransportRecord pending))
            {
                if (throughSequence > pending.ReliableFrameSequence)
                {
                    var updated = CreateAcknowledgement(streamId, throughSequence);
                    ReplacePendingControl(pending, updated);
                    m_PendingAcknowledgements[streamId.Value] = updated;
                }

                return;
            }

            V2TransportRecord acknowledgement = CreateAcknowledgement(streamId, throughSequence);
            if (QueueDirectControl(acknowledgement))
                m_PendingAcknowledgements.Add(streamId.Value, acknowledgement);
        }

        private bool QueueDirectControl(V2TransportRecord record)
        {
            if (m_DirectControls.Count >= MaximumDirectControls)
                return false;
            m_DirectControls.Enqueue(record);
            return true;
        }

        private void ReplacePendingControl(V2TransportRecord previous, V2TransportRecord replacement)
        {
            int count = m_DirectControls.Count;
            for (int i = 0; i < count; i++)
            {
                V2TransportRecord current = m_DirectControls.Dequeue();
                m_DirectControls.Enqueue(ReferenceEquals(current, previous) ? replacement : current);
            }
        }

        private V2TransportRecord CreateApplicationRecord(V2ReliableFrame frame)
        {
            SceneId scene = frame.Lane == V2ScheduleLane.SessionControl ? null : m_Scheduler.SceneId;
            IncarnationId incarnation = frame.Lane == V2ScheduleLane.SessionControl ? null : m_Scheduler.IncarnationId;
            ReliableStreamId stream = frame.StreamId ?? m_Scheduler.SessionControlStreamId;
            int? chunkIndex = frame.ChunkIndex;
            ushort bodySchema = frame.BulkDescriptor?.BodySchema ?? (frame.Lane == V2ScheduleLane.SessionControl ? (ushort)0 : (ushort)1);
            ulong? canonicalSequence = frame.CanonicalSequence;
            ulong? observedCanonicalSequence = frame.ObservedCanonicalSequence;
            var record = new V2TransportRecord(V2TransportMessageKind.Application, m_Scheduler.SessionId, scene, incarnation, frame.OperationId, stream, frame.ReliableFrameSequence.GetValueOrDefault(), frame.OriginSequence.GetValueOrDefault(), m_Scheduler.OriginDevice, frame.Lane, bodySchema, chunkIndex, frame.GetPayloadCopy(), canonicalSequence, observedCanonicalSequence);
            return record;
        }

        private void ValidateMutationSequences(ulong? canonicalSequence, ulong? observedCanonicalSequence)
        {
            if (m_Scheduler.OriginDevice == V2OriginDevice.Desktop)
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

        private V2TransportRecord CreateResumeHello()
        {
            var watermarks = new Dictionary<ReliableStreamId, ulong>();
            foreach (InboundStreamState stream in m_InboundStreams.Values)
            {
                if (stream.ReceivedThrough > 0)
                    watermarks.Add(stream.StreamId, stream.ReceivedThrough);
            }

            return new V2TransportRecord(V2TransportMessageKind.ResumeHello, m_Scheduler.SessionId, originDevice: m_Scheduler.OriginDevice, payload: V2ResumeWatermarkCodec.Encode(watermarks), bulkStreamRetiredThrough: m_Scheduler.BulkStreamRetiredThrough);
        }

        private V2TransportRecord CreatePingRecord(Guid id, long timestamp)
        {
            byte[] payload = new byte[16];
            WriteUInt64(payload, 0, unchecked((ulong)timestamp));
            WriteUInt64(payload, 8, checked((ulong)Stopwatch.Frequency));
            return new V2TransportRecord(V2TransportMessageKind.Ping, m_Scheduler.SessionId, messageId: new OperationId(id), originDevice: m_Scheduler.OriginDevice, payload: payload);
        }

        private V2TransportRecord CreateAcknowledgement(ReliableStreamId streamId, ulong throughSequence)
        {
            return new V2TransportRecord(V2TransportMessageKind.Acknowledgement, m_Scheduler.SessionId, streamId: streamId, reliableFrameSequence: throughSequence, originDevice: m_Scheduler.OriginDevice);
        }

        private V2TransportRecord CreateBulkStreamRetirementRecord(ulong retiredThrough)
        {
            return new V2TransportRecord(V2TransportMessageKind.BulkStreamRetirement, m_Scheduler.SessionId, originDevice: m_Scheduler.OriginDevice, bulkStreamRetiredThrough: retiredThrough);
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
                    if (V2BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, m_Scheduler.OriginDevice, streamId, out ulong ordinal) && ordinal <= retiredThrough)
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
            V2OriginDevice remoteOrigin = m_Scheduler.OriginDevice == V2OriginDevice.Desktop ? V2OriginDevice.Quest : V2OriginDevice.Desktop;
            var retiredStreams = new List<Guid>();
            foreach (KeyValuePair<Guid, InboundStreamState> entry in m_InboundStreams)
            {
                if (!V2BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, remoteOrigin, entry.Value.StreamId, out ulong ordinal) || ordinal > retiredThrough)
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
            return V2BulkStreamIdentityCodec.TryGetOrdinal(m_Scheduler.SessionId, m_Scheduler.OriginDevice, streamId, out ulong ordinal) && ordinal <= m_Scheduler.BulkStreamRetiredThrough;
        }

        private void TrackSentSequence(ReliableStreamId streamId, ulong sequence)
        {
            if (!m_LastSentSequences.TryGetValue(streamId.Value, out ulong previous))
            {
                if (m_LastSentSequences.Count >= V2ResumeWatermarkCodec.MaximumStreams)
                    throw new V2TransportBackpressureException("The bounded sent-stream table is full.");
                m_LastSentSequences.Add(streamId.Value, sequence);
            }
            else if (sequence > previous)
                m_LastSentSequences[streamId.Value] = sequence;
        }

        private void SendScheduledPing()
        {
            if (State == V2PersistentTransportState.Connected)
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
                throw new ObjectDisposedException(nameof(V2PersistentTransport));
            if (m_Faulted)
                throw new InvalidOperationException("The v2 transport session is faulted.");
        }

        private bool MarkSchedulerFaultedLocked()
        {
            if (m_Scheduler.State != V2SchedulerState.Faulted || m_Faulted)
                return false;
            MarkFaultedLocked(CreateSchedulerFaultException());
            return true;
        }

        private V2TransportProtocolException CreateSchedulerFaultException() => new V2TransportProtocolException("The v2 outgoing scheduler faulted after required work could not be retained.");

        private void MarkFaultedLocked(Exception exception)
        {
            if (m_Faulted)
                return;
            m_Faulted = true;
            m_State = V2PersistentTransportState.Faulted;
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
            throw new ObjectDisposedException(nameof(V2PersistentTransport));
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
                m_State = V2PersistentTransportState.Disposed;
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
            public SortedDictionary<ulong, V2TransportRecord> Pending { get; } = new SortedDictionary<ulong, V2TransportRecord>();

            public InboundStreamState(ReliableStreamId streamId) => StreamId = streamId;
        }
    }

    public sealed class V2TransportProtocolException : Exception
    {
        public V2TransportProtocolException(string message) : base(message)
        {
        }
    }

    public sealed class V2TransportBackpressureException : IOException
    {
        public V2TransportBackpressureException(string message) : base(message)
        {
        }
    }
}
