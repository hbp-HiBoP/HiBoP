namespace HBP.Core.DLL
{
    internal enum NativeBackend
    {
        HbpExport,
        HbpCore
    }

    internal static class NativeDll
    {
        internal const string HbpExport = "hbp_export";
        internal const string HbpCore = "hbp_core";
    }

    internal static class NativeBackendOptions
    {
        internal static NativeBackend ExperimentalBackend { get; set; } = NativeBackend.HbpCore;

        internal static bool UsesHbpCore => ExperimentalBackend == NativeBackend.HbpCore;

        internal static void Reset()
        {
            ExperimentalBackend = NativeBackend.HbpCore;
        }
    }
}
