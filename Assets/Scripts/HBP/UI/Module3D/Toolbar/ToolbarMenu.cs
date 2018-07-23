using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class ToolbarMenu : MonoBehaviour
    {
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
        private CCEPToolbar m_CCEPToolbar;
        /// <summary>
        /// Toolbar for the sites settings
        /// </summary>
        public CCEPToolbar CCEPToolbar
        {
            get
            {
                return m_CCEPToolbar;
            }
            set
            {
                m_CCEPToolbar = value;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private FMRIToolbar m_FMRIToolbar;
        /// <summary>
        /// Toolbar for the sites settings
        /// </summary>
        public FMRIToolbar FMRIToolbar
        {
            get
            {
                return m_FMRIToolbar;
            }
            set
            {
                m_FMRIToolbar = value;
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

        private bool m_UpdateRequired;
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
        }
        private void Update()
        {
            if (m_UpdateRequired)
            {
                m_SceneSettingsToolbar.UpdateToolbarState();
                m_DisplaySettingsToolbar.UpdateToolbarState();
                m_IEEGSettingsToolbar.UpdateToolbarState();
                m_TimelineToolbar.UpdateToolbarState();
                m_SiteToolbar.UpdateToolbarState();
                m_ROIToolbar.UpdateToolbarState();
                m_TriangleToolbar.UpdateToolbarState();
                m_ConfigurationToolbar.UpdateToolbarState();
                m_CCEPToolbar.UpdateToolbarState();
                m_FMRIToolbar.UpdateToolbarState();
                m_UpdateRequired = false;
            }
        }
        /// <summary>
        /// Initialize the toolbar menu
        /// </summary>
        private void Initialize()
        {
            ApplicationState.Module3D.OnRequestUpdateInToolbar.AddListener(() =>
            {
                m_UpdateRequired = true;
            });
            ApplicationState.Module3D.OnFinishedAddingNewScenes.AddListener(() =>
            {
                CurrentToolbar.ShowToolbarCallback();
            });

            m_SceneSettingsToolbar.Initialize();
            m_DisplaySettingsToolbar.Initialize();
            m_IEEGSettingsToolbar.Initialize();
            m_TimelineToolbar.Initialize();
            m_SiteToolbar.Initialize();
            m_ROIToolbar.Initialize();
            m_TriangleToolbar.Initialize();
            m_ConfigurationToolbar.Initialize();
            m_CCEPToolbar.Initialize();
            m_FMRIToolbar.Initialize();

            CurrentToolbar = m_SceneSettingsToolbar;

            m_SceneSettingsToolbar.gameObject.SetActive(true);
            m_DisplaySettingsToolbar.gameObject.SetActive(false);
            m_IEEGSettingsToolbar.gameObject.SetActive(false);
            m_TimelineToolbar.gameObject.SetActive(false);
            m_SiteToolbar.gameObject.SetActive(false);
            m_ROIToolbar.gameObject.SetActive(false);
            m_TriangleToolbar.gameObject.SetActive(false);
            m_ConfigurationToolbar.gameObject.SetActive(false);
            m_CCEPToolbar.gameObject.SetActive(false);
            m_FMRIToolbar.gameObject.SetActive(false);
        }
        #endregion
    }
}