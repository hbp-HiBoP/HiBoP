using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Tools;

namespace HBP.UI.Toolbar
{
    public class TimelineGlobal : Tool
    {
        #region Properties

        /// <summary>
        /// Toggle global mode
        /// </summary>
        [SerializeField] private Toggle m_Toggle;

        private bool? m_LastQuestGlobal;

        #endregion

        #region Events

        /// <summary>
        /// Event called when changing the global mode
        /// </summary>
        public GenericEvent<bool> OnChangeValue = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock || TimelineToolbar.QuestGlobal(SelectedScene).HasValue) return;

                OnChangeValue.Invoke(isOn);
            });
        }

        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_LastQuestGlobal = TimelineToolbar.QuestGlobal(SelectedScene);
            m_Toggle.interactable = false;
        }

        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamicOrFMRI = SelectedColumn?.NavigationTimeline != null;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_LastQuestGlobal = TimelineToolbar.QuestGlobal(SelectedScene);
            m_Toggle.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed && !m_LastQuestGlobal.HasValue;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void UpdateStatus()
        {
            m_Toggle.SetIsOnWithoutNotify(m_LastQuestGlobal ?? GetComponentInParent<TimelineToolbar>(true).IsGlobal);
        }

        private void Update()
        {
            if (m_LastQuestGlobal != TimelineToolbar.QuestGlobal(SelectedScene)) UpdateTool();
        }

        #endregion
    }
}
