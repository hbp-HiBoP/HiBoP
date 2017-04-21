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
        InputField constantLineInputField;
        InputField LineRatioInputField;
        InputField BlocRatioInputField;
        FolderSelector defaultLocationProjectFolderSelector;
        FolderSelector defaultPatientDatabaseLocationFolderSelector;
        FolderSelector defaultLocalizerDatabaseLocationFolderSelector;
        Dropdown trialBaseLineOption;
        Dropdown plotNameAutoCorrectionOption;
        Dropdown trialMatrixSmoothingOption;
        Dropdown blocFormatOption;
        #endregion

        #region Public Methods
        public void Save()
        {
            ApplicationState.GeneralSettings.DefaultProjectName = defaultNameProjectInputField.text;
            ApplicationState.GeneralSettings.DefaultProjectLocation = defaultLocationProjectFolderSelector.Path;
            ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation = defaultPatientDatabaseLocationFolderSelector.Path;
            ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation = defaultLocalizerDatabaseLocationFolderSelector.Path;
            ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType = (GeneralSettings.PlotNameCorrectionTypeEnum) plotNameAutoCorrectionOption.value;
            TrialMatrixSettings trialMatrixSettings = ApplicationState.GeneralSettings.TrialMatrixSettings;
            trialMatrixSettings.Smoothing = (TrialMatrixSettings.SmoothingType) trialMatrixSmoothingOption.value;
            trialMatrixSettings.Baseline = (TrialMatrixSettings.BaselineType) trialBaseLineOption.value;
            trialMatrixSettings.BlocFormat = (TrialMatrixSettings.BlocFormatType) blocFormatOption.value;
            ApplicationState.GeneralSettings.TrialMatrixSettings = trialMatrixSettings;
            ApplicationState.GeneralSettings.TrialMatrixSettings.ConstantLineHeight = int.Parse(constantLineInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.LineHeightByWidth = float.Parse(LineRatioInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.HeightByWidth = float.Parse(BlocRatioInputField.text);

            ClassLoaderSaver.SaveToJSon(ApplicationState.GeneralSettings, GeneralSettings.PATH,true);
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
            blocFormatOption = transform.FindChild("Content").FindChild("Trial Matrix").FindChild("BlocFormat").GetComponentInChildren<Dropdown>();
            constantLineInputField = transform.FindChild("Content").FindChild("Trial Matrix").FindChild("ConstantLine").GetComponentInChildren<InputField>(true);
            LineRatioInputField = transform.FindChild("Content").FindChild("Trial Matrix").FindChild("LineRatio").GetComponentInChildren<InputField>(true);
            BlocRatioInputField = transform.FindChild("Content").FindChild("Trial Matrix").FindChild("BlocRatio").GetComponentInChildren<InputField>(true);

            defaultNameProjectInputField.text = ApplicationState.GeneralSettings.DefaultProjectName;
            defaultLocationProjectFolderSelector.Path = ApplicationState.GeneralSettings.DefaultProjectLocation;
            defaultPatientDatabaseLocationFolderSelector.Path = ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation;
            defaultLocalizerDatabaseLocationFolderSelector.Path = ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation;

            string[] l_typesBaseLine = Enum.GetNames(typeof(TrialMatrixSettings.BaselineType));
            trialBaseLineOption.ClearOptions();
            foreach (string i_type in l_typesBaseLine)
            {
                trialBaseLineOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialBaseLineOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.Baseline;
            trialBaseLineOption.RefreshShownValue();

            string[] l_typesPlotName = Enum.GetNames(typeof(GeneralSettings.PlotNameCorrectionTypeEnum));
            plotNameAutoCorrectionOption.ClearOptions();
            foreach (string i_type in l_typesPlotName)
            {
                plotNameAutoCorrectionOption.options.Add(new Dropdown.OptionData(i_type));
            }
            plotNameAutoCorrectionOption.value = (int)ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType;
            plotNameAutoCorrectionOption.RefreshShownValue();

            string[] l_typesSmoothing = Enum.GetNames(typeof(TrialMatrixSettings.SmoothingType));
            trialMatrixSmoothingOption.ClearOptions();
            foreach (string i_type in l_typesSmoothing)
            {
                trialMatrixSmoothingOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialMatrixSmoothingOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.Smoothing;
            trialMatrixSmoothingOption.RefreshShownValue();

            string[] typesBlocFormat = Enum.GetNames(typeof(TrialMatrixSettings.BlocFormatType));
            blocFormatOption.ClearOptions();
            blocFormatOption.onValueChanged.AddListener((i) => UpdateBlocFormat((TrialMatrixSettings.BlocFormatType)i));
            foreach (string i_type in typesBlocFormat)
            {
                blocFormatOption.options.Add(new Dropdown.OptionData(i_type));
            }
            blocFormatOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.BlocFormat;
            blocFormatOption.RefreshShownValue();
            constantLineInputField.text = ApplicationState.GeneralSettings.TrialMatrixSettings.ConstantLineHeight.ToString();
            LineRatioInputField.text = ApplicationState.GeneralSettings.TrialMatrixSettings.LineHeightByWidth.ToString();
            BlocRatioInputField.text = ApplicationState.GeneralSettings.TrialMatrixSettings.HeightByWidth.ToString();
        }

        void UpdateBlocFormat(TrialMatrixSettings.BlocFormatType blocFormat)
        {
            switch(blocFormat)
            {
                case TrialMatrixSettings.BlocFormatType.ConstantLine:
                    constantLineInputField.transform.parent.gameObject.SetActive(true);
                    LineRatioInputField.transform.parent.gameObject.SetActive(false);
                    BlocRatioInputField.transform.parent.gameObject.SetActive(false);
                    break;
                case TrialMatrixSettings.BlocFormatType.LineRatio:
                    constantLineInputField.transform.parent.gameObject.SetActive(false);
                    LineRatioInputField.transform.parent.gameObject.SetActive(true);
                    BlocRatioInputField.transform.parent.gameObject.SetActive(false);
                    break;
                case TrialMatrixSettings.BlocFormatType.BlocRatio:
                    constantLineInputField.transform.parent.gameObject.SetActive(false);
                    LineRatioInputField.transform.parent.gameObject.SetActive(false);
                    BlocRatioInputField.transform.parent.gameObject.SetActive(true);
                    break;
            }
        }
        #endregion
    }
}