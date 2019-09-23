using HBP.Data.Preferences;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.UserPreferences
{
    public class TrialMatrixPreferencesSubModifier : SubModifier<TrialMatrixPreferences>
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

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

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

        public override void Initialize()
        {
            base.Initialize();

            m_ShowWholeProtocolToggle.onValueChanged.AddListener(value => Object.ShowWholeProtocol = value);
            m_TrialSynchronizationToggle.onValueChanged.AddListener(value => Object.TrialsSynchronization = value);
            m_SmoothTrialToggle.onValueChanged.AddListener(value => Object.TrialSmoothing = value);
            m_NumberOfIntermediateValuesSlider.onValueChanged.AddListener(value => Object.NumberOfIntermediateValues = Mathf.RoundToInt(value));
            m_BlocFormatDropdown.onValueChanged.AddListener(OnChangeBlocFormat);
            m_TrialHeightSlider.onValueChanged.AddListener(value => Object.TrialHeight = Mathf.RoundToInt(value));
            m_TrialRatioSlider.onValueChanged.AddListener(value => Object.TrialRatio = value);
            m_BlocRatioSlider.onValueChanged.AddListener(value => Object.BlocRatio = value);
        }
        public void OnChangeBlocFormat(int value)
        {
            Object.SubBlocFormat = (Data.Enums.BlocFormatType)m_BlocFormatDropdown.value;
            switch (Object.SubBlocFormat)
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

        #region Protected Methods
        protected override void SetFields(TrialMatrixPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            // Show whole protocol.
            m_ShowWholeProtocolToggle.isOn = objectToDisplay.ShowWholeProtocol;

            // Trial synchronization.
            m_TrialSynchronizationToggle.isOn = objectToDisplay.TrialsSynchronization;

            // Trial smoothing.
            m_SmoothTrialToggle.isOn = objectToDisplay.TrialSmoothing;

            // Intermediate values.
            m_NumberOfIntermediateValuesSlider.interactable = objectToDisplay.TrialSmoothing;
            m_NumberOfIntermediateValuesSlider.value = objectToDisplay.NumberOfIntermediateValues;

            // Bloc format.
            m_BlocFormatDropdown.Set(typeof(Data.Enums.BlocFormatType), (int)objectToDisplay.SubBlocFormat);
            m_BlocFormatDropdown.onValueChanged.AddListener(OnChangeBlocFormat);
            OnChangeBlocFormat(m_BlocFormatDropdown.value);

            // Trial height.
            m_TrialHeightSlider.minValue = TrialMatrixPreferences.MINIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.maxValue = TrialMatrixPreferences.MAXIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.wholeNumbers = true;
            m_TrialHeightSlider.value = objectToDisplay.TrialHeight;

            // Trial ratio.
            m_TrialRatioSlider.minValue = TrialMatrixPreferences.MINIMUM_TRIAL_RATIO;
            m_TrialRatioSlider.maxValue = TrialMatrixPreferences.MAXIMUM_TRIAL_RATIO;
            m_TrialRatioSlider.wholeNumbers = false;
            m_TrialRatioSlider.value = objectToDisplay.TrialRatio;

            // Bloc ratio.
            m_BlocRatioSlider.minValue = TrialMatrixPreferences.MINIMUM_BLOC_RATIO;
            m_BlocRatioSlider.maxValue = TrialMatrixPreferences.MAXIMUM_BLOC_RATIO;
            m_BlocRatioSlider.wholeNumbers = false;
            m_BlocRatioSlider.value = objectToDisplay.BlocRatio;
        }

        #endregion
    }
}