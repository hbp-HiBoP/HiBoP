using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using HBP.Core.DLL;
using Microsoft.Win32.SafeHandles;

namespace HBP.Core.Tools
{
    /// <summary>SHA-256 backed by hbp_core, shared by scientific data and application services.</summary>
    public static class Sha256
    {
        private const int StreamBufferBytes = 262144;

        public static SHA256 Create() => new NativeHash();

        public static byte[] ComputeHash(byte[] bytes)
        {
            using var sha = Create();
            return sha.ComputeHash(bytes);
        }

        /// <summary>Hashes from the current position to EOF without closing or rewinding the stream.</summary>
        public static byte[] ComputeHash(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using var sha = Create();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(StreamBufferBytes);
            try
            {
                int count;
                while ((count = stream.Read(buffer, 0, StreamBufferBytes)) != 0)
                    sha.TransformBlock(buffer, 0, count, null, 0);
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return sha.Hash;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        public static string HashFile(string path)
        {
            using var input = File.OpenRead(path);
            return BitConverter.ToString(ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }

        private sealed class DigestHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal DigestHandle() : base(true)
            {
                SetHandle(Native.hbp_sha256_create());
                if (IsInvalid) throw new CryptographicException("Native SHA-256 initialization failed.");
            }

            protected override bool ReleaseHandle()
            {
                Native.hbp_sha256_destroy(handle);
                return true;
            }
        }

        private sealed class NativeHash : SHA256
        {
            private readonly DigestHandle context = new();

            public override void Initialize()
            {
                if (Native.hbp_sha256_reset(context) != 0) throw new CryptographicException("Native SHA-256 reset failed.");
            }

            protected override void HashCore(byte[] array, int offset, int count)
            {
                if (array == null || offset < 0 || count < 0 || offset > array.Length - count) throw new ArgumentOutOfRangeException();
                if (Native.hbp_sha256_update(context, array, offset, count) != 0) throw new CryptographicException("Native SHA-256 update failed.");
            }

            protected override byte[] HashFinal()
            {
                var result = new byte[32];
                if (Native.hbp_sha256_finish(context, result) != 0) throw new CryptographicException("Native SHA-256 finalization failed.");
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
            [DllImport(HbpCoreLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr hbp_sha256_create();

            [DllImport(HbpCoreLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbp_sha256_reset(DigestHandle context);

            [DllImport(HbpCoreLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbp_sha256_update(DigestHandle context, byte[] bytes, int offset, int count);

            [DllImport(HbpCoreLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hbp_sha256_finish(DigestHandle context, byte[] output);

            [DllImport(HbpCoreLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void hbp_sha256_destroy(IntPtr context);
        }
    }
}
