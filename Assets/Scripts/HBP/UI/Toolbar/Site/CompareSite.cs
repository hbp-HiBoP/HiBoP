using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class CompareSite : Tool
    {
        #region Properties
        /// <summary>
        /// Toggle the compare sites mode (after having selected a site, allows the selection of another site)
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
                SelectedScene.ImplantationManager.ComparingSites = isOn;
                UpdateInteractable();
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
            bool isSiteSelected = SelectedColumn.SelectedSite != null;
            bool isComparingSites = SelectedScene.ImplantationManager.ComparingSites;

            m_Toggle.interactable = isSiteSelected || isComparingSites;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.ImplantationManager.ComparingSites;
        }
        #endregion
    }
}