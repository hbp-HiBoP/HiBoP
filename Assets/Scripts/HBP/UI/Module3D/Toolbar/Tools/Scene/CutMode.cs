using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CutMode : Tool
    {
        #region Properties
        /// <summary>
        /// Toggle hard cut mode
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

                SelectedScene.StrongCuts = isOn;
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
            m_Toggle.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.StrongCuts;
        }
        #endregion
    }
}