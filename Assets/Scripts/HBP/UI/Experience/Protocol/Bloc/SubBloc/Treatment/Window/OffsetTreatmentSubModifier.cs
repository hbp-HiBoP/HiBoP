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

        public override Data.Experience.Protocol.OffsetTreatment Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_OffsetInputField.text = value.Offset.ToString();
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
    }
}