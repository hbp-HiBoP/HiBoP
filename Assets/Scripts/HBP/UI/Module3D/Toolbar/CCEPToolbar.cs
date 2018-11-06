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
        /// Change the state of the ccep mode
        /// </summary>
        [SerializeField]
        private Tools.CCEPState m_CCEPState;
        /// <summary>
        /// Change the source
        /// </summary>
        [SerializeField]
        private Tools.CCEPSourceSelector m_CCEPSourceSelector;
        /// <summary>
        /// Change the connectivity file to use
        /// </summary>
        [SerializeField]
        private Tools.CCEPFileSelector m_CCEPFileSelector;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_CCEPState);
            m_Tools.Add(m_CCEPSourceSelector);
            m_Tools.Add(m_CCEPFileSelector);
        }
        protected override void AddListeners()
        {
            base.AddListeners();

            m_CCEPState.OnChangeState.AddListener((isOn) =>
            {
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
        }
        #endregion
    }
}