using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HBP.Sync
{
    /// <summary>One canonical, immutable inclusion mask for a prepared scene roster.</summary>
    public sealed class SetSiteFilterResult : V2Mutation
    {
        private readonly byte[] m_RosterHash;
        private readonly byte[] m_InclusionBits;

        public const int MaximumSiteCount = 65536;
        public OperationId JobId { get; }
        public ulong Generation { get; }
        public int SiteCount { get; }
        public byte[] RosterHash => (byte[])m_RosterHash.Clone();
        public byte[] InclusionBits => (byte[])m_InclusionBits.Clone();
        public override V2OperationType Type => V2OperationType.SetSiteFilterResult;

        public SetSiteFilterResult(OperationId jobId, ulong generation, byte[] rosterHash, int siteCount, byte[] inclusionBits)
        {
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            if (generation == 0) throw new ArgumentOutOfRangeException(nameof(generation));
            if (rosterHash == null || rosterHash.Length != 32) throw new ArgumentException("A site-filter result requires a SHA-256 roster identity.", nameof(rosterHash));
            if (siteCount <= 0 || siteCount > MaximumSiteCount) throw new ArgumentOutOfRangeException(nameof(siteCount));
            int expectedLength = checked((siteCount + 7) / 8);
            if (inclusionBits == null || inclusionBits.Length != expectedLength) throw new ArgumentException("The inclusion bitset length does not match the site roster.", nameof(inclusionBits));
            ValidateUnusedBits(siteCount, inclusionBits);
            Generation = generation;
            SiteCount = siteCount;
            m_RosterHash = (byte[])rosterHash.Clone();
            m_InclusionBits = (byte[])inclusionBits.Clone();
        }

        public bool IsIncluded(int index)
        {
            if ((uint)index >= (uint)SiteCount) throw new ArgumentOutOfRangeException(nameof(index));
            return (m_InclusionBits[index >> 3] & (1 << (index & 7))) != 0;
        }

        private static void ValidateUnusedBits(int siteCount, byte[] bits)
        {
            int usedBits = siteCount & 7;
            if (usedBits == 0) return;
            int allowedMask = (1 << usedBits) - 1;
            if ((bits[bits.Length - 1] & ~allowedMask) != 0)
                throw new ArgumentException("Unused inclusion bits must be zero.", nameof(bits));
        }
    }

    /// <summary>Typed checkpoint form of the current IsFiltered state, independent of job history.</summary>
    public sealed class V2SiteFilterCheckpointRecord
    {
        private const ushort RecordMagic = 0x5432;
        private const ushort RecordSchema = 1;
        private readonly byte[] m_RosterHash;
        private readonly byte[] m_InclusionBits;

        public int SiteCount { get; }
        public byte[] RosterHash => (byte[])m_RosterHash.Clone();
        public byte[] InclusionBits => (byte[])m_InclusionBits.Clone();

        public V2SiteFilterCheckpointRecord(byte[] rosterHash, int siteCount, byte[] inclusionBits)
        {
            if (rosterHash == null || rosterHash.Length != 32) throw new ArgumentException("A site-filter checkpoint requires a SHA-256 roster identity.", nameof(rosterHash));
            if (siteCount <= 0 || siteCount > SetSiteFilterResult.MaximumSiteCount) throw new ArgumentOutOfRangeException(nameof(siteCount));
            if (inclusionBits == null || inclusionBits.Length != checked((siteCount + 7) / 8)) throw new ArgumentException("The inclusion bitset length does not match the site roster.", nameof(inclusionBits));
            int usedBits = siteCount & 7;
            if (usedBits != 0 && (inclusionBits[inclusionBits.Length - 1] & ~((1 << usedBits) - 1)) != 0)
                throw new ArgumentException("Unused inclusion bits must be zero.", nameof(inclusionBits));
            SiteCount = siteCount;
            m_RosterHash = (byte[])rosterHash.Clone();
            m_InclusionBits = (byte[])inclusionBits.Clone();
        }

        public bool IsIncluded(int index)
        {
            if ((uint)index >= (uint)SiteCount) throw new ArgumentOutOfRangeException(nameof(index));
            return (m_InclusionBits[index >> 3] & (1 << (index & 7))) != 0;
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream(46 + m_InclusionBits.Length);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(RecordMagic);
            writer.Write(RecordSchema);
            writer.Write(SiteCount);
            writer.Write(m_RosterHash);
            writer.Write(m_InclusionBits.Length);
            writer.Write(m_InclusionBits);
            writer.Flush();
            return stream.ToArray();
        }

        public static V2SiteFilterCheckpointRecord Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 45 || bytes.Length > V2MutationPayloadCodec.MaximumPayloadBytes + 8)
                throw new InvalidDataException("Invalid T12 checkpoint record length.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            try
            {
                if (reader.ReadUInt16() != RecordMagic || reader.ReadUInt16() != RecordSchema)
                    throw new InvalidDataException("Unsupported T12 checkpoint record signature or schema.");
                int siteCount = reader.ReadInt32();
                byte[] rosterHash = reader.ReadBytes(32);
                if (rosterHash.Length != 32) throw new EndOfStreamException();
                int bitLength = reader.ReadInt32();
                if (bitLength != checked((siteCount + 7) / 8) || bitLength != stream.Length - stream.Position)
                    throw new InvalidDataException("Invalid T12 checkpoint bitset length.");
                byte[] bits = reader.ReadBytes(bitLength);
                if (bits.Length != bitLength || stream.Position != stream.Length) throw new EndOfStreamException();
                return new V2SiteFilterCheckpointRecord(rosterHash, siteCount, bits);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated T12 checkpoint record.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid T12 checkpoint value.", exception);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("Invalid T12 checkpoint length.", exception);
            }
        }
    }

    internal static class V2T12MutationCodec
    {
        internal static V2Mutation ReadBody(BinaryReader reader)
        {
            byte[] jobBytes = reader.ReadBytes(16);
            byte[] rosterHash = reader.ReadBytes(32);
            if (jobBytes.Length != 16 || rosterHash.Length != 32) throw new EndOfStreamException();
            ulong generation = reader.ReadUInt64();
            int siteCount = reader.ReadInt32();
            int bitLength = reader.ReadInt32();
            if (siteCount <= 0 || siteCount > SetSiteFilterResult.MaximumSiteCount || bitLength != checked((siteCount + 7) / 8))
                throw new InvalidDataException("Invalid T12 site-filter result bounds.");
            byte[] bits = reader.ReadBytes(bitLength);
            if (bits.Length != bitLength) throw new EndOfStreamException();
            return new SetSiteFilterResult(new OperationId(new Guid(jobBytes)), generation, rosterHash, siteCount, bits);
        }

        internal static void WriteBody(BinaryWriter writer, V2Mutation mutation)
        {
            if (mutation is not SetSiteFilterResult result) throw new ArgumentException("Unsupported T12 mutation type.", nameof(mutation));
            writer.Write(result.JobId.ToByteArray());
            writer.Write(result.RosterHash);
            writer.Write(result.Generation);
            writer.Write(result.SiteCount);
            byte[] bits = result.InclusionBits;
            writer.Write(bits.Length);
            writer.Write(bits);
        }
    }

    public static class V2SiteFilterMaskCodec
    {
        public static byte[] EncodeBits(bool[] included)
        {
            if (included == null) throw new ArgumentNullException(nameof(included));
            if (included.Length <= 0 || included.Length > SetSiteFilterResult.MaximumSiteCount) throw new ArgumentOutOfRangeException(nameof(included));
            var bits = new byte[(included.Length + 7) / 8];
            for (int i = 0; i < included.Length; i++)
                if (included[i])
                    bits[i >> 3] |= (byte)(1 << (i & 7));
            return bits;
        }
    }
}
