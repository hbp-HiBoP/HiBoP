using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HBP.Sync
{
    public readonly struct StateKey : IComparable<StateKey>, IEquatable<StateKey>
    {
        public EntityKind Entity { get; }
        public string ParentId { get; }
        public string Id { get; }
        public ushort FieldId { get; }

        public StateKey(EntityKind entity, string parentId, string id, ushort fieldId)
        {
            Entity = entity;
            ParentId = parentId ?? string.Empty;
            Id = id ?? string.Empty;
            FieldId = fieldId;
        }

        public int CompareTo(StateKey other)
        {
            int result = Entity.CompareTo(other.Entity);
            if (result != 0) return result;
            result = StringComparer.Ordinal.Compare(ParentId, other.ParentId);
            if (result != 0) return result;
            result = StringComparer.Ordinal.Compare(Id, other.Id);
            return result != 0 ? result : FieldId.CompareTo(other.FieldId);
        }

        public bool Equals(StateKey other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is StateKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Entity, ParentId, Id, FieldId);
        public override string ToString() => $"{Entity}/{ParentId}/{Id}/{FieldId}";
    }

    public sealed class StateSnapshot
    {
        public ushort SchemaVersion { get; }
        public Guid EpochId { get; }
        public string VisualizationId { get; }
        public string ManifestHash { get; }
        public ulong CommonRevision { get; }
        internal SortedDictionary<StateKey, byte[]> RawFields { get; }
        public SortedDictionary<StateKey, byte[]> Fields => CloneFields(RawFields);

        public StateSnapshot(Guid epochId, string visualizationId, string manifestHash, ulong commonRevision, IEnumerable<KeyValuePair<StateKey, byte[]>> fields, ushort schemaVersion = SharedStateSchema.Version)
        {
            SchemaVersion = schemaVersion;
            EpochId = epochId;
            VisualizationId = visualizationId;
            ManifestHash = manifestHash;
            CommonRevision = commonRevision;
            RawFields = new SortedDictionary<StateKey, byte[]>();
            foreach (var field in fields)
            {
                if (field.Value == null || RawFields.ContainsKey(field.Key))
                    throw new ArgumentException("Null or duplicate state field", nameof(fields));
                RawFields.Add(field.Key, (byte[])field.Value.Clone());
            }
        }

        public StateSnapshot WithFields(IEnumerable<KeyValuePair<StateKey, byte[]>> fields, ulong revision) => new StateSnapshot(EpochId, VisualizationId, ManifestHash, revision, fields, SchemaVersion);

        private static SortedDictionary<StateKey, byte[]> CloneFields(SortedDictionary<StateKey, byte[]> source)
        {
            var copy = new SortedDictionary<StateKey, byte[]>();
            foreach (var entry in source) copy.Add(entry.Key, (byte[])entry.Value.Clone());
            return copy;
        }
    }

    public static class StateValue
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Bool(bool value) => new[] { value ? (byte)1 : (byte)0 };
        public static byte[] Int(int value) => BitConverter.GetBytes(value);

        public static byte[] Float(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            return BitConverter.GetBytes(value == 0 ? 0f : value);
        }

        public static byte[] Text(string value) => Utf8.GetBytes(value ?? throw new ArgumentNullException(nameof(value)));
        public static byte[] Vector3(float x, float y, float z) => Floats(x, y, z);
        public static byte[] Color(float r, float g, float b, float a) => Floats(r, g, b, a);
        public static byte[] Mask(byte[] bits) => (byte[])(bits ?? throw new ArgumentNullException(nameof(bits))).Clone();

        public static byte[] TextList(IReadOnlyList<string> values)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(values.Count);
            foreach (string value in values)
            {
                byte[] bytes = Text(value);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            return stream.ToArray();
        }

        private static byte[] Floats(params float[] values)
        {
            using var stream = new MemoryStream();
            foreach (float value in values)
            {
                byte[] bytes = Float(value);
                stream.Write(bytes, 0, bytes.Length);
            }

            return stream.ToArray();
        }

        internal static void Validate(ValueKind kind, byte[] bytes)
        {
            if (bytes == null || bytes.Length > 8 * 1024 * 1024) throw new InvalidDataException("Invalid value length");
            switch (kind)
            {
                case ValueKind.Bool:
                    if (bytes.Length != 1 || bytes[0] > 1) throw new InvalidDataException("Invalid boolean");
                    break;
                case ValueKind.Int:
                    if (bytes.Length != 4) throw new InvalidDataException("Invalid integer");
                    break;
                case ValueKind.Float:
                case ValueKind.Vector3:
                case ValueKind.Color:
                    int count = kind == ValueKind.Float ? 1 : kind == ValueKind.Vector3 ? 3 : 4;
                    if (bytes.Length != count * 4) throw new InvalidDataException("Invalid float vector");
                    for (int i = 0; i < count; i++)
                    {
                        float value = BitConverter.ToSingle(bytes, i * 4);
                        if (float.IsNaN(value) || float.IsInfinity(value) || (value == 0 && BitConverter.ToInt32(bytes, i * 4) != 0))
                            throw new InvalidDataException("Noncanonical float");
                    }

                    break;
                case ValueKind.Text:
                case ValueKind.Id:
                case ValueKind.Resource:
                    if (bytes.Length > 4096) throw new InvalidDataException("Text too long");
                    string text = Utf8.GetString(bytes);
                    if (kind == ValueKind.Id && !ValidId(text, true)) throw new InvalidDataException("Invalid reference ID");
                    if (kind == ValueKind.Resource && text.Length != 0 && !ValidResource(text))
                        throw new InvalidDataException("Invalid resource reference");
                    break;
                case ValueKind.TextList:
                    using (var stream = new MemoryStream(bytes, false))
                    using (var reader = new BinaryReader(stream, Utf8))
                    {
                        if (stream.Length < 4) throw new InvalidDataException("Invalid list");
                        int itemCount = reader.ReadInt32();
                        if (itemCount < 0 || itemCount > 4096) throw new InvalidDataException("List too long");
                        for (int i = 0; i < itemCount; i++)
                        {
                            if (stream.Length - stream.Position < 4) throw new InvalidDataException("Invalid list item");
                            int length = reader.ReadInt32();
                            if (length < 0 || length > 4096 || stream.Length - stream.Position < length)
                                throw new InvalidDataException("Invalid list item length");
                            Utf8.GetString(reader.ReadBytes(length));
                        }

                        if (stream.Position != stream.Length) throw new InvalidDataException("Trailing list data");
                    }

                    break;
                case ValueKind.Mask:
                    break; // Exact bit count is checked against the prepared topology in S2.
                default:
                    throw new InvalidDataException("Unknown value type");
            }
        }

        internal static bool ValidId(string value, bool allowEmpty = false) => value != null && (allowEmpty || value.Length > 0) && value.Length <= 256 && Utf8.GetByteCount(value) <= 256 && value.IsNormalized(NormalizationForm.FormC) && value.All(c => !char.IsControl(c));

        internal static bool ValidHash(string value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');

        private static bool ValidResource(string value)
        {
            // kind:lowercase-sha256:positive-version
            string[] parts = value.Split(':');
            return parts.Length == 3 && parts[0].Length > 0 && parts[0].All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-') && ValidHash(parts[1]) && parts[2].Length > 0 && parts[2][0] >= '1' && parts[2][0] <= '9' && parts[2].All(c => c >= '0' && c <= '9') && int.TryParse(parts[2], out int version) && version > 0;
        }
    }

    public static class StateValidator
    {
        public static void Validate(StateSnapshot state)
        {
            if (state.SchemaVersion != SharedStateSchema.Version || state.EpochId == Guid.Empty || !StateValue.ValidId(state.VisualizationId) || !StateValue.ValidHash(state.ManifestHash) || state.RawFields.Count > SharedStateSchema.MaxFields)
                throw new InvalidDataException("Invalid state header");
            int total = 0;
            foreach (var entry in state.RawFields)
            {
                StateKey key = entry.Key;
                if (!SharedStateSchema.TryGet(key.Entity, key.FieldId, out FieldDefinition field) || !ValidKey(key)) throw new InvalidDataException($"Unknown or invalid field {key}");
                StateValue.Validate(field.Value, entry.Value);
                total = checked(total + entry.Value.Length + key.Id.Length + key.ParentId.Length + 16);
                if (total > SharedStateSchema.MaxStateBytes) throw new InvalidDataException("State too large");
                ValidateBounds(field, entry.Value);
            }

            ThreeWayStateMerge.ValidateReferences(state);
        }

        private static void ValidateBounds(FieldDefinition field, byte[] value)
        {
            if (field.Value == ValueKind.Int)
            {
                int number = BitConverter.ToInt32(value, 0);
                if ((field.Name == "order" || field.Name == "timelineIndex" || field.Name == "localizerIndex") && number < 0 || field.Name == "timelineStep" && number <= 0 || (field.Name == "modality" || field.Name == "orientation" || field.Name == "meshPart" || field.Name == "representation" || field.Name == "colormap" || field.Name == "ccepSourceMode") && (number < 0 || number > 255))
                    throw new InvalidDataException($"Invalid {field.Name}");
            }

            if (field.Value != ValueKind.Float) return;
            float scalar = BitConverter.ToSingle(value, 0);
            if ((field.Name.Contains("Alpha") || field.Name == "brainAlpha" || field.Name == "activityAlpha") && (scalar < 0 || scalar > 1) || (field.Name.Contains("InfluenceDistance") || field.Name == "influenceRadius" || field.Name == "siteGain") && scalar < 0 || field.Name == "timelineSampling" && scalar <= 0)
                throw new InvalidDataException($"Invalid {field.Name}");
        }

        private static bool ValidKey(StateKey key)
        {
            if (key.Entity == EntityKind.Scene) return key.ParentId.Length == 0 && key.Id.Length == 0;
            if (!StateValue.ValidId(key.Id)) return false;
            return key.Entity == EntityKind.Site || key.Entity == EntityKind.Sphere ? StateValue.ValidId(key.ParentId) : key.ParentId.Length == 0;
        }
    }
}
