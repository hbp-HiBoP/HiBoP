using UnityEngine;
using HBP.Data.Preferences;
using Tools.Unity;
using System.IO;
using HBP.UI;
using HBP.Module3D;
using HBP.Module3D.DLL;
using HBP.UI.Module3D;

namespace HBP
{
    public class ApplicationManager : MonoBehaviour
    {
        #region Properties
        [SerializeField] CoroutineManager m_CoroutineManager;
        [SerializeField] HBP3DModule m_Module3D;
        [SerializeField] DLLDebugManager m_DLLDebugManager;
        [SerializeField] DialogBoxManager m_DialogBoxManager;
        [SerializeField] LoadingManager m_LoadingManager;
        [SerializeField] TooltipManager m_TooltipManager;
        [SerializeField] MemoryManager m_MemoryManager;
        [SerializeField] WindowsManager m_WindowsManager;
        [SerializeField] Module3DUI m_Module3DUI;
        [SerializeField] GlobalExceptionManager m_GlobalExceptionManager;
        [SerializeField] ColorPicker m_ColorPicker;
        #endregion

        #region Private Methods
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
            ApplicationState.CoroutineManager = m_CoroutineManager;
            ApplicationState.Module3D = m_Module3D;
            ApplicationState.DLLDebugManager = m_DLLDebugManager;
            ApplicationState.DialogBoxManager = m_DialogBoxManager;
            ApplicationState.LoadingManager = m_LoadingManager;
            ApplicationState.TooltipManager = m_TooltipManager;
            ApplicationState.MemoryManager = m_MemoryManager;
            ApplicationState.WindowsManager = m_WindowsManager;
            ApplicationState.Module3DUI = m_Module3DUI;
            ApplicationState.GlobalExceptionManager = m_GlobalExceptionManager;
            ApplicationState.ColorPicker = m_ColorPicker;
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
        #endregion
    }
}
