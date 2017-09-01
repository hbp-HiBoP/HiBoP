using UnityEngine;
using UnityEditor;
using HBP.UI.Theme;

public class ThemeMenu
{
    [MenuItem("UI/Set Theme")]
    public static void SetTheme()
    {
        Theme theme = new Theme();
        ThemeManager[] windowThemeGestions = Object.FindObjectsOfType<ThemeManager>();
        foreach (ThemeManager windowThemeGestion in windowThemeGestions)
        {
            windowThemeGestion.Set(theme);
        }
    }
}
