using UnityEngine;
using UnityEditor;

namespace HBP.UI.Informations.Graphs
{
    [CustomEditor(typeof(Axis))]
    public class AxisEditor : Editor
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
        SerializedProperty m_IndependantValue;
        SerializedProperty m_IndependantTickMark;
        SerializedProperty m_UseIndependantTickMark;
        SerializedProperty m_Format;
        SerializedProperty m_CultureInfo;
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
            m_IndependantValue = serializedObject.FindProperty("m_IndependantValue");
            m_IndependantTickMark = serializedObject.FindProperty("m_IndependantTickMark");
            m_UseIndependantTickMark = serializedObject.FindProperty("m_UseIndependantTickMark");

            m_Format = serializedObject.FindProperty("m_Format");
            m_CultureInfo = serializedObject.FindProperty("m_CultureInfo");
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
                EditorGUILayout.PropertyField(m_Format);
                EditorGUILayout.PropertyField(m_CultureInfo);
                EditorGUILayout.PropertyField(m_UseIndependantTickMark);     
                EditorGUILayout.PropertyField(m_IndependantValue);
                EditorGUILayout.PropertyField(m_IndependantTickMark);
                EditorGUILayout.PropertyField(m_TickMarks, true);
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}