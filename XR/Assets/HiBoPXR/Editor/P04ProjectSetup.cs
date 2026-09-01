using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CRNL.HiBoP.XR.Bootstrap.Meta;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.CompositionLayers;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Object = UnityEngine.Object;

namespace CRNL.HiBoP.XR.Bootstrap.Editor
{
    public static class P04ProjectSetup
    {
        public const string PrefabPath = "Assets/HiBoPXR/Prefabs/P04XRBootstrap.prefab";
        public const string ScenePath = "Assets/HiBoPXR/Scenes/P04Diagnostic.unity";

        private const string GeneralSettingsPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
        private const string LoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

        [MenuItem("HiBoP XR/P04/Apply Quest 3 Bootstrap")]
        public static void Apply()
        {
            ConfigurePlayerSettings();
            ConfigureXRManagement();
            ConfigureOpenXRFeatures();
            CreatePrefabAndScene();
            Validate();
            AssetDatabase.SaveAssets();
            Debug.Log("P04 Quest 3 bootstrap configured and validated.");
        }

        [MenuItem("HiBoP XR/P04/Validate Quest 3 Bootstrap")]
        public static void Validate()
        {
            var failures = new List<string>();

            Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == "fr.crnl.hibop.xr.dev", "application ID", failures);
            Check(PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel32, "minimum API 32", failures);
            Check(PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto, "automatic target API", failures);
            Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, "IL2CPP", failures);
            Check(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "ARM64 only", failures);
            Check(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.Vulkan }), "Vulkan only", failures);
            Check(GetActiveInputHandler() == 1, "Input System only", failures);

            XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            Check(generalSettings?.Manager != null, "Android XR manager", failures);
            Check(generalSettings?.Manager?.activeLoaders.Count == 1 && generalSettings.Manager.activeLoaders[0].GetType().FullName == LoaderTypeName, "OpenXR as the only Android loader", failures);

            OpenXRSettings openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            Check(openXR != null, "Android OpenXR settings", failures);
            Check(IsEnabled<MetaQuestFeature>(openXR), "Meta Quest support", failures);
            Check(IsEnabled<ARSessionFeature>(openXR), "Meta AR session", failures);
            Check(IsEnabled<ARCameraFeature>(openXR), "Meta passthrough camera", failures);
            Check(IsEnabled<OpenXRCompositionLayersFeature>(openXR), "OpenXR composition layers", failures);
            Check(IsEnabled<DisplayUtilitiesFeature>(openXR), "Meta display utilities", failures);
            Check(IsEnabled<HandTracking>(openXR), "OpenXR hand tracking", failures);
            Check(IsEnabled<MetaHandTrackingAim>(openXR), "Meta hand aim", failures);
            Check(IsEnabled<OculusTouchControllerProfile>(openXR), "Oculus Touch controller profile", failures);
            Check(IsEnabled<MetaQuestTouchPlusControllerProfile>(openXR), "Quest 3 Touch Plus controller profile", failures);
            Check(IsOptional<ARCameraFeature>(openXR), "passthrough OpenXR feature optional", failures);

            Check(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null, "bootstrap prefab", failures);
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null, "diagnostic scene", failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("P04 validation failed: " + string.Join(", ", failures));
            }
        }

        public static void BuildAndroid()
        {
            string outputPath = GetArgument("-p04BuildOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Missing -p04BuildOutput.");
            }

            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            Apply();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"P04 Android build failed with result {report.summary.result}.");
            }

            WriteBuildEvidence(outputPath, report);
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "HiBoP XR P04";
            PlayerSettings.companyName = "CRNL";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "fr.crnl.hibop.xr.dev");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            SetActiveInputHandler(1);
        }

        private static void ConfigureXRManagement()
        {
            XRGeneralSettingsPerBuildTarget container = LoadOrCreateGeneralSettings();
            if (!container.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                container.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            }

            XRManagerSettings manager = container.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            manager.automaticLoading = true;
            manager.automaticRunning = true;

            foreach (XRLoader loader in manager.activeLoaders.ToArray())
            {
                manager.TryRemoveLoader(loader);
            }

            if (!XRPackageMetadataStore.AssignLoader(manager, LoaderTypeName, BuildTargetGroup.Android))
            {
                throw new InvalidOperationException("Unable to assign the Android OpenXR loader.");
            }

            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(container);
        }

        private static XRGeneralSettingsPerBuildTarget LoadOrCreateGeneralSettings()
        {
            XRGeneralSettingsPerBuildTarget container = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(GeneralSettingsPath);
            if (container != null)
            {
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, container, true);
                return container;
            }

            container = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(container, GeneralSettingsPath);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, container, true);
            return container;
        }

        private static void ConfigureOpenXRFeatures()
        {
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                throw new InvalidOperationException("Android OpenXR settings were not generated by the package.");
            }

            foreach (OpenXRFeature feature in settings.GetFeatures())
            {
                feature.enabled = false;
                EditorUtility.SetDirty(feature);
            }

            Enable<MetaQuestFeature>(settings);
            Enable<ARSessionFeature>(settings);
            Enable<ARCameraFeature>(settings);
            Enable<OpenXRCompositionLayersFeature>(settings);
            Enable<DisplayUtilitiesFeature>(settings);
            Enable<HandTracking>(settings);
            Enable<MetaHandTrackingAim>(settings);
            Enable<OculusTouchControllerProfile>(settings);
            Enable<MetaQuestTouchPlusControllerProfile>(settings);

            SetFeatureRequired(settings.GetFeature<ARCameraFeature>(), false);
            SelectQuest3Only(settings.GetFeature<MetaQuestFeature>());
            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(settings);
        }

        private static void Enable<TFeature>(OpenXRSettings settings) where TFeature : OpenXRFeature
        {
            TFeature feature = settings.GetFeature<TFeature>();
            if (feature == null)
            {
                throw new InvalidOperationException($"OpenXR feature {typeof(TFeature).Name} is unavailable.");
            }

            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        private static void SetFeatureRequired(OpenXRFeature feature, bool required)
        {
            var serializedFeature = new SerializedObject(feature);
            serializedFeature.FindProperty("required").boolValue = required;
            serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool IsOptional<TFeature>(OpenXRSettings settings) where TFeature : OpenXRFeature
        {
            var serializedFeature = new SerializedObject(settings.GetFeature<TFeature>());
            return !serializedFeature.FindProperty("required").boolValue;
        }

        private static void SelectQuest3Only(MetaQuestFeature feature)
        {
            var serializedFeature = new SerializedObject(feature);
            serializedFeature.FindProperty("forceRemoveInternetPermission").boolValue = true;
            SerializedProperty devices = serializedFeature.FindProperty("targetDevices");
            for (int index = 0; index < devices.arraySize; index++)
            {
                SerializedProperty device = devices.GetArrayElementAtIndex(index);
                string manifestName = device.FindPropertyRelative("manifestName").stringValue;
                device.FindPropertyRelative("enabled").boolValue = manifestName == "eureka";
            }

            serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePrefabAndScene()
        {
            Material controllerMaterial = LoadOrCreateMaterial("Assets/HiBoPXR/Materials/P04Controllers.mat", new Color(0.1f, 0.55f, 1f));
            Material handMaterial = LoadOrCreateMaterial("Assets/HiBoPXR/Materials/P04Hands.mat", new Color(0.2f, 1f, 0.55f));
            Material environmentMaterial = LoadOrCreateMaterial("Assets/HiBoPXR/Materials/P04Environment.mat", new Color(0.08f, 0.12f, 0.22f));

            var root = new GameObject("P04 XR Bootstrap");
            var passthroughProvider = root.AddComponent<MetaOpenXRPassthroughProvider>();
            root.AddComponent<P04MetaDisplayConfigurator>();
            var bootstrap = root.AddComponent<P04BootstrapController>();

            var arSessionObject = new GameObject("AR Session");
            arSessionObject.transform.SetParent(root.transform, false);
            arSessionObject.AddComponent<ARSession>();

            var originObject = new GameObject("XR Origin");
            originObject.transform.SetParent(root.transform, false);
            var origin = originObject.AddComponent<XROrigin>();

            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(originObject.transform, false);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraOffset.transform, false);
            var xrCamera = cameraObject.AddComponent<Camera>();
            xrCamera.nearClipPlane = 0.05f;
            xrCamera.clearFlags = CameraClearFlags.SolidColor;
            xrCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();
            var cameraManager = cameraObject.AddComponent<ARCameraManager>();
            var trackedPoseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
            trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPoseDriver.ignoreTrackingState = false;
            trackedPoseDriver.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            trackedPoseDriver.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            trackedPoseDriver.trackingStateInput = new InputActionProperty(new InputAction("Head Tracking State", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer"));
            var headTracker = cameraObject.AddComponent<P04DevicePoseTracker>();
            headTracker.Configure(P04DevicePoseTracker.DeviceRole.Head, cameraObject.transform, null);

            origin.Camera = xrCamera;
            origin.CameraFloorOffsetObject = cameraOffset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            P04DevicePoseTracker leftController = CreateDeviceMarker(cameraOffset.transform, "Left Controller", P04DevicePoseTracker.DeviceRole.LeftController, controllerMaterial, PrimitiveType.Capsule);
            P04DevicePoseTracker rightController = CreateDeviceMarker(cameraOffset.transform, "Right Controller", P04DevicePoseTracker.DeviceRole.RightController, controllerMaterial, PrimitiveType.Capsule);
            P04HandWristTracker leftHand = CreateHandMarker(cameraOffset.transform, "Left Hand Wrist", Handedness.Left, handMaterial);
            P04HandWristTracker rightHand = CreateHandMarker(cameraOffset.transform, "Right Hand Wrist", Handedness.Right, handMaterial);

            var vrEnvironment = new GameObject("VR Fallback Environment");
            vrEnvironment.transform.SetParent(root.transform, false);
            CreateEnvironmentPrimitive(vrEnvironment.transform, "Floor", new Vector3(0f, -0.08f, 0f), new Vector3(8f, 0.1f, 8f), environmentMaterial);
            CreateEnvironmentPrimitive(vrEnvironment.transform, "Forward Marker", new Vector3(0f, 1.4f, 3f), new Vector3(1f, 1f, 0.08f), controllerMaterial);

            var statusObject = new GameObject("P04 Status");
            statusObject.transform.SetParent(cameraObject.transform, false);
            statusObject.transform.localPosition = new Vector3(-0.42f, 0.28f, 1.25f);
            var statusText = statusObject.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.UpperLeft;
            statusText.alignment = TextAlignment.Left;
            statusText.characterSize = 0.018f;
            statusText.fontSize = 48;
            statusText.color = Color.white;
            statusText.text = "HiBoP XR P04 | starting OpenXR...";

            passthroughProvider.Configure(cameraManager, xrCamera, vrEnvironment);
            bootstrap.Configure(passthroughProvider, headTracker, leftController, rightController, leftHand, rightHand, statusText);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Unable to save the P04 diagnostic scene.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static P04DevicePoseTracker CreateDeviceMarker(Transform parent, string name, P04DevicePoseTracker.DeviceRole role, Material material, PrimitiveType primitiveType)
        {
            GameObject marker = GameObject.CreatePrimitive(primitiveType);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localScale = new Vector3(0.05f, 0.08f, 0.05f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            Renderer markerRenderer = marker.GetComponent<Renderer>();
            markerRenderer.sharedMaterial = material;
            var tracker = marker.AddComponent<P04DevicePoseTracker>();
            tracker.Configure(role, marker.transform, markerRenderer);
            return tracker;
        }

        private static P04HandWristTracker CreateHandMarker(Transform parent, string name, Handedness handedness, Material material)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localScale = Vector3.one * 0.075f;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            Renderer markerRenderer = marker.GetComponent<Renderer>();
            markerRenderer.sharedMaterial = material;
            var tracker = marker.AddComponent<P04HandWristTracker>();
            tracker.Configure(handedness, marker.transform, markerRenderer);
            return tracker;
        }

        private static void CreateEnvironmentPrimitive(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            primitive.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material LoadOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool IsEnabled<TFeature>(OpenXRSettings settings) where TFeature : OpenXRFeature
        {
            return settings != null && settings.GetFeature<TFeature>()?.enabled == true;
        }

        private static void Check(bool condition, string description, ICollection<string> failures)
        {
            if (!condition)
            {
                failures.Add(description);
            }
        }

        private static Object LoadProjectSettingsAsset()
        {
            return AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").First();
        }

        private static int GetActiveInputHandler()
        {
            return new SerializedObject(LoadProjectSettingsAsset()).FindProperty("activeInputHandler").intValue;
        }

        private static void SetActiveInputHandler(int value)
        {
            var serializedSettings = new SerializedObject(LoadProjectSettingsAsset());
            serializedSettings.FindProperty("activeInputHandler").intValue = value;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
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
            string evidencePath = GetArgument("-p04BuildEvidence");
            if (string.IsNullOrWhiteSpace(evidencePath))
            {
                evidencePath = Path.Combine(Path.GetDirectoryName(outputPath), "build-evidence.json");
            }

            string sha256;
            using (SHA256 hasher = SHA256.Create())
            using (FileStream stream = File.OpenRead(outputPath))
            {
                sha256 = BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }

            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"gate\": \"P04\",");
            json.AppendLine($"  \"unityVersion\": \"{Application.unityVersion}\",");
            json.AppendLine("  \"applicationId\": \"fr.crnl.hibop.xr.dev\",");
            json.AppendLine("  \"passthroughSupport\": \"Supported\",");
            json.AppendLine("  \"minimumApiLevel\": 32,");
            json.AppendLine($"  \"totalSizeBytes\": {report.summary.totalSize},");
            json.AppendLine($"  \"apkSha256\": \"{sha256}\"");
            json.AppendLine("}");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(evidencePath)));
            File.WriteAllText(evidencePath, json.ToString());
        }
    }
}
