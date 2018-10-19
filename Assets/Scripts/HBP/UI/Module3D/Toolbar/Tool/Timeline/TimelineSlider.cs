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
        [SerializeField]
        private Slider m_Slider;
        [SerializeField]
        private Text m_Min;
        [SerializeField]
        private Text m_Current;
        [SerializeField]
        private Text m_Max;
        [SerializeField]
        private RectTransform m_Events;
        [SerializeField]
        private RectTransform m_RawTimeline;
        [SerializeField]
        private GameObject m_MainEventPrefab;
        [SerializeField]
        private GameObject m_SecondaryEventPrefab;

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
        private void ShowEvents(HBP.Module3D.Column3DIEEG column)
        {
            DeleteEvents();

            GameObject mainEvent = Instantiate(m_MainEventPrefab, m_Events);
            RectTransform mainEventRectTransform = mainEvent.GetComponent<RectTransform>();
            Data.Visualization.Event mainEventData = column.ColumnData.TimeLine.MainEvent;
            float mainEventPosition = (float)mainEventData.Position / (column.ColumnData.TimeLine.Lenght-1);
            mainEventRectTransform.anchorMin = new Vector2(mainEventPosition, mainEventRectTransform.anchorMin.y);
            mainEventRectTransform.anchorMax = new Vector2(mainEventPosition, mainEventRectTransform.anchorMax.y);
            mainEvent.GetComponent<Tooltip>().Text = mainEventData.Label + " | " + mainEventData.Position + " (" + (column.ColumnData.TimeLine.Step * mainEventData.Position + column.Timeline.MinTime).ToString("N2") + column.Timeline.Unit + ")";

            foreach (Data.Visualization.Event timelineEvent in column.ColumnData.TimeLine.SecondaryEvents)
            {
                GameObject secondaryEvent = Instantiate(m_SecondaryEventPrefab, m_Events);
                RectTransform secondaryEventRectTransform = secondaryEvent.GetComponent<RectTransform>();
                float secondaryEventPosition = (float)timelineEvent.Position / (column.ColumnData.TimeLine.Lenght - 1);
                secondaryEventRectTransform.anchorMin = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMin.y);
                secondaryEventRectTransform.anchorMax = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMax.y);
                secondaryEvent.GetComponent<Tooltip>().Text = timelineEvent.Label + " | " + timelineEvent.Position + " (" + (column.ColumnData.TimeLine.Step * timelineEvent.Position + column.Timeline.MinTime).ToString("N2") + column.Timeline.Unit + ")" + " | " + (timelineEvent.AttendanceRate * 100).ToString("N2") +"%";
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
                    m_Current.text = selectedColumn.Timeline.CurrentIndex + " (" + selectedColumn.Timeline.CurrentIndex.ToString("N2") + selectedColumn.Timeline.Unit + ")";
                    m_Slider.value = selectedColumn.Timeline.CurrentIndex;
                }
                ListenerLock = false;
            });
        }

        public override void DefaultState()
        {
            m_Min.text = "Min";
            m_Max.text = "Max";
            m_Current.text = "Current Time";
            m_Slider.value = 0;
            m_Slider.interactable = false;
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
                HBP.Module3D.Column3DIEEG column = ((HBP.Module3D.Column3DIEEG)SelectedColumn);
                m_Slider.maxValue = column.Timeline.Length - 1;
                m_Slider.value = column.Timeline.CurrentIndex;
                m_Min.text = column.ColumnData.TimeLine.Start.RawValue.ToString("N2") + column.Timeline.Unit;
                m_Max.text = column.ColumnData.TimeLine.End.RawValue.ToString("N2") + column.Timeline.Unit;
                m_Current.text = column.Timeline.CurrentIndex + " (" + column.Timeline.CurrentTime.ToString("N2") + column.Timeline.Unit + ")";
                m_RawTimeline.anchorMin = new Vector2((column.ColumnData.TimeLine.Start.RawValue - column.Timeline.MinTime) / (column.Timeline.MaxTime - column.Timeline.MinTime), 0);
                m_RawTimeline.anchorMax = new Vector2(1 - ((column.Timeline.MaxTime - column.ColumnData.TimeLine.End.RawValue) / (column.Timeline.MaxTime - column.Timeline.MinTime)), 1);
                ShowEvents(column);
            }
            else
            {
                m_Min.text = "Min";
                m_Max.text = "Max";
                m_Current.text = "Current Time";
                m_Slider.value = 0;
                m_RawTimeline.anchorMin = new Vector2(0, 0);
                m_RawTimeline.anchorMax = new Vector2(1, 1);
                DeleteEvents();
            }
        }
        #endregion
    }
}