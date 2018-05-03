using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;

namespace HBP.UI.Preferences
{
    public class ProjectPreferences : Window
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FolderSelector m_PatientsDatabaseFolderSelector;
        [SerializeField] FolderSelector m_LocalizerDatabaseFolderSelector;
        #endregion

        #region Public Methods
        public void Save()
        {
            ApplicationState.ProjectLoaded.Settings.Name = m_NameInputField.text;
            ApplicationState.ProjectLoaded.Settings.PatientDatabase = m_PatientsDatabaseFolderSelector.Folder;
            ApplicationState.ProjectLoaded.Settings.LocalizerDatabase = m_LocalizerDatabaseFolderSelector.Folder;
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            m_NameInputField.text = ApplicationState.ProjectLoaded.Settings.Name;
            m_PatientsDatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;
            m_LocalizerDatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
        }
        #endregion
    }
}
