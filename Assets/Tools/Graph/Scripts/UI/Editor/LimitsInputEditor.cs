using UnityEngine;
using UnityEditor;

namespace Tools.Unity.Graph.Editor
{
    [CustomEditor(typeof(LimitsInput))]
    public class LimitsInputEditor : UnityEditor.Editor
    {
        bool abscissaFoldout = true;
        bool ordinateFoldout = true;
        bool formatFoldout = true;

        public override void OnInspectorGUI()
        {
            abscissaFoldout = EditorGUILayout.Foldout(abscissaFoldout, "Abscissa");
            if (abscissaFoldout)
            {
                EditorGUI.indentLevel++;
                SerializedProperty AbscissaTextProperty = serializedObject.FindProperty("AbscissaText");
                SerializedProperty MinAbscissaInputFieldProperty = serializedObject.FindProperty("MinAbscissaInputField");
                SerializedProperty MaxAbscissaInputFieldProperty = serializedObject.FindProperty("MaxAbscissaInputField");

                EditorGUILayout.PropertyField(AbscissaTextProperty, new GUIContent("Label"));
                EditorGUILayout.PropertyField(MinAbscissaInputFieldProperty, new GUIContent("Min InputField"));
                EditorGUILayout.PropertyField(MaxAbscissaInputFieldProperty, new GUIContent("Max InputField"));
                EditorGUI.indentLevel--;
            }

            ordinateFoldout = EditorGUILayout.Foldout(ordinateFoldout, "Ordinate");
            if (ordinateFoldout)
            {
                EditorGUI.indentLevel++;
                SerializedProperty OrdinateTextProperty = serializedObject.FindProperty("OrdinateText");
                SerializedProperty MinOrdinateInputFieldProperty = serializedObject.FindProperty("MinOrdinateInputField");
                SerializedProperty MaxOrdinateInputFieldProperty = serializedObject.FindProperty("MaxOrdinateInputField");

                EditorGUILayout.PropertyField(OrdinateTextProperty, new GUIContent("Label"));
                EditorGUILayout.PropertyField(MinOrdinateInputFieldProperty, new GUIContent("Min InputField"));
                EditorGUILayout.PropertyField(MaxOrdinateInputFieldProperty, new GUIContent("Max InputField"));
                EditorGUI.indentLevel--;
            }

            formatFoldout = EditorGUILayout.Foldout(formatFoldout, "Format");
            if (formatFoldout)
            {
                EditorGUI.indentLevel++;
                SerializedProperty FormatProperty = serializedObject.FindProperty("Format");
                SerializedProperty CultureInfoProperty = serializedObject.FindProperty("CultureInfo");

                EditorGUILayout.PropertyField(FormatProperty);
                EditorGUILayout.PropertyField(CultureInfoProperty);
                EditorGUI.indentLevel--;
            }

            SerializedProperty OnChangeLimitsProperty = serializedObject.FindProperty("OnChangeLimits");
            EditorGUILayout.PropertyField(OnChangeLimitsProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}