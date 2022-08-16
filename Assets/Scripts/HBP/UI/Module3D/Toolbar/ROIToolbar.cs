using HBP.Display.Module3D;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class ROIToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Tool to manage the Regions of Interest
        /// </summary>
        [SerializeField] private Tools.ROIManager m_ROIManager;
        /// <summary>
        /// Tool to import/export ROIs
        /// </summary>
        [SerializeField] private Tools.ROIExport m_ROIExport;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_ROIManager);
            m_Tools.Add(m_ROIExport);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Called when showing this toolbar
        /// </summary>
        public override void ShowToolbarCallback()
        {
            foreach (Base3DScene scene in HBP3DModule.Scenes)
            {
                scene.ROIManager.ROICreationMode = true;
            }
        }
        /// <summary>
        /// Called when hiding this toolbar
        /// </summary>
        public override void HideToolbarCallback()
        {
            foreach (Base3DScene scene in HBP3DModule.Scenes)
            {
                scene.ROIManager.ROICreationMode = false;
            }
        }
        #endregion
    }
}