using UnityEditor;

[CustomEditor(typeof(VerticalUIFitter))]
public class VerticalUIFitterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        VerticalUIFitter verticalUIFitter = target as VerticalUIFitter;
        verticalUIFitter.Rotation = (VerticalUIFitter.RotationEnum)EditorGUILayout.EnumPopup("Rotation", verticalUIFitter.Rotation);
    }
}
