﻿using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.UI.Toolbar
{
    public class TimelineToolbar : Toolbar
    {
        #region Properties
        private bool m_IsGlobal = false;
        /// <summary>
        /// Are the changes applied with this toolbar applied to all columns at once?
        /// </summary>
        public bool IsGlobal
        {
            get
            {
                return m_IsGlobal;
            }
            set
            {
                m_IsGlobal = value;
                m_TimelineLoop.IsGlobal = value;
                m_TimelineSlider.IsGlobal = value;
                m_TimelineStep.IsGlobal = value;
                m_TimelinePlay.IsGlobal = value;
            }
        }
        /// <summary>
        /// Timeline loop parameters
        /// </summary>
        [SerializeField] private TimelineLoop m_TimelineLoop;
        /// <summary>
        /// Timeline slider
        /// </summary>
        [SerializeField] private TimelineSlider m_TimelineSlider;
        /// <summary>
        /// Timeline slider
        /// </summary>
        [SerializeField] private TimelineGlobal m_TimelineGlobal;
        /// <summary>
        /// Timeline step
        /// </summary>
        [SerializeField] private TimelineStep m_TimelineStep;
        /// <summary>
        /// Timeline play
        /// </summary>
        [SerializeField] private TimelinePlay m_TimelinePlay;
        /// <summary>
        /// Timeline record (used to record a video of the timeline)
        /// </summary>
        [SerializeField] private TimelineRecord m_TimelineRecord;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_TimelineLoop);
            m_Tools.Add(m_TimelineSlider);
            m_Tools.Add(m_TimelineGlobal);
            m_Tools.Add(m_TimelineStep);
            m_Tools.Add(m_TimelinePlay);
            m_Tools.Add(m_TimelineRecord);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_TimelineGlobal.OnChangeValue.AddListener((global) =>
            {
                IsGlobal = global;
                m_ToolbarMenu.ActivitySettingsToolbar.IsGlobal = global;
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
        }
        #endregion
    }
}