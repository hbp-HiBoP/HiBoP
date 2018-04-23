using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;
using HBP.Data.Settings;
using System;
using Tools.CSharp;

namespace HBP.UI.Settings
{
    public class UserPreferences : Window
    {
        #region Properties
        // General
        [SerializeField] InputField m_DefaultNameProjectInputField;
        [SerializeField] FolderSelector m_DefaultLocationProjectFolderSelector;
        [SerializeField] FolderSelector m_DefaultPatientDatabaseLocationFolderSelector;
        [SerializeField] FolderSelector m_DefaultLocalizerDatabaseLocationFolderSelector;

        [SerializeField] Toggle m_MultiThreadingToggle;

        [SerializeField] FolderSelector m_DefaultScreenshotsLocationFolderSelector;

        // Data
        [SerializeField] Dropdown m_EEGAveragingDropdown;
        [SerializeField] Dropdown m_EEGNormalizationDropdown;

        [SerializeField] Dropdown m_EventAveragingDropdown;

        [SerializeField] Toggle m_SiteNameCorrectionToggle;
        [SerializeField] Toggle m_PreloadMeshesToggle;
        [SerializeField] Toggle m_PreloadMRIsToggle;
        [SerializeField] Toggle m_PreloadImplantationsToggle;

        // Visualization
        InputField constantLineInputField;
        InputField LineRatioInputField;
        InputField BlocRatioInputField;

        Dropdown trialMatrixSmoothingOption;
        Dropdown blocFormatOption;
        Dropdown trialsSynchronizationOption;
        Dropdown trialMatrixTypeOption;
        Dropdown m_AutoTriggerOption;
        Dropdown m_ThemeSelectorOption;
        Dropdown m_CutLinesOption;
        Dropdown m_HideCurveWhenColumnHiddenOption;
        #endregion

