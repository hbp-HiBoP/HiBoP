#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBP.Transfer.Anatomy;
using UnityEngine;

namespace HBP.Transfer.Projection
{
    /// <summary>Opt-in full-buffer qualification. No tolerance or scientific acceptance is embedded here.</summary>
    public static class DensityBenchmark
    {
        [Serializable]
        public sealed class Measurement
        {
            public string name, inputSha256, unity, nativeVersion, platform;
            public int vertices, gridPoints, sites;
            public int[] dimensions, masks, coverage;
            public float maxDensity;
            public double preparationMs, computeMs, projectionAndCopyMs;
            public long uvCopyBytes, managedBefore, managedAfter;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string fixture, output;
#if UNITY_ANDROID && !UNITY_EDITOR
            string marker = Path.Combine(Application.persistentDataPath, "quest019-benchmark");
            if (!File.Exists(marker)) return;
            File.Delete(marker);
            fixture = Path.Combine(Application.persistentDataPath, "quest019.hbna");
            output = Path.Combine(Application.persistentDataPath, "quest019-benchmark-result");
#else
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-densityBenchmark");
            if (index < 0 || index + 2 >= args.Length) return;
            fixture = args[index + 1];
            output = args[index + 2];
#endif
            _ = RunObservedAsync(fixture, output);
        }

        private static async Task RunObservedAsync(string fixture, string output)
        {
            try
            {
                Directory.CreateDirectory(output);
                File.Delete(Path.Combine(output, "complete.json"));
                File.Delete(Path.Combine(output, "failure.txt"));
                var source = AnatomySnapshotCodec.Decode(File.ReadAllBytes(fixture));
                foreach (string name in new[] { "mni", "radius25", "nearest", "linear", "constant", "none", "single", "masked", "boundary" })
                {
                    var snapshot = Variant(source, name);
                    for (int cycle = 0; cycle < 2; cycle++)
                    {
                        long before = GC.GetTotalMemory(false);
                        var inputs = NativeProjectionInputs.Create(snapshot, Path.Combine(output, "native"));
                        try
                        {
                            var result = await inputs.ComputeDensityAsync(true);
                            Write(output, name + "-" + cycle, snapshot, result, before);
                        }
                        finally
                        {
                            inputs.Dispose();
                        }

                        if (inputs.CleanupError != null || File.Exists(inputs.VolumePath)) throw new IOException("Native inputs not released: " + inputs.CleanupError);
                    }
                }

                File.WriteAllText(Path.Combine(output, "complete.json"), "{\"completed\":true,\"runs\":18}");
                Debug.Log("QUEST019_BENCHMARK_COMPLETE");
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "failure.txt"), exception.ToString());
                Debug.LogError("QUEST019_BENCHMARK_FAIL " + exception);
            }
        }

        public static void Write(string output, string name, AnatomySnapshot snapshot, NativeProjectionInputs.DensityResult result, long before)
        {
            Directory.CreateDirectory(output);
            byte[] bytes = AnatomySnapshotCodec.Encode(snapshot);
            using var sha = System.Security.Cryptography.SHA256.Create();
            var report = new Measurement
            {
                name = name, inputSha256 = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(),
                unity = Application.unityVersion, nativeVersion = Core.DLL.HbpCore.HbpCoreRuntime.Version, platform = Application.platform.ToString(),
                vertices = result.ActivityUV.Length, gridPoints = result.GridPoints?.Length ?? 0, sites = result.SiteMasks.Length,
                dimensions = new[] { result.GridDimensions.x, result.GridDimensions.y, result.GridDimensions.z }, masks = result.SiteMasks,
                coverage = new[] { result.Coverage.totalVertexCount, result.Coverage.validVertexCount, result.Coverage.invalidVertexCount, (int)result.Coverage.classification },
                maxDensity = result.MaxDensity, preparationMs = result.PreparationMs, computeMs = result.ComputeMs, projectionAndCopyMs = result.ProjectionAndCopyMs,
                uvCopyBytes = result.UvCopyBytes, managedBefore = before, managedAfter = GC.GetTotalMemory(false)
            };
            File.WriteAllText(Path.Combine(output, name + ".json"), JsonUtility.ToJson(report, true));
            using var writer = new BinaryWriter(File.Create(Path.Combine(output, name + ".bin")));
            writer.Write(result.MaxDensity);
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
                foreach (var point in result.GridPoints)
                {
                    writer.Write(point.x);
                    writer.Write(point.y);
                    writer.Write(point.z);
                }

            foreach (float value in snapshot.Positions.AsReadOnlySpan()) writer.Write(value);
        }

        private static AnatomySnapshot Variant(AnatomySnapshot source, string name)
        {
            var settings = source.Projection;
            var projection = new AnatomyProjection(settings.VolumeBytes.ToArray(), settings.GridDimension, name == "nearest" ? 0 : settings.Interpolation, name == "radius25" ? 25 : settings.InfluenceDistance, name == "linear" ? 1 : name == "constant" ? 0 : settings.InfluenceByDistance, settings.ActivityAlpha);
            IEnumerable<AnatomySite> selected = source.Contacts.Sites;
            if (name == "none") selected = Array.Empty<AnatomySite>();
            if (name == "single") selected = selected.Take(1);
            var sites = selected.Select((site, index) => new AnatomySite(site.Id, site.Name, site.Electrode, index, site.PatientIndex, site.SourceIndex, site.Position.ToArray(), site.Color.ToArray(), site.Diameter, site.Visible, name == "masked" ? site.Flags | AnatomySiteFlags.Masked : site.Flags, name == "masked" || site.EffectiveMasked)).ToArray();
            var contacts = new AnatomyContacts("MNI", source.Contacts.RoiActive, source.Contacts.PatientIds.ToArray(), sites);
            bool boundary = name == "boundary";
            return AnatomySnapshot.Create(source.TransferId, source.SessionId, source.VisualizationId, source.ColumnId, source.ContentRevision, source.Coordinates, source.Winding, source.Visible, source.Color.ToArray(), boundary ? new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0, 1000, 0, 0 } : source.Positions.ToArray(), boundary ? new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 } : source.Normals.ToArray(), boundary ? new uint[] { 0, 1, 2, 1, 2, 3 } : source.Indices.ToArray(), boundary ? Array.Empty<float>() : source.Uvs.ToArray(), contacts, projection);
        }
    }
}
#endif
