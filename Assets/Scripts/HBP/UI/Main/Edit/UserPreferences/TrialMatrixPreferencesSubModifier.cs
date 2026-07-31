using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class TrialMatrixPreferencesSubModifier : SubModifier<TrialMatrixPreferences>
    {
        #region Properties

        [SerializeField] Toggle m_ShowWholeProtocolToggle;
        [SerializeField] Toggle m_TrialSynchronizationToggle;
        [SerializeField] Toggle m_SmoothTrialToggle;
        [SerializeField] Slider m_NumberOfIntermediateValuesSlider;
        [SerializeField] Toggle m_Smooth2DToggle;
        [SerializeField] Dropdown m_BlocFormatDropdown;
        [SerializeField] Slider m_TrialHeightSlider;
        [SerializeField] Slider m_TrialRatioSlider;
        [SerializeField] Slider m_BlocRatioSlider;
        [SerializeField] Slider m_ProtocolRatioSlider;
        [SerializeField] GameObject m_TrialHeightSubMenu;
        [SerializeField] GameObject m_TrialRatioSubMenu;
        [SerializeField] GameObject m_BlocRatioSubMenu;
        [SerializeField] GameObject m_ProtocolRatioSubMenu;

        public override bool Interactable
        {
            get { return base.Interactable; }
            set
            {
                base.Interactable = value;

                m_ShowWholeProtocolToggle.interactable = value;
                m_TrialSynchronizationToggle.interactable = value;
                m_SmoothTrialToggle.interactable = value;
                m_NumberOfIntermediateValuesSlider.interactable = m_SmoothTrialToggle.isOn && value;
                m_Smooth2DToggle.interactable = m_SmoothTrialToggle.isOn && value;
                m_BlocFormatDropdown.interactable = value;
                m_TrialHeightSlider.interactable = value;
                m_TrialRatioSlider.interactable = value;
                m_BlocRatioSlider.interactable = value;
                m_ProtocolRatioSlider.interactable = value;
            }
        }

        #endregion

        #region Protected Methods

        protected override void SetFields(TrialMatrixPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            // Show whole protocol.
            m_ShowWholeProtocolToggle.isOn = objectToDisplay.ShowWholeProtocol;
            m_ShowWholeProtocolToggle.onValueChanged.AddListener(value => Object.ShowWholeProtocol = value);

            // Trial synchronization.
            m_TrialSynchronizationToggle.isOn = objectToDisplay.TrialsSynchronization;
            m_TrialSynchronizationToggle.onValueChanged.AddListener(value => Object.TrialsSynchronization = value);

            // Trial smoothing.
            m_SmoothTrialToggle.isOn = objectToDisplay.TrialSmoothing;
            m_SmoothTrialToggle.onValueChanged.AddListener(OnChangeTrialSmoothing);
            OnChangeTrialSmoothing(m_SmoothTrialToggle.isOn);

            // Intermediate values.
            m_NumberOfIntermediateValuesSlider.value = objectToDisplay.NumberOfIntermediateValues;
            m_NumberOfIntermediateValuesSlider.onValueChanged.AddListener(value => Object.NumberOfIntermediateValues = Mathf.RoundToInt(value));

            // Smooth 2D.
            m_Smooth2DToggle.isOn = objectToDisplay.Smooth2D;
            m_Smooth2DToggle.onValueChanged.AddListener(value => Object.Smooth2D = value);

            // Bloc format.
            m_BlocFormatDropdown.Set(typeof(BlocFormatType), (int)objectToDisplay.SubBlocFormat);
            m_BlocFormatDropdown.onValueChanged.AddListener(OnChangeBlocFormat);
            OnChangeBlocFormat(m_BlocFormatDropdown.value);

            // Trial height.
            m_TrialHeightSlider.minValue = TrialMatrixPreferences.MINIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.maxValue = TrialMatrixPreferences.MAXIMUM_TRIAL_HEIGHT;
            m_TrialHeightSlider.wholeNumbers = true;
            m_TrialHeightSlider.value = objectToDisplay.TrialHeight;
            m_TrialHeightSlider.onValueChanged.AddListener(value => Object.TrialHeight = Mathf.RoundToInt(value));

            // Trial ratio. Multiply by 10 to avoid float display issues.
            m_TrialRatioSlider.minValue = TrialMatrixPreferences.MINIMUM_TRIAL_RATIO * 10;
            m_TrialRatioSlider.maxValue = TrialMatrixPreferences.MAXIMUM_TRIAL_RATIO * 10;
            m_TrialRatioSlider.wholeNumbers = false;
            m_TrialRatioSlider.value = objectToDisplay.TrialRatio * 10;
            m_TrialRatioSlider.onValueChanged.AddListener(value => Object.TrialRatio = value / 10);

            // Bloc ratio.
            m_BlocRatioSlider.minValue = TrialMatrixPreferences.MINIMUM_BLOC_RATIO;
            m_BlocRatioSlider.maxValue = TrialMatrixPreferences.MAXIMUM_BLOC_RATIO;
            m_BlocRatioSlider.wholeNumbers = false;
            m_BlocRatioSlider.value = objectToDisplay.BlocRatio;
            m_BlocRatioSlider.onValueChanged.AddListener(value => Object.BlocRatio = value);

            // Protocol ratio.
            m_ProtocolRatioSlider.minValue = TrialMatrixPreferences.MINIMUM_PROTOCOL_RATIO;
            m_ProtocolRatioSlider.maxValue = TrialMatrixPreferences.MAXIMUM_PROTOCOL_RATIO;
            m_ProtocolRatioSlider.wholeNumbers = false;
            m_ProtocolRatioSlider.value = objectToDisplay.ProtocolRatio;
            m_ProtocolRatioSlider.onValueChanged.AddListener(value => Object.ProtocolRatio = value);
        }

        protected void OnChangeTrialSmoothing(bool value)
        {
            Object.TrialSmoothing = value;
            m_NumberOfIntermediateValuesSlider.interactable = value;
            m_Smooth2DToggle.interactable = value;
        }

        protected void OnChangeBlocFormat(int value)
        {
            Object.SubBlocFormat = (BlocFormatType)m_BlocFormatDropdown.value;
            switch (Object.SubBlocFormat)
            {
                case BlocFormatType.TrialHeight:
                    m_TrialHeightSubMenu.SetActive(true);
                    m_TrialRatioSubMenu.SetActive(false);
                    m_BlocRatioSubMenu.SetActive(false);
                    m_ProtocolRatioSubMenu.SetActive(false);
                    break;
                case BlocFormatType.TrialRatio:
                    m_TrialHeightSubMenu.SetActive(false);
                    m_TrialRatioSubMenu.SetActive(true);
                    m_BlocRatioSubMenu.SetActive(false);
                    m_ProtocolRatioSubMenu.SetActive(false);
                    break;
                case BlocFormatType.BlocRatio:
                    m_TrialHeightSubMenu.SetActive(false);
                    m_TrialRatioSubMenu.SetActive(false);
                    m_BlocRatioSubMenu.SetActive(true);
                    m_ProtocolRatioSubMenu.SetActive(false);
                    break;
                case BlocFormatType.ProtocolRatio:
                    m_TrialHeightSubMenu.SetActive(false);
                    m_TrialRatioSubMenu.SetActive(false);
                    m_BlocRatioSubMenu.SetActive(false);
                    m_ProtocolRatioSubMenu.SetActive(true);
                    break;
            }
        }

        #endregion
    }
}
