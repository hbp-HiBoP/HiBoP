using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Data.Enums;

namespace HBP.UI.UserPreferences
{
    public class ProtocolPreferencesSubModifier : SubModifier<Core.Data.Preferences.ProtocolPreferences>
    {
        #region Properties
        [SerializeField] Dropdown m_PositionAveragingDropdown;
        [SerializeField] Toggle m_1msStepToogle, m_5msStepToggle, m_10msStepToggle, m_50msStepToggle, m_100msStepToggle;
        [SerializeField] InputField m_MinPossibleValueInputField, m_MaxPossibleValueInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_PositionAveragingDropdown.interactable = value;

                m_MinPossibleValueInputField.interactable = value;
                m_MaxPossibleValueInputField.interactable = value;

                m_1msStepToogle.interactable = value;
                m_5msStepToggle.interactable = value;
                m_10msStepToggle.interactable = value;
                m_50msStepToggle.interactable = value;
                m_100msStepToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_MinPossibleValueInputField.onValueChanged.AddListener(OnChangeMinPossibleValue);
            m_MaxPossibleValueInputField.onValueChanged.AddListener(OnChangeMaxPossibleValue);

            m_1msStepToogle.onValueChanged.AddListener(value => { if (value) Object.Step = 1; });
            m_5msStepToggle.onValueChanged.AddListener(value => { if (value) Object.Step = 5; });
            m_10msStepToggle.onValueChanged.AddListener(value => { if (value) Object.Step = 10; });
            m_50msStepToggle.onValueChanged.AddListener(value => { if (value) Object.Step = 50; });
            m_100msStepToggle.onValueChanged.AddListener(value => { if (value) Object.Step = 100; });

            m_PositionAveragingDropdown.onValueChanged.AddListener(value => Object.PositionAveraging = (AveragingType)value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.Preferences.ProtocolPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_MinPossibleValueInputField.text = objectToDisplay.MinLimit.ToString();
            m_MaxPossibleValueInputField.text = objectToDisplay.MaxLimit.ToString();

            m_1msStepToogle.isOn = objectToDisplay.Step == 1;
            m_5msStepToggle.isOn = objectToDisplay.Step == 5;
            m_10msStepToggle.isOn = objectToDisplay.Step == 10;
            m_50msStepToggle.isOn = objectToDisplay.Step == 50;
            m_100msStepToggle.isOn = objectToDisplay.Step == 100;

            m_PositionAveragingDropdown.Set(typeof(AveragingType), (int)objectToDisplay.PositionAveraging);
        }
        protected void OnChangeMaxPossibleValue(string value)
        {
            if (int.TryParse(value, out int max))
            {
                Object.MaxLimit = Mathf.Clamp(max, Object.MinLimit, int.MaxValue);
                m_MaxPossibleValueInputField.text = Object.MaxLimit.ToString();
            }
        }
        protected void OnChangeMinPossibleValue(string value)
        {
            if (int.TryParse(value, out int min))
            {
                Object.MinLimit = Mathf.Clamp(min, int.MinValue, Object.MaxLimit);
                m_MinPossibleValueInputField.text = Object.MinLimit.ToString();
            }
        }
        #endregion
    }
}