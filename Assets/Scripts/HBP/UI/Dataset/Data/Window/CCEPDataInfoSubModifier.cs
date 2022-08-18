﻿using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class CCEPDataInfoSubModifier: SubModifier<Core.Data.CCEPDataInfo>
    {
        #region Properties     
        [SerializeField] InputField m_ChannelInputField;

        public override bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                base.Interactable = value;
                m_ChannelInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ChannelInputField.onValueChanged.AddListener((channel) => Object.StimulatedChannel = channel);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.CCEPDataInfo objectToDisplay)
        {
            m_ChannelInputField.text = objectToDisplay.StimulatedChannel;
        }
        #endregion
    }
}