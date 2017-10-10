using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelinePlay : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Toggle;

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
                    m_Toggle.onValueChanged.Invoke(m_Toggle.isOn);
                }
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnStopTimelinePlay.AddListener(() =>
            {
                ListenerLock = true;
                UpdateStatus(Toolbar.UpdateToolbarType.Column);
                ListenerLock = false;
            });
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                List<HBP.Module3D.Column3DIEEG> columns = new List<HBP.Module3D.Column3DIEEG>();
                if (IsGlobal)
                {
                    columns = ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG.ToList();
                }
                else
                {
                    columns.Add((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn);
                }

                foreach (HBP.Module3D.Column3DIEEG column in columns)
                {
                    column.IsTimelinePlaying = m_Toggle.isOn;
                }
            });
        }

        public override void DefaultState()
        {
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
        }

        public override void UpdateInteractable()
        {
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_Toggle.interactable = true;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_Toggle.interactable = false;
                    break;
                default:
                    break;
            }
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                m_Toggle.isOn = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).IsTimelinePlaying;
            }
        }
        #endregion
    }
}