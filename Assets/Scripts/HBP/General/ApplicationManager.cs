using UnityEngine;
using HBP.Data.Preferences;
using Tools.Unity;
using HBP.UI;

public class ApplicationManager : MonoBehaviour
{
    private void Awake()
    {
        ApplicationState.ProjectLoaded = null;
        ApplicationState.ProjectLoadedLocation = string.Empty;
        ApplicationState.UserPreferences = ClassLoaderSaver.LoadFromJson<UserPreferences>(UserPreferences.PATH);
        ClassLoaderSaver.SaveToJSon(ApplicationState.UserPreferences, UserPreferences.PATH, true);
        ApplicationState.CoroutineManager = FindObjectOfType<CoroutineManager>();
        ApplicationState.Module3D = FindObjectOfType<HBP.Module3D.HBP3DModule>();
        ApplicationState.DLLDebugManager = FindObjectOfType<HBP.Module3D.DLL.DLLDebugManager>();
        ApplicationState.DialogBoxManager = FindObjectOfType<DialogBoxManager>();
        ApplicationState.LoadingManager = FindObjectOfType<LoadingManager>();
        ApplicationState.TooltipManager = FindObjectOfType<TooltipManager>();
        ApplicationState.MemoryManager = FindObjectOfType<MemoryManager>();
        ApplicationState.WindowsManager = FindObjectOfType<WindowsManager>();
    }
}
