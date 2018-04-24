using UnityEngine;
using UnityEngine.UI;
using HBP.Data.General;
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
        protected override void SetWindow()
		{
            m_NameInputField.text = ApplicationState.UserPreferences.General.Project.DefaultName;
            m_ProjectLocationFolderSelector.Folder = ApplicationState.UserPreferences.General.Project.DefaultLocation;
            m_PatientsDatabaseLocationFolderSelector.Folder = ApplicationState.UserPreferences.General.Project.DefaultPatientDatabase;
            m_LocalizerDatabaseLocationFolderSelector.Folder = ApplicationState.UserPreferences.General.Project.DefaultLocalizerDatabase;
        }
        protected void CreateNewProject()
        {
            Data.Preferences.ProjectSettings settings = new Data.Preferences.ProjectSettings(m_NameInputField.text, m_PatientsDatabaseLocationFolderSelector.Folder, m_LocalizerDatabaseLocationFolderSelector.Folder);
            ApplicationState.ProjectLoaded = new Project(settings);
            ApplicationState.ProjectLoadedLocation = m_ProjectLocationFolderSelector.Folder;
            FindObjectOfType<ProjectLoaderSaver>().SaveAndReload();
            Close();
        }
        #endregion
	}
}
