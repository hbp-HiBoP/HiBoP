using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class RescaleTreatmentSubModifier : SubModifier<Data.Experience.Protocol.RescaleTreatment>
    {
        #region Properties
        [SerializeField] InputField m_MinInputField;
        [SerializeField] InputField m_MaxInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_MinInputField.interactable = value;
                m_MaxInputField.interactable = value;
            }
        }

        public override Data.Experience.Protocol.RescaleTreatment Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_MinInputField.text = value.Min.ToString();
                m_MaxInputField.text = value.Max.ToString();
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_MinInputField.onValueChanged.AddListener((value) => Object.Min = float.Parse(value));
            m_MaxInputField.onValueChanged.AddListener((value) => Object.Max = float.Parse(value));
        }
        #endregion
    }
}