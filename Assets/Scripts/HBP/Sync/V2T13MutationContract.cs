using System;
using System.IO;

namespace HBP.Sync
{
    /// <summary>One complete, bounded correlation result for a prepared scene generation.</summary>
    public sealed class SetCorrelationResult : V2Mutation
    {
        private readonly byte[] m_ResultBytes;

        public const int MaximumBytes = 8 * 1024 * 1024;
        public OperationId JobId { get; }
        public ulong Generation { get; }
        public byte[] ResultBytes => (byte[])m_ResultBytes.Clone();
        public override V2OperationType Type => V2OperationType.SetCorrelationResult;

        public SetCorrelationResult(OperationId jobId, ulong generation, byte[] resultBytes)
        {
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            if (generation == 0) throw new ArgumentOutOfRangeException(nameof(generation));
            if (resultBytes == null || resultBytes.Length == 0 || resultBytes.Length > MaximumBytes) throw new ArgumentOutOfRangeException(nameof(resultBytes));
            Generation = generation;
            m_ResultBytes = (byte[])resultBytes.Clone();
        }
    }

    internal static class V2T13MutationCodec
    {
        internal static V2Mutation ReadBody(BinaryReader reader)
        {
            byte[] jobBytes = reader.ReadBytes(16);
            if (jobBytes.Length != 16) throw new EndOfStreamException();
            ulong generation = reader.ReadUInt64();
            int length = reader.ReadInt32();
            if (length <= 0 || length > SetCorrelationResult.MaximumBytes || length != reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid T13 correlation result length.");
            byte[] result = reader.ReadBytes(length);
            if (result.Length != length) throw new EndOfStreamException();
            return new SetCorrelationResult(new OperationId(new Guid(jobBytes)), generation, result);
        }

        internal static void WriteBody(BinaryWriter writer, V2Mutation mutation)
        {
            if (mutation is not SetCorrelationResult result) throw new ArgumentException("Unsupported T13 mutation type.", nameof(mutation));
            byte[] bytes = result.ResultBytes;
            writer.Write(result.JobId.ToByteArray());
            writer.Write(result.Generation);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }
}
