using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync;

namespace HBP.Transfer.Transport
{
    public enum V2TransportMessageKind : ushort
    {
        Application = 1,
        Acknowledgement = 2,
        Ping = 3,
        Pong = 4,
        ResumeHello = 5,
        BulkStreamRetirement = 6
    }

    /// <summary>A bounded protocol record carried by one authenticated duplex stream.</summary>
    public sealed class V2TransportRecord
    {
        private readonly byte[] m_Payload;

        public V2TransportMessageKind Kind { get; }
        public SessionId SessionId { get; }
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public OperationId MessageId { get; }
        public ReliableStreamId StreamId { get; }
        public ulong ReliableFrameSequence { get; }
        public ulong OriginSequence { get; }
        public ulong? CanonicalSequence { get; }
        public ulong? ObservedCanonicalSequence { get; }
        public ulong BulkStreamRetiredThrough { get; }
        public V2OriginDevice OriginDevice { get; }
        public V2ScheduleLane Lane { get; }
        public ushort BodySchema { get; }
        public int? ChunkIndex { get; }
        public int PayloadLength => m_Payload.Length;

        public V2TransportRecord(V2TransportMessageKind kind, SessionId sessionId, SceneId sceneId = null, IncarnationId incarnationId = null, OperationId messageId = null, ReliableStreamId streamId = null, ulong reliableFrameSequence = 0, ulong originSequence = 0, V2OriginDevice originDevice = V2OriginDevice.Desktop, V2ScheduleLane lane = V2ScheduleLane.SessionControl, ushort bodySchema = 0, int? chunkIndex = null, byte[] payload = null, ulong? canonicalSequence = null, ulong? observedCanonicalSequence = null, ulong bulkStreamRetiredThrough = 0)
        {
            Kind = kind;
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            SceneId = sceneId;
            IncarnationId = incarnationId;
            MessageId = messageId;
            StreamId = streamId;
            ReliableFrameSequence = reliableFrameSequence;
            OriginSequence = originSequence;
            CanonicalSequence = canonicalSequence;
            ObservedCanonicalSequence = observedCanonicalSequence;
            BulkStreamRetiredThrough = bulkStreamRetiredThrough;
            OriginDevice = originDevice;
            Lane = lane;
            BodySchema = bodySchema;
            ChunkIndex = chunkIndex;
            m_Payload = payload == null ? Array.Empty<byte>() : (byte[])payload.Clone();
        }

        public byte[] GetPayloadCopy() => (byte[])m_Payload.Clone();
        internal byte[] PayloadBytes => m_Payload;
    }

    /// <summary>
    /// T05 transport framing. Typed mutation records use the unchanged T01 HBS2
    /// envelope. Other transport records use HBT2, where offsets 129-135 carry
    /// lane, chunk index and schema. Declared lengths are checked before body
    /// buffers are allocated.
    /// </summary>
    public static class V2TransportFrameCodec
    {
        public const int HeaderLength = 136;
        public const int MaximumPayloadBytes = 64 * 1024;
        public const int MaximumFrameBytes = HeaderLength + MaximumPayloadBytes;
        public const ushort ProtocolVersion = 2;
        private const ushort CanonicalSequenceFlag = 1;
        private const ushort ObservedCanonicalSequenceFlag = 2;
        private const uint NoChunk = uint.MaxValue;
        private static readonly byte[] Magic = { (byte)'H', (byte)'B', (byte)'T', (byte)'2' };
        private static readonly byte[] MutationMagic = { (byte)'H', (byte)'B', (byte)'S', (byte)'2' };

