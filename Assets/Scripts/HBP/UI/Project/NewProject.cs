using UnityEngine;
using UnityEngine.UI;
using HBP.Data.General;
using Tools.Unity;

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
            Data.Settings.ProjectSettings l_settings = new Data.Settings.ProjectSettings(nameInputField.text, patientsDatabaseFolderSelector.Folder, localizerDatabaseFolderSelector.Folder);
            ApplicationState.ProjectLoaded = new Project(l_settings);
            ApplicationState.ProjectLoadedLocation = projectFolderSelector.Folder;
            FindObjectOfType<ProjectLoaderSaver>().SaveAndReload();
            Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
		{
            nameInputField = transform.FindChild("Content").FindChild("Name").GetComponentInChildren<InputField>();
            projectFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("Global").GetComponentInChildren<FolderSelector>();
            patientsDatabaseFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("Patients").GetComponentInChildren<FolderSelector>();
            localizerDatabaseFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("Localizer").GetComponentInChildren<FolderSelector>();

            nameInputField.text = ApplicationState.GeneralSettings.DefaultProjectName;
            projectFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultProjectLocation;
            patientsDatabaseFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation;
            localizerDatabaseFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation;
        }
        #endregion
	}
}
