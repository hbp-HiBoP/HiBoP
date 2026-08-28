using System.IO;
using System.Linq;
using System.Reflection;
using HBP.Data.Module3D;
using HBP.Rendering;
using HBP.UI.Module3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HBP.Tests.Rendering
{
    public class Phase4MultiViewEdgesExportTests
    {
        private const string RendererPath = "Assets/Settings/Rendering/HBP-Desktop-Renderer.asset";
        private const string ViewPrefabPath = "Assets/Prefabs/3D/Scenes/View 3D.prefab";
        private const string EdgeShaderPath = "Assets/Shaders/HBP/Utility/HBPEdges.shader";
        private const string BrainShaderPath = "Assets/Shaders/HBP/Brain/HBPBrain.shader";
        private const string BrainTransparentShaderPath = "Assets/Shaders/HBP/Brain/HBPBrainTransparent.shader";
        private const string CutShaderPath = "Assets/Shaders/HBP/Cuts/HBPCut.shader";
        private const string CutTransparentShaderPath = "Assets/Shaders/HBP/Cuts/HBPCutTransparent.shader";

        [Test]
        public void RenderTextureOwner_ReusesStableSizeAndReleasesItsAllocation()
        {
            const string textureName = "Phase 4 stable view texture";
            int initialCount = CountRenderTextures(textureName);

            HBPRenderTextureOwner owner = new();
            RenderTexture first = null;
            for (int cycle = 0; cycle < 100; ++cycle)
            {
                RenderTexture current = owner.Acquire(640, 480, textureName);
                first ??= current;
                Assert.That(current, Is.SameAs(first), $"cycle {cycle}");
            }

            Assert.That(owner.AllocationCount, Is.EqualTo(1));
            Assert.That(first.IsCreated(), Is.True);
            owner.Release();

            Assert.That(owner.Texture, Is.Null);
            Assert.That(CountRenderTextures(textureName), Is.EqualTo(initialCount));
        }

        [Test]
        public void RenderTextureOwner_ResizesWithoutRetainingPreviousTextures()
        {
            const string textureName = "Phase 4 resized view texture";
            int initialCount = CountRenderTextures(textureName);
            HBPRenderTextureOwner owner = new();

            for (int cycle = 0; cycle < 100; ++cycle)
            {
                int width = cycle % 2 == 0 ? 320 : 640;
                RenderTexture texture = owner.Acquire(width, 240, textureName);
                Assert.That(texture.width, Is.EqualTo(width));
                Assert.That(CountRenderTextures(textureName), Is.EqualTo(initialCount + 1), $"cycle {cycle}");
            }

            Assert.That(owner.AllocationCount, Is.EqualTo(100));
            owner.Dispose();
            Assert.That(CountRenderTextures(textureName), Is.EqualTo(initialCount));
        }

        [Test]
        public void EdgeRendererFeature_IsActiveAndUsesRenderGraph()
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            HBPEdgeRendererFeature feature = renderer.rendererFeatures.OfType<HBPEdgeRendererFeature>().SingleOrDefault();
            string source = File.ReadAllText("Assets/Scripts/HBP/Rendering/HBPEdgeRendererFeature.cs");

            Assert.That(feature, Is.Not.Null);
            Assert.That(feature.isActive, Is.True);
            Assert.That(feature.EdgeSettings.Thickness, Is.GreaterThan(0.0f));
            StringAssert.Contains("RecordRenderGraph", source);
            StringAssert.Contains("OpaqueDataTag", source);
            StringAssert.Contains("GraphicsFormat.R16G16B16A16_SFloat", source);
            StringAssert.DoesNotContain("ConfigureInput(", source);
            StringAssert.DoesNotContain("Execute(ScriptableRenderContext", source);
        }

        [Test]
        public void TransparentBrain_UsesNearestSurfaceCompositeBeforeOtherTransparentObjects()
        {
            string feature = File.ReadAllText("Assets/Scripts/HBP/Rendering/HBPEdgeRendererFeature.cs");
            string brain = File.ReadAllText(BrainTransparentShaderPath);
            string composite = File.ReadAllText(EdgeShaderPath);

            StringAssert.Contains("TransparentBrainSurfaceTag", feature);
            StringAssert.Contains("RenderPassEvent.BeforeRenderingTransparents", feature);
            StringAssert.Contains("LightMode\" = \"HBPTransparentBrainSurface", brain);
            StringAssert.Contains("ZWrite On", brain);
            StringAssert.Contains("_HBPTransparentBrainSurface", composite);
            StringAssert.Contains("_HBPTransparentBrainDepth", composite);
        }

        [Test]
        public void EdgeShader_ImportsForMetalAndUsesOpaqueAndTransparentSilhouettes()
        {
            Shader shader = Shader.Find("Hidden/HBP/Edges");
            string source = File.ReadAllText(EdgeShaderPath);

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            Assert.That(ShaderUtil.GetShaderMessages(shader, ShaderCompilerPlatform.Metal), Is.Empty);
            StringAssert.Contains("_HBPEdgeOpaqueData", source);
            StringAssert.Contains("_HBPEdgeTransparentMask", source);
            StringAssert.Contains("DepthCurvature", source);
            StringAssert.DoesNotContain("RelativeDepthDifference", source);
            StringAssert.DoesNotContain("SampleSceneDepth", source);
            StringAssert.DoesNotContain("SampleSceneNormals", source);
        }

        [Test]
        public void ViewRenderTextureSize_MatchesItsPhysicalScreenRectangle()
        {
            GameObject parentObject = new("scaled view parent", typeof(RectTransform));
            GameObject viewObject = new("scaled view", typeof(RectTransform));

            try
            {
                RectTransform parent = parentObject.GetComponent<RectTransform>();
                RectTransform view = viewObject.GetComponent<RectTransform>();
                view.SetParent(parent, false);
                parent.localScale = new Vector3(1.2f, 1.2f, 1.0f);
                view.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500.0f);
                view.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 250.0f);

                MethodInfo method = typeof(View3DUI).GetMethod("GetRenderTextureSize", BindingFlags.Static | BindingFlags.NonPublic);
                Vector2Int size = (Vector2Int)method.Invoke(null, new object[] { view });

                Assert.That(size, Is.EqualTo(new Vector2Int(600, 300)));
                Assert.That(size.x / (float)size.y, Is.EqualTo(2.0f));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(parentObject);
            }
        }

        [Test]
        public void EdgeRenderer_DrawsCutContoursButDoesNotAlterSitesOrGizmos()
        {
            GameObject cameraObject = new("edge render camera", typeof(Camera), typeof(HBPEdgeCameraSettings));
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Camera camera = cameraObject.GetComponent<Camera>();
            HBPEdgeCameraSettings edgeSettings = cameraObject.GetComponent<HBPEdgeCameraSettings>();
            RenderTexture target = new(HBPRenderTextureDescriptorFactory.CreateViewDescriptor(256, 256));
            Material cutMaterial = new(Shader.Find("HBP/Cut"));
            Material siteMaterial = new(Shader.Find("HBP/Site"));
            Material gizmoMaterial = new(Shader.Find("HBP/Utility/UnlitColor"));
            Texture2D readback = new(256, 256, TextureFormat.RGBA32, false, false);
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                target.Create();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                camera.orthographic = true;
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
                camera.targetTexture = target;
                sphere.GetComponent<MeshRenderer>().sharedMaterial = cutMaterial;

                edgeSettings.EdgesEnabled = false;
                Color32[] cutWithoutEdges = RenderPixels(camera, target, readback);
                edgeSettings.EdgesEnabled = true;
                Color32[] cutWithEdges = RenderPixels(camera, target, readback);

                Assert.That(CountChangedPixels(cutWithoutEdges, cutWithEdges, 4), Is.GreaterThan(100), "cut contours must visibly change the image");

                siteMaterial.SetColor("_Color", Color.red);
                sphere.GetComponent<MeshRenderer>().sharedMaterial = siteMaterial;
                edgeSettings.EdgesEnabled = false;
                Color32[] siteWithoutEdges = RenderPixels(camera, target, readback);
                edgeSettings.EdgesEnabled = true;
                Color32[] siteWithEdges = RenderPixels(camera, target, readback);

                Assert.That(CountChangedPixels(siteWithoutEdges, siteWithEdges, 4), Is.Zero, "sites must be excluded from the edge renderer");

                gizmoMaterial.SetColor("_Color", Color.red);
                sphere.GetComponent<MeshRenderer>().sharedMaterial = gizmoMaterial;
                edgeSettings.EdgesEnabled = false;
                Color32[] gizmoWithoutEdges = RenderPixels(camera, target, readback);
                edgeSettings.EdgesEnabled = true;
                Color32[] gizmoWithEdges = RenderPixels(camera, target, readback);

                Assert.That(CountChangedPixels(gizmoWithoutEdges, gizmoWithEdges, 4), Is.Zero, "rotation and cut gizmos must be excluded from the edge renderer");
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cutMaterial);
                Object.DestroyImmediate(siteMaterial);
                Object.DestroyImmediate(gizmoMaterial);
                Object.DestroyImmediate(sphere);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TransparentBrain_DoesNotAccumulateNestedSurfaceOpacity()
        {
            GameObject cameraObject = new("transparent brain layer camera", typeof(Camera), typeof(HBPEdgeCameraSettings));
            GameObject outerSurface = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject innerSurface = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Camera camera = cameraObject.GetComponent<Camera>();
            HBPEdgeCameraSettings edgeSettings = cameraObject.GetComponent<HBPEdgeCameraSettings>();
            RenderTexture target = new(HBPRenderTextureDescriptorFactory.CreateViewDescriptor(256, 256));
            Material brainMaterial = new(Shader.Find("HBP/Brain/Transparent"));
            Texture2D readback = new(256, 256, TextureFormat.RGBA32, false, false);
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                target.Create();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                camera.orthographic = true;
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
                camera.targetTexture = target;
                edgeSettings.EdgesEnabled = false;

                brainMaterial.SetColor("_Color", new Color(0.8f, 0.7f, 0.6f, 0.25f));
                outerSurface.transform.localScale = Vector3.one * 1.5f;
                innerSurface.transform.localScale = Vector3.one * 1.2f;
                outerSurface.GetComponent<MeshRenderer>().sharedMaterial = brainMaterial;
                innerSurface.GetComponent<MeshRenderer>().sharedMaterial = brainMaterial;

                outerSurface.SetActive(false);
                innerSurface.SetActive(false);
                Color32[] backgroundPixels = RenderPixels(camera, target, readback);
                outerSurface.SetActive(true);
                innerSurface.SetActive(false);
                Color32[] outerPixels = RenderPixels(camera, target, readback);
                innerSurface.SetActive(true);
                Color32[] nestedPixels = RenderPixels(camera, target, readback);

                Assert.That(CountChangedPixels(backgroundPixels, outerPixels, 4), Is.GreaterThan(100), "the transparent brain surface must remain visible");
                Assert.That(CountChangedPixels(outerPixels, nestedPixels, 2, 80, 80, 176, 176, 256), Is.Zero, "a hidden brain surface must not add another alpha layer");
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(brainMaterial);
                Object.DestroyImmediate(outerSurface);
                Object.DestroyImmediate(innerSurface);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TransparentBrain_DoesNotDimSitesRenderedInsideIt()
        {
            GameObject cameraObject = new("transparent brain site camera", typeof(Camera), typeof(HBPEdgeCameraSettings));
            GameObject brain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject site = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Camera camera = cameraObject.GetComponent<Camera>();
            HBPEdgeCameraSettings edgeSettings = cameraObject.GetComponent<HBPEdgeCameraSettings>();
            RenderTexture target = new(HBPRenderTextureDescriptorFactory.CreateViewDescriptor(256, 256));
            Material brainMaterial = new(Shader.Find("HBP/Brain/Transparent"));
            Material siteMaterial = new(Shader.Find("HBP/Site"));
            Texture2D readback = new(256, 256, TextureFormat.RGBA32, false, false);
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                target.Create();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                camera.orthographic = true;
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
                camera.targetTexture = target;
                edgeSettings.EdgesEnabled = false;

                brainMaterial.SetColor("_Color", new Color(0.8f, 0.7f, 0.6f, 0.25f));
                siteMaterial.SetColor("_Color", Color.red);
                brain.transform.localScale = Vector3.one * 1.5f;
                site.transform.localScale = Vector3.one * 0.25f;
                brain.GetComponent<MeshRenderer>().sharedMaterial = brainMaterial;
                site.GetComponent<MeshRenderer>().sharedMaterial = siteMaterial;

                brain.SetActive(false);
                Color32 siteOnly = RenderPixels(camera, target, readback)[128 * 256 + 128];
                brain.SetActive(true);
                Color32 siteThroughBrain = RenderPixels(camera, target, readback)[128 * 256 + 128];

                Assert.That(siteThroughBrain.r, Is.EqualTo(siteOnly.r).Within(2));
                Assert.That(siteThroughBrain.g, Is.EqualTo(siteOnly.g).Within(2));
                Assert.That(siteThroughBrain.b, Is.EqualTo(siteOnly.b).Within(2));
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(brainMaterial);
                Object.DestroyImmediate(siteMaterial);
                Object.DestroyImmediate(brain);
                Object.DestroyImmediate(site);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TransparentBrain_DoesNotCoverOpaqueObjectsInFrontOfIt()
        {
            GameObject cameraObject = new("transparent brain opaque camera", typeof(Camera), typeof(HBPEdgeCameraSettings));
            GameObject brain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject opaqueObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Camera camera = cameraObject.GetComponent<Camera>();
            HBPEdgeCameraSettings edgeSettings = cameraObject.GetComponent<HBPEdgeCameraSettings>();
            RenderTexture target = new(HBPRenderTextureDescriptorFactory.CreateViewDescriptor(256, 256));
            Material brainMaterial = new(Shader.Find("HBP/Brain/Transparent"));
            Material cutMaterial = new(Shader.Find("HBP/Cut"));
            Texture2D readback = new(256, 256, TextureFormat.RGBA32, false, false);
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                target.Create();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                camera.orthographic = true;
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
                camera.targetTexture = target;
                edgeSettings.EdgesEnabled = false;

                brainMaterial.SetColor("_Color", new Color(0.8f, 0.7f, 0.6f, 0.25f));
                cutMaterial.SetColor("_Color", Color.green);
                brain.transform.localScale = Vector3.one * 1.5f;
                opaqueObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
                opaqueObject.transform.localScale = Vector3.one * 0.25f;
                brain.GetComponent<MeshRenderer>().sharedMaterial = brainMaterial;
                opaqueObject.GetComponent<MeshRenderer>().sharedMaterial = cutMaterial;

                brain.SetActive(false);
                Color32 opaqueOnly = RenderPixels(camera, target, readback)[128 * 256 + 128];
                brain.SetActive(true);
                Color32 opaqueInFront = RenderPixels(camera, target, readback)[128 * 256 + 128];

                Assert.That(opaqueInFront.r, Is.EqualTo(opaqueOnly.r).Within(2));
                Assert.That(opaqueInFront.g, Is.EqualTo(opaqueOnly.g).Within(2));
                Assert.That(opaqueInFront.b, Is.EqualTo(opaqueOnly.b).Within(2));
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(brainMaterial);
                Object.DestroyImmediate(cutMaterial);
                Object.DestroyImmediate(brain);
                Object.DestroyImmediate(opaqueObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TransparentEdges_DoNotOutlineAnOpaqueGizmoInsideTheSurface()
        {
            GameObject cameraObject = new("transparent edge camera", typeof(Camera), typeof(HBPEdgeCameraSettings));
            GameObject transparentSurface = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject gizmo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Camera camera = cameraObject.GetComponent<Camera>();
            HBPEdgeCameraSettings edgeSettings = cameraObject.GetComponent<HBPEdgeCameraSettings>();
            RenderTexture target = new(HBPRenderTextureDescriptorFactory.CreateViewDescriptor(256, 256));
            Material transparentMaterial = new(Shader.Find("HBP/Cut/Transparent"));
            Material gizmoMaterial = new(Shader.Find("HBP/Utility/UnlitColor"));
            Texture2D readback = new(256, 256, TextureFormat.RGBA32, false, false);
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                target.Create();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                camera.orthographic = true;
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
                camera.targetTexture = target;

                transparentMaterial.SetColor("_Color", new Color(0.8f, 0.7f, 0.6f, 0.25f));
                transparentSurface.transform.localScale = Vector3.one * 1.5f;
                transparentSurface.GetComponent<MeshRenderer>().sharedMaterial = transparentMaterial;

                gizmoMaterial.SetColor("_Color", Color.red);
                gizmo.transform.position = new Vector3(0.0f, 0.0f, -0.9f);
                gizmo.transform.localScale = Vector3.one * 0.25f;
                gizmo.GetComponent<MeshRenderer>().sharedMaterial = gizmoMaterial;

                edgeSettings.EdgesEnabled = false;
                Color32[] withoutEdges = RenderPixels(camera, target, readback);
                edgeSettings.EdgesEnabled = true;
                Color32[] withEdges = RenderPixels(camera, target, readback);

                Assert.That(CountChangedPixels(withoutEdges, withEdges, 4), Is.GreaterThan(100), "the transparent surface silhouette must remain visible");
                Assert.That(CountChangedPixels(withoutEdges, withEdges, 4, 96, 96, 160, 160, 256), Is.Zero, "the internal gizmo must not create an edge");
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(transparentMaterial);
                Object.DestroyImmediate(gizmoMaterial);
                Object.DestroyImmediate(transparentSurface);
                Object.DestroyImmediate(gizmo);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TransparentEdgeMask_IsLimitedToBrainAndCuts()
        {
            string brain = File.ReadAllText(BrainTransparentShaderPath);
            string cut = File.ReadAllText(CutTransparentShaderPath);
            string opaqueBrain = File.ReadAllText(BrainShaderPath);
            string opaqueCut = File.ReadAllText(CutShaderPath);
            string site = File.ReadAllText("Assets/Shaders/HBP/Sites/HBPSite.shader");
            string roi = File.ReadAllText("Assets/Shaders/HBP/ROI/HBPROIWireframe.shader");
            string gizmo = File.ReadAllText("Assets/Shaders/HBP/Utility/HBPUnlitColor.shader");

            StringAssert.Contains("LightMode\" = \"HBPEdgeMask", brain);
            StringAssert.Contains("LightMode\" = \"HBPEdgeMask", cut);
            StringAssert.Contains("ZTest Always", brain);
            StringAssert.Contains("ZTest Always", cut);
            StringAssert.Contains("LightMode\" = \"HBPEdgeData", opaqueBrain);
            StringAssert.Contains("LightMode\" = \"HBPEdgeData", opaqueCut);
            StringAssert.DoesNotContain("HBPEdgeMask", site);
            StringAssert.DoesNotContain("HBPEdgeMask", roi);
            StringAssert.DoesNotContain("HBPEdgeData", site);
            StringAssert.DoesNotContain("HBPEdgeData", roi);
            StringAssert.DoesNotContain("HBPEdgeData", gizmo);
        }

        [Test]
        public void ViewPrefab_HasPerCameraEdgeSettingsAndNoLegacyPostProcessing()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath);
            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            Assert.That(components.OfType<HBPEdgeCameraSettings>().Count(), Is.EqualTo(1));
            Assert.That(components.Any(component => component != null && component.GetType().FullName.Contains("PostProcess")), Is.False);
        }

        [Test]
        public void CameraEdgeSettings_AreIndependentBetweenViews()
        {
            GameObject firstView = new("first view", typeof(Camera), typeof(HBPEdgeCameraSettings));
            GameObject secondView = new("second view", typeof(Camera), typeof(HBPEdgeCameraSettings));

            try
            {
                HBPEdgeCameraSettings first = firstView.GetComponent<HBPEdgeCameraSettings>();
                HBPEdgeCameraSettings second = secondView.GetComponent<HBPEdgeCameraSettings>();
                first.EdgesEnabled = true;
                second.EdgesEnabled = false;

                Assert.That(first.EdgesEnabled, Is.True);
                Assert.That(second.EdgesEnabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(firstView);
                Object.DestroyImmediate(secondView);
            }
        }

        [Test]
        public void HiddenView_ReleasesItsTextureAndStopsItsCamera()
        {
            CreateLogicalView(out GameObject viewObject, out GameObject cameraObject, out View3D view, out Camera camera);
            GameObject uiObject = new("view UI", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage), typeof(View3DUI));
            HBPRenderTextureOwner owner = GetField<HBPRenderTextureOwner>(uiObject.GetComponent<View3DUI>(), "m_RenderTextureOwner");
            RenderTexture texture = owner.Acquire(64, 64, "Phase 4 hidden view texture");

            try
            {
                View3DUI viewUI = uiObject.GetComponent<View3DUI>();
                UnityEngine.UI.RawImage rawImage = uiObject.GetComponent<UnityEngine.UI.RawImage>();
                SetField(viewUI, "m_View", view);
                SetField(viewUI, "m_RawImage", rawImage);
                view.TargetTexture = texture;
                rawImage.texture = texture;
                camera.enabled = true;

                viewUI.enabled = false;
                InvokePrivate(viewUI, "OnDisable");

                Assert.That(owner.Texture, Is.Null);
                Assert.That(view.TargetTexture, Is.Null);
                Assert.That(rawImage.texture, Is.Null);
                Assert.That(camera.enabled, Is.False);
            }
            finally
            {
                owner.Release();
                Object.DestroyImmediate(uiObject);
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TransparentExport_RestoresCameraStateAndLeavesNoTemporaryRenderTexture()
        {
            CreateLogicalView(out GameObject viewObject, out GameObject cameraObject, out View3D view, out Camera camera);
            RenderTexture originalTarget = new(HBPRenderTextureDescriptorFactory.CreateViewDescriptor(32, 16));
            originalTarget.Create();
            camera.targetTexture = originalTarget;
            camera.aspect = 1.75f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.magenta;
            Texture2D export = null;

            try
            {
                export = view.GetTexture(64, 32, Color.clear);

                Assert.That(camera.targetTexture, Is.SameAs(originalTarget));
                Assert.That(camera.aspect, Is.EqualTo(1.75f));
                Assert.That(camera.backgroundColor, Is.EqualTo(Color.magenta));
                Assert.That(export.width, Is.EqualTo(64));
                Assert.That(export.height, Is.EqualTo(32));
                Assert.That(export.GetPixels32().All(pixel => pixel.Equals(new Color32(0, 0, 0, 0))), Is.True);
                Assert.That(CountRenderTextures("HBP Export 64x32"), Is.Zero);
            }
            finally
            {
                camera.targetTexture = null;
                originalTarget.Release();
                Object.DestroyImmediate(originalTarget);
                Object.DestroyImmediate(export);
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TwentySevenViewOwners_StayBoundedAcrossOneHundredResizeCycles()
        {
            const string texturePrefix = "Phase 4 9x3 view ";
            int initialCount = Resources.FindObjectsOfTypeAll<RenderTexture>().Count(texture => texture.name.StartsWith(texturePrefix));
            HBPRenderTextureOwner[] owners = Enumerable.Range(0, 27).Select(_ => new HBPRenderTextureOwner()).ToArray();

            try
            {
                for (int cycle = 0; cycle < 100; ++cycle)
                {
                    int width = cycle % 2 == 0 ? 16 : 24;
                    for (int view = 0; view < owners.Length; ++view)
                        owners[view].Acquire(width, 16 + view % 3, texturePrefix + view);

                    Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Count(texture => texture.name.StartsWith(texturePrefix)), Is.EqualTo(initialCount + owners.Length), $"cycle {cycle}");
                }
            }
            finally
            {
                foreach (HBPRenderTextureOwner owner in owners)
                    owner.Dispose();
            }

            Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Count(texture => texture.name.StartsWith(texturePrefix)), Is.EqualTo(initialCount));
        }

        [Test]
        public void RepeatedTransparentExports_ReturnToTheTextureBaseline()
        {
            CreateLogicalView(out GameObject viewObject, out GameObject cameraObject, out View3D view, out Camera camera);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            try
            {
                Texture2D warmup32 = view.GetTexture(32, 32, Color.clear);
                Texture2D warmup48 = view.GetTexture(48, 48, Color.clear);
                Object.DestroyImmediate(warmup32);
                Object.DestroyImmediate(warmup48);
                int initialRenderTextures = Resources.FindObjectsOfTypeAll<RenderTexture>().Length;
                int initialTextures = Resources.FindObjectsOfTypeAll<Texture2D>().Length;

                for (int cycle = 0; cycle < 100; ++cycle)
                {
                    int size = cycle % 2 == 0 ? 32 : 48;
                    Texture2D export = view.GetTexture(size, size, Color.clear);
                    Assert.That(export.GetPixel(0, 0).a, Is.Zero, $"cycle {cycle}");
                    Object.DestroyImmediate(export);
                }

                Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Length, Is.EqualTo(initialRenderTextures));
                Assert.That(Resources.FindObjectsOfTypeAll<Texture2D>().Length, Is.EqualTo(initialTextures));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void SemiTransparentExport_ContainsStraightSrgbPixels()
        {
            CreateLogicalView(out GameObject viewObject, out GameObject cameraObject, out View3D view, out Camera camera);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Material siteMaterial = new(Shader.Find("HBP/Site"));
            Texture2D export = null;

            try
            {
                siteMaterial.SetColor("_Color", new Color(1.0f, 0.0f, 0.0f, 0.5f));
                sphere.GetComponent<MeshRenderer>().sharedMaterial = siteMaterial;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.orthographic = true;
                camera.orthographicSize = 1.25f;
                camera.transform.position = new Vector3(0.0f, 0.0f, -3.0f);

                export = view.GetTexture(128, 128, Color.clear);
                Color32 center = export.GetPixels32()[64 * export.width + 64];

                Assert.That(center.a, Is.InRange(120, 136));
                Assert.That(center.r, Is.GreaterThan(245), "straight alpha must preserve the original red channel");
                Assert.That(center.g, Is.LessThan(5));
                Assert.That(center.b, Is.LessThan(5));
            }
            finally
            {
                Object.DestroyImmediate(export);
                Object.DestroyImmediate(siteMaterial);
                Object.DestroyImmediate(sphere);
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void StraightAlphaConversion_RemovesTransparentRgbAndPreservesComposition()
        {
            Color32[] pixels =
            {
                new(0, 0, 0, 0),
                new(137, 99, 71, 128),
                new(10, 20, 30, 255)
            };

            RenderingColorUtility.ConvertPremultipliedToStraightAlpha(pixels);

            Assert.That(pixels[0], Is.EqualTo(new Color32(0, 0, 0, 0)));
            Assert.That(pixels[1].r, Is.EqualTo(188).Within(1));
            Assert.That(pixels[1].g, Is.EqualTo(137).Within(1));
            Assert.That(pixels[1].b, Is.EqualTo(99).Within(1));
            Assert.That(pixels[1].a, Is.EqualTo(128));
            Assert.That(pixels[2], Is.EqualTo(new Color32(10, 20, 30, 255)));

            AssertLinearComposition(pixels[1], Color.white, new Color32(225, 207, 197, 255));
            AssertLinearComposition(pixels[1], new Color32(40, 40, 40, 255), new Color32(140, 103, 77, 255));
        }

        private static int CountRenderTextures(string name)
        {
            return Resources.FindObjectsOfTypeAll<RenderTexture>().Count(texture => texture.name == name);
        }

        private static void CreateLogicalView(out GameObject viewObject, out GameObject cameraObject, out View3D view, out Camera camera)
        {
            cameraObject = new GameObject("phase 4 camera", typeof(Camera), typeof(HBPEdgeCameraSettings), typeof(Camera3D));
            camera = cameraObject.GetComponent<Camera>();
            Camera3D camera3D = cameraObject.GetComponent<Camera3D>();
            SetField(camera3D, "m_Camera", camera);
            SetField(camera3D, "m_EdgeSettings", cameraObject.GetComponent<HBPEdgeCameraSettings>());

            viewObject = new GameObject("phase 4 view", typeof(View3D));
            view = viewObject.GetComponent<View3D>();
            SetField(view, "m_Camera3D", camera3D);
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static void InvokePrivate(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private static void AssertLinearComposition(Color32 foreground, Color32 background, Color32 expected)
        {
            float alpha = foreground.a / 255.0f;
            Color foregroundLinear = ((Color)foreground).linear;
            Color backgroundLinear = ((Color)background).linear;
            Color32 actual = new Color(foregroundLinear.r * alpha + backgroundLinear.r * (1.0f - alpha), foregroundLinear.g * alpha + backgroundLinear.g * (1.0f - alpha), foregroundLinear.b * alpha + backgroundLinear.b * (1.0f - alpha), 1.0f).gamma;

            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1));
        }

        private static Color32[] RenderPixels(Camera camera, RenderTexture target, Texture2D readback)
        {
            camera.Render();
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            readback.Apply(false, false);
            return readback.GetPixels32();
        }

        private static int CountChangedPixels(Color32[] first, Color32[] second, int tolerance)
        {
            int changed = 0;
            for (int index = 0; index < first.Length; ++index)
            {
                if (Mathf.Abs(first[index].r - second[index].r) > tolerance || Mathf.Abs(first[index].g - second[index].g) > tolerance || Mathf.Abs(first[index].b - second[index].b) > tolerance || Mathf.Abs(first[index].a - second[index].a) > tolerance)
                {
                    ++changed;
                }
            }

            return changed;
        }

        private static int CountChangedPixels(Color32[] first, Color32[] second, int tolerance, int minX, int minY, int maxX, int maxY, int width)
        {
            int changed = 0;
            for (int y = minY; y < maxY; ++y)
            {
                for (int x = minX; x < maxX; ++x)
                {
                    int index = y * width + x;
                    if (Mathf.Abs(first[index].r - second[index].r) > tolerance || Mathf.Abs(first[index].g - second[index].g) > tolerance || Mathf.Abs(first[index].b - second[index].b) > tolerance || Mathf.Abs(first[index].a - second[index].a) > tolerance)
                    {
                        ++changed;
                    }
                }
            }

            return changed;
        }
    }
}
