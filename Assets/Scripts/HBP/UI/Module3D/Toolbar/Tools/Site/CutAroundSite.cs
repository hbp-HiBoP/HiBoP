using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CutAroundSite : Tool
    {
        #region Properties
        /// <summary>
        /// Button to create 3 cuts around the selected site
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
                if (ListenerLock) return;

                SelectedScene.AutomaticCutAroundSelectedSite = isOn;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Toggle.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Toggle.interactable = true;
        }
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.AutomaticCutAroundSelectedSite;
        }
        #endregion
    }
}