using UnityEngine;
using UnityEditor;

namespace NewTheme
{
    [CustomEditor(typeof(HorizontalOrVerticalLayoutGroup))]
    public class HorizontalOrVerticalLayoutGroupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty paddingProperty = serializedObject.FindProperty("Padding");
            SerializedProperty spacingProperty = serializedObject.FindProperty("Spacing");
            SerializedProperty childAlignmentProperty = serializedObject.FindProperty("ChildAlignment");
            SerializedProperty childControlWidthProperty = serializedObject.FindProperty("ChildControlWidth");
            SerializedProperty childControlHeightProperty = serializedObject.FindProperty("ChildControlHeight");
            SerializedProperty childForceExpandWidthProperty = serializedObject.FindProperty("ChildForceExpandWidth");
            SerializedProperty childForceExpandHeightProperty = serializedObject.FindProperty("ChildForceExpandHeight");

            EditorGUILayout.PropertyField(paddingProperty, true);
            EditorGUILayout.PropertyField(spacingProperty);
            EditorGUILayout.PropertyField(childAlignmentProperty);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Control Child Size");
            childControlWidthProperty.boolValue = EditorGUILayout.ToggleLeft("Width", childControlWidthProperty.boolValue, new GUILayoutOption[] { GUILayout.MinWidth(14), GUILayout.Width(14), GUILayout.ExpandWidth(true) });
            childControlHeightProperty.boolValue = EditorGUILayout.ToggleLeft("Height", childControlHeightProperty.boolValue, new GUILayoutOption[] { GUILayout.MinWidth(14), GUILayout.Width(14), GUILayout.ExpandWidth(true) });
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Child Force Expand");
            childForceExpandWidthProperty.boolValue = EditorGUILayout.ToggleLeft("Width", childForceExpandWidthProperty.boolValue, new GUILayoutOption[] { GUILayout.MinWidth(14), GUILayout.Width(14), GUILayout.ExpandWidth(true) });
            childForceExpandHeightProperty.boolValue = EditorGUILayout.ToggleLeft("Height", childForceExpandHeightProperty.boolValue, new GUILayoutOption[] { GUILayout.MinWidth(14), GUILayout.Width(14), GUILayout.ExpandWidth(true) });
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}