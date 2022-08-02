using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class IntTagSubModifier : SubModifier<Core.Data.IntTag>
    {
        #region Properties
        [SerializeField] Toggle m_ClampedToggle;
        [SerializeField] InputField m_MinInputField;
        [SerializeField] InputField m_MaxInputField;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ClampedToggle.onValueChanged.AddListener(OnChangeLimited);
            m_MinInputField.onValueChanged.AddListener(OnChangeMin);
            m_MaxInputField.onValueChanged.AddListener(OnChangeMax);
        }
        #endregion

        #region Private Methods
        protected void OnChangeLimited(bool value)
        {
            Object.Clamped = value;
            m_MinInputField.interactable = value;
            m_MaxInputField.interactable = value;
        }
        protected void OnChangeMin(string value)
        {
            if (int.TryParse(value, out int min)) Object.Min = min;
        }
        protected void OnChangeMax(string value)
        {
            if (int.TryParse(value, out int max)) Object.Max = max;
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.IntTag objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ClampedToggle.isOn = objectToDisplay.Clamped;

            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
            m_MinInputField.text = objectToDisplay.Min.ToString("0.##", cultureInfo);
            m_MaxInputField.text = objectToDisplay.Max.ToString("0.##", cultureInfo);
        }
        #endregion
    }
}