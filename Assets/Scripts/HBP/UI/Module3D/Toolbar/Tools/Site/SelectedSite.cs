using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SelectedSite : Tool
    {
        #region Properties
        /// <summary>
        /// Text to display the selected site
        /// </summary>
        [SerializeField] private Text m_Text;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Text.text = "No site selected";
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {

        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            Site site = SelectedColumn.SelectedSite;
            m_Text.text = site ? site.Information.DisplayedName : "No site selected";
        }
        #endregion
    }
}