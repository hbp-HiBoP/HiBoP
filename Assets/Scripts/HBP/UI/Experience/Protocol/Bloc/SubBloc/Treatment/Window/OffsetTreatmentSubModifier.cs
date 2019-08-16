using HBP.Data.Experience.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class OffsetTreatmentSubModifier : SubModifier<Data.Experience.Protocol.OffsetTreatment>
    {
        #region Properties
        [SerializeField] InputField m_OffsetInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_OffsetInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_OffsetInputField.onValueChanged.AddListener((value) => Object.Offset = float.Parse(value));
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(OffsetTreatment objectToDisplay)
        {
            m_OffsetInputField.text = objectToDisplay.Offset.ToString();
        }
        #endregion
    }
}