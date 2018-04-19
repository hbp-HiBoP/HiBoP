using UnityEngine.UI;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI
{
    public class SaveProjectAs : Window
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FolderSelector m_LocationFolderSelector;
        #endregion

        #region Public Methods
        public void SaveAs()
        {
            Data.Settings.ProjectSettings l_ps = new Data.Settings.ProjectSettings(m_NameInputField.text, ApplicationState.ProjectLoaded.Settings.PatientDatabase, ApplicationState.ProjectLoaded.Settings.LocalizerDatabase);
            ApplicationState.ProjectLoaded.Settings = l_ps;
            FindObjectOfType<ProjectLoaderSaver>().Save(m_LocationFolderSelector.Folder);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            m_NameInputField.text = ApplicationState.ProjectLoaded.Settings.Name;
            m_LocationFolderSelector.Folder = ApplicationState.ProjectLoadedLocation;
        }
        #endregion
    }
}