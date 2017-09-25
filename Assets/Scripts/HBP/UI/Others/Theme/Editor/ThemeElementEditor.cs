using UnityEditor;
using UnityEngine;

namespace HBP.UI.Theme
{
    [CustomEditor(typeof(ThemeElement))]
    [CanEditMultipleObjects]
    public class ThemeElementEditor : Editor
    {
        SerializedProperty m_IgnoreThemeProp;
        SerializedProperty m_ZoneProp;
        SerializedProperty m_GeneralProp;
        SerializedProperty m_MenuProp;
        SerializedProperty m_WindowProp;
        SerializedProperty m_HeaderProp;
        SerializedProperty m_ContentProp;
        SerializedProperty m_ItemProp;
        SerializedProperty m_ToolbarProp;
        SerializedProperty m_VisualizationProp;
        SerializedProperty m_EffectProp;
        SerializedProperty m_GraphicsProp;

        private void OnEnable()
        {
            m_IgnoreThemeProp = serializedObject.FindProperty("IgnoreTheme");
            m_ZoneProp = serializedObject.FindProperty("Zone");
            m_GeneralProp = serializedObject.FindProperty("General");
            m_MenuProp = serializedObject.FindProperty("Menu");
            m_WindowProp = serializedObject.FindProperty("Window");
            m_HeaderProp = serializedObject.FindProperty("Header");
            m_ContentProp = serializedObject.FindProperty("Content");
            m_ItemProp = serializedObject.FindProperty("Item");
            m_ToolbarProp = serializedObject.FindProperty("Toolbar");
            m_VisualizationProp = serializedObject.FindProperty("Visualization");
            m_EffectProp = serializedObject.FindProperty("Effect");
            m_GraphicsProp = serializedObject.FindProperty("Graphics");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_IgnoreThemeProp);
            if (!m_IgnoreThemeProp.boolValue)
            {
                EditorGUILayout.PropertyField(m_ZoneProp);
                switch ((ThemeElement.ZoneEnum) m_ZoneProp.enumValueIndex)
                {
                    case ThemeElement.ZoneEnum.General: EditorGUILayout.PropertyField(m_GeneralProp, new GUIContent("Type")); break;
                    case ThemeElement.ZoneEnum.Menu: EditorGUILayout.PropertyField(m_MenuProp, new GUIContent("Type")); break;
                    case ThemeElement.ZoneEnum.Window:
                        EditorGUILayout.PropertyField(m_WindowProp, new GUIContent("Window"));
                        switch ((ThemeElement.WindowEnum)m_WindowProp.enumValueIndex)
                        {
                            case ThemeElement.WindowEnum.Header: EditorGUILayout.PropertyField(m_HeaderProp, new GUIContent("Type")); break;
                            case ThemeElement.WindowEnum.Content:
                                EditorGUILayout.PropertyField(m_ContentProp, new GUIContent("Type"));
                                if ((ThemeElement.ContentEnum) m_ContentProp.enumValueIndex == ThemeElement.ContentEnum.Item)
                                {
                                    EditorGUILayout.PropertyField(m_ContentProp, new GUIContent("Type"));
                                }
                                break;
                        }
                        break;
                    case ThemeElement.ZoneEnum.Toolbar: EditorGUILayout.PropertyField(m_ToolbarProp, new GUIContent("Type")); break;
                    case ThemeElement.ZoneEnum.Visualization: EditorGUILayout.PropertyField(m_VisualizationProp, new GUIContent("Type")); break;
                }
                EditorGUILayout.PropertyField(m_EffectProp, new GUIContent("Effect"));
                if ((ThemeElement.EffectEnum)m_EffectProp.enumValueIndex == ThemeElement.EffectEnum.Custom)
                {
                    EditorGUILayout.PropertyField(m_GraphicsProp,new GUIContent("Graphics"),true);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}