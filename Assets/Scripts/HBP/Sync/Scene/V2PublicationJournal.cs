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

        public V2PublicationJournalResult Complete(Func<V2SceneMutationCheckpoint> captureCheckpoint, bool forceCheckpoint = false)
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

            if (!overflowed && !forceCheckpoint) return new V2PublicationJournalResult(mutations);
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

    /// <summary>Versioned bounded composition of typed scene-mutation checkpoint records.</summary>
    public static class V2SceneMutationCheckpointCodec
    {
        private const ushort SchemaVersion = 7;
        private const int MinimumHeaderLength = 26;
        private const int HeaderLength = 46;
        private const int MaximumRecordLength = V2MutationPayloadCodec.MaximumPayloadBytes + 8;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBCP");
        private static readonly SceneId ValidationSceneId = new SceneId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        private static readonly IncarnationId ValidationIncarnationId = new IncarnationId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        public const int MaximumCheckpointBytes = 16 * 1024 * 1024;
        public const int MaximumRecords = 65536;

        public static byte[] Encode(ulong canonicalSequence, V2SceneMutationCheckpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            ValidateCounts(checkpoint.SiteColors.Count, checkpoint.CutDefinitions.Count, checkpoint.TimelineAnchors.Count, checkpoint.T09Records.Count, checkpoint.T10Records.Count, checkpoint.T11Records.Count, checkpoint.T12Records.Count, checkpoint.T13Records.Count);

            SiteColorCheckpointRecord[] sites = checkpoint.SiteColors.OrderBy(record => record.Value.ColumnId.Value, StringComparer.Ordinal).ThenBy(record => record.Value.FullSiteId.Value, StringComparer.Ordinal).ToArray();
            CutDefinitionCheckpointRecord[] cuts = checkpoint.CutDefinitions.OrderBy(record => record.Value.CutId.Value, StringComparer.Ordinal).ToArray();
            TimelineAnchorCheckpointRecord[] timelines = checkpoint.TimelineAnchors.OrderBy(record => record.Value.ColumnId.Value, StringComparer.Ordinal).ToArray();
            V2T09CheckpointRecord[] t09Records = checkpoint.T09Records.OrderBy(record => Convert.ToBase64String(V2MutationPayloadCodec.Encode(record.Value)), StringComparer.Ordinal).ToArray();
            V2T10CheckpointRecord[] t10Records = checkpoint.T10Records.OrderBy(record => Convert.ToBase64String(V2MutationPayloadCodec.Encode(record.Value)), StringComparer.Ordinal).ToArray();
            V2T11CheckpointRecord[] t11Records = checkpoint.T11Records.OrderBy(record => Convert.ToBase64String(V2MutationPayloadCodec.Encode(record.Value)), StringComparer.Ordinal).ToArray();
            V2SiteFilterCheckpointRecord[] t12Records = checkpoint.T12Records.ToArray();
            V2CorrelationCheckpointRecord[] t13Records = checkpoint.T13Records.ToArray();
            ValidateUniqueKeys(sites, cuts, timelines, t09Records, t10Records, t11Records, t12Records, t13Records);

            using var stream = new MemoryStream(Math.Min(MaximumCheckpointBytes, HeaderLength + sites.Length * 64));
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write(canonicalSequence);
            writer.Write(sites.Length);
            writer.Write(cuts.Length);
            writer.Write(timelines.Length);
            writer.Write(t09Records.Length);
            writer.Write(t10Records.Length);
            writer.Write(t11Records.Length);
            writer.Write(t12Records.Length);
            writer.Write(t13Records.Length);
            WriteRecords(writer, sites.Select(record => record.Encode()), wideLength: true);
            WriteRecords(writer, cuts.Select(record => record.Encode()), wideLength: true);
            WriteRecords(writer, timelines.Select(record => record.Encode()), wideLength: true);
            WriteRecords(writer, t09Records.Select(record => record.Encode()), wideLength: true);
            WriteRecords(writer, t10Records.Select(record => record.Encode()), wideLength: true);
            WriteRecords(writer, t11Records.Select(record => record.Encode()), wideLength: true);
            WriteRecords(writer, t12Records.Select(record => record.Encode()), wideLength: true);
            WriteRecords(writer, t13Records.Select(record => record.Encode()), wideLength: true);
            writer.Flush();
            return stream.ToArray();
        }

        public static V2PublishedSceneCheckpoint Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < MinimumHeaderLength || bytes.Length > MaximumCheckpointBytes)
                throw new InvalidDataException("Invalid typed scene checkpoint length.");

            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic))
                    throw new InvalidDataException("Unsupported typed scene checkpoint signature or schema.");
                ushort schemaVersion = reader.ReadUInt16();
                if (schemaVersion < 1 || schemaVersion > SchemaVersion)
                    throw new InvalidDataException("Unsupported typed scene checkpoint signature or schema.");
                ulong canonicalSequence = reader.ReadUInt64();
                int siteCount = reader.ReadInt32();
                int cutCount = reader.ReadInt32();
                int timelineCount = reader.ReadInt32();
                int t09Count = schemaVersion >= 2 ? reader.ReadInt32() : 0;
                int t10Count = schemaVersion >= 3 ? reader.ReadInt32() : 0;
                int t11Count = schemaVersion >= 4 ? reader.ReadInt32() : 0;
                int t12Count = schemaVersion >= 5 ? reader.ReadInt32() : 0;
                int t13Count = schemaVersion >= 6 ? reader.ReadInt32() : 0;
                ValidateCounts(siteCount, cutCount, timelineCount, t09Count, t10Count, t11Count, t12Count, t13Count);
                int expectedHeaderLength = schemaVersion >= 6 ? HeaderLength : schemaVersion >= 5 ? 42 : schemaVersion >= 4 ? 38 : schemaVersion >= 3 ? 34 : MinimumHeaderLength;
                if (bytes.Length < expectedHeaderLength) throw new InvalidDataException("Truncated typed scene checkpoint header.");

                var sites = new List<SiteColorCheckpointRecord>(siteCount);
                var cuts = new List<CutDefinitionCheckpointRecord>(cutCount);
                var timelines = new List<TimelineAnchorCheckpointRecord>(timelineCount);
                var t09Records = new List<V2T09CheckpointRecord>(t09Count);
                var t10Records = new List<V2T10CheckpointRecord>(t10Count);
                var t11Records = new List<V2T11CheckpointRecord>(t11Count);
                var t12Records = new List<V2SiteFilterCheckpointRecord>(t12Count);
                var t13Records = new List<V2CorrelationCheckpointRecord>(t13Count);
                ReadRecords(reader, siteCount, SiteColorCheckpointRecord.Decode, sites, wideLength: schemaVersion >= 3);
                ReadRecords(reader, cutCount, CutDefinitionCheckpointRecord.Decode, cuts, wideLength: schemaVersion >= 3);
                ReadRecords(reader, timelineCount, TimelineAnchorCheckpointRecord.Decode, timelines, wideLength: schemaVersion >= 3);
                ReadRecords(reader, t09Count, V2T09CheckpointRecord.Decode, t09Records, wideLength: schemaVersion >= 3);
                ReadRecords(reader, t10Count, V2T10CheckpointRecord.Decode, t10Records, wideLength: true);
                ReadRecords(reader, t11Count, V2T11CheckpointRecord.Decode, t11Records, wideLength: true);
                ReadRecords(reader, t12Count, V2SiteFilterCheckpointRecord.Decode, t12Records, wideLength: true);
                ReadRecords(reader, t13Count, V2CorrelationCheckpointRecord.Decode, t13Records, wideLength: true);
                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Trailing typed scene checkpoint bytes.");

                ValidateUniqueKeys(sites, cuts, timelines, t09Records, t10Records, t11Records, t12Records, t13Records);
                return new V2PublishedSceneCheckpoint(canonicalSequence, new V2SceneMutationCheckpoint(sites, cuts, timelines, t09Records, t10Records, t11Records, t12Records, t13Records));
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

        private static void WriteRecords(BinaryWriter writer, IEnumerable<byte[]> records, bool wideLength)
        {
            foreach (byte[] record in records)
            {
                if (record == null || record.Length < 6 || record.Length > MaximumRecordLength)
                    throw new InvalidDataException("Invalid typed checkpoint record length.");
                if (wideLength) writer.Write(checked((uint)record.Length));
                else writer.Write(checked((ushort)record.Length));
                writer.Write(record);
                if (writer.BaseStream.Length > MaximumCheckpointBytes)
                    throw new InvalidDataException("Typed scene checkpoint exceeds its 16 MiB transport bound.");
            }
        }

        private static void ReadRecords<T>(BinaryReader reader, int count, Func<byte[], T> decode, List<T> output, bool wideLength)
        {
            for (int i = 0; i < count; i++)
            {
                uint length = wideLength ? reader.ReadUInt32() : reader.ReadUInt16();
                if (length < 6 || length > (uint)MaximumRecordLength)
                    throw new InvalidDataException("Invalid typed checkpoint record length.");
                byte[] record = reader.ReadBytes(checked((int)length));
                if (record.Length != length) throw new EndOfStreamException();
                output.Add(decode(record));
            }
        }

        private static void ValidateCounts(int siteCount, int cutCount, int timelineCount, int t09Count, int t10Count, int t11Count, int t12Count, int t13Count)
        {
            if (siteCount < 0 || cutCount < 0 || timelineCount < 0 || t09Count < 0 || t10Count < 0 || t11Count < 0 || t12Count < 0 || t13Count < 0 || (long)siteCount + cutCount + timelineCount + t09Count + t10Count + t11Count + t12Count + t13Count > MaximumRecords)
                throw new InvalidDataException("Typed scene checkpoint record count exceeds its bound.");
        }

        private static void ValidateUniqueKeys(IReadOnlyList<SiteColorCheckpointRecord> sites, IReadOnlyList<CutDefinitionCheckpointRecord> cuts, IReadOnlyList<TimelineAnchorCheckpointRecord> timelines, IReadOnlyList<V2T09CheckpointRecord> t09Records, IReadOnlyList<V2T10CheckpointRecord> t10Records, IReadOnlyList<V2T11CheckpointRecord> t11Records, IReadOnlyList<V2SiteFilterCheckpointRecord> t12Records, IReadOnlyList<V2CorrelationCheckpointRecord> t13Records)
        {
            if (t12Records.Count > 1 || t12Records.Any(record => record == null))
                throw new InvalidDataException("Typed scene checkpoint contains more than one or a null T12 site-filter result.");
            if (t13Records.Count > 1 || t13Records.Any(record => record == null))
                throw new InvalidDataException("Typed scene checkpoint contains more than one or a null T13 correlation result.");
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

            var t09Keys = new HashSet<V2TouchedKey>();
            foreach (V2T09CheckpointRecord record in t09Records)
                if (record?.Value == null || !t09Keys.Add(V2MutationDescriptor.Create(ValidationSceneId, ValidationIncarnationId, record.Value).CoalescingKey))
                    throw new InvalidDataException("Typed scene checkpoint contains a duplicate or null T09 key.");

            var t10Keys = new HashSet<V2TouchedKey>();
            foreach (V2T10CheckpointRecord record in t10Records)
            {
                if (record?.Value == null || !IsCheckpointT10Type(record.Value.Type))
                    throw new InvalidDataException("Typed scene checkpoint contains an unsupported T10 record.");
                foreach (V2TouchedKey key in V2MutationDescriptor.Create(ValidationSceneId, ValidationIncarnationId, record.Value).TouchedKeys)
                    if (!t10Keys.Add(key))
                        throw new InvalidDataException("Typed scene checkpoint contains a duplicate T10 key.");
            }

            var t11Keys = new HashSet<V2TouchedKey>();
            foreach (V2T11CheckpointRecord record in t11Records)
            {
                if (record?.Value == null || record.Value is SetConfigurationTransaction)
                    throw new InvalidDataException("Typed scene checkpoint contains an unsupported T11 record.");
                foreach (V2TouchedKey key in V2MutationDescriptor.Create(ValidationSceneId, ValidationIncarnationId, record.Value).TouchedKeys)
                    if (!t11Keys.Add(key))
                        throw new InvalidDataException("Typed scene checkpoint contains a duplicate T11 key.");
            }
        }

        private static bool IsCheckpointT10Type(V2OperationType type) => type is V2OperationType.CreateCut or V2OperationType.SetCutOrder or V2OperationType.CreateRoi or V2OperationType.SetActiveRoi or V2OperationType.SetMeshDisplay or V2OperationType.SetSelectedMri or V2OperationType.SetMriCalibration or V2OperationType.SetImplantation or V2OperationType.ApplyTriangleMask or V2OperationType.MoveSites;
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
