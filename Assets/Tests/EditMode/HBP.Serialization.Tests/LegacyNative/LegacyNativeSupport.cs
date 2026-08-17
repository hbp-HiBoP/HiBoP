namespace HBP.Tests.Serialization
{
    /// <summary>Backend selected by parity tests and comparative benchmark CLIs.</summary>
    internal enum BenchmarkBackend
    {
        HbpExport,
        HbpCore
    }

    internal static class OracleBackendContext
    {
        internal static BenchmarkBackend Current { get; set; } = BenchmarkBackend.HbpCore;
        internal static bool UsesHbpCore => Current == BenchmarkBackend.HbpCore;

        internal static void Reset()
        {
            Current = BenchmarkBackend.HbpCore;
        }
    }

    internal static class LegacyNativeLibrary
    {
        internal const string HbpCore = "hbp_core";
        internal const string HbpExport = "hbp_export";
    }
}
