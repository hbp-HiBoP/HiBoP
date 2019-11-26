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

                foreach (HBP.Module3D.Column3DDynamic column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.Timeline.IsLooping = m_Toggle.isOn;
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
            bool isColumnDynamic = SelectedColumn is HBP.Module3D.Column3DDynamic;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_Toggle.interactable = isColumnDynamic && areAmplitudesComputed;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn is HBP.Module3D.Column3DDynamic dynamicColumn)
            {
                m_Toggle.isOn = dynamicColumn.Timeline.IsLooping;
            }
            else
            {
                m_Toggle.isOn = false;
            }
        }
        #endregion
    }
}