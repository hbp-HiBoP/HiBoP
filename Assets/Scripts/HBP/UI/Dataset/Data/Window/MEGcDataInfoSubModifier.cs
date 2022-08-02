﻿using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Dataset
{
    public class MEGcDataInfoSubModifier : SubModifier<Core.Data.MEGcDataInfo>
    {
        #region Properties     
        public override bool Interactable
        {
            get
            {
                return base.m_Interactable;
            }
            set
            {
                base.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.MEGcDataInfo objectToDisplay)
        {
        }
        #endregion
    }
}