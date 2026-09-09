using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Preferences;
using HBP.Data.Module3D;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HBP.Dev
{
    /// <summary>Opt-in Desktop iEEG comparison against the pre-QUEST-021 native call sequence.</summary>
    public static class IEEGProjectionDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-ieegEvidence");
            if (index >= 0 && index + 1 < args.Length)
                Run(Path.GetFullPath(args[index + 1]), args.Contains("-ieegEvidenceOnce"), Application.exitCancellationToken).Forget();
#endif
        }

        private static async UniTaskVoid Run(string directory, bool quit, CancellationToken token)
        {
            int exitCode = 0;
            try
            {
                await RunAsync(directory, token);
            }
            catch (Exception exception)
            {
                exitCode = 1;
                Debug.LogException(exception);
            }

            if (quit) Application.Quit(exitCode);
        }

        private static async UniTask RunAsync(string directory, CancellationToken token)
        {
            Directory.CreateDirectory(directory);
            var rows = new JArray();
            bool checkSites = Environment.GetCommandLineArgs().Contains("-siteAppearanceEvidence");
            try
            {
                await WaitAsync(() => Module3DMain.IsInitialized && Module3DMain.SelectedScene != null && Module3DMain.SelectedScene.SceneInformation.CompletelyLoaded && !Module3DMain.SelectedScene.SceneInformation.GeometryNeedsUpdate && !Module3DMain.SelectedScene.SceneInformation.SurfaceProjectionNeedsUpdate, token);
                var scene = Module3DMain.SelectedScene;
                var column = scene.SelectedColumn as Column3DIEEG ?? throw new InvalidOperationException("Select the MNI iEEG fixture.");
                if (column.ProjectionTimeline.Length != 151 || column.Sites.Count != 8)
                    throw new InvalidOperationException("Expected the QUEST-020 fixture: eight contacts and 151 samples.");
                var parameters = column.DynamicParameters;
                float originalDistance = parameters.InfluenceDistance;
                float originalMin = parameters.SpanMin, originalMiddle = parameters.Middle, originalMax = parameters.SpanMax;
                int originalIndex = column.Timeline.CurrentIndex;
                Vector2[] initial = null;
                try
                {
                    for (int configuration = 0; configuration < 3; ++configuration)
                    {
                        parameters.InfluenceDistance = configuration == 1 ? 25 : originalDistance;
                        parameters.SetSpanValues(configuration == 1 ? -8 : originalMin, configuration == 1 ? -1 : originalMiddle, configuration == 1 ? 3 : originalMax);
                        int beforeVersion = scene.ActivityFieldVersion;
                        scene.UpdateGenerator();
                        await WaitAsync(() => scene.IsGeneratorUpToDate && scene.ActivityFieldVersion > beforeVersion && !scene.SceneInformation.FunctionalSurfaceNeedsUpdate, token);

                        // Separate reference generator/sites; production owns its resources throughout.
                        using var sites = new RawSiteList(column.RawElectrodes);
                        using var reference = new IEEGGenerator();
                        using var referenceProjection = new SurfaceGenerator();
                        reference.Initialize(column.ActivityGenerator.ProjectionGrid);
                        reference.SetSmoothActivityBoundaries(PersistentDataManager.UserPreferences.Visualization._3D.SmoothActivityBoundaries);
                        referenceProjection.Initialize(reference, column.SurfaceGenerator.Surface);
                        reference.ComputeActivity(sites, parameters.InfluenceDistance, column.ActivityValues, column.ProjectionTimeline.Length, sites.NumberOfSites, PersistentDataManager.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
                        reference.AdjustValues(parameters.Middle, parameters.SpanMin, parameters.SpanMax);
                        foreach (int index in new[] { 0, 10, 50, 75, 130, 150 })
                        {
                            column.Timeline.CurrentIndex = index;
                            for (int frame = 0; frame < 10; ++frame) await UniTask.NextFrame(cancellationToken: token);
                            await WaitAsync(() => !scene.SceneInformation.FunctionalSurfaceNeedsUpdate, token);
                            referenceProjection.ComputeActivityUV(column.CurrentProjectionSample.Index, column.ActivityAlpha);
                            var projection = column.SurfaceGenerator;
                            if (!projection.ActivityUV.SequenceEqual(referenceProjection.ActivityUV) || !projection.AlphaUV.SequenceEqual(referenceProjection.AlphaUV))
                                throw new InvalidOperationException($"Native reference differs at configuration {configuration}, index {index}.");
                            var mesh = column.BrainMesh.GetComponent<MeshFilter>().sharedMesh;
                            if (!mesh.uv3.SequenceEqual(projection.ActivityUV) || !mesh.uv2.SequenceEqual(projection.AlphaUV))
                                throw new InvalidOperationException("The Desktop renderer did not receive the common iEEG UVs.");
                            if (checkSites)
                            {
                                VerifySites(scene, column);
                                if (index == 50) await VerifyCaptureWithoutSiteRenderers(scene, column, token);
                                if (configuration == 0 && (index == 10 || index == 130))
                                    SaveView(column.Views[0], Path.Combine(directory, $"sites-index{index}.png"));
                            }

                            if (index == 50)
                            {
                                if (configuration == 0) initial = (Vector2[])projection.ActivityUV.Clone();
                                if (configuration == 1 && projection.ActivityUV.SequenceEqual(initial)) throw new InvalidOperationException("Changing parameters did not change the projection.");
                                if (configuration == 2 && !projection.ActivityUV.SequenceEqual(initial)) throw new InvalidOperationException("Restoring parameters did not restore the projection.");
                                SaveView(column.Views[0], Path.Combine(directory, $"ieeg-{configuration}-index50.png"));
                            }

                            rows.Add(new JObject
                            {
                                ["configuration"] = configuration, ["navigationIndex"] = index, ["projectionIndex"] = column.CurrentProjectionSample.Index,
                                ["distanceMm"] = parameters.InfluenceDistance, ["spanMin"] = parameters.SpanMin, ["middle"] = parameters.Middle, ["spanMax"] = parameters.SpanMax,
                                ["vertices"] = projection.ActivityUV.Length, ["validVertices"] = projection.ProjectionCoverage.validVertexCount,
                                ["siteAppearanceExact"] = checkSites, ["captureWithoutSiteRenderers"] = checkSites && index == 50,
                                ["activeSites"] = sites.GetMask().Count(mask => mask == 0), ["referenceBitExact"] = true, ["uploadedExactly"] = true
                            });
                        }
                    }
                }
                finally
                {
                    parameters.InfluenceDistance = originalDistance;
                    parameters.SetSpanValues(originalMin, originalMiddle, originalMax);
                    column.Timeline.CurrentIndex = originalIndex;
                }

                File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["success"] = true, ["unity"] = Application.unityVersion, ["graphics"] = SystemInfo.graphicsDeviceName, ["rows"] = rows }.ToString(Formatting.Indented));
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["success"] = false, ["error"] = exception.ToString(), ["rows"] = rows }.ToString(Formatting.Indented));
                throw;
            }
        }

        // Independent pre-QUEST-022 reference; compares actual Desktop presentation, not two calls to the new rule.
        private static void VerifySites(Base3DScene scene, Column3DIEEG column)
        {
            var p = column.DynamicParameters;
            for (int i = 0; i < column.Sites.Count; i++)
            {
                var site = column.Sites[i];
                var state = site.State;
                bool visible = !(state.IsMasked || (state.IsOutOfROI && !scene.ShowAllSites) || !state.IsFiltered || (state.IsBlackListed && scene.HideBlacklistedSites));
                if (site.IsActive != visible) throw new InvalidOperationException($"Site {i}: visibility changed.");
                if (!visible) continue;
                float value = column.CurrentProjectionSample.Evaluate(column.ActivityValuesBySiteID[i]);
                if (value < p.SpanMin) value = p.SpanMin;
                if (value > p.SpanMax) value = p.SpanMax;
                value -= p.Middle;
                var type = value > 0 ? Core.Enums.SiteType.Positive : Core.Enums.SiteType.Negative;
                float scale = value < 0 ? 0.5f + 2 * (value / (p.SpanMin - p.Middle)) : value > 0 ? 0.5f + 2 * (value / (p.SpanMax - p.Middle)) : 0.5f;
                if (state.IsBlackListed)
                {
                    scale = 1;
                    type = Core.Enums.SiteType.BlackListed;
                }
                else if (!scene.IsGeneratorUpToDate)
                {
                    scale = 1;
                    type = Core.Enums.SiteType.Normal;
                }

                if (site.transform.localScale != Vector3.one * scale * scene.SiteGain || site.GetComponent<MeshRenderer>().sharedMaterial != Module3DMain.SharedMaterials.Site.GetSharedMaterial(state.IsHighlighted, type, state.Color))
                    throw new InvalidOperationException($"Site {i}: legacy size or material differs.");
            }
        }

        private static async UniTask VerifyCaptureWithoutSiteRenderers(Base3DScene scene, Column3DIEEG column, CancellationToken token)
        {
            var renderers = column.Sites.Select(site => site.GetComponent<MeshRenderer>()).ToArray();
            var materials = renderers.Select(renderer => renderer.sharedMaterial).ToArray();
            System.Threading.Tasks.Task<Transfer.Anatomy.AnatomySnapshot> pending;
            try
            {
                foreach (var renderer in renderers) renderer.sharedMaterial = null;
                pending = Transfer.Anatomy.Desktop.DesktopAnatomyCapture.CaptureSelectedAsync(Guid.NewGuid().ToString(), "quest-022", 1, token);
            }
            finally
            {
                for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterial = materials[i];
            }

            var snapshot = await pending;
            for (int i = 0; i < column.Sites.Count; i++)
            {
                var site = column.Sites[i];
                var actual = snapshot.Contacts.Sites[i];
                var appearance = column.EvaluateSiteAppearance(i, scene.ShowAllSites, scene.HideBlacklistedSites, scene.IsGeneratorUpToDate);
                Color color = Module3DMain.SharedMaterials.Site.GetSharedMaterial(false, appearance.Type, site.State.Color).GetColor("_Color");
                if (QualitySettings.activeColorSpace == ColorSpace.Linear) color = color.linear;
                if (actual.Visible != appearance.Visible || actual.Diameter != 2f * (appearance.Scale * scene.SiteGain) || !actual.Color.ToArray().SequenceEqual(new[] { color.r, color.g, color.b, color.a }) || actual.EffectiveMasked != site.State.IsEffectivelyMasked(snapshot.Contacts.RoiActive))
                    throw new InvalidOperationException($"Site {i}: prepared transfer appearance differs.");
            }
        }

        private static async UniTask WaitAsync(Func<bool> ready, CancellationToken token)
        {
            float deadline = Time.realtimeSinceStartup + 120;
            while (!ready())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("iEEG evidence did not become ready within 120 seconds.");
                await UniTask.NextFrame(cancellationToken: token);
            }
        }

        private static void SaveView(View3D view, string path)
        {
            var texture = view.GetTexture(1024, 1024, new Color(.15f, .15f, .15f, 1));
            try
            {
                if (!texture.GetPixels32().Any(pixel => pixel.r > 60 || pixel.g > 60 || pixel.b > 60)) throw new InvalidOperationException("The iEEG reference has no visible geometry.");
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
