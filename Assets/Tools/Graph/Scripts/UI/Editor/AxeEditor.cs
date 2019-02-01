using UnityEngine;
using UnityEditor;

namespace Tools.Unity.Graph
{
    [CustomEditor(typeof(Axe))]
    public class AxeEditor : Editor
    {
        #region Properties
        bool m_ShowTickMarks = false;
        bool m_ShowGraphics = false;
        bool m_ShowEvents = false;
        SerializedProperty m_Direction;
        SerializedProperty m_Label;
        SerializedProperty m_Unit;
        SerializedProperty m_UnitText;
        SerializedProperty m_LabelText;
        SerializedProperty m_VisualImage;
        SerializedProperty m_VisualArrowImage;
        SerializedProperty m_Color;
        SerializedProperty m_DisplayRange;
        SerializedProperty m_TickMarks;
        SerializedProperty m_IndependentTickMarkValue;
        SerializedProperty m_IndependentTickMark;
        SerializedProperty m_UseIndependentTickMark;
        #endregion

        public void OnEnable()
        {
            m_Direction = serializedObject.FindProperty("m_Direction");
            m_LabelText = serializedObject.FindProperty("m_LabelText");
            m_Label = serializedObject.FindProperty("m_Label");

            m_UnitText = serializedObject.FindProperty("m_UnitText");
            m_Unit = serializedObject.FindProperty("m_Unit");
            m_VisualImage = serializedObject.FindProperty("m_VisualImage");
            m_VisualArrowImage = serializedObject.FindProperty("m_VisualArrowImage");

            m_Color = serializedObject.FindProperty("m_Color");

            m_DisplayRange = serializedObject.FindProperty("m_DisplayRange");
            m_TickMarks = serializedObject.FindProperty("m_TickMarks");
            m_IndependentTickMarkValue = serializedObject.FindProperty("m_IndependentTickMarkValue");
            m_IndependentTickMark = serializedObject.FindProperty("m_IndependentTickMark");
            m_UseIndependentTickMark = serializedObject.FindProperty("m_UseIndependentTickMark");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PrefixLabel(new GUIContent("General"));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_Direction);
            EditorGUILayout.PropertyField(m_Label);
            EditorGUILayout.PropertyField(m_Unit);
            EditorGUILayout.PropertyField(m_DisplayRange);
            EditorGUILayout.PropertyField(m_Color);
            EditorGUI.indentLevel--;

            m_ShowGraphics = EditorGUILayout.Foldout(m_ShowGraphics, "Graphics");
            if (m_ShowGraphics)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LabelText);
                EditorGUILayout.PropertyField(m_UnitText);
                EditorGUILayout.PropertyField(m_VisualImage);
                EditorGUILayout.PropertyField(m_VisualArrowImage);
                EditorGUI.indentLevel--;
            }

            m_ShowTickMarks = EditorGUILayout.Foldout(m_ShowTickMarks, "TickMarks");
            if(m_ShowTickMarks)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_UseIndependentTickMark);     
                EditorGUILayout.PropertyField(m_IndependentTickMarkValue);
                EditorGUILayout.PropertyField(m_IndependentTickMark);
                EditorGUILayout.PropertyField(m_TickMarks, true);
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}