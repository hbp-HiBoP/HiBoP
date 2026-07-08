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
        public static NativeBackend ExperimentalBackend { get; set; } = NativeBackend.HbpCore;

        public static bool UsesHbpCore => ExperimentalBackend == NativeBackend.HbpCore;

        public static bool TrySetExperimentalBackend(string backend)
        {
            if (string.IsNullOrWhiteSpace(backend))
            {
                return false;
            }

            switch (backend.Trim().ToLowerInvariant())
            {
                case "hbp_export":
                case "hbp-export":
                case "export":
                    ExperimentalBackend = NativeBackend.HbpExport;
                    return true;
                case "hbp_core":
                case "hbp-core":
                case "core":
                    ExperimentalBackend = NativeBackend.HbpCore;
                    return true;
                default:
                    return false;
            }
        }

        public static void Reset()
        {
            ExperimentalBackend = NativeBackend.HbpExport;
        }
    }
}
