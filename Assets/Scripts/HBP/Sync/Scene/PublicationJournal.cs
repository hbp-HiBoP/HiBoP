using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HBP.Sync;

namespace HBP.Sync.Scene
{
    public enum PublicationJournalDisposition
    {
        Replay,
        Checkpoint
    }

    public sealed class PublicationJournalResult
    {
        public PublicationJournalDisposition Disposition { get; }
        public IReadOnlyList<CanonicalMutation> Mutations { get; }
        public SceneMutationCheckpoint Checkpoint { get; }

        internal PublicationJournalResult(IReadOnlyList<CanonicalMutation> mutations)
        {
            Disposition = PublicationJournalDisposition.Replay;
            Mutations = Array.AsReadOnly(mutations.ToArray());
        }

        internal PublicationJournalResult(SceneMutationCheckpoint checkpoint)
        {
            Disposition = PublicationJournalDisposition.Checkpoint;
            Mutations = Array.Empty<CanonicalMutation>();
            Checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        }
    }

    /// <summary>Bounded canonical edits accepted after the initial capture boundary.</summary>
    public sealed class PublicationMutationJournal
    {
        public const int DefaultMaximumMutations = 256;

        private readonly object m_Gate = new object();
        private readonly SceneId m_SceneId;
        private readonly IncarnationId m_IncarnationId;
        private readonly int m_MaximumMutations;
        private readonly List<CanonicalMutation> m_Mutations;
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

        public PublicationMutationJournal(SceneId sceneId, IncarnationId incarnationId, int maximumMutations = DefaultMaximumMutations)
        {
            m_SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            m_IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            if (maximumMutations <= 0 || maximumMutations > DesktopMutationAuthority.MaximumRememberedOperations)
                throw new ArgumentOutOfRangeException(nameof(maximumMutations));
            m_MaximumMutations = maximumMutations;
            m_Mutations = new List<CanonicalMutation>(Math.Min(maximumMutations, 256));
        }

        public bool TryRecord(CanonicalMutation mutation)
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

        public PublicationJournalResult Complete(Func<SceneMutationCheckpoint> captureCheckpoint, bool forceCheckpoint = false)
        {
            if (captureCheckpoint == null) throw new ArgumentNullException(nameof(captureCheckpoint));
            bool overflowed;
            CanonicalMutation[] mutations;
            lock (m_Gate)
            {
                if (m_Completed) throw new InvalidOperationException("The initial-publication journal is already complete.");
                m_Completed = true;
                overflowed = m_Overflowed;
                mutations = m_Mutations.ToArray();
            }

            if (!overflowed && !forceCheckpoint) return new PublicationJournalResult(mutations);
            SceneMutationCheckpoint checkpoint = captureCheckpoint();
            if (checkpoint == null) throw new InvalidOperationException("The initial-publication checkpoint is unavailable.");
            return new PublicationJournalResult(checkpoint);
        }
    }

    public sealed class PublishedSceneCheckpoint
    {
        public ulong CanonicalSequence { get; }
        public SceneMutationCheckpoint Checkpoint { get; }

        internal PublishedSceneCheckpoint(ulong canonicalSequence, SceneMutationCheckpoint checkpoint)
        {
            CanonicalSequence = canonicalSequence;
            Checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        }
    }

    /// <summary>Versioned bounded composition of typed scene-mutation checkpoint records.</summary>
    public static class SceneMutationCheckpointCodec
    {
        private const ushort SchemaVersion = 8;
        private const int HeaderLength = 46;
        private const int MaximumRecordLength = MutationPayloadCodec.MaximumPayloadBytes + 8;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBCP");
        private static readonly SceneId ValidationSceneId = new SceneId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        private static readonly IncarnationId ValidationIncarnationId = new IncarnationId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        public const int MaximumCheckpointBytes = 16 * 1024 * 1024;
        public const int MaximumRecords = 65536;

        internal static IEnumerable<SceneConfigurationCheckpointRecord> CreateSiteConfigurationRecords(IEnumerable<SiteConfigurationAssignment> assignments)
        {
            var batch = new List<SiteConfigurationAssignment>();
            foreach (SiteConfigurationAssignment assignment in assignments)
            {
                batch.Add(assignment);
                if (batch.Count == SetSiteConfigurationBatch.MaximumAssignments)
                {
                    yield return new SceneConfigurationCheckpointRecord(new SetSiteConfigurationBatch(batch));
                    batch.Clear();
                }
            }

            if (batch.Count > 0) yield return new SceneConfigurationCheckpointRecord(new SetSiteConfigurationBatch(batch));
        }

