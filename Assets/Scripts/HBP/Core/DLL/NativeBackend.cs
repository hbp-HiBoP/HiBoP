namespace HBP.Core.DLL
{
    public enum NativeBackend
    {
        HbpExport,
        HbpCore
    }

    public static class NativeDll
    {
        public const string HbpExport = "hbp_export";
        public const string HbpCore = "hbp_core";
    }

    public static class NativeBackendOptions
    {
        public static NativeBackend ExperimentalBackend { get; set; } = NativeBackend.HbpExport;

        public static bool UsesHbpCore => ExperimentalBackend == NativeBackend.HbpCore;

        public static void Reset()
        {
            ExperimentalBackend = NativeBackend.HbpExport;
        }
    }
}
