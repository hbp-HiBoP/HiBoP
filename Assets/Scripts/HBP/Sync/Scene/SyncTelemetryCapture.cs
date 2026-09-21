using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HBP.Sync.Scene
{
    internal static class SyncTelemetryCapture
    {
        private const int MaximumSamples = 8192;
        private static BoundedSyncTelemetrySink s_Sink;
        private static IDisposable s_Capture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            string marker = Path.Combine(Application.persistentDataPath, "sync-baseline.enabled");
            if (!Environment.GetCommandLineArgs().Contains("-syncBaseline", StringComparer.OrdinalIgnoreCase) && !File.Exists(marker))
                return;
            s_Sink = new BoundedSyncTelemetrySink(MaximumSamples);
            s_Capture = SyncTelemetry.BeginCapture(s_Sink);
            Application.quitting += Flush;
        }

        private static void Flush()
        {
            Application.quitting -= Flush;
            s_Capture?.Dispose();
            s_Capture = null;
            if (s_Sink == null)
                return;
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "SyncBaselines");
                Directory.CreateDirectory(directory);
                string file = Path.Combine(directory, $"sync-{Application.platform}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
                File.WriteAllText(file, ToCsv(s_Sink), Encoding.UTF8);
                Debug.Log($"HBP_SYNC_BASELINE file={file} samples={s_Sink.Count} dropped={s_Sink.Dropped}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to write sync baseline telemetry: " + exception.Message);
            }
            finally
            {
                s_Sink = null;
            }
        }

        private static string ToCsv(BoundedSyncTelemetrySink sink)
        {
            var text = new StringBuilder(sink.Count * 112);
            text.AppendLine("profile,milestone,scopeId,logicalTraceId,logicalTraceIdKind,captureGeneration,captureGenerationKind,attemptId,attemptIdKind,timestamp,frequency,allocatedBytes,thread,isMainThread,payloadBytes");
            for (int i = 0; i < sink.Count; i++)
            {
                if (!sink.TryGet(i, out SyncTelemetrySample sample)) break;
                text.Append(sample.Profile).Append(',').Append(sample.Milestone).Append(',').Append(Escape(sample.Identity.ScopeId)).Append(',').Append(sample.Identity.LogicalTraceId.ToString(CultureInfo.InvariantCulture)).Append(',').Append(sample.Identity.LogicalTraceIdKind).Append(',').Append(sample.Identity.CaptureGeneration.ToString(CultureInfo.InvariantCulture)).Append(',').Append(sample.Identity.CaptureGenerationKind).Append(',').Append(sample.Identity.AttemptId.ToString(CultureInfo.InvariantCulture)).Append(',').Append(sample.Identity.AttemptIdKind).Append(',').Append(sample.Timestamp.ToString(CultureInfo.InvariantCulture)).Append(',').Append(sample.ClockFrequency.ToString(CultureInfo.InvariantCulture)).Append(',').Append(sample.AllocatedBytes.ToString(CultureInfo.InvariantCulture)).Append(',').Append(sample.ManagedThreadId.ToString(CultureInfo.InvariantCulture)).Append(',').Append(sample.IsMainThread ? "true" : "false").Append(',').Append(sample.PayloadBytes.ToString(CultureInfo.InvariantCulture)).AppendLine();
            }

            text.Append("dropped,").Append(sink.Dropped.ToString(CultureInfo.InvariantCulture)).AppendLine();
            return text.ToString();
        }

        private static string Escape(string value)
        {
            string quote = ((char)34).ToString();
            return quote + (value ?? string.Empty).Replace(quote, quote + quote) + quote;
        }
    }
}
