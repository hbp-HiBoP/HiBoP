using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HBP.Transfer.Codecs
{
    /// <summary>Pinned native codecs; no change to scientific native libraries.</summary>
    public static class TransferCodec
    {
        public const int BlockBytes = 262144;
        public const int MaximumBlockBytes = 1048576;
        public const byte Zstd = 3;

        public static SHA256 CreateHash() => HBP.Core.Tools.Sha256.Create();

        public static int Encode(byte codec, int level, byte[] input, int count, byte[] output)
        {
            if (input == null || count < 1 || count > input.Length || count > MaximumBlockBytes || output == null || output.Length < count || codec != 2 && codec != 3) throw new ArgumentException("Invalid compression block.");
            int result = Native.hbp_codec_encode(codec, level, input, count, output, output.Length);
            if (result < 1 || result > output.Length) throw new InvalidDataException("Block compression failed.");
            return result;
        }

        public static void Decode(byte codec, byte[] input, int count, byte[] output, int expected)
        {
            if (input == null || count < 1 || count > input.Length || count > MaximumBlockBytes || output == null || expected < 1 || expected > output.Length || expected > MaximumBlockBytes) throw new InvalidDataException("Invalid block dimensions.");
            if (codec == 0)
            {
                if (count != expected) throw new InvalidDataException("Invalid raw block.");
                Buffer.BlockCopy(input, 0, output, 0, count);
            }
            else if ((codec != 2 && codec != 3) || count >= expected || Native.hbp_codec_decode(codec, input, count, output, expected) != expected) throw new InvalidDataException("Invalid compressed block.");
        }

        private static class Native
        {
            private const string Library = "hbp_core";

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbp_codec_encode(int codec, int level, byte[] input, int size, byte[] output, int capacity);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbp_codec_decode(int codec, byte[] input, int size, byte[] output, int expected);
        }
    }
}
