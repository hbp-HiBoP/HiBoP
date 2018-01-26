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
		InputField nameInputField;
        FolderSelector projectFolderSelector;
        FolderSelector patientsDatabaseFolderSelector;
        FolderSelector localizerDatabaseFolderSelector;
        #endregion

        #region Public Methods
        public void CreateNewProject()
		{
            if (ApplicationState.ProjectLoaded != null)
            {
                if (ApplicationState.ProjectLoaded.Visualizations.Any(v => v.IsOpen))
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Opened visualizations", "Some visualizations of the currently loaded project are opened. Loading another project will close any opened visualization.\n\nWould you like to load another project ?", () =>
                    {
                        ApplicationState.Module3D.RemoveAllScenes();
                        CreateProject();
                    },
                    "Load project");
                }
                else
                {
                    CreateProject();
                }
            }
            else
            {
                CreateProject();
            }
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
		{
            nameInputField = transform.Find("Content").Find("General").Find("Name").GetComponentInChildren<InputField>();
            projectFolderSelector = transform.Find("Content").Find("General").Find("Location").GetComponentInChildren<FolderSelector>();
            patientsDatabaseFolderSelector = transform.Find("Content").Find("Database").Find("Patients").GetComponentInChildren<FolderSelector>();
            localizerDatabaseFolderSelector = transform.Find("Content").Find("Database").Find("Localizers").GetComponentInChildren<FolderSelector>();

            nameInputField.text = ApplicationState.GeneralSettings.DefaultProjectName;
            projectFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultProjectLocation;
            patientsDatabaseFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation;
            localizerDatabaseFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation;
        }
        protected void CreateProject()
        {
            Data.Settings.ProjectSettings l_settings = new Data.Settings.ProjectSettings(nameInputField.text, patientsDatabaseFolderSelector.Folder, localizerDatabaseFolderSelector.Folder);
            ApplicationState.ProjectLoaded = new Project(l_settings);
            ApplicationState.ProjectLoadedLocation = projectFolderSelector.Folder;
            FindObjectOfType<ProjectLoaderSaver>().SaveAndReload();
            Close();
        }
        #endregion
	}
}
