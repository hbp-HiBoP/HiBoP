using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class Screenshot : Tool
    {
        #region Properties
        /// <summary>
        /// Take a screenshot of the whole scene
        /// </summary>
        [SerializeField] private Button m_SingleScreenshot;
        /// <summary>
        /// Take multiple screenshots and export some tables of values
        /// </summary>
        [SerializeField] private Button m_MultiScreenshots;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_SingleScreenshot.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.OnRequestScreenshot.Invoke(false);
            });
            m_MultiScreenshots.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.OnRequestScreenshot.Invoke(true);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_SingleScreenshot.interactable = false;
            m_MultiScreenshots.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_SingleScreenshot.interactable = true;
            m_MultiScreenshots.interactable = true;
        }
        #endregion
    }
}