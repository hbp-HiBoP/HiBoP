using HBP.Data.Tags;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class IntTagValueSubModifier : SubModifier<IntTagValue>
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
        protected override void SetFields(IntTagValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_ValueInputField.text = objectToDisplay.Value.ToString();
        }
        void OnChangeValue(string value)
        {
            if (int.TryParse(value, out int intValue))
            {
                Object.Value = intValue;
                m_ValueInputField.text = Object.Value.ToString();
            }
        }
        #endregion
    }
}