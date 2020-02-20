using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ResetViews : Tool
    {
        #region Properties
        /// <summary>
        /// Button to reset the positions of the views
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

                SelectedScene.OnResetViewPositions.Invoke();
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