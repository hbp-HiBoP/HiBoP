using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class FMRIToolbar : Toolbar
    {
        #region Properties
        [SerializeField]
        private Tools.FMRISelector m_FMRISelector;
        [SerializeField]
        private Tools.FMRIParameters m_FMRIParameters;
        #endregion

        #region Private Methods
        protected override void AddTools()
        {
            m_Tools.Add(m_FMRISelector);
            m_Tools.Add(m_FMRIParameters);
        }
        protected override void AddListeners()
        {
            base.AddListeners();

            m_FMRISelector.OnChangeFMRI.AddListener(() =>
            {
                UpdateInteractableButtons();
                UpdateButtonsStatus(UpdateToolbarType.Scene);
            });
        }
        #endregion
    }
}