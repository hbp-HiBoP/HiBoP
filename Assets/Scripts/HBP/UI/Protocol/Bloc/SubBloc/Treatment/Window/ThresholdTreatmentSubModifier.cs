using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class ThresholdTreatmentSubModifier : SubModifier<Core.Data.ThresholdTreatment>
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
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_UseMinTresholdToggle.onValueChanged.AddListener(OnChangeUseMinValue);
            m_UseMaxTresholdToggle.onValueChanged.AddListener(OnChangeUseMaxValue);
        }
        public void OnChangeMinValue(float value)
        {
            Object.Min = value;
        }
        public void OnChangeMaxValue(float value)
        {
            Object.Max = value;
        }
        #endregion

        #region Private Methods

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

        #region Protected Methods
        protected override void SetFields(Core.Data.ThresholdTreatment objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_UseMinTresholdToggle.isOn = objectToDisplay.UseMinTreshold;
            m_UseMaxTresholdToggle.isOn = objectToDisplay.UseMaxTreshold;

            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
            m_MinValueInputField.text = objectToDisplay.Min.ToString("0.##", cultureInfo);
            m_MaxValueInputField.text = objectToDisplay.Max.ToString("0.##", cultureInfo);
        }
        #endregion
    }
}