using HBP.Module3D;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class TriangleErasingToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Change the triangle erasing mode (triangle, area, cylinder) and the parameters
        /// </summary>
        [SerializeField] private Tools.TriangleErasingMode m_TriangleErasingMode;
        /// <summary>
        /// Expand the erased area
        /// </summary>
        [SerializeField] private Tools.ExpandErasing m_ExpandErasing;
        /// <summary>
        /// Invert the erased area
        /// </summary>
        [SerializeField] private Tools.InvertErasing m_InvertErasing;
        /// <summary>
        /// Cancel the last erasing action
        /// </summary>
        [SerializeField] private Tools.CancelErasing m_CancelErasing;
        /// <summary>
        /// Reset the erasing area
        /// </summary>
        [SerializeField] private Tools.ResetErasing m_ResetErasing;
        /// <summary>
        /// Allows to save and load triangle erasing masks
        /// </summary>
        [SerializeField] private Tools.TriangleErasingLoaderSaver m_TriangleErasingLoaderSaver;
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
            foreach (HBP.Module3D.Base3DScene scene in HBP3DModule.Scenes)
            {
                scene.TriangleEraser.IsEnabled = true;
            }
        }
        /// <summary>
        /// Called when hiding this toolbar
        /// </summary>
        public override void HideToolbarCallback()
        {
            foreach (HBP.Module3D.Base3DScene scene in HBP3DModule.Scenes)
            {
                scene.TriangleEraser.IsEnabled = false;
            }
        }
        #endregion
    }
}