        internal static bool TryAddConfigurationKeys(Mutation mutation, SceneId scene, IncarnationId incarnation, HashSet<TouchedKey> touched, HashSet<(string ColumnId, string SiteId)> sites)
        {
            // The checkpoint is atomic across records. Site assignments must not overlap.
            if (mutation is SetSiteConfigurationBatch batch)
                return batch.Assignments.All(assignment => sites.Add((assignment.ColumnId.Value, assignment.SiteId.Value)));
            if (mutation is SetSiteBlacklist blacklist)
                return sites.Add((blacklist.ColumnId.Value, blacklist.SiteId.Value));
            return MutationDescriptor.Create(scene, incarnation, mutation).TouchedKeys.All(touched.Add);
        }

        public static byte[] Encode(ulong canonicalSequence, SceneMutationCheckpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            ValidateCounts(checkpoint.SiteColors.Count, checkpoint.CutDefinitions.Count, checkpoint.TimelineAnchors.Count, checkpoint.PresentationRecords.Count, checkpoint.StructureRecords.Count, checkpoint.ConfigurationRecords.Count, checkpoint.SiteFilterRecords.Count, checkpoint.CorrelationRecords.Count);

            SiteColorCheckpointRecord[] sites = checkpoint.SiteColors.OrderBy(record => record.Value.ColumnId.Value, StringComparer.Ordinal).ThenBy(record => record.Value.FullSiteId.Value, StringComparer.Ordinal).ToArray();
            CutDefinitionCheckpointRecord[] cuts = checkpoint.CutDefinitions.OrderBy(record => record.Value.CutId.Value, StringComparer.Ordinal).ToArray();
            TimelineAnchorCheckpointRecord[] timelines = checkpoint.TimelineAnchors.OrderBy(record => record.Value.ColumnId.Value, StringComparer.Ordinal).ToArray();
            ScenePresentationCheckpointRecord[] presentationRecords = checkpoint.PresentationRecords.OrderBy(record => Convert.ToBase64String(MutationPayloadCodec.Encode(record.Value)), StringComparer.Ordinal).ToArray();
            SceneStructureCheckpointRecord[] structureRecords = checkpoint.StructureRecords.OrderBy(record => Convert.ToBase64String(MutationPayloadCodec.Encode(record.Value)), StringComparer.Ordinal).ToArray();
            SceneConfigurationCheckpointRecord[] configurationRecords = checkpoint.ConfigurationRecords.OrderBy(record => Convert.ToBase64String(MutationPayloadCodec.Encode(record.Value)), StringComparer.Ordinal).ToArray();
            SiteFilterCheckpointRecord[] siteFilterRecords = checkpoint.SiteFilterRecords.ToArray();
            CorrelationCheckpointRecord[] correlationRecords = checkpoint.CorrelationRecords.ToArray();
            ValidateUniqueKeys(sites, cuts, timelines, presentationRecords, structureRecords, configurationRecords, siteFilterRecords, correlationRecords);

            using var stream = new MemoryStream(Math.Min(MaximumCheckpointBytes, HeaderLength + sites.Length * 64));
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write(canonicalSequence);
            writer.Write(sites.Length);
            writer.Write(cuts.Length);
            writer.Write(timelines.Length);
            writer.Write(presentationRecords.Length);
            writer.Write(structureRecords.Length);
            writer.Write(configurationRecords.Length);
            writer.Write(siteFilterRecords.Length);
            writer.Write(correlationRecords.Length);
            WriteRecords(writer, sites.Select(record => record.Encode()));
            WriteRecords(writer, cuts.Select(record => record.Encode()));
            WriteRecords(writer, timelines.Select(record => record.Encode()));
            WriteRecords(writer, presentationRecords.Select(record => record.Encode()));
            WriteRecords(writer, structureRecords.Select(record => record.Encode()));
            WriteRecords(writer, configurationRecords.Select(record => record.Encode()));
            WriteRecords(writer, siteFilterRecords.Select(record => record.Encode()));
            WriteRecords(writer, correlationRecords.Select(record => record.Encode()));
            writer.Flush();
            return stream.ToArray();
        }

