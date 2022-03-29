using UnityEditor;
using System.Linq;

namespace NewTheme
{
    [CustomEditor(typeof(Image))]
    public class ImageEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty sourceImageProperty = serializedObject.FindProperty("SourceImage");
            SerializedProperty colorProperty = serializedObject.FindProperty("Color");
            SerializedProperty materialProperty = serializedObject.FindProperty("Material");
            SerializedProperty typeProperty = serializedObject.FindProperty("ImageType");
            SerializedProperty preserveAspectProperty = serializedObject.FindProperty("PreserveAspect");
            SerializedProperty fillCenterProperty = serializedObject.FindProperty("FillCenter");
            SerializedProperty fillMethodProperty = serializedObject.FindProperty("FillMethod");
            SerializedProperty fillOriginProperty = serializedObject.FindProperty("FillOrigin");
            SerializedProperty fillAmountProperty = serializedObject.FindProperty("FillAmount");
            SerializedProperty ClockwiseProperty = serializedObject.FindProperty("Clockwise");

            EditorGUILayout.PropertyField(sourceImageProperty);
            EditorGUILayout.PropertyField(materialProperty);
            EditorGUILayout.PropertyField(typeProperty);
            UnityEngine.UI.Image.Type type = (UnityEngine.UI.Image.Type) typeProperty.enumValueIndex;

            EditorGUI.indentLevel++;
            switch (type)
            {
                case UnityEngine.UI.Image.Type.Simple:
                    EditorGUILayout.PropertyField(preserveAspectProperty);
                    break;
                case UnityEngine.UI.Image.Type.Sliced:
                    EditorGUILayout.PropertyField(fillCenterProperty);
                    break;
                case UnityEngine.UI.Image.Type.Tiled:
                    EditorGUILayout.PropertyField(fillCenterProperty);
                    break;
                case UnityEngine.UI.Image.Type.Filled:

                    EditorGUILayout.PropertyField(fillMethodProperty);
                    UnityEngine.UI.Image.FillMethod fillMethod = (UnityEngine.UI.Image.FillMethod)fillMethodProperty.enumValueIndex;
                    switch (fillMethod)
                    {
                        case UnityEngine.UI.Image.FillMethod.Horizontal:
                            fillOriginProperty.intValue = (int)(UnityEngine.UI.Image.OriginHorizontal)EditorGUILayout.EnumPopup("Fill Origin", (UnityEngine.UI.Image.OriginHorizontal)fillOriginProperty.intValue);
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Vertical:
                            fillOriginProperty.intValue = (int)(UnityEngine.UI.Image.OriginVertical)EditorGUILayout.EnumPopup("Fill Origin", (UnityEngine.UI.Image.OriginVertical)fillOriginProperty.intValue);
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Radial90:
                            fillOriginProperty.intValue = (int)(UnityEngine.UI.Image.Origin90)EditorGUILayout.EnumPopup("Fill Origin", (UnityEngine.UI.Image.Origin90)fillOriginProperty.intValue);
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(ClockwiseProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Radial180:
                            fillOriginProperty.intValue = (int)(UnityEngine.UI.Image.Origin180)EditorGUILayout.EnumPopup("Fill Origin", (UnityEngine.UI.Image.Origin180)fillOriginProperty.intValue);
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(ClockwiseProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Radial360:
                            fillOriginProperty.intValue = (int)(UnityEngine.UI.Image.Origin360) EditorGUILayout.EnumPopup("Fill Origin", (UnityEngine.UI.Image.Origin360)fillOriginProperty.intValue);
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(ClockwiseProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                    }
                    break;
            }
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }
    }
}