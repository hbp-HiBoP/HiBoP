using UnityEngine;
using UnityEditor;
using NewTheme.Components;
using HBP.UI.Anatomy;

#if UNITY_EDITOR
public static class DEBUG
{
    [MenuItem("DEBUG/Adrien/Main")]
    private static void Main()
    {
        PatientModifier patientModifier = GameObject.FindObjectOfType<PatientModifier>();
        patientModifier.Item = new HBP.Data.Patient();
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