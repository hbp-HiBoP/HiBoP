using System;
using HBP.Data.Module3D;
using UnityEditor;
using UnityEngine;

namespace HBP.Quest.Editor
{
    /// <summary>Repair serialized presentation references without rebuilding scientific objects.</summary>
    public static class QuestAnatomySetup
    {
        public const string ViewPath = "Assets/Prefabs/Quest/QuestAnatomy.prefab";
        public const string ColumnPath = "Assets/Prefabs/Quest/QuestColumn.prefab";
        public const string ServicesPath = "Assets/Prefabs/Quest/QuestServices.prefab";

        [MenuItem("Tools/Quest/Configure Common Visualization")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(ViewPath);
            try
            {
                var serialized = new SerializedObject(root.GetComponent<QuestAnatomyView>());
                serialized.FindProperty("contentPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D Content.prefab").GetComponent<Base3DScene>();
                serialized.FindProperty("columnPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ColumnPath).GetComponent<QuestColumnPresentation>();
                serialized.FindProperty("servicesPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ServicesPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ViewPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AttachToBootstrap();
        }

        public static void AttachToBootstrap()
        {
            var root = PrefabUtility.LoadPrefabContents(QuestBootstrapSetup.PrefabPath);
            try
            {
                var view = root.GetComponentInChildren<QuestAnatomyView>(true);
                var input = root.GetComponentInChildren<QuestAnatomyInput>(true);
                if (view == null) view = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ViewPath), root.transform)).GetComponent<QuestAnatomyView>();
                if (input == null) input = root.AddComponent<QuestAnatomyInput>();
                var serialized = new SerializedObject(input);
                serialized.FindProperty("view").objectReferenceValue = view;
                foreach (var tracker in root.GetComponentsInChildren<QuestDevicePoseTracker>(true))
                {
                    string field = tracker.Role == QuestDevicePoseTracker.DeviceRole.Head ? "head" : tracker.Role == QuestDevicePoseTracker.DeviceRole.LeftController ? "left" : "right";
                    serialized.FindProperty(field).objectReferenceValue = tracker;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, QuestBootstrapSetup.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
