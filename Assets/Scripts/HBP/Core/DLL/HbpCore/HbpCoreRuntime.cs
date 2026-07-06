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

    public enum HbpCoreLogType
    {
        Info = 0,
        Warning = 1,
        Error = 2
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

        public static HbpCoreStatus SetDebugCallback(DLLDebugManager.LoggerDelegate callback)
        {
            return hbp_core_set_debug_callback(callback);
        }

        public static HbpCoreStatus ResetDebugCallback()
        {
            return hbp_core_reset_debug_callback();
        }

        public static HbpCoreStatus DebugMessage(string message, HbpCoreLogType type)
        {
            return hbp_core_debug_message(message, type);
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

        public static bool TrySetDebugCallback(DLLDebugManager.LoggerDelegate callback, out string error)
        {
            try
            {
                HbpCoreStatus status = SetDebugCallback(callback);
                error = status == HbpCoreStatus.Ok ? string.Empty : LastError;
                return status == HbpCoreStatus.Ok;
            }
            catch (Exception exception) when (IsNativeLoadException(exception))
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryResetDebugCallback(out string error)
        {
            try
            {
                HbpCoreStatus status = ResetDebugCallback();
                error = status == HbpCoreStatus.Ok ? string.Empty : LastError;
                return status == HbpCoreStatus.Ok;
            }
            catch (Exception exception) when (IsNativeLoadException(exception))
            {
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

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_core_set_debug_callback", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_core_set_debug_callback(DLLDebugManager.LoggerDelegate callback);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_core_reset_debug_callback", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_core_reset_debug_callback();

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_core_debug_message", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_core_debug_message([MarshalAs(UnmanagedType.LPUTF8Str)] string message, HbpCoreLogType type);
    }
}
