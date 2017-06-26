using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class SiteToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Display the name of the selected site
        /// </summary>
        [SerializeField]
        private Tools.SelectedSite m_SelectedSite;
        /// <summary>
        /// Modify the state of some sites
        /// </summary>
        [SerializeField]
        private Tools.SiteModifier m_SiteModifier;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_SelectedSite);
            m_Tools.Add(m_SiteModifier);
        }
        #endregion
    }
}