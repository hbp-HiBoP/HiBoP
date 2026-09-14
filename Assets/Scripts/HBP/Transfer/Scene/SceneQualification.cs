#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace HBP.Transfer.Scene
{
    /// <summary>Opt-in evidence from the real common scene, usable before or after transport.</summary>
    public static class SceneQualification
    {
        public static async UniTask RunAsync(Base3DScene scene, string directory, CancellationToken token)
        {
            using var lease = scene.RetainForPreparation();
            Directory.CreateDirectory(directory);
            var report = new JObject { ["schemaVersion"] = 2, ["success"] = false, ["unity"] = Application.unityVersion, ["platform"] = Application.platform.ToString(), ["graphics"] = SystemInfo.graphicsDeviceName };
            var states = new JArray();
            report["states"] = states;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var timelines = scene.Columns.Select(c => c.NavigationTimeline).Where(t => t != null).ToArray();
            var saved = timelines.Select(t => (t.CurrentIndex, t.IsPlaying, t.IsLooping)).ToArray();
            long peak = Profiler.GetTotalAllocatedMemoryLong();
            bool sampling = true;

            async UniTask Sample()
            {
                while (sampling)
                {
                    peak = Math.Max(peak, Profiler.GetTotalAllocatedMemoryLong());
                    await UniTask.NextFrame();
                }
            }

            var sampler = Sample();
            try
            {
                foreach (var timeline in timelines) timeline.IsPlaying = false;
                await scene.PrepareRenderingAsync(token);
                states.Add(WriteState(scene, directory, "initial"));
                foreach (int position in new[] { 0, 1, 2 })
                {
                    foreach (var column in scene.Columns)
                        if (column.NavigationTimeline is { Length: > 1 } timeline)
                            scene.SetTimelineIndex(column, position * (timeline.Length - 1) / 2);
                    await scene.PrepareRenderingAsync(token);
                    states.Add(WriteState(scene, directory, "time" + position));
                }

                for (int i = 0; i < timelines.Length; i++) timelines[i].CurrentIndex = saved[i].CurrentIndex;
                scene.InvalidateActivityField();
                await scene.PrepareRenderingAsync(token);
                var recomputed = WriteState(scene, directory, "recomputed");
                states.Add(recomputed);
                // Exact repeatability on one platform; cross-platform differences are reported separately.
                var initial = (JArray)states[0]["columns"];
                var repeated = (JArray)recomputed["columns"];
                for (int i = 0; i < initial.Count; i++)
                    if ((string)initial[i]["sha256"] != (string)repeated[i]["sha256"])
                        throw new InvalidOperationException("Recalculation changed the restored buffers of column " + i);
                var cut = scene.AddCutPlane();
                try
                {
                    scene.UpdateCutPlane(cut);
                    await scene.PrepareRenderingAsync(token);
                    states.Add(WriteState(scene, directory, "cut"));
                    if (scene.Columns.Any(c => c.BrainCutMeshes.Count != scene.Cuts.Count)) throw new InvalidOperationException("Cut rendering is incomplete.");
                }
                finally
                {
                    scene.RemoveCutPlane(cut);
                }

                await scene.PrepareRenderingAsync(token);
                // Exercise selectors which have no controller UI on Quest.
                foreach (var column in scene.Columns)
                {
                    Action restore = null;
                    try
                    {
                        if (column is Column3DStatic stat && stat.Labels.Length > 1)
                        {
                            int old = stat.SelectedLabelIndex;
                            restore = () => stat.SelectedLabelIndex = old;
                            stat.SelectedLabelIndex = stat.Labels.Length - 1;
                        }
                        else if (column is Column3DFMRI fmri && fmri.ColumnFMRIData.Data.FMRIs.Count > 1)
                        {
                            int old = fmri.SelectedFMRIIndex;
                            restore = () => fmri.SelectedFMRIIndex = old;
                            fmri.SelectedFMRIIndex = fmri.ColumnFMRIData.Data.FMRIs.Count - 1;
                        }
                        else if (column is Column3DMEG meg && meg.ColumnMEGData.Data.MEGItems.Count > 1)
                        {
                            int old = meg.SelectedMEGIndex;
                            restore = () => meg.SelectedMEGIndex = old;
                            meg.SelectedMEGIndex = meg.ColumnMEGData.Data.MEGItems.Count - 1;
                        }
                        else if (column is Column3DCCEP ccep && ccep.Sources.Count > 0)
                        {
                            var old = ccep.SelectedSourceSite;
                            restore = () => ccep.SelectedSourceSite = old;
                            ccep.SelectedSourceSite = ccep.Sources.Last();
                        }

                        if (restore != null)
                        {
                            await scene.PrepareRenderingAsync(token);
                            states.Add(WriteState(scene, directory, "resource-" + scene.Columns.IndexOf(column)));
                        }
                    }
                    finally
                    {
                        if (!scene.IsClosing) restore?.Invoke();
                    }
                }

                await scene.PrepareRenderingAsync(token);
                string originalMesh = scene.MeshManager.SelectedMesh.Name;
                string originalMRI = scene.MRIManager.SelectedMRI.Name;
                var originalMasks = scene.TriangleEraser.CurrentMasks.Select(mask => (int[])mask.Clone()).ToList();
                var alternativeMesh = scene.MeshManager.Meshes.FirstOrDefault(mesh => mesh.IsLoaded && mesh.Name != originalMesh);
                var alternativeMRI = scene.MRIManager.MRIs.FirstOrDefault(mri => mri.Name != originalMRI);
                try
                {
                    if (alternativeMesh != null)
                    {
                        scene.MeshManager.Select(alternativeMesh.Name);
                        await scene.PrepareRenderingAsync(token);
                        states.Add(WriteState(scene, directory, "mesh-alternative"));
                    }

                    if (alternativeMRI != null)
                    {
                        scene.MRIManager.Select(alternativeMRI.Name);
                        await scene.PrepareRenderingAsync(token);
                        states.Add(WriteState(scene, directory, "mri-alternative"));
                    }
                }
                finally
                {
                    if (!scene.IsClosing)
                    {
                        using var restoration = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
                        restoration.CancelAfter(TimeSpan.FromMinutes(1));
                        scene.MeshManager.Select(originalMesh);
                        scene.MRIManager.Select(originalMRI);
                        // Resolve topology before restoring the user's erasure.
                        await scene.PrepareRenderingAsync(restoration.Token);
                        scene.TriangleEraser.CurrentMasks = originalMasks;
                        for (int i = 0; i < timelines.Length; i++) timelines[i].CurrentIndex = saved[i].CurrentIndex;
                        scene.InvalidateActivityField();
                        await scene.PrepareRenderingAsync(restoration.Token);
                    }
                }

                if (scene.TriangleEraser.CurrentMasks.Count != originalMasks.Count || scene.TriangleEraser.CurrentMasks.Where((mask, i) => !mask.SequenceEqual(originalMasks[i])).Any())
                    throw new InvalidOperationException("Geometry restoration changed the user's erasure masks.");
                states.Add(WriteState(scene, directory, "geometry-restored"));
                report["success"] = true;
            }
            catch (Exception exception)
            {
                report["error"] = exception.ToString();
                throw;
            }
            finally
            {
                if (scene != null && !scene.IsClosing)
                    for (int i = 0; i < timelines.Length; i++)
                    {
                        timelines[i].CurrentIndex = saved[i].CurrentIndex;
                        timelines[i].IsLooping = saved[i].IsLooping;
                        timelines[i].IsPlaying = saved[i].IsPlaying;
                    }

                sampling = false;
                await sampler;
                // Allow deferred Unity destruction to finish before measuring retained resources.
                await UniTask.NextFrame();
                report["elapsedMs"] = clock.Elapsed.TotalMilliseconds;
                report["peakUnityAllocatedBytesSampledPerFrame"] = Math.Max(peak, Profiler.GetTotalAllocatedMemoryLong());
                report["unityAllocatedBytesAtEnd"] = Profiler.GetTotalAllocatedMemoryLong();
                report["meshesAtEnd"] = Resources.FindObjectsOfTypeAll<Mesh>().Length;
                report["meshInventoryAtEnd"] = new JArray(Resources.FindObjectsOfTypeAll<Mesh>().Select(mesh => new JObject
                {
                    ["id"] = mesh.GetEntityId().ToString(),
                    ["name"] = mesh.name,
                    ["vertices"] = mesh.vertexCount,
                    ["bytes"] = Profiler.GetRuntimeMemorySizeLong(mesh)
                }));
                report["texturesAtEnd"] = Resources.FindObjectsOfTypeAll<Texture>().Length;
                report["materialsAtEnd"] = Resources.FindObjectsOfTypeAll<Material>().Length;
                report["managedBytesAtEnd"] = GC.GetTotalMemory(false);
                File.WriteAllText(Path.Combine(directory, "result.json"), report.ToString());
            }
        }

        private static JObject WriteState(Base3DScene scene, string directory, string name)
        {
            var columns = new JArray();
            for (int i = 0; i < scene.Columns.Count; i++)
            {
                var column = scene.Columns[i];
                var mesh = column.BrainMesh.GetComponent<MeshFilter>().sharedMesh;
                var activity = mesh.uv3;
                var alpha = mesh.uv2;
                var colors = mesh.colors;
                var grid = column.ActivityGenerator.ProjectionGrid;
                var points = grid.Points;
                string file = name + "-column" + i + ".bin";
                using (var writer = new BinaryWriter(File.Create(Path.Combine(directory, file))))
                {
                    foreach (var value in activity)
                    {
                        writer.Write(value.x);
                        writer.Write(value.y);
                    }

                    foreach (var value in alpha)
                    {
                        writer.Write(value.x);
                        writer.Write(value.y);
                    }

                    foreach (var value in points)
                    {
                        writer.Write(value.x);
                        writer.Write(value.y);
                        writer.Write(value.z);
                    }

                    foreach (var value in colors)
                    {
                        writer.Write(value.r);
                        writer.Write(value.g);
                        writer.Write(value.b);
                        writer.Write(value.a);
                    }
                }

                var sites = new JArray(column.Sites.Select(site => new JObject
                {
                    ["id"] = site.Information.FullID,
                    ["position"] = new JArray(site.transform.localPosition.x, site.transform.localPosition.y, site.transform.localPosition.z),
                    ["scale"] = new JArray(site.transform.localScale.x, site.transform.localScale.y, site.transform.localScale.z),
                    ["visible"] = site.IsActive, ["masked"] = site.State.IsMasked, ["filtered"] = site.State.IsFiltered, ["blacklisted"] = site.State.IsBlackListed,
                    ["material"] = site.GetComponent<Renderer>().sharedMaterial.name,
                    ["materialColor"] = ColorValues(site.GetComponent<Renderer>().sharedMaterial)
                }));
                var cutTextures = new JArray();
                for (int cutIndex = 0; cutIndex < scene.Cuts.Count; cutIndex++)
                {
                    var rendered = column.BrainCutMeshes[cutIndex].GetComponent<Renderer>();
                    var properties = new MaterialPropertyBlock();
                    rendered.GetPropertyBlock(properties);
                    if (properties.GetTexture("_MainTex") != column.CutTextures.BrainCutTextures[cutIndex])
                        throw new InvalidOperationException("The rendered cut does not use its column's texture.");
                    foreach (var entry in new[] { ("base", column.CutTextures.BaseBrainCutTextures[cutIndex]), ("functional", column.CutTextures.BrainCutTextures[cutIndex]) })
                    {
                        string textureFile = name + "-column" + i + "-cut" + cutIndex + "-" + entry.Item1 + ".rgba";
                        using (var writer = new BinaryWriter(File.Create(Path.Combine(directory, textureFile))))
                            foreach (Color32 pixel in entry.Item2.GetPixels32())
                            {
                                writer.Write(pixel.r);
                                writer.Write(pixel.g);
                                writer.Write(pixel.b);
                                writer.Write(pixel.a);
                            }

                        cutTextures.Add(new JObject
                        {
                            ["cutIndex"] = cutIndex, ["kind"] = entry.Item1, ["width"] = entry.Item2.width, ["height"] = entry.Item2.height,
                            ["file"] = textureFile, ["sha256"] = StandardData.HashFile(Path.Combine(directory, textureFile))
                        });
                    }
                }

                columns.Add(new JObject
                {
                    ["cutTextures"] = cutTextures,
                    ["id"] = column.ColumnData.ID, ["type"] = column.ColumnData.GetType().Name,
                    ["timeIndex"] = column.NavigationTimeline?.CurrentIndex, ["timeLength"] = column.NavigationTimeline?.Length,
                    ["vertices"] = mesh.vertexCount, ["activityCount"] = activity.Length, ["alphaCount"] = alpha.Length,
                    ["gridPoints"] = points.Length, ["gridDimensions"] = new JArray(grid.Dimensions.x, grid.Dimensions.y, grid.Dimensions.z),
                    ["colorCount"] = colors.Length,
                    ["resourceIndex"] = column is Column3DStatic stat ? stat.SelectedLabelIndex : column is Column3DFMRI fmri ? fmri.SelectedFMRIIndex : column is Column3DMEG meg ? meg.SelectedMEGIndex : -1,
                    ["sourceSite"] = (column as Column3DCCEP)?.SelectedSourceSite?.Information.FullID,
                    ["masks"] = new JArray(column.RawElectrodes.GetMask()), ["sites"] = sites,
                    ["file"] = file, ["sha256"] = StandardData.HashFile(Path.Combine(directory, file))
                });
            }

            return new JObject { ["name"] = name, ["sceneId"] = scene.Visualization.ID, ["mesh"] = scene.MeshManager.SelectedMesh.Name, ["mri"] = scene.MRIManager.SelectedMRI.Name, ["columns"] = columns, ["cuts"] = scene.Cuts.Count, ["unityAllocatedBytes"] = Profiler.GetTotalAllocatedMemoryLong() };
        }

        private static JArray ColorValues(Material material)
        {
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            return new JArray(color.r, color.g, color.b, color.a);
        }
    }
}
#endif
