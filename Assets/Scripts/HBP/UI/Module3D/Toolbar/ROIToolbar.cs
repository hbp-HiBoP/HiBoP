using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class ROIToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Tool to manage the Regions of Interest
        /// </summary>
        [SerializeField]
        private Tools.ROIManager m_ROIManager;
        /// <summary>
        /// Tool to copy all ROIs from a column to every others
        /// </summary>
        [SerializeField]
        private Tools.ROICopy m_ROICopy;
        /// <summary>
        /// Tool to import/export ROIs
        /// </summary>
        [SerializeField]
        private Tools.ROIExport m_ROIExport;
        /// <summary>
        /// Export sites to a csv file
        /// </summary>
        [SerializeField]
        private Tools.ExportSites m_ExportSites;
        #endregion

        #region Private Methods
        protected override void AddTools()
        {
            m_Tools.Add(m_ROIManager);
            m_Tools.Add(m_ROICopy);
            m_Tools.Add(m_ROIExport);
            m_Tools.Add(m_ExportSites);
        }
        #endregion

        #region Public Methods
        public override void ShowToolbarCallback()
        {
            foreach (HBP.Module3D.Base3DScene scene in ApplicationState.Module3D.Scenes)
            {
                scene.ROICreation = true;
            }
        }
        public override void HideToolbarCallback()
        {
            foreach (HBP.Module3D.Base3DScene scene in ApplicationState.Module3D.Scenes)
            {
                scene.ROICreation = false;
            }
        }
        #endregion
    }
}