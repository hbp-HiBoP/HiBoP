using UnityEngine;
using UnityEditor;
using HBP.UI.Theme;

public class ThemeMenu
{
    [MenuItem("UI/Set Theme")]
    public static void SetTheme()
    {
        foreach (ThemeElement element in Object.FindObjectsOfType<ThemeElement>())
        {
            element.Set((Theme)Resources.Load("Themes/Dark"));
        }
    }

    [MenuItem("Assets/Initialize Theme", false, 100)]
    public static void InitializeTheme()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
        Theme theme = (Theme) AssetDatabase.LoadAssetAtPath(assetPath, typeof(Theme));
        theme.SetDefaultValues();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Initialize Theme", true, 100)]
    public static bool CanInitializeTheme()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
        Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
        return asset is Theme;
    }
}