        public static byte[] Encode(V2TransportRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            Validate(record, false);

            if (TryCreateMutationEnvelope(record, out V2MutationEnvelope mutationEnvelope))
                return V2MutationEnvelopeCodec.Encode(mutationEnvelope);

            byte[] payload = record.PayloadBytes;
            var bytes = new byte[checked(HeaderLength + payload.Length)];
            Buffer.BlockCopy(Magic, 0, bytes, 0, Magic.Length);
            WriteUInt16(bytes, 4, ProtocolVersion);
            WriteUInt16(bytes, 6, (ushort)record.Kind);
            ushort flags = 0;
            if (record.CanonicalSequence.HasValue)
                flags |= CanonicalSequenceFlag;
            if (record.ObservedCanonicalSequence.HasValue)
                flags |= ObservedCanonicalSequenceFlag;
            WriteUInt16(bytes, 8, flags);
            WriteUInt16(bytes, 10, HeaderLength);
            WriteUInt32(bytes, 12, checked((uint)payload.Length));
            WriteGuid(bytes, 16, record.SessionId.Value);
            WriteGuid(bytes, 32, record.SceneId?.Value ?? Guid.Empty);
            WriteGuid(bytes, 48, record.IncarnationId?.Value ?? Guid.Empty);
            WriteGuid(bytes, 64, record.MessageId?.Value ?? Guid.Empty);
            WriteGuid(bytes, 80, record.StreamId?.Value ?? Guid.Empty);
            WriteUInt64(bytes, 96, record.Kind == V2TransportMessageKind.ResumeHello || record.Kind == V2TransportMessageKind.BulkStreamRetirement ? record.BulkStreamRetiredThrough : record.ReliableFrameSequence);
            WriteUInt64(bytes, 104, record.OriginSequence);
            WriteUInt64(bytes, 112, record.CanonicalSequence.GetValueOrDefault());
            WriteUInt64(bytes, 120, record.ObservedCanonicalSequence.GetValueOrDefault());
            bytes[128] = (byte)record.OriginDevice;
            bytes[129] = (byte)record.Lane;
            WriteUInt32(bytes, 130, record.ChunkIndex.HasValue ? checked((uint)record.ChunkIndex.Value) : NoChunk);
            WriteUInt16(bytes, 134, record.BodySchema);
            if (payload.Length > 0)
                Buffer.BlockCopy(payload, 0, bytes, HeaderLength, payload.Length);
            return bytes;
        }

        public static V2TransportRecord Decode(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length < HeaderLength)
                throw new InvalidDataException("Truncated v2 transport header.");
            if (HasMagic(bytes, 0, MutationMagic))
                return CreateRecord(V2MutationEnvelopeCodec.Decode(bytes));
            HeaderFields fields = ReadHeader(bytes, 0);
            if (bytes.Length != checked(HeaderLength + fields.PayloadLength))
                throw new InvalidDataException("V2 transport frame length does not match its payload length.");
            byte[] payload = new byte[fields.PayloadLength];
            if (payload.Length > 0)
                Buffer.BlockCopy(bytes, HeaderLength, payload, 0, payload.Length);
            return CreateRecord(fields, payload);
        }

        public static async Task<V2TransportRecord> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            var header = new byte[HeaderLength];
            if (!await ReadExactAsync(stream, header, 0, header.Length, true, cancellationToken).ConfigureAwait(false))
                return null;

            if (HasMagic(header, 0, MutationMagic))
            {
                int payloadLength = ReadMutationPayloadLength(header);
                var frame = new byte[checked(HeaderLength + payloadLength)];
                Buffer.BlockCopy(header, 0, frame, 0, HeaderLength);
                if (payloadLength > 0 && !await ReadExactAsync(stream, frame, HeaderLength, payloadLength, false, cancellationToken).ConfigureAwait(false))
                    throw new EndOfStreamException("Truncated v2 mutation payload.");
                return CreateRecord(V2MutationEnvelopeCodec.Decode(frame));
            }

