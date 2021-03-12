using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineLoop : Tool
    {
        #region Properties
        /// <summary>
        /// Make the timeline loop
        /// </summary>
        [SerializeField] private Toggle m_Toggle;

        private bool m_IsGlobal = false;
        /// <summary>
        /// Are the changes applied to all columns ?
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
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;
                
                if (SelectedColumn is HBP.Module3D.Column3DDynamic)
                {
                    foreach (HBP.Module3D.Column3DDynamic column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                    {
                        column.Timeline.IsLooping = m_Toggle.isOn;
                    }
                }
                else if (SelectedColumn is HBP.Module3D.Column3DFMRI)
                {
                    foreach (HBP.Module3D.Column3DFMRI column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                    {
                        column.Timeline.IsLooping = m_Toggle.isOn;
                    }
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamicOrFMRI = SelectedColumn is HBP.Module3D.Column3DDynamic || SelectedColumn is HBP.Module3D.Column3DFMRI;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_Toggle.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedColumn is HBP.Module3D.Column3DDynamic dynamicColumn)
            {
                m_Toggle.isOn = dynamicColumn.Timeline.IsLooping;
            }
            else if (SelectedColumn is HBP.Module3D.Column3DFMRI fmriColumn)
            {
                m_Toggle.isOn = fmriColumn.Timeline.IsLooping;
            }
            else
            {
                m_Toggle.isOn = false;
            }
        }
        #endregion
    }
}