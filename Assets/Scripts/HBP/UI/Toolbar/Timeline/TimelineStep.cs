using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class TimelineStep : Tool
    {
        #region Properties

        /// <summary>
        /// Decrease the current sample
        /// </summary>
        [SerializeField] private Button m_Minus;

        /// <summary>
        /// Increase the current sample
        /// </summary>
        [SerializeField] private Button m_Plus;

        /// <summary>
        /// Change the step of increase/decrease
        /// </summary>
        [SerializeField] private InputField m_InputField;

        private bool m_IsGlobal = false;

        /// <summary>
        /// Do we need to perform the actions on all columns ?
        /// </summary>
        public bool IsGlobal
        {
            get { return m_IsGlobal; }
            set
            {
                m_IsGlobal = value;
                if (m_IsGlobal)
                {
                    m_InputField.onEndEdit.Invoke(m_InputField.text);
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
            m_Minus.onClick.AddListener(() =>
            {
                if (ListenerLock) return;
                SelectedScene.AdvanceTimeline(SelectedColumn, -1, IsGlobal);
            });
            m_Plus.onClick.AddListener(() =>
            {
                if (ListenerLock) return;
                SelectedScene.AdvanceTimeline(SelectedColumn, 1, IsGlobal);
            });
            m_InputField.onEndEdit.AddListener(value =>
            {
                if (ListenerLock) return;
                int step = int.TryParse(value, out int parsed) ? parsed : 1;
                SelectedScene.SetTimelineStep(SelectedColumn, step, IsGlobal);
                m_InputField.text = (SelectedColumn.NavigationTimeline?.Step ?? 1).ToString();
            });
        }

        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Minus.interactable = false;
            m_Plus.interactable = false;
            m_InputField.text = "1";
            m_InputField.interactable = false;
        }

        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamicOrFMRI = SelectedColumn?.NavigationTimeline != null;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_Minus.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed;
            m_InputField.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed;
            m_Plus.interactable = isColumnDynamicOrFMRI && areAmplitudesComputed;
        }

        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_InputField.text = (SelectedColumn?.NavigationTimeline?.Step ?? 1).ToString();
        }

        #endregion
    }
}
