using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ExpandErasing : Tool
    {
        #region Properties
        /// <summary>
        /// Expand the erased area
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

                SelectedScene.TriangleEraser.CurrentMode = Data.Enums.TriEraserMode.Expand;
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