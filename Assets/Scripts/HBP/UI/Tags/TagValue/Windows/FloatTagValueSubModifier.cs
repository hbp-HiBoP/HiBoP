using HBP.Data.Tags;
using System.Globalization;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class FloatTagValueSubModifier : SubModifier<FloatTagValue>
    {
        #region Properties
        [SerializeField] InputField m_ValueInputField;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_ValueInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ValueInputField.onValueChanged.AddListener(OnChangeValue);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(FloatTagValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
            m_ValueInputField.text = objectToDisplay.Value.ToString("0.##", cultureInfo);
        }
        void OnChangeValue(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatValue))
            {
                Object.Value = floatValue;
                m_ValueInputField.text = Object.Value.ToString();
            }
        }
        #endregion
    }
}