using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    /// <summary>Presentation bulk is multiplexed independently of structural mutation reassembly.</summary>
    public sealed class SignalBulkReceiver : IDisposable
    {
        private sealed class Transfer
        {
            public TransportRecord Descriptor;
            public byte[] Bytes, Digest;
            public int ChunkSize, Next;
        }

        private readonly Dictionary<Guid, Transfer> transfers = new();
        private long bytes;

        public bool TryReceive(TransportRecord record, out byte[] body, out TransportRecord completed)
        {
            body = null;
            completed = null;
            if (record.BodySchema != SignalWire.BodySchema || record.Kind != TransportMessageKind.Application) return false;
            if (record.OriginDevice != OriginDevice.Desktop) throw new InvalidDataException("Only Desktop can publish Signals.");
            if (record.CanonicalSequence.HasValue || record.ObservedCanonicalSequence.HasValue) throw new InvalidDataException("Presentation cannot carry a mutation sequence.");
            if (!record.ChunkIndex.HasValue)
            {
                var payload = record.GetPayloadCopy();
                if (SignalWire.IsControl(payload))
                {
                    body = payload;
                    completed = record;
                    return true;
                }

                using var stream = new MemoryStream(payload, false);
                using var reader = new BinaryReader(stream);
                if (EncodingMagic(reader) != "HBSO" || reader.ReadUInt16() != 1) throw new InvalidDataException("Invalid presentation descriptor.");
                var operation = new Guid(Read(reader, 16));
                var schema = reader.ReadUInt16();
                uint length = reader.ReadUInt32(), chunkSize = reader.ReadUInt32(), count = reader.ReadUInt32();
                var id = new Guid(Read(reader, 16));
                int digestLength = reader.ReadByte();
                byte[] digest = Read(reader, digestLength);
                var barrier = reader.ReadByte();
                var touched = reader.ReadUInt16();
                if (schema != SignalWire.BodySchema || !record.MessageId.Value.Equals(operation) || length == 0 || length > SignalSnapshotCodec.MaximumMetadataBytes + 4096 || chunkSize == 0 || chunkSize > TransportFrameCodec.MaximumPayloadBytes || count != (length + chunkSize - 1) / chunkSize || digestLength != 32 || barrier != 0 || touched != 0 || stream.Position != stream.Length || transfers.Count >= 8 || bytes + length > 16 * 1024 * 1024 || !BulkStreamIdentityCodec.TryGetOrdinal(record.SessionId, record.OriginDevice, new ReliableStreamId(id), out _)) throw new InvalidDataException("Presentation descriptor exceeds capacity or scope.");
                transfers.Add(id, new Transfer { Descriptor = record, Bytes = new byte[(int)length], Digest = digest, ChunkSize = (int)chunkSize });
                bytes += length;
                return true;
            }

            if (!transfers.TryGetValue(record.StreamId.Value, out var transfer)) return true; // Cancelled or superseded transfer.
            if (!record.SessionId.Equals(transfer.Descriptor.SessionId) || !record.SceneId.Equals(transfer.Descriptor.SceneId) || !record.IncarnationId.Equals(transfer.Descriptor.IncarnationId) || !record.MessageId.Equals(transfer.Descriptor.MessageId) || record.OriginDevice != transfer.Descriptor.OriginDevice || record.ChunkIndex.Value != transfer.Next) throw new InvalidDataException("Mismatched presentation chunk.");
            int offset = checked(transfer.Next * transfer.ChunkSize), expected = Math.Min(transfer.ChunkSize, transfer.Bytes.Length - offset);
            if (record.PayloadLength != expected || expected <= 0) throw new InvalidDataException("Invalid presentation chunk length.");
            Buffer.BlockCopy(record.GetPayloadCopy(), 0, transfer.Bytes, offset, expected);
            transfer.Next++;
            if (offset + expected != transfer.Bytes.Length) return true;
            using (var sha = HBP.Core.Tools.Sha256.Create())
                if (!sha.ComputeHash(transfer.Bytes).SequenceEqual(transfer.Digest))
                    throw new InvalidDataException("Presentation checksum mismatch.");
            body = transfer.Bytes;
            completed = transfer.Descriptor;
            transfers.Remove(record.StreamId.Value);
            bytes -= body.Length;
            return true;
        }

        public TransportRecord[] Clear()
        {
            var records = transfers.Values.Select(t => t.Descriptor).ToArray();
            transfers.Clear();
            bytes = 0;
            return records;
        }

        public void Dispose() => Clear();

        private static byte[] Read(BinaryReader r, int count)
        {
            byte[] result = r.ReadBytes(count);
            if (result.Length != count) throw new EndOfStreamException();
            return result;
        }

        private static string EncodingMagic(BinaryReader r) => System.Text.Encoding.ASCII.GetString(Read(r, 4));
    }
}
