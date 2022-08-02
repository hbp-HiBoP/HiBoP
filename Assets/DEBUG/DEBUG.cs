using UnityEditor;
using Theme.Components;
using HBP.Data;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
public static class DEBUG
{
    [MenuItem("DEBUG/Adrien/Main")]
    private static void Main()
    {
        HBP.Core.Data.Patient.LoadFromBIDSDatabase(@"Z:\BrainTV\HBP\Development\BaseBidsCCEPGrenoble\07-bids_20190416\07-bids", out HBP.Core.Data.Patient[] patients);
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