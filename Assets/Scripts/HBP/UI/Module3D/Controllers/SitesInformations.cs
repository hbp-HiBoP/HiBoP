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
        private Base3DScene m_Scene;
        private RectTransform m_RectTransform;
        private ResizableGrid m_ParentGrid;
        [SerializeField] private SiteList m_SiteList;
        [SerializeField] private SiteConditions m_SiteConditions;
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

        private bool m_Excluded;
        public bool Excluded
        {
            get
            {
                return m_Excluded;
            }
            set
            {
                m_Excluded = value;
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

        private bool m_Marked;
        public bool Marked
        {
            get
            {
                return m_Marked;
            }
            set
            {
                m_Marked = value;
                UpdateList();
            }
        }

        private bool m_Suspicious;
        public bool Suspicious
        {
            get
            {
                return m_Suspicious;
            }
            set
            {
                m_Suspicious = value;
                UpdateList();
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
            if (m_RectTransformChanged)
            {
                if (GetComponent<RectTransform>().rect.width < 3 * m_ParentGrid.MinimumViewWidth)
                {
                    m_MinimizedGameObject.SetActive(true);
                }
                else
                {
                    m_MinimizedGameObject.SetActive(false);
                }
                m_RectTransformChanged = false;
            }
        }
        #endregion

        #region Public Methods
        public void OnRectTransformDimensionsChange()
        {
            m_RectTransformChanged = true;
        }
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_SiteList.Initialize();
            UpdateList();
            m_SiteConditions.Initialize(scene);
            m_Scene.ColumnManager.OnSelectColumn.AddListener((c) => UpdateList());
            m_Scene.ColumnManager.OnSelectSite.AddListener(s =>
            {
                m_SiteList.Refresh();
            });
        }
        public void UpdateList()
        {
            List<Site> sites = m_Scene.ColumnManager.SelectedColumn.Sites.ToList();
            if (!string.IsNullOrEmpty(m_Name))
            {
                sites.RemoveAll(s => !s.Information.ChannelName.ToUpper().Contains(m_Name.ToUpper()));
            }
            if (!string.IsNullOrEmpty(m_Patient))
            {
                sites.RemoveAll(s => !s.Information.Patient.Name.ToUpper().Contains(m_Patient.ToUpper()));
            }
            if (m_Excluded)
            {
                sites.RemoveAll(s => !s.State.IsExcluded);
            }
            if (m_Blacklisted)
            {
                sites.RemoveAll(s => !s.State.IsBlackListed);
            }
            if (m_Highlighted)
            {
                sites.RemoveAll(s => !s.State.IsHighlighted);
            }
            if (m_Marked)
            {
                sites.RemoveAll(s => !s.State.IsMarked);
            }
            if (m_Suspicious)
            {
                sites.RemoveAll(s => !s.State.IsSuspicious);
            }
            m_SiteList.ObjectsList = sites;
        }
        #endregion
    }
}