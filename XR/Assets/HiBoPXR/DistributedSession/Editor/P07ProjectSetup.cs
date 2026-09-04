using System;
using System.IO;
using System.Security.Cryptography;
using CRNL.HiBoP.XR.Bootstrap.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CRNL.HiBoP.XR.DistributedSession.Editor
{
    public static class P07ProjectSetup
    {
        public const string PrefabPath = "Assets/HiBoPXR/DistributedSession/Prefabs/P07SyntheticSession.prefab";
        public const string ScenePath = "Assets/HiBoPXR/DistributedSession/Scenes/P07SyntheticSession.unity";

        [MenuItem("HiBoP XR/P07/Apply Synthetic Session")]
        public static void Apply()
        {
            P04ProjectSetup.Validate();
            CreatePrefabAndScene();
            Validate();
            AssetDatabase.SaveAssets();
            Debug.Log("P07 synthetic session configured and validated.");
        }

        [MenuItem("HiBoP XR/P07/Validate Synthetic Session")]
        public static void Validate()
        {
            P04ProjectSetup.Validate();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            P07SyntheticSessionProbe probe = prefab == null ? null : prefab.GetComponent<P07SyntheticSessionProbe>();
            if (probe == null || !probe.HasStatusText)
                throw new InvalidOperationException("P07 diagnostic prefab is missing its serialized probe.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("P07 diagnostic scene is missing.");
        }

        public static void BuildAndroid()
        {
            string outputPath = GetArgument("-p07BuildOutput");
            string evidencePath = GetArgument("-p07BuildEvidence");
            if (string.IsNullOrWhiteSpace(outputPath) || string.IsNullOrWhiteSpace(evidencePath))
                throw new ArgumentException("Missing P07 build output or evidence path.");

            outputPath = Path.GetFullPath(outputPath);
            evidencePath = Path.GetFullPath(evidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
            Validate();
            string originalProductName = PlayerSettings.productName;
            BuildReport report;
            try
            {
                PlayerSettings.productName = "HiBoP XR P07";
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
                AssetDatabase.SaveAssets();
            }

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"P07 Android build failed with result {report.summary.result}.");

            BuildEvidence evidence = new()
            {
                schema = "p07-quest-build-v1",
                unity = Application.unityVersion,
                target = report.summary.platform.ToString(),
                result = report.summary.result.ToString(),
                totalBytes = report.summary.totalSize,
                apkSha256 = ComputeHash(outputPath),
                scriptingBackend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android).ToString(),
                architecture = PlayerSettings.Android.targetArchitectures.ToString(),
                scene = ScenePath,
            };
            File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true));
        }

        private static void CreatePrefabAndScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            GameObject root = new("P07 Synthetic Session");
            TextMesh statusText = root.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.UpperLeft;
            statusText.alignment = TextAlignment.Left;
            statusText.characterSize = 0.014f;
            statusText.fontSize = 64;
            statusText.fontStyle = FontStyle.Bold;
            statusText.richText = false;
            statusText.color = Color.yellow;
            statusText.text = "HiBoP XR — P07\nRUNNING...";
            P07SyntheticSessionProbe probe = root.AddComponent<P07SyntheticSessionProbe>();
            probe.Configure(statusText);
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
            byte[] hash = algorithm.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
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
            public string scene;
        }
    }
}
