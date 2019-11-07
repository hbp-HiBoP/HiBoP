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
                m_LocationFolderSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SaveAs()
        {
            Data.ProjectPreferences l_ps = new Data.ProjectPreferences(m_NameInputField.text, ApplicationState.ProjectLoaded.Preferences.PatientDatabase, ApplicationState.ProjectLoaded.Preferences.LocalizerDatabase);
            ApplicationState.ProjectLoaded.Preferences = l_ps;
            FindObjectOfType<ProjectLoaderSaver>().Save(m_LocationFolderSelector.Folder);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_NameInputField.text = ApplicationState.ProjectLoaded.Preferences.Name;
            m_LocationFolderSelector.Folder = ApplicationState.ProjectLoadedLocation;
        }
        #endregion
    }
}