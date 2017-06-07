using HBP.UI.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class DisplaySettingsToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Change IEEG colormap
        /// </summary>
        [SerializeField]
        private Tools.Colormap m_Colormap;
        /// <summary>
        /// Change brain surface color
        /// </summary>
        [SerializeField]
        private Tools.BrainColor m_BrainColor;
        /// <summary>
        /// Change brain cut color
        /// </summary>
        [SerializeField]
        private Tools.CutColor m_CutColor;
        /// <summary>
        /// Show / hide edges
        /// </summary>
        [SerializeField]
        private Tools.EdgeMode m_EdgeMode;
        /// <summary>
        /// Handle automatic rotation
        /// </summary>
        [SerializeField]
        private Tools.AutoRotate m_AutoRotate;
        /// <summary>
        /// Threshold MRI parameters
        /// </summary>
        [SerializeField]
        private Tools.ThresholdMRI m_ThresholdMRI;
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
        }
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_Colormap);
            m_Tools.Add(m_BrainColor);
            m_Tools.Add(m_CutColor);
            m_Tools.Add(m_EdgeMode);
            m_Tools.Add(m_AutoRotate);
            m_Tools.Add(m_ThresholdMRI);
        }
        #endregion
    }
}