            HeaderFields fields = ReadHeader(header, 0);
            var payload = new byte[fields.PayloadLength];
            if (payload.Length > 0 && !await ReadExactAsync(stream, payload, 0, payload.Length, false, cancellationToken).ConfigureAwait(false))
                throw new EndOfStreamException("Truncated v2 transport payload.");
            return CreateRecord(fields, payload);
        }

        public static async Task WriteAsync(Stream stream, V2TransportRecord record, CancellationToken cancellationToken)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            byte[] frame = Encode(record);
            await stream.WriteAsync(frame, 0, frame.Length, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, bool allowCleanEnd, CancellationToken cancellationToken)
        {
            int read = 0;
            while (read < count)
            {
                int current = await stream.ReadAsync(buffer, offset + read, count - read, cancellationToken).ConfigureAwait(false);
                if (current == 0)
                {
                    if (read == 0 && allowCleanEnd)
                        return false;
                    throw new EndOfStreamException("The v2 transport stream ended mid-frame.");
                }

                read += current;
            }

            return true;
        }

        private static HeaderFields ReadHeader(byte[] bytes, int offset)
        {
            if (bytes == null || offset < 0 || bytes.Length - offset < HeaderLength)
                throw new InvalidDataException("Truncated v2 transport header.");
            for (int i = 0; i < Magic.Length; i++)
            {
                if (bytes[offset + i] != Magic[i])
                    throw new InvalidDataException("Invalid v2 transport magic.");
            }

            if (ReadUInt16(bytes, offset + 4) != ProtocolVersion)
                throw new InvalidDataException("Unsupported v2 transport version.");
            if (ReadUInt16(bytes, offset + 10) != HeaderLength)
                throw new InvalidDataException("Invalid v2 transport header length.");

            ushort kindValue = ReadUInt16(bytes, offset + 6);
            if (!Enum.IsDefined(typeof(V2TransportMessageKind), kindValue))
                throw new InvalidDataException("Unsupported v2 transport message kind.");
            ushort flags = ReadUInt16(bytes, offset + 8);
            if ((flags & ~(CanonicalSequenceFlag | ObservedCanonicalSequenceFlag)) != 0)
                throw new InvalidDataException("Unsupported v2 transport flags.");
            uint payloadLength = ReadUInt32(bytes, offset + 12);
            if (payloadLength > MaximumPayloadBytes)
                throw new InvalidDataException("V2 transport payload exceeds its bound.");

            byte originByte = bytes[offset + 128];
            if (originByte != (byte)V2OriginDevice.Desktop && originByte != (byte)V2OriginDevice.Quest)
                throw new InvalidDataException("Invalid v2 transport origin device.");
            byte laneByte = bytes[offset + 129];
            if (laneByte < (byte)V2ScheduleLane.SessionControl || laneByte > (byte)V2ScheduleLane.Bulk)
                throw new InvalidDataException("Invalid v2 transport lane.");
            uint chunkValue = ReadUInt32(bytes, offset + 130);
            if (chunkValue != NoChunk && chunkValue > int.MaxValue)
                throw new InvalidDataException("Invalid v2 transport chunk index.");

            var fields = new HeaderFields
            {
                Kind = (V2TransportMessageKind)kindValue,
                Flags = flags,
                PayloadLength = checked((int)payloadLength),
                SessionId = ReadGuid(bytes, offset + 16),
                SceneId = ReadGuid(bytes, offset + 32),
                IncarnationId = ReadGuid(bytes, offset + 48),
                MessageId = ReadGuid(bytes, offset + 64),
                StreamId = ReadGuid(bytes, offset + 80),
                ReliableFrameSequence = (V2TransportMessageKind)kindValue == V2TransportMessageKind.ResumeHello || (V2TransportMessageKind)kindValue == V2TransportMessageKind.BulkStreamRetirement ? 0UL : ReadUInt64(bytes, offset + 96),
                BulkStreamRetiredThrough = (V2TransportMessageKind)kindValue == V2TransportMessageKind.ResumeHello || (V2TransportMessageKind)kindValue == V2TransportMessageKind.BulkStreamRetirement ? ReadUInt64(bytes, offset + 96) : 0UL,
                OriginSequence = ReadUInt64(bytes, offset + 104),
                CanonicalSequence = ReadUInt64(bytes, offset + 112),
                ObservedCanonicalSequence = ReadUInt64(bytes, offset + 120),
                OriginDevice = (V2OriginDevice)originByte,
                Lane = (V2ScheduleLane)laneByte,
                ChunkIndex = chunkValue == NoChunk ? (int?)null : (int)chunkValue,
                BodySchema = ReadUInt16(bytes, offset + 134)
            };
            Validate(fields, true);
            return fields;
        }

        private static int ReadMutationPayloadLength(byte[] header)
        {
            if (header == null || header.Length < HeaderLength || !HasMagic(header, 0, MutationMagic))
                throw new InvalidDataException("Truncated v2 mutation header.");
            if (ReadUInt16(header, 4) != V2MutationEnvelopeCodec.ProtocolVersion || ReadUInt16(header, 6) != V2MutationEnvelopeCodec.MutationMessageKind)
                throw new InvalidDataException("Unsupported v2 mutation version or message kind.");
            ushort flags = ReadUInt16(header, 8);
            if ((flags & ~(CanonicalSequenceFlag | ObservedCanonicalSequenceFlag)) != 0)
                throw new InvalidDataException("Unsupported v2 mutation flags.");
            if (ReadUInt16(header, 10) != HeaderLength)
                throw new InvalidDataException("Invalid v2 mutation header length.");
            uint payloadLength = ReadUInt32(header, 12);
            if (payloadLength > V2MutationEnvelopeCodec.MaximumPayloadBytes)
                throw new InvalidDataException("V2 mutation payload exceeds its bound.");
            for (int i = 129; i < HeaderLength; i++)
            {
                if (header[i] != 0)
                    throw new InvalidDataException("Nonzero v2 mutation reserved byte.");
            }

            bool hasCanonicalSequence = (flags & CanonicalSequenceFlag) != 0;
            bool hasObservedCanonicalSequence = (flags & ObservedCanonicalSequenceFlag) != 0;
            ulong canonicalSequence = ReadUInt64(header, 112);
            ulong observedCanonicalSequence = ReadUInt64(header, 120);
            if ((!hasCanonicalSequence && canonicalSequence != 0) || (hasCanonicalSequence && canonicalSequence == 0) || (!hasObservedCanonicalSequence && observedCanonicalSequence != 0))
                throw new InvalidDataException("Invalid v2 mutation canonical sequence fields.");
            ulong reliableFrameSequence = ReadUInt64(header, 96);
            ulong originSequence = ReadUInt64(header, 104);
            if (reliableFrameSequence == 0 || originSequence == 0)
                throw new InvalidDataException("Committed mutation sequences must be nonzero.");

            byte originDevice = header[128];
            if ((originDevice != (byte)V2OriginDevice.Desktop && originDevice != (byte)V2OriginDevice.Quest) || (originDevice == (byte)V2OriginDevice.Desktop && (!hasCanonicalSequence || hasObservedCanonicalSequence)) || (originDevice == (byte)V2OriginDevice.Quest && !hasObservedCanonicalSequence))
                throw new InvalidDataException("Mutation has invalid device-specific canonical sequence fields.");

            var identities = new Guid[5];
            for (int i = 0; i < identities.Length; i++)
            {
                identities[i] = ReadGuid(header, 16 + i * 16);
                if (identities[i] == Guid.Empty)
                    throw new InvalidDataException("Mutation identities must be nonzero.");
                for (int j = 0; j < i; j++)
                {
                    if (identities[j] == identities[i])
                        throw new InvalidDataException("Mutation identities must be distinct.");
                }
            }

            return checked((int)payloadLength);
        }

        private static bool TryCreateMutationEnvelope(V2TransportRecord record, out V2MutationEnvelope envelope)
        {
            envelope = null;
            if (record.Kind != V2TransportMessageKind.Application || record.Lane != V2ScheduleLane.Interactive || record.ChunkIndex.HasValue || record.BodySchema != 1 || record.PayloadLength < 4 || record.PayloadLength > V2MutationEnvelopeCodec.MaximumPayloadBytes)
                return false;

            V2Mutation mutation;
            try
            {
                mutation = V2MutationPayloadCodec.Decode(record.PayloadBytes);
            }
            catch (InvalidDataException)
            {
                return false;
            }

            envelope = new V2MutationEnvelope(record.SessionId, record.SceneId, record.IncarnationId, record.MessageId, record.StreamId, record.ReliableFrameSequence, record.OriginSequence, record.OriginDevice, record.CanonicalSequence, record.ObservedCanonicalSequence, mutation);
            return true;
        }

        private static V2TransportRecord CreateRecord(V2MutationEnvelope envelope)
        {
            return new V2TransportRecord(V2TransportMessageKind.Application, envelope.SessionId, envelope.SceneId, envelope.IncarnationId, envelope.OperationId, envelope.ReliableStreamId, envelope.ReliableFrameSequence, envelope.OriginSequence, envelope.OriginDevice, V2ScheduleLane.Interactive, 1, payload: V2MutationPayloadCodec.Encode(envelope.Mutation), canonicalSequence: envelope.CanonicalSequence, observedCanonicalSequence: envelope.ObservedCanonicalSequence);
        }

        private static V2TransportRecord CreateRecord(HeaderFields fields, byte[] payload)
        {
            return new V2TransportRecord(fields.Kind, new SessionId(fields.SessionId), fields.SceneId == Guid.Empty ? null : new SceneId(fields.SceneId), fields.IncarnationId == Guid.Empty ? null : new IncarnationId(fields.IncarnationId), fields.MessageId == Guid.Empty ? null : new OperationId(fields.MessageId), fields.StreamId == Guid.Empty ? null : new ReliableStreamId(fields.StreamId), fields.ReliableFrameSequence, fields.OriginSequence, fields.OriginDevice, fields.Lane, fields.BodySchema, fields.ChunkIndex, payload, (fields.Flags & CanonicalSequenceFlag) != 0 ? (ulong?)fields.CanonicalSequence : null, (fields.Flags & ObservedCanonicalSequenceFlag) != 0 ? (ulong?)fields.ObservedCanonicalSequence : null, fields.BulkStreamRetiredThrough);
        }

        private static void Validate(V2TransportRecord record, bool malformed)
        {
            Validate(record.Kind, record.SessionId?.Value ?? Guid.Empty, record.SceneId?.Value ?? Guid.Empty, record.IncarnationId?.Value ?? Guid.Empty, record.MessageId?.Value ?? Guid.Empty, record.StreamId?.Value ?? Guid.Empty, record.ReliableFrameSequence, record.BulkStreamRetiredThrough, record.OriginSequence, record.OriginDevice, record.Lane, record.BodySchema, record.ChunkIndex, record.CanonicalSequence, record.ObservedCanonicalSequence, record.PayloadLength, malformed);
        }

        private static void Validate(HeaderFields fields, bool malformed)
        {
            Validate(fields.Kind, fields.SessionId, fields.SceneId, fields.IncarnationId, fields.MessageId, fields.StreamId, fields.ReliableFrameSequence, fields.BulkStreamRetiredThrough, fields.OriginSequence, fields.OriginDevice, fields.Lane, fields.BodySchema, fields.ChunkIndex, (fields.Flags & CanonicalSequenceFlag) != 0 ? (ulong?)fields.CanonicalSequence : null, (fields.Flags & ObservedCanonicalSequenceFlag) != 0 ? (ulong?)fields.ObservedCanonicalSequence : null, fields.PayloadLength, malformed);
            if (((fields.Flags & CanonicalSequenceFlag) == 0 && fields.CanonicalSequence != 0) || ((fields.Flags & ObservedCanonicalSequenceFlag) == 0 && fields.ObservedCanonicalSequence != 0))
                Throw(malformed, "A v2 transport sequence is nonzero without its presence flag.");
        }

        private static void Validate(V2TransportMessageKind kind, Guid sessionId, Guid sceneId, Guid incarnationId, Guid messageId, Guid streamId, ulong sequence, ulong bulkStreamRetiredThrough, ulong originSequence, V2OriginDevice originDevice, V2ScheduleLane lane, ushort bodySchema, int? chunkIndex, ulong? canonicalSequence, ulong? observedCanonicalSequence, int payloadLength, bool malformed)
        {
            if (sessionId == Guid.Empty)
                Throw(malformed, "A v2 transport session identity must be nonzero.");
            if (payloadLength < 0 || payloadLength > MaximumPayloadBytes)
                Throw(malformed, "V2 transport payload exceeds its bound.");
            if (originDevice != V2OriginDevice.Desktop && originDevice != V2OriginDevice.Quest)
                Throw(malformed, "Invalid v2 transport origin device.");
            if (canonicalSequence == 0 || (observedCanonicalSequence == 0 && originDevice != V2OriginDevice.Quest))
                Throw(malformed, "A present canonical sequence must be nonzero (observed zero is valid only for Quest proposals).");

            switch (kind)
            {
                case V2TransportMessageKind.Application:
                    if (streamId == Guid.Empty)
                        Throw(malformed, "An application record requires a reliable stream identity.");
                    if (lane == V2ScheduleLane.SessionControl)
                    {
                        if (sceneId != Guid.Empty || incarnationId != Guid.Empty || originSequence != 0 || chunkIndex.HasValue)
                            Throw(malformed, "Session-control records cannot carry scene or chunk metadata.");
                    }
                    else
                    {
                        if (sceneId == Guid.Empty || incarnationId == Guid.Empty || messageId == Guid.Empty)
                            Throw(malformed, "Scene records require scene, incarnation and operation identities.");
                        if ((lane == V2ScheduleLane.Bulk) != chunkIndex.HasValue)
                            Throw(malformed, "Bulk records require a chunk index and non-bulk records cannot carry one.");
                        if (lane == V2ScheduleLane.Bulk && (sequence == 0 || originSequence != 0 || bodySchema == 0))
                            Throw(malformed, "Bulk chunks require a reliable sequence and body schema.");
                        if ((lane == V2ScheduleLane.Interactive || lane == V2ScheduleLane.SceneControl) && (sequence == 0 || originSequence == 0))
                            Throw(malformed, "Scene operations require reliable and origin sequences.");
                    }

                    if ((sequence == 0) != (lane == V2ScheduleLane.SessionControl && sequence == 0))
                        Throw(malformed, "Invalid application reliability metadata.");
                    break;
                case V2TransportMessageKind.Acknowledgement:
                    if (streamId == Guid.Empty || sequence == 0 || payloadLength != 0 || sceneId != Guid.Empty || incarnationId != Guid.Empty || messageId != Guid.Empty || originSequence != 0 || chunkIndex.HasValue || bodySchema != 0 || canonicalSequence.HasValue || observedCanonicalSequence.HasValue)
                        Throw(malformed, "Invalid cumulative acknowledgement record.");
                    break;
                case V2TransportMessageKind.Ping:
                case V2TransportMessageKind.Pong:
                    if (messageId == Guid.Empty || streamId != Guid.Empty || sequence != 0 || originSequence != 0 || sceneId != Guid.Empty || incarnationId != Guid.Empty || chunkIndex.HasValue || bodySchema != 0 || payloadLength != (kind == V2TransportMessageKind.Ping ? 16 : 32) || canonicalSequence.HasValue || observedCanonicalSequence.HasValue)
                        Throw(malformed, "Invalid liveness record.");
                    break;
                case V2TransportMessageKind.ResumeHello:
                    if (streamId != Guid.Empty || sequence != 0 || originSequence != 0 || sceneId != Guid.Empty || incarnationId != Guid.Empty || messageId != Guid.Empty || chunkIndex.HasValue || bodySchema != 0 || payloadLength < 4 || payloadLength > 4 + 128 * 24 || canonicalSequence.HasValue || observedCanonicalSequence.HasValue)
                        Throw(malformed, "Invalid resume handshake record.");
                    break;
                case V2TransportMessageKind.BulkStreamRetirement:
                    if (streamId != Guid.Empty || sequence != 0 || bulkStreamRetiredThrough == 0 || originSequence != 0 || sceneId != Guid.Empty || incarnationId != Guid.Empty || messageId != Guid.Empty || chunkIndex.HasValue || bodySchema != 0 || payloadLength != 0 || canonicalSequence.HasValue || observedCanonicalSequence.HasValue)
                        Throw(malformed, "Invalid bulk stream retirement record.");
                    break;
                default:
                    Throw(malformed, "Unsupported v2 transport message kind.");
                    break;
            }

            if (kind != V2TransportMessageKind.ResumeHello && kind != V2TransportMessageKind.BulkStreamRetirement && bulkStreamRetiredThrough != 0)
                Throw(malformed, "A bulk retirement watermark is only valid on resume and retirement records.");
        }

        private static void Throw(bool malformed, string message)
        {
            if (malformed)
                throw new InvalidDataException(message);
            throw new ArgumentException(message);
        }

        private static Guid ReadGuid(byte[] bytes, int offset)
        {
            var value = new byte[16];
            Buffer.BlockCopy(bytes, offset, value, 0, value.Length);
            return new Guid(value);
        }

        private static bool HasMagic(byte[] bytes, int offset, byte[] magic)
        {
            if (bytes == null || magic == null || offset < 0 || bytes.Length - offset < magic.Length)
                return false;
            for (int i = 0; i < magic.Length; i++)
            {
                if (bytes[offset + i] != magic[i])
                    return false;
            }

            return true;
        }

        private static void WriteGuid(byte[] bytes, int offset, Guid value) => Buffer.BlockCopy(value.ToByteArray(), 0, bytes, offset, 16);
        private static ushort ReadUInt16(byte[] bytes, int offset) => (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
        private static uint ReadUInt32(byte[] bytes, int offset) => (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
        private static ulong ReadUInt64(byte[] bytes, int offset) => ReadUInt32(bytes, offset) | ((ulong)ReadUInt32(bytes, offset + 4) << 32);

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            for (int i = 0; i < 4; i++) bytes[offset + i] = (byte)(value >> (8 * i));
        }

        private static void WriteUInt64(byte[] bytes, int offset, ulong value)
        {
            WriteUInt32(bytes, offset, (uint)value);
            WriteUInt32(bytes, offset + 4, (uint)(value >> 32));
        }

        private sealed class HeaderFields
        {
            public V2TransportMessageKind Kind;
            public ushort Flags;
            public int PayloadLength;
            public Guid SessionId;
            public Guid SceneId;
            public Guid IncarnationId;
            public Guid MessageId;
            public Guid StreamId;
            public ulong ReliableFrameSequence;
            public ulong BulkStreamRetiredThrough;
            public ulong OriginSequence;
            public ulong CanonicalSequence;
            public ulong ObservedCanonicalSequence;
            public V2OriginDevice OriginDevice;
            public V2ScheduleLane Lane;
            public int? ChunkIndex;
            public ushort BodySchema;
        }
    }

    public static class V2ResumeWatermarkCodec
    {
        public const int MaximumStreams = 128;
        private const ushort SchemaVersion = 1;

        public static byte[] Encode(IReadOnlyDictionary<ReliableStreamId, ulong> watermarks)
        {
            if (watermarks == null)
                throw new ArgumentNullException(nameof(watermarks));
            if (watermarks.Count > MaximumStreams)
                throw new InvalidDataException("Too many reliable streams in resume state.");
            using (var stream = new MemoryStream(4 + watermarks.Count * 24))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(SchemaVersion);
                writer.Write(checked((ushort)watermarks.Count));
                foreach (KeyValuePair<ReliableStreamId, ulong> item in watermarks)
                {
                    if (item.Key == null || item.Value == 0)
                        throw new InvalidDataException("Resume state requires nonzero stream watermarks.");
                    writer.Write(item.Key.ToByteArray());
                    writer.Write(item.Value);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        public static Dictionary<ReliableStreamId, ulong> Decode(byte[] payload)
        {
            if (payload == null || payload.Length < 4)
                throw new InvalidDataException("Truncated resume state.");
            using (var stream = new MemoryStream(payload, false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                ushort version = reader.ReadUInt16();
                ushort count = reader.ReadUInt16();
                if (version != SchemaVersion || count > MaximumStreams || payload.Length != 4 + count * 24)
                    throw new InvalidDataException("Invalid resume state version or length.");
                var result = new Dictionary<ReliableStreamId, ulong>();
                for (int i = 0; i < count; i++)
                {
                    var identityBytes = reader.ReadBytes(16);
                    if (identityBytes.Length != 16)
                        throw new InvalidDataException("Truncated resume stream identity.");
                    ReliableStreamId identity;
                    try
                    {
                        identity = ReliableStreamId.FromBytes(identityBytes);
                    }
                    catch (ArgumentException exception)
                    {
                        throw new InvalidDataException("Invalid resume stream identity.", exception);
                    }

                    ulong watermark = reader.ReadUInt64();
                    if (watermark == 0 || result.ContainsKey(identity))
                        throw new InvalidDataException("Invalid or duplicate resume stream watermark.");
                    result.Add(identity, watermark);
                }

                return result;
            }
        }
    }

    public sealed class V2ScopedRejectionException : Exception
    {
        public OperationId OperationId { get; }
        public string Code { get; }

        public V2ScopedRejectionException(OperationId operationId, string code, string message) : base(message)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            if (string.IsNullOrWhiteSpace(code) || code.Length > 64)
                throw new ArgumentException("A rejection code must contain 1 to 64 characters.", nameof(code));
            Code = code;
        }
    }

    public static class V2ScopedRejectionCodec
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly byte[] Magic = { (byte)'H', (byte)'B', (byte)'R', (byte)'J' };
        public const int MaximumDiagnosticBytes = 512;

        public static byte[] Encode(OperationId operationId, string code, string diagnostic)
        {
            if (operationId == null)
                throw new ArgumentNullException(nameof(operationId));
            byte[] codeBytes = Encoding.ASCII.GetBytes(code ?? string.Empty);
            byte[] diagnosticBytes = StrictUtf8.GetBytes(diagnostic ?? string.Empty);
            if (codeBytes.Length < 1 || codeBytes.Length > 64 || diagnosticBytes.Length > MaximumDiagnosticBytes)
                throw new InvalidDataException("Scoped rejection data exceeds its bound.");
            foreach (byte value in codeBytes)
            {
                if (value < 0x21 || value > 0x7e)
                    throw new InvalidDataException("Scoped rejection code is not printable ASCII.");
            }

            using (var stream = new MemoryStream(24 + codeBytes.Length + diagnosticBytes.Length))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write((ushort)1);
                writer.Write(operationId.ToByteArray());
                writer.Write((byte)codeBytes.Length);
                writer.Write(codeBytes);
                writer.Write((ushort)diagnosticBytes.Length);
                writer.Write(diagnosticBytes);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static bool TryDecode(byte[] payload, out OperationId operationId, out string code, out string diagnostic)
        {
            operationId = null;
            code = null;
            diagnostic = null;
            if (payload == null || payload.Length < 25 || payload.Length > 4 + 2 + 16 + 1 + 64 + 2 + MaximumDiagnosticBytes)
                return false;
            try
            {
                using (var stream = new MemoryStream(payload, false))
                using (var reader = new BinaryReader(stream, StrictUtf8, true))
                {
                    byte[] magic = reader.ReadBytes(4);
                    for (int i = 0; i < Magic.Length; i++)
                        if (magic[i] != Magic[i])
                            return false;
                    if (reader.ReadUInt16() != 1) return false;
                    byte[] id = reader.ReadBytes(16);
                    if (id.Length != 16) return false;
                    operationId = OperationId.FromBytes(id);
                    int codeLength = reader.ReadByte();
                    if (codeLength < 1 || codeLength > 64) return false;
                    byte[] codeBytes = reader.ReadBytes(codeLength);
                    if (codeBytes.Length != codeLength) return false;
                    for (int i = 0; i < codeBytes.Length; i++)
                        if (codeBytes[i] < 0x21 || codeBytes[i] > 0x7e)
                            return false;
                    int diagnosticLength = reader.ReadUInt16();
                    if (diagnosticLength > MaximumDiagnosticBytes || diagnosticLength != stream.Length - stream.Position) return false;
                    byte[] diagnosticBytes = reader.ReadBytes(diagnosticLength);
                    code = Encoding.ASCII.GetString(codeBytes);
                    diagnostic = StrictUtf8.GetString(diagnosticBytes);
                    return true;
                }
            }
            catch (Exception exception) when (exception is ArgumentException || exception is EndOfStreamException || exception is DecoderFallbackException)
            {
                operationId = null;
                code = null;
                diagnostic = null;
                return false;
            }
        }
    }
}
