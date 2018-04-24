﻿using UnityEngine;

namespace HBP.UI.Preferences
{
    public class ExportPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Tools.Unity.FolderSelector m_DefaultScreenshotsLocation;
        #endregion

        #region Public Methods
        public void Set()
        {
            Data.Preferences.ExportPreferences preferences = ApplicationState.UserPreferences.General.Export;
            m_DefaultScreenshotsLocation.Folder = preferences.DefaultScreenshotsLocation;
        }
        public void Save()
        {
            Data.Preferences.ExportPreferences preferences = ApplicationState.UserPreferences.General.Export;
            preferences.DefaultScreenshotsLocation = m_DefaultScreenshotsLocation.Folder;
        }
        #endregion
    }
}

