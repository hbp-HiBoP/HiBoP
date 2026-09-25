using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HBP.Sync;

namespace HBP.Sync.Scene
{
    public enum V2PublicationJournalDisposition
    {
        Replay,
        Checkpoint
    }

    public sealed class V2PublicationJournalResult
    {
        public V2PublicationJournalDisposition Disposition { get; }
        public IReadOnlyList<V2CanonicalMutation> Mutations { get; }
        public V2SceneMutationCheckpoint Checkpoint { get; }

        internal V2PublicationJournalResult(IReadOnlyList<V2CanonicalMutation> mutations)
        {
            Disposition = V2PublicationJournalDisposition.Replay;
            Mutations = Array.AsReadOnly(mutations.ToArray());
        }

        internal V2PublicationJournalResult(V2SceneMutationCheckpoint checkpoint)
        {
            Disposition = V2PublicationJournalDisposition.Checkpoint;
            Mutations = Array.Empty<V2CanonicalMutation>();
            Checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        }
    }

    /// <summary>Bounded canonical edits accepted after the initial capture boundary.</summary>
    public sealed class V2PublicationMutationJournal
    {
        public const int DefaultMaximumMutations = 256;

        private readonly object m_Gate = new object();
        private readonly SceneId m_SceneId;
        private readonly IncarnationId m_IncarnationId;
        private readonly int m_MaximumMutations;
        private readonly List<V2CanonicalMutation> m_Mutations;
        private ulong m_LastCanonicalSequence;
        private bool m_Overflowed;
        private bool m_Completed;

        public int Count
        {
            get
            {
                lock (m_Gate) return m_Mutations.Count;
            }
        }

        public bool IsOverflowed
        {
            get
            {
                lock (m_Gate) return m_Overflowed;
            }
        }

        public bool IsCompleted
        {
            get
            {
                lock (m_Gate) return m_Completed;
            }
        }

        public V2PublicationMutationJournal(SceneId sceneId, IncarnationId incarnationId, int maximumMutations = DefaultMaximumMutations)
        {
            m_SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            m_IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            if (maximumMutations <= 0 || maximumMutations > V2DesktopMutationAuthority.MaximumRememberedOperations)
                throw new ArgumentOutOfRangeException(nameof(maximumMutations));
            m_MaximumMutations = maximumMutations;
            m_Mutations = new List<V2CanonicalMutation>(Math.Min(maximumMutations, 256));
        }

        public bool TryRecord(V2CanonicalMutation mutation)
        {
            lock (m_Gate)
            {
                if (m_Completed) throw new InvalidOperationException("The initial-publication journal is complete.");
                if (mutation == null) throw new ArgumentNullException(nameof(mutation));
                if (!m_SceneId.Equals(mutation.SceneId) || !m_IncarnationId.Equals(mutation.IncarnationId))
                    throw new InvalidDataException("A journal mutation belongs to another scene incarnation.");
                if (mutation.CanonicalSequence == 0 || mutation.CanonicalSequence <= m_LastCanonicalSequence)
                    throw new InvalidDataException("Journal mutations must have strictly increasing canonical sequences.");
                m_LastCanonicalSequence = mutation.CanonicalSequence;

                if (m_Overflowed) return false;
                if (m_Mutations.Count == m_MaximumMutations)
                {
                    m_Overflowed = true;
                    m_Mutations.Clear();
                    return false;
                }

                m_Mutations.Add(mutation);
                return true;
            }
        }

        public V2PublicationJournalResult Complete(Func<V2SceneMutationCheckpoint> captureCheckpoint)
        {
            if (captureCheckpoint == null) throw new ArgumentNullException(nameof(captureCheckpoint));
            bool overflowed;
            V2CanonicalMutation[] mutations;
            lock (m_Gate)
            {
                if (m_Completed) throw new InvalidOperationException("The initial-publication journal is already complete.");
                m_Completed = true;
                overflowed = m_Overflowed;
                mutations = m_Mutations.ToArray();
            }

            if (!overflowed) return new V2PublicationJournalResult(mutations);
            V2SceneMutationCheckpoint checkpoint = captureCheckpoint();
            if (checkpoint == null) throw new InvalidOperationException("The initial-publication checkpoint is unavailable.");
            return new V2PublicationJournalResult(checkpoint);
        }
    }

    public sealed class V2PublishedSceneCheckpoint
    {
        public ulong CanonicalSequence { get; }
        public V2SceneMutationCheckpoint Checkpoint { get; }

