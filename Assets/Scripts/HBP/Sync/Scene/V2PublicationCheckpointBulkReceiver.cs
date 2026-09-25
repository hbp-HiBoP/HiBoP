using System;
using System.IO;
using System.Security.Cryptography;
using HBP.Sync;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    public sealed class V2PublicationCheckpointBulkReceiver
    {
        public const ushort BodySchema = 2;
        private const int DescriptorMaximumBytes = V2SchedulerLimits.DefaultInlineThresholdBytes;
        private const int MaximumBodyBytes = 16 * 1024 * 1024;
        private const int MaximumChunkBytes = V2TransportFrameCodec.MaximumPayloadBytes;

        private OperationId m_OperationId;
        private ReliableStreamId m_StreamId;
        private SessionId m_SessionId;
        private SceneId m_SceneId;
        private IncarnationId m_IncarnationId;
        private V2OriginDevice m_OriginDevice;
        private byte[] m_Digest;
        private MemoryStream m_Body;
        private int m_ChunkSize;
        private int m_ChunkCount;
        private int m_NextChunk;
        private int m_TotalLength;

        public bool IsActive => m_Body != null;

        public bool IsCheckpointDescriptor(V2TransportRecord record)
        {
            if (record == null || record.Kind != V2TransportMessageKind.Application || record.Lane != V2ScheduleLane.SceneControl || record.BodySchema != BodySchema || record.ChunkIndex.HasValue)
                return false;
            if (record.PayloadLength < 2) return false;
            byte[] payload = record.GetPayloadCopy();
            return payload[0] == 1 && payload[1] == 0;
        }

        public void Begin(V2TransportRecord record, V2OriginDevice expectedOrigin)
        {
            if (!IsCheckpointDescriptor(record)) throw new InvalidDataException("Expected a scene-checkpoint bulk descriptor.");
            if (m_Body != null) throw new InvalidDataException("A checkpoint bulk transfer is already active.");
            if (expectedOrigin != V2OriginDevice.Desktop && expectedOrigin != V2OriginDevice.Quest)
                throw new ArgumentOutOfRangeException(nameof(expectedOrigin));
            if (record.OriginDevice != expectedOrigin || record.SceneId == null || record.IncarnationId == null || record.MessageId == null)
                throw new InvalidDataException("Checkpoint bulk descriptor has the wrong origin or scene scope.");

            byte[] payload = record.GetPayloadCopy();
            if (payload.Length > DescriptorMaximumBytes) throw new InvalidDataException("Checkpoint bulk descriptor exceeds the inline bound.");
            using var stream = new MemoryStream(payload, false);
            using var reader = new BinaryReader(stream);
            try
            {
                if (reader.ReadUInt16() != 1) throw new InvalidDataException("Unsupported bulk descriptor version.");
                var operationId = new OperationId(ReadGuid(reader));
                ushort bodySchema = reader.ReadUInt16();
                uint totalLength = reader.ReadUInt32();
                uint chunkSize = reader.ReadUInt32();
                uint chunkCount = reader.ReadUInt32();
                var bulkStreamId = new ReliableStreamId(ReadGuid(reader));
                byte digestLength = reader.ReadByte();
                byte[] digest = reader.ReadBytes(digestLength);
                V2BarrierScope barrierScope = (V2BarrierScope)reader.ReadByte();
                ushort touchedKeyCount = reader.ReadUInt16();

                if (digestLength != 32 || digest.Length != digestLength || bodySchema != BodySchema || totalLength == 0 || totalLength > MaximumBodyBytes || chunkSize == 0 || chunkSize > MaximumChunkBytes || chunkCount == 0 || chunkCount != (totalLength + chunkSize - 1) / chunkSize || barrierScope != V2BarrierScope.AllScene || touchedKeyCount != 0 || stream.Position != stream.Length)
                    throw new InvalidDataException("Invalid checkpoint bulk descriptor fields.");
                if (!record.MessageId.Equals(operationId) || !V2BulkStreamIdentityCodec.TryGetOrdinal(record.SessionId, expectedOrigin, bulkStreamId, out _))
                    throw new InvalidDataException("Checkpoint bulk descriptor identity does not match its transport record.");

                m_OperationId = operationId;
                m_StreamId = bulkStreamId;
                m_SessionId = record.SessionId;
                m_SceneId = record.SceneId;
                m_IncarnationId = record.IncarnationId;
                m_OriginDevice = expectedOrigin;
                m_Digest = digest;
                m_ChunkSize = (int)chunkSize;
                m_ChunkCount = (int)chunkCount;
                m_TotalLength = (int)totalLength;
                m_Body = new MemoryStream(m_TotalLength);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated checkpoint bulk descriptor.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid checkpoint bulk descriptor identity.", exception);
            }
        }

        public bool TryAppend(V2TransportRecord record, out byte[] body, out OperationId operationId)
        {
            body = null;
            operationId = null;
            if (m_Body == null || record == null || record.Lane != V2ScheduleLane.Bulk || !record.ChunkIndex.HasValue || !record.StreamId.Equals(m_StreamId))
                return false;
            if (!record.SessionId.Equals(m_SessionId) || !record.SceneId.Equals(m_SceneId) || !record.IncarnationId.Equals(m_IncarnationId) || !record.MessageId.Equals(m_OperationId) || record.OriginDevice != m_OriginDevice || record.BodySchema != BodySchema || record.ChunkIndex.Value != m_NextChunk)
                throw new InvalidDataException("Checkpoint bulk chunk is out of order or has mismatched identity.");

            int expectedBytes = Math.Min(m_ChunkSize, m_TotalLength - checked(m_NextChunk * m_ChunkSize));
            if (record.PayloadLength != expectedBytes || m_NextChunk >= m_ChunkCount)
                throw new InvalidDataException("Checkpoint bulk chunk has an invalid size or index.");
            byte[] chunk = record.GetPayloadCopy();
            m_Body.Write(chunk, 0, chunk.Length);
            m_NextChunk++;
            if (m_NextChunk != m_ChunkCount) return true;
            if (m_Body.Length != m_TotalLength) throw new InvalidDataException("Checkpoint bulk body has an invalid total length.");

            body = m_Body.ToArray();
            using (SHA256 sha = SHA256.Create())
                if (!Equal(sha.ComputeHash(body), m_Digest))
                    throw new InvalidDataException("Checkpoint bulk body digest mismatch.");
            operationId = m_OperationId;
            Reset();
            return true;
        }

        public void Reset()
        {
            m_Body?.Dispose();
            m_Body = null;
            m_OperationId = null;
            m_StreamId = null;
            m_SessionId = null;
            m_SceneId = null;
            m_IncarnationId = null;
            m_Digest = null;
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
