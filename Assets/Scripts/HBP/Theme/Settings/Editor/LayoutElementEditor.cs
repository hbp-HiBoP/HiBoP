using UnityEditor;
using UnityEngine;

namespace HBP.Theme
{
    [CustomEditor(typeof(LayoutElement))]
    public class LayoutElementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Ignore Layout.
            SerializedProperty ignoreLayoutProperty = serializedObject.FindProperty("IgnoreLayout");
            EditorGUILayout.PropertyField(ignoreLayoutProperty);

            // Empty Line.
            EditorGUILayout.Space();

            // Min Width.
            EditorGUILayout.BeginHorizontal();
            SerializedProperty minWidthProperty = serializedObject.FindProperty("MinWidth");
            EditorGUILayout.PrefixLabel("Min Width");
            bool minWidthEnable = !Mathf.Approximately(minWidthProperty.floatValue, -1f);
            if(EditorGUILayout.Toggle(minWidthEnable, new GUILayoutOption[] { GUILayout.Width(16) }))
            {
                if (!minWidthEnable) minWidthProperty.floatValue = 0;
                minWidthProperty.floatValue = EditorGUILayout.FloatField(minWidthProperty.floatValue);

            }
            else
            {
                minWidthProperty.floatValue = -1;
            }
            EditorGUILayout.EndHorizontal();

            // Min Height.
            EditorGUILayout.BeginHorizontal();
            SerializedProperty minHeightProperty = serializedObject.FindProperty("MinHeight");
            EditorGUILayout.PrefixLabel("Min Height");
            bool minHeightEnable = !Mathf.Approximately(minHeightProperty.floatValue, -1f);
            if (EditorGUILayout.Toggle(minHeightEnable, new GUILayoutOption[] { GUILayout.Width(16) }))
            {
                if (!minHeightEnable) minHeightProperty.floatValue = 0;
                minHeightProperty.floatValue = EditorGUILayout.FloatField(minHeightProperty.floatValue);

            }
            else
            {
                minHeightProperty.floatValue = -1;
            }
            EditorGUILayout.EndHorizontal();

            // Preferred Width.
            EditorGUILayout.BeginHorizontal();
            SerializedProperty preferredWidthProperty = serializedObject.FindProperty("PreferredWidth");
            EditorGUILayout.PrefixLabel("Preferred Width");
            bool preferredWidthEnable = !Mathf.Approximately(preferredWidthProperty.floatValue, -1f);
            if (EditorGUILayout.Toggle(preferredWidthEnable, new GUILayoutOption[] { GUILayout.Width(16) }))
            {
                if (!preferredWidthEnable) preferredWidthProperty.floatValue = 0;
                preferredWidthProperty.floatValue = EditorGUILayout.FloatField(preferredWidthProperty.floatValue);

            }
            else
            {
                preferredWidthProperty.floatValue = -1;
            }
            EditorGUILayout.EndHorizontal();

            // Preferred Height.
            EditorGUILayout.BeginHorizontal();
            SerializedProperty preferredHeightProperty = serializedObject.FindProperty("PreferredHeight");
            EditorGUILayout.PrefixLabel("Preferred Height");
            bool preferredHeightEnable = !Mathf.Approximately(preferredHeightProperty.floatValue, -1f);
            if (EditorGUILayout.Toggle(preferredHeightEnable, new GUILayoutOption[] { GUILayout.Width(16) }))
            {
                if (!preferredHeightEnable) preferredHeightProperty.floatValue = 0;
                preferredHeightProperty.floatValue = EditorGUILayout.FloatField(preferredHeightProperty.floatValue);

            }
            else
            {
                preferredHeightProperty.floatValue = -1;
            }
            EditorGUILayout.EndHorizontal();

            // Flexible Width.
            EditorGUILayout.BeginHorizontal();
            SerializedProperty flexibleWidthProperty = serializedObject.FindProperty("FlexibleWidth");
            EditorGUILayout.PrefixLabel("Flexible Width");
            bool flexibleWidthEnable = !Mathf.Approximately(flexibleWidthProperty.floatValue, -1f);
            if (EditorGUILayout.Toggle(flexibleWidthEnable, new GUILayoutOption[] { GUILayout.Width(16) }))
            {
                if (!flexibleWidthEnable) flexibleWidthProperty.floatValue = 0;
                flexibleWidthProperty.floatValue = EditorGUILayout.FloatField(flexibleWidthProperty.floatValue);

            }
            else
            {
                flexibleWidthProperty.floatValue = -1;
            }
            EditorGUILayout.EndHorizontal();

            // Flexible Height.
            EditorGUILayout.BeginHorizontal();
            SerializedProperty flexibleHeightProperty = serializedObject.FindProperty("FlexibleHeight");
            EditorGUILayout.PrefixLabel("Flexible Height");
            bool flexibleHeightEnable = !Mathf.Approximately(flexibleHeightProperty.floatValue, -1f);
            if (EditorGUILayout.Toggle(flexibleHeightEnable, new GUILayoutOption[] { GUILayout.Width(16) }))
            {
                if (!flexibleHeightEnable) flexibleHeightProperty.floatValue = 0;
                flexibleHeightProperty.floatValue = EditorGUILayout.FloatField(flexibleHeightProperty.floatValue);

            }
            else
            {
                flexibleHeightProperty.floatValue = -1;
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}