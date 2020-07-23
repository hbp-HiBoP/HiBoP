using UnityEngine;

namespace HBP.UI.Module3D
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
        [SerializeField] private Tools.TimelineLoop m_TimelineLoop;
        /// <summary>
        /// Timeline slider
        /// </summary>
        [SerializeField] private Tools.TimelineSlider m_TimelineSlider;
        /// <summary>
        /// Timeline slider
        /// </summary>
        [SerializeField] private Tools.TimelineGlobal m_TimelineGlobal;
        /// <summary>
        /// Timeline step
        /// </summary>
        [SerializeField] private Tools.TimelineStep m_TimelineStep;
        /// <summary>
        /// Timeline play
        /// </summary>
        [SerializeField] private Tools.TimelinePlay m_TimelinePlay;
        /// <summary>
        /// Timeline record (used to record a video of the timeline)
        /// </summary>
        [SerializeField] private Tools.TimelineRecord m_TimelineRecord;
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
            });
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Called when showing this toolbar
        /// </summary>
        public override void ShowToolbarCallback()
        {
            m_TimelineGlobal.Set(m_ToolbarMenu.IEEGSettingsToolbar.IsGlobal);
        }
        #endregion
    }
}