        internal V2PublishedSceneCheckpoint(ulong canonicalSequence, V2SceneMutationCheckpoint checkpoint)
        {
            CanonicalSequence = canonicalSequence;
            Checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        }
    }

    /// <summary>Versioned bounded composition of T04's three family-owned checkpoint records.</summary>
    public static class V2SceneMutationCheckpointCodec
    {
        private const ushort SchemaVersion = 1;
        private const int HeaderLength = 26;
        private const int MaximumRecordLength = V2MutationPayloadCodec.MaximumPayloadBytes + 6;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBCP");

        public const int MaximumCheckpointBytes = 16 * 1024 * 1024;
        public const int MaximumRecords = 65536;

        public static byte[] Encode(ulong canonicalSequence, V2SceneMutationCheckpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            ValidateCounts(checkpoint.SiteColors.Count, checkpoint.CutDefinitions.Count, checkpoint.TimelineAnchors.Count);

            SiteColorCheckpointRecord[] sites = checkpoint.SiteColors.OrderBy(record => record.Value.ColumnId.Value, StringComparer.Ordinal).ThenBy(record => record.Value.FullSiteId.Value, StringComparer.Ordinal).ToArray();
            CutDefinitionCheckpointRecord[] cuts = checkpoint.CutDefinitions.OrderBy(record => record.Value.CutId.Value, StringComparer.Ordinal).ToArray();
            TimelineAnchorCheckpointRecord[] timelines = checkpoint.TimelineAnchors.OrderBy(record => record.Value.ColumnId.Value, StringComparer.Ordinal).ToArray();
            ValidateUniqueKeys(sites, cuts, timelines);

            using var stream = new MemoryStream(Math.Min(MaximumCheckpointBytes, HeaderLength + sites.Length * 64));
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write(canonicalSequence);
            writer.Write(sites.Length);
            writer.Write(cuts.Length);
            writer.Write(timelines.Length);
            WriteRecords(writer, sites.Select(record => record.Encode()));
            WriteRecords(writer, cuts.Select(record => record.Encode()));
            WriteRecords(writer, timelines.Select(record => record.Encode()));
            writer.Flush();
            return stream.ToArray();
        }

