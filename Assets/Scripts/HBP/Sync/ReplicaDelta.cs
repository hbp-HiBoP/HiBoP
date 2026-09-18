using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HBP.Sync
{
    /// <summary>Bounded field assignments between complete canonical checkpoints.</summary>
    public sealed class ReplicaDelta
    {
        private const int Magic = 0x31444248; // HBD1
        private static readonly UTF8Encoding Utf8 = new(false, true);
        public ulong BaseRevision { get; }
        public ulong Revision { get; }
        public IReadOnlyDictionary<StateKey, byte[]> Assignments { get; }
        public IReadOnlyList<StateKey> Removals { get; }

        private ReplicaDelta(ulong baseRevision, ulong revision, SortedDictionary<StateKey, byte[]> assignments, List<StateKey> removals)
        {
            BaseRevision = baseRevision;
            Revision = revision;
            Assignments = assignments;
            Removals = removals;
        }

        public static ReplicaDelta Between(StateSnapshot baseline, StateSnapshot next)
        {
            if (baseline.EpochId != next.EpochId || baseline.ManifestHash != next.ManifestHash || baseline.VisualizationId != next.VisualizationId || next.CommonRevision != baseline.CommonRevision + 1)
                throw new InvalidDataException("Replica delta crosses a session or revision boundary.");
            var assignments = new SortedDictionary<StateKey, byte[]>();
            var removals = new List<StateKey>();
            foreach (var field in next.RawFields)
                if (!baseline.RawFields.TryGetValue(field.Key, out byte[] old) || !old.SequenceEqual(field.Value))
                    assignments.Add(field.Key, (byte[])field.Value.Clone());
            foreach (StateKey key in baseline.RawFields.Keys)
                if (!next.RawFields.ContainsKey(key))
                    removals.Add(key);
            return new ReplicaDelta(baseline.CommonRevision, next.CommonRevision, assignments, removals);
        }

        public StateSnapshot Apply(StateSnapshot baseline)
        {
            if (baseline.CommonRevision != BaseRevision || Revision != BaseRevision + 1)
                throw new InvalidDataException("Replica delta has a stale base revision.");
            var fields = baseline.Fields;
            foreach (StateKey key in Removals)
                if (!fields.Remove(key))
                    throw new InvalidDataException("Replica delta removes an absent field.");
            foreach (var field in Assignments) fields[field.Key] = (byte[])field.Value.Clone();
            var next = baseline.WithFields(fields, Revision);
            StateValidator.Validate(next);
            return next;
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(Magic);
            writer.Write(BaseRevision);
            writer.Write(Revision);
            writer.Write(Assignments.Count);
            writer.Write(Removals.Count);
            foreach (var field in Assignments)
            {
                WriteKey(writer, field.Key);
                writer.Write(field.Value.Length);
                writer.Write(field.Value);
            }

            foreach (StateKey key in Removals) WriteKey(writer, key);
            if (stream.Length > SharedStateSchema.MaxStateBytes) throw new InvalidDataException("Replica delta is too large.");
            return stream.ToArray();
        }

        public static ReplicaDelta Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length > SharedStateSchema.MaxStateBytes) throw new InvalidDataException("Invalid replica delta length.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Utf8);
            try
            {
                if (reader.ReadInt32() != Magic) throw new InvalidDataException("Invalid replica delta.");
                ulong baseRevision = reader.ReadUInt64();
                ulong revision = reader.ReadUInt64();
                if (revision != baseRevision + 1) throw new InvalidDataException("Invalid replica revision.");
                int assigned = reader.ReadInt32();
                int removed = reader.ReadInt32();
                if (assigned < 0 || removed < 0 || assigned + (long)removed > SharedStateSchema.MaxFields) throw new InvalidDataException("Invalid delta field count.");
                var assignments = new SortedDictionary<StateKey, byte[]>();
                var removals = new List<StateKey>(removed);
                for (int i = 0; i < assigned; i++)
                {
                    StateKey key = ReadKey(reader);
                    int length = reader.ReadInt32();
                    if (length < 0 || length > 8 * 1024 * 1024 || length > stream.Length - stream.Position || assignments.ContainsKey(key))
                        throw new InvalidDataException("Invalid delta assignment.");
                    assignments.Add(key, reader.ReadBytes(length));
                }

                for (int i = 0; i < removed; i++)
                {
                    StateKey key = ReadKey(reader);
                    if (assignments.ContainsKey(key) || removals.Contains(key)) throw new InvalidDataException("Duplicate delta field.");
                    removals.Add(key);
                }

                if (stream.Position != stream.Length) throw new InvalidDataException("Trailing delta bytes.");
                return new ReplicaDelta(baseRevision, revision, assignments, removals);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated replica delta.", exception);
            }
        }

        private static void WriteKey(BinaryWriter writer, StateKey key)
        {
            writer.Write((byte)key.Entity);
            WriteText(writer, key.ParentId);
            WriteText(writer, key.Id);
            writer.Write(key.FieldId);
        }

        private static StateKey ReadKey(BinaryReader reader) => new((EntityKind)reader.ReadByte(), ReadText(reader), ReadText(reader), reader.ReadUInt16());

        private static void WriteText(BinaryWriter writer, string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            if (bytes.Length > 256) throw new InvalidDataException("Delta identity is too long.");
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadText(BinaryReader reader)
        {
            int length = reader.ReadUInt16();
            if (length > 256) throw new InvalidDataException("Delta identity is too long.");
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new EndOfStreamException();
            return Utf8.GetString(bytes);
        }
    }
}
