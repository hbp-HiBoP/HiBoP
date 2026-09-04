using System;
using System.IO;
using System.Security.Cryptography;
using CRNL.HiBoP.XR.Bootstrap.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CRNL.HiBoP.XR.Timeline.Validation.Editor
{
    public static class P11TimelineProjectSetup
    {
        public const string PrefabPath = "Assets/HiBoPXR/Timeline/Validation/Prefabs/P11TimelineDeviceProbe.prefab";
        public const string ScenePath = "Assets/HiBoPXR/Timeline/Validation/Scenes/P11TimelineDeviceProbe.unity";
        public const string PackageId = "fr.crnl.hibop.xr.d20timeline";

        [MenuItem("HiBoP XR/P11/Apply Timeline Device Probe")]
        public static void Apply()
        {
            P04ProjectSetup.Validate();
            CreatePrefabAndScene();
            Validate();
            AssetDatabase.SaveAssets();
            Debug.Log("P11 timeline device probe configured and validated.");
        }

        [MenuItem("HiBoP XR/P11/Validate Timeline Device Probe")]
        public static void Validate()
        {
            P04ProjectSetup.Validate();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            P11TimelineDeviceProbe probe = prefab == null ? null : prefab.GetComponent<P11TimelineDeviceProbe>();
            if (probe == null || !probe.HasStatusText)
                throw new InvalidOperationException("P11 device probe prefab is missing its serialized status.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("P11 device probe scene is missing.");
        }

        public static void BuildAndroid()
        {
            string outputPath = GetArgument("-p11BuildOutput");
            string evidencePath = GetArgument("-p11BuildEvidence");
            if (string.IsNullOrWhiteSpace(outputPath) || string.IsNullOrWhiteSpace(evidencePath))
                throw new ArgumentException("Missing P11 build output or evidence path.");

            outputPath = Path.GetFullPath(outputPath);
            evidencePath = Path.GetFullPath(evidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
            Apply();
            string originalProductName = PlayerSettings.productName;
            string originalPackageId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
            BuildReport report;
            try
            {
                PlayerSettings.productName = "HiBoP XR D20 Timeline";
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageId);
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.Development,
                });
            }
            finally
            {
                PlayerSettings.productName = originalProductName;
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, originalPackageId);
                AssetDatabase.SaveAssets();
            }

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"P11 Android build failed with result {report.summary.result}.");

            var evidence = new BuildEvidence
            {
                schema = "d20-timeline-quest-build-v1",
                unity = Application.unityVersion,
                target = report.summary.platform.ToString(),
                result = report.summary.result.ToString(),
                totalBytes = report.summary.totalSize,
                apkSha256 = ComputeHash(outputPath),
                scriptingBackend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android).ToString(),
                architecture = PlayerSettings.Android.targetArchitectures.ToString(),
                packageId = PackageId,
                scene = ScenePath,
            };
            File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true));
        }

        private static void CreatePrefabAndScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var root = new GameObject("P11 Timeline Device Probe");
            TextMesh status = root.AddComponent<TextMesh>();
            status.anchor = TextAnchor.UpperLeft;
            status.alignment = TextAlignment.Left;
            status.characterSize = 0.014f;
            status.fontSize = 64;
            status.fontStyle = FontStyle.Bold;
            status.richText = false;
            status.color = Color.yellow;
            status.text = "HiBoP XR — D20 timeline\nRUNNING...";
            P11TimelineDeviceProbe probe = root.AddComponent<P11TimelineDeviceProbe>();
            probe.Configure(status);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>(P04ProjectSetup.PrefabPath);
            GameObject bootstrapInstance = (GameObject)PrefabUtility.InstantiatePrefab(bootstrap, scene);
            Camera xrCamera = bootstrapInstance.GetComponentInChildren<Camera>(true);
            if (xrCamera == null)
                throw new InvalidOperationException("P04 bootstrap is missing its XR camera.");
            TextMesh p04Status = Array.Find(bootstrapInstance.GetComponentsInChildren<TextMesh>(true), candidate => candidate.name == "P04 Status");
            if (p04Status != null)
                p04Status.gameObject.SetActive(false);

            GameObject probePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject probeInstance = (GameObject)PrefabUtility.InstantiatePrefab(probePrefab, scene);
            probeInstance.transform.SetParent(xrCamera.transform, false);
            probeInstance.transform.localPosition = new Vector3(-0.5f, 0.3f, 1.25f);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                    return arguments[index + 1];
            }

            return string.Empty;
        }

        private static string ComputeHash(string path)
        {
            using SHA256 algorithm = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
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
            public string scriptingBackend;
            public string architecture;
            public string packageId;
            public string scene;
        }
    }
}
