using UnityEngine;
using HBP.Data.Settings;

public class GestionSettings : MonoBehaviour
{
    void Awake()
    {
        ApplicationState.GeneralSettings = GeneralSettings.LoadJson();
        ApplicationState.GeneralSettings.SaveJSon();
        ApplicationState.Skin = new HBP.UI.Skin.Skin();
    }

    public void Exit()
    {
        Application.Quit();
    }
}