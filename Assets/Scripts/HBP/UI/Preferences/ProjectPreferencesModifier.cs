using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class ProjectPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] InputField m_DefaultName;
        [SerializeField] Tools.Unity.FolderSelector m_DefaultLocation;
        [SerializeField] Tools.Unity.FolderSelector m_DefaultPatientDatabase;
        [SerializeField] Tools.Unity.FolderSelector m_DefaultLocalizerDatabase;
        #endregion

        #region Public Methods
        public void Set()
        {
            Data.Preferences.ProjectPreferences preferences = ApplicationState.UserPreferences.General.Project;
            m_DefaultName.text = preferences.DefaultName;
            m_DefaultLocation.Folder = preferences.DefaultLocation;
            m_DefaultPatientDatabase.Folder = preferences.DefaultPatientDatabase;
            m_DefaultLocalizerDatabase.Folder = preferences.DefaultLocalizerDatabase;
        }

        public void Save()
        {
            Data.Preferences.ProjectPreferences preferences = ApplicationState.UserPreferences.General.Project;
            preferences.DefaultName = m_DefaultName.text;
            preferences.DefaultLocation = m_DefaultLocation.Folder;
            preferences.DefaultPatientDatabase = m_DefaultPatientDatabase.Folder;
            preferences.DefaultLocalizerDatabase = m_DefaultLocalizerDatabase.Folder;
        }
        #endregion
    }
}