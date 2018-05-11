using UnityEditor;

namespace NewTheme
{
    [CustomEditor(typeof(Text))]
    public class TextEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            SerializedProperty fontProperty = serializedObject.FindProperty("Font");
            EditorGUILayout.PropertyField(fontProperty);

            SerializedProperty fontStyleProperty = serializedObject.FindProperty("FontStyle");
            EditorGUILayout.PropertyField(fontStyleProperty);

            SerializedProperty fontSizeProperty = serializedObject.FindProperty("FontSize");
            EditorGUILayout.PropertyField(fontSizeProperty);

            SerializedProperty lineSpacingProperty = serializedObject.FindProperty("LineSpacing");
            EditorGUILayout.PropertyField(lineSpacingProperty);

            SerializedProperty richTextProperty = serializedObject.FindProperty("RichText");
            EditorGUILayout.PropertyField(richTextProperty);

            EditorGUI.indentLevel--;

            //EditorGUILayout.LabelField("Paragraph", EditorStyles.boldLabel);
            //EditorGUI.indentLevel++;

            //SerializedProperty alignmentProperty = serializedObject.FindProperty("Alignment");
            //EditorGUILayout.PropertyField(alignmentProperty);

            //SerializedProperty alignByGeometryProperty = serializedObject.FindProperty("AlignByGeometry");
            //EditorGUILayout.PropertyField(alignByGeometryProperty);

            //SerializedProperty horizontalOverflowProperty = serializedObject.FindProperty("HorizontalOverflow");
            //EditorGUILayout.PropertyField(horizontalOverflowProperty);

            //SerializedProperty verticalOverflowProperty = serializedObject.FindProperty("VerticalOverflow");
            //EditorGUILayout.PropertyField(verticalOverflowProperty);

            //SerializedProperty bestFitProperty = serializedObject.FindProperty("BestFit");
            //EditorGUILayout.PropertyField(bestFitProperty);

            //EditorGUI.indentLevel--;

            //SerializedProperty colorProperty = serializedObject.FindProperty("Color");
            //EditorGUILayout.PropertyField(colorProperty);

            SerializedProperty materialProperty = serializedObject.FindProperty("Material");
            EditorGUILayout.PropertyField(materialProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}