using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Tools;
using HBP.UI.Module3D;
using HBP.UI.Tools;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.Dev.Rendering
{
    /// <summary>
    /// Reproducible rendering capture used to compare the Built-in baseline with
    /// the current pipeline. Artifacts are isolated by pipeline.
    /// </summary>
    public static class RenderingBaselineCapture
    {
        private const string DefaultProjectName = "visu_full_test.hibop";
        private const string DefaultVisualizationName = "Small";
        private const int ExportSize = 2048;
        private const int RealWarmupFrames = 120;
        private const int RealSampleFrames = 300;
        private const int SiteStressTarget = 30000;
        private const int SiteStressWarmupFrames = 120;
        private const int SiteStressSampleFrames = 300;
        private static readonly Color CompositeBackground = new(40f / 255f, 40f / 255f, 40f / 255f, 1f);

        public static bool IsRunning { get; private set; }
        public static string LastRunDirectory { get; private set; }
        public static string LastError { get; private set; }

        public static string ResolveDefaultProjectPath()
        {
            string fromEnvironment = Environment.GetEnvironmentVariable("HIBOP_RENDERING_BASELINE_PROJECT");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return Path.GetFullPath(fromEnvironment);
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HiBoP", "Projects", DefaultProjectName);
        }

        public static async UniTask<string> RunAsync(string projectPath = null, string visualizationName = DefaultVisualizationName, bool includeSiteStress = true)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("A rendering baseline capture is already running.");
            }

            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("The rendering baseline must run in Play Mode.");
            }

            IsRunning = true;
            LastError = null;
            RenderingBaselineReport report = null;
            PerformanceManager performanceManager = UnityEngine.Object.FindFirstObjectByType<PerformanceManager>();
            bool oldSleepModeSuspended = performanceManager != null && performanceManager.SleepModeSuspended;
            if (performanceManager != null)
            {
                performanceManager.SleepModeSuspended = true;
            }

            try
            {
                projectPath ??= ResolveDefaultProjectPath();
                Base3DScene scene = await LoadReferenceSceneAsync(projectPath, visualizationName);
                report = CreateReport(scene, projectPath);
                LastRunDirectory = report.OutputDirectory;
                Directory.CreateDirectory(report.OutputDirectory);
                WriteReport(report);

                await CaptureReferenceCasesAsync(scene, report);
                SiteInventory realSiteInventory = CaptureSiteInventory(scene.Columns.SelectMany(column => column.Sites).Select(site => site.gameObject));
                report.Performance.Add(await SamplePerformanceAsync("visu_full_test_Small", scene, RealWarmupFrames, RealSampleFrames, scene.Columns.Sum(column => column.Sites.Count), realSiteInventory));
                WriteReport(report);

                report.PatchFixture = CapturePatchFixture(report.OutputDirectory);
                WriteReport(report);

                if (includeSiteStress)
                {
                    report.Performance.Add(await SampleSiteStressAsync(scene, report));
                    WriteReport(report);
                }

                File.WriteAllText(Path.Combine(GetOutputRoot(), "latest-run.txt"), report.OutputDirectory);
                Debug.Log($"Rendering validation completed: {report.OutputDirectory}");
                return report.OutputDirectory;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                if (report != null)
                {
                    report.Warnings.Add("Capture failed: " + exception);
                    WriteReport(report);
                    File.WriteAllText(Path.Combine(report.OutputDirectory, "capture-error.txt"), exception.ToString());
                }

                Debug.LogException(exception);
                throw;
            }
            finally
            {
                if (performanceManager != null)
                {
                    performanceManager.SleepModeSuspended = oldSleepModeSuspended;
                }

                IsRunning = false;
            }
        }

        private static async UniTask<Base3DScene> LoadReferenceSceneAsync(string projectPath, string visualizationName)
        {
            Base3DScene loadedScene = Module3DMain.Scenes.FirstOrDefault(scene => scene.Name == visualizationName);
            if (loadedScene != null)
            {
                await WaitForSceneReadyAsync(loadedScene);
                return loadedScene;
            }

            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("Reference project not found. Set HIBOP_RENDERING_BASELINE_PROJECT or pass an explicit path.", projectPath);
            }

            await ProjectLoaderSaver.LoadAsync(new ProjectInfo(projectPath));
            Visualization visualization = ApplicationState.LoadedProject.Visualizations.FirstOrDefault(candidate => candidate.Name == visualizationName);
            if (visualization == null)
            {
                throw new InvalidOperationException($"Visualization '{visualizationName}' was not found in '{projectPath}'.");
            }

            Module3DMain.RemoveAllScenes();
            await UniTask.NextFrame();
            await Module3DMain.LoadAsync(new[] { visualization }, (_, _, _) => { }, CancellationToken.None);

            loadedScene = Module3DMain.Scenes.FirstOrDefault(scene => scene.Name == visualizationName);
            if (loadedScene == null)
            {
                throw new InvalidOperationException($"Visualization '{visualizationName}' did not create a 3D scene.");
            }

            await WaitForSceneReadyAsync(loadedScene);
            return loadedScene;
        }

        private static async UniTask WaitForSceneReadyAsync(Base3DScene scene, int maximumFrames = 1200)
        {
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                bool hasViews = scene.Columns.Count > 0 && scene.Columns.All(column => column.Views.Count > 0);
                if (hasViews && scene.IsGeneratorUpToDate && !scene.SceneInformation.CutsNeedUpdate)
                {
                    return;
                }

                await UniTask.NextFrame();
            }

            throw new TimeoutException($"Scene '{scene.Name}' did not become render-ready after {maximumFrames} frames.");
        }

        private static RenderingBaselineReport CreateReport(Base3DScene scene, string projectPath)
        {
            string runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            RenderingBaselineReport report = new()
            {
                RunId = runId,
                CreatedUtc = DateTime.UtcNow.ToString("O"),
                ProjectPath = Path.GetFullPath(projectPath),
                Visualization = scene.Name,
                OutputDirectory = Path.Combine(GetOutputRoot(), runId),
                Runtime = CaptureRuntimeConfiguration(),
                Scene = CaptureSceneConfiguration(scene)
            };
            return report;
        }

        private static string GetOutputRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string pipelineDirectory = GraphicsSettings.currentRenderPipeline == null ? "baseline-birp" : "urp-phase4";
            string outputRoot = Path.Combine(projectRoot, ".test-results", "rendering", pipelineDirectory);
            Directory.CreateDirectory(outputRoot);
            return outputRoot;
        }

        private static RuntimeConfiguration CaptureRuntimeConfiguration()
        {
            int qualityLevel = QualitySettings.GetQualityLevel();
            return new RuntimeConfiguration
            {
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                OperatingSystem = SystemInfo.operatingSystem,
                GraphicsDeviceName = SystemInfo.graphicsDeviceName,
                GraphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                GraphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                GraphicsMemorySizeMb = SystemInfo.graphicsMemorySize,
                ColorSpace = QualitySettings.activeColorSpace.ToString(),
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                QualityLevel = qualityLevel,
                QualityName = QualitySettings.names.ElementAtOrDefault(qualityLevel),
                VSyncCount = QualitySettings.vSyncCount,
                TargetFrameRate = Application.targetFrameRate,
                RenderPipeline = GraphicsSettings.currentRenderPipeline == null ? "Built-in Render Pipeline" : GraphicsSettings.currentRenderPipeline.name
            };
        }

        private static SceneConfiguration CaptureSceneConfiguration(Base3DScene scene)
        {
            SceneConfiguration result = new()
            {
                Name = scene.Name,
                ColumnCount = scene.Columns.Count,
                ViewCount = scene.Columns.Sum(column => column.Views.Count),
                CutCount = scene.Cuts.Count,
                TotalSiteCount = scene.Columns.Sum(column => column.Sites.Count),
                BrainTransparent = scene.IsBrainTransparent,
                BrainAlpha = scene.BrainMaterials.Alpha,
                Edges = scene.EdgeMode,
                StrongCuts = scene.StrongCuts,
                ShowAllSites = scene.ShowAllSites,
                BrainColor = scene.BrainColor.ToString(),
                CutColor = scene.CutColor.ToString(),
                Colormap = scene.Colormap.ToString()
            };

            for (int columnIndex = 0; columnIndex < scene.Columns.Count; columnIndex++)
            {
                Column3D column = scene.Columns[columnIndex];
                ColumnConfiguration columnConfiguration = new()
                {
                    Index = columnIndex,
                    Name = column.Name,
                    Type = column.GetType().Name,
                    Layer = column.Layer,
                    SiteCount = column.Sites.Count,
                    ActivityAlpha = column.ActivityAlpha
                };

                for (int viewIndex = 0; viewIndex < column.Views.Count; viewIndex++)
                {
                    View3D view = column.Views[viewIndex];
                    RenderTexture target = view.Camera.targetTexture;
                    columnConfiguration.Cameras.Add(new CameraConfiguration
                    {
                        ViewIndex = viewIndex,
                        Enabled = view.Camera.enabled,
                        Minimized = view.IsMinimized,
                        Edges = view.ShowEdges,
                        ClearFlags = view.Camera.clearFlags.ToString(),
                        Background = view.Camera.backgroundColor,
                        LocalPosition = view.LocalCameraPosition,
                        LocalRotation = view.LocalCameraRotation,
                        LocalTarget = view.LocalCameraTarget,
                        TargetWidth = target != null ? target.width : Screen.width,
                        TargetHeight = target != null ? target.height : Screen.height,
                        TargetFormat = target != null ? target.graphicsFormat.ToString() : "backbuffer",
                        TargetSrgb = target != null && target.sRGB
                    });
                }

                result.Columns.Add(columnConfiguration);
            }

            return result;
        }

        private static async UniTask CaptureReferenceCasesAsync(Base3DScene scene, RenderingBaselineReport report)
        {
            bool oldTransparent = scene.IsBrainTransparent;
            bool oldEdges = scene.EdgeMode;
            float oldAlpha = scene.BrainMaterials.Alpha;
            bool oldMarsAtlas = scene.AtlasManager.DisplayMarsAtlas;
            bool oldJuBrainAtlas = scene.AtlasManager.DisplayJuBrainAtlas;
            bool oldRoiCreationMode = scene.ROIManager.ROICreationMode;
            List<HBP.Core.Object3D.Cut> addedCuts = new();
            ROI temporaryRoi = null;

            try
            {
                scene.IsBrainTransparent = false;
                scene.EdgeMode = false;
                await CaptureScenarioAsync(scene, report, "anatomy_activity_sites", "opaque_edges_off", true);

                scene.EdgeMode = true;
                await CaptureScenarioAsync(scene, report, "edges", "opaque_edges_on", true);

                scene.BrainMaterials.SetAlpha(0.2f);
                scene.IsBrainTransparent = true;
                scene.EdgeMode = false;
                await CaptureScenarioAsync(scene, report, "transparency", "transparent_edges_off", true);

                scene.EdgeMode = true;
                await CaptureScenarioAsync(scene, report, "transparency_edges", "transparent_edges_on", true);

                scene.EdgeMode = false;
                scene.IsBrainTransparent = false;
                scene.AtlasManager.DisplayJuBrainAtlas = false;
                scene.AtlasManager.DisplayMarsAtlas = true;
                await WaitForUpdatesAsync(scene);
                await CaptureScenarioAsync(scene, report, "atlas", "mars_atlas", true);
                scene.AtlasManager.DisplayMarsAtlas = false;

                if (scene.Cuts.Count == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        addedCuts.Add(scene.AddCutPlane());
                    }
                }

                await WaitForUpdatesAsync(scene);
                await CaptureScenarioAsync(scene, report, "cuts", "cuts_opaque", true);
                CaptureCutTextureExports(scene, report);
                scene.IsBrainTransparent = true;
                await WaitForUpdatesAsync(scene);
                await CaptureScenarioAsync(scene, report, "cuts_transparent", "cuts_transparent", true);
                scene.IsBrainTransparent = false;
                report.SurfaceCutSamples.AddRange(CaptureSurfaceCutSamples(scene, report));

                Vector3 roiPosition = GetReferencePosition(scene);
                float roiRadius = GetReferenceRadius(scene);
                temporaryRoi = scene.ROIManager.AddROI("URP migration baseline");
                temporaryRoi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Baseline sphere", roiPosition, roiRadius);
                temporaryRoi.SelectedSphere.SetInfluenceRadius(roiRadius);
                scene.ROIManager.ROICreationMode = true;
                temporaryRoi.SelectSphere(-1);
                await WaitForUpdatesAsync(scene);
                await CaptureScenarioAsync(scene, report, "roi", "roi_wireframe", true);
                temporaryRoi.SelectSphere(0);
                await UniTask.NextFrame();
                await CaptureScenarioAsync(scene, report, "roi_selection", "roi_selected", true);

                temporaryRoi.SelectSphere(-1);
                scene.IsBrainTransparent = true;
                await WaitForUpdatesAsync(scene);
                await CaptureScenarioAsync(scene, report, "roi_transparency", "roi_through_transparent_brain", true);
                scene.IsBrainTransparent = false;

                await CaptureCompositeAndVideoAsync(scene, report);
            }
            finally
            {
                scene.ROIManager.ROICreationMode = oldRoiCreationMode;
                if (temporaryRoi != null && scene.ROIManager.ROIs.Contains(temporaryRoi))
                {
                    scene.ROIManager.SelectedROI = temporaryRoi;
                    scene.ROIManager.RemoveSelectedROI();
                }

                foreach (HBP.Core.Object3D.Cut cut in addedCuts.AsEnumerable().Reverse())
                {
                    if (scene.Cuts.Contains(cut))
                    {
                        scene.RemoveCutPlane(cut);
                    }
                }

                scene.AtlasManager.DisplayMarsAtlas = oldMarsAtlas;
                scene.AtlasManager.DisplayJuBrainAtlas = oldJuBrainAtlas;
                scene.BrainMaterials.SetAlpha(oldAlpha);
                scene.IsBrainTransparent = oldTransparent;
                scene.EdgeMode = oldEdges;
            }
        }

        private static async UniTask CaptureScenarioAsync(Base3DScene scene, RenderingBaselineReport report, string family, string scenario, bool captureIndividualExport)
        {
            await WaitForUpdatesAsync(scene);
            string scenarioDirectory = Path.Combine(report.OutputDirectory, "captures", scenario);
            Directory.CreateDirectory(scenarioDirectory);

            for (int columnIndex = 0; columnIndex < scene.Columns.Count; columnIndex++)
            {
                Column3D column = scene.Columns[columnIndex];
                for (int viewIndex = 0; viewIndex < column.Views.Count; viewIndex++)
                {
                    View3D view = column.Views[viewIndex];
                    RenderTexture target = view.Camera.targetTexture;
                    int width = target != null ? Math.Max(1, target.width) : Math.Max(1, Screen.width);
                    int height = target != null ? Math.Max(1, target.height) : Math.Max(1, Screen.height);
                    Texture2D texture = view.GetTexture(width, height, CompositeBackground);
                    string path = Path.Combine(scenarioDirectory, $"column-{columnIndex + 1}-view-{viewIndex + 1}.png");
                    SaveTexture(texture, path);
                    report.Captures.Add(CreateCaptureRecord(family, scenario, path, report.OutputDirectory, texture, false));
                    UnityEngine.Object.Destroy(texture);
                }
            }

            if (captureIndividualExport)
            {
                View3D primaryView = scene.Columns[0].Views[0];
                Texture2D export = primaryView.GetTexture(ExportSize, ExportSize, Color.clear);
                string exportPath = Path.Combine(scenarioDirectory, "individual-export-2048-transparent.png");
                SaveTexture(export, exportPath);
                CaptureRecord record = CreateCaptureRecord(family, scenario + "_individual_export", exportPath, report.OutputDirectory, export, true);
                report.Captures.Add(record);
                if (record.Alpha == null || !record.Alpha.AllCornersZero)
                {
                    report.Warnings.Add($"Transparent export background is not alpha zero: {record.Path}");
                }

                UnityEngine.Object.Destroy(export);
            }

            WriteReport(report);
        }

        private static CaptureRecord CreateCaptureRecord(string family, string scenario, string path, string outputDirectory, Texture2D texture, bool transparentBackground)
        {
            return new CaptureRecord
            {
                Family = family,
                Scenario = scenario,
                Path = MakeRelativePath(outputDirectory, path),
                Width = texture.width,
                Height = texture.height,
                TransparentBackground = transparentBackground,
                Alpha = InspectAlpha(texture)
            };
        }

        private static AlphaRecord InspectAlpha(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            int transparent = 0;
            int opaque = 0;
            int min = 255;
            int max = 0;
            foreach (Color32 pixel in pixels)
            {
                min = Math.Min(min, pixel.a);
                max = Math.Max(max, pixel.a);
                if (pixel.a == 0) transparent++;
                if (pixel.a == 255) opaque++;
            }

            byte bottomLeft = pixels[0].a;
            byte bottomRight = pixels[width - 1].a;
            byte topLeft = pixels[(height - 1) * width].a;
            byte topRight = pixels[pixels.Length - 1].a;
            return new AlphaRecord
            {
                HasAlphaChannel = true,
                AllCornersZero = bottomLeft == 0 && bottomRight == 0 && topLeft == 0 && topRight == 0,
                TransparentPixelCount = transparent,
                OpaquePixelCount = opaque,
                MinAlpha = min,
                MaxAlpha = max,
                TopLeftAlpha = topLeft,
                TopRightAlpha = topRight,
                BottomLeftAlpha = bottomLeft,
                BottomRightAlpha = bottomRight
            };
        }

        private static async UniTask WaitForUpdatesAsync(Base3DScene scene, int minimumFrames = 3, int maximumFrames = 600)
        {
            for (int i = 0; i < minimumFrames; i++)
            {
                await UniTask.NextFrame();
            }

            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (scene.IsGeneratorUpToDate && !scene.SceneInformation.CutsNeedUpdate)
                {
                    return;
                }

                await UniTask.NextFrame();
            }

            throw new TimeoutException("Rendering data did not settle before capture.");
        }

        private static List<SeamSample> CaptureSurfaceCutSamples(Base3DScene scene, RenderingBaselineReport report)
        {
            List<SeamSample> result = new();
            Column3D column = scene.Columns.FirstOrDefault();
            if (column?.BrainMesh == null || column.SurfaceGenerator == null)
            {
                report.Warnings.Add("Surface/cut samples unavailable: no generated surface.");
                return result;
            }

            Mesh mesh = column.BrainMesh.GetComponent<MeshFilter>()?.sharedMesh;
            Material material = scene.BrainMaterials.BrainMaterial;
            Texture anatomySource = material.GetTexture("_MainTex");
            Texture alphaSource = material.GetTexture("_AoTex");
            Texture colorSource = material.GetTexture("_ColorTex");
            if (mesh == null || anatomySource == null || alphaSource == null || colorSource == null)
            {
                report.Warnings.Add("Surface/cut samples unavailable: required surface textures are missing.");
                return result;
            }

            Texture2D anatomyTexture = CopyTextureToLinearReadable(anatomySource);
            Texture2D alphaTexture = CopyTextureToLinearReadable(alphaSource);
            Texture2D colorTexture = CopyTextureToLinearReadable(colorSource);
            Vector3[] vertices = mesh.vertices;
            Vector2[] anatomyUvs = mesh.uv;
            Vector2[] alphaUvs = column.SurfaceGenerator.AlphaUV;
            Vector2[] colorUvs = column.SurfaceGenerator.ActivityUV;
            Color materialTint = material.GetColor("_Color");
            try
            {
                int cutCount = Math.Min(scene.Cuts.Count, column.CutTextures.BrainCutTextures.Count);
                for (int cutIndex = 0; cutIndex < cutCount; cutIndex++)
                {
                    HBP.Core.Object3D.Cut cut = scene.Cuts[cutIndex];
                    Texture2D cutTexture = column.CutTextures.BrainCutTextures[cutIndex];
                    CutGeometryGenerator geometry = column.CutTextures.CutGenerators[cutIndex].CutGeometryGenerator;
                    if (cutTexture == null || geometry == null || !cutTexture.isReadable)
                    {
                        report.Warnings.Add($"Surface/cut samples unavailable for cut {cutIndex}: cut texture is not readable.");
                        continue;
                    }

                    List<(int Index, float Distance, Vector2 CutUv)> candidates = new();
                    int vertexCount = Math.Min(vertices.Length, Math.Min(anatomyUvs.Length, Math.Min(alphaUvs.Length, colorUvs.Length)));
                    for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                    {
                        Vector3 localPosition = vertices[vertexIndex];
                        float normalMagnitude = cut.Normal.magnitude;
                        if (normalMagnitude <= Mathf.Epsilon) continue;
                        float distance = Mathf.Abs(Vector3.Dot(localPosition - cut.Point, cut.Normal)) / normalMagnitude;
                        Vector2 cutUv = geometry.GetPositionRatioOnTexture(localPosition);
                        if (cutUv.x < 0 || cutUv.x > 1 || cutUv.y < 0 || cutUv.y > 1) continue;
                        candidates.Add((vertexIndex, distance, cutUv));
                    }

                    candidates.Sort((left, right) => left.Distance.CompareTo(right.Distance));
                    int samples = Math.Min(5, candidates.Count);
                    Texture2D readableCutTexture = CopyTextureToLinearReadable(cutTexture);
                    try
                    {
                        for (int sampleIndex = 0; sampleIndex < samples; sampleIndex++)
                        {
                            (int vertexIndex, float distance, Vector2 cutUv) = candidates[sampleIndex];
                            Vector2 cutTextureUv = new(cutUv.y, 1.0f - cutUv.x - 0.005f);
                            Color surfaceAnatomy = anatomyTexture.GetPixelBilinear(anatomyUvs[vertexIndex].x, anatomyUvs[vertexIndex].y) * materialTint;
                            Color surfaceAlpha = alphaTexture.GetPixelBilinear(alphaUvs[vertexIndex].x, alphaUvs[vertexIndex].y);
                            Color surfaceColor = colorTexture.GetPixelBilinear(colorUvs[vertexIndex].x, colorUvs[vertexIndex].y);
                            Color cutColor = readableCutTexture.GetPixelBilinear(cutTextureUv.x, cutTextureUv.y);
                            float legacyBoostedAlpha = Mathf.Clamp01(surfaceAlpha.r * 2.5f);
                            float effectiveSurfaceAlpha = GraphicsSettings.currentRenderPipeline == null ? legacyBoostedAlpha : Mathf.Clamp01(surfaceAlpha.r);
                            float surfaceTransparency = 1.0f - effectiveSurfaceAlpha;
                            float paletteWeightedAlpha = 1.0f - surfaceTransparency * surfaceTransparency;
                            Color surfaceComposed = Color.Lerp(surfaceAnatomy, surfaceColor, paletteWeightedAlpha);
                            result.Add(new SeamSample
                            {
                                CutIndex = cutIndex,
                                CutOrientation = cut.Orientation.ToString(),
                                VertexIndex = vertexIndex,
                                LocalPosition = vertices[vertexIndex],
                                DistanceToCut = distance,
                                CutUv = cutUv,
                                CutTextureUv = cutTextureUv,
                                SurfaceAnatomyUv = anatomyUvs[vertexIndex],
                                SurfaceAlphaUv = alphaUvs[vertexIndex],
                                SurfaceColorUv = colorUvs[vertexIndex],
                                SurfaceAnatomySample = surfaceAnatomy,
                                SurfaceAlphaSample = surfaceAlpha,
                                SurfaceBoostedAlpha = legacyBoostedAlpha,
                                SurfaceEffectiveAlpha = effectiveSurfaceAlpha,
                                SurfaceColormapSample = surfaceColor,
                                SurfaceComposedSample = surfaceComposed,
                                CutSample = cutColor,
                                ColormapRgbDistance = RgbDistance(surfaceColor, cutColor),
                                RgbDistance = RgbDistance(surfaceComposed, cutColor),
                                AlphaDistance = Mathf.Abs(effectiveSurfaceAlpha - cutColor.a)
                            });
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.Destroy(readableCutTexture);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(anatomyTexture);
                UnityEngine.Object.Destroy(alphaTexture);
                UnityEngine.Object.Destroy(colorTexture);
            }

            return result;
        }

        private static float RgbDistance(Color left, Color right)
        {
            return Mathf.Sqrt(Mathf.Pow(left.r - right.r, 2) + Mathf.Pow(left.g - right.g, 2) + Mathf.Pow(left.b - right.b, 2));
        }

        private static Texture2D CopyTextureToLinearReadable(Texture source)
        {
            RenderTexture oldActive = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                Texture2D readable = new(source.width, source.height, TextureFormat.RGBA32, false, true);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                readable.Apply(false, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static Vector3 GetReferencePosition(Base3DScene scene)
        {
            HBP.Core.Object3D.Site selectedSite = scene.SelectedColumn?.SelectedSite;
            if (selectedSite != null)
            {
                return selectedSite.transform.localPosition;
            }

            Renderer renderer = scene.Columns.First().BrainMesh.GetComponent<Renderer>();
            return scene.transform.InverseTransformPoint(renderer.bounds.center);
        }

        private static float GetReferenceRadius(Base3DScene scene)
        {
            Renderer renderer = scene.Columns.First().BrainMesh.GetComponent<Renderer>();
            return Mathf.Max(2f, renderer.bounds.extents.magnitude * 0.08f);
        }

        private static void CaptureCutTextureExports(Base3DScene scene, RenderingBaselineReport report)
        {
            Column3D column = scene.Columns.First();
            int count = Math.Min(scene.Cuts.Count, column.CutTextures.GUIBrainCutTextures.Count);
            for (int index = 0; index < count; index++)
            {
                Texture2D texture = column.CutTextures.GUIBrainCutTextures[index];
                if (texture == null || texture.width <= 1 || texture.height <= 1) continue;
                string path = Path.Combine(report.OutputDirectory, "exports", "cuts", $"{index + 1}-{scene.Cuts[index].Orientation}.png");
                SaveTexture(texture, path);
                report.Captures.Add(CreateCaptureRecord("cut_png_export", "cut_" + scene.Cuts[index].Orientation, path, report.OutputDirectory, texture, false));
            }

            WriteReport(report);
        }

        private static async UniTask CaptureCompositeAndVideoAsync(Base3DScene scene, RenderingBaselineReport report)
        {
            Texture2D composite = CreateComposite(scene, 1920, 1080);
            string compositePath = Path.Combine(report.OutputDirectory, "exports", "composite-1920x1080.png");
            SaveTexture(composite, compositePath);
            report.Captures.Add(CreateCaptureRecord("composite_export", "composite_1920x1080", compositePath, report.OutputDirectory, composite, false));

            string videoPath = Path.Combine(report.OutputDirectory, "exports", "video-smoke-10-frames.avi");
            Directory.CreateDirectory(Path.GetDirectoryName(videoPath));
            using (VideoStream stream = new())
            {
                stream.Open(videoPath, composite.width, composite.height, 10f);
                for (int frame = 0; frame < 10; frame++)
                {
                    stream.WriteFrame(composite);
                }
            }

            report.Captures.Add(new CaptureRecord
            {
                Family = "video_export",
                Scenario = "video_smoke_10_frames",
                Path = MakeRelativePath(report.OutputDirectory, videoPath),
                Width = composite.width,
                Height = composite.height,
                TransparentBackground = false
            });
            UnityEngine.Object.Destroy(composite);

            await UniTask.WaitForEndOfFrame();
            if (Module3DUI.Scenes.TryGetValue(scene, out Scene3DWindow sceneWindow))
            {
                Rect screenRect = sceneWindow.GetComponent<RectTransform>().ToScreenSpace();
                if (screenRect.width > 0 && screenRect.height > 0)
                {
                    Texture2D fullScene = Texture2DExtension.ScreenRectToTexture(screenRect);
                    string fullScenePath = Path.Combine(report.OutputDirectory, "exports", "full-scene-ui.png");
                    SaveTexture(fullScene, fullScenePath);
                    report.Captures.Add(CreateCaptureRecord("full_scene_ui_export", "full_scene_ui", fullScenePath, report.OutputDirectory, fullScene, false));
                    UnityEngine.Object.Destroy(fullScene);
                }
            }
            else
            {
                report.Warnings.Add("Full-scene UI export skipped: Scene3DWindow was not found.");
            }

            WriteReport(report);
        }

        private static Texture2D CreateComposite(Base3DScene scene, int width, int height)
        {
            int columns = Math.Max(1, scene.Columns.Count);
            int rows = Math.Max(1, scene.ViewLineNumber);
            int cellWidth = width / columns;
            int cellHeight = height / rows;
            Texture2D composite = new(width, height, TextureFormat.RGBA32, false, false);
            Color[] background = Enumerable.Repeat(CompositeBackground, width * height).ToArray();
            composite.SetPixels(background);

            for (int columnIndex = 0; columnIndex < scene.Columns.Count; columnIndex++)
            {
                for (int viewIndex = 0; viewIndex < scene.Columns[columnIndex].Views.Count; viewIndex++)
                {
                    Texture2D cell = scene.Columns[columnIndex].Views[viewIndex].GetTexture(cellWidth, cellHeight, CompositeBackground);
                    int y = (rows - viewIndex - 1) * cellHeight;
                    composite.SetPixels(columnIndex * cellWidth, y, cellWidth, cellHeight, cell.GetPixels());
                    UnityEngine.Object.Destroy(cell);
                }
            }

            composite.Apply(false, false);
            return composite;
        }

        private static async UniTask<PerformanceRecord> SamplePerformanceAsync(string scenario, Base3DScene scene, int warmupFrames, int sampleFrames, int renderedSiteCount, SiteInventory siteInventory, string note = null)
        {
            bool focusedAtStart = Application.isFocused;
            int oldVSyncCount = QualitySettings.vSyncCount;
            int oldTargetFrameRate = Application.targetFrameRate;
            bool oldRunInBackground = Application.runInBackground;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;

            ProfilerRecorder cpuMain = StartRecorder("CPU Main Thread Frame Time", ProfilerCategory.Internal, ProfilerCategory.Render);
            ProfilerRecorder cpuRender = StartRecorder("CPU Render Thread Frame Time", ProfilerCategory.Render, ProfilerCategory.Internal);
            ProfilerRecorder gpu = StartRecorder("GPU Frame Time", ProfilerCategory.Render, ProfilerCategory.Internal);
            ProfilerRecorder drawCalls = StartRecorder("Draw Calls Count", ProfilerCategory.Render);
            ProfilerRecorder setPass = StartRecorder("SetPass Calls Count", ProfilerCategory.Render);
            ProfilerRecorder triangles = StartRecorder("Triangles Count", ProfilerCategory.Render);
            ProfilerRecorder vertices = StartRecorder("Vertices Count", ProfilerCategory.Render);

            List<double> frameIntervals = new(sampleFrames);
            List<double> cpuMainValues = new(sampleFrames);
            List<double> cpuRenderValues = new(sampleFrames);
            List<double> gpuValues = new(sampleFrames);
            List<double> drawCallValues = new(sampleFrames);
            List<double> setPassValues = new(sampleFrames);
            List<double> triangleValues = new(sampleFrames);
            List<double> vertexValues = new(sampleFrames);

            try
            {
                for (int frame = 0; frame < warmupFrames; frame++)
                {
                    await UniTask.NextFrame();
                }

                for (int frame = 0; frame < sampleFrames; frame++)
                {
                    double start = Time.realtimeSinceStartupAsDouble;
                    await UniTask.NextFrame();
                    frameIntervals.Add((Time.realtimeSinceStartupAsDouble - start) * 1000.0);
                    AddRecorderValue(cpuMain, cpuMainValues, 1e-6);
                    AddRecorderValue(cpuRender, cpuRenderValues, 1e-6);
                    AddRecorderValue(gpu, gpuValues, 1e-6);
                    AddRecorderValue(drawCalls, drawCallValues, 1.0);
                    AddRecorderValue(setPass, setPassValues, 1.0);
                    AddRecorderValue(triangles, triangleValues, 1.0);
                    AddRecorderValue(vertices, vertexValues, 1.0);
                }
            }
            finally
            {
                cpuMain.Dispose();
                cpuRender.Dispose();
                gpu.Dispose();
                drawCalls.Dispose();
                setPass.Dispose();
                triangles.Dispose();
                vertices.Dispose();
                QualitySettings.vSyncCount = oldVSyncCount;
                Application.targetFrameRate = oldTargetFrameRate;
                Application.runInBackground = oldRunInBackground;
            }

            MetricStatistics frameIntervalStatistics = MetricStatistics.From(frameIntervals, "ms");
            MetricStatistics cpuMainStatistics = MetricStatistics.From(cpuMainValues, "ms");
            MetricStatistics cpuRenderStatistics = MetricStatistics.From(cpuRenderValues, "ms");
            MetricStatistics gpuStatistics = MetricStatistics.From(gpuValues, "ms");
            bool idleThrottled = frameIntervalStatistics.Available && frameIntervalStatistics.Median >= 250.0;
            bool focusedAtEnd = Application.isFocused;
            return new PerformanceRecord
            {
                Scenario = scenario,
                ApplicationFocusedAtStart = focusedAtStart,
                ApplicationFocusedAtEnd = focusedAtEnd,
                IdleThrottled = idleThrottled,
                Normative = !idleThrottled && cpuMainStatistics.Available && gpuStatistics.Available,
                Note = (idleThrottled ? "Non-normative: Unity Editor idle throttling detected from a median frame interval >= 250 ms." : "Normative profiler sample after warm-up.") + (string.IsNullOrWhiteSpace(note) ? string.Empty : " " + note),
                WarmupFrames = warmupFrames,
                SampleFrames = sampleFrames,
                ColumnCount = scene.Columns.Count,
                EnabledViewCount = scene.Columns.Sum(column => column.Views.Count(view => view.Camera.enabled)),
                RenderedSiteCount = renderedSiteCount,
                SiteGameObjectCount = siteInventory.GameObjectCount,
                SiteRendererCount = siteInventory.RendererCount,
                SiteColliderCount = siteInventory.ColliderCount,
                UniqueSiteMaterialCount = siteInventory.UniqueMaterialCount,
                FrameIntervalMs = frameIntervalStatistics,
                CpuMainThreadMs = cpuMainStatistics,
                CpuRenderThreadMs = cpuRenderStatistics,
                GpuFrameMs = gpuStatistics,
                DrawCalls = MetricStatistics.From(drawCallValues, "count"),
                SetPassCalls = MetricStatistics.From(setPassValues, "count"),
                Triangles = MetricStatistics.From(triangleValues, "count"),
                Vertices = MetricStatistics.From(vertexValues, "count")
            };
        }

        private static SiteInventory CaptureSiteInventory(IEnumerable<GameObject> siteObjects)
        {
            GameObject[] objects = siteObjects.Where(site => site != null).Distinct().ToArray();
            MeshRenderer[] renderers = objects.Select(site => site.GetComponent<MeshRenderer>()).Where(renderer => renderer != null).ToArray();
            int colliderCount = objects.Count(site => site.GetComponent<Collider>() != null);
            int uniqueMaterialCount = renderers.SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().Count();
            return new SiteInventory(objects.Length, renderers.Length, colliderCount, uniqueMaterialCount);
        }

        private static ProfilerRecorder StartRecorder(string name, params ProfilerCategory[] categories)
        {
            foreach (ProfilerCategory category in categories)
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, name, 1, ProfilerRecorderOptions.Default);
                if (recorder.Valid)
                {
                    return recorder;
                }

                recorder.Dispose();
            }

            return default;
        }

        private static void AddRecorderValue(ProfilerRecorder recorder, List<double> values, double scale)
        {
            if (recorder.Valid && recorder.LastValue > 0)
            {
                values.Add(recorder.LastValue * scale);
            }
        }

        private static async UniTask<PerformanceRecord> SampleSiteStressAsync(Base3DScene scene, RenderingBaselineReport report)
        {
            Column3D column = scene.Columns[0];
            View3D primaryView = column.Views[0];
            MeshFilter sourceFilter = column.Sites.Select(site => site.GetComponent<MeshFilter>()).FirstOrDefault(filter => filter != null);
            MeshRenderer sourceRenderer = column.Sites.Select(site => site.GetComponent<MeshRenderer>()).FirstOrDefault(renderer => renderer != null);
            if (sourceFilter == null || sourceRenderer == null)
            {
                report.Warnings.Add("30,000-site baseline skipped: no site MeshFilter/MeshRenderer was found.");
                return new PerformanceRecord { Scenario = "sites_30000_1x1_skipped" };
            }

            Dictionary<Camera, bool> cameraStates = scene.Columns.SelectMany(item => item.Views).Select(view => view.Camera).Distinct().ToDictionary(camera => camera, camera => camera.enabled);
            GameObject root = new("Rendering baseline - temporary sites");
            root.transform.SetParent(column.transform, false);
            root.SetActive(false);
            int existingSites = column.Sites.Count;
            int additionalSites = Math.Max(0, SiteStressTarget - existingSites);
            Bounds bounds = column.BrainMesh.GetComponent<Renderer>().bounds;
            Vector3 localCenter = column.transform.InverseTransformPoint(bounds.center);
            float radius = bounds.extents.magnitude * 0.6f;
            const float goldenAngle = 2.39996323f;

            try
            {
                foreach (Camera camera in cameraStates.Keys)
                {
                    camera.enabled = camera == primaryView.Camera;
                }

                for (int index = 0; index < additionalSites; index++)
                {
                    float normalized = (index + 0.5f) / additionalSites;
                    float y = 1f - 2f * normalized;
                    float radial = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                    float angle = goldenAngle * index;
                    Vector3 direction = new(Mathf.Cos(angle) * radial, y, Mathf.Sin(angle) * radial);
                    GameObject site = new("Baseline site");
                    site.layer = sourceRenderer.gameObject.layer;
                    site.transform.SetParent(root.transform, false);
                    site.transform.localPosition = localCenter + direction * radius;
                    site.transform.localScale = sourceRenderer.transform.lossyScale;
                    MeshFilter filter = site.AddComponent<MeshFilter>();
                    filter.sharedMesh = sourceFilter.sharedMesh;
                    MeshRenderer renderer = site.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                    if ((index + 1) % 500 == 0)
                    {
                        await UniTask.NextFrame();
                    }
                }

                root.SetActive(true);
                await UniTask.NextFrame();
                IEnumerable<GameObject> stressSites = column.Sites.Select(site => site.gameObject).Concat(root.GetComponentsInChildren<MeshRenderer>(true).Select(renderer => renderer.gameObject));
                SiteInventory stressInventory = CaptureSiteInventory(stressSites);
                PerformanceRecord performance = await SamplePerformanceAsync("sites_30000_1x1", scene, SiteStressWarmupFrames, SiteStressSampleFrames, SiteStressTarget, stressInventory, "The 30,000-site fixture intentionally excludes colliders from temporary sites to isolate rendering cost.");

                Texture2D capture = primaryView.GetTexture(ExportSize, ExportSize, Color.clear);
                string path = Path.Combine(report.OutputDirectory, "captures", "sites-30000-1x1.png");
                SaveTexture(capture, path);
                report.Captures.Add(CreateCaptureRecord("sites_stress", "sites_30000_1x1", path, report.OutputDirectory, capture, true));
                UnityEngine.Object.Destroy(capture);
                return performance;
            }
            finally
            {
                foreach ((Camera camera, bool enabled) in cameraStates)
                {
                    if (camera != null) camera.enabled = enabled;
                }

                UnityEngine.Object.Destroy(root);
                await UniTask.NextFrame();
            }
        }

        private static PatchFixtureRecord CapturePatchFixture(string outputDirectory)
        {
            const int width = 512;
            const int height = 256;
            GameObject cameraObject = new("Rendering baseline patch camera");
            GameObject quadObject = new("Rendering baseline patch quad");
            Material material = null;
            Mesh mesh = null;
            Texture2D srgbTexture = null;
            Texture2D linearTexture = null;
            RenderTexture renderTexture = null;
            Texture2D readback = null;
            RenderTexture oldActive = RenderTexture.active;

            try
            {
                int layer = 31;
                cameraObject.layer = layer;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 0.5f;
                camera.transform.position = new Vector3(0, 0, -1);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << layer;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.enabled = false;

                quadObject.layer = layer;
                mesh = CreatePatchMesh();
                quadObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = quadObject.AddComponent<MeshRenderer>();
                Shader shader = Shader.Find("Hidden/HBP/Dev/RenderingBaselinePatches");
                if (shader == null)
                {
                    throw new InvalidOperationException("Rendering baseline patch shader was not found.");
                }

                material = new Material(shader);
                srgbTexture = CreatePatchTexture(false);
                linearTexture = CreatePatchTexture(true);
                material.SetColor("_UniformColor", new Color(0.25f, 0.5f, 0.75f, 1f));
                material.SetTexture("_SrgbTexture", srgbTexture);
                material.SetTexture("_LinearTexture", linearTexture);
                renderer.sharedMaterial = material;

                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Point
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.aspect = (float)width / height;
                camera.Render();

                RenderTexture.active = renderTexture;
                readback = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                readback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                readback.Apply(false, false);

                string path = Path.Combine(outputDirectory, "fixtures", "color-alpha-patches.png");
                SaveTexture(readback, path);
                PatchFixtureRecord record = new()
                {
                    Path = MakeRelativePath(outputDirectory, path),
                    Width = width,
                    Height = height
                };
                string[] names = { "uniform", "srgb_texture", "linear_texture", "vertex_color" };
                int[] xs = { 64, 192, 320, 448 };
                for (int index = 0; index < names.Length; index++)
                {
                    AddPatchSample(record, readback, names[index] + "_alpha_0_5", xs[index], 64);
                    AddPatchSample(record, readback, names[index] + "_opaque", xs[index], 192);
                }

                return record;
            }
            finally
            {
                RenderTexture.active = oldActive;
                if (renderTexture != null) renderTexture.Release();
                UnityEngine.Object.Destroy(readback);
                UnityEngine.Object.Destroy(renderTexture);
                UnityEngine.Object.Destroy(srgbTexture);
                UnityEngine.Object.Destroy(linearTexture);
                UnityEngine.Object.Destroy(material);
                UnityEngine.Object.Destroy(mesh);
                UnityEngine.Object.Destroy(quadObject);
                UnityEngine.Object.Destroy(cameraObject);
            }
        }

        private static Mesh CreatePatchMesh()
        {
            Mesh mesh = new() { name = "Rendering baseline patch mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-1f, -0.5f, 0),
                new Vector3(1f, -0.5f, 0),
                new Vector3(1f, 0.5f, 0),
                new Vector3(-1f, 0.5f, 0)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            Color32 vertexColor = new(64, 128, 192, 255);
            mesh.colors32 = new[] { vertexColor, vertexColor, vertexColor, vertexColor };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D CreatePatchTexture(bool linear)
        {
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false, linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, new Color32(64, 128, 192, 255));
            texture.Apply(false, false);
            return texture;
        }

        private static void AddPatchSample(PatchFixtureRecord record, Texture2D texture, string name, int x, int y)
        {
            record.Samples.Add(new PatchSample
            {
                Name = name,
                Pixel = new Vector2Int(x, y),
                Value = texture.GetPixel(x, y)
            });
        }

        private static void SaveTexture(Texture2D texture, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static void WriteReport(RenderingBaselineReport report)
        {
            Directory.CreateDirectory(report.OutputDirectory);
            string path = Path.Combine(report.OutputDirectory, "manifest.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }

        private static string MakeRelativePath(string root, string path)
        {
            Uri rootUri = new(AppendDirectorySeparator(root));
            Uri pathUri = new(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? path : path + Path.DirectorySeparatorChar;
        }

        private sealed class SiteInventory
        {
            public SiteInventory(int gameObjectCount, int rendererCount, int colliderCount, int uniqueMaterialCount)
            {
                GameObjectCount = gameObjectCount;
                RendererCount = rendererCount;
                ColliderCount = colliderCount;
                UniqueMaterialCount = uniqueMaterialCount;
            }

            public int GameObjectCount { get; }
            public int RendererCount { get; }
            public int ColliderCount { get; }
            public int UniqueMaterialCount { get; }
        }
    }
}
