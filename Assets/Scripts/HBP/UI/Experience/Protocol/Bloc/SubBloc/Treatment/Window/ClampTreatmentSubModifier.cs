using HBP.Data.Experience.Protocol;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class ClampTreatmentSubModifier : SubModifier<ClampTreatment>
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
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
            m_MinValueInputField.text = objectToDisplay.Min.ToString("0.##", cultureInfo);
            m_MaxValueInputField.text = objectToDisplay.Max.ToString("0.##", cultureInfo);
        }
        #endregion
    }
}