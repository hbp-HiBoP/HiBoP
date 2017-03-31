using UnityEngine;
using UnityEditor;
using HBP.UI.Theme;

public class ThemeMenu
{
    [MenuItem("UI/Set Theme")]
    public static void SetTheme()
    {
        Theme theme = new Theme();
        WindowThemeGestion[] windowThemeGestions = Object.FindObjectsOfType<WindowThemeGestion>();
        foreach (WindowThemeGestion windowThemeGestion in windowThemeGestions)
        {
            windowThemeGestion.Set(theme);
        }
    }
}
