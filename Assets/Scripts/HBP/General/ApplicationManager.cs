using UnityEngine;
using HBP.Data.Preferences;
using Tools.Unity;
using System.IO;
using HBP.UI;

namespace HBP
{
    public class ApplicationManager : MonoBehaviour
    {
        private void Awake()
        {
            ApplicationState.ProjectLoaded = null;
            ApplicationState.ProjectLoadedLocation = string.Empty;
            if (new FileInfo(UserPreferences.PATH).Exists)
            {
                ApplicationState.UserPreferences = ClassLoaderSaver.LoadFromJson<UserPreferences>(UserPreferences.PATH);
            }
            else
            {
                ApplicationState.UserPreferences = new UserPreferences();
            }
            ClassLoaderSaver.SaveToJSon(ApplicationState.UserPreferences, UserPreferences.PATH, true);
            ApplicationState.CoroutineManager = FindObjectOfType<CoroutineManager>();
            ApplicationState.Module3D = FindObjectOfType<Module3D.HBP3DModule>();
            ApplicationState.DLLDebugManager = FindObjectOfType<Module3D.DLL.DLLDebugManager>();
            ApplicationState.DialogBoxManager = FindObjectOfType<DialogBoxManager>();
            ApplicationState.LoadingManager = FindObjectOfType<LoadingManager>();
            ApplicationState.TooltipManager = FindObjectOfType<TooltipManager>();
            ApplicationState.MemoryManager = FindObjectOfType<MemoryManager>();
            ApplicationState.WindowsManager = FindObjectOfType<WindowsManager>();
            ApplicationState.Module3DUI = FindObjectOfType<UI.Module3D.Module3DUI>();
            ApplicationState.ProjectTMPFolder = GetProjectTMPDirectory();
        }

        private void OnDestroy()
        {
            DataManager.Clear();
            string tmpDir = ApplicationState.ProjectLoadedTMPFullPath;
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
        }

        private string GetProjectTMPDirectory()
        {
            string tmpDir = Path.Combine(Application.persistentDataPath, ".tmp");
            if (!Directory.Exists(tmpDir))
            {
                DirectoryInfo di = Directory.CreateDirectory(tmpDir);
                di.Attributes |= FileAttributes.Hidden;
            }
            return tmpDir;
        }
    }
}
