using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class TimelineSlider : Tool
    {
        #region Properties

        /// <summary>
        /// Slider to change the current sample of the timeline
        /// </summary>
        [SerializeField] private Slider m_Slider;

        /// <summary>
        /// Subtimelines of the timeline
        /// </summary>
        [SerializeField] private RectTransform m_SubTimelines;

        /// <summary>
        /// Prefab for the subtimeline
        /// </summary>
        [SerializeField] private GameObject m_TimelinePrefab;

        private bool m_IsGlobal = false;

        /// <summary>
        /// Do we need to perform the actions on all columns ?
        /// </summary>
        public bool IsGlobal
        {
            get { return m_IsGlobal; }
            set => SetGlobal(value, true);
        }

        internal void SetGlobal(bool value, bool applyImmediately)
        {
            m_IsGlobal = value;
            if (value && applyImmediately) m_Slider.onValueChanged.Invoke(m_Slider.value);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Display the subtimelines of the timeline
        /// </summary>
        private void ShowSubTimelines()
        {
            DeleteSubTimelines();

            var timeline = SelectedColumn?.NavigationTimeline;
            if (timeline == null) return;
            m_Slider.maxValue = timeline.Length - 1;
            m_Slider.value = timeline.CurrentIndex;
            foreach (var (segment, start, end) in SubTimeline.GetSegments(timeline))
            {
                SubTimeline subTimeline = Instantiate(m_TimelinePrefab, m_SubTimelines).GetComponent<SubTimeline>();
                subTimeline.Initialize(timeline, segment);
                var rect = subTimeline.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(start, 0);
                rect.anchorMax = new Vector2(end, 1);
            }
        }

        /// <summary>
        /// Remove all subtimelines of the timeline
        /// </summary>
        private void DeleteSubTimelines()
        {
            foreach (Transform subTimeline in m_SubTimelines)
            {
                Destroy(subTimeline.gameObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.SetTimelineIndex(SelectedColumn, (int)value, TimelineToolbar.ResolveGlobal(SelectedScene, IsGlobal));
            });
            Module3DMain.OnUpdateSelectedColumnTimeLineIndex.AddListener(() =>
            {
                ListenerLock = true;
                if (SelectedColumn?.NavigationTimeline != null)
                {
                    m_Slider.SetValueWithoutNotify(SelectedColumn.NavigationTimeline.CurrentIndex);
                    foreach (Transform subTimeline in m_SubTimelines)
                        subTimeline.GetComponent<SubTimeline>().UpdateCurrentTime();
                }

                ListenerLock = false;
            });
        }

        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Slider.value = 0;
            m_Slider.interactable = false;
            DeleteSubTimelines();
        }

        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamicOrFMRI = SelectedColumn?.NavigationTimeline != null;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_Slider.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed;
        }

        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if ((SelectedColumn?.NavigationTimeline != null) && SelectedScene.IsGeneratorUpToDate)
            {
                ShowSubTimelines();
            }
            else
            {
                m_Slider.value = 0;
                DeleteSubTimelines();
            }
        }

        #endregion
    }
}
