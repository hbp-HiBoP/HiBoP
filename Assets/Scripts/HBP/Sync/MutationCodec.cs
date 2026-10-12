using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public static class MutationPayloadCodec
    {
        public const ushort SchemaVersion = 2;
        public const int MaximumPayloadBytes = 16 * 1024 * 1024;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static byte[] Encode(Mutation mutation)
        {
            if (mutation == null)
                throw new ArgumentNullException(nameof(mutation));

            byte[] encodedBody = EncodeBody(mutation);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, StrictUtf8, true))
            {
                writer.Write(SchemaVersion);
                writer.Write((ushort)mutation.Type);
                writer.Write(encodedBody);
                if (stream.Length > MaximumPayloadBytes)
                    throw new InvalidDataException("Mutation payload exceeds the reliable bulk bound.");
                return stream.ToArray();
            }
        }

        public static Mutation Decode(byte[] payload)
        {
            if (payload == null || payload.Length < 4 || payload.Length > MaximumPayloadBytes)
                throw new InvalidDataException("Invalid mutation payload length.");

            using (var stream = new MemoryStream(payload, false))
            using (var reader = new BinaryReader(stream, StrictUtf8))
            {
                try
                {
                    ushort schemaVersion = reader.ReadUInt16();
                    if (schemaVersion != SchemaVersion)
                        throw new InvalidDataException("Unsupported mutation schema.");
                    OperationType operationType = (OperationType)reader.ReadUInt16();
                    Mutation mutation = ReadBody(reader, operationType);
                    if (stream.Position != stream.Length)
                        throw new InvalidDataException("Trailing mutation payload bytes.");
                    return mutation;
                }
                catch (EndOfStreamException exception)
                {
                    throw new InvalidDataException("Truncated mutation payload.", exception);
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidDataException("Invalid mutation payload value.", exception);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidDataException("Invalid mutation payload length.", exception);
                }
            }
        }

        internal static byte[] EncodeBody(Mutation mutation)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, StrictUtf8, true))
            {
                WriteBody(writer, mutation);
                if (stream.Length > MaximumPayloadBytes)
                    throw new InvalidDataException("Mutation body exceeds the reliable bulk bound.");
                return stream.ToArray();
            }
        }

        internal static Mutation ReadBody(BinaryReader reader, OperationType operationType)
        {
            switch (operationType)
            {
                case OperationType.SetSiteColor:
                    return new SetSiteColor(ReadColumnId(reader), ReadSiteId(reader), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case OperationType.SetCutDefinition:
                    {
                        CutId cutId = ReadCutId(reader);
                        byte orientationByte = reader.ReadByte();
                        if (orientationByte > (byte)MutationCutOrientation.Custom)
                            throw new InvalidDataException("Invalid cut orientation.");
                        bool flip = ReadBoolean(reader);
                        uint numberOfCuts = reader.ReadUInt32();
                        float position = reader.ReadSingle();
                        float normalX = reader.ReadSingle();
                        float normalY = reader.ReadSingle();
                        float normalZ = reader.ReadSingle();
                        return new SetCutDefinition(cutId, (MutationCutOrientation)orientationByte, flip, numberOfCuts, position, normalX, normalY, normalZ);
                    }
                case OperationType.SetTimelineAnchor:
                    {
                        ColumnId columnId = ReadColumnId(reader);
                        int index = reader.ReadInt32();
                        bool playing = ReadBoolean(reader);
                        bool looping = ReadBoolean(reader);
                        int step = reader.ReadInt32();
                        long monotonicAnchorTicks = reader.ReadInt64();
                        ulong tickFrequency = reader.ReadUInt64();
                        TimelineAnchorIntent intent = ReadTimelineAnchorIntent(reader);
                        return new SetTimelineAnchor(columnId, index, playing, looping, step, monotonicAnchorTicks, tickFrequency, intent);
                    }
                case OperationType.SetSiteFilterResult:
                    return T12MutationCodec.ReadBody(reader);
                case OperationType.SetCorrelationResult:
                    return T13MutationCodec.ReadBody(reader);
                default:
                    if ((ushort)operationType >= (ushort)OperationType.SetSiteBlacklist) return T11MutationCodec.ReadBody(reader, operationType);
                    return (ushort)operationType >= (ushort)OperationType.CreateCut ? T10MutationCodec.ReadBody(reader, operationType) : T09MutationCodec.ReadBody(reader, operationType);
            }
        }

        internal static void WriteBody(BinaryWriter writer, Mutation mutation)
        {
            if (mutation is SetSiteColor siteColor)
            {
                WriteTextIdentity(writer, siteColor.ColumnId.Value);
                WriteTextIdentity(writer, siteColor.FullSiteId.Value);
                writer.Write(siteColor.Red);
                writer.Write(siteColor.Green);
                writer.Write(siteColor.Blue);
                writer.Write(siteColor.Alpha);
                return;
            }

            if (mutation is SetCutDefinition cutDefinition)
            {
                WriteTextIdentity(writer, cutDefinition.CutId.Value);
                writer.Write((byte)cutDefinition.Orientation);
                writer.Write((byte)(cutDefinition.Flip ? 1 : 0));
                writer.Write(cutDefinition.NumberOfCuts);
                writer.Write(cutDefinition.Position);
                writer.Write(cutDefinition.NormalX);
                writer.Write(cutDefinition.NormalY);
                writer.Write(cutDefinition.NormalZ);
                return;
            }

            if (mutation is SetTimelineAnchor timelineAnchor)
            {
                WriteTextIdentity(writer, timelineAnchor.ColumnId.Value);
                writer.Write(timelineAnchor.Index);
                writer.Write((byte)(timelineAnchor.Playing ? 1 : 0));
                writer.Write((byte)(timelineAnchor.Looping ? 1 : 0));
                writer.Write(timelineAnchor.Step);
                writer.Write(timelineAnchor.MonotonicAnchorTicks);
                writer.Write(timelineAnchor.TickFrequency);
                writer.Write((byte)timelineAnchor.Intent);
                return;
            }

            if (mutation is SetSiteFilterResult)
            {
                T12MutationCodec.WriteBody(writer, mutation);
                return;
            }

            if (mutation is SetCorrelationResult)
            {
                T13MutationCodec.WriteBody(writer, mutation);
                return;
            }

            if ((ushort)mutation.Type >= (ushort)OperationType.SetSiteBlacklist)
            {
                T11MutationCodec.WriteBody(writer, mutation);
                return;
            }

            if ((ushort)mutation.Type >= (ushort)OperationType.CreateCut)
            {
                T10MutationCodec.WriteBody(writer, mutation);
                return;
            }

            if ((ushort)mutation.Type >= (ushort)OperationType.SetSelectedColumn)
            {
                T09MutationCodec.WriteBody(writer, mutation);
                return;
            }

            throw new ArgumentException("Unsupported mutation operation.", nameof(mutation));
        }

        private static ColumnId ReadColumnId(BinaryReader reader) => new ColumnId(ReadTextIdentity(reader));
        private static SiteId ReadSiteId(BinaryReader reader) => new SiteId(ReadTextIdentity(reader));
        private static CutId ReadCutId(BinaryReader reader) => new CutId(ReadTextIdentity(reader));

        private static string ReadTextIdentity(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            if (length == 0 || length > 256 || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid text identity length.");

            byte[] bytes = reader.ReadBytes(length);
            string value;
            try
            {
                value = StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Malformed UTF-8 identity.", exception);
            }

            if (!StringComparer.Ordinal.Equals(value, value.Normalize(NormalizationForm.FormC)))
                throw new InvalidDataException("Text identity is not NFC-normalized.");
            foreach (char character in value)
            {
                if (char.IsControl(character))
                    throw new InvalidDataException("Text identity contains a control character.");
            }

            return value;
        }

        private static void WriteTextIdentity(BinaryWriter writer, string value)
        {
            byte[] bytes;
            try
            {
                bytes = StrictUtf8.GetBytes(value);
            }
            catch (EncoderFallbackException exception)
            {
                throw new InvalidDataException("Malformed Unicode identity.", exception);
            }

            if (bytes.Length == 0 || bytes.Length > 256)
                throw new InvalidDataException("Text identity exceeds its bounds.");
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }

        private static bool ReadBoolean(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1)
                throw new InvalidDataException("Invalid boolean value.");
            return value == 1;
        }

        private static TimelineAnchorIntent ReadTimelineAnchorIntent(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (!Enum.IsDefined(typeof(TimelineAnchorIntent), value))
                throw new InvalidDataException("Invalid timeline anchor intent.");
            return (TimelineAnchorIntent)value;
        }
    }

    public static class MutationEnvelopeCodec
    {
        public const int HeaderLength = 136;
        public const int MaximumPayloadBytes = MutationPayloadCodec.MaximumPayloadBytes;
        public const int MaximumFrameBytes = HeaderLength + MaximumPayloadBytes;
        public const ushort ProtocolVersion = 2;
        public const ushort MutationMessageKind = 1;
        private const ushort CanonicalSequenceFlag = 1;
        private const ushort ObservedCanonicalSequenceFlag = 2;

        public static byte[] Encode(MutationEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            byte[] payload = MutationPayloadCodec.Encode(envelope.Mutation);
            using (var stream = new MemoryStream(HeaderLength + payload.Length))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)'H');
                writer.Write((byte)'B');
                writer.Write((byte)'S');
                writer.Write((byte)'2');
                writer.Write(ProtocolVersion);
                writer.Write(MutationMessageKind);
                ushort flags = 0;
                if (envelope.CanonicalSequence.HasValue)
                    flags |= CanonicalSequenceFlag;
                if (envelope.ObservedCanonicalSequence.HasValue)
                    flags |= ObservedCanonicalSequenceFlag;
                writer.Write(flags);
                writer.Write((ushort)HeaderLength);
                writer.Write((uint)payload.Length);
                writer.Write(envelope.SessionId.ToByteArray());
                writer.Write(envelope.SceneId.ToByteArray());
                writer.Write(envelope.IncarnationId.ToByteArray());
                writer.Write(envelope.OperationId.ToByteArray());
                writer.Write(envelope.ReliableStreamId.ToByteArray());
                writer.Write(envelope.ReliableFrameSequence);
                writer.Write(envelope.OriginSequence);
                writer.Write(envelope.CanonicalSequence.GetValueOrDefault());
                writer.Write(envelope.ObservedCanonicalSequence.GetValueOrDefault());
                writer.Write((byte)envelope.OriginDevice);
                for (int i = 0; i < 7; i++)
                    writer.Write((byte)0);
                writer.Write(payload);
                return stream.ToArray();
            }
        }

        public static MutationEnvelope Decode(byte[] frame)
        {
            if (frame == null || frame.Length < HeaderLength)
                throw new InvalidDataException("Truncated sync mutation header.");
            if (frame.Length > MaximumFrameBytes)
                throw new InvalidDataException("Sync mutation frame exceeds its bound.");
            if (frame[0] != (byte)'H' || frame[1] != (byte)'B' || frame[2] != (byte)'S' || frame[3] != (byte)'2')
                throw new InvalidDataException("Invalid sync mutation magic.");
            if (ReadUInt16(frame, 4) != ProtocolVersion)
                throw new InvalidDataException("Unsupported protocol version.");
            if (ReadUInt16(frame, 6) != MutationMessageKind)
                throw new InvalidDataException("Unsupported message kind.");

            ushort flags = ReadUInt16(frame, 8);
            if ((flags & ~(CanonicalSequenceFlag | ObservedCanonicalSequenceFlag)) != 0)
                throw new InvalidDataException("Unsupported sync mutation flags.");
            ushort headerLength = ReadUInt16(frame, 10);
            if (headerLength != HeaderLength)
                throw new InvalidDataException("Invalid sync mutation header length.");
            uint payloadLength = ReadUInt32(frame, 12);
            if (payloadLength > MaximumPayloadBytes)
                throw new InvalidDataException("Sync mutation payload exceeds its bound.");

            int expectedFrameLength;
            try
            {
                expectedFrameLength = checked(headerLength + (int)payloadLength);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("Invalid sync mutation frame length.", exception);
            }

            if (frame.Length != expectedFrameLength)
                throw new InvalidDataException("Sync mutation frame length does not match its payload length.");
            for (int i = 129; i < HeaderLength; i++)
            {
                if (frame[i] != 0)
                    throw new InvalidDataException("Nonzero sync mutation reserved byte.");
            }

            bool hasCanonicalSequence = (flags & CanonicalSequenceFlag) != 0;
            bool hasObservedCanonicalSequence = (flags & ObservedCanonicalSequenceFlag) != 0;
            ulong reliableFrameSequence = ReadUInt64(frame, 96);
            ulong originSequence = ReadUInt64(frame, 104);
            ulong canonicalSequence = ReadUInt64(frame, 112);
            ulong observedCanonicalSequence = ReadUInt64(frame, 120);
            if ((!hasCanonicalSequence && canonicalSequence != 0) || (hasCanonicalSequence && canonicalSequence == 0))
                throw new InvalidDataException("Invalid canonical sequence presence or value.");
            if (!hasObservedCanonicalSequence && observedCanonicalSequence != 0)
                throw new InvalidDataException("Observed canonical sequence is nonzero without its flag.");
            if (reliableFrameSequence == 0 || originSequence == 0)
                throw new InvalidDataException("Committed mutation sequences must be nonzero.");

            byte originDeviceByte = frame[128];
            if (originDeviceByte != (byte)OriginDevice.Desktop && originDeviceByte != (byte)OriginDevice.Quest)
                throw new InvalidDataException("Invalid origin device.");
            OriginDevice originDevice = (OriginDevice)originDeviceByte;
            if ((originDevice == OriginDevice.Desktop && (!hasCanonicalSequence || hasObservedCanonicalSequence)) || (originDevice == OriginDevice.Quest && !hasObservedCanonicalSequence))
                throw new InvalidDataException("Mutation has invalid device-specific canonical sequence fields.");

            SessionId sessionId;
            SceneId sceneId;
            IncarnationId incarnationId;
            OperationId operationId;
            ReliableStreamId reliableStreamId;
            try
            {
                sessionId = new SessionId(ReadGuid(frame, 16));
                sceneId = new SceneId(ReadGuid(frame, 32));
                incarnationId = new IncarnationId(ReadGuid(frame, 48));
                operationId = new OperationId(ReadGuid(frame, 64));
                reliableStreamId = new ReliableStreamId(ReadGuid(frame, 80));
                EnsureDistinctIdentityValues(sessionId, sceneId, incarnationId, operationId, reliableStreamId);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid sync mutation identity.", exception);
            }

            // No declared-body buffer is allocated until the complete fixed header has passed validation.
            var payload = new byte[(int)payloadLength];
            Buffer.BlockCopy(frame, HeaderLength, payload, 0, payload.Length);
            Mutation mutation = MutationPayloadCodec.Decode(payload);
            try
            {
                return new MutationEnvelope(sessionId, sceneId, incarnationId, operationId, reliableStreamId, reliableFrameSequence, originSequence, originDevice, hasCanonicalSequence ? (ulong?)canonicalSequence : null, hasObservedCanonicalSequence ? (ulong?)observedCanonicalSequence : null, mutation);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid sync mutation envelope.", exception);
            }
        }

        private static void EnsureDistinctIdentityValues(params BinaryIdentity[] identities)
        {
            for (int i = 0; i < identities.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (identities[i].Value == identities[j].Value)
                        throw new ArgumentException("Envelope identities must be distinct.");
                }
            }
        }

        private static Guid ReadGuid(byte[] bytes, int offset)
        {
            var value = new byte[16];
            Buffer.BlockCopy(bytes, offset, value, 0, value.Length);
            return new Guid(value);
        }

        private static ushort ReadUInt16(byte[] bytes, int offset) => (ushort)(bytes[offset] | bytes[offset + 1] << 8);

        private static uint ReadUInt32(byte[] bytes, int offset) => (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);

        private static ulong ReadUInt64(byte[] bytes, int offset) => ReadUInt32(bytes, offset) | ((ulong)ReadUInt32(bytes, offset + 4) << 32);
    }

    internal static class CheckpointRecordCodec
    {
        private const ushort SchemaVersion = 1;
        private const ushort TimelineAnchorSchemaVersion = 2;
        private const int PrefixLength = 6;

        internal static byte[] Encode(ushort recordType, Mutation mutation)
        {
            byte[] body = MutationPayloadCodec.EncodeBody(mutation);
            ushort schemaVersion = mutation is SetTimelineAnchor ? TimelineAnchorSchemaVersion : SchemaVersion;
            using (var stream = new MemoryStream(PrefixLength + body.Length))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(recordType);
                writer.Write(schemaVersion);
                writer.Write((ushort)body.Length);
                writer.Write(body);
                return stream.ToArray();
            }
        }

        internal static Mutation Decode(byte[] bytes, OperationType expectedType)
        {
            if (bytes == null || bytes.Length < PrefixLength || bytes.Length > PrefixLength + MutationPayloadCodec.MaximumPayloadBytes)
                throw new InvalidDataException("Invalid checkpoint record length.");

            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream))
            {
                try
                {
                    ushort recordType = reader.ReadUInt16();
                    ushort schemaVersion = reader.ReadUInt16();
                    ushort bodyLength = reader.ReadUInt16();
                    if (recordType != (ushort)expectedType)
                        throw new InvalidDataException("Unexpected checkpoint record type.");
                    bool timelineAnchor = expectedType == OperationType.SetTimelineAnchor;
                    if (schemaVersion != (timelineAnchor ? TimelineAnchorSchemaVersion : SchemaVersion))
                        throw new InvalidDataException("Unsupported checkpoint record schema.");
                    if (PrefixLength + bodyLength != bytes.Length)
                        throw new InvalidDataException("Invalid checkpoint body length.");

                    Mutation mutation = MutationPayloadCodec.ReadBody(reader, expectedType);
                    if (stream.Position != stream.Length)
                        throw new InvalidDataException("Trailing checkpoint record bytes.");
                    return mutation;
                }
                catch (EndOfStreamException exception)
                {
                    throw new InvalidDataException("Truncated checkpoint record.", exception);
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidDataException("Invalid checkpoint record value.", exception);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidDataException("Invalid checkpoint record length.", exception);
                }
            }
        }

        internal static Mutation DecodeAny(byte[] bytes)
        {
            if (bytes == null || bytes.Length < PrefixLength || bytes.Length > PrefixLength + MutationPayloadCodec.MaximumPayloadBytes)
                throw new InvalidDataException("Invalid checkpoint record length.");
            ushort type = (ushort)(bytes[0] | bytes[1] << 8);
            if (type < (ushort)OperationType.SetSelectedColumn || type > (ushort)OperationType.SetSelectedRoiSphere)
                throw new InvalidDataException("Unsupported T09 checkpoint record type.");
            return Decode(bytes, (OperationType)type);
        }
    }
}
