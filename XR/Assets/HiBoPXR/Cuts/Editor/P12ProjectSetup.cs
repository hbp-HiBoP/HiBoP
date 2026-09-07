using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CRNL.HiBoP.XR.Cuts.Editor
{
    public static class P12ProjectSetup
    {
        public const string PrefabPath = "Assets/HiBoPXR/Cuts/Prefabs/P12Cut.prefab";
        public const string CutMaterialPath = "Assets/HiBoPXR/Cuts/Materials/P12DesktopCut.mat";
        public const string GizmoMaterialPath = "Assets/HiBoPXR/Cuts/Materials/P12Gizmo.mat";
        private const string CutShaderName = "HiBoP XR/P12/Desktop Cut";

        [MenuItem("HiBoP XR/P12/Apply Cut Prefab")]
        public static void Apply()
        {
            EnsureDirectories();
            Material cutMaterial = CreateOrUpdateMaterial(CutMaterialPath, Shader.Find(CutShaderName) ?? throw new InvalidOperationException($"Missing shader {CutShaderName}."), false);
            Shader gizmoShader = Shader.Find("Universal Render Pipeline/Unlit") ?? throw new InvalidOperationException("Missing URP Unlit shader.");
            Material gizmoMaterial = CreateOrUpdateMaterial(GizmoMaterialPath, gizmoShader, true);
            CreatePrefab(cutMaterial, gizmoMaterial);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("P12 local cut gizmo and Desktop-result renderer prefab configured and validated.");
        }

        [MenuItem("HiBoP XR/P12/Validate Cut Prefab")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("The P12 cut prefab is missing.");
            P12CutGizmo gizmo = prefab.GetComponentInChildren<P12CutGizmo>(true);
            P12CutResultRenderer renderer = prefab.GetComponentInChildren<P12CutResultRenderer>(true);
            if (gizmo == null || renderer == null || prefab.GetComponentInChildren<XRGrabInteractable>(true) == null)
                throw new InvalidOperationException("The P12 prefab is missing its gizmo, renderer or XRI grab interactable.");
            var serializedGizmo = new SerializedObject(gizmo);
            var serializedRenderer = new SerializedObject(renderer);
            if (serializedGizmo.FindProperty("grabInteractable").objectReferenceValue == null || serializedGizmo.FindProperty("feedbackRenderers").arraySize < 2)
                throw new InvalidOperationException("The P12 gizmo references must be serialized in the prefab.");
            if (serializedRenderer.FindProperty("meshFilter").objectReferenceValue == null || serializedRenderer.FindProperty("meshRenderer").objectReferenceValue == null || serializedRenderer.FindProperty("cutMaterial").objectReferenceValue == null || serializedRenderer.FindProperty("gizmo").objectReferenceValue == null)
                throw new InvalidOperationException("The P12 result renderer references must be serialized in the prefab.");
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(CutMaterialPath));
        }

        private static Material CreateOrUpdateMaterial(string path, Shader shader, bool transparent)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            if (transparent)
            {
                material.SetColor("_BaseColor", new Color(0.1f, 0.85f, 1f, 0.65f));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreatePrefab(Material cutMaterial, Material gizmoMaterial)
        {
            var root = new GameObject("P12 Cut");
            try
            {
                var canonical = new GameObject("Canonical Desktop Result");
                canonical.transform.SetParent(root.transform, false);
                MeshFilter cutFilter = canonical.AddComponent<MeshFilter>();
                MeshRenderer cutRenderer = canonical.AddComponent<MeshRenderer>();
                cutRenderer.sharedMaterial = cutMaterial;
                cutRenderer.enabled = false;

                var gizmoRoot = new GameObject("Local Cut Gizmo");
                gizmoRoot.transform.SetParent(root.transform, false);
                Rigidbody body = gizmoRoot.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                BoxCollider collider = gizmoRoot.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.18f, 0.18f, 0.02f);
                XRGrabInteractable grab = gizmoRoot.AddComponent<XRGrabInteractable>();

                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plane.name = "Plane";
                plane.transform.SetParent(gizmoRoot.transform, false);
                plane.transform.localScale = new Vector3(0.18f, 0.18f, 0.002f);
                UnityEngine.Object.DestroyImmediate(plane.GetComponent<Collider>());
                MeshRenderer planeRenderer = plane.GetComponent<MeshRenderer>();
                planeRenderer.sharedMaterial = gizmoMaterial;

                GameObject normal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                normal.name = "Normal";
                normal.transform.SetParent(gizmoRoot.transform, false);
                normal.transform.localPosition = new Vector3(0f, 0f, 0.04f);
                normal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                normal.transform.localScale = new Vector3(0.008f, 0.04f, 0.008f);
                UnityEngine.Object.DestroyImmediate(normal.GetComponent<Collider>());
                MeshRenderer normalRenderer = normal.GetComponent<MeshRenderer>();
                normalRenderer.sharedMaterial = gizmoMaterial;

                P12CutGizmo gizmo = gizmoRoot.AddComponent<P12CutGizmo>();
                gizmo.Configure(grab, new[] { planeRenderer, normalRenderer });
                P12CutResultRenderer resultRenderer = canonical.AddComponent<P12CutResultRenderer>();
                resultRenderer.Configure(cutFilter, cutRenderer, cutMaterial, gizmo);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
