using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class TimelinePlay : Tool
    {
        #region Properties

        /// <summary>
        /// Make the timeline play
        /// </summary>
        [SerializeField] private Toggle m_Toggle;

        private bool m_IsGlobal = false;

        /// <summary>
        /// Are the changes applied to all columns ?
        /// </summary>
        public bool IsGlobal
        {
            get { return m_IsGlobal; }
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
            m_Toggle.onValueChanged.AddListener(value =>
            {
                if (ListenerLock) return;
                SelectedScene.SetTimelinePlaying(SelectedColumn, value, IsGlobal);
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
            bool isColumnDynamicOrFMRI = SelectedColumn?.NavigationTimeline != null;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_Toggle.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed;
        }

        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedColumn?.NavigationTimeline?.IsPlaying ?? false;
        }

        #endregion
    }
}
