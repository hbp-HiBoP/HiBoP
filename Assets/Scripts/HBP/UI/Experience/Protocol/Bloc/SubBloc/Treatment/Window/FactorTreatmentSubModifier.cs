using HBP.Data.Experience.Protocol;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class FactorTreatmentSubModifier : SubModifier<FactorTreatment>
    {
        #region Properties
        [SerializeField] InputField m_FactorInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_FactorInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void OnChangeFactorValue(float value)
        {
            Object.Factor = value;
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(FactorTreatment objectToDisplay)
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
            m_FactorInputField.text = objectToDisplay.Factor.ToString("0.##", cultureInfo);
        }
        #endregion
    }
}