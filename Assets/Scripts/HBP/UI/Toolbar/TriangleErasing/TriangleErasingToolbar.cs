using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.UI.Toolbar
{
    public class TriangleErasingToolbar : Toolbar
    {
        private bool m_Open;

        protected override void AddListeners()
        {
            base.AddListeners();
            Module3DMain.OnSelectScene.AddListener(UpdateScene);
        }

        private void OnDestroy() => Module3DMain.OnSelectScene.RemoveListener(UpdateScene);

        private void UpdateScene(Base3DScene selected)
        {
            foreach (Base3DScene scene in Module3DMain.Scenes)
                scene.TriangleEraser.IsEnabled = m_Open && scene == selected;
        }

        #region Properties

        /// <summary>
        /// Change the triangle erasing mode (triangle, area, cylinder) and the parameters
        /// </summary>
        [SerializeField] private TriangleErasingMode m_TriangleErasingMode;

        /// <summary>
        /// Expand the erased area
        /// </summary>
        [SerializeField] private ExpandErasing m_ExpandErasing;

        /// <summary>
        /// Invert the erased area
        /// </summary>
        [SerializeField] private InvertErasing m_InvertErasing;

        /// <summary>
        /// Cancel the last erasing action
        /// </summary>
        [SerializeField] private CancelErasing m_CancelErasing;

        /// <summary>
        /// Reset the erasing area
        /// </summary>
        [SerializeField] private ResetErasing m_ResetErasing;

        /// <summary>
        /// Allows to save and load triangle erasing masks
        /// </summary>
        [SerializeField] private TriangleErasingLoaderSaver m_TriangleErasingLoaderSaver;

        #endregion

        #region Private Methods

        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_TriangleErasingMode);
            m_Tools.Add(m_ExpandErasing);
            m_Tools.Add(m_InvertErasing);
            m_Tools.Add(m_CancelErasing);
            m_Tools.Add(m_ResetErasing);
            m_Tools.Add(m_TriangleErasingLoaderSaver);
        }

        /// <summary>
        /// Called when showing this toolbar
        /// </summary>
        public override void ShowToolbarCallback()
        {
            m_Open = true;
            UpdateScene(Module3DMain.SelectedScene);
        }

        /// <summary>
        /// Called when hiding this toolbar
        /// </summary>
        public override void HideToolbarCallback()
        {
            m_Open = false;
            UpdateScene(null);
        }

        #endregion
    }
}
