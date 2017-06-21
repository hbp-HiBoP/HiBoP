using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class TimelineToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Timeline loop parameters
        /// </summary>
        [SerializeField]
        private Tools.TimelineLoop m_TimelineLoop;
        /// <summary>
        /// Timeline slider
        /// </summary>
        [SerializeField]
        private Tools.TimelineSlider m_TimelineSlider;
        /// <summary>
        /// Timeline slider
        /// </summary>
        [SerializeField]
        private Tools.TimelineGlobal m_TimelineGlobal;
        #endregion

        #region Private Methods
        protected override void AddTools()
        {
            m_Tools.Add(m_TimelineLoop);
            m_Tools.Add(m_TimelineSlider);
            m_Tools.Add(m_TimelineGlobal);
        }

        protected override void AddListeners()
        {
            base.AddListeners();

            m_TimelineGlobal.OnChangeValue.AddListener((global) =>
            {
                m_TimelineLoop.IsGlobal = global;
                m_TimelineSlider.IsGlobal = global;
            });
        }
        #endregion
    }
}