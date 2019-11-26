using UnityEditor;
using NewTheme.Components;

#if UNITY_EDITOR
public static class DEBUG
{
    [MenuItem("DEBUG/Adrien/Main")]
    private static void Main()
    {
        ApplicationState.WindowsManager.OpenModifier(ApplicationState.ProjectLoaded.Patients[0], false);
    }

    [MenuItem("Tools/Theme/ActiveThemeElement")]
    private static void ActiveThemeElement()
    {
        var selected = Selection.activeGameObject;
        var themeElements = selected.GetComponentsInChildren<ThemeElement>(true);
        foreach (var element in themeElements)
        {
            element.enabled = true;
        }
    }
}
#endif