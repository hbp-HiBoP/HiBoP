using System;
using System.IO;
using CRNL.HiBoP.XR.Bootstrap.Editor;
using CRNL.HiBoP.XR.StaticRendering;
using CRNL.HiBoP.XR.StaticRendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CRNL.HiBoP.XR.BrainInstances.Editor
{
    public static class P09ProjectSetup
    {
        public const string InstancePrefabPath = "Assets/HiBoPXR/BrainInstances/Prefabs/P09BrainInstance.prefab";
        public const string DemoPrefabPath = "Assets/HiBoPXR/BrainInstances/Prefabs/P09MultiBrainDemo.prefab";
        public const string ScenePath = "Assets/HiBoPXR/BrainInstances/Scenes/P09MultiBrain.unity";

        [MenuItem("HiBoP XR/P09/Apply Multi-Brain Scene")]
        public static void Apply()
        {
            P04ProjectSetup.Validate();
            P05ProjectSetup.Validate();
            CreatePrefabsAndScene();
            Validate();
            AssetDatabase.SaveAssets();
            Debug.Log("P09 multi-brain prefabs and scene configured and validated.");
        }

        [MenuItem("HiBoP XR/P09/Validate Multi-Brain Scene")]
        public static void Validate()
        {
            GameObject instancePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InstancePrefabPath);
            BrainInstanceView view = instancePrefab == null ? null : instancePrefab.GetComponent<BrainInstanceView>();
            if (view == null || instancePrefab.GetComponentInChildren<P05StaticSurfaceRenderer>(true) == null)
                throw new InvalidOperationException("P09 BrainInstance prefab is missing its serialized P05 renderer.");

            GameObject demoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DemoPrefabPath);
            P09MultiBrainProbe probe = demoPrefab == null ? null : demoPrefab.GetComponent<P09MultiBrainProbe>();
            if (probe == null)
                throw new InvalidOperationException("P09 multi-brain demo prefab is missing.");
            var serializedProbe = new SerializedObject(probe);
            if (serializedProbe.FindProperty("instancePrefab").objectReferenceValue == null || serializedProbe.FindProperty("instanceRoot").objectReferenceValue == null)
                throw new InvalidOperationException("P09 multi-brain demo references are not serialized.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("P09 multi-brain scene is missing.");
        }

        private static void CreatePrefabsAndScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(InstancePrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Material opaque = AssetDatabase.LoadAssetAtPath<Material>(P05ProjectSetup.OpaqueMaterialPath);
            Material transparent = AssetDatabase.LoadAssetAtPath<Material>(P05ProjectSetup.TransparentMaterialPath);
            Material transparentDepth = AssetDatabase.LoadAssetAtPath<Material>(P05ProjectSetup.TransparentDepthMaterialPath);

            var instanceRoot = new GameObject("P09 BrainInstance");
            BrainInstanceView view = instanceRoot.AddComponent<BrainInstanceView>();
            var surface = new GameObject("Surface");
            surface.transform.SetParent(instanceRoot.transform, false);
            surface.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(0f, 100f, 90f));
            MeshFilter filter = surface.AddComponent<MeshFilter>();
            MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = opaque;
            var depth = new GameObject("Transparent Depth Prepass");
            depth.transform.SetParent(surface.transform, false);
            MeshFilter depthFilter = depth.AddComponent<MeshFilter>();
            MeshRenderer depthRenderer = depth.AddComponent<MeshRenderer>();
            depthRenderer.sharedMaterial = transparentDepth;
            depthRenderer.enabled = false;
            P05StaticSurfaceRenderer surfaceRenderer = surface.AddComponent<P05StaticSurfaceRenderer>();
            surfaceRenderer.Configure(filter, renderer, opaque, transparent, depthFilter, depthRenderer, transparentDepth, new Color(0.72f, 0.72f, 0.74f, 1f), 0.25f, 0);
            view.Configure(surfaceRenderer);
            PrefabUtility.SaveAsPrefabAsset(instanceRoot, InstancePrefabPath);
            UnityEngine.Object.DestroyImmediate(instanceRoot);

            BrainInstanceView instancePrefab = AssetDatabase.LoadAssetAtPath<BrainInstanceView>(InstancePrefabPath);
            var demoRoot = new GameObject("P09 Multi-Brain Demo");
            var views = new GameObject("Brain Instances");
            views.transform.SetParent(demoRoot.transform, false);
            views.transform.localPosition = new Vector3(0f, 1.35f, 0.7f);
            demoRoot.AddComponent<P09MultiBrainProbe>().Configure(instancePrefab, views.transform);
            PrefabUtility.SaveAsPrefabAsset(demoRoot, DemoPrefabPath);
            UnityEngine.Object.DestroyImmediate(demoRoot);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(P04ProjectSetup.PrefabPath), scene);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DemoPrefabPath), scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Unable to save the P09 multi-brain scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
