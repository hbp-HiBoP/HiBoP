using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HBP.Transfer.Anatomy
{
    /// <summary>
    /// Deterministic little-endian v1/v2/v3 codec. Hashes detect corruption, not sender
    /// authenticity. Numeric payloads remain float32/uint32. No transport dependency.
    /// </summary>
    public static class AnatomySnapshotCodec
    {
        public const ushort SchemaVersion = 3;
        public const int MaximumVertexCount = 2_000_000;
        public const int MaximumIndexCount = 12_000_000;
        public const int MaximumEncodedBytes = 128 * 1024 * 1024;
        public const int MaximumTextBytes = 1024;
        private const uint Magic = 0x414E4248; // HBNA

        private const int HashBytes = 32;

        // Fixed fields including five string length prefixes and both SHA-256 hashes.
        private const int FixedBytes = 214;
        private static readonly UTF8Encoding TextEncoding = new UTF8Encoding(false, true);

        /// <summary>Location of the stored surface hash in the matching encoded snapshot.
        /// Use only after Decode verified that payload; the v2 contact suffix changes its distance from the end.</summary>
        public static int GetSurfaceHashOffset(AnatomySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return FixedBytes - 2 * HashBytes + TextEncoding.GetByteCount(snapshot.TransferId) + TextEncoding.GetByteCount(snapshot.SessionId) + TextEncoding.GetByteCount(snapshot.VisualizationId) + TextEncoding.GetByteCount(snapshot.ColumnId) + TextEncoding.GetByteCount(snapshot.Coordinates.FrameId);
        }

        internal static void ValidateText(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumTextBytes || TextEncoding.GetByteCount(value) > MaximumTextBytes)
                throw new ArgumentException("Identifiers must contain 1..1024 UTF-8 bytes.");
        }

        internal static long ValidateCounts(int vertices, int indices, int uvs)
        {
            if (vertices < 3 || vertices > MaximumVertexCount || indices < 3 || indices > MaximumIndexCount || indices % 3 != 0 || (uvs != 0 && uvs != vertices))
                throw new ArgumentException("Surface counts exceed limits or are inconsistent.");
            return 24L * vertices + 4L * indices + 8L * uvs;
        }

        public static byte[] Encode(AnatomySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            string[] texts =
            {
                snapshot.TransferId, snapshot.SessionId, snapshot.VisualizationId,
                snapshot.ColumnId, snapshot.Coordinates.FrameId
            };
            long contactBytes = snapshot.SchemaVersion == 1 ? 0 : AnatomyContactsCodec.Length(snapshot.Contacts);
            long length = FixedBytes + snapshot.SurfaceByteLength + (contactBytes == 0 ? 0 : 4 + contactBytes);
            if (snapshot.Projection != null) length += 4 + AnatomyProjectionCodec.Length(snapshot.Projection);
            foreach (string value in texts) length += TextEncoding.GetByteCount(value);
            if (length > MaximumEncodedBytes) throw new ArgumentException($"Snapshot requires {length} bytes (surface {snapshot.SurfaceByteLength}, volume {snapshot.Projection?.VolumeBytes.Count ?? 0}); codec limit is {MaximumEncodedBytes}. Keep all scientific inputs; choose a smaller source dataset or qualify a larger transport budget.");
            byte[] encoded = new byte[(int)length];
            using var stream = new MemoryStream(encoded, true);
            using var writer = new BinaryWriter(stream, TextEncoding, true);
            writer.Write(Magic);
            writer.Write(snapshot.SchemaVersion);
            writer.Write(length);
            foreach (string value in texts)
            {
                byte[] bytes = TextEncoding.GetBytes(value);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            writer.Write(snapshot.ContentRevision);
            writer.Write((byte)snapshot.Coordinates.Handedness);
            writer.Write((byte)snapshot.Coordinates.Unit);
            writer.Write(snapshot.Coordinates.MappingVersion);
            Write(writer, snapshot.Coordinates.AssetToBrain);
            writer.Write((byte)snapshot.Winding);
            writer.Write(snapshot.Visible ? (byte)1 : (byte)0);
            Write(writer, snapshot.Color);
            writer.Write(snapshot.VertexCount);
            writer.Write(snapshot.Indices.Count);
            writer.Write(snapshot.Uvs.Count / 2);
            writer.Write(snapshot.SurfaceByteLength);
            int hashOffset = (int)stream.Position;
            stream.Position += HashBytes;
            int surfaceOffset = (int)stream.Position;
            Write(writer, snapshot.Positions);
            Write(writer, snapshot.Normals);
            foreach (uint index in snapshot.Indices.AsReadOnlySpan()) writer.Write(index);
            Write(writer, snapshot.Uvs);
            if (snapshot.SchemaVersion >= 2)
            {
                writer.Write((int)contactBytes);
                AnatomyContactsCodec.Write(writer, snapshot.Contacts);
            }

            if (snapshot.Projection != null)
            {
                writer.Write((int)AnatomyProjectionCodec.Length(snapshot.Projection));
                AnatomyProjectionCodec.Write(writer, snapshot.Projection);
            }

            if (stream.Position != length - HashBytes) throw new InvalidOperationException("Invalid codec layout.");
            using SHA256 sha = SHA256.Create();
            byte[] surfaceHash = sha.ComputeHash(encoded, surfaceOffset, (int)snapshot.SurfaceByteLength);
            Buffer.BlockCopy(surfaceHash, 0, encoded, hashOffset, HashBytes);
            byte[] contentHash = sha.ComputeHash(encoded, 0, encoded.Length - HashBytes);
            Buffer.BlockCopy(contentHash, 0, encoded, encoded.Length - HashBytes, HashBytes);
            return encoded;
        }

        /// <summary>
        /// Caller keeps encoded bytes stable for this call. Decode checks envelope
        /// size/hash, metadata and exact surface size/hash before allocating numeric
        /// buffers. Returned snapshot owns fresh arrays and never aliases encoded.
        /// </summary>
        public static AnatomySnapshot Decode(byte[] encoded)
        {
            if (encoded == null) throw new ArgumentNullException(nameof(encoded));
            if (encoded.Length < FixedBytes || encoded.Length > MaximumEncodedBytes)
                throw new InvalidDataException("Snapshot length is outside supported limits.");
            try
            {
                using var stream = new MemoryStream(encoded, false);
                using var reader = new BinaryReader(stream, TextEncoding, true);
                if (reader.ReadUInt32() != Magic) throw new InvalidDataException("Unknown snapshot magic.");
                ushort version = reader.ReadUInt16();
                if (version != 1 && version != 2 && version != SchemaVersion) throw new InvalidDataException("Unsupported snapshot schema version.");
                if (reader.ReadInt64() != encoded.Length) throw new InvalidDataException("Snapshot length does not match its header.");
                VerifyHash(encoded, 0, encoded.Length - HashBytes, encoded.Length - HashBytes);
                string transferId = ReadText(reader);
                string sessionId = ReadText(reader);
                string visualizationId = ReadText(reader);
                string columnId = ReadText(reader);
                string frameId = ReadText(reader);
                ulong revision = reader.ReadUInt64();
                var handedness = (AnatomyHandedness)reader.ReadByte();
                var unit = (AnatomyLengthUnit)reader.ReadByte();
                uint mappingVersion = reader.ReadUInt32();
                var coordinates = new AnatomyCoordinateSpace(frameId, handedness, unit, mappingVersion, ReadFloats(reader, 16));
                var winding = (AnatomyWinding)reader.ReadByte();
                byte visible = reader.ReadByte();
                if (visible > 1) throw new InvalidDataException("Invalid visibility value.");
                float[] color = ReadFloats(reader, 4);
                AnatomySnapshot.ValidateMetadata(transferId, sessionId, visualizationId, columnId, revision, coordinates, winding, color);
                int vertices = reader.ReadInt32();
                int indices = reader.ReadInt32();
                int uvs = reader.ReadInt32();
                long surfaceBytes = ValidateCounts(vertices, indices, uvs);
                if (reader.ReadInt64() != surfaceBytes || stream.Position + HashBytes + surfaceBytes + HashBytes > encoded.Length)
                    throw new InvalidDataException("Surface dimensions do not match the exact encoded length.");
                int hashOffset = (int)stream.Position;
                stream.Position += HashBytes;
                VerifyHash(encoded, (int)stream.Position, (int)surfaceBytes, hashOffset);
                long surfaceStart = stream.Position;
                stream.Position += surfaceBytes;
                AnatomyContacts contacts = AnatomyContacts.Empty;
                if (version >= 2)
                {
                    int contactBytes = reader.ReadInt32();
                    if (contactBytes < 0 || stream.Position + contactBytes > encoded.Length - HashBytes) throw new InvalidDataException("Invalid contact section length.");
                    contacts = AnatomyContactsCodec.Read(reader, stream.Position + contactBytes);
                    contacts.ValidateCoordinates(coordinates);
                }

                AnatomyProjection projection = null;
                if (version >= 3)
                {
                    int projectionBytes = reader.ReadInt32();
                    if (projectionBytes < 56 || stream.Position + projectionBytes != encoded.Length - HashBytes) throw new InvalidDataException("Missing or invalid projection section.");
                    projection = AnatomyProjectionCodec.Read(reader, encoded.Length - HashBytes);
                }

                if (stream.Position != encoded.Length - HashBytes) throw new InvalidDataException("Unexpected snapshot trailing bytes.");
                stream.Position = surfaceStart;
                float[] positions = ReadFloats(reader, vertices * 3);
                float[] normals = ReadFloats(reader, vertices * 3);
                uint[] triangles = new uint[indices];
                for (int i = 0; i < triangles.Length; i++) triangles[i] = reader.ReadUInt32();
                float[] textureCoordinates = ReadFloats(reader, uvs * 2);
                return new AnatomySnapshot(transferId, sessionId, visualizationId, columnId, revision, coordinates, winding, visible == 1, color, positions, normals, triangles, textureCoordinates, contacts, projection);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated snapshot.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid anatomical snapshot.", exception);
            }
        }

        private static string ReadText(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 1 || length > MaximumTextBytes || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid identifier length.");
            string text = TextEncoding.GetString(reader.ReadBytes(length));
            ValidateText(text);
            return text;
        }

        private static void VerifyHash(byte[] encoded, int offset, int count, int hashOffset)
        {
            using SHA256 sha = SHA256.Create();
            byte[] computed = sha.ComputeHash(encoded, offset, count);
            for (int i = 0; i < HashBytes; i++)
                if (computed[i] != encoded[hashOffset + i])
                    throw new InvalidDataException("Snapshot SHA-256 mismatch.");
        }

        private static float[] ReadFloats(BinaryReader reader, int count)
        {
            float[] values = new float[count];
            for (int i = 0; i < values.Length; i++) values[i] = reader.ReadSingle();
            return values;
        }

        private static void Write(BinaryWriter writer, AnatomyBuffer<float> values)
        {
            foreach (float value in values.AsReadOnlySpan()) writer.Write(value);
        }
    }
}
