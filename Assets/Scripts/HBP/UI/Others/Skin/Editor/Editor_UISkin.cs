using UnityEngine;
using UnityEditor;
using HBP.UI.Skin;

public class Editor_UISkin
{
    [MenuItem("UI/UpdateSkin")]
    public static void UpdateSkin()
    {
        Skin l_skin = new Skin();
        MenuSkinGestion[] menuSkin = Object.FindObjectsOfType<MenuSkinGestion>();
        foreach(MenuSkinGestion m in menuSkin)
        {
            m.Set(l_skin);
        }
    }
}
