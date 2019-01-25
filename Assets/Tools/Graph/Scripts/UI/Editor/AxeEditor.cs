using UnityEngine;
using UnityEditor;

namespace Tools.Unity.Graph
{
    [CustomEditor(typeof(Axe))]
    public class AxeEditor : UnityEditor.Editor
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
        SerializedProperty m_Color;
        SerializedProperty m_DisplayRange;
        SerializedProperty m_TickMarkContainer;
        SerializedProperty m_UsedTickMarks;
        SerializedProperty m_IndependantTickMark;
        SerializedProperty m_TickMarkPool;
        #endregion

        public void OnEnable()
        {
            m_Direction = serializedObject.FindProperty("m_Direction");
            m_LabelText = serializedObject.FindProperty("m_LabelText");
            m_Label = serializedObject.FindProperty("m_Label");

            m_UnitText = serializedObject.FindProperty("m_UnitText");
            m_Unit = serializedObject.FindProperty("m_Unit");

            m_Color = serializedObject.FindProperty("m_Color");

            m_DisplayRange = serializedObject.FindProperty("m_DisplayRange");

            m_TickMarkContainer = serializedObject.FindProperty("m_TickMarkContainer");
            m_UsedTickMarks = serializedObject.FindProperty("m_UsedTickMarks");
            m_IndependantTickMark = serializedObject.FindProperty("m_IndependentTickMark");
            m_TickMarkPool = serializedObject.FindProperty("m_TickMarkPool");
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
                EditorGUI.indentLevel--;
            }

            m_ShowTickMarks = EditorGUILayout.Foldout(m_ShowTickMarks, "TickMarks");
            if(m_ShowTickMarks)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_TickMarkContainer, new GUIContent("Container"));

                GUI.enabled = false;
                EditorGUILayout.PropertyField(m_IndependantTickMark, new GUIContent("Independent"), true);
                EditorGUILayout.PropertyField(m_UsedTickMarks, new GUIContent("Used"), true);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(m_TickMarkPool, new GUIContent("Pool"), true);
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}