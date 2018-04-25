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
        FolderSelector defaultScreenshotsLocationFolderSelector;
        Dropdown trialBaselineOption;
        Dropdown eventPositionAveragingOption;
        Dropdown valueAveragingOption;
        Dropdown plotNameAutoCorrectionOption;
        Dropdown trialMatrixSmoothingOption;
        Dropdown blocFormatOption;
        Dropdown trialsSynchronizationOption;
        Dropdown trialMatrixTypeOption;
        Dropdown m_AutoTriggerOption;
        Dropdown m_ThemeSelectorOption;
        Dropdown m_CutLinesOption;
        Dropdown m_HideCurveWhenColumnHiddenOption;
        Dropdown m_PreloadAnatomy;
        #endregion

        #region Public Methods
        public void Save()
        {
            ApplicationState.GeneralSettings.DefaultProjectName = defaultNameProjectInputField.text;
            ApplicationState.GeneralSettings.DefaultProjectLocation = defaultLocationProjectFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation = defaultPatientDatabaseLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation = defaultLocalizerDatabaseLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.DefaultScreenshotsLocation = defaultScreenshotsLocationFolderSelector.Folder;
            ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType = (GeneralSettings.PlotNameCorrectionTypeEnum) plotNameAutoCorrectionOption.value;
            TrialMatrixSettings trialMatrixSettings = ApplicationState.GeneralSettings.TrialMatrixSettings;
            trialMatrixSettings.Smoothing = (TrialMatrixSettings.SmoothingType) trialMatrixSmoothingOption.value;
            trialMatrixSettings.SetBaseline((TrialMatrixSettings.NormalizationType)trialBaselineOption.value);
            trialMatrixSettings.BlocFormat = (TrialMatrixSettings.BlocFormatType) blocFormatOption.value;
            trialMatrixSettings.TrialsSynchronization = (TrialMatrixSettings.TrialsSynchronizationType) trialsSynchronizationOption.value;
            trialMatrixSettings.Type = (TrialMatrixSettings.TrialMatrixType) trialMatrixTypeOption.value;
            ApplicationState.GeneralSettings.TrialMatrixSettings = trialMatrixSettings;
            ApplicationState.GeneralSettings.TrialMatrixSettings.ConstantLineHeight = int.Parse(constantLineInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.LineHeightByWidth = float.Parse(LineRatioInputField.text);
            ApplicationState.GeneralSettings.TrialMatrixSettings.HeightByWidth = float.Parse(BlocRatioInputField.text);
            ApplicationState.GeneralSettings.EventPositionAveraging = (GeneralSettings.AveragingMode) eventPositionAveragingOption.value;
            ApplicationState.GeneralSettings.ValueAveraging = (GeneralSettings.AveragingMode) valueAveragingOption.value;
            ApplicationState.GeneralSettings.ThemeName = m_ThemeSelectorOption.options[m_ThemeSelectorOption.value].text;
            ApplicationState.GeneralSettings.ShowCutLines = m_CutLinesOption.value == 0 ? true : false;
            ApplicationState.GeneralSettings.AutoTriggerIEEG = m_AutoTriggerOption.value == 0 ? true : false;
            ApplicationState.GeneralSettings.HideCurveWhenColumnHidden = m_HideCurveWhenColumnHiddenOption.value == 0 ? true : false;
            ApplicationState.GeneralSettings.PreloadAnatomy = m_PreloadAnatomy.value == 0 ? true : false;
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
            defaultScreenshotsLocationFolderSelector = transform.Find("Content").Find("Location").Find("Screenshots").Find("FolderSelector").GetComponentInChildren<FolderSelector>();

            plotNameAutoCorrectionOption = transform.Find("Content").Find("EEG").Find("PlotNameAutomaticCorrection").GetComponentInChildren<Dropdown>();
            m_AutoTriggerOption = transform.Find("Content").Find("EEG").Find("AutoTrigger").GetComponentInChildren<Dropdown>();

            trialBaselineOption = transform.Find("Content").Find("Trial Matrix").Find("Normalization").GetComponentInChildren<Dropdown>();
            trialMatrixSmoothingOption = transform.Find("Content").Find("Trial Matrix").Find("TrialMatrixSmoothing").GetComponentInChildren<Dropdown>();
            blocFormatOption = transform.Find("Content").Find("Trial Matrix").Find("BlocFormat").GetComponentInChildren<Dropdown>();
            constantLineInputField = transform.Find("Content").Find("Trial Matrix").Find("ConstantLine").GetComponentInChildren<InputField>(true);
            LineRatioInputField = transform.Find("Content").Find("Trial Matrix").Find("LineRatio").GetComponentInChildren<InputField>(true);
            BlocRatioInputField = transform.Find("Content").Find("Trial Matrix").Find("BlocRatio").GetComponentInChildren<InputField>(true);
            trialsSynchronizationOption = transform.Find("Content").Find("Trial Matrix").Find("TrialsSynchronization").GetComponentInChildren<Dropdown>();
            trialMatrixTypeOption = transform.Find("Content").Find("Trial Matrix").Find("Type").GetComponentInChildren<Dropdown>();

            eventPositionAveragingOption = transform.Find("Content").Find("Averaging").Find("EventPositionAveraging").GetComponentInChildren<Dropdown>();
            valueAveragingOption = transform.Find("Content").Find("Averaging").Find("ValueAveraging").GetComponentInChildren<Dropdown>();
            m_ThemeSelectorOption = transform.Find("Content").Find("Display").Find("Theme").GetComponentInChildren<Dropdown>();
            m_CutLinesOption = transform.Find("Content").Find("Display").Find("Cut lines").GetComponentInChildren<Dropdown>();
            m_HideCurveWhenColumnHiddenOption = transform.Find("Content").Find("Display").Find("Hide curve when column hidden").GetComponentInChildren<Dropdown>();
            m_PreloadAnatomy = transform.Find("Content").Find("Loading").Find("Preload").GetComponentInChildren<Dropdown>();

            defaultNameProjectInputField.text = ApplicationState.GeneralSettings.DefaultProjectName;
            defaultLocationProjectFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultProjectLocation;
            defaultPatientDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation;
            defaultLocalizerDatabaseLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation;
            defaultScreenshotsLocationFolderSelector.Folder = ApplicationState.GeneralSettings.DefaultScreenshotsLocation;

            string[] l_typesTrialMatrix = Enum.GetNames(typeof(TrialMatrixSettings.TrialMatrixType));
            trialMatrixTypeOption.ClearOptions();
            foreach (string i_type in l_typesTrialMatrix)
            {
                trialMatrixTypeOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialMatrixTypeOption.value = (int) ApplicationState.GeneralSettings.TrialMatrixSettings.Type;
            trialMatrixTypeOption.RefreshShownValue();

            string[] l_typesBaseline = Enum.GetNames(typeof(TrialMatrixSettings.NormalizationType));
            trialBaselineOption.ClearOptions();
            foreach (string i_type in l_typesBaseline)
            {
                trialBaselineOption.options.Add(new Dropdown.OptionData(i_type));
            }
            trialBaselineOption.value = (int)ApplicationState.GeneralSettings.TrialMatrixSettings.Normalization;
            trialBaselineOption.RefreshShownValue();

            string[] l_typesPlotName = Enum.GetNames(typeof(GeneralSettings.PlotNameCorrectionTypeEnum));
            plotNameAutoCorrectionOption.ClearOptions();
            foreach (string i_type in l_typesPlotName)
            {
                plotNameAutoCorrectionOption.options.Add(new Dropdown.OptionData(i_type));
            }
            plotNameAutoCorrectionOption.value = (int)ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType;
            plotNameAutoCorrectionOption.RefreshShownValue();
            
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

            m_PreloadAnatomy.ClearOptions();
            m_PreloadAnatomy.options.Add(new Dropdown.OptionData("Preload anatomy"));
            m_PreloadAnatomy.options.Add(new Dropdown.OptionData("Load only when required"));
            m_PreloadAnatomy.value = ApplicationState.GeneralSettings.PreloadAnatomy ? 0 : 1;
            m_PreloadAnatomy.RefreshShownValue();
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