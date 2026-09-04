using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.Bootstrap.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CRNL.HiBoP.XR.StaticRendering.Editor
{
    public static class P05ProjectSetup
    {
        public const string PrefabPath = "Assets/HiBoPXR/StaticRendering/Prefabs/P05StaticSurfaceDemo.prefab";
        public const string ScenePath = "Assets/HiBoPXR/StaticRendering/Scenes/P05StaticSurface.unity";
        public const string OpaqueMaterialPath = "Assets/HiBoPXR/StaticRendering/Materials/P05SurfaceOpaque.mat";
        public const string TransparentMaterialPath = "Assets/HiBoPXR/StaticRendering/Materials/P05SurfaceTransparent.mat";
        public const string TransparentDepthMaterialPath = "Assets/HiBoPXR/StaticRendering/Materials/P05SurfaceTransparentDepth.mat";
        public const string PipelinePath = "Assets/HiBoPXR/StaticRendering/Settings/P05QuestURP.asset";
        public const string RendererPath = "Assets/HiBoPXR/StaticRendering/Settings/P05QuestRenderer.asset";
        public const string AnatomicalDataPath = "Assets/HiBoPXR/StaticRendering/Data/P05D1Anatomical.bytes";
        public const string InflatedDataPath = "Assets/HiBoPXR/StaticRendering/Data/P05D1Inflated.bytes";

        private const string OpaqueShaderName = "HiBoP XR/P05/Surface Opaque";
        private const string TransparentShaderName = "HiBoP XR/P05/Surface Transparent";
        private const string TransparentDepthShaderName = "HiBoP XR/P05/Surface Transparent Depth";

        [MenuItem("HiBoP XR/P05/Apply Static Surface Renderer")]
        public static void Apply()
        {
            ConfigurePipeline();
            P04ProjectSetup.Validate();
            CreateMaterials();
            CreatePrefabAndScene();
            Validate();
            AssetDatabase.SaveAssets();
            Debug.Log("P05 static surface renderer configured and validated.");
        }

        [MenuItem("HiBoP XR/P05/Validate Static Surface Renderer")]
        public static void Validate()
        {
            P04ProjectSetup.Validate();
            var failures = new List<string>();
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            Check(pipeline != null, "URP asset", failures);
            Check(renderer != null, "Universal renderer data", failures);
            Check(GraphicsSettings.defaultRenderPipeline == pipeline, "URP default pipeline", failures);
            Check(QualitySettings.renderPipeline == pipeline, "URP quality pipeline", failures);
            Check(QualitySettings.activeColorSpace == ColorSpace.Linear, "linear color space", failures);
            Check(PlayerSettings.preserveFramebufferAlpha, "framebuffer alpha preservation for MR composition", failures);
            Check(pipeline != null && pipeline.allowPostProcessAlphaOutput, "URP alpha output", failures);
            Check(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.Vulkan }), "Vulkan-only Android", failures);

            ValidateShader(OpaqueShaderName, failures);
            ValidateShader(TransparentShaderName, failures);
            ValidateShader(TransparentDepthShaderName, failures);
            Material opaque = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMaterialPath);
            Material transparent = AssetDatabase.LoadAssetAtPath<Material>(TransparentMaterialPath);
            Material transparentDepth = AssetDatabase.LoadAssetAtPath<Material>(TransparentDepthMaterialPath);
            Check(opaque != null && opaque.shader != null && opaque.shader.name == OpaqueShaderName && opaque.renderQueue == (int)RenderQueue.Geometry, "opaque reference material", failures);
            Check(transparent != null && transparent.shader != null && transparent.shader.name == TransparentShaderName && transparent.renderQueue == (int)RenderQueue.Transparent, "transparent reference material", failures);
            Check(transparentDepth != null && transparentDepth.shader != null && transparentDepth.shader.name == TransparentDepthShaderName && transparentDepth.renderQueue == (int)RenderQueue.Transparent - 1, "transparent depth-prepass material", failures);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            TextAsset anatomicalData = AssetDatabase.LoadAssetAtPath<TextAsset>(AnatomicalDataPath);
            TextAsset inflatedData = AssetDatabase.LoadAssetAtPath<TextAsset>(InflatedDataPath);
            Check(prefab != null, "P05 prefab", failures);
            Check(anatomicalData != null, "local GIFTI-derived anatomical SurfaceAsset", failures);
            Check(inflatedData != null, "local GIFTI-derived inflated SurfaceAsset", failures);
            Check(prefab != null && prefab.GetComponent<P05LocalSurfaceBootstrap>() != null, "local SurfaceAsset source", failures);
            Check(prefab != null && prefab.GetComponentsInChildren<P05StaticSurfaceRenderer>(true).Length == 2, "two serialized surface renderers", failures);
            Check(prefab != null && prefab.GetComponent<P05DeviceProfiler>() != null, "device profiler", failures);
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null, "P05 scene", failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("P05 validation failed: " + string.Join(", ", failures));
            }
        }

        public static void BuildAndroid()
        {
            string outputPath = GetArgument("-p05BuildOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Missing -p05BuildOutput.");
            }

            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            Apply();
            PlayerSettings.productName = "HiBoP XR P05";
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"P05 Android build failed with result {report.summary.result}.");
            }

            WriteBuildEvidence(outputPath, report);
        }

        public static void CaptureSyntheticGolden()
        {
            Apply();
            string outputDirectory = GetArgument("-p05GoldenOutput");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Missing -p05GoldenOutput.");
            }

            outputDirectory = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(outputDirectory);
            GoldenCapture reference = RenderSyntheticGolden();
            GoldenCapture candidate = RenderSyntheticGolden();
            string referencePath = Path.Combine(outputDirectory, "synthetic-reference.png");
            string candidatePath = Path.Combine(outputDirectory, "synthetic-candidate.png");
            File.WriteAllBytes(referencePath, reference.Png);
            File.WriteAllBytes(candidatePath, candidate.Png);

            const float specular = 0.36f * 0.45f;
            Color authoredSrgb = new(0.25f, 0.50f, 0.75f, 1f);
            Color expectedLinear = authoredSrgb.linear + new Color(specular, specular, specular, 0f);
            float maximumError = Mathf.Max(Mathf.Abs(reference.CenterLinear.r - expectedLinear.r), Mathf.Abs(reference.CenterLinear.g - expectedLinear.g), Mathf.Abs(reference.CenterLinear.b - expectedLinear.b));
            string referenceHash = ComputeHash(reference.Png);
            string candidateHash = ComputeHash(candidate.Png);
            if (referenceHash != candidateHash)
            {
                throw new InvalidOperationException("P05 synthetic golden rendering is not deterministic.");
            }

            if (maximumError > 0.01f)
            {
                throw new InvalidOperationException($"P05 shader diverged from the Desktop static-lighting formula by {maximumError}.");
            }

            var evidence = new GoldenEvidence
            {
                schema = "P05-synthetic-golden-v1",
                sourceSurfaceHash = "19149b6a21d4f9df69bd500deacae220caeafb4f480c410de6021c6c7d0e5ea1",
                width = reference.Width,
                height = reference.Height,
                referenceSha256 = referenceHash,
                candidateSha256 = candidateHash,
                equal = true,
                expectedCenterLinear = ToArray(expectedLinear),
                actualCenterLinear = ToArray(reference.CenterLinear),
                maximumCenterAbsoluteError = maximumError,
                tolerance = 0.01f,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                colorSpace = QualitySettings.activeColorSpace.ToString(),
            };
            File.WriteAllText(Path.Combine(outputDirectory, "golden-evidence.json"), JsonUtility.ToJson(evidence, true));
            Debug.Log($"P05 synthetic golden PASS | sha256={referenceHash} maxError={maximumError}");
        }

        public static void CaptureD1Golden()
        {
            Apply();
            string outputDirectory = GetArgument("-p05GoldenOutput");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Missing -p05GoldenOutput.");
            }

            outputDirectory = Path.GetFullPath(outputDirectory);
            SurfaceAsset anatomical = P05SurfaceAssetBinary.Read(AssetDatabase.LoadAssetAtPath<TextAsset>(AnatomicalDataPath));
            SurfaceAsset inflated = P05SurfaceAssetBinary.Read(AssetDatabase.LoadAssetAtPath<TextAsset>(InflatedDataPath));
            var comparisons = new List<D1GoldenComparison>();
            CaptureD1Views(outputDirectory, anatomical, "anatomical", comparisons);
            CaptureD1Views(outputDirectory, inflated, "inflated", comparisons);
            D1TransparentBlendEvidence transparentBlend = CaptureD1TransparentGolden(outputDirectory, inflated);

            bool passed = comparisons.All(comparison => comparison.passed) && transparentBlend.passed;
            var evidence = new D1GoldenEvidence
            {
                schema = "P05-d1-golden-v1",
                desktopShader = "HBP/Brain",
                xrShader = OpaqueShaderName,
                anatomicalSurfaceHash = anatomical.Hash.ToString(),
                inflatedSurfaceHash = inflated.Hash.ToString(),
                width = 512,
                height = 512,
                maximumChannelErrorTolerance = 8f / 255f,
                meanChannelErrorTolerance = 0.003f,
                p99ChannelErrorTolerance = 0.02f,
                minimumForegroundIou = 0.995f,
                minimumForegroundFraction = 0.01f,
                passed = passed,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                colorSpace = QualitySettings.activeColorSpace.ToString(),
                comparisons = comparisons.ToArray(),
                transparentBlend = transparentBlend,
            };
            File.WriteAllText(Path.Combine(outputDirectory, "d1-golden-evidence.json"), JsonUtility.ToJson(evidence, true));
            if (!passed)
            {
                throw new InvalidOperationException("P05 D1 Desktop/XR golden comparison exceeded its tolerances.");
            }

            Debug.Log("P05 D1 Desktop/XR golden comparison PASS (6/6 views).");
        }

        private static void CaptureD1Views(string outputDirectory, SurfaceAsset asset, string representation, ICollection<D1GoldenComparison> comparisons)
        {
            CompareD1View(outputDirectory, asset, representation, "front", Vector3.forward, Vector3.up, comparisons);
            CompareD1View(outputDirectory, asset, representation, "right", Vector3.right, Vector3.up, comparisons);
            CompareD1View(outputDirectory, asset, representation, "top", Vector3.up, Vector3.forward, comparisons);
        }

        private static void CompareD1View(string outputDirectory, SurfaceAsset asset, string representation, string viewName, Vector3 direction, Vector3 up, ICollection<D1GoldenComparison> comparisons)
        {
            GoldenCapture candidate = RenderD1Golden(asset, direction, up);
            string stem = $"{representation}-{viewName}";
            string referenceRawPath = Path.Combine(outputDirectory, "desktop-" + stem + ".rgba32");
            if (!File.Exists(referenceRawPath))
            {
                throw new FileNotFoundException("Missing Desktop D1 golden.", referenceRawPath);
            }

            byte[] reference = File.ReadAllBytes(referenceRawPath);
            if (reference.Length != candidate.Raw.Length)
            {
                throw new InvalidDataException($"D1 golden byte count differs for {stem}.");
            }

            string candidatePngPath = Path.Combine(outputDirectory, "xr-" + stem + ".png");
            string candidateRawPath = Path.Combine(outputDirectory, "xr-" + stem + ".rgba32");
            File.WriteAllBytes(candidatePngPath, candidate.Png);
            File.WriteAllBytes(candidateRawPath, candidate.Raw);
            D1GoldenComparison comparison = ComparePixels(reference, candidate.Raw, representation, viewName);
            comparison.referencePngSha256 = ComputeHash(File.ReadAllBytes(Path.Combine(outputDirectory, "desktop-" + stem + ".png")));
            comparison.candidatePngSha256 = ComputeHash(candidate.Png);
            comparisons.Add(comparison);
        }

        private static D1TransparentBlendEvidence CaptureD1TransparentGolden(string outputDirectory, SurfaceAsset inflated)
        {
            const float alpha = 0.25f;
            GoldenCapture opaque = RenderD1Golden(inflated, Vector3.right, Vector3.up);
            GoldenCapture transparent = RenderD1Golden(inflated, Vector3.right, Vector3.up, SurfaceTransparency.Transparent, new Color(0.08f, 0.60f, 0.18f, 1f));
            File.WriteAllBytes(Path.Combine(outputDirectory, "xr-inflated-transparent-right.png"), transparent.Png);
            File.WriteAllBytes(Path.Combine(outputDirectory, "xr-inflated-transparent-right.rgba32"), transparent.Raw);

            byte[] background = { transparent.Raw[0], transparent.Raw[1], transparent.Raw[2] };
            var histogram = new int[256];
            long absoluteError = 0;
            int channelCount = 0;
            int maximumError = 0;
            for (int offset = 0; offset < opaque.Raw.Length; offset += 4)
            {
                if (opaque.Raw[offset] <= 3 && opaque.Raw[offset + 1] <= 3 && opaque.Raw[offset + 2] <= 3)
                {
                    continue;
                }

                for (int channel = 0; channel < 3; channel++)
                {
                    int expected = Mathf.RoundToInt(opaque.Raw[offset + channel] * alpha + background[channel] * (1f - alpha));
                    int error = Math.Abs(expected - transparent.Raw[offset + channel]);
                    histogram[error]++;
                    absoluteError += error;
                    maximumError = Math.Max(maximumError, error);
                    channelCount++;
                }
            }

            int target = Mathf.CeilToInt(channelCount * 0.99f);
            int cumulative = 0;
            int p99 = 0;
            for (; p99 < histogram.Length; p99++)
            {
                cumulative += histogram[p99];
                if (cumulative >= target)
                {
                    break;
                }
            }

            float maximum = maximumError / 255f;
            float mean = (float)(absoluteError / (double)channelCount / 255d);
            float percentile99 = p99 / 255f;
            return new D1TransparentBlendEvidence
            {
                view = "inflated-right",
                configuredAlpha = alpha,
                maximumChannelError = maximum,
                meanChannelError = mean,
                p99ChannelError = percentile99,
                pngSha256 = ComputeHash(transparent.Png),
                passed = channelCount > 0 && maximum <= 3f / 255f && mean <= 0.002f && percentile99 <= 2f / 255f,
            };
        }

        private static GoldenCapture RenderD1Golden(SurfaceAsset asset, Vector3 direction, Vector3 up, SurfaceTransparency transparency = SurfaceTransparency.Opaque, Color? background = null)
        {
            const int goldenLayer = 31;
            const int size = 512;
            GameObject cameraObject = new("P05 D1 Golden Camera");
            GameObject surfaceObject = new("P05 D1 Golden Surface");
            RenderTexture renderTexture = null;
            Texture2D readback = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                surfaceObject.layer = goldenLayer;
                var filter = surfaceObject.AddComponent<MeshFilter>();
                var renderer = surfaceObject.AddComponent<MeshRenderer>();
                var depthObject = new GameObject("P05 D1 Golden Depth Prepass")
                {
                    layer = goldenLayer,
                };
                depthObject.transform.SetParent(surfaceObject.transform, false);
                var depthFilter = depthObject.AddComponent<MeshFilter>();
                var depthRenderer = depthObject.AddComponent<MeshRenderer>();
                depthRenderer.enabled = false;
                var presenter = surfaceObject.AddComponent<P05StaticSurfaceRenderer>();
                Material opaque = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMaterialPath);
                Material transparent = AssetDatabase.LoadAssetAtPath<Material>(TransparentMaterialPath);
                Material transparentDepth = AssetDatabase.LoadAssetAtPath<Material>(TransparentDepthMaterialPath);
                presenter.Configure(filter, renderer, opaque, transparent, depthFilter, depthRenderer, transparentDepth, new Color(0.72f, 0.72f, 0.74f, 1f), 0.25f, 0);
                presenter.SetSurface(asset, transparency);

                Vector3 minimum = new(asset.Bounds.Minimum.X, asset.Bounds.Minimum.Y, asset.Bounds.Minimum.Z);
                Vector3 maximum = new(asset.Bounds.Maximum.X, asset.Bounds.Maximum.Y, asset.Bounds.Maximum.Z);
                minimum *= asset.CoordinateSpace.MetersPerUnit;
                maximum *= asset.CoordinateSpace.MetersPerUnit;
                var bounds = new Bounds();
                bounds.SetMinMax(minimum, maximum);
                float radius = Mathf.Max(bounds.extents.magnitude, 0.001f);

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = radius * 1.08f;
                camera.nearClipPlane = radius;
                camera.farClipPlane = radius * 7f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background ?? Color.black;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << goldenLayer;
                cameraObject.transform.position = bounds.center + direction * (radius * 4f);
                cameraObject.transform.rotation = Quaternion.LookRotation(-direction, up);

                renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    antiAliasing = 1,
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                readback = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                readback.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
                readback.Apply(false, false);
                return new GoldenCapture(size, size, readback.GetPixel(size / 2, size / 2), readback.EncodeToPNG(), readback.GetRawTextureData());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                }

                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(surfaceObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static D1GoldenComparison ComparePixels(byte[] reference, byte[] candidate, string representation, string viewName)
        {
            var histogram = new int[256];
            long absoluteError = 0;
            int maximumError = 0;
            int foregroundIntersection = 0;
            int foregroundUnion = 0;
            int referenceForegroundCount = 0;
            int candidateForegroundCount = 0;
            int channelCount = reference.Length / 4 * 3;
            for (int offset = 0; offset < reference.Length; offset += 4)
            {
                bool referenceForeground = reference[offset] > 3 || reference[offset + 1] > 3 || reference[offset + 2] > 3;
                bool candidateForeground = candidate[offset] > 3 || candidate[offset + 1] > 3 || candidate[offset + 2] > 3;
                if (referenceForeground && candidateForeground)
                {
                    foregroundIntersection++;
                }

                if (referenceForeground || candidateForeground)
                {
                    foregroundUnion++;
                }

                if (referenceForeground)
                {
                    referenceForegroundCount++;
                }

                if (candidateForeground)
                {
                    candidateForegroundCount++;
                }

                for (int channel = 0; channel < 3; channel++)
                {
                    int error = Math.Abs(reference[offset + channel] - candidate[offset + channel]);
                    absoluteError += error;
                    histogram[error]++;
                    maximumError = Math.Max(maximumError, error);
                }
            }

            int target = Mathf.CeilToInt(channelCount * 0.99f);
            int cumulative = 0;
            int p99 = 0;
            for (; p99 < histogram.Length; p99++)
            {
                cumulative += histogram[p99];
                if (cumulative >= target)
                {
                    break;
                }
            }

            float maximum = maximumError / 255f;
            float mean = (float)(absoluteError / (double)channelCount / 255d);
            float percentile99 = p99 / 255f;
            float foregroundIou = foregroundUnion == 0 ? 1f : foregroundIntersection / (float)foregroundUnion;
            int pixelCount = reference.Length / 4;
            float referenceForegroundFraction = referenceForegroundCount / (float)pixelCount;
            float candidateForegroundFraction = candidateForegroundCount / (float)pixelCount;
            return new D1GoldenComparison
            {
                representation = representation,
                view = viewName,
                maximumChannelError = maximum,
                meanChannelError = mean,
                p99ChannelError = percentile99,
                foregroundIou = foregroundIou,
                referenceForegroundFraction = referenceForegroundFraction,
                candidateForegroundFraction = candidateForegroundFraction,
                passed = maximum <= 8f / 255f && mean <= 0.003f && percentile99 <= 0.02f && foregroundIou >= 0.995f && referenceForegroundFraction >= 0.01f && candidateForegroundFraction >= 0.01f,
            };
        }

        private static void ConfigurePipeline()
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "P05 Quest Universal Renderer";
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            renderer.renderingMode = RenderingMode.Forward;
            renderer.depthPrimingMode = DepthPrimingMode.Disabled;
            renderer.intermediateTextureMode = IntermediateTextureMode.Auto;
            EditorUtility.SetDirty(renderer);

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "P05 Quest URP";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else
            {
                var serializedPipeline = new SerializedObject(pipeline);
                SerializedProperty renderers = serializedPipeline.FindProperty("m_RendererDataList");
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                serializedPipeline.FindProperty("m_DefaultRendererIndex").intValue = 0;
                serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            }

            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 0f;
            var pipelineSettings = new SerializedObject(pipeline);
            pipelineSettings.FindProperty("m_MainLightRenderingMode").intValue = (int)LightRenderingMode.Disabled;
            pipelineSettings.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.Disabled;
            pipelineSettings.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            pipelineSettings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            pipelineSettings.FindProperty("m_AllowPostProcessAlphaOutput").boolValue = true;
            pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.preserveFramebufferAlpha = true;
        }

        private static GoldenCapture RenderSyntheticGolden()
        {
            const int goldenLayer = 31;
            GameObject cameraObject = new("P05 Golden Camera");
            GameObject surfaceObject = new("P05 Golden Surface");
            RenderTexture renderTexture = null;
            Texture2D readback = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 0.00125f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << goldenLayer;
                cameraObject.transform.position = new Vector3(0f, 0f, 1f);
                cameraObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                surfaceObject.layer = goldenLayer;
                var filter = surfaceObject.AddComponent<MeshFilter>();
                var renderer = surfaceObject.AddComponent<MeshRenderer>();
                var presenter = surfaceObject.AddComponent<P05StaticSurfaceRenderer>();
                Material opaque = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMaterialPath);
                Material transparent = AssetDatabase.LoadAssetAtPath<Material>(TransparentMaterialPath);
                presenter.Configure(filter, renderer, opaque, transparent, null, null, null, new Color(0.25f, 0.5f, 0.75f, 1f), 0.42f, 0);
                presenter.SetSurface(CreateGoldenSurface(), SurfaceTransparency.Opaque);

                const int size = 256;
                renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    antiAliasing = 1,
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                readback = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                readback.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
                readback.Apply(false, false);
                return new GoldenCapture(size, size, readback.GetPixel(size / 2, size / 2), readback.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                }

                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(surfaceObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static SurfaceAsset CreateGoldenSurface()
        {
            return new SurfaceAsset(AssetHash.Parse("19149b6a21d4f9df69bd500deacae220caeafb4f480c410de6021c6c7d0e5ea1"), SurfaceRepresentation.Anatomical, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(-1f, -1f, 0f), new Float3(1f, 1f, 0f)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(-1f, -1f, 0f), new Float3(1f, -1f, 0f), new Float3(1f, 1f, 0f), new Float3(-1f, 1f, 0f) }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0f, 0f, 1f), new Float3(0f, 0f, 1f), new Float3(0f, 0f, 1f), new Float3(0f, 0f, 1f) }), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2, 0, 2, 3 }), RenderBuffer<Float2>.TakeOwnership(new[] { new Float2(0f, 0f), new Float2(1f, 0f), new Float2(1f, 1f), new Float2(0f, 1f) }));
        }

        private static void CreateMaterials()
        {
            CreateOrUpdateMaterial(OpaqueMaterialPath, OpaqueShaderName, new Color(0.72f, 0.72f, 0.74f, 1f), (int)RenderQueue.Geometry);
            CreateOrUpdateMaterial(TransparentMaterialPath, TransparentShaderName, new Color(0.72f, 0.72f, 0.74f, 0.25f), (int)RenderQueue.Transparent);
            CreateOrUpdateMaterial(TransparentDepthMaterialPath, TransparentDepthShaderName, Color.white, (int)RenderQueue.Transparent - 1);
        }

        private static Material CreateOrUpdateMaterial(string path, string shaderName, Color color, int renderQueue)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"P05 shader '{shaderName}' is unavailable.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_AmbientStrength", 0.35f);
            material.SetFloat("_DiffuseStrength", 0.65f);
            material.SetFloat("_Smoothness", 0.45f);
            material.renderQueue = renderQueue;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreatePrefabAndScene()
        {
            Material opaque = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMaterialPath);
            Material transparent = AssetDatabase.LoadAssetAtPath<Material>(TransparentMaterialPath);
            Material transparentDepth = AssetDatabase.LoadAssetAtPath<Material>(TransparentDepthMaterialPath);
            TextAsset anatomicalData = AssetDatabase.LoadAssetAtPath<TextAsset>(AnatomicalDataPath);
            TextAsset inflatedData = AssetDatabase.LoadAssetAtPath<TextAsset>(InflatedDataPath);
            if (anatomicalData == null || inflatedData == null)
            {
                throw new InvalidOperationException("Export the P05 D1 GIFTI SurfaceAssets before creating the prefab.");
            }

            var root = new GameObject("P05 Local Static Surfaces");
            root.transform.position = new Vector3(0f, 1.35f, 0.7f);
            P05StaticSurfaceRenderer anatomical = CreateSurfaceRenderer(root.transform, "Anatomical Surface", new Vector3(-0.10f, 0f, 0f), opaque, transparent, transparentDepth, new Color(0.72f, 0.72f, 0.74f, 1f), 0);
            P05StaticSurfaceRenderer inflated = CreateSurfaceRenderer(root.transform, "Inflated Surface", new Vector3(0.10f, 0f, 0f), opaque, transparent, transparentDepth, new Color(0.62f, 0.78f, 0.92f, 1f), 1);
            root.AddComponent<P05LocalSurfaceBootstrap>().Configure(anatomical, inflated, anatomicalData, inflatedData);
            root.AddComponent<P05DeviceProfiler>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P04ProjectSetup.PrefabPath);
            GameObject surfacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(bootstrapPrefab, scene);
            PrefabUtility.InstantiatePrefab(surfacePrefab, scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Unable to save the P05 static surface scene.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static P05StaticSurfaceRenderer CreateSurfaceRenderer(Transform parent, string name, Vector3 position, Material opaque, Material transparent, Material transparentDepth, Color color, int sortingOrder)
        {
            var surface = new GameObject(name);
            surface.transform.SetParent(parent, false);
            surface.transform.localPosition = position;
            surface.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(0f, 100f, 90f));
            var filter = surface.AddComponent<MeshFilter>();
            var renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = opaque;
            var depthObject = new GameObject("Transparent Depth Prepass");
            depthObject.transform.SetParent(surface.transform, false);
            var depthFilter = depthObject.AddComponent<MeshFilter>();
            var depthRenderer = depthObject.AddComponent<MeshRenderer>();
            depthRenderer.sharedMaterial = transparentDepth;
            depthRenderer.enabled = false;
            var presenter = surface.AddComponent<P05StaticSurfaceRenderer>();
            presenter.Configure(filter, renderer, opaque, transparent, depthFilter, depthRenderer, transparentDepth, color, 0.25f, sortingOrder);
            return presenter;
        }

        private static void ValidateShader(string shaderName, ICollection<string> failures)
        {
            Shader shader = Shader.Find(shaderName);
            Check(shader != null, shaderName, failures);
            Check(shader != null && shader.isSupported, shaderName + " supported", failures);
            Check(shader != null && !ShaderUtil.ShaderHasError(shader), shaderName + " has no compiler errors", failures);
            Material validationMaterial = shader == null ? null : new Material(shader);
            Check(validationMaterial != null && validationMaterial.FindPass("UniversalForward") >= 0, shaderName + " UniversalForward pass", failures);
            Object.DestroyImmediate(validationMaterial);
        }

        private static void Check(bool condition, string description, ICollection<string> failures)
        {
            if (!condition)
            {
                failures.Add(description);
            }
        }

        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        private static void WriteBuildEvidence(string outputPath, BuildReport report)
        {
            string evidencePath = GetArgument("-p05BuildEvidence");
            if (string.IsNullOrWhiteSpace(evidencePath))
            {
                evidencePath = Path.Combine(Path.GetDirectoryName(outputPath), "build-evidence.json");
            }

            string hash;
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(outputPath))
            {
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }

            var evidence = new BuildEvidence
            {
                schema = "P05-build-evidence-v1",
                unity = Application.unityVersion,
                target = report.summary.platform.ToString(),
                result = report.summary.result.ToString(),
                totalBytes = report.summary.totalSize,
                apkSha256 = hash,
                graphicsApi = "Vulkan",
                pipeline = "URP 17.5.0",
                colorSpace = QualitySettings.activeColorSpace.ToString(),
                opaqueShader = OpaqueShaderName,
                transparentShader = TransparentShaderName,
                transparentDepthShader = TransparentDepthShaderName,
                scene = ScenePath,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
            File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true));
        }

        private static string ComputeHash(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static float[] ToArray(Color color)
        {
            return new[] { color.r, color.g, color.b, color.a };
        }

        private sealed class GoldenCapture
        {
            public GoldenCapture(int width, int height, Color centerLinear, byte[] png) : this(width, height, centerLinear, png, null)
            {
            }

            public GoldenCapture(int width, int height, Color centerLinear, byte[] png, byte[] raw)
            {
                Width = width;
                Height = height;
                CenterLinear = centerLinear;
                Png = png;
                Raw = raw;
            }

            public int Width { get; }
            public int Height { get; }
            public Color CenterLinear { get; }
            public byte[] Png { get; }
            public byte[] Raw { get; }
        }

        [Serializable]
        private sealed class D1GoldenEvidence
        {
            public string schema;
            public string desktopShader;
            public string xrShader;
            public string anatomicalSurfaceHash;
            public string inflatedSurfaceHash;
            public int width;
            public int height;
            public float maximumChannelErrorTolerance;
            public float meanChannelErrorTolerance;
            public float p99ChannelErrorTolerance;
            public float minimumForegroundIou;
            public float minimumForegroundFraction;
            public bool passed;
            public string graphicsApi;
            public string colorSpace;
            public D1GoldenComparison[] comparisons;
            public D1TransparentBlendEvidence transparentBlend;
        }

        [Serializable]
        private sealed class D1GoldenComparison
        {
            public string representation;
            public string view;
            public string referencePngSha256;
            public string candidatePngSha256;
            public float maximumChannelError;
            public float meanChannelError;
            public float p99ChannelError;
            public float foregroundIou;
            public float referenceForegroundFraction;
            public float candidateForegroundFraction;
            public bool passed;
        }

        [Serializable]
        private sealed class D1TransparentBlendEvidence
        {
            public string view;
            public float configuredAlpha;
            public string pngSha256;
            public float maximumChannelError;
            public float meanChannelError;
            public float p99ChannelError;
            public bool passed;
        }

        [Serializable]
        private sealed class GoldenEvidence
        {
            public string schema;
            public string sourceSurfaceHash;
            public int width;
            public int height;
            public string referenceSha256;
            public string candidateSha256;
            public bool equal;
            public float[] expectedCenterLinear;
            public float[] actualCenterLinear;
            public float maximumCenterAbsoluteError;
            public float tolerance;
            public string graphicsApi;
            public string colorSpace;
        }

        [Serializable]
        private sealed class BuildEvidence
        {
            public string schema;
            public string unity;
            public string target;
            public string result;
            public ulong totalBytes;
            public string apkSha256;
            public string graphicsApi;
            public string pipeline;
            public string colorSpace;
            public string opaqueShader;
            public string transparentShader;
            public string transparentDepthShader;
            public string scene;
        }
    }
}
