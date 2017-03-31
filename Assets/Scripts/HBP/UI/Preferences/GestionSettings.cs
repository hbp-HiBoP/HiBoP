using UnityEngine;
using Tools.Unity;
using HBP.Data.Settings;

namespace HBP.UI.Settings
{
    public class GestionSettings : MonoBehaviour
    {
        void Awake()
        {
            ApplicationState.Theme = new Theme.Theme();
            ApplicationState.GeneralSettings = ClassLoaderSaver.LoadFromJson<GeneralSettings>(GeneralSettings.PATH);
            ClassLoaderSaver.SaveToJSon(ApplicationState.GeneralSettings, GeneralSettings.PATH, true);
        }

        public void Exit()
        {
            Application.Quit();
        }
    }
}