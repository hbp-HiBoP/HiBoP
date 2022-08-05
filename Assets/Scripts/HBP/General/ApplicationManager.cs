using UnityEngine;
using Tools.Unity;
using System.IO;
using HBP.Display.Preferences;

namespace HBP.Core.Data
{
    public class ApplicationManager : MonoBehaviour
    {
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
#if UNITY_EDITOR
            ApplicationState.DataPath =  Path.Combine(Application.dataPath, "Data");
#else
            ApplicationState.DataPath = Path.Combine(Application.dataPath, "..", "Data");
#endif
            ApplicationState.TMPFolder = Path.Combine(Application.persistentDataPath, "tmp");
            ApplicationState.ExtractProjectFolder = Path.Combine(Application.persistentDataPath, ApplicationState.InstanceID);
        }
        private void OnDestroy()
        {
            DataManager.Clear();
            string tmpDir = ApplicationState.ExtractProjectFolder;
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }
        }
        #endregion
    }
}