        public static V2PublishedSceneCheckpoint Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderLength || bytes.Length > MaximumCheckpointBytes)
                throw new InvalidDataException("Invalid typed scene checkpoint length.");

            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic) || reader.ReadUInt16() != SchemaVersion)
                    throw new InvalidDataException("Unsupported typed scene checkpoint signature or schema.");
                ulong canonicalSequence = reader.ReadUInt64();
                int siteCount = reader.ReadInt32();
                int cutCount = reader.ReadInt32();
                int timelineCount = reader.ReadInt32();
                ValidateCounts(siteCount, cutCount, timelineCount);

                var sites = new List<SiteColorCheckpointRecord>(siteCount);
                var cuts = new List<CutDefinitionCheckpointRecord>(cutCount);
                var timelines = new List<TimelineAnchorCheckpointRecord>(timelineCount);
                ReadRecords(reader, siteCount, SiteColorCheckpointRecord.Decode, sites);
                ReadRecords(reader, cutCount, CutDefinitionCheckpointRecord.Decode, cuts);
                ReadRecords(reader, timelineCount, TimelineAnchorCheckpointRecord.Decode, timelines);
                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Trailing typed scene checkpoint bytes.");

                ValidateUniqueKeys(sites, cuts, timelines);
                return new V2PublishedSceneCheckpoint(canonicalSequence, new V2SceneMutationCheckpoint(sites, cuts, timelines));
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated typed scene checkpoint.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid typed scene checkpoint value.", exception);
            }
        }

        private static void WriteRecords(BinaryWriter writer, IEnumerable<byte[]> records)
        {
            foreach (byte[] record in records)
            {
                if (record == null || record.Length < 6 || record.Length > MaximumRecordLength)
                    throw new InvalidDataException("Invalid typed checkpoint record length.");
                writer.Write(checked((ushort)record.Length));
                writer.Write(record);
                if (writer.BaseStream.Length > MaximumCheckpointBytes)
                    throw new InvalidDataException("Typed scene checkpoint exceeds its 16 MiB transport bound.");
            }
        }

        private static void ReadRecords<T>(BinaryReader reader, int count, Func<byte[], T> decode, List<T> output)
        {
            for (int i = 0; i < count; i++)
            {
                ushort length = reader.ReadUInt16();
                if (length < 6 || length > MaximumRecordLength)
                    throw new InvalidDataException("Invalid typed checkpoint record length.");
                byte[] record = reader.ReadBytes(length);
                if (record.Length != length) throw new EndOfStreamException();
                output.Add(decode(record));
            }
        }

        private static void ValidateCounts(int siteCount, int cutCount, int timelineCount)
        {
            if (siteCount < 0 || cutCount < 0 || timelineCount < 0 || (long)siteCount + cutCount + timelineCount > MaximumRecords)
                throw new InvalidDataException("Typed scene checkpoint record count exceeds its bound.");
        }

        private static void ValidateUniqueKeys(IReadOnlyList<SiteColorCheckpointRecord> sites, IReadOnlyList<CutDefinitionCheckpointRecord> cuts, IReadOnlyList<TimelineAnchorCheckpointRecord> timelines)
        {
            var siteKeys = new HashSet<(string Column, string Site)>();
            foreach (SiteColorCheckpointRecord record in sites)
                if (record?.Value == null || !siteKeys.Add((record.Value.ColumnId.Value, record.Value.FullSiteId.Value)))
                    throw new InvalidDataException("Typed scene checkpoint contains a duplicate or null site-color key.");

            var cutKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (CutDefinitionCheckpointRecord record in cuts)
                if (record?.Value == null || !cutKeys.Add(record.Value.CutId.Value))
                    throw new InvalidDataException("Typed scene checkpoint contains a duplicate or null cut key.");

            var timelineKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (TimelineAnchorCheckpointRecord record in timelines)
                if (record?.Value == null || !timelineKeys.Add(record.Value.ColumnId.Value))
                    throw new InvalidDataException("Typed scene checkpoint contains a duplicate or null timeline key.");
        }
    }

    public static class V2PublicationControlCodec
    {
        private static readonly byte[] LiveMagic = Encoding.ASCII.GetBytes("HBLV");
        private static readonly byte[] AcknowledgementMagic = Encoding.ASCII.GetBytes("HBAK");
        private const int LiveLength = 12;
        private const int AcknowledgementLength = 20;

        public static byte[] EncodeLiveBarrier(ulong canonicalSequence)
        {
            var bytes = new byte[LiveLength];
            Buffer.BlockCopy(LiveMagic, 0, bytes, 0, LiveMagic.Length);
            WriteUInt64(bytes, 4, canonicalSequence);
            return bytes;
        }

        public static bool TryDecodeLiveBarrier(byte[] bytes, out ulong canonicalSequence)
        {
            canonicalSequence = 0;
            if (!HasMagic(bytes, LiveMagic) || bytes.Length != LiveLength) return false;
            canonicalSequence = ReadUInt64(bytes, 4);
            return true;
        }

        public static byte[] EncodeAcknowledgement(OperationId barrierId)
        {
            if (barrierId == null) throw new ArgumentNullException(nameof(barrierId));
            var bytes = new byte[AcknowledgementLength];
            Buffer.BlockCopy(AcknowledgementMagic, 0, bytes, 0, AcknowledgementMagic.Length);
            Buffer.BlockCopy(barrierId.ToByteArray(), 0, bytes, 4, 16);
            return bytes;
        }

        public static bool TryDecodeAcknowledgement(byte[] bytes, out OperationId barrierId)
        {
            barrierId = null;
            if (!HasMagic(bytes, AcknowledgementMagic) || bytes.Length != AcknowledgementLength) return false;
            var identity = new byte[16];
            Buffer.BlockCopy(bytes, 4, identity, 0, identity.Length);
            try
            {
                barrierId = new OperationId(new Guid(identity));
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool HasMagic(byte[] bytes, byte[] magic)
        {
            if (bytes == null || bytes.Length < magic.Length) return false;
            for (int i = 0; i < magic.Length; i++)
                if (bytes[i] != magic[i])
                    return false;
            return true;
        }

        private static void WriteUInt64(byte[] bytes, int offset, ulong value)
        {
            for (int i = 0; i < 8; i++) bytes[offset + i] = (byte)(value >> (8 * i));
        }

        private static ulong ReadUInt64(byte[] bytes, int offset)
        {
            ulong value = 0;
            for (int i = 0; i < 8; i++) value |= (ulong)bytes[offset + i] << (8 * i);
            return value;
        }
    }
}
