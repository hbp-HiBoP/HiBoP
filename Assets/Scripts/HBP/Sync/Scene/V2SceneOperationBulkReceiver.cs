using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    /// <summary>Reassembles one ordered structural mutation whose body travels on the reliable bulk lane.</summary>
    public sealed class V2SceneOperationBulkReceiver
    {
        public const ushort BodySchema = 1;
        private static readonly byte[] DescriptorMagic = System.Text.Encoding.ASCII.GetBytes("HBSO");
        private const int DescriptorMaximumBytes = V2SchedulerLimits.DefaultInlineThresholdBytes;
        private const int MaximumBodyBytes = 16 * 1024 * 1024;
        private const int MaximumChunkBytes = V2TransportFrameCodec.MaximumPayloadBytes;

        private V2TransportRecord m_Descriptor;
        private ReliableStreamId m_BulkStreamId;
        private byte[] m_Digest;
        private byte[] m_TouchedKeyFingerprints;
        private MemoryStream m_Body;
        private int m_ChunkSize;
        private int m_ChunkCount;
        private int m_NextChunk;
        private int m_TotalLength;

        public bool IsActive => m_Body != null;
        public OperationId ActiveOperationId => m_Descriptor?.MessageId;

        public bool IsChunkForActiveTransfer(V2TransportRecord record) => m_Body != null && record != null && record.Lane == V2ScheduleLane.Bulk && record.ChunkIndex.HasValue && record.StreamId != null && record.StreamId.Equals(m_BulkStreamId);

        /// <summary>Consumes one matching buffered chunk while leaving interleaved records queued in their original order.</summary>
        public bool TryAppendNextBuffered<T>(Queue<T> records, Func<T, V2TransportRecord> getRecord, out T consumedRecord, out V2TransportRecord completed)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (getRecord == null) throw new ArgumentNullException(nameof(getRecord));
            consumedRecord = default;
            completed = null;
            if (!IsActive || records.Count == 0) return false;

            bool found = false;
            T selected = default;
            int count = records.Count;
            for (int i = 0; i < count; i++)
            {
                T candidate = records.Dequeue();
                V2TransportRecord record = getRecord(candidate);
                if (!found && IsChunkForActiveTransfer(record))
                {
                    found = true;
                    selected = candidate;
                    if (!TryAppend(record, out completed))
                        throw new InvalidDataException("A buffered scene-operation chunk stopped matching its active transfer.");
                }
                else
                {
                    records.Enqueue(candidate);
                }
            }

            if (!found) return false;
            consumedRecord = selected;
            return true;
        }

        public bool IsMutationDescriptor(V2TransportRecord record)
        {
            if (record == null || record.Kind != V2TransportMessageKind.Application || record.Lane != V2ScheduleLane.SceneControl || record.BodySchema != BodySchema || record.ChunkIndex.HasValue || record.PayloadLength < DescriptorMagic.Length + 2)
                return false;
            byte[] payload = record.GetPayloadCopy();
            for (int i = 0; i < DescriptorMagic.Length; i++)
                if (payload[i] != DescriptorMagic[i])
                    return false;
            return true;
        }

        public void Begin(V2TransportRecord record)
        {
            if (!IsMutationDescriptor(record)) throw new InvalidDataException("Expected an ordered scene-operation bulk descriptor.");
            if (m_Body != null) throw new InvalidDataException("A scene-operation bulk transfer is already active.");
            if (record.SceneId == null || record.IncarnationId == null || record.MessageId == null)
                throw new InvalidDataException("Scene-operation bulk descriptor has no scene identity.");

            byte[] payload = record.GetPayloadCopy();
            if (payload.Length > DescriptorMaximumBytes) throw new InvalidDataException("Scene-operation bulk descriptor exceeds the inline bound.");
            using var stream = new MemoryStream(payload, false);
            using var reader = new BinaryReader(stream);
            try
            {
                if (!reader.ReadBytes(DescriptorMagic.Length).SequenceEqual(DescriptorMagic)) throw new InvalidDataException("Unsupported scene-operation bulk descriptor signature.");
                if (reader.ReadUInt16() != 1) throw new InvalidDataException("Unsupported scene-operation bulk descriptor version.");
                var operationId = new OperationId(ReadGuid(reader));
                ushort bodySchema = reader.ReadUInt16();
                uint totalLength = reader.ReadUInt32();
                uint chunkSize = reader.ReadUInt32();
                uint chunkCount = reader.ReadUInt32();
                var streamId = new ReliableStreamId(ReadGuid(reader));
                byte digestLength = reader.ReadByte();
                byte[] digest = reader.ReadBytes(digestLength);
                V2BarrierScope barrierScope = (V2BarrierScope)reader.ReadByte();
                ushort touchedKeyCount = reader.ReadUInt16();
                byte[] touchedKeyFingerprints = reader.ReadBytes(touchedKeyCount * 16);

                if (digestLength != 32 || digest.Length != digestLength || bodySchema != BodySchema || totalLength == 0 || totalLength > MaximumBodyBytes || chunkSize == 0 || chunkSize > MaximumChunkBytes || chunkCount == 0 || chunkCount != (totalLength + chunkSize - 1) / chunkSize || barrierScope != V2BarrierScope.AllScene || touchedKeyCount == 0 || touchedKeyCount > 128 || touchedKeyFingerprints.Length != touchedKeyCount * 16 || stream.Position != stream.Length)
                    throw new InvalidDataException("Invalid scene-operation bulk descriptor fields.");
                if (!record.MessageId.Equals(operationId) || !V2BulkStreamIdentityCodec.TryGetOrdinal(record.SessionId, record.OriginDevice, streamId, out _))
                    throw new InvalidDataException("Scene-operation bulk descriptor identity does not match its transport record.");
                if (record.OriginDevice != V2OriginDevice.Desktop && record.OriginDevice != V2OriginDevice.Quest)
                    throw new InvalidDataException("Scene-operation bulk descriptor has an invalid origin.");

                m_Descriptor = record;
                m_BulkStreamId = streamId;
                m_Digest = digest;
                m_TouchedKeyFingerprints = touchedKeyFingerprints;
                m_ChunkSize = (int)chunkSize;
                m_ChunkCount = (int)chunkCount;
                m_TotalLength = (int)totalLength;
                m_Body = new MemoryStream(m_TotalLength);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated scene-operation bulk descriptor.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid scene-operation bulk descriptor identity.", exception);
            }
        }

        /// <summary>Appends an in-order chunk and returns a synthetic ordered scene-operation record on completion.</summary>
        public bool TryAppend(V2TransportRecord record, out V2TransportRecord completed)
        {
            completed = null;
            if (m_Body == null || record == null || record.Lane != V2ScheduleLane.Bulk || !record.ChunkIndex.HasValue || !record.StreamId.Equals(m_BulkStreamId))
                return false;
            if (!record.SessionId.Equals(m_Descriptor.SessionId) || !record.SceneId.Equals(m_Descriptor.SceneId) || !record.IncarnationId.Equals(m_Descriptor.IncarnationId) || !record.MessageId.Equals(m_Descriptor.MessageId) || record.OriginDevice != m_Descriptor.OriginDevice || record.BodySchema != BodySchema || record.ChunkIndex.Value != m_NextChunk)
                throw new InvalidDataException("Scene-operation bulk chunk is out of order or has mismatched identity.");

            int expectedBytes = Math.Min(m_ChunkSize, m_TotalLength - checked(m_NextChunk * m_ChunkSize));
            if (record.PayloadLength != expectedBytes || m_NextChunk >= m_ChunkCount)
                throw new InvalidDataException("Scene-operation bulk chunk has an invalid size or index.");
            byte[] chunk = record.GetPayloadCopy();
            m_Body.Write(chunk, 0, chunk.Length);
            m_NextChunk++;
            if (m_NextChunk != m_ChunkCount) return true;
            if (m_Body.Length != m_TotalLength) throw new InvalidDataException("Scene-operation bulk body has an invalid total length.");

            byte[] body = m_Body.ToArray();
            using (SHA256 sha = SHA256.Create())
                if (!Equal(sha.ComputeHash(body), m_Digest))
                    throw new InvalidDataException("Scene-operation bulk body digest mismatch.");

            V2Mutation mutation = V2MutationPayloadCodec.Decode(body);
            V2ScheduleDescriptor expected = V2ScheduleDescriptor.ForMutation(m_Descriptor.SceneId, m_Descriptor.IncarnationId, mutation);
            byte[] expectedFingerprints = expected.EncodeTouchedKeyFingerprints();
            if (expected.BarrierScope != V2BarrierScope.AllScene || expected.TouchedKeys.Count != m_TouchedKeyFingerprints.Length / 16 || expectedFingerprints.Length != m_TouchedKeyFingerprints.Length + 2)
                throw new InvalidDataException("Scene-operation bulk descriptor does not match its mutation barrier.");
            for (int i = 0; i < m_TouchedKeyFingerprints.Length; i++)
                if (expectedFingerprints[i + 2] != m_TouchedKeyFingerprints[i])
                    throw new InvalidDataException("Scene-operation bulk descriptor touched keys do not match its mutation.");

            completed = new V2TransportRecord(m_Descriptor.Kind, m_Descriptor.SessionId, m_Descriptor.SceneId, m_Descriptor.IncarnationId, m_Descriptor.MessageId, m_Descriptor.StreamId, m_Descriptor.ReliableFrameSequence, m_Descriptor.OriginSequence, m_Descriptor.OriginDevice, m_Descriptor.Lane, BodySchema, payload: body, canonicalSequence: m_Descriptor.CanonicalSequence, observedCanonicalSequence: m_Descriptor.ObservedCanonicalSequence);
            completed.SetReceivePoints(m_Descriptor.FirstReceived, record.LastReceived);
            Reset();
            return true;
        }

        public void Reset()
        {
            m_Body?.Dispose();
            m_Body = null;
            m_Descriptor = null;
            m_BulkStreamId = null;
            m_Digest = null;
            m_TouchedKeyFingerprints = null;
            m_ChunkSize = 0;
            m_ChunkCount = 0;
            m_NextChunk = 0;
            m_TotalLength = 0;
        }

        private static Guid ReadGuid(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(16);
            if (bytes.Length != 16) throw new EndOfStreamException();
            return new Guid(bytes);
        }

        private static bool Equal(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
