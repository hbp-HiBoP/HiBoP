using System;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL.HbpCore
{
    public enum HbpCoreStatus
    {
        Ok = 0,
        Error = 1,
        InvalidArgument = 2,
        InvalidHandle = 3,
        BufferTooSmall = 4
    }

    public static class HbpCoreRuntime
    {
        public static string Version => MarshalString(hbp_core_version());

        public static string LastError => MarshalString(hbp_core_last_error());

        public static HbpCoreStatus Init()
        {
            return hbp_core_init();
        }

        public static HbpCoreStatus Shutdown()
        {
            return hbp_core_shutdown();
        }

        public static bool TryGetVersion(out string version, out string error)
        {
            try
            {
                version = Version;
                error = string.Empty;
                return true;
            }
            catch (Exception exception) when (IsNativeLoadException(exception))
            {
                version = string.Empty;
                error = exception.Message;
                return false;
            }
        }

        private static string MarshalString(IntPtr value)
        {
            return Marshal.PtrToStringAnsi(value) ?? string.Empty;
        }

        private static bool IsNativeLoadException(Exception exception)
        {
            return exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;
        }

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_core_version", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr hbp_core_version();

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_core_last_error", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr hbp_core_last_error();

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_core_init", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_core_init();

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_core_shutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_core_shutdown();
    }
}
