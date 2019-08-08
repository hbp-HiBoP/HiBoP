using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class TresholdTreatmentSubModifier : SubModifier<Data.Experience.Protocol.TresholdTreatment>
    {
        #region Properties
        [SerializeField] Toggle m_UseMinTresholdToggle;
        [SerializeField] Toggle m_UseMaxTresholdToggle;
        [SerializeField] InputField m_MinValueInputField;
        [SerializeField] InputField m_MaxValueInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_UseMinTresholdToggle.interactable = value;
                m_UseMaxTresholdToggle.interactable = value;
                m_MinValueInputField.interactable = value && m_UseMinTresholdToggle.isOn;
                m_MaxValueInputField.interactable = value && m_UseMaxTresholdToggle.isOn;
            }
        }

        public override Data.Experience.Protocol.TresholdTreatment Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_UseMinTresholdToggle.isOn = value.UseMinTreshold;
                m_UseMaxTresholdToggle.isOn = value.UseMaxTreshold;
                m_MinValueInputField.text = value.Min.ToString();
                m_MaxValueInputField.text = value.Max.ToString();
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_UseMinTresholdToggle.onValueChanged.AddListener(OnChangeUseMinValue);
            m_UseMaxTresholdToggle.onValueChanged.AddListener(OnChangeUseMaxValue);
            m_MinValueInputField.onValueChanged.AddListener(OnChangeMinValue);
            m_MaxValueInputField.onValueChanged.AddListener(OnChangeMaxValue);
        }
        #endregion

        #region Private Methods
        void OnChangeMinValue(string value)
        {
            if (float.TryParse(value, out float result)) Object.Min = result;
        }
        void OnChangeMaxValue(string value)
        {
            if (float.TryParse(value, out float result)) Object.Max = result;
        }
        void OnChangeUseMinValue(bool value)
        {
            Object.UseMinTreshold = value;
            m_MinValueInputField.interactable = value;
        }
        void OnChangeUseMaxValue(bool value)
        {
            Object.UseMaxTreshold = value;
            m_MaxValueInputField.interactable = value;
        }
        #endregion
    }
}