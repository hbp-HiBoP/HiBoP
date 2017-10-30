using UnityEngine.UI;
using Tools.Unity;
using HBP.Data.Settings;
using System;
using Tools.CSharp;

namespace HBP.UI.Settings
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
        Dropdown trialBaselineOption;
        Dropdown eventPositionAveragingOption;
        Dropdown valueAveragingOption;
        Dropdown plotNameAutoCorrectionOption;
        Dropdown trialMatrixSmoothingOption;
        Dropdown blocFormatOption;
        Dropdown m_ThemeSelector;
        #endregion

        #region Public Methods
        public void Save()
        {
            ApplicationState.GeneralSettings.DefaultProjectName = defaultNameProjectInputField.text;
            ApplicationState.GeneralSettings.DefaultProjectLocation = defaultLocationProjectFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation = defaultPatientDatabaseLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation = defaultLocalizerDatabaseLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType = (GeneralSettings.PlotNameCorrectionTypeEnum) plotNameAutoCorrectionOption.value;
            TrialMatrixSettings trialMatrixSettings = ApplicationState.GeneralSettings.TrialMatrixSettings;
            trialMatrixSettings.Smoothing = (TrialMatrixSettings.SmoothingType) trialMatrixSmoothingOption.value;
            trialMatrixSettings.Baseline = (TrialMatrixSettings.BaselineType) trialBaselineOption.value;
            trialMatrixSettings.BlocFormat = (TrialMatrixSettings.BlocFormatType) blocFormatOption.value;
            ApplicationState.GeneralSettings.TrialMatrixSettings = trialMatrixSettings;
            ApplicationState.GeneralSettings.TrialMatrixSettings.ConstantLineHeight = int.Parse(constantLineInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.LineHeightByWidth = float.Parse(LineRatioInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.HeightByWidth = float.Parse(BlocRatioInputField.text);
            ApplicationState.GeneralSettings.EventPositionAveraging = (GeneralSettings.AveragingMode) eventPositionAveragingOption.value;
            ApplicationState.GeneralSettings.ValueAveraging = (GeneralSettings.AveragingMode) valueAveragingOption.value;
            ApplicationState.GeneralSettings.ThemeName = m_ThemeSelector.options[m_ThemeSelector.value].text;
            ClassLoaderSaver.SaveToJSon(ApplicationState.GeneralSettings, GeneralSettings.PATH,true);
            Close();
        }
        public override void Close()
        {
            base.Close();
            Theme.Theme.UpdateThemeElements(ApplicationState.GeneralSettings.Theme);
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            defaultNameProjectInputField = transform.Find("Content").Find("Name").Find("InputField").GetComponent<InputField>();
            defaultLocationProjectFolderSelector = transform.Find("Content").Find("Location").Find("Projects").Find("FolderSelector").GetComponent<FolderSelector>();
            defaultPatientDatabaseLocationFolderSelector = transform.Find("Content").Find("Location").Find("Patients").Find("FolderSelector").GetComponent<FolderSelector>();
            defaultLocalizerDatabaseLocationFolderSelector = transform.Find("Content").Find("Location").Find("Localizers").Find("FolderSelector").GetComponentInChildren<FolderSelector>();
            plotNameAutoCorrectionOption = transform.Find("Content").Find("EEG").Find("PlotNameAutomaticCorrection").GetComponentInChildren<Dropdown>();
            trialBaselineOption = transform.Find("Content").Find("Trial Matrix").Find("Baseline").GetComponentInChildren<Dropdown>();
            trialMatrixSmoothingOption = transform.Find("Content").Find("Trial Matrix").Find("TrialMatrixSmoothing").GetComponentInChildren<Dropdown>();
            blocFormatOption = transform.Find("Content").Find("Trial Matrix").Find("BlocFormat").GetComponentInChildren<Dropdown>();
            constantLineInputField = transform.Find("Content").Find("Trial Matrix").Find("ConstantLine").GetComponentInChildren<InputField>(true);
            LineRatioInputField = transform.Find("Content").Find("Trial Matrix").Find("LineRatio").GetComponentInChildren<InputField>(true);
            BlocRatioInputField = transform.Find("Content").Find("Trial Matrix").Find("BlocRatio").GetComponentInChildren<InputField>(true);

            eventPositionAveragingOption = transform.Find("Content").Find("Averaging").Find("EventPositionAveraging").GetComponentInChildren<Dropdown>();
            valueAveragingOption = transform.Find("Content").Find("Averaging").Find("ValueAveraging").GetComponentInChildren<Dropdown>();
            m_ThemeSelector = transform.Find("Content").Find("Display").Find("Theme").GetComponentInChildren<Dropdown>();

            defaultNameProjectInputField.text = ApplicationState.GeneralSettings.DefaultProjectName;
            defaultLocationProjectFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultProjectLocation;
            defaultPatientDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation;
            defaultLocalizerDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation;

            string[] l_typesBaseline = Enum.GetNames(typeof(TrialMatrixSettings.BaselineType));
            trialBaselineOption.ClearOptions();
            foreach (string i_type in l_typesBaseline)
            {
                trialBaselineOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialBaselineOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.Baseline;
            trialBaselineOption.RefreshShownValue();

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

            string[] l_averaging = Enum.GetNames(typeof(GeneralSettings.AveragingMode));
            eventPositionAveragingOption.ClearOptions();
            valueAveragingOption.ClearOptions();
            foreach (string i_type in l_averaging)
            {
                eventPositionAveragingOption.options.Add(new Dropdown.OptionData(i_type));
                valueAveragingOption.options.Add(new Dropdown.OptionData(i_type));
            }
            eventPositionAveragingOption.value = (int)ApplicationState.GeneralSettings.EventPositionAveraging;
            eventPositionAveragingOption.RefreshShownValue();
            valueAveragingOption.value = (int)ApplicationState.GeneralSettings.ValueAveraging;
            valueAveragingOption.RefreshShownValue();

            m_ThemeSelector.ClearOptions();
            m_ThemeSelector.onValueChanged.AddListener((t) =>
            {
                Theme.Theme theme = ApplicationState.GeneralSettings.Themes[t];
                Theme.Theme.UpdateThemeElements(theme);
            });
            foreach (Theme.Theme theme in ApplicationState.GeneralSettings.Themes)
            {
                m_ThemeSelector.options.Add(new Dropdown.OptionData(theme.name));
            }
            int themeID = m_ThemeSelector.options.FindIndex((o) => o.text == ApplicationState.GeneralSettings.ThemeName);
            m_ThemeSelector.value = themeID != -1 ? themeID : 0;
            m_ThemeSelector.RefreshShownValue();
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