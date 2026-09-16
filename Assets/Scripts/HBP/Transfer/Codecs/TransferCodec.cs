using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace HBP.Transfer.Codecs
{
    /// <summary>Pinned native codecs; no change to scientific native libraries.</summary>
    public static class TransferCodec
    {
        public const int BlockBytes = 262144;
        public const int MaximumBlockBytes = 1048576;
        public const byte Zstd = 3;

        public static SHA256 CreateHash()
        {
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
            return new NativeHash();
#else
            return SHA256.Create();
#endif
        }

        public static int Encode(byte codec, int level, byte[] input, int count, byte[] output)
        {
            if (input == null || count < 1 || count > input.Length || count > MaximumBlockBytes || output == null || output.Length < count || codec != 2 && codec != 3) throw new ArgumentException("Invalid compression block.");
            int result = Native.hbt_encode(codec, level, input, count, output, output.Length);
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
            else if ((codec != 2 && codec != 3) || count >= expected || Native.hbt_decode(codec, input, count, output, expected) != expected) throw new InvalidDataException("Invalid compressed block.");
        }

        private sealed class DigestHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal DigestHandle() : base(true)
            {
                SetHandle(Native.hbt_sha_create());
                if (IsInvalid) throw new CryptographicException("Native SHA-256 initialization failed.");
            }

            protected override bool ReleaseHandle()
            {
                Native.hbt_sha_destroy(handle);
                return true;
            }
        }

        private sealed class NativeHash : SHA256
        {
            private readonly DigestHandle context = new();

            public override void Initialize()
            {
                if (Native.hbt_sha_reset(context) != 0) throw new CryptographicException();
            }

            protected override void HashCore(byte[] array, int offset, int count)
            {
                if (array == null || offset < 0 || count < 0 || offset > array.Length - count) throw new ArgumentOutOfRangeException();
                if (Native.hbt_sha_update(context, array, offset, count) != 0) throw new CryptographicException();
            }

            protected override byte[] HashFinal()
            {
                var result = new byte[32];
                if (Native.hbt_sha_finish(context, result) != 0) throw new CryptographicException();
                return result;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) context.Dispose();
                base.Dispose(disposing);
            }
        }

        private static class Native
        {
            private const string Library = "hbp_core";

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbt_encode(int codec, int level, byte[] input, int size, byte[] output, int capacity);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbt_decode(int codec, byte[] input, int size, byte[] output, int expected);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr hbt_sha_create();

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbt_sha_reset(DigestHandle context);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbt_sha_update(DigestHandle context, byte[] bytes, int offset, int count);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbt_sha_finish(DigestHandle context, byte[] output);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void hbt_sha_destroy(IntPtr context);
        }
    }
}
