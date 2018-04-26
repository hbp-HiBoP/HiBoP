using System;
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
        #endregion

        #region Public Methods
        public void Set()
        {
            Data.Preferences.TrialMatrixPreferences trialMatrixPreferences = ApplicationState.UserPreferences.Visualization.TrialMatrix;

            m_ShowWholeProtocolToggle.isOn = trialMatrixPreferences.ShowWholeProtocol;
            m_TrialSynchronizationToggle.isOn = trialMatrixPreferences.TrialsSynchronization;
            m_SmoothLineToggle.isOn = trialMatrixPreferences.SmoothLine;
            m_NumberOfIntermediateValuesSlider.value = trialMatrixPreferences.NumberOfIntermediateValues;

            string[] options = Enum.GetNames(typeof(Data.Preferences.TrialMatrixPreferences.BlocFormatType));
            m_BlocFormatDropdown.ClearOptions();
            foreach (string option in options)
            {
                m_BlocFormatDropdown.options.Add(new Dropdown.OptionData(option));
            }
            m_BlocFormatDropdown.value = (int) ApplicationState.UserPreferences.Visualization.TrialMatrix.BlocFormat;
            m_BlocFormatDropdown.RefreshShownValue();

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

        public void OnChangeBlocFormat(int value)
        {
            Data.Preferences.TrialMatrixPreferences.BlocFormatType blocFormat = (Data.Preferences.TrialMatrixPreferences.BlocFormatType) m_BlocFormatDropdown.value;
            switch (blocFormat)
            {
                case Data.Preferences.TrialMatrixPreferences.BlocFormatType.HeightLine:
                    m_LineHeightInputField.gameObject.SetActive(true);
                    m_LineRatioInputField.gameObject.SetActive(false);
                    m_BlocRatioInputField.gameObject.SetActive(false);
                    break;
                case Data.Preferences.TrialMatrixPreferences.BlocFormatType.LineRatio:
                    m_LineHeightInputField.gameObject.SetActive(false);
                    m_LineRatioInputField.gameObject.SetActive(true);
                    m_BlocRatioInputField.gameObject.SetActive(false);
                    break;
                case Data.Preferences.TrialMatrixPreferences.BlocFormatType.BlocRatio:
                    m_LineHeightInputField.gameObject.SetActive(false);
                    m_LineRatioInputField.gameObject.SetActive(false);
                    m_BlocRatioInputField.gameObject.SetActive(true);
                    break;
            }
        }
        #endregion
    }
}