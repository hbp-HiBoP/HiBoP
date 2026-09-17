using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    /// <summary>Canonical schema-1 snapshot encoding. Resource bytes travel separately.</summary>
    public static class SharedStateCodec
    {
        private const int Magic = 0x31534248; // HBS1, little endian
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Encode(StateSnapshot state)
        {
            StateValidator.Validate(state);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(Magic);
            writer.Write(state.SchemaVersion);
            writer.Write(state.EpochId.ToByteArray());
            WriteString(writer, state.VisualizationId);
            WriteString(writer, state.ManifestHash);
            writer.Write(state.CommonRevision);
            writer.Write(state.RawFields.Count);
            foreach (var entry in state.RawFields)
            {
                writer.Write((byte)entry.Key.Entity);
                WriteString(writer, entry.Key.ParentId);
                WriteString(writer, entry.Key.Id);
                writer.Write(entry.Key.FieldId);
                writer.Write(entry.Value.Length);
                writer.Write(entry.Value);
            }

            if (stream.Length > SharedStateSchema.MaxStateBytes)
                throw new InvalidDataException("Encoded state too large");
            return stream.ToArray();
        }

        public static StateSnapshot Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length > SharedStateSchema.MaxStateBytes)
                throw new InvalidDataException("Invalid state length");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Utf8);
            try
            {
                if (reader.ReadInt32() != Magic) throw new InvalidDataException("Invalid state magic");
                ushort version = reader.ReadUInt16();
                if (version != SharedStateSchema.Version) throw new InvalidDataException("Unsupported schema");
                Guid epoch = new Guid(ReadExact(reader, 16));
                string visualization = ReadString(reader, 256);
                string manifest = ReadString(reader, 256);
                ulong revision = reader.ReadUInt64();
                int count = reader.ReadInt32();
                if (count < 0 || count > SharedStateSchema.MaxFields) throw new InvalidDataException("Invalid field count");
                var fields = new List<KeyValuePair<StateKey, byte[]>>(count);
                StateKey? previous = null;
                for (int i = 0; i < count; i++)
                {
                    EntityKind entity = (EntityKind)reader.ReadByte();
                    string parent = ReadString(reader, 256);
                    string id = ReadString(reader, 256);
                    ushort field = reader.ReadUInt16();
                    int length = reader.ReadInt32();
                    if (length < 0 || length > 8 * 1024 * 1024 || length > stream.Length - stream.Position)
                        throw new InvalidDataException("Invalid value length");
                    var key = new StateKey(entity, parent, id, field);
                    if (previous.HasValue && previous.Value.CompareTo(key) >= 0)
                        throw new InvalidDataException("Noncanonical field order or duplicate");
                    fields.Add(new KeyValuePair<StateKey, byte[]>(key, ReadExact(reader, length)));
                    previous = key;
                }

                if (stream.Position != stream.Length) throw new InvalidDataException("Trailing state data");
                var state = new StateSnapshot(epoch, visualization, manifest, revision, fields, version);
                StateValidator.Validate(state);
                return state;
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated state", exception);
            }
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadString(BinaryReader reader, int maxBytes)
        {
            int length = reader.ReadUInt16();
            if (length > maxBytes) throw new InvalidDataException("String too long");
            return Utf8.GetString(ReadExact(reader, length));
        }

        private static byte[] ReadExact(BinaryReader reader, int length)
        {
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new EndOfStreamException();
            return bytes;
        }
    }
}
