using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Tools.Unity.Components.Editor
{
    [CustomEditor(typeof(MinimumSizeTester))]
    [CanEditMultipleObjects]
    public class MinimumSizeTesterEditor : UnityEditor.Editor
    {
        #region Properties
        SerializedProperty m_UseMinWidth;
        SerializedProperty m_UseMinHeight;
        SerializedProperty m_MinWidth;
        SerializedProperty m_MinHeight;
        SerializedProperty m_Minimized;
        SerializedProperty m_OnChangeMinimized;
        #endregion

        void OnEnable()
        {
            m_UseMinWidth = serializedObject.FindProperty("m_UseMinWidth");
            m_UseMinHeight = serializedObject.FindProperty("m_UseMinHeight");
            m_MinWidth = serializedObject.FindProperty("m_MinWidth");
            m_MinHeight = serializedObject.FindProperty("m_MinHeight");
            m_Minimized = serializedObject.FindProperty("m_Minimized");
            m_OnChangeMinimized = serializedObject.FindProperty("m_OnChangeMinimized");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Conditions");

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = m_UseMinWidth.boolValue;
            EditorGUILayout.PropertyField(m_MinWidth, new GUIContent("Width"), new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            GUI.enabled = true; 
            EditorGUILayout.PropertyField(m_UseMinWidth, new GUIContent(""), new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MaxWidth(30), GUILayout.MinWidth(30) });
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = m_UseMinHeight.boolValue;
            EditorGUILayout.PropertyField(m_MinHeight, new GUIContent("Height"), new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            GUI.enabled = true;
            EditorGUILayout.PropertyField(m_UseMinHeight, new GUIContent(""), new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MaxWidth(30), GUILayout.MinWidth(30) });
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Output");

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_Minimized);
            EditorGUILayout.PropertyField(m_OnChangeMinimized);
            EditorGUI.indentLevel--;



            serializedObject.ApplyModifiedProperties();
        }
    }
}