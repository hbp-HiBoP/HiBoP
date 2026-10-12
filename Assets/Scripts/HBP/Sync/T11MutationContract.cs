using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HBP.Sync
{
    public enum ColumnResourceKind : byte
    {
        StaticLabel = 1,
        FmriResource = 2,
        MegResource = 3
    }

    public enum CcepSourceMode : byte
    {
        Site = 0,
        MarsAtlas = 1
    }

    /// <summary>Absolute blacklist state for one prepared site.</summary>
    public sealed class SetSiteBlacklist : Mutation
    {
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public bool Blacklisted { get; }
        public override OperationType Type => OperationType.SetSiteBlacklist;

        public SetSiteBlacklist(ColumnId columnId, SiteId siteId, bool blacklisted)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            SiteId = siteId ?? throw new ArgumentNullException(nameof(siteId));
            Blacklisted = blacklisted;
        }
    }

    /// <summary>Absolute influence radius for an anatomy, static or dynamic column.</summary>
    public sealed class SetInfluenceDistance : Mutation
    {
        public ColumnId ColumnId { get; }
        public float Distance { get; }
        public override OperationType Type => OperationType.SetInfluenceDistance;

        public SetInfluenceDistance(ColumnId columnId, float distance)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            ValueValidation.ValidateFloat(distance, nameof(distance));
            if (distance < 0f) throw new ArgumentOutOfRangeException(nameof(distance));
            Distance = distance;
        }
    }

    /// <summary>A stable, delivery-bound selected label or functional resource.</summary>
    public sealed class SetColumnResource : Mutation
    {
        public ColumnId ColumnId { get; }
        public ColumnResourceKind Kind { get; }
        public string ResourceReference { get; }
        public override OperationType Type => OperationType.SetColumnResource;

        public SetColumnResource(ColumnId columnId, ColumnResourceKind kind, string resourceReference)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            if (kind < ColumnResourceKind.StaticLabel || kind > ColumnResourceKind.MegResource) throw new ArgumentOutOfRangeException(nameof(kind));
            ResourceReference = T09ValueValidation.ValidateOptionalText(resourceReference, nameof(resourceReference));
            Kind = kind;
        }
    }

    /// <summary>The complete synchronized source selection for a prepared CCEP column.</summary>
    public sealed class SetCcepSource : Mutation
    {
        public ColumnId ColumnId { get; }
        public CcepSourceMode Mode { get; }
        public SiteId SourceSiteId { get; }
        public int MarsAtlasLabel { get; }
        public override OperationType Type => OperationType.SetCcepSource;

        public SetCcepSource(ColumnId columnId, CcepSourceMode mode, SiteId sourceSiteId, int marsAtlasLabel)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            if (mode != CcepSourceMode.Site && mode != CcepSourceMode.MarsAtlas) throw new ArgumentOutOfRangeException(nameof(mode));
            if (marsAtlasLabel < -1) throw new ArgumentOutOfRangeException(nameof(marsAtlasLabel));
            if (mode == CcepSourceMode.Site && marsAtlasLabel != -1 || mode == CcepSourceMode.MarsAtlas && sourceSiteId != null)
                throw new ArgumentException("CCEP source fields must match the selected source mode.");
            Mode = mode;
            SourceSiteId = sourceSiteId;
            MarsAtlasLabel = marsAtlasLabel;
        }
    }

    /// <summary>One persisted site assignment; derived filtering and position are intentionally absent.</summary>
    public sealed class SiteConfigurationAssignment
    {
        private readonly string[] m_Labels;

        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public bool Blacklisted { get; }
        public bool Highlighted { get; }
        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }
        public float Alpha { get; }
        public IReadOnlyList<string> Labels => Array.AsReadOnly(m_Labels);

        public SiteConfigurationAssignment(ColumnId columnId, SiteId siteId, bool blacklisted, bool highlighted, float red, float green, float blue, float alpha, IEnumerable<string> labels)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            SiteId = siteId ?? throw new ArgumentNullException(nameof(siteId));
            ValueValidation.ValidateFloat(red, nameof(red));
            ValueValidation.ValidateFloat(green, nameof(green));
            ValueValidation.ValidateFloat(blue, nameof(blue));
            ValueValidation.ValidateFloat(alpha, nameof(alpha));
            if (labels == null) throw new ArgumentNullException(nameof(labels));
            var setLabels = new SetSiteLabels(columnId, siteId, labels);
            m_Labels = setLabels.Labels.ToArray();
            Blacklisted = blacklisted;
            Highlighted = highlighted;
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }
    }

    /// <summary>One reliable, atomic assignment batch for up to the prepared 30,000-site roster.</summary>
    public sealed class SetSiteConfigurationBatch : Mutation
    {
        private readonly SiteConfigurationAssignment[] m_Assignments;

        public IReadOnlyList<SiteConfigurationAssignment> Assignments => Array.AsReadOnly(m_Assignments);
        public override OperationType Type => OperationType.SetSiteConfigurationBatch;

        public SetSiteConfigurationBatch(IEnumerable<SiteConfigurationAssignment> assignments)
        {
            if (assignments == null) throw new ArgumentNullException(nameof(assignments));
            m_Assignments = assignments.ToArray();
            if (m_Assignments.Length == 0 || m_Assignments.Length > 30000)
                throw new ArgumentOutOfRangeException(nameof(assignments), "A site configuration batch must contain 1 to 30,000 assignments.");
            if (m_Assignments.Any(value => value == null)) throw new ArgumentException("Site configuration assignments cannot be null.", nameof(assignments));
            if (m_Assignments.Select(value => (value.ColumnId.Value, value.SiteId.Value)).Distinct().Count() != m_Assignments.Length)
                throw new ArgumentException("A site configuration batch cannot assign a site more than once.", nameof(assignments));
        }
    }

    /// <summary>A bounded atomic group of typed configuration mutations.</summary>
    public sealed class SetConfigurationTransaction : Mutation
    {
        private readonly Mutation[] m_Mutations;

        public IReadOnlyList<Mutation> Mutations => Array.AsReadOnly(m_Mutations);
        public override OperationType Type => OperationType.SetConfigurationTransaction;

        public SetConfigurationTransaction(IEnumerable<Mutation> mutations)
        {
            if (mutations == null) throw new ArgumentNullException(nameof(mutations));
            m_Mutations = mutations.ToArray();
            if (m_Mutations.Length == 0 || m_Mutations.Length > 64)
                throw new ArgumentOutOfRangeException(nameof(mutations), "A configuration transaction must contain 1 to 64 typed mutations.");
            if (m_Mutations.Any(value => value == null || value is SetConfigurationTransaction))
                throw new ArgumentException("Configuration transactions cannot contain null values or nested transactions.", nameof(mutations));
        }
    }

    /// <summary>A typed checkpoint record for one T11 operation.</summary>
    public sealed class T11CheckpointRecord
    {
        private const ushort RecordMagic = 0x5431;
        private const ushort RecordSchema = 1;
        public Mutation Value { get; }

        public T11CheckpointRecord(Mutation value)
        {
            if (value == null || (ushort)value.Type < (ushort)OperationType.SetSiteBlacklist || value is SetConfigurationTransaction)
                throw new ArgumentException("A T11 checkpoint record requires a checkpointable T11 mutation.", nameof(value));
            Value = value;
        }

        public byte[] Encode()
        {
            byte[] mutation = MutationPayloadCodec.Encode(Value);
            using var stream = new MemoryStream(mutation.Length + 8);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(RecordMagic);
            writer.Write(RecordSchema);
            writer.Write(checked((uint)mutation.Length));
            writer.Write(mutation);
            writer.Flush();
            return stream.ToArray();
        }

        public static T11CheckpointRecord Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 12 || bytes.Length > MutationPayloadCodec.MaximumPayloadBytes + 8)
                throw new InvalidDataException("Invalid T11 checkpoint record length.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            try
            {
                if (reader.ReadUInt16() != RecordMagic || reader.ReadUInt16() != RecordSchema)
                    throw new InvalidDataException("Unsupported T11 checkpoint record signature or schema.");
                uint length = reader.ReadUInt32();
                if (length > MutationPayloadCodec.MaximumPayloadBytes || length != stream.Length - stream.Position)
                    throw new InvalidDataException("Invalid T11 checkpoint mutation length.");
                var record = new T11CheckpointRecord(MutationPayloadCodec.Decode(reader.ReadBytes(checked((int)length))));
                if (stream.Position != stream.Length) throw new InvalidDataException("Trailing T11 checkpoint record bytes.");
                return record;
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated T11 checkpoint record.", exception);
            }
        }
    }

    internal static class T11MutationCodec
    {
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        internal static Mutation ReadBody(BinaryReader reader, OperationType type)
        {
            switch (type)
            {
                case OperationType.SetSiteBlacklist:
                    return new SetSiteBlacklist(new ColumnId(ReadText(reader)), new SiteId(ReadText(reader)), ReadBoolean(reader));
                case OperationType.SetInfluenceDistance:
                    return new SetInfluenceDistance(new ColumnId(ReadText(reader)), reader.ReadSingle());
                case OperationType.SetColumnResource:
                    {
                        var columnId = new ColumnId(ReadText(reader));
                        byte kind = reader.ReadByte();
                        if (kind < (byte)ColumnResourceKind.StaticLabel || kind > (byte)ColumnResourceKind.MegResource) throw new InvalidDataException("Invalid prepared column resource kind.");
                        return new SetColumnResource(columnId, (ColumnResourceKind)kind, ReadOptionalText(reader));
                    }
                case OperationType.SetCcepSource:
                    {
                        var columnId = new ColumnId(ReadText(reader));
                        byte mode = reader.ReadByte();
                        if (mode > (byte)CcepSourceMode.MarsAtlas) throw new InvalidDataException("Invalid CCEP source mode.");
                        string siteId = ReadOptionalText(reader);
                        return new SetCcepSource(columnId, (CcepSourceMode)mode, siteId.Length == 0 ? null : new SiteId(siteId), reader.ReadInt32());
                    }
                case OperationType.SetSiteConfigurationBatch:
                    return ReadSiteConfigurationBatch(reader);
                case OperationType.SetConfigurationTransaction:
                    return ReadConfigurationTransaction(reader);
                default:
                    throw new InvalidDataException("Unsupported T11 mutation type.");
            }
        }

        internal static void WriteBody(BinaryWriter writer, Mutation mutation)
        {
            switch (mutation)
            {
                case SetSiteBlacklist value:
                    WriteText(writer, value.ColumnId.Value);
                    WriteText(writer, value.SiteId.Value);
                    writer.Write((byte)(value.Blacklisted ? 1 : 0));
                    break;
                case SetInfluenceDistance value:
                    WriteText(writer, value.ColumnId.Value);
                    writer.Write(value.Distance);
                    break;
                case SetColumnResource value:
                    WriteText(writer, value.ColumnId.Value);
                    writer.Write((byte)value.Kind);
                    WriteOptionalText(writer, value.ResourceReference);
                    break;
                case SetCcepSource value:
                    WriteText(writer, value.ColumnId.Value);
                    writer.Write((byte)value.Mode);
                    WriteOptionalText(writer, value.SourceSiteId?.Value ?? string.Empty);
                    writer.Write(value.MarsAtlasLabel);
                    break;
                case SetSiteConfigurationBatch value:
                    WriteSiteConfigurationBatch(writer, value);
                    break;
                case SetConfigurationTransaction value:
                    WriteConfigurationTransaction(writer, value);
                    break;
                default:
                    throw new ArgumentException("Unsupported T11 mutation type.", nameof(mutation));
            }
        }

        private static SetSiteConfigurationBatch ReadSiteConfigurationBatch(BinaryReader reader)
        {
            ushort count = reader.ReadUInt16();
            if (count == 0 || count > 30000) throw new InvalidDataException("Invalid site configuration batch count.");
            var assignments = new SiteConfigurationAssignment[count];
            for (int i = 0; i < count; i++)
            {
                var columnId = new ColumnId(ReadText(reader));
                var siteId = new SiteId(ReadText(reader));
                bool blacklisted = ReadBoolean(reader);
                bool highlighted = ReadBoolean(reader);
                float red = reader.ReadSingle();
                float green = reader.ReadSingle();
                float blue = reader.ReadSingle();
                float alpha = reader.ReadSingle();
                ushort labelCount = reader.ReadUInt16();
                if (labelCount > 128) throw new InvalidDataException("Site configuration label count exceeds its bound.");
                var labels = new string[labelCount];
                for (int label = 0; label < labelCount; label++) labels[label] = ReadText(reader);
                assignments[i] = new SiteConfigurationAssignment(columnId, siteId, blacklisted, highlighted, red, green, blue, alpha, labels);
            }

            return new SetSiteConfigurationBatch(assignments);
        }

        private static SetConfigurationTransaction ReadConfigurationTransaction(BinaryReader reader)
        {
            ushort count = reader.ReadUInt16();
            if (count == 0 || count > 64) throw new InvalidDataException("Invalid configuration transaction count.");
            var mutations = new Mutation[count];
            for (int i = 0; i < count; i++)
            {
                uint length = reader.ReadUInt32();
                if (length < 4 || length > MutationPayloadCodec.MaximumPayloadBytes || length > reader.BaseStream.Length - reader.BaseStream.Position)
                    throw new InvalidDataException("Invalid nested configuration mutation length.");
                mutations[i] = MutationPayloadCodec.Decode(reader.ReadBytes(checked((int)length)));
                if (mutations[i] is SetConfigurationTransaction) throw new InvalidDataException("Nested configuration transactions are not supported.");
            }

            return new SetConfigurationTransaction(mutations);
        }

        private static void WriteSiteConfigurationBatch(BinaryWriter writer, SetSiteConfigurationBatch value)
        {
            writer.Write(checked((ushort)value.Assignments.Count));
            foreach (SiteConfigurationAssignment assignment in value.Assignments)
            {
                WriteText(writer, assignment.ColumnId.Value);
                WriteText(writer, assignment.SiteId.Value);
                writer.Write((byte)(assignment.Blacklisted ? 1 : 0));
                writer.Write((byte)(assignment.Highlighted ? 1 : 0));
                writer.Write(assignment.Red);
                writer.Write(assignment.Green);
                writer.Write(assignment.Blue);
                writer.Write(assignment.Alpha);
                writer.Write(checked((ushort)assignment.Labels.Count));
                foreach (string label in assignment.Labels) WriteText(writer, label);
            }
        }

        private static void WriteConfigurationTransaction(BinaryWriter writer, SetConfigurationTransaction value)
        {
            writer.Write(checked((ushort)value.Mutations.Count));
            foreach (Mutation mutation in value.Mutations)
            {
                byte[] bytes = MutationPayloadCodec.Encode(mutation);
                writer.Write(checked((uint)bytes.Length));
                writer.Write(bytes);
            }
        }

        private static string ReadText(BinaryReader reader) => ReadText(reader, allowEmpty: false);
        private static string ReadOptionalText(BinaryReader reader) => ReadText(reader, allowEmpty: true);

        private static string ReadText(BinaryReader reader, bool allowEmpty)
        {
            ushort length = reader.ReadUInt16();
            if ((!allowEmpty && length == 0) || length > 256 || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid T11 text length.");
            string value = StrictUtf8.GetString(reader.ReadBytes(length));
            if (!StringComparer.Ordinal.Equals(value, value.Normalize(NormalizationForm.FormC))) throw new InvalidDataException("T11 text is not NFC-normalized.");
            foreach (char character in value)
                if (char.IsControl(character))
                    throw new InvalidDataException("T11 text contains a control character.");
            return value;
        }

        private static void WriteText(BinaryWriter writer, string value) => WriteText(writer, value, allowEmpty: false);
        private static void WriteOptionalText(BinaryWriter writer, string value) => WriteText(writer, value, allowEmpty: true);

        private static void WriteText(BinaryWriter writer, string value, bool allowEmpty)
        {
            byte[] bytes = StrictUtf8.GetBytes(value);
            if ((!allowEmpty && bytes.Length == 0) || bytes.Length > 256) throw new InvalidDataException("T11 text exceeds its bounds.");
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        private static bool ReadBoolean(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid T11 boolean value.");
            return value == 1;
        }

        private static string ValidateText(string value, string name, bool allowEmpty)
        {
            if (value == null) throw new ArgumentNullException(name);
            string normalized;
            try
            {
                normalized = value.Normalize(NormalizationForm.FormC);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("Text must contain valid Unicode.", name, exception);
            }

            if (!allowEmpty && normalized.Length == 0) throw new ArgumentException("Text must not be empty.", name);
            foreach (char character in normalized)
                if (char.IsControl(character))
                    throw new ArgumentException("Text must not contain control characters.", name);
            if (StrictUtf8.GetByteCount(normalized) > 256) throw new ArgumentException("Text exceeds 256 UTF-8 bytes.", name);
            return normalized;
        }
    }
}
