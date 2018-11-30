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
        private Column3DIEEG m_Column;
        [SerializeField] private GameObject m_MainEventPrefab;
        [SerializeField] private GameObject m_SecondaryEventPrefab;
        #endregion

        #region Public Methods
        public void Initialize(Column3DIEEG column, Data.Visualization.Timeline timeline, Data.Visualization.SubTimeline subTimeline, float offset)
        {
            m_Column = column;
            m_Timeline = timeline;
            m_SubTimeline = subTimeline;
            m_MinText.text = subTimeline.MinTime.ToString("N2") + timeline.Unit;
            m_MaxText.text = subTimeline.MaxTime.ToString("N2") + timeline.Unit;
            UpdateCurrentTime();
            m_Before.GetComponent<LayoutElement>().flexibleWidth = (float)subTimeline.Before;// / timeline.Length;
            m_During.GetComponent<LayoutElement>().flexibleWidth = (float)subTimeline.Length;// / timeline.Length;
            m_After.GetComponent<LayoutElement>().flexibleWidth = (float)subTimeline.After;// / timeline.Length;
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
                float eventPosition = eventStatistics.IndexFromStart / (m_SubTimeline.Length - 1);
                RectTransform eventRectTransform = eventGameObject.GetComponent<RectTransform>();
                eventRectTransform.anchorMin = new Vector2(eventPosition, eventRectTransform.anchorMin.y);
                eventRectTransform.anchorMax = new Vector2(eventPosition, eventRectTransform.anchorMax.y);
                eventRectTransform.GetComponent<Tooltip>().Text = string.Format("{0} | {1} ({2}{3}) | {4}%",
                    e.Name,
                    eventStatistics.RoundedIndexFromStart,
                    m_SubTimeline.TimeStep * eventStatistics.RoundedIndexFromStart + m_SubTimeline.MinTime,
                    m_Timeline.Unit,
                    eventStatistics.NumberOfOccurenceBySubTrial * 100);
            }

            //GameObject mainEvent = Instantiate(m_MainEventPrefab, m_Events);
            //RectTransform mainEventRectTransform = mainEvent.GetComponent<RectTransform>();
            //Data.Visualization.Event mainEventData = m_Column.ColumnIEEGData.Data.TimeLine.MainEvent;
            //float mainEventPosition = (float)mainEventData.Position / (m_Column.ColumnIEEGData.Data.TimeLine.Lenght - 1);
            //mainEventRectTransform.anchorMin = new Vector2(mainEventPosition, mainEventRectTransform.anchorMin.y);
            //mainEventRectTransform.anchorMax = new Vector2(mainEventPosition, mainEventRectTransform.anchorMax.y);
            //mainEvent.GetComponent<Tooltip>().Text = mainEventData.Label + " | " + mainEventData.Position + " (" + (m_Column.ColumnIEEGData.Data.TimeLine.Step * mainEventData.Position + m_Column.Timeline.CurrentSubtimeline.MinTime).ToString("N2") + m_Column.Timeline.Unit + ")";

            //foreach (Data.Visualization.Event timelineEvent in m_Column.ColumnIEEGData.Data.TimeLine.SecondaryEvents)
            //{
            //    GameObject secondaryEvent = Instantiate(m_SecondaryEventPrefab, m_Events);
            //    RectTransform secondaryEventRectTransform = secondaryEvent.GetComponent<RectTransform>();
            //    float secondaryEventPosition = (float)timelineEvent.Position / (m_Column.ColumnIEEGData.Data.TimeLine.Lenght - 1);
            //    secondaryEventRectTransform.anchorMin = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMin.y);
            //    secondaryEventRectTransform.anchorMax = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMax.y);
            //    secondaryEvent.GetComponent<Tooltip>().Text = timelineEvent.Label + " | " + timelineEvent.Position + " (" + (m_Column.ColumnIEEGData.Data.TimeLine.Step * timelineEvent.Position + m_Column.Timeline.CurrentSubtimeline.MinTime).ToString("N2") + m_Column.Timeline.Unit + ")" + " | " + (timelineEvent.AttendanceRate * 100).ToString("N2") + "%";
            //}
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