using System;
using System.Collections;
using System.Collections.Generic;
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

        public GenericEvent<int> OnChangeValue = new GenericEvent<int>();
        #endregion

        #region Private Methods
        private void Update()
        {
            HBP.Module3D.Column3DIEEG column = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn);
            if (column)
            {
                ListenerLock = true;
                m_Slider.value = column.CurrentTimeLineID; //FIXME : maybe event
                ListenerLock = false;
            }
        }
        private void ShowEvents(HBP.Module3D.Column3DIEEG column)
        {
            foreach (Transform timelineEvent in m_Events)
            {
                Destroy(timelineEvent.gameObject);
            }

            RectTransform parentRectTransform = m_Events.GetComponent<RectTransform>();

            GameObject mainEvent = Instantiate(m_MainEventPrefab, m_Events);
            RectTransform mainEventRectTransform = mainEvent.GetComponent<RectTransform>();
            float mainEventPosition = (float)column.ColumnData.TimeLine.MainEvent.Position / (column.ColumnData.TimeLine.Lenght-1);
            mainEventRectTransform.anchorMin = new Vector2(mainEventPosition, mainEventRectTransform.anchorMin.y);
            mainEventRectTransform.anchorMax = new Vector2(mainEventPosition, mainEventRectTransform.anchorMax.y);

            foreach (Data.Visualization.Event timelineEvent in column.ColumnData.TimeLine.SecondaryEvents)
            {
                GameObject secondaryEvent = Instantiate(m_SecondaryEventPrefab, m_Events);
                RectTransform secondaryEventRectTransform = secondaryEvent.GetComponent<RectTransform>();
                float secondaryEventPosition = (float)timelineEvent.Position / (column.ColumnData.TimeLine.Lenght - 1);
                secondaryEventRectTransform.anchorMin = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMin.y);
                secondaryEventRectTransform.anchorMax = new Vector2(secondaryEventPosition, secondaryEventRectTransform.anchorMax.y);
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
                    foreach (HBP.Module3D.Column3DIEEG column in ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.CurrentTimeLineID = val;
                    }
                }
                else
                {
                    ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).CurrentTimeLineID = val;
                }
                OnChangeValue.Invoke(val);
            });
            ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineID.AddListener(() =>
            {
                HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn;
                m_Current.text = selectedColumn.CurrentTimeLineID + " (" + selectedColumn.CurrentTimeLine.ToString("N2") + selectedColumn.TimeLineUnite + ")";
            });
        }

        public override void DefaultState()
        {
            m_Min.text = "Min";
            m_Max.text = "Max";
            m_Current.text = "Current Time";
            m_Slider.value = 0;
            m_Slider.interactable = false;
        }

        public override void UpdateInteractable()
        {
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_Slider.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_Slider.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_Slider.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_Slider.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_Slider.interactable = true;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_Slider.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_Slider.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_Slider.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_Slider.interactable = false;
                    break;
                default:
                    break;
            }
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                HBP.Module3D.Column3DIEEG column = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn);
                m_Slider.value = column.CurrentTimeLineID;
                m_Slider.maxValue = column.MaxTimeLineID;
                m_Min.text = column.MinTimeLine.ToString("N2") + column.TimeLineUnite;
                m_Max.text = column.MaxTimeLine.ToString("N2") + column.TimeLineUnite;
                m_Current.text = column.CurrentTimeLineID + " (" + column.CurrentTimeLine.ToString("N2") + column.TimeLineUnite + ")";
                ShowEvents(column);
            }
        }
        #endregion
    }
}