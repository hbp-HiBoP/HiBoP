using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineRecord : Tool
    {
        #region Properties
        /// <summary>
        /// Record a video of the timeline of the scene
        /// </summary>
        [SerializeField] private Button m_RecordVideo;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_RecordVideo.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3DUI.Scenes[SelectedScene].Video();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_RecordVideo.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamic = SelectedColumn is HBP.Module3D.Column3DDynamic;
            bool areAmplitudesComputed = SelectedScene.IsGeneratorUpToDate;

            m_RecordVideo.interactable = isColumnDynamic && areAmplitudesComputed;
        }
        #endregion
    }
}