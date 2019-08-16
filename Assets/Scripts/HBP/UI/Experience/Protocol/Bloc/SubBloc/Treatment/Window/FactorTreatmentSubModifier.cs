using HBP.Data.Experience.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class FactorTreatmentSubModifier : SubModifier<Data.Experience.Protocol.FactorTreatment>
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
        public override void Initialize()
        {
            base.Initialize();
            m_FactorInputField.onValueChanged.AddListener(OnChangeFactorValue);
        }
        #endregion

        #region Private Methods
        void OnChangeFactorValue(string value)
        {
            if (float.TryParse(value, out float result)) Object.Factor = result;
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(FactorTreatment objectToDisplay)
        {
            m_FactorInputField.text = objectToDisplay.Factor.ToString();
        }
        #endregion
    }
}