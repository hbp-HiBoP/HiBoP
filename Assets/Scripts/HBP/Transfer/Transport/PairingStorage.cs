using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace HBP.Transfer.Transport
{
    /// <summary>Atomic, user-bound DPAPI files on Windows. Android callers must use
    /// Context.getNoBackupFilesDir(), never shared/external storage.</summary>
    public static class PairingStorage
    {
        public static byte[] Read(string path) => File.Exists(path) ? Protect(File.ReadAllBytes(path), false) : null;

        public static void Write(string path, byte[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            byte[] stored = Protect(data, true);
            string temp = path + ".tmp";
            try
            {
                File.WriteAllBytes(temp, stored);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally
            {
                if (!ReferenceEquals(stored, data)) Array.Clear(stored, 0, stored.Length);
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        private static byte[] Protect(byte[] value, bool encrypt)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return value;
            var input = new Blob { Size = value.Length, Data = Marshal.AllocHGlobal(value.Length) };
            Blob output = default;
            try
            {
                Marshal.Copy(value, 0, input.Data, value.Length);
                bool ok = encrypt ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output) : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
                if (!ok) throw new Win32Exception(Marshal.GetLastWin32Error());
                var result = new byte[output.Size];
                Marshal.Copy(output.Data, result, 0, result.Length);
                return result;
            }
            finally
            {
                for (int i = 0; i < input.Size; i++) Marshal.WriteByte(input.Data, i, 0);
                Marshal.FreeHGlobal(input.Data);
                if (output.Data != IntPtr.Zero)
                {
                    for (int i = 0; i < output.Size; i++) Marshal.WriteByte(output.Data, i, 0);
                    LocalFree(output.Data);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Blob
        {
            public int Size;
            public IntPtr Data;
        }

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(ref Blob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);

        [DllImport("crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr value);
    }
}
