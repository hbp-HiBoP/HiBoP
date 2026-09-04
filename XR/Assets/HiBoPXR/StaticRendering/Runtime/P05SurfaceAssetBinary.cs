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

            int payloadLength = checked(bytes.Length - HeaderLength);
            using var payload = new MemoryStream(bytes, HeaderLength, payloadLength, false);
            return SurfaceAssetPayloadCodec.Decode(payload, AssetHash.FromBytes(computedHash), payloadLength);
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
