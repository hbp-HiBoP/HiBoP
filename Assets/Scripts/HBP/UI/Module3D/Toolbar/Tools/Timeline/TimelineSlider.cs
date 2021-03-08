using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
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
            get
            {
                return m_IsGlobal;
            }
            set
            {
                m_IsGlobal = value;
                if (m_IsGlobal)
                {
                    m_Slider.onValueChanged.Invoke(m_Slider.value);
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Display the subtimelines of the timeline
        /// </summary>
        private void ShowSubTimelines()
        {
            DeleteSubTimelines();

            if (SelectedColumn is HBP.Module3D.Column3DDynamic columnDynamic)
            {
                Data.Visualization.Timeline timeline = columnDynamic.Timeline;
                m_Slider.maxValue = timeline.Length - 1;
                m_Slider.value = timeline.CurrentIndex;
                foreach (var subTimeline in timeline.SubTimelinesBySubBloc.Values)
                {
                    SubTimeline subTl = Instantiate(m_TimelinePrefab, m_SubTimelines).GetComponent<SubTimeline>();
                    subTl.Initialize(timeline, subTimeline);
                    subTl.GetComponent<RectTransform>().anchorMin = new Vector2(Mathf.InverseLerp(0, timeline.Length - 1, subTimeline.GlobalMinIndex - subTimeline.Before), 0);
                    subTl.GetComponent<RectTransform>().anchorMax = new Vector2(Mathf.InverseLerp(0, timeline.Length - 1, subTimeline.GlobalMaxIndex + subTimeline.After), 1);
                }
            }
            else if (SelectedColumn is HBP.Module3D.Column3DFMRI columnFMRI)
            {
                Data.Visualization.BasicTimeline timeline = columnFMRI.Timeline;
                m_Slider.maxValue = timeline.Length - 1;
                m_Slider.value = timeline.CurrentIndex;
                SubTimeline subTl = Instantiate(m_TimelinePrefab, m_SubTimelines).GetComponent<SubTimeline>();
                subTl.Initialize(timeline, timeline.CurrentSubtimeline);
                subTl.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                subTl.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
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

                int val = (int)value;
                if (SelectedColumn is HBP.Module3D.Column3DDynamic)
                {
                    foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                    {
                        ((HBP.Module3D.Column3DDynamic)column).Timeline.CurrentIndex = val;
                    }
                }
                else if (SelectedColumn is HBP.Module3D.Column3DFMRI)
                {
                    foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                    {
                        ((HBP.Module3D.Column3DFMRI)column).Timeline.CurrentIndex = val;
                    }
                }
            });
            ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineIndex.AddListener(() =>
            {
                ListenerLock = true;
                if (SelectedColumn is HBP.Module3D.Column3DDynamic columnDynamic)
                {
                    m_Slider.value = columnDynamic.Timeline.CurrentIndex;
                    foreach (Transform subTimeline in m_SubTimelines)
                    {
                        subTimeline.GetComponent<SubTimeline>().UpdateCurrentTime();
                    }
                }
                else if (SelectedColumn is HBP.Module3D.Column3DFMRI columnFMRI)
                {
                    m_Slider.value = columnFMRI.Timeline.CurrentIndex;
                    foreach (Transform subTimeline in m_SubTimelines)
                    {
                        subTimeline.GetComponent<SubTimeline>().UpdateCurrentTime();
                    }
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
            bool isColumnDynamicOrFMRI = SelectedColumn is HBP.Module3D.Column3DDynamic || SelectedColumn is HBP.Module3D.Column3DFMRI;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_Slider.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if ((SelectedColumn is HBP.Module3D.Column3DDynamic || SelectedColumn is HBP.Module3D.Column3DFMRI) && SelectedScene.IsGeneratorUpToDate)
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