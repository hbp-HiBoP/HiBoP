using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D {
    public class ToolbarMenu : MonoBehaviour {
        
        #region Properties
        [SerializeField, Candlelight.PropertyBackingField]
        private SceneSettingsToolbar m_SceneSettingsToolbar;
        /// <summary>
        /// Toolbar for the scene settings
        /// </summary>
        public SceneSettingsToolbar SceneSettingsToolbar
        {
            get
            {
                return m_SceneSettingsToolbar;
            }
            set
            {
                m_SceneSettingsToolbar = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private DisplaySettingsToolbar m_DisplaySettingsToolbar;
        /// <summary>
        /// Toolbar for the scene settings
        /// </summary>
        public DisplaySettingsToolbar DisplaySettingsToolbar
        {
            get
            {
                return m_DisplaySettingsToolbar;
            }
            set
            {
                m_DisplaySettingsToolbar = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private IEEGSettingsToolbar m_IEEGSettingsToolbar;
        /// <summary>
        /// Toolbar for the scene settings
        /// </summary>
        public IEEGSettingsToolbar IEEGSettingsToolbar
        {
            get
            {
                return m_IEEGSettingsToolbar;
            }
            set
            {
                m_IEEGSettingsToolbar = value;
            }
        }

        /// <summary>
        /// Currently used toolbar
        /// </summary>
        public Toolbar CurrentToolbar { get; set; }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
        }
        /// <summary>
        /// Initialize the toolbar menu
        /// </summary>
        private void Initialize()
        {
            CurrentToolbar = m_SceneSettingsToolbar;

            m_SceneSettingsToolbar.gameObject.SetActive(true);
            m_DisplaySettingsToolbar.gameObject.SetActive(false);
            m_IEEGSettingsToolbar.gameObject.SetActive(false);
        }
        #endregion
    }
}