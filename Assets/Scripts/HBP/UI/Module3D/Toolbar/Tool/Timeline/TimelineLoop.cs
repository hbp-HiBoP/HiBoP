using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineLoop : Tool
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
                    column.IsTimelineLooping = m_Toggle.isOn;
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
            bool isColumnIEEG = ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG;
            bool areAmplitudesComputed = ApplicationState.Module3D.SelectedScene.SceneInformation.IsGeneratorUpToDate;

            m_Toggle.interactable = isColumnIEEG && areAmplitudesComputed;
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column)
            {
                if (ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG)
                {
                    m_Toggle.isOn = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).IsTimelineLooping;
                }
                else
                {
                    m_Toggle.isOn = false;
                }
            }
        }
        #endregion
    }
}