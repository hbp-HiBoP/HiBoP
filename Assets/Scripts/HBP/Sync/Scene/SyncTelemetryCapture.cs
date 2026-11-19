using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HBP.Sync.Scene
{
    /// <summary>Opt-in process recording. Export is explicit/on lifecycle events, never per frame.</summary>
    public static class SyncTelemetryCapture
    {
        private const int MaximumSamples = 8192;
        private static BoundedSyncTelemetrySink s_Sink;
        private static ISyncTelemetryCapture s_Capture;
        private static string s_RecordingId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Also handles an Editor configured without domain reload.
            FlushAndStop("reinitialize");
            string marker = Path.Combine(Application.persistentDataPath, "sync-baseline.enabled");
            if (!Environment.GetCommandLineArgs().Contains("-syncBaseline", StringComparer.OrdinalIgnoreCase) && !File.Exists(marker))
                return;
            try
            {
                var sink = new BoundedSyncTelemetrySink(MaximumSamples);
                s_Capture = SyncTelemetry.BeginCapture(sink);
                s_Sink = sink;
                s_RecordingId = Guid.NewGuid().ToString("N");
                Application.quitting += OnQuitting;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
                Debug.Log($"HBP_SYNC_BASELINE started recording={s_RecordingId} directory={Path.Combine(Application.persistentDataPath, "SyncBaselines")}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to start sync baseline telemetry: " + exception.Message);
            }
        }

        private static void OnQuitting() => FlushAndStop("application-quitting");

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                FlushAndStop("editor-exiting-play-mode");
        }

        [UnityEditor.MenuItem("Tools/HiBoP/Sync Baseline/Enable for next Play")]
        private static void EnableEditorRecording()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "sync-baseline.enabled"), string.Empty);
                Debug.Log("Sync baseline enabled for the next Play session. Start recording before sending a scene.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to enable sync baseline: " + exception.Message);
            }
        }

        [UnityEditor.MenuItem("Tools/HiBoP/Sync Baseline/Disable for next Play")]
        private static void DisableEditorRecording()
        {
            FlushAndStop("editor-disable");
            try
            {
                File.Delete(Path.Combine(Application.persistentDataPath, "sync-baseline.enabled"));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to remove sync baseline marker: " + exception.Message);
            }
        }

        [UnityEditor.MenuItem("Tools/HiBoP/Sync Baseline/Export checkpoint")]
        private static void ExportEditorCheckpoint() => ExportCheckpoint("editor-menu");

        [UnityEditor.MenuItem("Tools/HiBoP/Sync Baseline/Stop and export")]
        private static void StopEditorCapture() => FlushAndStop("editor-menu");
#endif

        /// <summary>Call on the Unity thread. A checkpoint is a prefix, not proof that in-flight work drained.</summary>
        public static void ExportCheckpoint(string reason = "manual")
        {
            if (s_Sink == null) return;
            WriteFile(s_Sink, "checkpoint", reason);
        }

        /// <summary>Call on the Unity thread after measured operations settle. Restart the application to re-enable.</summary>
        public static void FlushAndStop(string reason = "manual")
        {
            Application.quitting -= OnQuitting;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
            s_Capture?.Dispose();
            s_Capture = null;
            BoundedSyncTelemetrySink sink = s_Sink;
            s_Sink = null;
            if (sink != null) WriteFile(sink, "closed", reason);
        }

        private static void WriteFile(BoundedSyncTelemetrySink sink, string state, string reason)
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "SyncBaselines");
                Directory.CreateDirectory(directory);
                string file = Path.Combine(directory, $"sync-{Application.platform}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.csv");
                File.WriteAllText(file, ToCsv(sink, state, reason), new UTF8Encoding(false));
                Debug.Log($"HBP_SYNC_BASELINE file={file} recording={s_RecordingId} state={state} samples={sink.Count} dropped={sink.Dropped}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to write sync baseline telemetry: " + exception.Message);
            }
        }

        private static string ToCsv(BoundedSyncTelemetrySink sink, string state, string reason)
        {
            // Admission is stopped for a closed export. An active checkpoint deliberately copies a fixed prefix.
            int count = sink.Count;
            var text = new StringBuilder(count * 144);
            text.AppendLine("profile,milestone,scopeId,logicalTraceId,logicalTraceIdKind,captureGeneration,captureGenerationKind,attemptId,attemptIdKind,timestamp,frequency,allocatedBytes,thread,isMainThread,payloadBytes,recordingGeneration");
            for (int i = 0; i < count; i++)
            {
                if (!sink.TryGet(i, out SyncTelemetrySample sample)) break;
                text.Append(sample.Profile).Append(',').Append(sample.Milestone).Append(',').Append(Escape(sample.Identity.ScopeId)).Append(',').Append(Number(sample.Identity.LogicalTraceId)).Append(',').Append(sample.Identity.LogicalTraceIdKind).Append(',').Append(Number(sample.Identity.CaptureGeneration)).Append(',').Append(sample.Identity.CaptureGenerationKind).Append(',').Append(Number(sample.Identity.AttemptId)).Append(',').Append(sample.Identity.AttemptIdKind).Append(',').Append(Number(sample.Timestamp)).Append(',').Append(Number(sample.ClockFrequency)).Append(',').Append(Number(sample.AllocatedBytes)).Append(',').Append(Number(sample.ManagedThreadId)).Append(',').Append(sample.IsMainThread ? "true" : "false").Append(',').Append(Number(sample.PayloadBytes)).Append(',').Append(Number(sample.RecordingGeneration)).AppendLine();
            }

            text.Append("schemaVersion,2").AppendLine();
            text.Append("recordingId,").Append(Escape(s_RecordingId)).AppendLine();
            text.Append("captureState,").Append(state).AppendLine();
            text.Append("captureReason,").Append(Escape(reason)).AppendLine();
            text.Append("sampleCount,").Append(Number(count)).AppendLine();
            text.Append("dropped,").Append(Number(sink.Dropped)).AppendLine();
            text.Append("unityVersion,").Append(Escape(Application.unityVersion)).AppendLine();
            text.Append("buildGuid,").Append(Escape(Application.buildGUID)).AppendLine();
            text.Append("platform,").Append(Application.platform).AppendLine();
            return text.ToString();
        }

        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Escape(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    }
}
