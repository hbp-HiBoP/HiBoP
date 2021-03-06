﻿using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class FMRIDataInfoSubModifier : SubModifier<d.FMRIDataInfo>
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
        protected override void SetFields(d.FMRIDataInfo objectToDisplay)
        {
        }
        #endregion
    }
}