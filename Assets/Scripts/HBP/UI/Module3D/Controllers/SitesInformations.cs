using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SitesInformations : MonoBehaviour
    {
        #region Properties
        private const float MINIMIZED_THRESHOLD = 260.0f;
        private Base3DScene m_Scene;
        private RectTransform m_RectTransform;
        private ResizableGrid m_ParentGrid;
        [SerializeField] private SiteList m_SiteList;
        [SerializeField] private Toggle m_SiteFiltersToggle;
        [SerializeField] private SiteFilters m_SiteFilters;
        [SerializeField] private Toggle m_SiteActionsToggle;
        [SerializeField] private SiteActions m_SiteActions;
        [SerializeField] private GameObject m_MinimizedGameObject;
        private bool m_RectTransformChanged;

        private string m_Name;
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
                UpdateList();
            }
        }

        private string m_Label;
        public string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                m_Label = value;
                UpdateList();
            }
        }

        private string m_Patient;
        public string Patient
        {
            get
            {
                return m_Patient;
            }
            set
            {
                m_Patient = value;
                UpdateList();
            }
        }

        private bool m_Blacklisted;
        public bool Blacklisted
        {
            get
            {
                return m_Blacklisted;
            }
            set
            {
                m_Blacklisted = value;
                UpdateList();
            }
        }

        private bool m_Highlighted;
        public bool Highlighted
        {
            get
            {
                return m_Highlighted;
            }
            set
            {
                m_Highlighted = value;
                UpdateList();
            }
        }

        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - m_ParentGrid.MinimumViewWidth) <= MINIMIZED_THRESHOLD;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_ParentGrid = GetComponentInParent<ResizableGrid>();
        }
        private void Update()
        {
            if (m_RectTransform.hasChanged)
            {
                m_MinimizedGameObject.SetActive(IsMinimized);
                m_RectTransform.hasChanged = false;
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_SiteList.Initialize();
            m_SiteFilters.Initialize(scene);
            m_SiteFilters.OnRequestListUpdate.AddListener(UpdateList);
            m_SiteFiltersToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    m_SiteActionsToggle.isOn = false;
                }
            });
            m_SiteActions.Initialize(scene);
            m_SiteActions.OnRequestListUpdate.AddListener(UpdateList);
            m_SiteActionsToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    m_SiteFiltersToggle.isOn = false;
                }
            });
            m_Scene.OnUpdateSites.AddListener(UpdateList);
            m_Scene.OnSelect.AddListener(() => UpdateList());
            m_Scene.OnSitesRenderingUpdated.AddListener(() =>
            {
                UpdateList();
                m_SiteList.ScrollToObject(m_Scene.SelectedColumn.SelectedSite);
            });
        }
        public void UpdateList()
        {
            List<Site> sites = m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered && !s.State.IsMasked).ToList();
            if (!string.IsNullOrEmpty(m_Name))
            {
                sites.RemoveAll(s => !s.Information.ChannelName.ToUpper().Contains(m_Name.ToUpper()));
            }
            if (!string.IsNullOrEmpty(m_Label))
            {
                sites.RemoveAll(s => !s.State.Labels.Any(l => l.ToLower().Contains(m_Label.ToLower())));
            }
            if (!string.IsNullOrEmpty(m_Patient))
            {
                sites.RemoveAll(s => !s.Information.Patient.Name.ToUpper().Contains(m_Patient.ToUpper()));
            }
            if (m_Blacklisted)
            {
                sites.RemoveAll(s => !s.State.IsBlackListed);
            }
            if (m_Highlighted)
            {
                sites.RemoveAll(s => !s.State.IsHighlighted);
            }
            m_SiteList.ObjectsList = sites;
        }
        #endregion
    }
}