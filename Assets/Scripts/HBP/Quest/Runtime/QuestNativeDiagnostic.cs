#if DEVELOPMENT_BUILD && UNITY_ANDROID && ENABLE_IL2CPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AOT;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using UnityEngine;
using UnityEngine.Profiling;
using Plane = HBP.Core.DLL.Plane;

namespace HBP.Quest
{
    /// <summary>One-shot, opt-in native lifecycle probe. No projection or scientific parity claim.</summary>
    public static class QuestNativeDiagnostic
    {
        private const int CycleCount = 100;
        private const string CallbackMessage = "QUEST016 callback épreuve";
        private static readonly DLLDebugManager.LoggerDelegate s_Callback = OnNativeLog;
        private static int s_CallbackCount;
        private static int s_ErrorCallbackCount;

        [Serializable]
        private sealed class MemorySample
        {
            public int completedCycles;
            public long nativeHeapBytes;
            public long unityAllocatedBytes;
            public long managedBytes;
        }

        [Serializable]
        private sealed class Report
        {
            public string runId;
            public string startedUtc = DateTime.UtcNow.ToString("o");
            public string finishedUtc;
            public string unity = Application.unityVersion;
            public string backend = "IL2CPP";
            public string device = SystemInfo.deviceModel;
            public string os = SystemInfo.operatingSystem;
            public string nativeVersion;
            public string fixtureSha256;
            public string controlledError;
            public int requestedCycles = CycleCount;
            public int completedCycles;
            public int releasedHandles;
            public int callbackChecks;
            public int nativeErrorChecks;
            public bool passed;
            public string failure;
            public string[] nativeMappings;
            public List<MemorySample> memory = new();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string marker = Path.Combine(Application.persistentDataPath, "quest016-native-probe");
            if (!File.Exists(marker)) return;
            string runId = File.ReadAllText(marker).Trim();
            File.Delete(marker);
            if (!Guid.TryParseExact(runId, "N", out _))
            {
                Debug.LogError("QUEST016_PROBE_FAIL invalid run ID");
                return;
            }
            _ = RunAsync(runId); // The diagnostic catches and persists its own failures.
        }

