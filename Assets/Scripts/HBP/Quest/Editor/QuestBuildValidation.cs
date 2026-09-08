using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.CompositionLayers;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace HBP.Quest.Editor
{
    public sealed class QuestBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) Validate();
        }

        public static void Validate()
        {
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            Require(general != null && general.InitManagerOnStart && general.Manager != null && general.Manager.automaticLoading && general.Manager.automaticRunning, "Android XR initialization");
            Require(general.Manager.activeLoaders.Count == 1 && general.Manager.activeLoaders[0] is OpenXRLoader, "one Android OpenXR loader");
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            Require(settings != null && settings.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced, "single pass instanced stereo");
            RequireFeature<MetaQuestFeature>(settings);
            RequireFeature<ARSessionFeature>(settings);
            RequireFeature<ARCameraFeature>(settings);
            RequireFeature<BoundaryVisibilityFeature>(settings);
            Require(!new SerializedObject(settings.GetFeature<BoundaryVisibilityFeature>()).FindProperty("m_SuppressVisibility").boolValue, "contextual boundary visibility, no automatic suppression");
            RequireFeature<OpenXRCompositionLayersFeature>(settings);
            RequireFeature<OculusTouchControllerProfile>(settings);
            RequireFeature<MetaQuestTouchPlusControllerProfile>(settings);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QuestBootstrapSetup.PipelinePath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(QuestBootstrapSetup.RendererPath);
            Require(pipeline != null && !pipeline.supportsHDR && pipeline.msaaSampleCount == 4, "Quest URP, no HDR, MSAA 4x");
            Require(renderer != null && renderer.intermediateTextureMode == IntermediateTextureMode.Auto && renderer.rendererFeatures.Count == 0, "Quest renderer without Desktop effects");
            Require(new SerializedObject(pipeline).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue == renderer, "Quest renderer reference");
            var quality = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var defaults = quality.FindProperty("m_PerPlatformDefaultQuality");
            int index = -1;
            for (int i = 0; i < defaults.arraySize; i++)
                if (defaults.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue == "Android")
                    index = defaults.GetArrayElementAtIndex(i).FindPropertyRelative("second").intValue;
            var levels = quality.FindProperty("m_QualitySettings");
            Require(index >= 0 && index < levels.arraySize && levels.GetArrayElementAtIndex(index).FindPropertyRelative("customRenderPipeline").objectReferenceValue == pipeline, "Android default uses Quest URP");
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                Require(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.Vulkan }), "Vulkan only");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuestBootstrapSetup.PrefabPath);
            Require(prefab != null && prefab.GetComponent<QuestBootstrap>() != null, "Quest bootstrap prefab");
            var boundary = prefab.GetComponentInChildren<QuestBoundaryVisibility>(true);
            Require(boundary != null && new SerializedObject(boundary).FindProperty("passthrough").objectReferenceValue != null, "serialized passthrough boundary controller");
            var bootstrap = new SerializedObject(prefab.GetComponent<QuestBootstrap>());
            foreach (string field in new[] { "passthrough", "head", "leftController", "rightController", "statusText" })
                Require(bootstrap.FindProperty(field).objectReferenceValue != null, "serialized bootstrap reference " + field);
            var camera = prefab.GetComponentsInChildren<Camera>(true).Single();
            Require(camera.clearFlags == CameraClearFlags.SolidColor && camera.backgroundColor.a == 0 && !camera.allowHDR && !camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing, "transparent camera without post-processing");
        }

        private static void RequireFeature<T>(OpenXRSettings settings) where T : OpenXRFeature => Require(settings.GetFeature<T>()?.enabled == true, typeof(T).Name);

        private static void Require(bool valid, string requirement)
        {
            if (!valid) throw new BuildFailedException("QUEST-004 requires " + requirement + ".");
        }
    }
}
