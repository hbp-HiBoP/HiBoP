using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class CCEPDataInfoSubModifier: SubModifier<d.CCEPDataInfo>
    {
        #region Properties     
        [SerializeField] InputField m_ChannelInputField;

        public override bool Interactable
        {
            get
            {
                return base.m_Interactable;
            }
            set
            {
                base.Interactable = value;
                m_ChannelInputField.interactable = value;
            }
        }
        public override d.CCEPDataInfo Object
        {
            get => base.Object;
            set
            {
                base.Object = value;
                m_ChannelInputField.text = value.StimulatedChannel;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ChannelInputField.onValueChanged.AddListener((channel) => m_Object.StimulatedChannel = channel);
        }
        #endregion
    }
}