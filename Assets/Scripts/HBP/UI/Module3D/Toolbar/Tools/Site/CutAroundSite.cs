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
        [SerializeField] private Button m_Button;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                SelectedScene.CutAroundSelectedSite();
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
            bool isInteractable = SelectedColumn.SelectedSite != null;

            m_Button.interactable = isInteractable;
        }
        #endregion
    }
}