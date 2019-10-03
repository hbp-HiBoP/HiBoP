using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineSlider : Tool
    {
        #region Properties
        [SerializeField] private Slider m_Slider;
        [SerializeField] private RectTransform m_SubTimelines;
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
        private void ShowSubTimelines()
        {
            DeleteSubTimelines();

            HBP.Module3D.Column3DDynamic column = (HBP.Module3D.Column3DDynamic)SelectedColumn;
            Data.Visualization.Timeline timeline = column.Timeline;
            m_Slider.maxValue = timeline.Length - 1;
            m_Slider.value = timeline.CurrentIndex;
            foreach (var subTimeline in timeline.SubTimelinesBySubBloc.Values)
            {
                SubTimeline subTl = Instantiate(m_TimelinePrefab, m_SubTimelines).GetComponent<SubTimeline>();
                subTl.Initialize(column, timeline, subTimeline, 0);
                subTl.GetComponent<RectTransform>().anchorMin = new Vector2(Mathf.InverseLerp(0, timeline.Length - 1, subTimeline.GlobalMinIndex - subTimeline.Before), 0);
                subTl.GetComponent<RectTransform>().anchorMax = new Vector2(Mathf.InverseLerp(0, timeline.Length - 1, subTimeline.GlobalMaxIndex + subTimeline.After), 1);
            }
        }
        private void DeleteSubTimelines()
        {
            foreach (Transform subTimeline in m_SubTimelines)
            {
                Destroy(subTimeline.gameObject);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                int val = (int)value;
                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.Timeline.CurrentIndex = val;
                }
            });
            ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineIndex.AddListener(() =>
            {
                ListenerLock = true;
                HBP.Module3D.Column3DDynamic selectedColumn = (HBP.Module3D.Column3DDynamic)SelectedColumn;
                if (selectedColumn)
                {
                    m_Slider.value = selectedColumn.Timeline.CurrentIndex;
                    foreach (Transform subTimeline in m_SubTimelines)
                    {
                        subTimeline.GetComponent<SubTimeline>().UpdateCurrentTime();
                    }
                }
                ListenerLock = false;
            });
        }

        public override void DefaultState()
        {
            m_Slider.value = 0;
            m_Slider.interactable = false;
            DeleteSubTimelines();
        }

        public override void UpdateInteractable()
        {
            bool isColumnDynamic = SelectedColumn is HBP.Module3D.Column3DDynamic;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_Slider.interactable = isColumnDynamic && areAmplitudesComputed;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn is HBP.Module3D.Column3DDynamic && SelectedScene.IsGeneratorUpToDate)
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