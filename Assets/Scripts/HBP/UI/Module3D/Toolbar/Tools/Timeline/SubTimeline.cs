using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SubTimeline : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_MinText;
        [SerializeField] private Text m_MaxText;
        [SerializeField] private Text m_Current;
        [SerializeField] private RectTransform m_Events;
        [SerializeField] private RectTransform m_Before;
        [SerializeField] private RectTransform m_During;
        [SerializeField] private RectTransform m_After;
        private Data.Visualization.Timeline m_Timeline;
        private Data.Visualization.SubTimeline m_SubTimeline;
        private Column3DDynamic m_Column;
        [SerializeField] private GameObject m_MainEventPrefab;
        [SerializeField] private GameObject m_SecondaryEventPrefab;
        #endregion

        #region Public Methods
        public void Initialize(Column3DDynamic column, Data.Visualization.Timeline timeline, Data.Visualization.SubTimeline subTimeline, float offset)
        {
            m_Column = column;
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
        private void ShowEvents()
        {
            DeleteEvents();

            foreach (var eventStat in m_SubTimeline.StatisticsByEvent)
            {
                Data.Experience.Protocol.Event e = eventStat.Key;
                Data.Experience.Dataset.EventStatistics eventStatistics = eventStat.Value;
                if (eventStatistics.NumberOfOccurences == 0) continue;

                GameObject eventGameObject;
                if (e.Type == Data.Enums.MainSecondaryEnum.Main)
                {
                    eventGameObject = Instantiate(m_MainEventPrefab, m_Events);
                }
                else
                {
                    eventGameObject = Instantiate(m_SecondaryEventPrefab, m_Events);
                }
                int eventIndex = m_SubTimeline.Frequency.ConvertToRoundedNumberOfSamples(eventStatistics.RoundedTimeFromStart) - 1;
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