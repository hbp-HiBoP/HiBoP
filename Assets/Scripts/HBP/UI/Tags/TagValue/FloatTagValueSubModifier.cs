using System.Globalization;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class FloatTagValueSubModifier : SubModifier<Data.FloatTagValue>
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
            m_ValueInputField.onEndEdit.AddListener(OnChangeValue);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Data.FloatTagValue objectToDisplay)
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