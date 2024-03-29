﻿using UnityEngine;

namespace HBP.UI.Toolbar
{
    public class AtlasToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Change the state of the IBC mode
        /// </summary>
        [SerializeField] private AtlasState m_IBCState;
        /// <summary>
        /// Change the contrast
        /// </summary>
        [SerializeField] private IBCSelector m_IBCSelector;
        /// <summary>
        /// Change the parameters of the IBC contrasts
        /// </summary>
        [SerializeField] private IBCParameters m_IBCParameters;

        [SerializeField] private DiFuMoSelector m_DiFuMoSelector;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_IBCState);
            m_Tools.Add(m_IBCSelector);
            m_Tools.Add(m_IBCParameters);
            m_Tools.Add(m_DiFuMoSelector);
        }
        #endregion
    }
}