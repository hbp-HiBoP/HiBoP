using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CRNL.HiBoP.Spikes.P06.UnityClient.Editor
{
    public static class P06UnityBuilder
    {
        private const string PrefabPath = "Assets/P06/Generated/P06TransportProbe.prefab";
        private const string ScenePath = "Assets/P06/Generated/P06TransportProbe.unity";

        public static void BuildAndroid()
        {
            var output = GetArgument("-p06BuildOutput");
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentException("Missing -p06BuildOutput.");
            }

            ConfigureAndroid();
            CreatePrefabAndScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.GetFullPath(output),
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("P06 Android build failed: " + report.summary.result);
            }

            Debug.Log($"P06_BUILD_PASS output={Path.GetFullPath(output)} bytes={report.summary.totalSize} " + $"backend={PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)} " + $"architectures={PlayerSettings.Android.targetArchitectures}");
        }

        private static void ConfigureAndroid()
        {
            PlayerSettings.productName = "HiBoP P06 Transport Spike";
            PlayerSettings.companyName = "CRNL";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "fr.crnl.hibop.p06.spike");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
        }

        private static void CreatePrefabAndScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var root = new GameObject("P06TransportProbe");
            root.AddComponent<P06TransportProbe>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static string GetArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == name)
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
