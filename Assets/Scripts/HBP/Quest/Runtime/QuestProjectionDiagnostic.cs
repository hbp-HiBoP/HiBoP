#if DEVELOPMENT_BUILD && UNITY_ANDROID && ENABLE_IL2CPP
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Transfer.Anatomy;
using UnityEngine;
using Plane = HBP.Core.DLL.Plane;

namespace HBP.Quest
{
    /// <summary>Opt-in reconstruction proof using an exported Desktop snapshot. Does not calculate density.</summary>
    public static class QuestProjectionDiagnostic
    {
        [Serializable]
        private sealed class Report
        {
            public string runId, snapshotSha256, volumeSha256, privateVolumePath, failure;
            public string unity, device, nativeVersion;
            public int volumeBytes, vertices, sites, maskedSites, cycles;
            public Vector3Int dimensions;
            public Vector3 spacing, center;
            public int grid, interpolation, influenceRule;
            public float influenceDistance;
            public bool passed, surfaceRoundTrip, sitePositions, nativeMasks, replacementRelease, failedReplacementPreserved, closeRelease;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string marker = Path.Combine(Application.persistentDataPath, "quest017-projection-probe");
            if (!File.Exists(marker)) return;
            string id = File.ReadAllText(marker).Trim();
            File.Delete(marker);
            if (!Guid.TryParseExact(id, "N", out _)) { Debug.LogError("QUEST017_PROBE_FAIL invalid run ID"); return; }
            _ = RunAsync(id);
        }

