using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tools.Unity.Graph
{
    [CustomEditor(typeof(Graph))]
    public class GraphEditor : UnityEditor.Editor
    {
        #region Properties
        // General
        SerializedProperty m_Title;
        SerializedProperty m_FontColor;
        SerializedProperty m_BackgroundColor;
        SerializedProperty m_UseDefaultDisplayRange;

        // Abscissa
        SerializedProperty m_AbscissaUnit;
        SerializedProperty m_AbscissaLabel;
        SerializedProperty m_AbscissaDisplayRange;
        SerializedProperty m_DefaultAbscissaDisplayRange;

        // Ordinate
        SerializedProperty m_OrdinateUnit;
        SerializedProperty m_OrdinateLabel;
        SerializedProperty m_OrdinateDisplayRange;
        SerializedProperty m_DefaultOrdinateDisplayRange;

        // Eventss
        bool m_ShowEvents = false;
        SerializedProperty m_OnChangeTitle;
        SerializedProperty m_OnChangeAbscissaUnit;
        SerializedProperty m_OnChangeAbscissaLabel;
        SerializedProperty m_OnChangeOrdinateUnit;
        SerializedProperty m_OnChangeOrdinateLabel;
        SerializedProperty m_OnChangeFontColor;
        SerializedProperty m_OnChangeBackgroundColor;
        SerializedProperty m_OnChangeOrdinateDisplayRange;
        SerializedProperty m_OnChangeAbscissaDisplayRange;
        #endregion

        #region Public Methods
        public void OnEnable()
        {
            // General
            m_Title = serializedObject.FindProperty("m_Title");
            m_AbscissaUnit = serializedObject.FindProperty("m_AbscissaUnit");
            m_AbscissaLabel = serializedObject.FindProperty("m_AbscissaLabel");
            m_OrdinateUnit = serializedObject.FindProperty("m_OrdinateUnit");
            m_OrdinateLabel = serializedObject.FindProperty("m_OrdinateLabel");
            m_FontColor = serializedObject.FindProperty("m_FontColor");
            m_BackgroundColor = serializedObject.FindProperty("m_BackgroundColor");
            m_OrdinateDisplayRange = serializedObject.FindProperty("m_OrdinateDisplayRange");
            m_AbscissaDisplayRange = serializedObject.FindProperty("m_AbscissaDisplayRange");
            m_DefaultOrdinateDisplayRange = serializedObject.FindProperty("m_DefaultOrdinateDisplayRange");
            m_DefaultAbscissaDisplayRange = serializedObject.FindProperty("m_DefaultAbscissaDisplayRange");
            m_UseDefaultDisplayRange = serializedObject.FindProperty("m_UseDefaultDisplayRange");

            // Events
            m_OnChangeTitle = serializedObject.FindProperty("m_OnChangeTitle");
            m_OnChangeAbscissaUnit = serializedObject.FindProperty("m_OnChangeAbscissaUnit");
            m_OnChangeAbscissaLabel = serializedObject.FindProperty("m_OnChangeAbscissaLabel");
            m_OnChangeOrdinateUnit = serializedObject.FindProperty("m_OnChangeOrdinateUnit");
            m_OnChangeOrdinateLabel = serializedObject.FindProperty("m_OnChangeOrdinateLabel");
            m_OnChangeFontColor = serializedObject.FindProperty("m_OnChangeFontColor");
            m_OnChangeBackgroundColor = serializedObject.FindProperty("m_OnChangeBackgroundColor");
            m_OnChangeAbscissaDisplayRange = serializedObject.FindProperty("m_OnChangeAbscissaDisplayRange");
            m_OnChangeOrdinateDisplayRange = serializedObject.FindProperty("m_OnChangeOrdinateDisplayRange");
        }

        public override void OnInspectorGUI()
        {
            // General
            EditorGUILayout.PrefixLabel(new GUIContent("General"));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_Title);
            EditorGUILayout.PropertyField(m_FontColor);
            EditorGUILayout.PropertyField(m_BackgroundColor);
            EditorGUILayout.PropertyField(m_UseDefaultDisplayRange);
            EditorGUI.indentLevel--;

            EditorGUILayout.PrefixLabel("Abscissa");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_AbscissaLabel, new GUIContent("Label"));
            EditorGUILayout.PropertyField(m_AbscissaUnit, new GUIContent("Unit"));
            EditorGUILayout.PropertyField(m_AbscissaDisplayRange, new GUIContent("Range"));
            EditorGUILayout.PropertyField(m_DefaultAbscissaDisplayRange, new GUIContent("Default Range"));
            EditorGUI.indentLevel--;

            EditorGUILayout.PrefixLabel("Ordinate");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OrdinateLabel, new GUIContent("Label"));
            EditorGUILayout.PropertyField(m_OrdinateUnit, new GUIContent("Unit"));
            EditorGUILayout.PropertyField(m_OrdinateDisplayRange, new GUIContent("Range"));
            EditorGUILayout.PropertyField(m_DefaultOrdinateDisplayRange, new GUIContent("Default Range"));
            EditorGUI.indentLevel--;

            // Events
            m_ShowEvents = EditorGUILayout.Foldout(m_ShowEvents, "Events");
            if(m_ShowEvents)
            {
                EditorGUILayout.PropertyField(m_OnChangeTitle);
                EditorGUILayout.PropertyField(m_OnChangeAbscissaUnit);
                EditorGUILayout.PropertyField(m_OnChangeAbscissaLabel);
                EditorGUILayout.PropertyField(m_OnChangeOrdinateUnit);
                EditorGUILayout.PropertyField(m_OnChangeOrdinateLabel);
                EditorGUILayout.PropertyField(m_OnChangeFontColor);
                EditorGUILayout.PropertyField(m_OnChangeBackgroundColor);
                EditorGUILayout.PropertyField(m_OnChangeAbscissaDisplayRange);
                EditorGUILayout.PropertyField(m_OnChangeOrdinateDisplayRange);
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}