        private static async Task RunAsync(string runId)
        {
            var report = new Report { runId = runId };
            string directory = Path.Combine(Application.persistentDataPath, "quest016");
            try
            {
                Directory.CreateDirectory(directory);
                Require(IntPtr.Size == 8, "64-bit process required");
                string fixture = Path.Combine(directory, "synthetic-32x24x16.nii");
                WriteFixture(fixture);
                using (var sha = SHA256.Create())
                    report.fixtureSha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(fixture))).Replace("-", "").ToLowerInvariant();
                report.nativeVersion = HbpCoreRuntime.Version;
                Require(!string.IsNullOrEmpty(report.nativeVersion), "Native version missing");
                Debug.Log($"QUEST016_START runId={runId}; native={report.nativeVersion}; cycles={CycleCount}");
                await Task.Delay(2000, Application.exitCancellationToken);
                Sample(report);
                for (int cycle = 0; cycle < CycleCount; cycle++)
                {
                    RunCycle(fixture, report);
                    report.completedCycles++;
                    if (report.completedCycles % 10 == 0)
                    {
                        // Yield to the PlayerLoop so deferred Mesh destruction and XR can progress.
                        await Task.Delay(1000, Application.exitCancellationToken);
                        Sample(report);
                    }
                }
                report.nativeMappings = File.ReadLines("/proc/self/maps")
                    .Where(line => line.Contains("libhbp_core.so") || line.Contains("libil2cpp.so"))
                    .Select(line => line.Substring(line.IndexOf('/'))).Distinct().ToArray();
                Require(report.nativeMappings.Any(line => line.Contains("libhbp_core.so")), "No mapped hbp_core Android library");
                Require(report.nativeMappings.Any(line => line.Contains("libil2cpp.so")), "No mapped IL2CPP library");
                report.passed = true;
            }
            catch (Exception exception)
            {
                report.failure = exception.ToString();
            }
            finally
            {
                report.finishedUtc = DateTime.UtcNow.ToString("o");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "result.json"), JsonUtility.ToJson(report, true));
                }
                catch (Exception exception)
                {
                    report.passed = false;
                    report.failure += " Result write failed: " + exception.Message;
                }
                if (report.passed) Debug.Log($"QUEST016_PROBE_PASS runId={runId}; cycles={report.completedCycles}; handles={report.releasedHandles}");
                else Debug.LogError($"QUEST016_PROBE_FAIL runId={runId}; {report.failure}");
            }
        }

        private static void RunCycle(string fixture, Report report)
        {
            Require(HbpCoreRuntime.Init() == HbpCoreStatus.Ok, "Native init");
            try
            {
                s_CallbackCount = 0;
                s_ErrorCallbackCount = 0;
                Require(HbpCoreRuntime.SetDebugCallback(s_Callback) == HbpCoreStatus.Ok, "Register AOT callback");
                Require(HbpCoreRuntime.DebugMessage(CallbackMessage, HbpCoreLogType.Info) == HbpCoreStatus.Ok && s_CallbackCount == 1, "UTF-8 reverse P/Invoke");
                report.callbackChecks++;
                Require(HbpCoreRuntime.SetLogFile("") == HbpCoreStatus.InvalidArgument, "Controlled native error status");
                report.controlledError = HbpCoreRuntime.LastError;
                Require(report.controlledError == "hbp_core_set_log_file received an empty path" && s_ErrorCallbackCount == 1, "Native error text and callback");
                report.nativeErrorChecks++;

                using (var volume = new Volume())
                using (var surface = new Surface())
                using (var sites = new RawSiteList())
                using (var plane = new Plane(Vector3.zero, Vector3.forward))
                {
                    Require(volume.LoadNIFTIFile(fixture), "Load synthetic NIfTI after error");
                    Require(volume.IsLoaded && volume.Dimensions == new Vector3Int(32, 24, 16), "Volume dimensions");
                    Require(volume.Spacing == Vector3.one, "Volume spacing");
                    var extrema = volume.ExtremeValues;
                    Require(Math.Abs(extrema.Min) < 0.00001f && Math.Abs(extrema.Max - 255) < 0.00001f, "Volume extrema");

                    var vertices = new[] { new Vector3(-1, 2, 3), new Vector3(4, 2, 3), new Vector3(-1, 7, 3) };
                    int[] triangles = { 0, 1, 2 };
                    surface.SetBuffers(vertices, triangles);
                    surface.ComputeNormals();
                    using (var clone = new Surface(surface))
                    {
                        var mesh = new Mesh();
                        try
                        {
                            clone.UpdateMeshFromDLL(mesh);
                            Require(clone.NumberOfVertices == 3 && clone.NumberOfTriangles == 1, "Surface clone sizes");
                            Require(mesh.triangles.SequenceEqual(triangles), "Surface winding round trip");
                            Vector3[] copied = mesh.vertices;
                            Require(copied.Length == vertices.Length, "Surface vertex copy size");
                            for (int i = 0; i < vertices.Length; i++) Require(Vector3.Distance(copied[i], vertices[i]) < 0.00001f, "Surface vertex round trip");
                            Require(mesh.normals.Length == 3 && mesh.normals.All(normal => Vector3.Distance(normal, Vector3.forward) < 0.00001f), "Generated surface normals");
                        }
                        finally { UnityEngine.Object.Destroy(mesh); }
                        Release(clone, report);
                    }

                    sites.AddSite("site-é", new Vector3(1, 2, 0), 0, 0); // Explicit native coordinates.
                    sites.AddSite("site-2", new Vector3(4, 5, 6), 0, 1);
                    Require(sites.NumberOfSites == 2, "Raw site count");
                    using (var clone = new RawSiteList(sites))
                    {
                        clone.GetSitesOnPlane(plane, 0.01f, out int[] onPlane);
                        Require(clone.NumberOfSites == 2 && onPlane.SequenceEqual(new[] { 1, 0 }), "Raw site clone and buffer copy");
                        clone.UpdateMask(0, true);
                        Release(clone, report);
                    }
                    Release(plane, report);
                    Release(sites, report);
                    Release(surface, report);
                    Release(volume, report);
                }
            }
            finally
            {
                // All owned wrappers have left their using scopes before the runtime is shut down.
                try { Require(HbpCoreRuntime.ResetDebugCallback() == HbpCoreStatus.Ok, "Reset callback"); }
                finally { Require(HbpCoreRuntime.Shutdown() == HbpCoreStatus.Ok, "Native shutdown"); }
            }
        }

        private static void Release(CppDLLImportBase resource, Report report)
        {
            Require(resource.getHandle().Handle != IntPtr.Zero, "Expected live native handle");
            resource.Dispose();
            Require(resource.getHandle().Handle == IntPtr.Zero, "Dispose clears handle");
            resource.Dispose(); // Idempotent disposal must not call native destroy twice.
            report.releasedHandles++;
        }

        [MonoPInvokeCallback(typeof(DLLDebugManager.LoggerDelegate))]
        private static void OnNativeLog([MarshalAs(UnmanagedType.LPUTF8Str)] string message, int type)
        {
            if (type == (int)HbpCoreLogType.Info && message == CallbackMessage) s_CallbackCount++;
            if (type == (int)HbpCoreLogType.Error) s_ErrorCallbackCount++;
        }

        private static void Sample(Report report)
        {
            using var androidDebug = new AndroidJavaClass("android.os.Debug");
            var sample = new MemorySample
            {
                completedCycles = report.completedCycles,
                nativeHeapBytes = androidDebug.CallStatic<long>("getNativeHeapAllocatedSize"),
                unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                managedBytes = GC.GetTotalMemory(false)
            };
            report.memory.Add(sample);
            Debug.Log("QUEST016_MEMORY " + JsonUtility.ToJson(sample));
        }

        private static void WriteFixture(string path)
        {
            // Deterministic NIfTI-1 float32 fixture, identity affine, unit spacing, values 0..255.
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);
            writer.Write(new byte[352]);
            stream.Position = 0; writer.Write(348);
            stream.Position = 40;
            foreach (short value in new short[] { 3, 32, 24, 16, 1, 1, 1, 1 }) writer.Write(value);
            stream.Position = 70; writer.Write((short)16); writer.Write((short)32);
            stream.Position = 76;
            for (int i = 0; i < 8; i++) writer.Write(1.0f);
            stream.Position = 108; writer.Write(352.0f); writer.Write(1.0f);
            stream.Position = 124; writer.Write(255.0f); writer.Write(0.0f);
            stream.Position = 252; writer.Write((short)1); writer.Write((short)1);
            foreach (int offset in new[] { 280, 300, 320 }) { stream.Position = offset; writer.Write(1.0f); }
            stream.Position = 344; writer.Write(new byte[] { (byte)'n', (byte)'+', (byte)'1', 0 });
            stream.Position = 352;
            for (int i = 0; i < 32 * 24 * 16; i++) writer.Write((float)(i % 256));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
