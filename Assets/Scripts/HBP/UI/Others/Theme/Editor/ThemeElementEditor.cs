using UnityEditor;

[CustomEditor(typeof(ThemeElement))]
public class ThemeElementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ThemeElement themeElement = target as ThemeElement;
        themeElement.Type = (ThemeElement.ElementType) EditorGUILayout.EnumPopup("Type", themeElement.Type) ;
        themeElement.IgnoreTheme = EditorGUILayout.Toggle("Ignore Theme",themeElement.IgnoreTheme);
    }
}
