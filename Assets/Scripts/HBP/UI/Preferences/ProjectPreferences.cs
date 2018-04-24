﻿using UnityEngine.UI;
using Tools.Unity;
using System;

namespace HBP.UI.Preferences
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
            ApplicationState.ProjectLoaded.Settings.PatientDatabase = patientsDatabaseFolderSelector.Folder;
            ApplicationState.ProjectLoaded.Settings.LocalizerDatabase = localizerDatabaseFolderSelector.Folder;
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            nameInputField = transform.Find("Content").Find("Name").Find("InputField").GetComponent<InputField>();
            patientsDatabaseFolderSelector = transform.Find("Content").Find("Location").Find("Patients").Find("FolderSelector").GetComponent<FolderSelector>();
            localizerDatabaseFolderSelector = transform.Find("Content").Find("Location").Find("Localizers").Find("FolderSelector").GetComponent<FolderSelector>();

            nameInputField.text = ApplicationState.ProjectLoaded.Settings.Name;
            patientsDatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;
            localizerDatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
        }
        #endregion
    }
}
