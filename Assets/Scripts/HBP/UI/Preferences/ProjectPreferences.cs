using UnityEngine.UI;
using Tools.Unity;
using System;

namespace HBP.UI
{
    public class ProjectPreferences : Window
    {
        #region Properties
        InputField nameInputField;
        FolderSelector patientsDatabaseFolderSelector;
        FolderSelector localizerDatabaseFolderSelector;
        #endregion

        #region Public Methods
        public void Save()
        {
            ApplicationState.ProjectLoaded.Settings.Name = nameInputField.text;
            ApplicationState.ProjectLoaded.Settings.PatientDatabase = patientsDatabaseFolderSelector.Path;
            ApplicationState.ProjectLoaded.Settings.LocalizerDatabase = localizerDatabaseFolderSelector.Path;
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            nameInputField = transform.FindChild("Content").FindChild("Name").GetComponentInChildren<InputField>();
            patientsDatabaseFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("Patients").FindChild("FolderSelector").GetComponent<FolderSelector>();
            localizerDatabaseFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("SEEG").FindChild("FolderSelector").GetComponent<FolderSelector>();

            nameInputField.text = ApplicationState.ProjectLoaded.Settings.Name;
            patientsDatabaseFolderSelector.Path = ApplicationState.ProjectLoaded.Settings.PatientDatabase;
            localizerDatabaseFolderSelector.Path = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
        }
        #endregion
    }
}
