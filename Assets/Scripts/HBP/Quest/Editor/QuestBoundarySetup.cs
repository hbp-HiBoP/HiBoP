using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace HBP.Quest.Editor
{
    public static class QuestBoundarySetup
    {
        [MenuItem("Tools/Quest/Configure Passthrough Free Movement")]
        public static void Apply()
        {
            ConfigureFeature();
            var root = PrefabUtility.LoadPrefabContents(QuestBootstrapSetup.RigPath);
            try
            {
                var provider = root.GetComponent<QuestPassthroughStatus>();
                if (provider == null) throw new InvalidOperationException("Missing passthrough provider in QuestRig.");
                var boundary = root.GetComponent<QuestBoundaryVisibility>();
                if (boundary == null) boundary = root.AddComponent<QuestBoundaryVisibility>();
                boundary.Configure(provider);
                PrefabUtility.SaveAsPrefabAsset(root, QuestBootstrapSetup.RigPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        public static void ConfigureFeature()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var feature = settings.GetFeature<BoundaryVisibilityFeature>();
            if (feature == null) throw new InvalidOperationException("Missing Meta Boundary Visibility feature.");
            feature.enabled = true;
            var serialized = new SerializedObject(feature);
            serialized.FindProperty("m_SuppressVisibility").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(settings);
        }
    }
}
