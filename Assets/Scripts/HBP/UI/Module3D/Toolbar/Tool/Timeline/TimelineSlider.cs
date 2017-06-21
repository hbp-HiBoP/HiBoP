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
        private Text m_Max;

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
        #endregion

        #region Public Methods
        public override void AddListeners()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                int val = (int)value;
                ApplicationState.Module3D.SelectedScene.UpdateIEEGTimeline(val, IsGlobal);
                ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).CurrentTimeLineID = val;
                OnChangeValue.Invoke(val);
            });
        }

        public override void DefaultState()
        {
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
                m_Min.text = column.MinTimeLine.ToString("N2");
                m_Max.text = column.MaxTimeLine.ToString("N2");
            }
        }
        #endregion
    }
}