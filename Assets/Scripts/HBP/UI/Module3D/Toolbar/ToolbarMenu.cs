using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D {
    public class ToolbarMenu : MonoBehaviour {

        enum ToolBarState { SceneSettings, DisplaySettings, IEEGSettings, FMRISettings, SiteSettings, TriangleErasing, RegionOfInterest }

        #region Properties
        /// <summary>
        /// Toolbar for the scene settings
        /// </summary>
        private SceneSettingsToolbar m_SceneSettingsToolbar;

        private Toolbar m_CurrentToolbar;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_SceneSettingsToolbar = transform.Find("Scene Settings").GetComponent<SceneSettingsToolbar>();
        }
        #endregion

        #region Public Methods

        #endregion
    }
}