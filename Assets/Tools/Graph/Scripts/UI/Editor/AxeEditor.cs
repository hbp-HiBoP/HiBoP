using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Tools.Unity.Graph
{
    [CustomEditor(typeof(Axe))]
    public class AxeEditor : UnityEditor.Editor
    {
        #region Properties
        bool showTickMarks = true;
        SerializedProperty m_Type;
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
            m_Type = serializedObject.FindProperty("m_Type");
            m_LabelText = serializedObject.FindProperty("m_LabelText");
            m_Label = serializedObject.FindProperty("m_Label");

            m_UnitText = serializedObject.FindProperty("m_UnitText");
            m_Unit = serializedObject.FindProperty("m_Unit");

            m_Color = serializedObject.FindProperty("m_Color");

            m_DisplayRange = serializedObject.FindProperty("m_DisplayRange");

            m_TickMarkContainer = serializedObject.FindProperty("m_TickMarkContainer");
            m_UsedTickMarks = serializedObject.FindProperty("UsedTickMarks");
            m_IndependantTickMark = serializedObject.FindProperty("IndependentTickMark");
            m_TickMarkPool = serializedObject.FindProperty("TickMarkPool");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_Type);

            EditorGUILayout.PropertyField(m_Label);
            EditorGUILayout.PropertyField(m_LabelText);

            EditorGUILayout.PropertyField(m_Unit);
            EditorGUILayout.PropertyField(m_UnitText);

            EditorGUILayout.PropertyField(m_Color);

            EditorGUILayout.PropertyField(m_DisplayRange);


            SerializedProperty UsedTickMarks = serializedObject.FindProperty("UsedTickMarks");
            SerializedProperty IndependantTickMark = serializedObject.FindProperty("IndependentTickMark");
            SerializedProperty TickMarkPool = serializedObject.FindProperty("TickMarkPool");
            showTickMarks = EditorGUILayout.Foldout(showTickMarks, "TickMarks");
            if(showTickMarks)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(IndependantTickMark, new GUIContent("Independant"), true);
                EditorGUILayout.PropertyField(UsedTickMarks, new GUIContent("Used"), true);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(TickMarkPool, new GUIContent("Pool"), true);
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}