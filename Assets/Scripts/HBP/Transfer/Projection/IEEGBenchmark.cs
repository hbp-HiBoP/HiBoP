#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBP.Transfer.Anatomy;
using UnityEngine;
using UnityEngine.Profiling;

namespace HBP.Transfer.Projection
{
    /// <summary>Opt-in complete-buffer comparison. Reports differences without choosing a tolerance.</summary>
    public static class IEEGBenchmark
    {
        [Serializable]
        public sealed class Measurement
        {
            public string name, inputSha256, unity, nativeVersion, platform, provenance;
            public int vertices, gridPoints;
            public int[] dimensions, masks, coverage;
            public float[] surfaceValues, siteValues, diameters, colors;
            public bool[] visible;
            public byte[] availability;
            public double preparationMs, computeMs, projectionAndCopyMs;
            public long uvCopyBytes, inputCopyBytes, allocatedBytes, managedBytes;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string fixtures, output;
#if UNITY_ANDROID && !UNITY_EDITOR
            string marker = Path.Combine(Application.persistentDataPath, "quest023-benchmark");
            if (!File.Exists(marker)) return;
            File.Delete(marker);
            fixtures = Path.Combine(Application.persistentDataPath, "quest023-fixtures");
            output = Path.Combine(Application.persistentDataPath, "quest023-benchmark-result");
#else
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-ieegBenchmark");
            if (index < 0 || index + 2 >= args.Length) return;
            fixtures = args[index + 1];
            output = args[index + 2];
#endif
            _ = RunAsync(fixtures, output);
        }

        private static async Task RunAsync(string fixtures, string output)
        {
            Directory.CreateDirectory(output);
            try
            {
                var paths = Directory.GetFiles(fixtures, "*.hbna").OrderBy(p => p).ToArray();
                if (paths.Length == 0) throw new InvalidDataException("No iEEG fixtures.");
                int runs = 0;
                foreach (string path in paths)
                    for (int cycle = 0; cycle < 2; cycle++)
                    {
                        var snapshot = AnatomySnapshotCodec.Decode(File.ReadAllBytes(path));
                        var inputs = NativeProjectionInputs.Create(snapshot, Path.Combine(output, "native"));
                        try
                        {
                            var result = await inputs.ComputeIEEGAsync(snapshot.IEEG, true);
                            Write(output, Path.GetFileNameWithoutExtension(path) + "-" + cycle, snapshot, result);
                        }
                        finally
                        {
                            inputs.Dispose();
                        }

                        if (inputs.CleanupError != null || File.Exists(inputs.VolumePath)) throw new IOException("Native resources not released: " + inputs.CleanupError);
                        runs++;
                    }

                File.WriteAllText(Path.Combine(output, "complete.json"), "{\"runs\":" + runs + "}");
                Debug.Log("QUEST023_BENCHMARK_COMPLETE");
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(output, "failure.txt"), exception.ToString());
                Debug.LogError("QUEST023_BENCHMARK_FAIL " + exception);
            }
        }

        public static void Write(string output, string name, AnatomySnapshot snapshot, NativeProjectionInputs.IEEGResult result)
        {
            Directory.CreateDirectory(output);
            using var sha = System.Security.Cryptography.SHA256.Create();
            var report = new Measurement
            {
                name = name, inputSha256 = BitConverter.ToString(sha.ComputeHash(AnatomySnapshotCodec.Encode(snapshot))).Replace("-", "").ToLowerInvariant(),
                unity = Application.unityVersion, nativeVersion = Core.DLL.HbpCore.HbpCoreRuntime.Version, platform = Application.platform.ToString(), provenance = snapshot.IEEG.Summary + " | " + snapshot.IEEG.PreparedSha256,
                vertices = result.ActivityUV.Length, gridPoints = result.GridPoints?.Length ?? 0,
                dimensions = new[] { result.GridDimensions.x, result.GridDimensions.y, result.GridDimensions.z }, masks = result.SiteMasks,
                coverage = new[] { result.Coverage.totalVertexCount, result.Coverage.validVertexCount, result.Coverage.invalidVertexCount, (int)result.Coverage.classification },
                surfaceValues = snapshot.IEEG.SurfaceValues.ToArray(), siteValues = snapshot.IEEG.SiteValues.ToArray(), availability = snapshot.IEEG.Availability.ToArray(),
                diameters = snapshot.Contacts.Sites.Select(s => s.Diameter).ToArray(), colors = snapshot.Contacts.Sites.SelectMany(s => s.Color.ToArray()).ToArray(), visible = snapshot.Contacts.Sites.Select(s => s.Visible).ToArray(),
                preparationMs = result.PreparationMs, computeMs = result.ComputeMs, projectionAndCopyMs = result.ProjectionAndCopyMs, uvCopyBytes = result.UvCopyBytes, inputCopyBytes = result.InputCopyBytes,
                allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), managedBytes = GC.GetTotalMemory(false)
            };
            File.WriteAllText(Path.Combine(output, name + ".json"), JsonUtility.ToJson(report, true));
            using var writer = new BinaryWriter(File.Create(Path.Combine(output, name + ".bin")));
            foreach (var uv in result.ActivityUV)
            {
                writer.Write(uv.x);
                writer.Write(uv.y);
            }

            foreach (var uv in result.AlphaUV)
            {
                writer.Write(uv.x);
                writer.Write(uv.y);
            }

            if (result.GridPoints != null)
                foreach (var p in result.GridPoints)
                {
                    writer.Write(p.x);
                    writer.Write(p.y);
                    writer.Write(p.z);
                }
        }
    }
}
#endif
