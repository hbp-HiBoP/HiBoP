using HBP.UI.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class TriangleErasingToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Change IEEG colormap
        /// </summary>
        [SerializeField]
        private Tools.TriangleErasingMode m_TriangleErasingMode;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_TriangleErasingMode);
        }
        public override void ShowToolbarCallback()
        {
            foreach (HBP.Module3D.Base3DScene scene in ApplicationState.Module3D.ScenesManager.Scenes)
            {
                scene.IsTriangleErasingEnabled = true;
            }
        }
        public override void HideToolbarCallback()
        {
            foreach (HBP.Module3D.Base3DScene scene in ApplicationState.Module3D.ScenesManager.Scenes)
            {
                scene.IsTriangleErasingEnabled = false;
            }
        }
        #endregion
    }
}