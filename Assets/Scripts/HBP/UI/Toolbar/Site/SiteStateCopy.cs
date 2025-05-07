using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class SiteStateCopy : Tool
    {
        #region Properties
        /// <summary>
        /// Copy all states of the selected column to all columns
        /// </summary>
        [SerializeField] private Button m_Button;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Button.onClick.AddListener(async () =>
            {
                if (ListenerLock) return;

                var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Override Sites Attributes", "The attributes of all sites will be overridden by the attributes of the selected column. Do you want to continue?\n\nReminder: a site's attributes consist of its blacklisted status, highlighted status, color and labels.", "Override", "Cancel");
                if (result == 0)
                    SelectedScene.ApplySelectedColumnSiteStatesToOtherColumns();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
        }
        #endregion
    }
}