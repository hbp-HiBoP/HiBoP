using Tools.Unity;
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

        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;

                m_ShowWholeProtocolToggle.interactable = value;
                m_TrialSynchronizationToggle.interactable = value;
                m_SmoothTrialToggle.interactable = value;
                m_NumberOfIntermediateValuesSlider.interactable = m_SmoothTrialToggle.isOn && value;
                m_BlocFormatDropdown.interactable = value;
                m_TrialHeightSlider.interactable = value;
                m_TrialRatioSlider.interactable = value;
                m_BlocRatioSlider.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetFields()
        {
            Data.Preferences.TrialMatrixPreferences preferences = ApplicationState.UserPreferences.Visualization.TrialMatrix;

            // Show whole protocol.
            m_ShowWholeProtocolToggle.isOn = preferences.ShowWholeProtocol;

            // Trial synchronization.
            m_TrialSynchronizationToggle.isOn = preferences.TrialsSynchronization;

            // Trial smoothing.
            m_SmoothTrialToggle.isOn = preferences.TrialSmoothing;

            // Intermediate values.
            m_NumberOfIntermediateValuesSlider.interactable = preferences.TrialSmoothing;
            m_NumberOfIntermediateValuesSlider.value = preferences.NumberOfIntermediateValues;

            // Bloc format.
            m_BlocFormatDropdown.Set(typeof(Data.Enums.BlocFormatType), (int) preferences.SubBlocFormat);
            m_BlocFormatDropdown.onValueChanged.AddListener(OnChangeBlocFormat);
            OnChangeBlocFormat(m_BlocFormatDropdown.value);

            // Trial height.
            m_TrialHeightSlider.minValue = Data.Preferences.TrialMatrixPreferences.MINIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.maxValue = Data.Preferences.TrialMatrixPreferences.MAXIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.wholeNumbers = true;
            m_TrialHeightSlider.value = preferences.TrialHeight;

            // Trial ratio.
            m_TrialRatioSlider.minValue = Data.Preferences.TrialMatrixPreferences.MINIMUM_TRIAL_RATIO;
            m_TrialRatioSlider.maxValue = Data.Preferences.TrialMatrixPreferences.MAXIMUM_TRIAL_RATIO;
            m_TrialRatioSlider.wholeNumbers = false;
            m_TrialRatioSlider.value = preferences.TrialRatio;

            // Bloc ratio.
            m_BlocRatioSlider.minValue = Data.Preferences.TrialMatrixPreferences.MINIMUM_BLOC_RATIO;
            m_BlocRatioSlider.maxValue = Data.Preferences.TrialMatrixPreferences.MAXIMUM_BLOC_RATIO;
            m_BlocRatioSlider.wholeNumbers = false;
            m_BlocRatioSlider.value = preferences.SubBlocRatio;
        }
        public void Save()
        {
            Data.Preferences.TrialMatrixPreferences preferences = ApplicationState.UserPreferences.Visualization.TrialMatrix;

            preferences.ShowWholeProtocol = m_ShowWholeProtocolToggle.isOn;
            preferences.TrialsSynchronization = m_TrialSynchronizationToggle.isOn;
            preferences.TrialSmoothing = m_SmoothTrialToggle.isOn;
            preferences.NumberOfIntermediateValues = (int) m_NumberOfIntermediateValuesSlider.value;
            preferences.SubBlocFormat = (Data.Enums.BlocFormatType) m_BlocFormatDropdown.value;
            preferences.TrialHeight = (int) m_TrialHeightSlider.value;
            preferences.TrialRatio = m_TrialRatioSlider.value;
            preferences.SubBlocRatio = m_BlocRatioSlider.value;
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