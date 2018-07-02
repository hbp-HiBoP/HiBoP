using System;
using System.Text.RegularExpressions;
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
        [SerializeField] Toggle m_SmoothLineToggle;
        [SerializeField] Slider m_NumberOfIntermediateValuesSlider;
        [SerializeField] Dropdown m_BlocFormatDropdown;
        [SerializeField] InputField m_LineHeightInputField;
        [SerializeField] InputField m_LineRatioInputField;
        [SerializeField] InputField m_BlocRatioInputField;
        [SerializeField] GameObject m_LineHeightSubMenu;
        [SerializeField] GameObject m_LineRatioSubMenu;
        [SerializeField] GameObject m_BlocRatioSubMenu;

        #endregion

        #region Public Methods
        public void Initialize()
        {
            Data.Preferences.TrialMatrixPreferences trialMatrixPreferences = ApplicationState.UserPreferences.Visualization.TrialMatrix;

            m_ShowWholeProtocolToggle.isOn = trialMatrixPreferences.ShowWholeProtocol;
            m_TrialSynchronizationToggle.isOn = trialMatrixPreferences.TrialsSynchronization;
            m_SmoothLineToggle.isOn = trialMatrixPreferences.SmoothLine;
            m_NumberOfIntermediateValuesSlider.value = trialMatrixPreferences.NumberOfIntermediateValues;

            string[] options = Enum.GetNames(typeof(Data.Preferences.TrialMatrixPreferences.BlocFormatType));
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

            m_LineHeightInputField.text = trialMatrixPreferences.LineHeight.ToString();
            m_LineRatioInputField.text = trialMatrixPreferences.LineRatio.ToString();
            m_BlocRatioInputField.text = trialMatrixPreferences.BlocRatio.ToString();
        }
        public void Save()
        {
            Data.Preferences.TrialMatrixPreferences trialMatrixPreferences = ApplicationState.UserPreferences.Visualization.TrialMatrix;

            trialMatrixPreferences.ShowWholeProtocol = m_ShowWholeProtocolToggle.isOn;
            trialMatrixPreferences.TrialsSynchronization = m_TrialSynchronizationToggle.isOn;
            trialMatrixPreferences.SmoothLine = m_SmoothLineToggle.isOn;
            trialMatrixPreferences.NumberOfIntermediateValues = (int) m_NumberOfIntermediateValuesSlider.value;
            trialMatrixPreferences.BlocFormat = (Data.Preferences.TrialMatrixPreferences.BlocFormatType) m_BlocFormatDropdown.value;
            trialMatrixPreferences.LineHeight = int.Parse(m_LineHeightInputField.text);
            trialMatrixPreferences.LineRatio = int.Parse(m_LineRatioInputField.text);
            trialMatrixPreferences.BlocRatio = int.Parse(m_BlocRatioInputField.text);
        }
        public void SetInteractable(bool interactable)
        {
            m_ShowWholeProtocolToggle.interactable = interactable;
            m_TrialSynchronizationToggle.interactable = interactable;
            m_SmoothLineToggle.interactable = interactable;
            m_NumberOfIntermediateValuesSlider.interactable = interactable;
            m_BlocFormatDropdown.interactable = interactable;
            m_LineHeightInputField.interactable = interactable;
            m_LineRatioInputField.interactable = interactable;
            m_BlocRatioInputField.interactable = interactable;
        }
        public void OnChangeBlocFormat(int value)
        {
            Data.Preferences.TrialMatrixPreferences.BlocFormatType blocFormat = (Data.Preferences.TrialMatrixPreferences.BlocFormatType) m_BlocFormatDropdown.value;
            switch (blocFormat)
            {
                case Data.Preferences.TrialMatrixPreferences.BlocFormatType.LineHeight:
                    m_LineHeightSubMenu.SetActive(true);
                    m_LineRatioSubMenu.SetActive(false);
                    m_BlocRatioSubMenu.SetActive(false);
                    break;
                case Data.Preferences.TrialMatrixPreferences.BlocFormatType.LineRatio:
                    m_LineHeightSubMenu.SetActive(false);
                    m_LineRatioSubMenu.SetActive(true);
                    m_BlocRatioSubMenu.SetActive(false);
                    break;
                case Data.Preferences.TrialMatrixPreferences.BlocFormatType.BlocRatio:
                    m_LineHeightSubMenu.SetActive(false);
                    m_LineRatioSubMenu.SetActive(false);
                    m_BlocRatioSubMenu.SetActive(true);
                    break;
            }
        }
        #endregion
    }
}