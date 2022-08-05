using HBP.Module3D;
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
        private void SetList()
        {
            m_SiteList.ObjectsList = m_Scene.SelectedColumn.Sites.ToList();
            m_SiteList.MaskList(m_Scene.SelectedColumn.Sites.Select(s => s.State.IsFiltered && !s.State.IsMasked).ToArray());
        }
        private void UpdateList()
        {
            m_SiteList.MaskList(m_Scene.SelectedColumn.Sites.Select(s => s.State.IsFiltered && !s.State.IsMasked).ToArray());
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
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
            m_Scene.OnSelect.AddListener(SetList);
            m_Scene.OnSitesRenderingUpdated.AddListener(SetList);
            m_Scene.OnSelectSite.AddListener((s) =>
            {
                UpdateList();
                m_SiteList.ScrollToObject(s);
            });
            foreach (var column in m_Scene.Columns)
            {
                column.OnSelect.AddListener(SetList);
            }
        }
        #endregion
    }
}