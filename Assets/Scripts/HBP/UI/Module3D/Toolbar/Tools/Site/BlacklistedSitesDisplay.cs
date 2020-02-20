using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class BlacklistedSitesDisplay : Tool
    {
        #region Properties
        /// <summary>
        /// Hide the blacklisted sites
        /// </summary>
        [SerializeField] private Toggle m_Toggle;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                SelectedScene.HideBlacklistedSites = isOn;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Toggle.interactable = false;
            m_Toggle.isOn = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Toggle.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.HideBlacklistedSites;
        }
        #endregion
    }
}