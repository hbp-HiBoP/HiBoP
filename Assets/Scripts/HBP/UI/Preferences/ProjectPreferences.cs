using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;
using HBP.Data.Preferences;

namespace HBP.UI.Preferences
{
    public class ProjectPreferences : SavableWindow
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_LocationInputField;
        [SerializeField] FolderSelector m_PatientsDatabaseFolderSelector;
        [SerializeField] FolderSelector m_LocalizerDatabaseFolderSelector;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_PatientsDatabaseFolderSelector.interactable = value;
                m_LocalizerDatabaseFolderSelector.interactable = value;
                m_LocationInputField.interactable = false;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            ProjectSettings settings = ApplicationState.ProjectLoaded.Settings;
            settings.Name = m_NameInputField.text;
            settings.PatientDatabase = m_PatientsDatabaseFolderSelector.Folder;
            settings.LocalizerDatabase = m_LocalizerDatabaseFolderSelector.Folder;
            base.Save();
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {        
            ProjectSettings settings = ApplicationState.ProjectLoaded.Settings;
            m_NameInputField.text = settings.Name;
            m_LocationInputField.text = ApplicationState.ProjectLoadedLocation;
            m_PatientsDatabaseFolderSelector.Folder = settings.PatientDatabase;
            m_LocalizerDatabaseFolderSelector.Folder = settings.LocalizerDatabase;
        }
        #endregion
    }
}
