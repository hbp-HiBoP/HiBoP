using System.Globalization;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class RescaleTreatmentSubModifier : SubModifier<Core.Data.RescaleTreatment>
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

            m_MinBeforeInputField.onEndEdit.AddListener(OnChangeMinBeforeValue);
            m_MaxBeforeInputField.onEndEdit.AddListener(OnChangeMaxBeforeValue);
            m_MinAfterInputField.onEndEdit.AddListener(OnChangeMinAfterValue);
            m_MaxAfterInputField.onEndEdit.AddListener(OnChangeMaxAfterValue);
        }
        #endregion

        #region Private Methods
        void OnChangeMinBeforeValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.BeforeMin = floatResult;
            }
        }
        void OnChangeMaxBeforeValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.BeforeMax = floatResult;
            }
        }
        void OnChangeMinAfterValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.AfterMin = floatResult;
            }
        }
        void OnChangeMaxAfterValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.AfterMax = floatResult;
            }
        }
        protected override void SetFields(Core.Data.RescaleTreatment objectToDisplay)
        {
            m_MinBeforeInputField.text = objectToDisplay.BeforeMin.ToString("0.##", CultureInfo.InvariantCulture);
            m_MaxBeforeInputField.text = objectToDisplay.BeforeMax.ToString("0.##", CultureInfo.InvariantCulture);

            m_MinAfterInputField.text = objectToDisplay.AfterMin.ToString("0.##", CultureInfo.InvariantCulture);
            m_MaxAfterInputField.text = objectToDisplay.AfterMax.ToString("0.##", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}