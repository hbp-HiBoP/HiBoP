using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.UI.Quest
{
    /// <summary>Bounded ADB client execution. System.Diagnostics.Process.Start is unavailable in Windows IL2CPP.</summary>
    internal static class QuestAdbProcess
    {
        public static Task<string> RunAsync(string executable, string arguments, CancellationToken stop) =>
            Task.Run(() => Run(executable, arguments, stop), stop);

        private static string Run(string executable, string arguments, CancellationToken stop)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            stop.ThrowIfCancellationRequested();
            string outputPath = Path.Combine(Path.GetTempPath(), "hibop-adb-" + Guid.NewGuid().ToString("N") + ".log");
            IntPtr output = IntPtr.Zero, input = IntPtr.Zero, attributes = IntPtr.Zero, handles = IntPtr.Zero;
            ProcessInformation process = default;
            bool initialized = false;
            try
            {
                using (var file = new FileStream(outputPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    IntPtr self = GetCurrentProcess();
                    Check(DuplicateHandle(self, file.SafeFileHandle.DangerousGetHandle(), self, out output, 0, true, 2));
                }

                var security = new SecurityAttributes { Size = Marshal.SizeOf<SecurityAttributes>(), InheritHandle = true };
                input = CreateFileW("NUL", 0x80000000, 3, ref security, 3, 0, IntPtr.Zero);
                Check(input != new IntPtr(-1));
                // Inherit only our stdin/stdout handles, never unrelated Unity handles.
                IntPtr bytes = IntPtr.Zero;
                InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref bytes);
                attributes = Marshal.AllocHGlobal(bytes);
                Check(InitializeProcThreadAttributeList(attributes, 1, 0, ref bytes));
                initialized = true;
                handles = Marshal.AllocHGlobal(IntPtr.Size * 2);
                Marshal.WriteIntPtr(handles, 0, input);
                Marshal.WriteIntPtr(handles, IntPtr.Size, output);
                Check(UpdateProcThreadAttribute(attributes, 0, new IntPtr(0x20002), handles, new IntPtr(IntPtr.Size * 2), IntPtr.Zero, IntPtr.Zero));
                var startup = new StartupInfoEx
                {
                    Startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfoEx>(), Flags = 0x100, Input = input, Output = output, Error = output },
                    Attributes = attributes
                };
                var command = new StringBuilder("\"" + executable + "\" " + arguments);
                Check(CreateProcessW(executable, command, IntPtr.Zero, IntPtr.Zero, true, 0x08080000, IntPtr.Zero,
                    Path.GetDirectoryName(executable), ref startup, out process));
                var elapsed = Stopwatch.StartNew();
                while (true)
                {
                    uint wait = WaitForSingleObject(process.Process, 25);
                    if (wait == 0) break;
                    if (wait != 258) throw new Win32Exception(Marshal.GetLastWin32Error());
                    stop.ThrowIfCancellationRequested();
                    if (elapsed.ElapsedMilliseconds >= 4000) throw new TimeoutException("USB discovery timed out.");
                }

                Check(GetExitCodeProcess(process.Process, out uint exit));
                if (exit != 0) throw new IOException("USB discovery unavailable. Check USB debugging authorization.");
                if (new FileInfo(outputPath).Length > 65536) throw new IOException("Unexpected ADB output size.");
                using var reader = new StreamReader(new FileStream(outputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                return reader.ReadToEnd();
            }
            finally
            {
                if (process.Process != IntPtr.Zero)
                {
                    if (WaitForSingleObject(process.Process, 0) == 258)
                    {
                        TerminateProcess(process.Process, 1); // Only this ADB client; never the server or HiBoP.
                        WaitForSingleObject(process.Process, 1000);
                    }
                    CloseHandle(process.Process);
                }
                if (process.Thread != IntPtr.Zero) CloseHandle(process.Thread);
                if (initialized) DeleteProcThreadAttributeList(attributes);
                if (attributes != IntPtr.Zero) Marshal.FreeHGlobal(attributes);
                if (handles != IntPtr.Zero) Marshal.FreeHGlobal(handles);
                if (input != IntPtr.Zero && input != new IntPtr(-1)) CloseHandle(input);
                if (output != IntPtr.Zero) CloseHandle(output);
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
#else
            throw new PlatformNotSupportedException("USB discovery is available on Windows.");
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static void Check(bool success)
        {
            if (!success) throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SecurityAttributes
        {
            public int Size;
            public IntPtr Descriptor;
            [MarshalAs(UnmanagedType.Bool)] public bool InheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfo
        {
            public int Size;
            public IntPtr Reserved, Desktop, Title;
            public uint X, Y, Width, Height, CharactersX, CharactersY, FillAttribute, Flags;
            public ushort ShowWindow, ReservedSize;
            public IntPtr ReservedBytes, Input, Output, Error;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfoEx { public StartupInfo Startup; public IntPtr Attributes; }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation { public IntPtr Process, Thread; public uint ProcessId, ThreadId; }

        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool DuplicateHandle(IntPtr sourceProcess, IntPtr source, IntPtr targetProcess, out IntPtr target, uint access, bool inherit, uint options);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateFileW(string name, uint access, uint share, ref SecurityAttributes security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, uint flags, ref IntPtr size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returnedSize);
        [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcessW(string application, StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity, bool inherit, uint flags, IntPtr environment, string directory, ref StartupInfoEx startup, out ProcessInformation process);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(IntPtr process, out uint exit);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(IntPtr process, uint exit);
#endif
    }
}
