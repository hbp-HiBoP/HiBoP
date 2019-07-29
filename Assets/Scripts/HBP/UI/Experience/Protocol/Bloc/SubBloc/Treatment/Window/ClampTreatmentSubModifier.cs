using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
    public class ClampTreatmentSubModifier : SubModifier<Data.Experience.Protocol.ClampTreatment>
    {
        #region Properties
        [SerializeField] Toggle m_UseMinClampToggle;
        [SerializeField] Toggle m_UseMaxClampToggle;
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
                m_UseMinClampToggle.interactable = value;
                m_UseMaxClampToggle.interactable = value;
                m_MinValueInputField.interactable = value && base.Object.UseMinClamp;
                m_MaxValueInputField.interactable = value && base.Object.UseMaxClamp;
            }
        }

        public override Data.Experience.Protocol.ClampTreatment Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_UseMinClampToggle.isOn = value.UseMinClamp;
                m_UseMaxClampToggle.isOn = value.UseMaxClamp;
                m_MinValueInputField.text = value.Min.ToString();
                m_MaxValueInputField.text = value.Max.ToString();
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_UseMinClampToggle.onValueChanged.AddListener((value) => Object.UseMinClamp = value);
            m_UseMaxClampToggle.onValueChanged.AddListener((value) => Object.UseMaxClamp = value);
            m_MinValueInputField.onValueChanged.AddListener((value) => Object.Min = float.Parse(value));
            m_MaxValueInputField.onValueChanged.AddListener((value) => Object.Max = float.Parse(value));
        }
        #endregion
    }
}