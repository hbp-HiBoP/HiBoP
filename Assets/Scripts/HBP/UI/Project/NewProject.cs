using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;
using System.Linq;

namespace HBP.UI
{
    /// <summary>
    /// Manage the New Project window.
    /// </summary>
    public class NewProject : Window
	{
        #region Properties
		[SerializeField] InputField m_NameInputField;
        [SerializeField] FolderSelector m_ProjectLocationFolderSelector;
        [SerializeField] FolderSelector m_PatientsDatabaseLocationFolderSelector;
        [SerializeField] FolderSelector m_LocalizerDatabaseLocationFolderSelector;

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
                m_ProjectLocationFolderSelector.interactable = value;
                m_PatientsDatabaseLocationFolderSelector.interactable = value;
                m_LocalizerDatabaseLocationFolderSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void Create()
		{
            if (ApplicationState.ProjectLoaded != null)
            {
                if (ApplicationState.ProjectLoaded.Visualizations.Any(v => v.IsOpen))
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Opened visualizations", "Some visualizations of the currently loaded project are opened. Loading another project will close any opened visualization.\n\nWould you like to load another project ?", () =>
                    {
                        ApplicationState.Module3D.RemoveAllScenes();
                        CreateNewProject();
                    },
                    "Load project");
                }
                else
                {
                    CreateNewProject();
                }
            }
            else
            {
                CreateNewProject();
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
		{
            Data.Preferences.ProjectPreferences preferences = ApplicationState.UserPreferences.General.Project;

            m_NameInputField.text = preferences.DefaultName;
            m_ProjectLocationFolderSelector.Folder = preferences.DefaultLocation;
            m_PatientsDatabaseLocationFolderSelector.Folder = preferences.DefaultPatientDatabase;
            m_LocalizerDatabaseLocationFolderSelector.Folder = preferences.DefaultLocalizerDatabase;
        }
        void CreateNewProject()
        {
            Data.ProjectPreferences preferences = new Data.ProjectPreferences(m_NameInputField.text, m_PatientsDatabaseLocationFolderSelector.Folder, m_LocalizerDatabaseLocationFolderSelector.Folder);
            ApplicationState.ProjectLoaded = new Data.Project(preferences);
            ApplicationState.ProjectLoadedLocation = m_ProjectLocationFolderSelector.Folder;
            FindObjectOfType<ProjectLoaderSaver>().SaveAndReload();
            Close();
        }
        #endregion
    }
}
