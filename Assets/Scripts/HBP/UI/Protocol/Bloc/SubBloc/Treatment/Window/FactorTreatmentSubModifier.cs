using System.Globalization;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class FactorTreatmentSubModifier : SubModifier<Core.Data.FactorTreatment>
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

            m_FactorInputField.onEndEdit.AddListener(OnChangeFactorValue);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.FactorTreatment objectToDisplay)
        {
            m_FactorInputField.text = objectToDisplay.Factor.ToString("0.##", CultureInfo.InvariantCulture);
        }
        void OnChangeFactorValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.Factor = floatResult;
            }
        }
        #endregion
    }
}