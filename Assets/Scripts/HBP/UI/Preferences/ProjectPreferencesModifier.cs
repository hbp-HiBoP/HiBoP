using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;

namespace HBP.UI.Preferences
{
    public class ProjectPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] InputField m_DefaultName;
        [SerializeField] FolderSelector m_DefaultLocation;
        [SerializeField] FolderSelector m_DefaultPatientDatabase;
        [SerializeField] FolderSelector m_DefaultLocalizerDatabase;
        #endregion

        #region Public Methods
        public void Initialize()
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

        public void SetInteractable(bool interactable)
        {
            m_DefaultName.interactable = interactable;
            m_DefaultLocation.interactable = interactable;
            m_DefaultPatientDatabase.interactable = interactable;
            m_DefaultLocalizerDatabase.interactable = interactable;
        }
        #endregion
    }
}