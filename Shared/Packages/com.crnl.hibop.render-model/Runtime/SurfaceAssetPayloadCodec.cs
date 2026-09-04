using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.RenderModel
{
    public static class SurfaceAssetPayloadCodec
    {
        public const ushort SchemaVersion = 1;
        public const int MaximumVertexCount = 2_000_000;
        public const int MaximumIndexCount = 12_000_000;

        public static byte[] Encode(SurfaceAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((byte)asset.Representation);
            ComputeExactBounds(asset.Positions, out Float3 minimum, out Float3 maximum);
            Write(writer, minimum);
            Write(writer, maximum);
            writer.Write(asset.Positions.Count);
            writer.Write(asset.Indices.Count);
            writer.Write(asset.StaticUvs.Count);
            Write(writer, asset.Positions);
            Write(writer, asset.Normals);
            for (int index = 0; index < asset.Indices.Count; index++)
                writer.Write(asset.Indices[index]);
            for (int index = 0; index < asset.StaticUvs.Count; index++)
            {
                writer.Write(asset.StaticUvs[index].X);
                writer.Write(asset.StaticUvs[index].Y);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static AssetHash ComputeHash(byte[] payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            using SHA256 sha256 = SHA256.Create();
            return AssetHash.FromBytes(sha256.ComputeHash(payload));
        }

        public static SurfaceAsset Decode(byte[] payload, AssetHash expectedHash)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            using var stream = new MemoryStream(payload, false);
            return Decode(stream, expectedHash, payload.Length);
        }

        public static SurfaceAsset Decode(Stream payload, AssetHash expectedHash, int expectedBytes)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (!expectedHash.IsValid)
                throw new ArgumentException("A valid expected hash is required.", nameof(expectedHash));
            if (expectedBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedBytes));
            if (!payload.CanRead || !payload.CanSeek)
                throw new ArgumentException("The payload stream must be readable and seekable.", nameof(payload));

            long start = payload.Position;
            if (payload.Length - start != expectedBytes)
                throw new InvalidDataException("The surface payload length does not match its descriptor.");
            using (SHA256 sha256 = SHA256.Create())
            {
                AssetHash computed = AssetHash.FromBytes(sha256.ComputeHash(payload));
                if (computed != expectedHash)
                    throw new InvalidDataException("The surface payload hash does not match its descriptor.");
            }

            payload.Position = start;
            using var reader = new BinaryReader(payload, Encoding.UTF8, true);
            SurfaceRepresentation representation = (SurfaceRepresentation)reader.ReadByte();
            Bounds3F bounds = new(ReadFloat3(reader), ReadFloat3(reader));
            int vertexCount = ReadCount(reader, MaximumVertexCount, "vertex");
            int indexCount = ReadCount(reader, MaximumIndexCount, "index");
            int uvCount = ReadCount(reader, MaximumVertexCount, "UV", true);
            if (indexCount % 3 != 0 || (uvCount != 0 && uvCount != vertexCount))
                throw new InvalidDataException("Surface buffer counts are inconsistent.");

            long exactBytes = 37L + (24L * vertexCount) + (4L * indexCount) + (8L * uvCount);
            if (exactBytes != expectedBytes)
                throw new InvalidDataException("Surface dimensions do not match the encoded length.");

            var positions = new Float3[vertexCount];
            var normals = new Float3[vertexCount];
            var indices = new uint[indexCount];
            var uvs = new Float2[uvCount];
            for (int index = 0; index < positions.Length; index++)
                positions[index] = ReadFloat3(reader);
            for (int index = 0; index < normals.Length; index++)
                normals[index] = ReadFloat3(reader);
            for (int index = 0; index < indices.Length; index++)
                indices[index] = reader.ReadUInt32();
            for (int index = 0; index < uvs.Length; index++)
                uvs[index] = new Float2(reader.ReadSingle(), reader.ReadSingle());
            if (payload.Position != start + expectedBytes)
                throw new InvalidDataException("Surface payload contains trailing data.");

            return new SurfaceAsset(expectedHash, representation, CoordinateSpace.DesktopUnityMillimetersV1, bounds, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Float3>.TakeOwnership(normals), RenderBuffer<uint>.TakeOwnership(indices), RenderBuffer<Float2>.TakeOwnership(uvs));
        }

        private static int ReadCount(BinaryReader reader, int maximum, string label, bool allowZero = false)
        {
            int value = reader.ReadInt32();
            if (value < (allowZero ? 0 : 1) || value > maximum)
                throw new InvalidDataException($"Surface {label} count is outside the supported range.");
            return value;
        }

        private static Float3 ReadFloat3(BinaryReader reader)
        {
            return new Float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        private static void ComputeExactBounds(RenderBuffer<Float3> positions, out Float3 minimum, out Float3 maximum)
        {
            minimum = positions[0];
            maximum = positions[0];
            for (int index = 1; index < positions.Count; index++)
            {
                Float3 position = positions[index];
                minimum = new Float3(Math.Min(minimum.X, position.X), Math.Min(minimum.Y, position.Y), Math.Min(minimum.Z, position.Z));
                maximum = new Float3(Math.Max(maximum.X, position.X), Math.Max(maximum.Y, position.Y), Math.Max(maximum.Z, position.Z));
            }
        }

        private static void Write(BinaryWriter writer, RenderBuffer<Float3> values)
        {
            for (int index = 0; index < values.Count; index++)
                Write(writer, values[index]);
        }

        private static void Write(BinaryWriter writer, Float3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }
    }
}
