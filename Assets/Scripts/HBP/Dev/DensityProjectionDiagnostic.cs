using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Data.Module3D;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HBP.Dev
{
    /// <summary>Opt-in Desktop density evidence. Uses the production scene update only.</summary>
    public static class DensityProjectionDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-densityEvidence");
            if (index >= 0 && index + 1 < args.Length)
                Run(Path.GetFullPath(args[index + 1]), Application.exitCancellationToken).Forget();
#endif
        }

        private static async UniTaskVoid Run(string directory, CancellationToken cancellationToken)
        {
            try
            {
                await RunAsync(directory, cancellationToken);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static async UniTask RunAsync(string directory, CancellationToken token)
        {
            Directory.CreateDirectory(directory);
            var rows = new JArray();
            try
            {
                await WaitAsync(() => Module3DMain.IsInitialized && Module3DMain.SelectedScene != null && Module3DMain.SelectedScene.SceneInformation.CompletelyLoaded && !Module3DMain.SelectedScene.SceneInformation.GeometryNeedsUpdate && !Module3DMain.SelectedScene.SceneInformation.SurfaceProjectionNeedsUpdate, token);
                var scene = Module3DMain.SelectedScene;
                var column = scene.SelectedColumn as Column3DAnatomy ?? throw new InvalidOperationException("Select the MNI Contacts anatomical column.");
                float originalDistance = column.AnatomyParameters.InfluenceDistance;
                Vector2[] reference = null;
                try
                {
                    foreach (float distance in new[] { originalDistance, 25f, originalDistance })
                    {
                        int beforeVersion = scene.ActivityFieldVersion;
                        float started = Time.realtimeSinceStartup;
                        column.AnatomyParameters.InfluenceDistance = distance;
                        scene.UpdateGenerator();
                        await WaitAsync(() => scene.IsGeneratorUpToDate && scene.ActivityFieldVersion > beforeVersion && !scene.SceneInformation.FunctionalSurfaceNeedsUpdate, token);
                        // Let the existing cameras render their updated mesh streams.
                        for (int i = 0; i < 10; ++i) await UniTask.NextFrame(cancellationToken: token);
                        var uv = (Vector2[])column.SurfaceGenerator.ActivityUV.Clone();
                        var alpha = column.SurfaceGenerator.AlphaUV;
                        var mesh = column.BrainMesh.GetComponent<MeshFilter>().sharedMesh;
                        if (!mesh.uv3.SequenceEqual(uv) || !mesh.uv2.SequenceEqual(alpha)) throw new InvalidOperationException("The Desktop renderer did not receive the common projection UVs.");
                        if (reference != null && rows.Count == 2 && !uv.SequenceEqual(reference)) throw new InvalidOperationException("Restoring influence did not restore the density UVs.");
                        if (reference != null && rows.Count == 1 && uv.SequenceEqual(reference)) throw new InvalidOperationException("Changing influence left the fixture density unchanged.");
                        string name = $"density-{rows.Count}-{distance:0}mm";
                        SaveView(column.Views[0], Path.Combine(directory, name + ".png"));
                        var coverage = column.SurfaceGenerator.ProjectionCoverage;
                        rows.Add(new JObject
                        {
                            ["name"] = name, ["distanceMm"] = distance, ["activityFieldVersion"] = scene.ActivityFieldVersion,
                            ["maxDensity"] = ((DensityGenerator)column.ActivityGenerator).MaxDensity,
                            ["vertices"] = uv.Length, ["uploadedExactly"] = true,
                            ["seconds"] = Time.realtimeSinceStartup - started,
                            ["coverage"] = coverage.classification.ToString(), ["validVertices"] = coverage.validVertexCount,
                            ["activeSites"] = column.RawElectrodes.GetMask().Count(value => value == 0)
                        });
                        reference ??= uv;
                    }
                }
                finally
                {
                    column.AnatomyParameters.InfluenceDistance = originalDistance;
                }

                File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["success"] = true, ["unity"] = Application.unityVersion, ["graphics"] = SystemInfo.graphicsDeviceName, ["rows"] = rows }.ToString(Formatting.Indented));
                Debug.Log("QUEST-018 density evidence completed: " + directory);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["success"] = false, ["error"] = exception.ToString(), ["rows"] = rows }.ToString(Formatting.Indented));
                throw;
            }
        }

        private static async UniTask WaitAsync(Func<bool> ready, CancellationToken token)
        {
            float deadline = Time.realtimeSinceStartup + 120;
            while (!ready())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Density evidence did not become ready within 120 seconds.");
                await UniTask.NextFrame(cancellationToken: token);
            }
        }

        private static void SaveView(View3D view, string path)
        {
            var texture = view.GetTexture(1024, 1024, new Color(.15f, .15f, .15f, 1));
            try
            {
                if (!texture.GetPixels32().Any(pixel => pixel.r > 60 || pixel.g > 60 || pixel.b > 60))
                    throw new InvalidOperationException("The density reference image contains no visible geometry.");
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
