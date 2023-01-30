using UnityEditor;

namespace HBP.UI.Tools
{
    [CustomEditor(typeof(SquareRectTransform))]
    public class SquareRectTransformInspector : Editor
    {
        #region Properties
        SerializedProperty m_TypeProperty;
        #endregion

        private void OnEnable()
        {
            m_TypeProperty = serializedObject.FindProperty("Type");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_TypeProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}