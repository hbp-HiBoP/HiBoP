using HBP.Data.Module3D;
using UnityEngine;
using HBP.Quest.Desktop;

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
            get { return m_IsGlobal; }
            set => SetDesktopGlobal(value, true);
        }

        internal static bool? QuestGlobal(Base3DScene scene) => QuestManager.Instance?.QuestTimelineGlobal(scene);

        internal static bool ResolveGlobal(Base3DScene scene, bool desktopGlobal) => QuestGlobal(scene) ?? desktopGlobal;

        internal void SetDesktopGlobal(bool value, bool applyImmediately)
        {
            m_IsGlobal = value;
            m_TimelineLoop.SetGlobal(value, applyImmediately);
            m_TimelineSlider.SetGlobal(value, applyImmediately);
            m_TimelineStep.SetGlobal(value, applyImmediately);
            m_TimelinePlay.SetGlobal(value, applyImmediately);
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
                if (QuestGlobal(Module3DMain.SelectedScene).HasValue) return;
                IsGlobal = global;
                m_ToolbarMenu.ActivitySettingsToolbar.IsGlobal = global;
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
        }

        #endregion
    }
}
