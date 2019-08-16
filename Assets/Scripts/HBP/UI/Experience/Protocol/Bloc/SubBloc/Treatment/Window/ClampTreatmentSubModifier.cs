using HBP.Data.Experience.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class ClampTreatmentSubModifier : SubModifier<Data.Experience.Protocol.ClampTreatment>
    {
        #region Properties
        [SerializeField] Toggle m_UseMinClampToggle;
        [SerializeField] Toggle m_UseMaxClampToggle;
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
                m_UseMinClampToggle.interactable = value;
                m_UseMaxClampToggle.interactable = value;
                m_MinValueInputField.interactable = value && m_UseMinClampToggle.isOn;
                m_MaxValueInputField.interactable = value && m_UseMaxClampToggle.isOn;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_UseMinClampToggle.onValueChanged.AddListener(OnChangeUseMinValue);
            m_UseMaxClampToggle.onValueChanged.AddListener(OnChangeUseMaxValue);
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
            Object.UseMinClamp = value;
            m_MinValueInputField.interactable = Interactable && value;
        }
        void OnChangeUseMaxValue(bool value)
        {
            Object.UseMaxClamp = value;
            m_MaxValueInputField.interactable = Interactable && value;
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(ClampTreatment objectToDisplay)
        {
            m_UseMinClampToggle.isOn = objectToDisplay.UseMinClamp;
            m_UseMaxClampToggle.isOn = objectToDisplay.UseMaxClamp;
            m_MinValueInputField.text = objectToDisplay.Min.ToString();
            m_MaxValueInputField.text = objectToDisplay.Max.ToString();
        }
        #endregion
    }
}