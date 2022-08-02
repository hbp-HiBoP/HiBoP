using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class StringTagValueSubModifier : SubModifier<Core.Data.StringTagValue>
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
        protected override void SetFields(Core.Data.StringTagValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_ValueInputField.text = objectToDisplay.Value;
        }
        void OnChangeValue(string value)
        {
            Object.Value = value;
        }
        #endregion
    }
}