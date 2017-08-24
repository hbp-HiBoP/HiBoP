using HBP.UI.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ConfigurationToolbar : Toolbar
    {
        #region Properties
        [SerializeField]
        private Tools.ConfigurationLoaderSaver m_ConfigurationLoaderSaver;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_ConfigurationLoaderSaver);
        }
        #endregion
    }
}