using System;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.CompositionLayers;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Object = UnityEngine.Object;

namespace HBP.Quest.Editor
{
    // Adapted selectively from P04ProjectSetup (eb26c323e). Run explicitly, never during a build.
    public static class QuestBootstrapSetup
    {
        public const string RigPath = "Assets/Prefabs/Quest/QuestRig.prefab";
        public const string PrefabPath = "Assets/Prefabs/Quest/QuestBootstrap.prefab";
        public const string ScenePath = "Assets/_Scenes/QuestBootstrap.unity";
        public const string PipelinePath = "Assets/Settings/Rendering/HBP-Quest-URP.asset";
        public const string RendererPath = "Assets/Settings/Rendering/HBP-Quest-Renderer.asset";

        [MenuItem("Tools/Quest/Rebuild Bootstrap Assets")]
        public static void Apply()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Activate the Quest build profile before configuring its assets.");
            ConfigureXR();
            ConfigureRendering();
            CreatePrefabs();
            QuestAnatomySetup.AttachToBootstrap();
            QuestConnectionSetup.AttachQuest();
            AssetDatabase.SaveAssets();
            Debug.Log("QUEST-004 bootstrap assets serialized.");
        }

        private static void ConfigureXR()
        {
            var container = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            if (!container.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) container.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var general = container.SettingsForBuildTarget(BuildTargetGroup.Android);
            var manager = container.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            general.InitManagerOnStart = true;
            manager.automaticLoading = true;
            manager.automaticRunning = true;
            foreach (var loader in manager.activeLoaders.ToArray()) manager.TryRemoveLoader(loader);
            if (!XRPackageMetadataStore.AssignLoader(manager, typeof(OpenXRLoader).FullName, BuildTargetGroup.Android)) throw new InvalidOperationException("Cannot assign OpenXR.");
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(container);

            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            foreach (var feature in settings.GetFeatures())
            {
                feature.enabled = false;
                EditorUtility.SetDirty(feature);
            }

            Enable<MetaQuestFeature>(settings);
            QuestBoundarySetup.ConfigureFeature();
            Enable<ARSessionFeature>(settings);
            Enable<ARCameraFeature>(settings);
            Enable<OpenXRCompositionLayersFeature>(settings);
            Enable<OculusTouchControllerProfile>(settings);
            Enable<MetaQuestTouchPlusControllerProfile>(settings);
            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            var meta = new SerializedObject(settings.GetFeature<MetaQuestFeature>());
            var devices = meta.FindProperty("targetDevices");
            for (int i = 0; i < devices.arraySize; i++)
            {
                var device = devices.GetArrayElementAtIndex(i);
                device.FindPropertyRelative("enabled").boolValue = device.FindPropertyRelative("manifestName").stringValue == "eureka";
            }

            meta.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void Enable<T>(OpenXRSettings settings) where T : OpenXRFeature
        {
            var feature = settings.GetFeature<T>();
            if (feature == null) throw new InvalidOperationException("Missing OpenXR feature " + typeof(T).Name);
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        private static void ConfigureRendering()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            renderer.intermediateTextureMode = IntermediateTextureMode.Auto;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 4;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);

            // A dedicated Android quality level keeps every existing Desktop pipeline and default intact.
            var quality = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = quality.FindProperty("m_QualitySettings");
            int questIndex = Enumerable.Range(0, levels.arraySize).Where(i => levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Quest").DefaultIfEmpty(-1).First();
            if (questIndex < 0)
            {
                questIndex = levels.arraySize;
                levels.InsertArrayElementAtIndex(questIndex);
            }

            var level = levels.GetArrayElementAtIndex(questIndex);
            level.FindPropertyRelative("name").stringValue = "Quest";
            level.FindPropertyRelative("customRenderPipeline").objectReferenceValue = pipeline;
            level.FindPropertyRelative("vSyncCount").intValue = 0;
            var excluded = level.FindPropertyRelative("excludedTargetPlatforms");
            excluded.arraySize = 1;
            excluded.GetArrayElementAtIndex(0).stringValue = "Standalone";
            var defaults = quality.FindProperty("m_PerPlatformDefaultQuality");
            int android = -1;
            for (int i = 0; i < defaults.arraySize; i++)
                if (defaults.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue == "Android")
                    android = i;
            if (android < 0)
            {
                android = defaults.arraySize;
                defaults.InsertArrayElementAtIndex(android);
            }

            var entry = defaults.GetArrayElementAtIndex(android);
            entry.FindPropertyRelative("first").stringValue = "Android";
            entry.FindPropertyRelative("second").intValue = questIndex;
            quality.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePrefabs()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Quest Rig");
            var provider = root.AddComponent<QuestPassthroughStatus>();
            root.AddComponent<QuestBoundaryVisibility>().Configure(provider);
            Child(root.transform, "AR Session").AddComponent<ARSession>();
            var origin = Child(root.transform, "XR Origin").AddComponent<XROrigin>();
            var offset = Child(origin.transform, "Camera Offset");
            var cameraObject = Child(offset.transform, "Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.allowHDR = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            cameraObject.AddComponent<AudioListener>();
            var cameraManager = cameraObject.AddComponent<ARCameraManager>();
            var driver = cameraObject.AddComponent<TrackedPoseDriver>();
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.positionInput = new InputActionProperty(new InputAction("Head position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(new InputAction("Head rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            driver.trackingStateInput = new InputActionProperty(new InputAction("Head tracking", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer"));
            cameraObject.AddComponent<QuestDevicePoseTracker>().Configure(QuestDevicePoseTracker.DeviceRole.Head, camera.transform, null);
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            CreateController(offset.transform, "Left Controller", QuestDevicePoseTracker.DeviceRole.LeftController, new Color(0.1f, 0.55f, 1f));
            CreateController(offset.transform, "Right Controller", QuestDevicePoseTracker.DeviceRole.RightController, new Color(1f, 0.45f, 0.1f));
            var status = Child(camera.transform, "Quest Status").AddComponent<TextMesh>();
            status.transform.localPosition = new Vector3(-0.45f, 0.25f, 1.25f);
            status.anchor = TextAnchor.UpperLeft;
            status.fontSize = 48;
            status.characterSize = 0.012f;
            status.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status.GetComponent<Renderer>().sharedMaterial = status.font.material;
            status.text = "HiBoP Quest | STARTING";
            provider.Configure(cameraManager, camera);
            var rig = PrefabUtility.SaveAsPrefabAsset(root, RigPath);
            Object.DestroyImmediate(root);

            var bootstrapObject = new GameObject("Quest Bootstrap");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(rig, bootstrapObject.transform);
            var trackers = instance.GetComponentsInChildren<QuestDevicePoseTracker>();
            bootstrapObject.AddComponent<QuestBootstrap>().Configure(instance.GetComponent<QuestPassthroughStatus>(), trackers.Single(t => t.Role == QuestDevicePoseTracker.DeviceRole.Head), trackers.Single(t => t.Role == QuestDevicePoseTracker.DeviceRole.LeftController), trackers.Single(t => t.Role == QuestDevicePoseTracker.DeviceRole.RightController), instance.GetComponentInChildren<TextMesh>());
            var bootstrap = PrefabUtility.SaveAsPrefabAsset(bootstrapObject, PrefabPath);
            Object.DestroyImmediate(bootstrapObject);
            PrefabUtility.InstantiatePrefab(bootstrap, scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save QuestBootstrap scene.");
        }

        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void CreateController(Transform parent, string name, QuestDevicePoseTracker.DeviceRole role, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localScale = new Vector3(0.04f, 0.06f, 0.12f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            string path = "Assets/Prefabs/Quest/" + role + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            var renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = false;
            marker.AddComponent<QuestDevicePoseTracker>().Configure(role, marker.transform, renderer);
        }
    }
}
