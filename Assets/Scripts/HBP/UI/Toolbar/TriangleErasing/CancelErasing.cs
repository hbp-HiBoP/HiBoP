using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class CancelErasing : Tool
    {
        #region Properties
        /// <summary>
        /// Cancel the last erasing action
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
                if (ListenerLock) return;

                SelectedScene.TriangleEraser.CancelLastAction();
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
            bool isCancelAvailable = SelectedScene.TriangleEraser.CanCancelLastAction;

            m_Button.interactable = isCancelAvailable;
        }
        #endregion
    }
}