using HBP.Data.Module3D;
using HBP.UI.Module3D;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class OpenSiteTools : Tool
    {
        #region Properties
        [SerializeField] private Button m_OpenToolsButton;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_OpenToolsButton.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                var siteTools = WindowsManager.Open("Site Tools window", null).GetComponent<SiteToolsWindow>();
                siteTools.Scene = SelectedScene;
                siteTools.OnToolApplied.AddListener(Module3DMain.OnRequestUpdateInSiteList.Invoke);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_OpenToolsButton.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_OpenToolsButton.interactable = true;
        }
        #endregion
    }
}