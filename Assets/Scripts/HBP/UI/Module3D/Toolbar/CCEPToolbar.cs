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
        [SerializeField] private Tools.CCEPModeSelector m_CCEPModeSelector;
        /// <summary>
        /// Change the site source
        /// </summary>
        [SerializeField] private Tools.CCEPSiteSourceSelector m_CCEPSiteSourceSelector;
        /// <summary>
        /// Change the area source
        /// </summary>
        [SerializeField] private Tools.CCEPAreaSourceSelector m_CCEPAreaSourceSelector;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_CCEPModeSelector);
            m_Tools.Add(m_CCEPSiteSourceSelector);
            m_Tools.Add(m_CCEPAreaSourceSelector);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();
            m_CCEPModeSelector.OnChangeValue.AddListener(m =>
            {
                switch (m)
                {
                    case Column3DCCEP.CCEPMode.Site:
                        m_CCEPSiteSourceSelector.gameObject.SetActive(true);
                        m_CCEPAreaSourceSelector.gameObject.SetActive(false);
                        break;
                    case Column3DCCEP.CCEPMode.MarsAtlas:
                        m_CCEPSiteSourceSelector.gameObject.SetActive(false);
                        m_CCEPAreaSourceSelector.gameObject.SetActive(true);
                        break;
                }
            });
        }
        #endregion
    }
}