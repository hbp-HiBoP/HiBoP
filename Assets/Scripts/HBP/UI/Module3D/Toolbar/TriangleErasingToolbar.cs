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
        [SerializeField]
        private Tools.TriangleErasingMode m_TriangleErasingMode;

        [SerializeField]
        private Tools.ExpandErasing m_ExpandErasing;

        [SerializeField]
        private Tools.InvertErasing m_InvertErasing;

        [SerializeField]
        private Tools.CancelErasing m_CancelErasing;

        [SerializeField]
        private Tools.ResetErasing m_ResetErasing;
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