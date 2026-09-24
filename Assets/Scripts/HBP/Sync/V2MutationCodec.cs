using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public static class V2MutationPayloadCodec
    {
        public const ushort SchemaVersion = 1;
        public const int MaximumPayloadBytes = 1024;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static byte[] Encode(V2Mutation mutation)
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
                    throw new InvalidDataException("Mutation payload exceeds 1024 bytes.");
                return stream.ToArray();
            }
        }

        public static V2Mutation Decode(byte[] payload)
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
                    V2OperationType operationType = (V2OperationType)reader.ReadUInt16();
                    V2Mutation mutation = ReadBody(reader, operationType);
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

        internal static byte[] EncodeBody(V2Mutation mutation)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, StrictUtf8, true))
            {
                WriteBody(writer, mutation);
                if (stream.Length > MaximumPayloadBytes)
                    throw new InvalidDataException("Mutation body exceeds 1024 bytes.");
                return stream.ToArray();
            }
        }

        internal static V2Mutation ReadBody(BinaryReader reader, V2OperationType operationType)
        {
            switch (operationType)
            {
                case V2OperationType.SetSiteColor:
                    return new SetSiteColor(ReadColumnId(reader), ReadSiteId(reader), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case V2OperationType.SetCutDefinition:
                    {
                        CutId cutId = ReadCutId(reader);
                        byte orientationByte = reader.ReadByte();
                        if (orientationByte > (byte)V2CutOrientation.Custom)
                            throw new InvalidDataException("Invalid cut orientation.");
                        bool flip = ReadBoolean(reader);
                        uint numberOfCuts = reader.ReadUInt32();
                        float position = reader.ReadSingle();
                        float normalX = reader.ReadSingle();
                        float normalY = reader.ReadSingle();
                        float normalZ = reader.ReadSingle();
                        return new SetCutDefinition(cutId, (V2CutOrientation)orientationByte, flip, numberOfCuts, position, normalX, normalY, normalZ);
                    }
                case V2OperationType.SetTimelineAnchor:
                    {
                        ColumnId columnId = ReadColumnId(reader);
                        int index = reader.ReadInt32();
                        bool playing = ReadBoolean(reader);
                        bool looping = ReadBoolean(reader);
                        int step = reader.ReadInt32();
                        long monotonicAnchorTicks = reader.ReadInt64();
                        ulong tickFrequency = reader.ReadUInt64();
                        return new SetTimelineAnchor(columnId, index, playing, looping, step, monotonicAnchorTicks, tickFrequency);
                    }
                default:
                    throw new InvalidDataException("Unsupported mutation operation type.");
            }
        }

        internal static void WriteBody(BinaryWriter writer, V2Mutation mutation)
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
    }

    public static class V2MutationEnvelopeCodec
    {
        public const int HeaderLength = 136;
        public const int MaximumPayloadBytes = V2MutationPayloadCodec.MaximumPayloadBytes;
        public const int MaximumFrameBytes = HeaderLength + MaximumPayloadBytes;
        public const ushort ProtocolVersion = 2;
        public const ushort MutationMessageKind = 1;
        private const ushort CanonicalSequenceFlag = 1;
        private const ushort ObservedCanonicalSequenceFlag = 2;

        public static byte[] Encode(V2MutationEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            byte[] payload = V2MutationPayloadCodec.Encode(envelope.Mutation);
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

        public static V2MutationEnvelope Decode(byte[] frame)
        {
            if (frame == null || frame.Length < HeaderLength)
                throw new InvalidDataException("Truncated v2 mutation header.");
            if (frame.Length > MaximumFrameBytes)
                throw new InvalidDataException("V2 mutation frame exceeds its bound.");
            if (frame[0] != (byte)'H' || frame[1] != (byte)'B' || frame[2] != (byte)'S' || frame[3] != (byte)'2')
                throw new InvalidDataException("Invalid v2 mutation magic.");
            if (ReadUInt16(frame, 4) != ProtocolVersion)
                throw new InvalidDataException("Unsupported protocol version.");
            if (ReadUInt16(frame, 6) != MutationMessageKind)
                throw new InvalidDataException("Unsupported message kind.");

            ushort flags = ReadUInt16(frame, 8);
            if ((flags & ~(CanonicalSequenceFlag | ObservedCanonicalSequenceFlag)) != 0)
                throw new InvalidDataException("Unsupported v2 mutation flags.");
            ushort headerLength = ReadUInt16(frame, 10);
            if (headerLength != HeaderLength)
                throw new InvalidDataException("Invalid v2 mutation header length.");
            uint payloadLength = ReadUInt32(frame, 12);
            if (payloadLength > MaximumPayloadBytes)
                throw new InvalidDataException("V2 mutation payload exceeds its bound.");

            int expectedFrameLength;
            try
            {
                expectedFrameLength = checked(headerLength + (int)payloadLength);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("Invalid v2 mutation frame length.", exception);
            }

            if (frame.Length != expectedFrameLength)
                throw new InvalidDataException("V2 mutation frame length does not match its payload length.");
            for (int i = 129; i < HeaderLength; i++)
            {
                if (frame[i] != 0)
                    throw new InvalidDataException("Nonzero v2 mutation reserved byte.");
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
            if (originDeviceByte != (byte)V2OriginDevice.Desktop && originDeviceByte != (byte)V2OriginDevice.Quest)
                throw new InvalidDataException("Invalid origin device.");
            V2OriginDevice originDevice = (V2OriginDevice)originDeviceByte;
            if ((originDevice == V2OriginDevice.Desktop && (!hasCanonicalSequence || hasObservedCanonicalSequence)) || (originDevice == V2OriginDevice.Quest && !hasObservedCanonicalSequence))
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
                throw new InvalidDataException("Invalid v2 mutation identity.", exception);
            }

            // No declared-body buffer is allocated until the complete fixed header has passed validation.
            var payload = new byte[(int)payloadLength];
            Buffer.BlockCopy(frame, HeaderLength, payload, 0, payload.Length);
            V2Mutation mutation = V2MutationPayloadCodec.Decode(payload);
            try
            {
                return new V2MutationEnvelope(sessionId, sceneId, incarnationId, operationId, reliableStreamId, reliableFrameSequence, originSequence, originDevice, hasCanonicalSequence ? (ulong?)canonicalSequence : null, hasObservedCanonicalSequence ? (ulong?)observedCanonicalSequence : null, mutation);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid v2 mutation envelope.", exception);
            }
        }

        private static void EnsureDistinctIdentityValues(params V2BinaryIdentity[] identities)
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

    internal static class V2CheckpointRecordCodec
    {
        private const ushort SchemaVersion = 1;
        private const int PrefixLength = 6;

        internal static byte[] Encode(ushort recordType, V2Mutation mutation)
        {
            byte[] body = V2MutationPayloadCodec.EncodeBody(mutation);
            using (var stream = new MemoryStream(PrefixLength + body.Length))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(recordType);
                writer.Write(SchemaVersion);
                writer.Write((ushort)body.Length);
                writer.Write(body);
                return stream.ToArray();
            }
        }

        internal static V2Mutation Decode(byte[] bytes, V2OperationType expectedType)
        {
            if (bytes == null || bytes.Length < PrefixLength || bytes.Length > PrefixLength + V2MutationPayloadCodec.MaximumPayloadBytes)
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
                    if (schemaVersion != SchemaVersion)
                        throw new InvalidDataException("Unsupported checkpoint record schema.");
                    if (bodyLength > V2MutationPayloadCodec.MaximumPayloadBytes || PrefixLength + bodyLength != bytes.Length)
                        throw new InvalidDataException("Invalid checkpoint body length.");

                    V2Mutation mutation = V2MutationPayloadCodec.ReadBody(reader, expectedType);
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
    }
}
