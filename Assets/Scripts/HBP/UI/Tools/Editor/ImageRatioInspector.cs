using UnityEditor;

namespace HBP.UI.Tools
{
    [CustomEditor(typeof(ImageRatio))]
    public class ImageRatioInspector : Editor
    {
        #region Properties
        SerializedProperty m_TypeProperty;
        SerializedProperty m_MinHeightProperty;
        SerializedProperty m_PreferredHeightProperty;
        SerializedProperty m_FlexibleHeightProperty;
        SerializedProperty m_MinWidthProperty;
        SerializedProperty m_PreferredWidthProperty;
        SerializedProperty m_FlexibleWidthProperty;
        #endregion

        private void OnEnable()
        {
            m_TypeProperty = serializedObject.FindProperty("Type");
            m_MinHeightProperty = serializedObject.FindProperty("m_MinHeight");
            m_PreferredHeightProperty = serializedObject.FindProperty("m_PreferredHeight");
            m_FlexibleHeightProperty = serializedObject.FindProperty("m_FlexibleHeight");
            m_MinWidthProperty = serializedObject.FindProperty("m_MinWidth");
            m_PreferredWidthProperty = serializedObject.FindProperty("m_PreferredWidth");
            m_FlexibleWidthProperty = serializedObject.FindProperty("m_FlexibleWidth");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_TypeProperty);
            EditorGUI.indentLevel++;
            switch((ImageRatio.ControlType)m_TypeProperty.enumValueIndex)
            {
                case ImageRatio.ControlType.HeightControlsWidth:
                    EditorGUILayout.PropertyField(m_MinHeightProperty);
                    EditorGUILayout.PropertyField(m_PreferredHeightProperty);
                    EditorGUILayout.PropertyField(m_FlexibleHeightProperty);
                    break;
                case ImageRatio.ControlType.WidthControlsHeight:
                    EditorGUILayout.PropertyField(m_MinWidthProperty);
                    EditorGUILayout.PropertyField(m_PreferredWidthProperty);
                    EditorGUILayout.PropertyField(m_FlexibleWidthProperty);
                    break;
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}