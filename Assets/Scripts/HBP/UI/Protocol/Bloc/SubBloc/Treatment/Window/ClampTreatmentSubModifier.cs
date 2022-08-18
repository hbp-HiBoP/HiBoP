using System.Globalization;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class ClampTreatmentSubModifier : SubModifier<Core.Data.ClampTreatment>
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
            m_MinValueInputField.onEndEdit.AddListener(OnChangeMinValue);
            m_MaxValueInputField.onEndEdit.AddListener(OnChangeMaxValue);
        }

        #endregion

        #region Private Methods
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
        void OnChangeMinValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.Min = floatResult;
            }
        }
        void OnChangeMaxValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.Max = floatResult;
            }
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.ClampTreatment objectToDisplay)
        {
            m_UseMinClampToggle.isOn = objectToDisplay.UseMinClamp;
            m_UseMaxClampToggle.isOn = objectToDisplay.UseMaxClamp;

            m_MinValueInputField.text = objectToDisplay.Min.ToString("0.##", CultureInfo.InvariantCulture);
            m_MaxValueInputField.text = objectToDisplay.Max.ToString("0.##", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}