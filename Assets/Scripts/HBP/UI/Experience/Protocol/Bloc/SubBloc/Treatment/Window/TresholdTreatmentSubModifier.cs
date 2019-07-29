using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class TresholdTreatmentSubModifier : SubModifier<Data.Experience.Protocol.TresholdTreatment>
    {
        #region Properties
        [SerializeField] Toggle m_UseMinTresholdToggle;
        [SerializeField] Toggle m_UseMaxTresholdToggle;
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
                m_UseMinTresholdToggle.interactable = value;
                m_UseMaxTresholdToggle.interactable = value;
                m_MinValueInputField.interactable = value && base.Object.UseMinTreshold;
                m_MaxValueInputField.interactable = value && base.Object.UseMaxTreshold;
            }
        }

        public override Data.Experience.Protocol.TresholdTreatment Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_UseMinTresholdToggle.isOn = value.UseMinTreshold;
                m_UseMaxTresholdToggle.isOn = value.UseMaxTreshold;
                m_MinValueInputField.text = value.Min.ToString();
                m_MaxValueInputField.text = value.Max.ToString();
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_UseMinTresholdToggle.onValueChanged.AddListener((value) => Object.UseMinTreshold = value);
            m_UseMaxTresholdToggle.onValueChanged.AddListener((value) => Object.UseMaxTreshold = value);
            m_MinValueInputField.onValueChanged.AddListener((value) => Object.Min = float.Parse(value));
            m_MaxValueInputField.onValueChanged.AddListener((value) => Object.Max = float.Parse(value));
        }
        #endregion
    }
}