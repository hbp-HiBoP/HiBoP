using HBP.Data.Experience.Protocol;
using System.Globalization;
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
        public void OnChangeMinBeforeValue(float value)
        {
            Object.BeforeMin = value;
        }
        public void OnChangeMaxBeforeValue(float value)
        {
            Object.BeforeMax = value;
        }
        public void OnChangeMinAfterValue(float value)
        {
            Object.AfterMin = value;
        }
        public void OnChangeMaxAfterValue(float value)
        {
            Object.AfterMax = value;
        }
        #endregion

        #region Private Methods

        protected override void SetFields(RescaleTreatment objectToDisplay)
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
            m_MinBeforeInputField.text = objectToDisplay.BeforeMin.ToString("0.##", cultureInfo);
            m_MaxBeforeInputField.text = objectToDisplay.BeforeMax.ToString("0.##", cultureInfo);

            m_MinAfterInputField.text = objectToDisplay.AfterMin.ToString("0.##", cultureInfo);
            m_MaxAfterInputField.text = objectToDisplay.AfterMax.ToString("0.##", cultureInfo);
        }
        #endregion
    }
}