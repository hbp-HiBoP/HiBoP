using HBP.Data.Experience.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class RescaleTreatmentSubModifier : SubModifier<Data.Experience.Protocol.RescaleTreatment>
    {
        #region Properties
        [SerializeField] InputField m_MinBeforeInputField;
        [SerializeField] InputField m_MaxBeforeInputField;

        [SerializeField] InputField m_MinAfterInputField;
        [SerializeField] InputField m_MaxAfterInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_MinBeforeInputField.interactable = value;
                m_MaxBeforeInputField.interactable = value;
                m_MinAfterInputField.interactable = value;
                m_MaxAfterInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_MinBeforeInputField.onValueChanged.AddListener(OnChangeMinBeforeValue);
            m_MaxBeforeInputField.onValueChanged.AddListener(OnChangeMaxBeforeValue);
            m_MinAfterInputField.onValueChanged.AddListener(OnChangeMinAfterValue);
            m_MaxAfterInputField.onValueChanged.AddListener(OnChangeMaxAfterValue);
        }
        #endregion

        #region Private Methods
        protected void OnChangeMinBeforeValue(string value)
        {
            if (float.TryParse(value, out float result)) Object.BeforeMin = result;
        }
        protected void OnChangeMaxBeforeValue(string value)
        {
            if (float.TryParse(value, out float result)) Object.BeforeMax = result;
        }
        protected void OnChangeMinAfterValue(string value)
        {
            if (float.TryParse(value, out float result)) Object.AfterMin = result;
        }
        protected void OnChangeMaxAfterValue(string value)
        {
            if (float.TryParse(value, out float result)) Object.AfterMax = result;
        }
        protected override void SetFields(RescaleTreatment objectToDisplay)
        {
            m_MinBeforeInputField.text = objectToDisplay.BeforeMin.ToString();
            m_MaxBeforeInputField.text = objectToDisplay.BeforeMax.ToString();

            m_MinAfterInputField.text = objectToDisplay.AfterMin.ToString();
            m_MaxAfterInputField.text = objectToDisplay.AfterMax.ToString();
        }
        #endregion
    }
}