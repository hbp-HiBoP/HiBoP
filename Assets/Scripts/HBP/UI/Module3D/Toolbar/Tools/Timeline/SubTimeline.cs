using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;

namespace HBP.UI.Module3D.Tools
{
    public class SubTimeline : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Text to display the minimum time of the subtimeline
        /// </summary>
        [SerializeField] private Text m_MinText;
        /// <summary>
        /// Text to display the maximum time of the subtimeline
        /// </summary>
        [SerializeField] private Text m_MaxText;
        /// <summary>
        /// Text to display the current time of the subtimeline
        /// </summary>
        [SerializeField] private Text m_Current;
        /// <summary>
        /// Container for the events
        /// </summary>
        [SerializeField] private RectTransform m_Events;
        /// <summary>
        /// RectTransform to represent the time before the subtimeline
        /// </summary>
        [SerializeField] private RectTransform m_Before;
        /// <summary>
        /// RectTransform to contain information of the subtimeline
        /// </summary>
        [SerializeField] private RectTransform m_During;
        /// <summary>
        /// RectTransform to represent the time after the subtimeline
        /// </summary>
        [SerializeField] private RectTransform m_After;
        /// <summary>
        /// Timeline data of the parent timeline
        /// </summary>
        private Core.Data.BasicTimeline m_Timeline;
        /// <summary>
        /// Subtimeline data of the subtimeline
        /// </summary>
        private Core.Data.SubTimeline m_SubTimeline;

        /// <summary>
        /// Prefab for the main event
        /// </summary>
        [SerializeField] private GameObject m_MainEventPrefab;
        /// <summary>
        /// Prefab for the secondary events
        /// </summary>
        [SerializeField] private GameObject m_SecondaryEventPrefab;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the subtimeline
        /// </summary>
        /// <param name="column">Column to be considered</param>
        /// <param name="timeline">Data timeline of the parent timeline</param>
        /// <param name="subTimeline">Subtimeline data</param>
        public void Initialize(Core.Data.BasicTimeline timeline, Core.Data.SubTimeline subTimeline)
        {
            m_Timeline = timeline;
            m_SubTimeline = subTimeline;
            m_MinText.text = subTimeline.MinTime.ToString("N0") + timeline.Unit;
            m_MaxText.text = subTimeline.MaxTime.ToString("N0") + timeline.Unit;
            UpdateCurrentTime();
            float beforeDuringTransition = Mathf.InverseLerp(0, subTimeline.Length + subTimeline.Before + subTimeline.After, subTimeline.Before);
            float duringAfterTransition = Mathf.InverseLerp(0, subTimeline.Length + subTimeline.Before + subTimeline.After, subTimeline.Before + subTimeline.Length);
            m_Before.anchorMin = new Vector2(0, 0);
            m_Before.anchorMax = new Vector2(beforeDuringTransition, 1);
            m_During.anchorMin = new Vector2(beforeDuringTransition, 0);
            m_During.anchorMax = new Vector2(duringAfterTransition, 1);
            m_After.anchorMin = new Vector2(duringAfterTransition, 0);
            m_After.anchorMax = new Vector2(1, 1);
            ShowEvents();
        }
        /// <summary>
        /// Update the text displaying the current time of the subtimeline
        /// </summary>
        public void UpdateCurrentTime()
        {
            if (m_Timeline.CurrentSubtimeline == m_SubTimeline)
            {
                m_Current.gameObject.SetActive(true);
                m_Current.text = m_SubTimeline.GetLocalIndex(m_Timeline.CurrentIndex) + " (" + m_SubTimeline.GetLocalTime(m_Timeline.CurrentIndex).ToString("N2") + m_Timeline.Unit + ")";
            }
            else
            {
                m_Current.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Display the events on the subtimeline
        /// </summary>
        private void ShowEvents()
        {
            DeleteEvents();

            foreach (var eventStat in m_SubTimeline.StatisticsByEvent)
            {
                Core.Data.Event e = eventStat.Key;
                Core.Data.EventStatistics eventStatistics = eventStat.Value;
                if (eventStatistics.NumberOfOccurences == 0) continue;

                GameObject eventGameObject;
                if (e.Type == MainSecondaryEnum.Main)
                {
                    eventGameObject = Instantiate(m_MainEventPrefab, m_Events);
                }
                else
                {
                    eventGameObject = Instantiate(m_SecondaryEventPrefab, m_Events);
                }
                int eventIndex = m_SubTimeline.Frequency.ConvertToFlooredNumberOfSamples(eventStatistics.RoundedTimeFromStart);
                float eventPosition = Mathf.InverseLerp(0, m_SubTimeline.Length - 1, eventIndex);
                RectTransform eventRectTransform = eventGameObject.GetComponent<RectTransform>();
                eventRectTransform.anchorMin = new Vector2(eventPosition, eventRectTransform.anchorMin.y);
                eventRectTransform.anchorMax = new Vector2(eventPosition, eventRectTransform.anchorMax.y);
                eventRectTransform.GetComponent<Tooltip>().Text = string.Format("{0} | {1} ({2}{3}) | {4}%",
                    e.Name,
                    eventIndex,
                    m_SubTimeline.TimeStep * eventIndex + m_SubTimeline.MinTime,
                    m_Timeline.Unit,
                    eventStatistics.NumberOfOccurenceBySubTrial * 100);
            }
        }
        /// <summary>
        /// Remove the events of the subtimeline
        /// </summary>
        private void DeleteEvents()
        {
            foreach (Transform timelineEvent in m_Events)
            {
                Destroy(timelineEvent.gameObject);
            }
        }
        #endregion
    }
}