        public static PublishedSceneCheckpoint Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderLength || bytes.Length > MaximumCheckpointBytes)
                throw new InvalidDataException("Invalid typed scene checkpoint length.");

            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic))
                    throw new InvalidDataException("Unsupported typed scene checkpoint signature or schema.");
                ushort schemaVersion = reader.ReadUInt16();
                if (schemaVersion != SchemaVersion)
                    throw new InvalidDataException("Unsupported typed scene checkpoint signature or schema.");
                ulong canonicalSequence = reader.ReadUInt64();
                int siteCount = reader.ReadInt32();
                int cutCount = reader.ReadInt32();
                int timelineCount = reader.ReadInt32();
                int presentationCount = reader.ReadInt32();
                int structureCount = reader.ReadInt32();
                int configurationCount = reader.ReadInt32();
                int siteFilterCount = reader.ReadInt32();
                int correlationCount = reader.ReadInt32();
                ValidateCounts(siteCount, cutCount, timelineCount, presentationCount, structureCount, configurationCount, siteFilterCount, correlationCount);

                var sites = new List<SiteColorCheckpointRecord>(siteCount);
                var cuts = new List<CutDefinitionCheckpointRecord>(cutCount);
                var timelines = new List<TimelineAnchorCheckpointRecord>(timelineCount);
                var presentationRecords = new List<ScenePresentationCheckpointRecord>(presentationCount);
                var structureRecords = new List<SceneStructureCheckpointRecord>(structureCount);
                var configurationRecords = new List<SceneConfigurationCheckpointRecord>(configurationCount);
                var siteFilterRecords = new List<SiteFilterCheckpointRecord>(siteFilterCount);
                var correlationRecords = new List<CorrelationCheckpointRecord>(correlationCount);
                ReadRecords(reader, siteCount, SiteColorCheckpointRecord.Decode, sites);
                ReadRecords(reader, cutCount, CutDefinitionCheckpointRecord.Decode, cuts);
                ReadRecords(reader, timelineCount, TimelineAnchorCheckpointRecord.Decode, timelines);
                ReadRecords(reader, presentationCount, ScenePresentationCheckpointRecord.Decode, presentationRecords);
                ReadRecords(reader, structureCount, SceneStructureCheckpointRecord.Decode, structureRecords);
                ReadRecords(reader, configurationCount, SceneConfigurationCheckpointRecord.Decode, configurationRecords);
                ReadRecords(reader, siteFilterCount, SiteFilterCheckpointRecord.Decode, siteFilterRecords);
                ReadRecords(reader, correlationCount, CorrelationCheckpointRecord.Decode, correlationRecords);
                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Trailing typed scene checkpoint bytes.");

                ValidateUniqueKeys(sites, cuts, timelines, presentationRecords, structureRecords, configurationRecords, siteFilterRecords, correlationRecords);
                return new PublishedSceneCheckpoint(canonicalSequence, new SceneMutationCheckpoint(sites, cuts, timelines, presentationRecords, structureRecords, configurationRecords, siteFilterRecords, correlationRecords));
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
                writer.Write(checked((uint)record.Length));
                writer.Write(record);
                if (writer.BaseStream.Length > MaximumCheckpointBytes)
                    throw new InvalidDataException("Typed scene checkpoint exceeds its 16 MiB transport bound.");
            }
        }

        private static void ReadRecords<T>(BinaryReader reader, int count, Func<byte[], T> decode, List<T> output)
        {
            for (int i = 0; i < count; i++)
            {
                uint length = reader.ReadUInt32();
                if (length < 6 || length > (uint)MaximumRecordLength)
                    throw new InvalidDataException("Invalid typed checkpoint record length.");
                byte[] record = reader.ReadBytes(checked((int)length));
                if (record.Length != length) throw new EndOfStreamException();
                output.Add(decode(record));
            }
        }

        private static void ValidateCounts(int siteCount, int cutCount, int timelineCount, int presentationCount, int structureCount, int configurationCount, int siteFilterCount, int correlationCount)
        {
            if (siteCount < 0 || cutCount < 0 || timelineCount < 0 || presentationCount < 0 || structureCount < 0 || configurationCount < 0 || siteFilterCount < 0 || correlationCount < 0 || (long)siteCount + cutCount + timelineCount + presentationCount + structureCount + configurationCount + siteFilterCount + correlationCount > MaximumRecords)
                throw new InvalidDataException("Typed scene checkpoint record count exceeds its bound.");
        }

        private static void ValidateUniqueKeys(IReadOnlyList<SiteColorCheckpointRecord> sites, IReadOnlyList<CutDefinitionCheckpointRecord> cuts, IReadOnlyList<TimelineAnchorCheckpointRecord> timelines, IReadOnlyList<ScenePresentationCheckpointRecord> presentationRecords, IReadOnlyList<SceneStructureCheckpointRecord> structureRecords, IReadOnlyList<SceneConfigurationCheckpointRecord> configurationRecords, IReadOnlyList<SiteFilterCheckpointRecord> siteFilterRecords, IReadOnlyList<CorrelationCheckpointRecord> correlationRecords)
        {
            if (siteFilterRecords.Count > 1 || siteFilterRecords.Any(record => record == null))
                throw new InvalidDataException("Typed scene checkpoint contains more than one or a null site-filter result.");
            if (correlationRecords.Count > 1 || correlationRecords.Any(record => record == null))
                throw new InvalidDataException("Typed scene checkpoint contains more than one or a null correlation result.");
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

            var presentationKeys = new HashSet<TouchedKey>();
            foreach (ScenePresentationCheckpointRecord record in presentationRecords)
                if (record?.Value == null || !presentationKeys.Add(MutationDescriptor.Create(ValidationSceneId, ValidationIncarnationId, record.Value).CoalescingKey))
                    throw new InvalidDataException("Typed scene checkpoint contains a duplicate or null scene presentation key.");

            var structureKeys = new HashSet<TouchedKey>();
            foreach (SceneStructureCheckpointRecord record in structureRecords)
            {
                if (record?.Value == null || !IsCheckpointStructureType(record.Value.Type))
                    throw new InvalidDataException("Typed scene checkpoint contains an unsupported scene structure record.");
                foreach (TouchedKey key in MutationDescriptor.Create(ValidationSceneId, ValidationIncarnationId, record.Value).TouchedKeys)
                    if (!structureKeys.Add(key))
                        throw new InvalidDataException("Typed scene checkpoint contains a duplicate scene structure key.");
            }

            var configurationKeys = new HashSet<TouchedKey>();
            var configurationSites = new HashSet<(string ColumnId, string SiteId)>();
            foreach (SceneConfigurationCheckpointRecord record in configurationRecords)
            {
                if (record?.Value == null || record.Value is SetConfigurationTransaction)
                    throw new InvalidDataException("Typed scene checkpoint contains an unsupported scene configuration record.");
                if (!TryAddConfigurationKeys(record.Value, ValidationSceneId, ValidationIncarnationId, configurationKeys, configurationSites))
                    throw new InvalidDataException("Typed scene checkpoint contains a duplicate scene configuration key.");
            }
        }

        private static bool IsCheckpointStructureType(OperationType type) => type is OperationType.CreateCut or OperationType.SetCutOrder or OperationType.CreateRoi or OperationType.SetActiveRoi or OperationType.SetMeshDisplay or OperationType.SetSelectedMri or OperationType.SetMriCalibration or OperationType.SetImplantation or OperationType.ApplyTriangleMask or OperationType.MoveSites;
    }

    public static class PublicationControlCodec
    {
        private static readonly byte[] LiveMagic = Encoding.ASCII.GetBytes("HBLV");
        private static readonly byte[] AcknowledgementMagic = Encoding.ASCII.GetBytes("HBAK");
        private static readonly byte[] ReadyMagic = Encoding.ASCII.GetBytes("HBRD");
        private static readonly byte[] FailedMagic = Encoding.ASCII.GetBytes("HBRF");
        private const int LiveLength = 12;
        private const int AcknowledgementLength = 20;

        // The policy identity is carried on the same ordered reliable stream as the policy.
        public static byte[] EncodeRenderReady(OperationId policyId, bool succeeded = true)
        {
            var bytes = EncodeAcknowledgement(policyId);
            Buffer.BlockCopy(succeeded ? ReadyMagic : FailedMagic, 0, bytes, 0, 4);
            return bytes;
        }

        public static bool TryDecodeRenderReady(byte[] bytes, out OperationId policyId) => TryDecodeRenderReady(bytes, out policyId, out var succeeded) && succeeded;

        public static bool TryDecodeRenderReady(byte[] bytes, out OperationId policyId, out bool succeeded)
        {
            policyId = null;
            succeeded = HasMagic(bytes, ReadyMagic);
            if (!succeeded && !HasMagic(bytes, FailedMagic) || bytes.Length != AcknowledgementLength) return false;
            var copy = (byte[])bytes.Clone();
            Buffer.BlockCopy(AcknowledgementMagic, 0, copy, 0, 4);
            return TryDecodeAcknowledgement(copy, out policyId);
        }

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
