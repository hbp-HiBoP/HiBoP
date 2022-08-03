using UnityEditor;
using UnityEditor.UI;

namespace UnityEngine.UI
{
    /// <summary>
    ///         <para>Custom Editor for the ContentSizeFitter Component.
    /// </para>
    ///       </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ContentSizeClamper), true)]
    public class ContentSizeClamperEditor : SelfControllerEditor
    {
        #region Properties
        private SerializedProperty m_MinHorizontalClamp;
        private SerializedProperty m_MinHorizontalCustomValue;

        private SerializedProperty m_MaxHorizontalClamp;
        private SerializedProperty m_MaxHorizontalCustomValue;

        private SerializedProperty m_MinVerticalClamp;
        private SerializedProperty m_MinVerticalCustomValue;

        private SerializedProperty m_MaxVerticalClamp;
        private SerializedProperty m_MaxVerticalCustomValue;
        #endregion

        protected virtual void OnEnable()
        {
            m_MinHorizontalClamp = serializedObject.FindProperty("m_MinHorizontalClamp");
            m_MinHorizontalCustomValue = serializedObject.FindProperty("m_MinHorizontalCustomValue");

            m_MaxHorizontalClamp = serializedObject.FindProperty("m_MaxHorizontalClamp");
            m_MaxHorizontalCustomValue = serializedObject.FindProperty("m_MaxHorizontalCustomValue");

            m_MinVerticalClamp = serializedObject.FindProperty("m_MinVerticalClamp");
            m_MinVerticalCustomValue = serializedObject.FindProperty("m_MinVerticalCustomValue");

            m_MaxVerticalClamp = serializedObject.FindProperty("m_MaxVerticalClamp");
            m_MaxVerticalCustomValue = serializedObject.FindProperty("m_MaxVerticalCustomValue");
        }

        /// <summary>
        ///   <para>See Editor.OnInspectorGUI.</para>
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Horizontal");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_MinHorizontalClamp, new GUIContent("Min Clamp"));
            if((ContentSizeClamper.ClampMode) m_MinHorizontalClamp.enumValueIndex == ContentSizeClamper.ClampMode.Custom)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MinHorizontalCustomValue, new GUIContent("Value"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(m_MaxHorizontalClamp, new GUIContent("Max Clamp"));
            if ((ContentSizeClamper.ClampMode)m_MaxHorizontalClamp.enumValueIndex == ContentSizeClamper.ClampMode.Custom)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MaxHorizontalCustomValue, new GUIContent("Value"));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("Vertical");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_MinVerticalClamp, new GUIContent("Min Clamp"));
            if ((ContentSizeClamper.ClampMode)m_MinVerticalClamp.enumValueIndex == ContentSizeClamper.ClampMode.Custom)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MinVerticalCustomValue, new GUIContent("Value"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(m_MaxVerticalClamp, new GUIContent("Max Clamp"));
            if ((ContentSizeClamper.ClampMode)m_MaxVerticalClamp.enumValueIndex == ContentSizeClamper.ClampMode.Custom)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MaxVerticalCustomValue, new GUIContent("Value"));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}