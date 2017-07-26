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
        /// <summary>
        /// Timeline step
        /// </summary>
        [SerializeField]
        private Tools.TimelineStep m_TimelineStep;
        /// <summary>
        /// Timeline play
        /// </summary>
        [SerializeField]
        private Tools.TimelinePlay m_TimelinePlay;
        #endregion

        #region Private Methods
        protected override void AddTools()
        {
            m_Tools.Add(m_TimelineLoop);
            m_Tools.Add(m_TimelineSlider);
            m_Tools.Add(m_TimelineGlobal);
            m_Tools.Add(m_TimelineStep);
            m_Tools.Add(m_TimelinePlay);
        }

        protected override void AddListeners()
        {
            base.AddListeners();

            m_TimelineGlobal.OnChangeValue.AddListener((global) =>
            {
                m_TimelineLoop.IsGlobal = global;
                m_TimelineSlider.IsGlobal = global;
                m_TimelineStep.IsGlobal = global;
                m_TimelinePlay.IsGlobal = global;
            });
        }
        #endregion
    }
}