        private static async Task RunAsync(string id)
        {
            var report = new Report { runId = id, unity = Application.unityVersion, device = SystemInfo.deviceModel, nativeVersion = Core.DLL.HbpCore.HbpCoreRuntime.Version };
            QuestAnatomyView view = null;
            try
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(Application.persistentDataPath, "quest017-projection.hbna"));
                using (var sha = SHA256.Create()) report.snapshotSha256 = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                var snapshot = await Task.Run(() => AnatomySnapshotCodec.Decode(bytes), Application.exitCancellationToken);
                Require(snapshot.SchemaVersion == 3 && snapshot.Projection != null, "Expected a complete projection snapshot");
                view = UnityEngine.Object.FindAnyObjectByType<QuestAnatomyView>();
                report.volumeBytes = snapshot.Projection.VolumeBytes.Count;
                report.volumeSha256 = BitConverter.ToString(snapshot.Projection.VolumeHash.ToArray()).Replace("-", "").ToLowerInvariant();
                report.vertices = snapshot.VertexCount;
                report.sites = snapshot.Contacts.Sites.Count;
                report.maskedSites = snapshot.Contacts.Sites.Count(site => site.EffectiveMasked);
                report.grid = snapshot.Projection.GridDimension;
                report.interpolation = snapshot.Projection.Interpolation;
                report.influenceDistance = snapshot.Projection.InfluenceDistance;
                report.influenceRule = snapshot.Projection.InfluenceByDistance;
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    view.ApplySnapshot(snapshot);
                    await view.DensityCompletion;
                    var first = view.ProjectionInputs;
                    report.privateVolumePath = first.VolumePath;
                    Require(first.VolumePath.StartsWith("/data/", StringComparison.Ordinal), "Volume must be in Android internal private storage");
                    report.dimensions = first.Volume.Dimensions;
                    report.spacing = first.Volume.Spacing;
                    report.center = first.Volume.Center;
                    Require(report.dimensions == new Vector3Int(snapshot.Projection.Dimensions[0], snapshot.Projection.Dimensions[1], snapshot.Projection.Dimensions[2]), "Dimensions");
                    var mesh = new Mesh();
                    try
                    {
                        first.Surface.UpdateMeshFromDLL(mesh);
                        Require(mesh.vertices.SelectMany(p => new[] { p.x, p.y, p.z }).SequenceEqual(snapshot.Positions.ToArray()), "Surface positions");
                        Require(mesh.normals.SelectMany(p => new[] { p.x, p.y, p.z }).SequenceEqual(snapshot.Normals.ToArray()), "Surface normals");
                        Require(mesh.triangles.Select(index => (uint)index).SequenceEqual(snapshot.Indices.ToArray()), "Surface winding");
                        report.surfaceRoundTrip = true;
                    }
                    finally { UnityEngine.Object.Destroy(mesh); }
                    foreach (var site in snapshot.Contacts.Sites)
                    {
                        using var plane = new Plane(new Vector3(site.Position[0], site.Position[1], site.Position[2]), Vector3.right);
                        first.Sites.GetSitesOnPlane(plane, .01f, out int[] onPlane);
                        Require(onPlane[site.Order] == 1, "Site coordinate reflection");
                    }
                    Require(first.Sites.NumberOfSites == report.sites, "Site count");
                    Require(first.Sites.GetNativePositions().SequenceEqual(snapshot.Contacts.Sites.Select(site => Transfer.Projection.NativeProjectionInputs.TransportToNativeSite(site.Position))), "Native site position readback");
                    Require(first.Sites.GetMask().SequenceEqual(snapshot.Contacts.Sites.Select(site => site.EffectiveMasked ? 1 : 0)), "Native mask readback");
                    if (report.sites > 0)
                    {
                        first.Sites.UpdateMask(0, !snapshot.Contacts.Sites[0].EffectiveMasked);
                        Require(first.Sites.GetMask()[0] == (snapshot.Contacts.Sites[0].EffectiveMasked ? 0 : 1), "Native changed mask readback");
                        first.Sites.UpdateMask(0, snapshot.Contacts.Sites[0].EffectiveMasked);
                    }
                    report.sitePositions = true;
                    report.nativeMasks = true;
                    var rejected = AnatomySnapshot.Create("rejected", snapshot.SessionId, snapshot.VisualizationId, snapshot.ColumnId, 2, snapshot.Coordinates, snapshot.Winding, snapshot.Visible, new float[] { 1, 1, 1, .5f }, snapshot.Positions.ToArray(), snapshot.Normals.ToArray(), snapshot.Indices.ToArray(), snapshot.Uvs.ToArray(), snapshot.Contacts, snapshot.Projection);
                    bool rejectedAsExpected = false;
                    try { view.ApplySnapshot(rejected); } catch (ArgumentException) { rejectedAsExpected = true; }
                    Require(rejectedAsExpected && view.ProjectionInputs == first && first.Surface.NumberOfVertices == report.vertices && File.Exists(first.VolumePath), "Failed publication preserves current native resources");
                    report.failedReplacementPreserved = true;
                    // Lifetime persists while disconnected; no network dependency is retained in the resource owner.
                    await Task.Delay(1000, Application.exitCancellationToken);
                    Require(first.Volume.IsLoaded && first.Sites.NumberOfSites == report.sites, "Independent session lifetime");
                    view.ApplySnapshot(snapshot);
                    await view.DensityCompletion;
                    Require(first.Volume.getHandle().Handle == IntPtr.Zero && first.Surface.getHandle().Handle == IntPtr.Zero && first.Sites.getHandle().Handle == IntPtr.Zero && !File.Exists(first.VolumePath) && first.CleanupError == null, "Replacement release");
                    report.replacementRelease = true;
                    var last = view.ProjectionInputs;
                    view.Clear();
                    Require(view.ProjectionInputs == null && last.Volume.getHandle().Handle == IntPtr.Zero && last.Surface.getHandle().Handle == IntPtr.Zero && last.Sites.getHandle().Handle == IntPtr.Zero && !File.Exists(last.VolumePath) && last.CleanupError == null, "Close release");
                    report.closeRelease = true;
                    report.cycles++;
                }
                report.passed = true;
            }
            catch (Exception exception) { report.failure = exception.ToString(); }
            finally
            {
                if (view != null) view.Clear();
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "quest017-result.json"), JsonUtility.ToJson(report, true));
                if (report.passed) Debug.Log("QUEST017_PROBE_PASS " + id);
                else Debug.LogError("QUEST017_PROBE_FAIL " + report.failure);
            }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
#endif
