using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class ProtocolPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_PositionAveragingDropdown;
        [SerializeField] Toggle m_1msStepToogle, m_5msStepToggle, m_10msStepToggle, m_50msStepToggle, m_100msStepToggle;
        [SerializeField] InputField m_MinPossibleValueInputField, m_MaxPossibleValueInputField;

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
        public void SetFields()
        {
            Data.Preferences.ProtocolPreferences preferences = ApplicationState.UserPreferences.Data.Protocol;

            m_MinPossibleValueInputField.text = preferences.MinLimit.ToString();
            m_MaxPossibleValueInputField.text = preferences.MaxLimit.ToString();
            m_MinPossibleValueInputField.onEndEdit.AddListener((value) =>
            {
                int min, max;
                if (int.TryParse(value, out min) && int.TryParse(m_MaxPossibleValueInputField.text, out max))
                {
                    m_MinPossibleValueInputField.text = Mathf.Clamp(min, int.MinValue, max).ToString();
                }
            });
            m_MaxPossibleValueInputField.onEndEdit.AddListener((value) =>
            {
                int min, max;
                if (int.TryParse(value, out max) && int.TryParse(m_MinPossibleValueInputField.text, out min))
                {
                    m_MaxPossibleValueInputField.text = Mathf.Clamp(max, min, int.MaxValue).ToString();
                }
            });

            m_1msStepToogle.isOn = preferences.Step == 1;
            m_5msStepToggle.isOn = preferences.Step == 5;
            m_10msStepToggle.isOn = preferences.Step == 10;
            m_50msStepToggle.isOn = preferences.Step == 50;
            m_100msStepToggle.isOn = preferences.Step == 100;

            m_PositionAveragingDropdown.Set(typeof(Data.Enums.AveragingType), (int)preferences.PositionAveraging);
        }
        public void Save()
        {
            Data.Preferences.ProtocolPreferences preferences = ApplicationState.UserPreferences.Data.Protocol;

            preferences.PositionAveraging = (Data.Enums.AveragingType) m_PositionAveragingDropdown.value;

            int minLimit, maxLimit;
            if(int.TryParse(m_MinPossibleValueInputField.text, out minLimit))
            {
                preferences.MinLimit = minLimit;
            }
            if(int.TryParse(m_MaxPossibleValueInputField.text, out maxLimit))
            {
                preferences.MaxLimit = maxLimit;
            }

            if (m_1msStepToogle.isOn) preferences.Step = 1;
            else if (m_5msStepToggle.isOn) preferences.Step = 5;
            else if (m_10msStepToggle.isOn) preferences.Step = 10;
            else if (m_50msStepToggle.isOn) preferences.Step = 50;
            else if (m_100msStepToggle.isOn) preferences.Step = 100;
        }
        #endregion
    }
}