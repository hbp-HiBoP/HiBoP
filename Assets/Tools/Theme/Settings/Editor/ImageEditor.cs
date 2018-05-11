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
                    SerializedProperty fillMethodProperty = serializedObject.FindProperty("FillMethod");
                    SerializedProperty fillOriginProperty = serializedObject.FindProperty("FillOrigin");
                    SerializedProperty fillAmountProperty = serializedObject.FindProperty("FillAmount");
                    SerializedProperty ClockwiseProperty = serializedObject.FindProperty("Clockwise");
                    EditorGUILayout.PropertyField(fillMethodProperty);
                    UnityEngine.UI.Image.FillMethod fillMethod = (UnityEngine.UI.Image.FillMethod)fillMethodProperty.enumValueIndex;
                    switch (fillMethod)
                    {
                        case UnityEngine.UI.Image.FillMethod.Horizontal:
                            fillOriginProperty.intValue = EditorGUILayout.IntPopup("Fill Origin",fillOriginProperty.intValue, System.Enum.GetNames(typeof(UnityEngine.UI.Image.OriginHorizontal)), System.Enum.GetValues(typeof(UnityEngine.UI.Image.OriginHorizontal)).OfType<int>().ToArray());
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Vertical:
                            fillOriginProperty.intValue = EditorGUILayout.IntPopup("Fill Origin", fillOriginProperty.intValue, System.Enum.GetNames(typeof(UnityEngine.UI.Image.OriginVertical)), System.Enum.GetValues(typeof(UnityEngine.UI.Image.OriginVertical)).OfType<int>().ToArray());
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Radial90:
                            fillOriginProperty.intValue = EditorGUILayout.IntPopup("Fill Origin", fillOriginProperty.intValue, System.Enum.GetNames(typeof(UnityEngine.UI.Image.Origin90)), System.Enum.GetValues(typeof(UnityEngine.UI.Image.Origin90)).OfType<int>().ToArray());
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(ClockwiseProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Radial180:
                            fillOriginProperty.intValue = EditorGUILayout.IntPopup("Fill Origin", fillOriginProperty.intValue, System.Enum.GetNames(typeof(UnityEngine.UI.Image.Origin180)), System.Enum.GetValues(typeof(UnityEngine.UI.Image.Origin180)).OfType<int>().ToArray());
                            EditorGUILayout.PropertyField(fillAmountProperty);
                            EditorGUILayout.PropertyField(ClockwiseProperty);
                            EditorGUILayout.PropertyField(preserveAspectProperty);
                            break;
                        case UnityEngine.UI.Image.FillMethod.Radial360:
                            fillOriginProperty.intValue = EditorGUILayout.IntPopup("Fill Origin", fillOriginProperty.intValue, System.Enum.GetNames(typeof(UnityEngine.UI.Image.Origin360)), System.Enum.GetValues(typeof(UnityEngine.UI.Image.Origin360)).OfType<int>().ToArray());
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