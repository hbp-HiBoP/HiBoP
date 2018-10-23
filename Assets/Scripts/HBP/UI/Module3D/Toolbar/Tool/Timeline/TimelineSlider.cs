using System;
using System.Collections;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineSlider : Tool
    {
        #region Properties
        [SerializeField] private Slider m_Slider;
        [SerializeField] private RectTransform m_SubTimelines;
        [SerializeField] private RectTransform m_Events;
        [SerializeField] private GameObject m_TimelinePrefab;
        [SerializeField] private GameObject m_MainEventPrefab;
        [SerializeField] private GameObject m_SecondaryEventPrefab;

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

            HBP.Module3D.Timeline timeline = ((HBP.Module3D.Column3DIEEG)SelectedColumn).Timeline;
            m_Slider.maxValue = timeline.Length - 1;
            m_Slider.value = timeline.CurrentIndex;
            foreach (var subTimeline in timeline.SubTimelines)
            {
                SubTimeline subTl = Instantiate(m_TimelinePrefab, m_SubTimelines).GetComponent<SubTimeline>();
                subTl.Initialize(timeline, subTimeline, m_Slider.handleRect.rect.width);
            }
        }
        private void ShowEvents()
        {
            DeleteEvents();

            HBP.Module3D.Column3DIEEG column = ((HBP.Module3D.Column3DIEEG)SelectedColumn);
            GameObject mainEvent = Instantiate(m_MainEventPrefab, m_Events);
            RectTransform mainEventRectTransform = mainEvent.GetComponent<RectTransform>();
            Data.Visualization.Event mainEventData = column.ColumnData.TimeLine.MainEvent;
            float mainEventPosition = (float)mainEventData.Position / (column.ColumnData.TimeLine.Lenght-1);
            mainEventRectTransform.anchorMin = new Vector2(mainEventPosition, mainEventRectTransform.anchorMin.y);
            mainEventRectTransform.anchorMax = new Vector2(mainEventPosition, mainEventRectTransform.anchorMax.y);
            mainEvent.GetComponent<Tooltip>().Text = mainEventData.Label + " | " + mainEventData.Position + " (" + (column.ColumnData.TimeLine.Step * mainEventData.Position + column.Timeline.CurrentSubtimeline.MinTime).ToString("N2") + column.Timeline.Unit + ")";

            foreach (Data.Visualization.Event timelineEvent in column.ColumnData.TimeLine.SecondaryEvents)
            {
                GameObject secondaryEvent = Instantiate(m_SecondaryEventPrefab, m_Events);
                RectTransform secondaryEventRectTransform = secondaryEvent.GetComponent<RectTransform>();
                float secondaryEventPosition = (float)timelineEvent.Position / (column.ColumnData.TimeLine.Lenght - 1);
                secondaryEventRectTransform.anchorMin = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMin.y);
                secondaryEventRectTransform.anchorMax = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMax.y);
                secondaryEvent.GetComponent<Tooltip>().Text = timelineEvent.Label + " | " + timelineEvent.Position + " (" + (column.ColumnData.TimeLine.Step * timelineEvent.Position + column.Timeline.CurrentSubtimeline.MinTime).ToString("N2") + column.Timeline.Unit + ")" + " | " + (timelineEvent.AttendanceRate * 100).ToString("N2") +"%";
            }
        }
        private void DeleteSubTimelines()
        {
            foreach (Transform subTimeline in m_SubTimelines)
            {
                Destroy(subTimeline.gameObject);
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

        #region Public Methods
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                int val = (int)value;
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DIEEG column in SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.Timeline.CurrentIndex = val;
                    }
                }
                else
                {
                    ((HBP.Module3D.Column3DIEEG)SelectedColumn).Timeline.CurrentIndex = val;
                }
            });
            ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineID.AddListener(() =>
            {
                ListenerLock = true;
                HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)SelectedColumn;
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
            DeleteEvents();
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = SelectedColumn.Type == Data.Enums.ColumnType.iEEG;
            bool areAmplitudesComputed = SelectedScene.SceneInformation.IsGeneratorUpToDate;

            m_Slider.interactable = isColumnIEEG && areAmplitudesComputed;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn.Type == Data.Enums.ColumnType.iEEG)
            {
                ShowSubTimelines();
                ShowEvents();
            }
            else
            {
                m_Slider.value = 0;
                DeleteSubTimelines();
                DeleteEvents();
            }
        }
        #endregion
    }
}