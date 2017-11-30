using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ThresholdMRI : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;

        [SerializeField]
        private Module3D.ThresholdMRI m_ThresholdMRI;
        #endregion

        #region Public Methods
        public override void Initialize()
        {

        }

        public override void DefaultState()
        {
            m_Button.interactable = false;
        }

        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                m_ThresholdMRI.UpdateMRICalValues(ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedMRI.Volume.ExtremeValues);
            }
        }
        #endregion
    }
}