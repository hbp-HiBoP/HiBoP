using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CRNL.HiBoP.XR.BrainInstances;
using CRNL.HiBoP.XR.BrainInstances.Editor;
using CRNL.HiBoP.XR.Bootstrap.Editor;
using CRNL.HiBoP.XR.Sites.Validation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CRNL.HiBoP.XR.Sites.Editor
{
    public static class P10ProjectSetup
    {
        public const string MeshPath = "Assets/HiBoPXR/Sites/Assets/P10SiteSphere.asset";
        public const string MaterialPath = "Assets/HiBoPXR/Sites/Materials/P10BufferedSites.mat";
        public const string SiteSetPrefabPath = "Assets/HiBoPXR/Sites/Prefabs/P10SiteSet.prefab";
        public const string D0PrefabPath = "Assets/HiBoPXR/Sites/Prefabs/P10D0.prefab";
        public const string D3PrefabPath = "Assets/HiBoPXR/Sites/Prefabs/P10D3.prefab";
        public const string D0ScenePath = "Assets/HiBoPXR/Sites/Scenes/P10D0.unity";
        public const string D3ScenePath = "Assets/HiBoPXR/Sites/Scenes/P10D3.unity";

        private const string ShaderName = "HiBoP XR/P10/Buffered Sites";

        [MenuItem("HiBoP XR/P10/Apply Buffered Sites")]
        public static void Apply()
        {
            P04ProjectSetup.Validate();
            P09ProjectSetup.Validate();
            EnsureDirectories();
            Mesh mesh = CreateOrUpdateMesh();
            Material material = CreateOrUpdateMaterial();
            CreateSiteSetPrefab(mesh, material);
            AttachSiteSetToBrainInstancePrefab();
            CreateDatasetPrefab(D0PrefabPath, 1, 4, false);
            CreateDatasetPrefab(D3PrefabPath, 8, 37_500, true);
            CreateScene(D0ScenePath, D0PrefabPath);
            CreateScene(D3ScenePath, D3PrefabPath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(D3ScenePath, true), new EditorBuildSettingsScene(D0ScenePath, true) };
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("P10 buffered site prefabs and D0/D3 scenes configured and validated.");
        }

        [MenuItem("HiBoP XR/P10/Validate Buffered Sites")]
        public static void Validate()
        {
            var failures = new List<string>();
            Shader shader = Shader.Find(ShaderName);
            Check(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "supported P10 shader", failures);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Check(material != null && material.shader != null && material.shader.name == ShaderName, "P10 material", failures);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            Check(mesh != null && mesh.vertexCount == 4 && mesh.GetIndexCount(0) == 6 && mesh.bounds.extents.magnitude >= 0.99f, "unit-radius site impostor", failures);

            GameObject siteSet = AssetDatabase.LoadAssetAtPath<GameObject>(SiteSetPrefabPath);
            Check(siteSet != null && siteSet.GetComponent<P10SiteRenderer>() != null, "site set renderer", failures);
            Check(siteSet != null && siteSet.GetComponent<P10SiteSelectionController>() != null, "site set selection controller", failures);
            Check(siteSet != null && siteSet.GetComponentsInChildren<MeshRenderer>(true).Length == 0, "no MeshRenderer in site set", failures);
            Check(siteSet != null && siteSet.GetComponentsInChildren<Collider>(true).Length == 0, "no collider in site set", failures);
            GameObject brainInstance = AssetDatabase.LoadAssetAtPath<GameObject>(P09ProjectSetup.InstancePrefabPath);
            Check(brainInstance != null && brainInstance.GetComponentsInChildren<P10SiteRenderer>(true).Length == 1, "one buffered site set in BrainInstance prefab", failures);
            BrainInstanceView brainView = brainInstance == null ? null : brainInstance.GetComponent<BrainInstanceView>();
            Check(brainView != null && brainView.SiteRenderer != null && brainView.SiteSelection != null, "BrainInstance P10 production binding", failures);
            ValidateDatasetPrefab(D0PrefabPath, 1, failures);
            ValidateDatasetPrefab(D3PrefabPath, 8, failures);
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(D0ScenePath) != null, "D0 scene", failures);
            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(D3ScenePath) != null, "D3 scene", failures);
            if (failures.Count > 0)
                throw new InvalidOperationException("P10 validation failed: " + string.Join(", ", failures));
        }

        public static void BuildAndroid()
        {
            string outputPath = GetArgument("-p10BuildOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Missing -p10BuildOutput.");
            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            Apply();
            PlayerSettings.productName = "HiBoP XR P10";
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { D3ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"P10 Android build failed with result {report.summary.result}.");
            WriteBuildEvidence(outputPath, report);
        }

        private static void EnsureDirectories()
        {
            foreach (string path in new[] { MeshPath, MaterialPath, SiteSetPrefabPath, D0ScenePath })
                Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        private static Mesh CreateOrUpdateMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "P10 Site Sphere" };
                AssetDatabase.CreateAsset(mesh, MeshPath);
            }

            BuildImpostorQuad(mesh);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Material CreateOrUpdateMaterial()
        {
            Shader shader = Shader.Find(ShaderName) ?? throw new InvalidOperationException($"Shader '{ShaderName}' is unavailable.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.enableInstancing = true;
            material.renderQueue = (int)RenderQueue.Geometry + 10;
            material.SetFloat("_Ambient", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateSiteSetPrefab(Mesh mesh, Material material)
        {
            var root = new GameObject("P10 Site Set");
            P10SiteRenderer renderer = root.AddComponent<P10SiteRenderer>();
            renderer.Configure(mesh, material);
            P10SiteSelectionController selection = root.AddComponent<P10SiteSelectionController>();
            selection.Configure(renderer);
            PrefabUtility.SaveAsPrefabAsset(root, SiteSetPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void AttachSiteSetToBrainInstancePrefab()
        {
            GameObject siteSet = AssetDatabase.LoadAssetAtPath<GameObject>(SiteSetPrefabPath);
            GameObject root = PrefabUtility.LoadPrefabContents(P09ProjectSetup.InstancePrefabPath);
            try
            {
                Transform surface = root.transform.Find("Surface");
                if (surface == null)
                    throw new InvalidOperationException("P09 BrainInstance prefab has no Surface transform.");
                Transform existing = surface.Find("P10 Site Set");
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(siteSet, surface);
                instance.name = "P10 Site Set";
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                BrainInstanceView view = root.GetComponent<BrainInstanceView>();
                view.ConfigureSites(instance.GetComponent<P10SiteRenderer>(), instance.GetComponent<P10SiteSelectionController>());
                PrefabUtility.SaveAsPrefabAsset(root, P09ProjectSetup.InstancePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateDatasetPrefab(string path, int rendererCount, int count, bool profile)
        {
            GameObject siteSet = AssetDatabase.LoadAssetAtPath<GameObject>(SiteSetPrefabPath);
            var root = new GameObject(count == 37_500 ? "P10 D3 37500 Sites" : "P10 D0 Sites");
            root.transform.position = new Vector3(0f, 1.35f, 0.7f);
            var renderers = new P10SiteRenderer[rendererCount];
            for (int index = 0; index < rendererCount; index++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(siteSet, root.transform);
                instance.name = $"Site Set {index + 1}";
                instance.transform.localPosition = new Vector3((index % 4 - 1.5f) * 0.28f, (index / 4 - 0.5f) * 0.28f, 0f);
                instance.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(0f, 100f, 90f));
                instance.transform.localScale = Vector3.one;
                renderers[index] = instance.GetComponent<P10SiteRenderer>();
            }

            root.AddComponent<P10SyntheticSiteProbe>().Configure(renderers, count, profile);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateScene(string scenePath, string datasetPrefabPath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P04ProjectSetup.PrefabPath), scene);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(datasetPrefabPath), scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException($"Unable to save {scenePath}.");
        }

        private static void ValidateDatasetPrefab(string path, int expectedRenderers, ICollection<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab != null && prefab.GetComponent<P10SyntheticSiteProbe>() != null, path + " synthetic probe", failures);
            Check(prefab != null && prefab.GetComponentsInChildren<P10SiteRenderer>(true).Length == expectedRenderers, path + " renderer count", failures);
            Check(prefab != null && prefab.GetComponentsInChildren<Transform>(true).Length == expectedRenderers + 1, path + " aggregate-only hierarchy", failures);
            Check(prefab != null && prefab.GetComponentsInChildren<MeshRenderer>(true).Length == 0, path + " no per-site renderers", failures);
            Check(prefab != null && prefab.GetComponentsInChildren<Collider>(true).Length == 0, path + " no per-site colliders", failures);
        }

        private static void BuildImpostorQuad(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(new[]
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(1f, -1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(-1f, 1f, 0f),
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
        }

        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                    return arguments[index + 1];
            }

            return null;
        }

        private static void WriteBuildEvidence(string outputPath, BuildReport report)
        {
            string evidencePath = GetArgument("-p10BuildEvidence");
            if (string.IsNullOrWhiteSpace(evidencePath))
                evidencePath = Path.Combine(Path.GetDirectoryName(outputPath), "build-evidence.json");
            string hash;
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(outputPath))
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            GameObject d3 = AssetDatabase.LoadAssetAtPath<GameObject>(D3PrefabPath);
            int rendererCount = d3.GetComponentsInChildren<P10SiteRenderer>(true).Length;
            int individualSiteObjectCount = d3.GetComponentsInChildren<Transform>(true).Length - rendererCount - 1;
            var evidence = new BuildEvidence
            {
                schema = "P10-build-evidence-v1",
                unity = Application.unityVersion,
                target = report.summary.platform.ToString(),
                result = report.summary.result.ToString(),
                totalBytes = report.summary.totalSize,
                apkSha256 = hash,
                siteCount = 37_500,
                rendererCount = rendererCount,
                individualSiteObjectCount = individualSiteObjectCount,
                shader = ShaderName,
                scene = D3ScenePath,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
            File.WriteAllText(evidencePath, JsonUtility.ToJson(evidence, true));
        }

        private static void Check(bool condition, string description, ICollection<string> failures)
        {
            if (!condition)
                failures.Add(description);
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
            public int siteCount;
            public int rendererCount;
            public int individualSiteObjectCount;
            public string shader;
            public string scene;
        }
    }
}
