using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class CCEPToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Change the source
        /// </summary>
        [SerializeField] private Tools.CCEPSourceSelector m_CCEPSourceSelector;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_CCEPSourceSelector);
        }
        #endregion
    }
}