        #region Public Methods
        public void Save()
        {
            SaveGeneral();

            TrialMatrixSettings trialMatrixSettings = ApplicationState.GeneralSettings.TrialMatrixSettings;
            trialMatrixSettings.Smoothing = (TrialMatrixSettings.SmoothingType) trialMatrixSmoothingOption.value;
            trialMatrixSettings.SetBaseline((TrialMatrixSettings.NormalizationType)m_EEGNormalizationDropdown.value);
            trialMatrixSettings.BlocFormat = (TrialMatrixSettings.BlocFormatType) blocFormatOption.value;
            trialMatrixSettings.TrialsSynchronization = (TrialMatrixSettings.TrialsSynchronizationType) trialsSynchronizationOption.value;
            trialMatrixSettings.Type = (TrialMatrixSettings.TrialMatrixType) trialMatrixTypeOption.value;
            ApplicationState.GeneralSettings.TrialMatrixSettings = trialMatrixSettings;
            ApplicationState.GeneralSettings.TrialMatrixSettings.ConstantLineHeight = int.Parse(constantLineInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.LineHeightByWidth = float.Parse(LineRatioInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.HeightByWidth = float.Parse(BlocRatioInputField.text);
            ApplicationState.GeneralSettings.EventPositionAveraging = (Data.Settings.UserPreferences.AveragingMode)m_EventAveragingDropdown.value;
            ApplicationState.GeneralSettings.ValueAveraging = (Data.Settings.UserPreferences.AveragingMode)m_EEGAveragingDropdown.value;
            ApplicationState.GeneralSettings.ThemeName = m_ThemeSelectorOption.options[m_ThemeSelectorOption.value].text;
            ApplicationState.GeneralSettings.ShowCutLines = m_CutLinesOption.value == 0 ? true : false;
            ApplicationState.GeneralSettings.AutoTriggerIEEG = m_AutoTriggerOption.value == 0 ? true : false;
            ApplicationState.GeneralSettings.HideCurveWhenColumnHidden = m_HideCurveWhenColumnHiddenOption.value == 0 ? true : false;
            ClassLoaderSaver.SaveToJSon(ApplicationState.GeneralSettings, Data.Settings.UserPreferences.PATH,true);
            Close();
        }
        public override void Close()
        {
            base.Close();
            Theme.Theme.UpdateThemeElements(ApplicationState.GeneralSettings.Theme);
        }
        #endregion

        #region Private Methods
        // General
        protected void SaveGeneral()
        {
            SaveProject();
            SaveTheme();
            SaveLocalization();
            SaveSystem();
            SaveExport();
        }
        protected void SaveProject()
        {
            ApplicationState.GeneralSettings.DefaultProjectName = m_DefaultNameProjectInputField.text;
            ApplicationState.GeneralSettings.DefaultProjectLocation = m_DefaultLocationProjectFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation = m_DefaultPatientDatabaseLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation = m_DefaultLocalizerDatabaseLocationFolderSelector.Folder;
        }
        protected void SaveTheme()
        {

        }
        protected void SaveLocalization()
        {

        }
        protected void SaveSystem()
        {

        }
        protected void SaveExport()
        {
            ApplicationState.GeneralSettings.DefaultScreenshotsLocation = m_DefaultScreenshotsLocationFolderSelector.Folder;
        }

        // Data
        protected void SaveData()
        {
            SaveEEG();
            SaveEvent();
            SaveAnatomy();
        }
        protected void SaveEEG()
        {
            ApplicationState.GeneralSettings.SiteNameCorrection = m_SiteNameCorrectionToggle.isOn;
        }
        protected void SaveEvent()
        {

        }
        protected void SaveAnatomy()
        {

        }
        protected override void SetWindow()
        {
            // Project
            m_DefaultNameProjectInputField.text = ApplicationState.GeneralSettings.DefaultProjectName;
            m_DefaultLocationProjectFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultProjectLocation;
            m_DefaultPatientDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation;
            m_DefaultLocalizerDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation;

            // Export
            m_DefaultScreenshotsLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultScreenshotsLocation;

            string[] l_typesTrialMatrix = Enum.GetNames(typeof(TrialMatrixSettings.TrialMatrixType));
            trialMatrixTypeOption.ClearOptions();
            foreach (string i_type in l_typesTrialMatrix)
            {
                trialMatrixTypeOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialMatrixTypeOption.value = (int) ApplicationState.GeneralSettings.TrialMatrixSettings.Type;
            trialMatrixTypeOption.RefreshShownValue();

            string[] l_typesBaseline = Enum.GetNames(typeof(TrialMatrixSettings.NormalizationType));
            m_EEGNormalizationDropdown.ClearOptions();
            foreach (string i_type in l_typesBaseline)
            {
                m_EEGNormalizationDropdown.options.Add(new Dropdown.OptionData(i_type));
            }
            m_EEGNormalizationDropdown.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.Normalization;
            m_EEGNormalizationDropdown.RefreshShownValue();

            string[] l_typesPlotName = Enum.GetNames(typeof(Data.Settings.UserPreferences.PlotNameCorrectionTypeEnum));
            m_SiteNameCorrectionToggle.ClearOptions();
            foreach (string i_type in l_typesPlotName)
            {
                m_SiteNameCorrectionToggle.options.Add(new Dropdown.OptionData(i_type));
            }
            m_SiteNameCorrectionToggle.value = (int)ApplicationState.GeneralSettings.SiteNameCorrection;
            m_SiteNameCorrectionToggle.RefreshShownValue();
            
            m_AutoTriggerOption.ClearOptions();
            m_AutoTriggerOption.options.Add(new Dropdown.OptionData("Yes"));
            m_AutoTriggerOption.options.Add(new Dropdown.OptionData("No"));
            m_AutoTriggerOption.value = ApplicationState.GeneralSettings.AutoTriggerIEEG ? 0 : 1;
            m_AutoTriggerOption.RefreshShownValue();

            string[] l_typesSmoothing = Enum.GetNames(typeof(TrialMatrixSettings.SmoothingType));
            trialMatrixSmoothingOption.ClearOptions();
            foreach (string i_type in l_typesSmoothing)
            {
                trialMatrixSmoothingOption.options.Add(new Dropdown.OptionData(i_type.SplitPascalCase()));
            }
            trialMatrixSmoothingOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.Smoothing;
            trialMatrixSmoothingOption.RefreshShownValue();

            string[] typesBlocFormat = Enum.GetNames(typeof(TrialMatrixSettings.BlocFormatType));
            blocFormatOption.ClearOptions();
            blocFormatOption.onValueChanged.AddListener((i) => UpdateBlocFormat((TrialMatrixSettings.BlocFormatType)i));
            foreach (string i_type in typesBlocFormat)
            {
                blocFormatOption.options.Add(new Dropdown.OptionData(i_type.SplitPascalCase()));
            }
            blocFormatOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.BlocFormat;
            blocFormatOption.RefreshShownValue();
            constantLineInputField.text = ApplicationState.GeneralSettings.TrialMatrixSettings.ConstantLineHeight.ToString();
            LineRatioInputField.text = ApplicationState.GeneralSettings.TrialMatrixSettings.LineHeightByWidth.ToString();
            BlocRatioInputField.text = ApplicationState.GeneralSettings.TrialMatrixSettings.HeightByWidth.ToString();


            string[] l_typesTrialsSynchronization = Enum.GetNames(typeof(TrialMatrixSettings.TrialsSynchronizationType));
            trialsSynchronizationOption.ClearOptions();
            foreach (string i_type in l_typesTrialsSynchronization)
            {
                trialsSynchronizationOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialsSynchronizationOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.TrialsSynchronization;
            trialsSynchronizationOption.RefreshShownValue();

            string[] l_averaging = Enum.GetNames(typeof(Data.Settings.UserPreferences.AveragingMode));
            m_EventAveragingDropdown.ClearOptions();
            m_EEGAveragingDropdown.ClearOptions();
            foreach (string i_type in l_averaging)
            {
                m_EventAveragingDropdown.options.Add(new Dropdown.OptionData(i_type));
                m_EEGAveragingDropdown.options.Add(new Dropdown.OptionData(i_type));
            }
            m_EventAveragingDropdown.value = (int)ApplicationState.GeneralSettings.EventPositionAveraging;
            m_EventAveragingDropdown.RefreshShownValue();
            m_EEGAveragingDropdown.value = (int)ApplicationState.GeneralSettings.ValueAveraging;
            m_EEGAveragingDropdown.RefreshShownValue();

            m_ThemeSelectorOption.ClearOptions();
            m_ThemeSelectorOption.onValueChanged.AddListener((t) =>
            {
                Theme.Theme theme = ApplicationState.GeneralSettings.Themes[t];
                Theme.Theme.UpdateThemeElements(theme);
            });
            foreach (Theme.Theme theme in ApplicationState.GeneralSettings.Themes)
            {
                m_ThemeSelectorOption.options.Add(new Dropdown.OptionData(theme.name));
            }
            int themeID = m_ThemeSelectorOption.options.FindIndex((o) => o.text == ApplicationState.GeneralSettings.ThemeName);
            m_ThemeSelectorOption.value = themeID != -1 ? themeID : 0;
            m_ThemeSelectorOption.RefreshShownValue();

            m_CutLinesOption.ClearOptions();
            m_CutLinesOption.options.Add(new Dropdown.OptionData("Show"));
            m_CutLinesOption.options.Add(new Dropdown.OptionData("Hide"));
            m_CutLinesOption.value = ApplicationState.GeneralSettings.ShowCutLines ? 0 : 1;
            m_CutLinesOption.RefreshShownValue();

            m_HideCurveWhenColumnHiddenOption.ClearOptions();
            m_HideCurveWhenColumnHiddenOption.options.Add(new Dropdown.OptionData("Hide"));
            m_HideCurveWhenColumnHiddenOption.options.Add(new Dropdown.OptionData("Show"));
            m_HideCurveWhenColumnHiddenOption.value = ApplicationState.GeneralSettings.HideCurveWhenColumnHidden ? 0 : 1;
            m_HideCurveWhenColumnHiddenOption.RefreshShownValue();
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