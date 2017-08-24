using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D {
    public class ToolbarMenu : MonoBehaviour {

        #region Properties
        [SerializeField, Candlelight.PropertyBackingField]
        private ConfigurationToolbar m_ConfigurationToolbar;
        /// <summary>
        /// Toolbar for the scene settings
        /// </summary>
        public ConfigurationToolbar ConfigurationToolbar
        {
            get
            {
                return m_ConfigurationToolbar;
            }
            set
            {
                m_ConfigurationToolbar = value;
            }
        }

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
        /// Toolbar for the display settings
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
        /// Toolbar for the IEEG settings
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

        [SerializeField, Candlelight.PropertyBackingField]
        private TimelineToolbar m_TimelineToolbar;
        /// <summary>
        /// Toolbar for the Timeline control
        /// </summary>
        public TimelineToolbar TimelineToolbar
        {
            get
            {
                return m_TimelineToolbar;
            }
            set
            {
                m_TimelineToolbar = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private SiteToolbar m_SiteToolbar;
        /// <summary>
        /// Toolbar for the sites settings
        /// </summary>
        public SiteToolbar SiteToolbar
        {
            get
            {
                return m_SiteToolbar;
            }
            set
            {
                m_SiteToolbar = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private ROIToolbar m_ROIToolbar;
        /// <summary>
        /// Toolbar for the regions of interest settings
        /// </summary>
        public ROIToolbar ROIToolbar
        {
            get
            {
                return m_ROIToolbar;
            }
            set
            {
                m_ROIToolbar = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private TriangleErasingToolbar m_TriangleToolbar;
        /// <summary>
        /// Toolbar to erase triangles
        /// </summary>
        public TriangleErasingToolbar TriangleToolbar
        {
            get
            {
                return m_TriangleToolbar;
            }
            set
            {
                m_TriangleToolbar = value;
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
            m_SceneSettingsToolbar.Initialize();
            m_DisplaySettingsToolbar.Initialize();
            m_IEEGSettingsToolbar.Initialize();
            m_TimelineToolbar.Initialize();
            m_SiteToolbar.Initialize();
            m_ROIToolbar.Initialize();
            m_TriangleToolbar.Initialize();
            m_ConfigurationToolbar.Initialize();

            CurrentToolbar = m_SceneSettingsToolbar;

            m_SceneSettingsToolbar.gameObject.SetActive(true);
            m_DisplaySettingsToolbar.gameObject.SetActive(false);
            m_IEEGSettingsToolbar.gameObject.SetActive(false);
            m_TimelineToolbar.gameObject.SetActive(false);
            m_SiteToolbar.gameObject.SetActive(false);
            m_ROIToolbar.gameObject.SetActive(false);
            m_TriangleToolbar.gameObject.SetActive(false);
            m_ConfigurationToolbar.gameObject.SetActive(false);
        }
        #endregion
    }
}