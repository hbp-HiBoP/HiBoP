using UnityEngine;
using HBP.Data.Settings;
using Tools.Unity;

public class ApplicationManager : MonoBehaviour
{
    private void Awake()
    {
        ApplicationState.ProjectLoaded = null;
        ApplicationState.ProjectLoadedLocation = string.Empty;
        ApplicationState.GeneralSettings = ClassLoaderSaver.LoadFromJson<GeneralSettings>(GeneralSettings.PATH);
        ClassLoaderSaver.SaveToJSon(ApplicationState.GeneralSettings, GeneralSettings.PATH, true);
        ApplicationState.Theme = new HBP.UI.Theme.Theme();
        ApplicationState.CoroutineManager = FindObjectOfType<CoroutineManager>();
        ApplicationState.Module3D = FindObjectOfType<HBP.Module3D.HBP3DModule>();
        ApplicationState.DLLDebugManager = FindObjectOfType<HBP.Module3D.DLL.DLLDebugManager>();
        ApplicationState.DialogBoxManager = FindObjectOfType<DialogBoxManager>();
        ApplicationState.LoadingManager = FindObjectOfType<LoadingManager>();
    }
}
