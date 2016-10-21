using UnityEngine.UI;
using Tools.Unity;
using HBP.Data.Settings;
using System;

namespace HBP.UI
{
    public class Preferences : Window
    {
        #region Properties
        InputField defaultNameProjectInputField;
        FolderSelector defaultLocationProjectFolderSelector;
        FolderSelector defaultPatientDatabaseLocationFolderSelector;
        FolderSelector defaultLocalizerDatabaseLocationFolderSelector;
        Dropdown trialBaseLineOption;
        Dropdown plotNameAutoCorrectionOption;
        Dropdown trialMatrixSmoothingOption;
        #endregion

        #region Public Methods
        public void Save()
        {
            ApplicationState.GeneralSettings.DefaultProjectName = defaultNameProjectInputField.text;
            ApplicationState.GeneralSettings.DefaultProjectLocation = defaultLocationProjectFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation = defaultPatientDatabaseLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation = defaultLocalizerDatabaseLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType = (GeneralSettings.PlotNameCorrectionTypeEnum) plotNameAutoCorrectionOption.value;
            ApplicationState.GeneralSettings.TrialMatrixSmoothingType = (GeneralSettings.TrialMatrixSmoothingEnum) trialMatrixSmoothingOption.value;
            ApplicationState.GeneralSettings.BaseLineType = (GeneralSettings.BaseLineTypeEnum) trialBaseLineOption.value;
            ApplicationState.GeneralSettings.SaveJSon();
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            defaultNameProjectInputField = transform.FindChild("Content").FindChild("Name").FindChild("InputField").GetComponent<InputField>();
            defaultLocationProjectFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("Projects").FindChild("FolderSelector").GetComponent<FolderSelector>();
            defaultPatientDatabaseLocationFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("Patients").FindChild("FolderSelector").GetComponent<FolderSelector>();
            defaultLocalizerDatabaseLocationFolderSelector = transform.FindChild("Content").FindChild("Location").FindChild("SEEG").FindChild("FolderSelector").GetComponent<FolderSelector>();
            plotNameAutoCorrectionOption = transform.FindChild("Content").FindChild("EEG").FindChild("PlotNameAutomaticCorrection").GetComponentInChildren<Dropdown>();
            trialBaseLineOption = transform.FindChild("Content").FindChild("Trial Matrix").FindChild("BaseLine").GetComponentInChildren<Dropdown>();
            trialMatrixSmoothingOption = transform.FindChild("Content").FindChild("Trial Matrix").FindChild("TrialMatrixSmoothing").GetComponentInChildren<Dropdown>();

            defaultNameProjectInputField.text = ApplicationState.GeneralSettings.DefaultProjectName;
            defaultLocationProjectFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultProjectLocation;
            defaultPatientDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation;
            defaultLocalizerDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation;

            string[] l_typesBaseLine = Enum.GetNames(typeof(GeneralSettings.BaseLineTypeEnum));
            trialBaseLineOption.ClearOptions();
            foreach (string i_type in l_typesBaseLine)
            {
                trialBaseLineOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialBaseLineOption.value = (int)ApplicationState.GeneralSettings.BaseLineType;
            trialBaseLineOption.RefreshShownValue();

            string[] l_typesPlotName = Enum.GetNames(typeof(GeneralSettings.PlotNameCorrectionTypeEnum));
            plotNameAutoCorrectionOption.ClearOptions();
            foreach (string i_type in l_typesPlotName)
            {
                plotNameAutoCorrectionOption.options.Add(new Dropdown.OptionData(i_type));
            }
            plotNameAutoCorrectionOption.value = (int)ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType;
            plotNameAutoCorrectionOption.RefreshShownValue();

            string[] l_typesSmoothing = Enum.GetNames(typeof(GeneralSettings.TrialMatrixSmoothingEnum));
            trialMatrixSmoothingOption.ClearOptions();
            foreach (string i_type in l_typesSmoothing)
            {
                trialMatrixSmoothingOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialMatrixSmoothingOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSmoothingType;
            trialMatrixSmoothingOption.RefreshShownValue();
        }
        #endregion
    }
}