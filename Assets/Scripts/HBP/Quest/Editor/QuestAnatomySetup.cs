using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HBP.Quest.Editor
{
    public static class QuestAnatomySetup
    {
        public const string ViewPath = "Assets/Prefabs/Quest/QuestAnatomy.prefab";
        public const string MaterialPath = "Assets/Prefabs/Quest/AnatomyOpaque.mat";

        [MenuItem("Tools/Quest/Rebuild Anatomy Assets")]
        public static void Apply()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("HiBoP Quest/Anatomy Opaque");
                if (shader == null) throw new InvalidOperationException("Missing anatomy shader.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var root = new GameObject("Quest Anatomy Spatial Group");
            try
            {
                var frame = new GameObject("Anatomical Frame (mm to m)");
                frame.transform.SetParent(root.transform, false);
                frame.transform.localScale = Vector3.one * 0.001f;
                var filter = frame.AddComponent<MeshFilter>();
                var renderer = frame.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.enabled = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var serialized = new SerializedObject(root.AddComponent<QuestAnatomyView>());
                Set(serialized, "millimeterFrame", frame.transform);
                Set(serialized, "meshFilter", filter);
                Set(serialized, "meshRenderer", renderer);
                Set(serialized, "opaqueMaterial", material);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var session = new SerializedObject(root.AddComponent<QuestAnatomySession>());
                Set(session, "view", root.GetComponent<QuestAnatomyView>());
                session.ApplyModifiedPropertiesWithoutUndo();
                var manipulation = new SerializedObject(root.AddComponent<QuestAnatomyManipulator>());
                Set(manipulation, "view", root.GetComponent<QuestAnatomyView>());
                manipulation.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ViewPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AttachToBootstrap();
            QuestConnectionSetup.AttachQuest();
            AssetDatabase.SaveAssets();
        }

        public static void AttachToBootstrap()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath);
            if (asset == null) return;
            var root = PrefabUtility.LoadPrefabContents(QuestBootstrapSetup.PrefabPath);
            try
            {
                var previous = root.GetComponentInChildren<QuestAnatomyDiagnostic>(true);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                var oldText = root.GetComponentInChildren<Camera>().transform.Find("Anatomy Diagnostic Status");
                if (oldText != null) Object.DestroyImmediate(oldText.gameObject);
                var diagnostic = new GameObject("Local Anatomy Diagnostic");
                diagnostic.transform.SetParent(root.transform, false);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, diagnostic.transform);
                var markers = new GameObject("Diagnostic Landmarks");
                markers.transform.SetParent(instance.transform, false);
                Label(markers.transform, "Origin", "O", Vector3.zero, Color.white);
                Label(markers.transform, "Positive X", "+X 100 mm", new Vector3(0.1f, 0, 0), Color.red);
                Label(markers.transform, "Positive Y", "+Y 100 mm", new Vector3(0, 0.1f, 0), Color.green);
                Label(markers.transform, "Positive Z", "+Z 100 mm", new Vector3(0, 0, 0.1f), Color.cyan);
                foreach (Transform label in markers.transform) label.localRotation = Quaternion.Euler(90, 0, 0);
                var head = Array.Find(root.GetComponentsInChildren<QuestDevicePoseTracker>(), tracker => tracker.Role == QuestDevicePoseTracker.DeviceRole.Head);
                var input = new SerializedObject(diagnostic.AddComponent<QuestAnatomyInput>());
                Set(input, "view", instance.GetComponent<QuestAnatomyView>());
                Set(input, "manipulator", instance.GetComponent<QuestAnatomyManipulator>());
                Set(input, "head", head);
                Set(input, "left", Array.Find(root.GetComponentsInChildren<QuestDevicePoseTracker>(), tracker => tracker.Role == QuestDevicePoseTracker.DeviceRole.LeftController));
                Set(input, "right", Array.Find(root.GetComponentsInChildren<QuestDevicePoseTracker>(), tracker => tracker.Role == QuestDevicePoseTracker.DeviceRole.RightController));
                input.ApplyModifiedPropertiesWithoutUndo();
                var status = Label(head.transform, "Anatomy Diagnostic Status", "", new Vector3(-0.45f, -0.20f, 1.25f), Color.white);
                status.characterSize = 0.012f;
                var serialized = new SerializedObject(diagnostic.AddComponent<QuestAnatomyDiagnostic>());
                Set(serialized, "view", instance.GetComponent<QuestAnatomyView>());
                Set(serialized, "head", head);
                Set(serialized, "landmarks", markers);
                Set(serialized, "statusText", status);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                markers.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, QuestBootstrapSetup.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static TextMesh Label(Transform parent, string name, string text, Vector3 position, Color color)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            var label = child.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.characterSize = 0.006f;
            label.fontSize = 48;
            label.color = color;
            label.text = text;
            return label;
        }

        private static void Set(SerializedObject serialized, string field, Object value) => serialized.FindProperty(field).objectReferenceValue = value;
    }
}
