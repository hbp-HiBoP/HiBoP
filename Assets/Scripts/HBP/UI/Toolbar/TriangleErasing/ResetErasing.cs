using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class ResetErasing : Tool
    {
        #region Properties
        /// <summary>
        /// Reset the erasing area
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

                SelectedScene.TriangleEraser.ResetEraser();
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
            bool haveTrianglesBeenErased = SelectedScene.TriangleEraser.MeshHasInvisibleTriangles;

            m_Button.interactable = haveTrianglesBeenErased;
        }
        #endregion
    }
}