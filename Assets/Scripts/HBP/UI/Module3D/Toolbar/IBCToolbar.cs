using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class IBCToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Change the state of the IBC mode
        /// </summary>
        [SerializeField] private Tools.IBCState m_IBCState;
        /// <summary>
        /// Change the contrast
        /// </summary>
        [SerializeField] private Tools.IBCSelector m_IBCSelector;
        /// <summary>
        /// Change the parameters of the IBC contrasts
        /// </summary>
        [SerializeField] private Tools.IBCParameters m_IBCParameters;
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
        }
        #endregion
    }
}