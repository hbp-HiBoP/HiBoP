using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ThemeChecker
{
    [MenuItem("Tools/Check Theme/All")]
    public static void CheckThemeAll()
    {
        CheckTheme(GetAllGraphicsAndSelectables());
    }
    [MenuItem("Tools/Check Theme/Scene")]
    public static void CheckThemeScene()
    {
        CheckTheme(GetSceneGraphicsAndSelectables());
    }
    public static List<MonoBehaviour> GetAllGraphicsAndSelectables()
    {
        List<MonoBehaviour> elements = new List<MonoBehaviour>();
        elements.AddRange(Resources.FindObjectsOfTypeAll<Graphic>());
        elements.AddRange(Resources.FindObjectsOfTypeAll<Selectable>());
        return elements;
    }
    public static List<MonoBehaviour> GetSceneGraphicsAndSelectables()
    {
        List<MonoBehaviour> elements = new List<MonoBehaviour>();

        foreach (Graphic graphic in Resources.FindObjectsOfTypeAll(typeof(Graphic)) as Graphic[])
        {
            if (PrefabUtility.GetPrefabParent(graphic.gameObject) == null && PrefabUtility.GetPrefabObject(graphic.gameObject) != null)
                continue;

            if (graphic.GetComponent<Mask>())
                continue;

            if (graphic.GetComponent<RectMask2D>())
                continue;

            elements.Add(graphic);
        }
        foreach (Selectable selectable in Resources.FindObjectsOfTypeAll(typeof(Selectable)) as Selectable[])
        {
            if(PrefabUtility.GetPrefabParent(selectable.gameObject) == null && PrefabUtility.GetPrefabObject(selectable.gameObject) != null)
                continue;

            elements.Add(selectable);
        }

        return elements;
    }

    public static void CheckTheme(List<MonoBehaviour> list)
    {
        List<string> fullNames = new List<string>();
        foreach (var element in list)
        {
            if (!element.GetComponent<NewTheme.Components.ThemeElement>())
            {
                fullNames.Add(element.transform.FullName());
            }
        }
        fullNames.Sort();
        foreach (var name in fullNames)
        {
            Debug.LogWarningFormat("This object has a graphic or a selectable and no theme element: {0}", name);
        }
    }
}
