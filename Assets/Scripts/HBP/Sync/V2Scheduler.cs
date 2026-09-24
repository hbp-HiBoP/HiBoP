using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HBP.Sync
{
    public enum V2ScheduleLane : byte
    {
        SessionControl = 1,
        SceneControl = 2,
        Interactive = 3,
        Bulk = 4
    }

    public enum V2DeliveryReliability : byte
    {
        Ephemeral = 0,
        Reliable = 1
    }

    public enum V2SchedulerState : byte
    {
        Online = 0,
        DisconnectedGrace = 1,
        Offline = 2,
        Faulted = 3
    }

    public enum V2EnqueueDisposition : byte
    {
        Accepted = 0,
        ReplacedUnsent = 1,
        Backpressured = 2,
        Rejected = 3,
        SessionFaulted = 4
    }

    public sealed class V2SchedulerLimits
    {
        public const int DefaultInlineThresholdBytes = 4096;
        public const int DefaultBulkChunkBytes = 16 * 1024;
        public const int DefaultInteractiveBurst = 8;

        public int InlineThresholdBytes { get; }
        public int BulkChunkBytes { get; }
        public int InteractiveBurst { get; }
        public int MaxSceneQueuedRecords { get; }
        public int MaxSceneQueuedBytes { get; }
        public int ReservedSceneControlRecords { get; }
        public int ReservedSceneControlBytes { get; }
        public int MaxSessionControlQueuedRecords { get; }
        public int MaxSessionControlQueuedBytes { get; }
        public int ReservedSessionControlRecords { get; }
        public int ReservedSessionControlBytes { get; }
        public int MaxReliableFrames { get; }
        public int MaxReliableBytes { get; }
        public int ReservedReliableFrames { get; }
        public int ReservedReliableBytes { get; }
        public int MaxBulkTransfers { get; }
        public int MaxBulkBodyBytesPerTransfer { get; }
        public int MaxBulkBodyBytesTotal { get; }
        public int MaxTouchedKeysPerRecord { get; }

        public V2SchedulerLimits(int inlineThresholdBytes = DefaultInlineThresholdBytes, int bulkChunkBytes = DefaultBulkChunkBytes, int interactiveBurst = DefaultInteractiveBurst, int maxSceneQueuedRecords = 256, int maxSceneQueuedBytes = 256 * 1024, int reservedSceneControlRecords = 16, int reservedSceneControlBytes = 16 * 1024, int maxSessionControlQueuedRecords = 64, int maxSessionControlQueuedBytes = 64 * 1024, int reservedSessionControlRecords = 8, int reservedSessionControlBytes = 8 * 1024, int maxReliableFrames = 128, int maxReliableBytes = 256 * 1024, int reservedReliableFrames = 8, int reservedReliableBytes = 16 * 1024, int maxBulkTransfers = 8, int maxBulkBodyBytesPerTransfer = 16 * 1024 * 1024, int maxBulkBodyBytesTotal = 32 * 1024 * 1024, int maxTouchedKeysPerRecord = 128)
        {
            ValidatePositive(inlineThresholdBytes, nameof(inlineThresholdBytes));
            ValidatePositive(bulkChunkBytes, nameof(bulkChunkBytes));
            ValidatePositive(interactiveBurst, nameof(interactiveBurst));
            ValidatePositive(maxSceneQueuedRecords, nameof(maxSceneQueuedRecords));
            ValidatePositive(maxSceneQueuedBytes, nameof(maxSceneQueuedBytes));
            ValidatePositive(maxSessionControlQueuedRecords, nameof(maxSessionControlQueuedRecords));
            ValidatePositive(maxSessionControlQueuedBytes, nameof(maxSessionControlQueuedBytes));
            ValidatePositive(maxReliableFrames, nameof(maxReliableFrames));
            ValidatePositive(maxReliableBytes, nameof(maxReliableBytes));
            ValidatePositive(maxBulkTransfers, nameof(maxBulkTransfers));
            ValidatePositive(maxBulkBodyBytesPerTransfer, nameof(maxBulkBodyBytesPerTransfer));
            ValidatePositive(maxBulkBodyBytesTotal, nameof(maxBulkBodyBytesTotal));
            ValidatePositive(maxTouchedKeysPerRecord, nameof(maxTouchedKeysPerRecord));
            ValidateReserve(reservedSceneControlRecords, maxSceneQueuedRecords, nameof(reservedSceneControlRecords));
            ValidateReserve(reservedSceneControlBytes, maxSceneQueuedBytes, nameof(reservedSceneControlBytes));
            ValidateReserve(reservedSessionControlRecords, maxSessionControlQueuedRecords, nameof(reservedSessionControlRecords));
            ValidateReserve(reservedSessionControlBytes, maxSessionControlQueuedBytes, nameof(reservedSessionControlBytes));
            ValidateReserve(reservedReliableFrames, maxReliableFrames, nameof(reservedReliableFrames));
            ValidateReserve(reservedReliableBytes, maxReliableBytes, nameof(reservedReliableBytes));
            if (bulkChunkBytes > maxReliableBytes - reservedReliableBytes)
                throw new ArgumentOutOfRangeException(nameof(bulkChunkBytes), "A bulk chunk must fit in the non-reserved reliable window.");
            if (maxBulkBodyBytesTotal < maxBulkBodyBytesPerTransfer)
                throw new ArgumentOutOfRangeException(nameof(maxBulkBodyBytesTotal));

            InlineThresholdBytes = inlineThresholdBytes;
            BulkChunkBytes = bulkChunkBytes;
            InteractiveBurst = interactiveBurst;
            MaxSceneQueuedRecords = maxSceneQueuedRecords;
            MaxSceneQueuedBytes = maxSceneQueuedBytes;
            ReservedSceneControlRecords = reservedSceneControlRecords;
            ReservedSceneControlBytes = reservedSceneControlBytes;
            MaxSessionControlQueuedRecords = maxSessionControlQueuedRecords;
            MaxSessionControlQueuedBytes = maxSessionControlQueuedBytes;
            ReservedSessionControlRecords = reservedSessionControlRecords;
            ReservedSessionControlBytes = reservedSessionControlBytes;
            MaxReliableFrames = maxReliableFrames;
            MaxReliableBytes = maxReliableBytes;
            ReservedReliableFrames = reservedReliableFrames;
            ReservedReliableBytes = reservedReliableBytes;
            MaxBulkTransfers = maxBulkTransfers;
            MaxBulkBodyBytesPerTransfer = maxBulkBodyBytesPerTransfer;
            MaxBulkBodyBytesTotal = maxBulkBodyBytesTotal;
            MaxTouchedKeysPerRecord = maxTouchedKeysPerRecord;
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidateReserve(int reserve, int capacity, string name)
        {
            if (reserve < 0 || reserve >= capacity)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    /// <summary>
    /// Scheduling metadata produced from validated operation values. Keys cannot be
    /// supplied as arbitrary strings; they originate in T01 mutation descriptors.
    /// </summary>
    public sealed class V2ScheduleDescriptor
    {
        private readonly ReadOnlyCollection<V2TouchedKey> m_TouchedKeys;

        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public V2TouchedKey CoalescingKey { get; }
        public IReadOnlyList<V2TouchedKey> TouchedKeys => m_TouchedKeys;
        public V2BarrierScope BarrierScope { get; }

        private V2ScheduleDescriptor(SceneId sceneId, IncarnationId incarnationId, V2TouchedKey coalescingKey, IList<V2TouchedKey> touchedKeys, V2BarrierScope barrierScope)
        {
            SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            CoalescingKey = coalescingKey;
            BarrierScope = barrierScope;

            var copy = new List<V2TouchedKey>(touchedKeys?.Count ?? 0);
            if (touchedKeys != null)
            {
                for (int i = 0; i < touchedKeys.Count; i++)
                {
                    V2TouchedKey key = touchedKeys[i] ?? throw new ArgumentException("A touched key cannot be null.", nameof(touchedKeys));
                    if (!key.SceneId.Equals(sceneId) || !key.IncarnationId.Equals(incarnationId))
                        throw new ArgumentException("All scheduling keys must belong to the descriptor scene incarnation.", nameof(touchedKeys));
                    if (!copy.Contains(key))
                        copy.Add(key);
                }
            }

            if (coalescingKey != null && !copy.Contains(coalescingKey))
                throw new ArgumentException("The coalescing key must also be a touched key.", nameof(coalescingKey));
            m_TouchedKeys = new ReadOnlyCollection<V2TouchedKey>(copy);
        }

        public static V2ScheduleDescriptor ForMutation(SceneId sceneId, IncarnationId incarnationId, V2Mutation mutation)
        {
            if (mutation == null)
                throw new ArgumentNullException(nameof(mutation));
            V2MutationDescriptor descriptor = new V2MutationDescriptor(sceneId, incarnationId, mutation);
            return new V2ScheduleDescriptor(sceneId, incarnationId, descriptor.CoalescingKey, descriptor.TouchedKeys as IList<V2TouchedKey>, V2BarrierScope.None);
        }

        public static V2ScheduleDescriptor ForBarrier(SceneId sceneId, IncarnationId incarnationId, IEnumerable<V2ScheduleDescriptor> affectedDescriptors, V2BarrierScope scope, int maximumTouchedKeys = 128)
        {
            if (scope != V2BarrierScope.TouchedKeys && scope != V2BarrierScope.AllScene)
                throw new ArgumentOutOfRangeException(nameof(scope));
            if (maximumTouchedKeys <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumTouchedKeys));

            var keys = new List<V2TouchedKey>();
            if (affectedDescriptors != null)
            {
                foreach (V2ScheduleDescriptor descriptor in affectedDescriptors)
                {
                    if (descriptor == null)
                        throw new ArgumentException("A scheduling descriptor cannot be null.", nameof(affectedDescriptors));
                    if (!descriptor.SceneId.Equals(sceneId) || !descriptor.IncarnationId.Equals(incarnationId))
                        throw new ArgumentException("Barrier inputs must belong to one scene incarnation.", nameof(affectedDescriptors));
                    for (int i = 0; i < descriptor.TouchedKeys.Count; i++)
                    {
                        V2TouchedKey key = descriptor.TouchedKeys[i];
                        if (!keys.Contains(key))
                            keys.Add(key);
                        if (keys.Count > maximumTouchedKeys)
                            throw new ArgumentOutOfRangeException(nameof(affectedDescriptors), "Barrier exceeds the touched-key bound.");
                    }
                }
            }

            if (scope == V2BarrierScope.TouchedKeys && keys.Count == 0)
                throw new ArgumentException("A touched-key barrier requires at least one validated touched key.", nameof(affectedDescriptors));
            return new V2ScheduleDescriptor(sceneId, incarnationId, null, keys, scope);
        }

        internal byte[] EncodeTouchedKeyFingerprints()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(checked((ushort)m_TouchedKeys.Count));
                for (int i = 0; i < m_TouchedKeys.Count; i++)
                {
                    byte[] fingerprint = Fingerprint(m_TouchedKeys[i]);
                    writer.Write(fingerprint);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        internal static byte[] Fingerprint(V2TouchedKey key)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((byte)key.Kind);
                WriteBoundedString(writer, key.ColumnId?.Value);
                WriteBoundedString(writer, key.SiteId?.Value);
                WriteBoundedString(writer, key.CutId?.Value);
                writer.Flush();
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] digest = sha.ComputeHash(stream.ToArray());
                    var shortFingerprint = new byte[16];
                    Buffer.BlockCopy(digest, 0, shortFingerprint, 0, shortFingerprint.Length);
                    return shortFingerprint;
                }
            }
        }

        private static void WriteBoundedString(BinaryWriter writer, string value)
        {
            if (value == null)
            {
                writer.Write((ushort)0);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }
    }

    public sealed class V2BulkTransferDescriptor
    {
        private readonly byte[] m_ContentDigest;

        public OperationId OperationId { get; }
        public ReliableStreamId BulkStreamId { get; }
        public ushort BodySchema { get; }
        public int TotalLength { get; }
        public int ChunkSize { get; }
        public int ChunkCount { get; }
        public V2ScheduleDescriptor Scheduling { get; }
        public byte[] GetContentDigestCopy() => (byte[])m_ContentDigest.Clone();

        internal V2BulkTransferDescriptor(OperationId operationId, ReliableStreamId bulkStreamId, ushort bodySchema, byte[] body, int chunkSize, V2ScheduleDescriptor scheduling, byte[] digest)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            BulkStreamId = bulkStreamId ?? throw new ArgumentNullException(nameof(bulkStreamId));
            BodySchema = bodySchema;
            TotalLength = body?.Length ?? throw new ArgumentNullException(nameof(body));
            ChunkSize = chunkSize;
            ChunkCount = checked((TotalLength + chunkSize - 1) / chunkSize);
            Scheduling = scheduling ?? throw new ArgumentNullException(nameof(scheduling));
            m_ContentDigest = (byte[])digest.Clone();
        }

        internal byte[] Encode()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((ushort)1);
                writer.Write(OperationId.ToByteArray());
                writer.Write(BodySchema);
                writer.Write(checked((uint)TotalLength));
                writer.Write(checked((uint)ChunkSize));
                writer.Write(checked((uint)ChunkCount));
                writer.Write(BulkStreamId.ToByteArray());
                writer.Write((byte)m_ContentDigest.Length);
                writer.Write(m_ContentDigest);
                writer.Write((byte)Scheduling.BarrierScope);
                writer.Write(Scheduling.EncodeTouchedKeyFingerprints());
                writer.Flush();
                return stream.ToArray();
            }
        }
    }

    public sealed class V2EnqueueResult
    {
        public bool Accepted { get; }
        public V2EnqueueDisposition Disposition { get; }
        public OperationId OperationId { get; }
        public ReliableStreamId BulkStreamId { get; }

        internal V2EnqueueResult(bool accepted, V2EnqueueDisposition disposition, OperationId operationId = null, ReliableStreamId bulkStreamId = null)
        {
            Accepted = accepted;
            Disposition = disposition;
            OperationId = operationId;
            BulkStreamId = bulkStreamId;
        }
    }

    public sealed class V2ReliableFrame
    {
        private readonly byte[] m_Payload;

        public ReliableStreamId StreamId { get; }
        public ulong? ReliableFrameSequence { get; }
        public ulong? OriginSequence { get; }
        public OperationId OperationId { get; }
        public V2ScheduleLane Lane { get; }
        public V2DeliveryReliability Reliability { get; }
        public int WireBytes { get; }
        public V2BulkTransferDescriptor BulkDescriptor { get; }
        public int? ChunkIndex { get; }
        public int? ChunkCount { get; }
        public int? ChunkOffset { get; }
        public int PayloadLength => m_Payload.Length;

        internal V2ReliableFrame(ReliableStreamId streamId, ulong? reliableFrameSequence, ulong? originSequence, OperationId operationId, V2ScheduleLane lane, V2DeliveryReliability reliability, byte[] payload, V2BulkTransferDescriptor bulkDescriptor = null, int? chunkIndex = null, int? chunkCount = null, int? chunkOffset = null)
        {
            StreamId = streamId;
            ReliableFrameSequence = reliableFrameSequence;
            OriginSequence = originSequence;
            OperationId = operationId;
            Lane = lane;
            Reliability = reliability;
            m_Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            WireBytes = checked(payload.Length + V2MutationEnvelopeCodec.HeaderLength);
            BulkDescriptor = bulkDescriptor;
            ChunkIndex = chunkIndex;
            ChunkCount = chunkCount;
            ChunkOffset = chunkOffset;
        }

        public byte[] GetPayloadCopy() => (byte[])m_Payload.Clone();

        internal byte[] PayloadBytes => m_Payload;
    }

    public sealed class V2TransmissionAttempt
    {
        public V2ReliableFrame Frame { get; }
        public Guid AttemptId { get; }
        public bool IsReplay { get; }

        internal V2TransmissionAttempt(V2ReliableFrame frame, Guid attemptId, bool isReplay)
        {
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            AttemptId = attemptId;
            IsReplay = isReplay;
        }
    }

    public sealed class V2SchedulerMetrics
    {
        public int PendingSceneRecords { get; }
        public int PendingSessionControlRecords { get; }
        public int PendingSceneBytes { get; }
        public int PendingSessionControlBytes { get; }
        public int RetainedBulkBodyBytes { get; }
        public int OutstandingReliableFrames { get; }
        public int OutstandingReliableBytes { get; }
        public long CoalescedPreviewCount { get; }
        public long PreviewPressureCount { get; }
        public long EphemeralDropCount { get; }
        public long RetryCount { get; }
        public long SessionOverflowCount { get; }
        public long GraceExpiredFrameCount { get; }
        public long GraceExpiredRecordCount { get; }

        internal V2SchedulerMetrics(int pendingSceneRecords, int pendingSessionControlRecords, int pendingSceneBytes, int pendingSessionControlBytes, int retainedBulkBodyBytes, int outstandingReliableFrames, int outstandingReliableBytes, long coalescedPreviewCount, long previewPressureCount, long ephemeralDropCount, long retryCount, long sessionOverflowCount, long graceExpiredFrameCount, long graceExpiredRecordCount)
        {
            PendingSceneRecords = pendingSceneRecords;
            PendingSessionControlRecords = pendingSessionControlRecords;
            PendingSceneBytes = pendingSceneBytes;
            PendingSessionControlBytes = pendingSessionControlBytes;
            RetainedBulkBodyBytes = retainedBulkBodyBytes;
            OutstandingReliableFrames = outstandingReliableFrames;
            OutstandingReliableBytes = outstandingReliableBytes;
            CoalescedPreviewCount = coalescedPreviewCount;
            PreviewPressureCount = previewPressureCount;
            EphemeralDropCount = ephemeralDropCount;
            RetryCount = retryCount;
            SessionOverflowCount = sessionOverflowCount;
            GraceExpiredFrameCount = graceExpiredFrameCount;
            GraceExpiredRecordCount = graceExpiredRecordCount;
        }
    }

    /// <summary>
    /// Deterministic pre-network scheduler. Enqueue owns mutable pending records;
    /// returned reliable frames are immutable and remain retained until ACK or
    /// reconnect-grace expiry.
    /// </summary>
    public sealed class V2OutgoingScheduler
    {
        private const int ReconnectGraceMilliseconds = 500;

        private readonly SessionId m_SessionId;
        private readonly SceneId m_SceneId;
        private readonly IncarnationId m_IncarnationId;
        private readonly V2OriginDevice m_OriginDevice;
        private readonly V2SchedulerLimits m_Limits;
        private readonly IMonotonicClock m_Clock;
        private readonly Func<Guid> m_GuidFactory;
        private readonly LinkedList<PendingRecord> m_SessionControlQueue = new LinkedList<PendingRecord>();
        private readonly LinkedList<PendingRecord> m_SceneQueue = new LinkedList<PendingRecord>();
        private readonly Dictionary<V2TouchedKey, LinkedListNode<PendingRecord>> m_CoalescingSlots = new Dictionary<V2TouchedKey, LinkedListNode<PendingRecord>>();
        private readonly Dictionary<Guid, ReliableStreamState> m_Streams = new Dictionary<Guid, ReliableStreamState>();
        private readonly Dictionary<Guid, BulkTransferState> m_BulkByStream = new Dictionary<Guid, BulkTransferState>();
        private readonly Dictionary<Guid, BulkTransferState> m_BulkByOperation = new Dictionary<Guid, BulkTransferState>();
        private readonly Queue<BulkTransferState> m_BulkRoundRobin = new Queue<BulkTransferState>();
        private readonly HashSet<Guid> m_BulkQueuedIds = new HashSet<Guid>();

        private readonly ReliableStreamState m_SessionControlStream;
        private readonly ReliableStreamState m_SceneOperationStream;
        private V2SchedulerState m_State = V2SchedulerState.Online;
        private long m_GraceDeadline;
        private int m_SceneQueuedBytes;
        private int m_SessionControlQueuedBytes;
        private int m_RetainedBulkBodyBytes;
        private int m_OutstandingReliableFrames;
        private int m_OutstandingReliableBytes;
        private ulong m_NextOriginSequence = 1;
        private int m_InteractiveSinceBulk;
        private long m_CoalescedPreviewCount;
        private long m_PreviewPressureCount;
        private long m_EphemeralDropCount;
        private long m_RetryCount;
        private long m_SessionOverflowCount;
        private long m_GraceExpiredFrameCount;
        private long m_GraceExpiredRecordCount;

        public SessionId SessionId => m_SessionId;
        public SceneId SceneId => m_SceneId;
        public IncarnationId IncarnationId => m_IncarnationId;
        public ReliableStreamId SessionControlStreamId => m_SessionControlStream.StreamId;
        public ReliableStreamId SceneOperationStreamId => m_SceneOperationStream.StreamId;
        public V2OriginDevice OriginDevice => m_OriginDevice;
        public V2SchedulerLimits Limits => m_Limits;
        public V2SchedulerState State => m_State;

        public V2OutgoingScheduler(SessionId sessionId, SceneId sceneId, IncarnationId incarnationId, V2OriginDevice originDevice, IMonotonicClock clock = null, V2SchedulerLimits limits = null, Func<Guid> guidFactory = null)
        {
            m_SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            m_SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            m_IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            if (originDevice != V2OriginDevice.Desktop && originDevice != V2OriginDevice.Quest)
                throw new ArgumentOutOfRangeException(nameof(originDevice));
            m_OriginDevice = originDevice;
            m_Clock = clock ?? StopwatchMonotonicClock.Instance;
            m_Limits = limits ?? new V2SchedulerLimits();
            m_GuidFactory = guidFactory ?? Guid.NewGuid;

            Guid sessionControlGuid = NextGuid();
            Guid sceneOperationGuid = NextGuid();
            while (sceneOperationGuid == sessionControlGuid || sceneOperationGuid == m_SessionId.Value || sceneOperationGuid == m_SceneId.Value || sceneOperationGuid == m_IncarnationId.Value)
                sceneOperationGuid = NextGuid();
            if (sessionControlGuid == m_SessionId.Value || sessionControlGuid == m_SceneId.Value || sessionControlGuid == m_IncarnationId.Value)
                throw new InvalidOperationException("The identity factory returned a duplicate stream identity.");
            m_SessionControlStream = new ReliableStreamState(new ReliableStreamId(sessionControlGuid), StreamKind.SessionControl);
            m_SceneOperationStream = new ReliableStreamState(new ReliableStreamId(sceneOperationGuid), StreamKind.SceneOperation);
            m_Streams.Add(m_SessionControlStream.StreamId.Value, m_SessionControlStream);
            m_Streams.Add(m_SceneOperationStream.StreamId.Value, m_SceneOperationStream);
        }

        public V2EnqueueResult EnqueueSessionControl(byte[] payload, V2DeliveryReliability reliability)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (reliability != V2DeliveryReliability.Ephemeral && reliability != V2DeliveryReliability.Reliable)
                throw new ArgumentOutOfRangeException(nameof(reliability));
            if (payload.Length > m_Limits.InlineThresholdBytes)
                return new V2EnqueueResult(false, V2EnqueueDisposition.Rejected);
            if (!CanAdmitNewWork())
                return new V2EnqueueResult(false, V2EnqueueDisposition.Rejected);

            byte[] ownedPayload = (byte[])payload.Clone();
            var pending = new PendingRecord(ownedPayload, reliability, V2ScheduleLane.SessionControl, null, false, null, null, null, 0);
            int bytes = pending.WireBytes;
            int count = m_SessionControlQueue.Count + 1;
            int totalBytes = checked(m_SessionControlQueuedBytes + bytes);
            bool fits = reliability == V2DeliveryReliability.Reliable ? count <= m_Limits.MaxSessionControlQueuedRecords && totalBytes <= m_Limits.MaxSessionControlQueuedBytes : count <= m_Limits.MaxSessionControlQueuedRecords - m_Limits.ReservedSessionControlRecords && totalBytes <= m_Limits.MaxSessionControlQueuedBytes - m_Limits.ReservedSessionControlBytes;
            if (!fits)
            {
                if (reliability == V2DeliveryReliability.Reliable)
                    return FaultRequiredAdmission();
                m_EphemeralDropCount++;
                return new V2EnqueueResult(false, V2EnqueueDisposition.Backpressured);
            }

            m_SessionControlQueue.AddLast(pending);
            m_SessionControlQueuedBytes = totalBytes;
            return new V2EnqueueResult(true, V2EnqueueDisposition.Accepted);
        }

        public V2EnqueueResult EnqueueMutation(V2Mutation mutation, bool coalesciblePreview = true, OperationId operationId = null)
        {
            if (mutation == null)
                throw new ArgumentNullException(nameof(mutation));
            byte[] payload = V2MutationPayloadCodec.Encode(mutation);
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, mutation);
            return EnqueueSceneOperation(payload, descriptor, coalesciblePreview, false, false, 1, operationId);
        }

        /// <summary>
        /// Enqueues bytes produced by a validated operation or atomic batch codec.
        /// Its descriptor must be derived from those validated operation values.
        /// Bodies above InlineThresholdBytes are automatically replaced on the scene
        /// stream by a descriptor and transferred on a separate reliable bulk stream.
        /// </summary>
        public V2EnqueueResult EnqueueSceneOperation(byte[] encodedBody, V2ScheduleDescriptor descriptor, bool coalesciblePreview = false, bool structural = false, bool final = false, ushort bodySchema = 1, OperationId operationId = null)
        {
            if (encodedBody == null)
                throw new ArgumentNullException(nameof(encodedBody));
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (!descriptor.SceneId.Equals(m_SceneId) || !descriptor.IncarnationId.Equals(m_IncarnationId))
                throw new ArgumentException("The operation descriptor belongs to another scene incarnation.", nameof(descriptor));
            if (structural && descriptor.BarrierScope == V2BarrierScope.None)
                throw new ArgumentException("Structural operations require an explicit barrier scope.", nameof(descriptor));
            if (coalesciblePreview && (descriptor.CoalescingKey == null || structural || final || descriptor.BarrierScope != V2BarrierScope.None))
                throw new ArgumentException("Only non-final, non-structural records with a derived key can coalesce.", nameof(coalesciblePreview));
            if (final && descriptor.BarrierScope == V2BarrierScope.None)
                descriptor = V2ScheduleDescriptor.ForBarrier(m_SceneId, m_IncarnationId, new[] { descriptor }, V2BarrierScope.TouchedKeys, m_Limits.MaxTouchedKeysPerRecord);
            if (descriptor.TouchedKeys.Count > m_Limits.MaxTouchedKeysPerRecord)
                return new V2EnqueueResult(false, V2EnqueueDisposition.Rejected);
            if (!CanAdmitNewWork())
                return new V2EnqueueResult(false, V2EnqueueDisposition.Rejected);

            OperationId id = operationId ?? CreateOperationId();
            if (id == null)
                throw new ArgumentNullException(nameof(operationId));

            bool asBulk = encodedBody.Length > m_Limits.InlineThresholdBytes;
            if (!asBulk)
                return EnqueueInlineSceneRecord((byte[])encodedBody.Clone(), descriptor, coalesciblePreview, structural, final, id);

            LinkedListNode<PendingRecord> existingSlot = coalesciblePreview ? FindCoalescingSlot(descriptor.CoalescingKey) : null;
            BulkTransferState replacedTransfer = existingSlot?.Value.BulkTransfer;
            int replacedBodyBytes = replacedTransfer?.Body?.Length ?? 0;
            int resultingBulkTransferCount = m_BulkByOperation.Count - (replacedTransfer == null ? 0 : 1) + 1;
            int retainedBulkBytesAfterReplacement = checked(m_RetainedBulkBodyBytes - replacedBodyBytes);
            bool withinTransferLimit = resultingBulkTransferCount <= m_Limits.MaxBulkTransfers;
            bool withinBodyByteLimit = encodedBody.Length <= m_Limits.MaxBulkBodyBytesTotal - retainedBulkBytesAfterReplacement;
            if (encodedBody.Length > m_Limits.MaxBulkBodyBytesPerTransfer || !withinTransferLimit || !withinBodyByteLimit)
                return structural || final ? FaultRequiredAdmission(id) : new V2EnqueueResult(false, V2EnqueueDisposition.Backpressured, id);

            byte[] body = (byte[])encodedBody.Clone();
            byte[] digest;
            using (SHA256 sha = SHA256.Create())
                digest = sha.ComputeHash(body);

            ReliableStreamId bulkStreamId = CreateBulkStreamId();
            var bulkDescriptor = new V2BulkTransferDescriptor(id, bulkStreamId, bodySchema, body, m_Limits.BulkChunkBytes, descriptor, digest);
            byte[] descriptorPayload = bulkDescriptor.Encode();
            if (descriptorPayload.Length > m_Limits.InlineThresholdBytes)
                return new V2EnqueueResult(false, V2EnqueueDisposition.Rejected, id);

            var transfer = new BulkTransferState(bulkDescriptor, body);
            V2ScheduleLane lane = structural || final ? V2ScheduleLane.SceneControl : V2ScheduleLane.Interactive;
            var pending = new PendingRecord(descriptorPayload, V2DeliveryReliability.Reliable, lane, id, coalesciblePreview, descriptor, transfer, bulkDescriptor, bodySchema);
            V2EnqueueDisposition disposition;
            if (!CanAdmitSceneRecord(pending, lane != V2ScheduleLane.Interactive, existingSlot))
                return lane == V2ScheduleLane.Interactive ? RecordPreviewPressure(id) : FaultRequiredAdmission(id);

            if (existingSlot != null)
                ReplaceSceneSlot(existingSlot, pending);
            else
            {
                LinkedListNode<PendingRecord> node = m_SceneQueue.AddLast(pending);
                m_SceneQueuedBytes = checked(m_SceneQueuedBytes + pending.WireBytes);
                if (coalesciblePreview)
                    m_CoalescingSlots[descriptor.CoalescingKey] = node;
            }

            if (descriptor.BarrierScope != V2BarrierScope.None)
                SealCoalescingSlots(descriptor);
            m_BulkByStream.Add(bulkStreamId.Value, transfer);
            m_BulkByOperation.Add(id.Value, transfer);
            m_Streams.Add(bulkStreamId.Value, new ReliableStreamState(bulkStreamId, StreamKind.Bulk, transfer));
            m_RetainedBulkBodyBytes = checked(m_RetainedBulkBodyBytes + body.Length);
            disposition = existingSlot == null ? V2EnqueueDisposition.Accepted : V2EnqueueDisposition.ReplacedUnsent;
            if (existingSlot != null)
                m_CoalescedPreviewCount++;
            return new V2EnqueueResult(true, disposition, id, bulkStreamId);
        }

        public bool TryGetNextTransmission(out V2TransmissionAttempt transmission)
        {
            RefreshGraceState();
            transmission = null;
            if (m_State != V2SchedulerState.Online)
                return false;

            if (TryReplay(m_SessionControlStream, out transmission))
                return true;

            if (m_SessionControlQueue.First != null)
            {
                PendingRecord pendingControl = m_SessionControlQueue.First.Value;
                if (pendingControl.Reliability == V2DeliveryReliability.Ephemeral)
                {
                    transmission = DequeueEphemeralSessionControl(m_SessionControlQueue.First);
                    return true;
                }

                if (TryCommit(m_SessionControlStream, pendingControl, true, out transmission))
                {
                    m_SessionControlQueue.RemoveFirst();
                    m_SessionControlQueuedBytes -= pendingControl.WireBytes;
                    return true;
                }

                LinkedListNode<PendingRecord> pendingEphemeral = FindFirstEphemeralSessionControl(m_SessionControlQueue.First.Next);
                if (pendingEphemeral != null)
                {
                    transmission = DequeueEphemeralSessionControl(pendingEphemeral);
                    return true;
                }
            }

            if (TryReplay(m_SceneOperationStream, out transmission))
                return true;

            LinkedListNode<PendingRecord> sceneNode = m_SceneQueue.First;
            if (sceneNode != null && sceneNode.Value.Lane == V2ScheduleLane.Interactive && m_InteractiveSinceBulk >= m_Limits.InteractiveBurst && TryGetNextBulkTransmission(out transmission))
                return true;

            if (sceneNode != null)
            {
                if (TryCommit(m_SceneOperationStream, sceneNode.Value, sceneNode.Value.Lane != V2ScheduleLane.Interactive, out transmission))
                {
                    RemoveSceneNode(sceneNode);
                    if (transmission.Frame.Lane == V2ScheduleLane.Interactive)
                        m_InteractiveSinceBulk++;
                    return true;
                }

                if (sceneNode.Value.Lane == V2ScheduleLane.Interactive && sceneNode.Value.Coalescible)
                    m_PreviewPressureCount++;
            }

            return TryGetNextBulkTransmission(out transmission);
        }

        public bool Acknowledge(ReliableStreamId streamId, ulong throughSequence)
        {
            if (streamId == null)
                throw new ArgumentNullException(nameof(streamId));
            if (!m_Streams.TryGetValue(streamId.Value, out ReliableStreamState stream))
                return false;
            if (throughSequence == 0 || throughSequence >= stream.NextSequence)
                return false;

            List<V2ReliableFrame> removed = stream.Acknowledge(throughSequence);
            for (int i = 0; i < removed.Count; i++)
            {
                m_OutstandingReliableFrames--;
                m_OutstandingReliableBytes -= removed[i].WireBytes;
            }

            if (stream.Kind == StreamKind.Bulk && stream.UnacknowledgedCount == 0 && stream.BulkTransfer != null && (stream.BulkTransfer.AllChunksCommitted || stream.BulkTransfer.Cancelled))
                CompleteBulkTransfer(stream.BulkTransfer);
            return removed.Count > 0;
        }

        public void BeginDisconnectGrace()
        {
            if (m_State != V2SchedulerState.Online)
                return;
            long graceTicks = checked((m_Clock.Frequency * ReconnectGraceMilliseconds) / 1000L);
            if (graceTicks <= 0)
                graceTicks = 1;
            m_GraceDeadline = checked(m_Clock.GetTimestamp() + graceTicks);
            m_State = V2SchedulerState.DisconnectedGrace;
        }

        public bool TryResume()
        {
            RefreshGraceState();
            if (m_State != V2SchedulerState.DisconnectedGrace)
                return false;

            foreach (ReliableStreamState stream in m_Streams.Values)
                stream.BeginReplay();
            foreach (BulkTransferState transfer in m_BulkByStream.Values)
                QueueBulkIfReady(transfer);
            m_State = V2SchedulerState.Online;
            return true;
        }

        public bool CancelBulk(OperationId operationId)
        {
            if (operationId == null)
                throw new ArgumentNullException(nameof(operationId));
            if (!m_BulkByOperation.TryGetValue(operationId.Value, out BulkTransferState transfer))
                return false;

            transfer.Cancelled = true;
            if (transfer.Body != null)
            {
                m_RetainedBulkBodyBytes -= transfer.Body.Length;
                transfer.Body = null;
            }

            m_BulkByOperation.Remove(operationId.Value);
            if (transfer.DescriptorCommitted)
            {
                QueueBulkIfReady(transfer);
                ReliableStreamState stream = m_Streams[transfer.Descriptor.BulkStreamId.Value];
                if (stream.UnacknowledgedCount == 0)
                    CompleteBulkTransfer(transfer);
            }
            else
                RemovePendingBulkDescriptor(transfer);

            return true;
        }

        public V2SchedulerMetrics SnapshotMetrics()
        {
            return new V2SchedulerMetrics(m_SceneQueue.Count, m_SessionControlQueue.Count, m_SceneQueuedBytes, m_SessionControlQueuedBytes, m_RetainedBulkBodyBytes, m_OutstandingReliableFrames, m_OutstandingReliableBytes, m_CoalescedPreviewCount, m_PreviewPressureCount, m_EphemeralDropCount, m_RetryCount, m_SessionOverflowCount, m_GraceExpiredFrameCount, m_GraceExpiredRecordCount);
        }

        private V2EnqueueResult EnqueueInlineSceneRecord(byte[] payload, V2ScheduleDescriptor descriptor, bool coalesciblePreview, bool structural, bool final, OperationId operationId)
        {
            V2ScheduleLane lane = structural || final ? V2ScheduleLane.SceneControl : V2ScheduleLane.Interactive;
            var pending = new PendingRecord(payload, V2DeliveryReliability.Reliable, lane, operationId, coalesciblePreview, descriptor, null, null, 1);
            LinkedListNode<PendingRecord> existingSlot = coalesciblePreview ? FindCoalescingSlot(descriptor.CoalescingKey) : null;
            if (!CanAdmitSceneRecord(pending, lane != V2ScheduleLane.Interactive, existingSlot))
                return lane == V2ScheduleLane.Interactive ? RecordPreviewPressure(operationId) : FaultRequiredAdmission(operationId);

            if (existingSlot != null)
            {
                ReplaceSceneSlot(existingSlot, pending);
                m_CoalescedPreviewCount++;
                return new V2EnqueueResult(true, V2EnqueueDisposition.ReplacedUnsent, operationId);
            }

            LinkedListNode<PendingRecord> node = m_SceneQueue.AddLast(pending);
            m_SceneQueuedBytes = checked(m_SceneQueuedBytes + pending.WireBytes);
            if (coalesciblePreview)
                m_CoalescingSlots[descriptor.CoalescingKey] = node;
            if (descriptor.BarrierScope != V2BarrierScope.None)
                SealCoalescingSlots(descriptor);
            return new V2EnqueueResult(true, V2EnqueueDisposition.Accepted, operationId);
        }

        private bool CanAdmitSceneRecord(PendingRecord record, bool useControlReserve, LinkedListNode<PendingRecord> replacing)
        {
            int count = m_SceneQueue.Count + (replacing == null ? 1 : 0);
            int bytes = checked(m_SceneQueuedBytes - (replacing == null ? 0 : replacing.Value.WireBytes) + record.WireBytes);
            int recordLimit = useControlReserve ? m_Limits.MaxSceneQueuedRecords : m_Limits.MaxSceneQueuedRecords - m_Limits.ReservedSceneControlRecords;
            int byteLimit = useControlReserve ? m_Limits.MaxSceneQueuedBytes : m_Limits.MaxSceneQueuedBytes - m_Limits.ReservedSceneControlBytes;
            return count <= recordLimit && bytes <= byteLimit;
        }

        private static LinkedListNode<PendingRecord> FindFirstEphemeralSessionControl(LinkedListNode<PendingRecord> start)
        {
            for (LinkedListNode<PendingRecord> node = start; node != null; node = node.Next)
            {
                if (node.Value.Reliability == V2DeliveryReliability.Ephemeral)
                    return node;
            }

            return null;
        }

        private V2TransmissionAttempt DequeueEphemeralSessionControl(LinkedListNode<PendingRecord> node)
        {
            PendingRecord pending = node.Value;
            m_SessionControlQueue.Remove(node);
            m_SessionControlQueuedBytes -= pending.WireBytes;
            var ephemeral = new V2ReliableFrame(null, null, null, null, V2ScheduleLane.SessionControl, V2DeliveryReliability.Ephemeral, pending.Payload);
            return CreateAttempt(ephemeral, false);
        }

        private bool TryCommit(ReliableStreamState stream, PendingRecord pending, bool useControlReserve, out V2TransmissionAttempt transmission)
        {
            transmission = null;
            int wireBytes = checked(pending.Payload.Length + V2MutationEnvelopeCodec.HeaderLength);
            if (!CanRetainReliable(wireBytes, useControlReserve))
                return false;
            if (stream.NextSequence == ulong.MaxValue)
            {
                FaultSession();
                return false;
            }

            ulong sequence = stream.NextSequence++;
            ulong? originSequence = null;
            if (stream.Kind == StreamKind.SceneOperation)
            {
                if (m_NextOriginSequence == ulong.MaxValue)
                {
                    FaultSession();
                    return false;
                }

                originSequence = m_NextOriginSequence++;
            }

            var frame = new V2ReliableFrame(stream.StreamId, sequence, originSequence, pending.OperationId, pending.Lane, V2DeliveryReliability.Reliable, pending.Payload, pending.BulkDescriptor, pending.ChunkIndex, pending.ChunkCount, pending.ChunkOffset);
            stream.Retain(frame);
            m_OutstandingReliableFrames++;
            m_OutstandingReliableBytes = checked(m_OutstandingReliableBytes + frame.WireBytes);
            if (pending.BulkTransfer != null)
            {
                pending.BulkTransfer.DescriptorCommitted = true;
                pending.BulkTransfer.DescriptorSequence = sequence;
                QueueBulkIfReady(pending.BulkTransfer);
            }

            transmission = CreateAttempt(frame, false);
            return true;
        }

        private bool CanRetainReliable(int bytes, bool useControlReserve)
        {
            int frameLimit = useControlReserve ? m_Limits.MaxReliableFrames : m_Limits.MaxReliableFrames - m_Limits.ReservedReliableFrames;
            int byteLimit = useControlReserve ? m_Limits.MaxReliableBytes : m_Limits.MaxReliableBytes - m_Limits.ReservedReliableBytes;
            return m_OutstandingReliableFrames < frameLimit && bytes <= byteLimit - m_OutstandingReliableBytes;
        }

        private bool TryReplay(ReliableStreamState stream, out V2TransmissionAttempt transmission)
        {
            transmission = null;
            if (!stream.TryPopReplay(out V2ReliableFrame frame))
                return false;
            m_RetryCount++;
            transmission = CreateAttempt(frame, true);
            return true;
        }

        private bool TryGetNextBulkTransmission(out V2TransmissionAttempt transmission)
        {
            transmission = null;
            int candidates = m_BulkRoundRobin.Count;
            for (int i = 0; i < candidates; i++)
            {
                BulkTransferState transfer = m_BulkRoundRobin.Dequeue();
                m_BulkQueuedIds.Remove(transfer.Descriptor.BulkStreamId.Value);
                if (!m_BulkByStream.ContainsKey(transfer.Descriptor.BulkStreamId.Value) || !HasBulkWork(transfer))
                    continue;

                ReliableStreamState stream = m_Streams[transfer.Descriptor.BulkStreamId.Value];
                if (stream.TryPopReplay(out V2ReliableFrame replay))
                {
                    m_RetryCount++;
                    transmission = CreateAttempt(replay, true);
                    QueueBulkIfReady(transfer);
                    m_InteractiveSinceBulk = 0;
                    return true;
                }

                if (transfer.Cancelled || transfer.Body == null || transfer.NextOffset >= transfer.Body.Length)
                {
                    QueueBulkIfReady(transfer);
                    continue;
                }

                int offset = transfer.NextOffset;
                int count = Math.Min(m_Limits.BulkChunkBytes, transfer.Body.Length - offset);
                if (!CanRetainReliable(checked(count + V2MutationEnvelopeCodec.HeaderLength), false))
                {
                    QueueBulkIfReady(transfer);
                    continue;
                }

                byte[] chunk = new byte[count];
                Buffer.BlockCopy(transfer.Body, offset, chunk, 0, count);
                int chunkIndex = offset / transfer.Descriptor.ChunkSize;
                var pendingChunk = new PendingRecord(chunk, V2DeliveryReliability.Reliable, V2ScheduleLane.Bulk, transfer.Descriptor.OperationId, false, transfer.Descriptor.Scheduling, null, transfer.Descriptor, transfer.Descriptor.BodySchema, chunkIndex, offset, transfer.Descriptor.ChunkCount);
                if (!TryCommit(stream, pendingChunk, false, out transmission))
                {
                    QueueBulkIfReady(transfer);
                    continue;
                }

                transfer.NextOffset = checked(offset + count);
                transfer.LastChunkSequence = transmission.Frame.ReliableFrameSequence.Value;
                transfer.LastChunkIndex = chunkIndex;
                if (transfer.NextOffset >= transfer.Body.Length)
                    transfer.AllChunksCommitted = true;
                QueueBulkIfReady(transfer);
                m_InteractiveSinceBulk = 0;
                return true;
            }

            return false;
        }

        private bool HasBulkWork(BulkTransferState transfer)
        {
            if (transfer == null || !m_BulkByStream.ContainsKey(transfer.Descriptor.BulkStreamId.Value))
                return false;
            ReliableStreamState stream = m_Streams[transfer.Descriptor.BulkStreamId.Value];
            return stream.HasReplayPending || (!transfer.Cancelled && transfer.Body != null && transfer.DescriptorCommitted && transfer.NextOffset < transfer.Body.Length);
        }

        private void QueueBulkIfReady(BulkTransferState transfer)
        {
            if (HasBulkWork(transfer) && m_BulkQueuedIds.Add(transfer.Descriptor.BulkStreamId.Value))
                m_BulkRoundRobin.Enqueue(transfer);
        }

        private void RemoveSceneNode(LinkedListNode<PendingRecord> node)
        {
            if (node.Value.Coalescible && node.Value.Descriptor?.CoalescingKey != null && m_CoalescingSlots.TryGetValue(node.Value.Descriptor.CoalescingKey, out LinkedListNode<PendingRecord> slot) && ReferenceEquals(slot, node))
                m_CoalescingSlots.Remove(node.Value.Descriptor.CoalescingKey);
            m_SceneQueuedBytes -= node.Value.WireBytes;
            m_SceneQueue.Remove(node);
        }

        private void ReplaceSceneSlot(LinkedListNode<PendingRecord> node, PendingRecord replacement)
        {
            PendingRecord previous = node.Value;
            m_SceneQueuedBytes = checked(m_SceneQueuedBytes - previous.WireBytes + replacement.WireBytes);
            if (previous.BulkTransfer != null)
                RemoveUncommittedTransfer(previous.BulkTransfer);
            node.Value = replacement;
            if (replacement.Coalescible)
                m_CoalescingSlots[replacement.Descriptor.CoalescingKey] = node;
        }

        private void SealCoalescingSlots(V2ScheduleDescriptor descriptor)
        {
            if (descriptor.BarrierScope == V2BarrierScope.AllScene)
            {
                m_CoalescingSlots.Clear();
                return;
            }

            for (int i = 0; i < descriptor.TouchedKeys.Count; i++)
                m_CoalescingSlots.Remove(descriptor.TouchedKeys[i]);
        }

        private LinkedListNode<PendingRecord> FindCoalescingSlot(V2TouchedKey key)
        {
            if (key == null || !m_CoalescingSlots.TryGetValue(key, out LinkedListNode<PendingRecord> node))
                return null;
            return node.List == m_SceneQueue ? node : null;
        }

        private void RemovePendingBulkDescriptor(BulkTransferState transfer)
        {
            for (LinkedListNode<PendingRecord> node = m_SceneQueue.First; node != null; node = node.Next)
            {
                if (!ReferenceEquals(node.Value.BulkTransfer, transfer))
                    continue;
                RemoveSceneNode(node);
                break;
            }

            RemoveUncommittedTransfer(transfer);
        }

        private void RemoveUncommittedTransfer(BulkTransferState transfer)
        {
            m_BulkByOperation.Remove(transfer.Descriptor.OperationId.Value);
            RemoveBulkFromRoundRobin(transfer.Descriptor.BulkStreamId.Value);
            if (transfer.Body != null)
            {
                m_RetainedBulkBodyBytes -= transfer.Body.Length;
                transfer.Body = null;
            }

            m_BulkByStream.Remove(transfer.Descriptor.BulkStreamId.Value);
            m_Streams.Remove(transfer.Descriptor.BulkStreamId.Value);
        }

        private void CompleteBulkTransfer(BulkTransferState transfer)
        {
            RemoveBulkFromRoundRobin(transfer.Descriptor.BulkStreamId.Value);
            if (transfer.Body != null)
            {
                m_RetainedBulkBodyBytes -= transfer.Body.Length;
                transfer.Body = null;
            }

            m_BulkByOperation.Remove(transfer.Descriptor.OperationId.Value);
            m_BulkByStream.Remove(transfer.Descriptor.BulkStreamId.Value);
            m_Streams.Remove(transfer.Descriptor.BulkStreamId.Value);
        }

        private void RemoveBulkFromRoundRobin(Guid streamId)
        {
            if (!m_BulkQueuedIds.Remove(streamId))
                return;
            int count = m_BulkRoundRobin.Count;
            for (int i = 0; i < count; i++)
            {
                BulkTransferState candidate = m_BulkRoundRobin.Dequeue();
                if (candidate.Descriptor.BulkStreamId.Value != streamId)
                    m_BulkRoundRobin.Enqueue(candidate);
            }
        }

        private void RefreshGraceState()
        {
            if (m_State != V2SchedulerState.DisconnectedGrace || m_Clock.GetTimestamp() < m_GraceDeadline)
                return;
            m_GraceExpiredFrameCount += m_OutstandingReliableFrames;
            m_GraceExpiredRecordCount += m_SceneQueue.Count + m_SessionControlQueue.Count + m_BulkByOperation.Count;
            m_OutstandingReliableFrames = 0;
            m_OutstandingReliableBytes = 0;
            m_SceneQueue.Clear();
            m_SessionControlQueue.Clear();
            m_CoalescingSlots.Clear();
            m_SceneQueuedBytes = 0;
            m_SessionControlQueuedBytes = 0;
            m_RetainedBulkBodyBytes = 0;
            m_BulkByStream.Clear();
            m_BulkByOperation.Clear();
            m_BulkRoundRobin.Clear();
            m_BulkQueuedIds.Clear();
            foreach (ReliableStreamState stream in m_Streams.Values)
                stream.Clear();
            m_Streams.Clear();
            m_Streams.Add(m_SessionControlStream.StreamId.Value, m_SessionControlStream);
            m_Streams.Add(m_SceneOperationStream.StreamId.Value, m_SceneOperationStream);
            m_State = V2SchedulerState.Offline;
        }

        private bool CanAdmitNewWork() => m_State == V2SchedulerState.Online || m_State == V2SchedulerState.DisconnectedGrace;

        private V2EnqueueResult RecordPreviewPressure(OperationId operationId)
        {
            m_PreviewPressureCount++;
            return new V2EnqueueResult(false, V2EnqueueDisposition.Backpressured, operationId);
        }

        private V2EnqueueResult FaultRequiredAdmission(OperationId operationId = null)
        {
            FaultSession();
            return new V2EnqueueResult(false, V2EnqueueDisposition.SessionFaulted, operationId);
        }

        private void FaultSession()
        {
            m_SessionOverflowCount++;
            m_State = V2SchedulerState.Faulted;
        }

        private OperationId CreateOperationId()
        {
            return new OperationId(NextGuid());
        }

        private ReliableStreamId CreateBulkStreamId()
        {
            Guid value = NextGuid();
            while (m_Streams.ContainsKey(value) || value == m_SessionId.Value || value == m_SceneId.Value || value == m_IncarnationId.Value)
                value = NextGuid();
            return new ReliableStreamId(value);
        }

        private Guid NextGuid()
        {
            Guid value = m_GuidFactory();
            if (value == Guid.Empty)
                throw new InvalidOperationException("The identity factory returned an empty identity.");
            return value;
        }

        private V2TransmissionAttempt CreateAttempt(V2ReliableFrame frame, bool replay)
        {
            Guid attemptId = NextGuid();
            return new V2TransmissionAttempt(frame, attemptId, replay);
        }

        private enum StreamKind : byte
        {
            SessionControl,
            SceneOperation,
            Bulk
        }

        private sealed class PendingRecord
        {
            public byte[] Payload { get; }
            public V2DeliveryReliability Reliability { get; }
            public V2ScheduleLane Lane { get; }
            public OperationId OperationId { get; }
            public bool Coalescible { get; }
            public V2ScheduleDescriptor Descriptor { get; }
            public BulkTransferState BulkTransfer { get; }
            public V2BulkTransferDescriptor BulkDescriptor { get; }
            public ushort BodySchema { get; }
            public int? ChunkIndex { get; }
            public int? ChunkOffset { get; }
            public int? ChunkCount { get; }
            public int WireBytes { get; }

            public PendingRecord(byte[] payload, V2DeliveryReliability reliability, V2ScheduleLane lane, OperationId operationId, bool coalescible, V2ScheduleDescriptor descriptor, BulkTransferState bulkTransfer, V2BulkTransferDescriptor bulkDescriptor, ushort bodySchema, int? chunkIndex = null, int? chunkOffset = null, int? chunkCount = null)
            {
                Payload = payload ?? throw new ArgumentNullException(nameof(payload));
                Reliability = reliability;
                Lane = lane;
                OperationId = operationId;
                Coalescible = coalescible;
                Descriptor = descriptor;
                BulkTransfer = bulkTransfer;
                BulkDescriptor = bulkDescriptor;
                BodySchema = bodySchema;
                ChunkIndex = chunkIndex;
                ChunkOffset = chunkOffset;
                ChunkCount = chunkCount;
                WireBytes = checked(payload.Length + V2MutationEnvelopeCodec.HeaderLength);
            }
        }

        private sealed class BulkTransferState
        {
            public V2BulkTransferDescriptor Descriptor { get; }
            public byte[] Body;
            public int NextOffset;
            public ulong DescriptorSequence;
            public ulong LastChunkSequence;
            public int LastChunkIndex;
            public bool DescriptorCommitted;
            public bool AllChunksCommitted;
            public bool Cancelled;

            public BulkTransferState(V2BulkTransferDescriptor descriptor, byte[] body)
            {
                Descriptor = descriptor;
                Body = body;
                LastChunkIndex = -1;
            }
        }

        private sealed class ReliableStreamState
        {
            private readonly SortedDictionary<ulong, V2ReliableFrame> m_Unacknowledged = new SortedDictionary<ulong, V2ReliableFrame>();
            private readonly Queue<ulong> m_ReplaySequences = new Queue<ulong>();
            public ReliableStreamId StreamId { get; }
            public StreamKind Kind { get; }
            public BulkTransferState BulkTransfer { get; }
            public ulong NextSequence { get; set; } = 1;
            public int UnacknowledgedCount => m_Unacknowledged.Count;
            public bool HasReplayPending => m_ReplaySequences.Count > 0;

            public ReliableStreamState(ReliableStreamId streamId, StreamKind kind, BulkTransferState bulkTransfer = null)
            {
                StreamId = streamId;
                Kind = kind;
                BulkTransfer = bulkTransfer;
            }

            public void Retain(V2ReliableFrame frame)
            {
                m_Unacknowledged.Add(frame.ReliableFrameSequence.Value, frame);
            }

            public void BeginReplay()
            {
                m_ReplaySequences.Clear();
                foreach (ulong sequence in m_Unacknowledged.Keys)
                    m_ReplaySequences.Enqueue(sequence);
            }

            public bool TryPopReplay(out V2ReliableFrame frame)
            {
                while (m_ReplaySequences.Count > 0)
                {
                    ulong sequence = m_ReplaySequences.Dequeue();
                    if (m_Unacknowledged.TryGetValue(sequence, out frame))
                        return true;
                }

                frame = null;
                return false;
            }

            public List<V2ReliableFrame> Acknowledge(ulong throughSequence)
            {
                var removed = new List<V2ReliableFrame>();
                var keys = new List<ulong>();
                foreach (KeyValuePair<ulong, V2ReliableFrame> entry in m_Unacknowledged)
                {
                    if (entry.Key > throughSequence)
                        break;
                    keys.Add(entry.Key);
                    removed.Add(entry.Value);
                }

                for (int i = 0; i < keys.Count; i++)
                    m_Unacknowledged.Remove(keys[i]);
                return removed;
            }

            public void Clear()
            {
                m_Unacknowledged.Clear();
                m_ReplaySequences.Clear();
                NextSequence = 1;
            }
        }
    }
}
