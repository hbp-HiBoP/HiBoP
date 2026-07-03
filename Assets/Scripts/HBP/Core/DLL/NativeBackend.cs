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
}
