using System;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class TrialMatrixPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Toggle m_ShowWholeProtocolToggle;
        [SerializeField] Toggle m_TrialSynchronizationToggle;
        [SerializeField] Toggle m_SmoothTrialToggle;
        [SerializeField] Slider m_NumberOfIntermediateValuesSlider;
        [SerializeField] Dropdown m_BlocFormatDropdown;
        [SerializeField] Slider m_TrialHeightSlider;
        [SerializeField] Slider m_TrialRatioSlider;
        [SerializeField] Slider m_BlocRatioSlider;
        [SerializeField] GameObject m_TrialHeightSubMenu;
        [SerializeField] GameObject m_TrialRatioSubMenu;
        [SerializeField] GameObject m_BlocRatioSubMenu;

        #endregion

        #region Public Methods
        public void Initialize()
        {
            Data.Preferences.TrialMatrixPreferences trialMatrixPreferences = ApplicationState.UserPreferences.Visualization.TrialMatrix;

            m_ShowWholeProtocolToggle.isOn = trialMatrixPreferences.ShowWholeProtocol;
            m_TrialSynchronizationToggle.isOn = trialMatrixPreferences.TrialsSynchronization;
            m_SmoothTrialToggle.isOn = trialMatrixPreferences.TrialSmoothing;
            m_NumberOfIntermediateValuesSlider.value = trialMatrixPreferences.NumberOfIntermediateValues;

            string[] options = Enum.GetNames(typeof(Data.Enums.BlocFormatType));
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = StringExtension.CamelCaseToWords(options[i]);
            }
            m_BlocFormatDropdown.ClearOptions();
            foreach (string option in options)
            {
                m_BlocFormatDropdown.options.Add(new Dropdown.OptionData(option));
            }
            m_BlocFormatDropdown.value = (int) ApplicationState.UserPreferences.Visualization.TrialMatrix.BlocFormat;
            m_BlocFormatDropdown.RefreshShownValue();
            OnChangeBlocFormat(m_BlocFormatDropdown.value);

            // Trial height.
            m_TrialHeightSlider.minValue = Data.Preferences.TrialMatrixPreferences.MINIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.maxValue = Data.Preferences.TrialMatrixPreferences.MAXIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.wholeNumbers = true;
            m_TrialHeightSlider.value = trialMatrixPreferences.TrialHeight;

            // Trial ratio.
            m_TrialRatioSlider.minValue = Data.Preferences.TrialMatrixPreferences.MINIMUM_TRIAL_RATIO;
            m_TrialRatioSlider.maxValue = Data.Preferences.TrialMatrixPreferences.MAXIMUM_TRIAL_RATIO;
            m_TrialRatioSlider.wholeNumbers = false;
            m_TrialRatioSlider.value = trialMatrixPreferences.TrialRatio;

            // Bloc ratio.
            m_BlocRatioSlider.minValue = Data.Preferences.TrialMatrixPreferences.MINIMUM_BLOC_RATIO;
            m_BlocRatioSlider.maxValue = Data.Preferences.TrialMatrixPreferences.MAXIMUM_BLOC_RATIO;
            m_BlocRatioSlider.wholeNumbers = false;
            m_BlocRatioSlider.value = trialMatrixPreferences.BlocRatio;
        }
        public void Save()
        {
            Data.Preferences.TrialMatrixPreferences trialMatrixPreferences = ApplicationState.UserPreferences.Visualization.TrialMatrix;

            trialMatrixPreferences.ShowWholeProtocol = m_ShowWholeProtocolToggle.isOn;
            trialMatrixPreferences.TrialsSynchronization = m_TrialSynchronizationToggle.isOn;
            trialMatrixPreferences.TrialSmoothing = m_SmoothTrialToggle.isOn;
            trialMatrixPreferences.NumberOfIntermediateValues = (int) m_NumberOfIntermediateValuesSlider.value;
            trialMatrixPreferences.BlocFormat = (Data.Enums.BlocFormatType) m_BlocFormatDropdown.value;
            trialMatrixPreferences.TrialHeight = (int) m_TrialHeightSlider.value;
            trialMatrixPreferences.TrialRatio = m_TrialRatioSlider.value;
            trialMatrixPreferences.BlocRatio = m_BlocRatioSlider.value;
        }
        public void SetInteractable(bool interactable)
        {
            m_ShowWholeProtocolToggle.interactable = interactable;
            m_TrialSynchronizationToggle.interactable = interactable;
            m_SmoothTrialToggle.interactable = interactable;
            m_NumberOfIntermediateValuesSlider.interactable = interactable;
            m_BlocFormatDropdown.interactable = interactable;
            m_TrialHeightSlider.interactable = interactable;
            m_TrialRatioSlider.interactable = interactable;
            m_BlocRatioSlider.interactable = interactable;
        }
        public void OnChangeBlocFormat(int value)
        {
            Data.Enums.BlocFormatType blocFormat = (Data.Enums.BlocFormatType) m_BlocFormatDropdown.value;
            switch (blocFormat)
            {
                case Data.Enums.BlocFormatType.TrialHeight:
                    m_TrialHeightSubMenu.SetActive(true);
                    m_TrialRatioSubMenu.SetActive(false);
                    m_BlocRatioSubMenu.SetActive(false);
                    break;
                case Data.Enums.BlocFormatType.TrialRatio:
                    m_TrialHeightSubMenu.SetActive(false);
                    m_TrialRatioSubMenu.SetActive(true);
                    m_BlocRatioSubMenu.SetActive(false);
                    break;
                case Data.Enums.BlocFormatType.BlocRatio:
                    m_TrialHeightSubMenu.SetActive(false);
                    m_TrialRatioSubMenu.SetActive(false);
                    m_BlocRatioSubMenu.SetActive(true);
                    break;
            }
        }
        #endregion
    }
}