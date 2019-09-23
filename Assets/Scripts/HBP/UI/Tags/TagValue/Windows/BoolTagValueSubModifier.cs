using HBP.Data.Tags;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class BoolTagValueSubModifier : SubModifier<BoolTagValue>
    {
        #region Properties
        [SerializeField] Toggle m_ValueToggle;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_ValueToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ValueToggle.onValueChanged.AddListener(OnChangeValue);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(BoolTagValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_ValueToggle.isOn = objectToDisplay.Value;
        }
        void OnChangeValue(bool value)
        {
            Object.Value = value;
        }
        #endregion
    }
}