using System;
using System.IO;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using UnityEngine;

namespace CRNL.HiBoP.XR.StaticRendering
{
    internal static class P05SurfaceAssetBinary
    {
        internal const ulong Magic = 0x3530505258425048UL; // HBPXRP05 in little-endian order.
        internal const ushort SchemaVersion = 1;
        private const int HeaderLength = sizeof(ulong) + sizeof(ushort) + AssetHash.ByteLength;
        private const int MaximumVertexCount = 2_000_000;
        private const int MaximumIndexCount = 12_000_000;

        public static SurfaceAsset Read(TextAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Read(source.bytes);
        }

        internal static SurfaceAsset Read(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length <= HeaderLength)
            {
                throw new InvalidDataException("P05 surface asset is truncated.");
            }

            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt64() != Magic)
            {
                throw new InvalidDataException("P05 surface asset magic is invalid.");
            }

            if (reader.ReadUInt16() != SchemaVersion)
            {
                throw new InvalidDataException("P05 surface asset schema is unsupported.");
            }

            byte[] storedHash = reader.ReadBytes(AssetHash.ByteLength);
            using SHA256 sha256 = SHA256.Create();
            byte[] computedHash = sha256.ComputeHash(bytes, HeaderLength, bytes.Length - HeaderLength);
            if (!FixedTimeEquals(storedHash, computedHash))
            {
                throw new InvalidDataException("P05 surface asset hash does not match its payload.");
            }

            SurfaceRepresentation representation = (SurfaceRepresentation)reader.ReadByte();
            Bounds3F bounds = new(ReadFloat3(reader), ReadFloat3(reader));
            int vertexCount = ReadCount(reader, MaximumVertexCount, "vertex");
            int indexCount = ReadCount(reader, MaximumIndexCount, "index");
            int uvCount = ReadCount(reader, MaximumVertexCount, "UV", true);
            if (indexCount % 3 != 0 || (uvCount != 0 && uvCount != vertexCount))
            {
                throw new InvalidDataException("P05 surface buffer counts are inconsistent.");
            }

            var positions = new Float3[vertexCount];
            var normals = new Float3[vertexCount];
            var indices = new uint[indexCount];
            var uvs = new Float2[uvCount];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = ReadFloat3(reader);
            }

            for (int index = 0; index < normals.Length; index++)
            {
                normals[index] = ReadFloat3(reader);
            }

            for (int index = 0; index < indices.Length; index++)
            {
                indices[index] = reader.ReadUInt32();
            }

            for (int index = 0; index < uvs.Length; index++)
            {
                uvs[index] = new Float2(reader.ReadSingle(), reader.ReadSingle());
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException("P05 surface asset contains trailing data.");
            }

            return new SurfaceAsset(AssetHash.FromBytes(computedHash), representation, CoordinateSpace.DesktopUnityMillimetersV1, bounds, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Float3>.TakeOwnership(normals), RenderBuffer<uint>.TakeOwnership(indices), RenderBuffer<Float2>.TakeOwnership(uvs));
        }

        private static int ReadCount(BinaryReader reader, int maximum, string label, bool allowZero = false)
        {
            int value = reader.ReadInt32();
            if (value < (allowZero ? 0 : 1) || value > maximum)
            {
                throw new InvalidDataException($"P05 {label} count is outside the supported range.");
            }

            return value;
        }

        private static Float3 ReadFloat3(BinaryReader reader)
        {
            return new Float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int index = 0; index < left.Length; index++)
            {
                difference |= left[index] ^ right[index];
            }

            return difference == 0;
        }
    }
}
