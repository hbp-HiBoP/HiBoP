using UnityEditor;

[CustomEditor(typeof(ImageAspectFitter))]
public class ImageAspectFitterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ImageAspectFitter imageFitter = target as ImageAspectFitter;
        imageFitter.AspectMode = (ImageAspectFitter.AspectModeEnum) EditorGUILayout.EnumPopup("Aspect Mode",imageFitter.AspectMode);
    }
}
