using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class IEEGSettingsToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Threshold IEEG parameters
        /// </summary>
        [SerializeField]
        private Tools.ThresholdIEEG m_ThresholdIEEG;
        #endregion

        #region Private Methods
        protected override void AddTools()
        {
            m_Tools.Add(m_ThresholdIEEG);
        }
        #endregion
    }
}