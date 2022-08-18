using UnityEditor;
using UnityEngine;

namespace HBP.UI.Tools.Graphs
{
    [CustomEditor(typeof(SimpleGraph))]
    public class SimpleGraphEditor : Editor
    {
        #region Properties
        // General
        SerializedProperty m_Title;
        SerializedProperty m_FontColor;
        SerializedProperty m_BackgroundColor;
        SerializedProperty m_UseDefaultDisplayRange;

        // Abscissa
        SerializedProperty m_AbscissaDisplayRange;
        SerializedProperty m_DefaultAbscissaDisplayRange;


        // Ordinate
        SerializedProperty m_OrdinateDisplayRange;
        SerializedProperty m_DefaultOrdinateDisplayRange;

        // Curves
        SerializedProperty m_Curves;

        // Events
        bool m_ShowEvents = false;
        SerializedProperty m_OnChangeTitle;
        SerializedProperty m_OnChangeFontColor;
        SerializedProperty m_OnChangeBackgroundColor;
        SerializedProperty m_OnChangeOrdinateDisplayRange;
        SerializedProperty m_OnChangeAbscissaDisplayRange;
        SerializedProperty m_OnChangeUseDefaultRange;
        SerializedProperty m_OnChangeSelected;
        SerializedProperty m_OnChangeCurves;
        #endregion

        #region Public Methods
        public void OnEnable()
        {
            // General
            m_Title = serializedObject.FindProperty("m_Title");
            m_FontColor = serializedObject.FindProperty("m_FontColor");
            m_BackgroundColor = serializedObject.FindProperty("m_BackgroundColor");
            m_OrdinateDisplayRange = serializedObject.FindProperty("m_OrdinateDisplayRange");
            m_AbscissaDisplayRange = serializedObject.FindProperty("m_AbscissaDisplayRange");
            m_DefaultOrdinateDisplayRange = serializedObject.FindProperty("m_DefaultOrdinateDisplayRange");
            m_DefaultAbscissaDisplayRange = serializedObject.FindProperty("m_DefaultAbscissaDisplayRange");
            m_UseDefaultDisplayRange = serializedObject.FindProperty("m_UseDefaultDisplayRange");
            m_Curves = serializedObject.FindProperty("m_Curves");

            // Events
            m_OnChangeTitle = serializedObject.FindProperty("m_OnChangeTitle");
            m_OnChangeFontColor = serializedObject.FindProperty("m_OnChangeFontColor");
            m_OnChangeBackgroundColor = serializedObject.FindProperty("m_OnChangeBackgroundColor");
            m_OnChangeAbscissaDisplayRange = serializedObject.FindProperty("m_OnChangeAbscissaDisplayRange");
            m_OnChangeOrdinateDisplayRange = serializedObject.FindProperty("m_OnChangeOrdinateDisplayRange");
            m_OnChangeUseDefaultRange = serializedObject.FindProperty("m_OnChangeUseDefaultRange");
            m_OnChangeSelected = serializedObject.FindProperty("m_OnChangeSelected");
            m_OnChangeCurves = serializedObject.FindProperty("m_OnChangeCurves");
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
            EditorGUILayout.PropertyField(m_AbscissaDisplayRange, new GUIContent("Range"));
            EditorGUILayout.PropertyField(m_DefaultAbscissaDisplayRange, new GUIContent("Default Range"));
            EditorGUI.indentLevel--;

            EditorGUILayout.PrefixLabel("Ordinate");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OrdinateDisplayRange, new GUIContent("Range"));
            EditorGUILayout.PropertyField(m_DefaultOrdinateDisplayRange, new GUIContent("Default Range"));
            EditorGUI.indentLevel--;
        
            EditorGUILayout.PropertyField(m_Curves, true);

            // Events
            m_ShowEvents = EditorGUILayout.Foldout(m_ShowEvents, "Events");
            if(m_ShowEvents)
            {
                EditorGUILayout.PropertyField(m_OnChangeTitle);
                EditorGUILayout.PropertyField(m_OnChangeFontColor);
                EditorGUILayout.PropertyField(m_OnChangeBackgroundColor);
                EditorGUILayout.PropertyField(m_OnChangeAbscissaDisplayRange);
                EditorGUILayout.PropertyField(m_OnChangeOrdinateDisplayRange);
                EditorGUILayout.PropertyField(m_OnChangeUseDefaultRange);
                EditorGUILayout.PropertyField(m_OnChangeSelected);
                EditorGUILayout.PropertyField(m_OnChangeCurves);
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}