using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.UI.Tools.ResizableGrids;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField] private Tooltip m_SiteTooltip;
        [SerializeField] private Tooltip m_PatientsTooltip;
        [SerializeField] private Tooltip m_LabelsTooltip;
        [SerializeField] private Tooltip m_HighlightedTooltip;
        [SerializeField] private Tooltip m_BlacklistedTooltip;
        [SerializeField] private Tooltip m_ColorTooltip;

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
            m_SiteTooltip.OnBeforeDisplayTooltip.AddListener(() =>
            {
                m_SiteTooltip.Text = string.Format("Number of sites: {0}", m_SiteList.Objects.Count);
            });
            m_PatientsTooltip.OnBeforeDisplayTooltip.AddListener(() =>
            {
                m_PatientsTooltip.Text = string.Format("Number of distinct patients: {0}", m_SiteList.Objects.Select(s => s.Information.Patient).Distinct().Count());
            });
            m_LabelsTooltip.OnBeforeDisplayTooltip.AddListener(() =>
            {
                string labelsTooltip = "Number of sites with";
                Dictionary<int, int> countByNumberOfLabels = new Dictionary<int, int>();
                foreach (var site in m_SiteList.Objects)
                {
                    if (!countByNumberOfLabels.ContainsKey(site.State.Labels.Count))
                    {
                        countByNumberOfLabels.Add(site.State.Labels.Count, 1);
                    }
                    else
                    {
                        countByNumberOfLabels[site.State.Labels.Count]++;
                    }
                }
                foreach (var kv in countByNumberOfLabels)
                {
                    if (kv.Key == 1)
                        labelsTooltip += string.Format("\n{0} label: {1}", kv.Key, kv.Value);
                    else
                        labelsTooltip += string.Format("\n{0} labels: {1}", kv.Key, kv.Value);
                }
                m_LabelsTooltip.Text = labelsTooltip;
            });
            m_HighlightedTooltip.OnBeforeDisplayTooltip.AddListener(() =>
            {
                m_HighlightedTooltip.Text = string.Format("Number of highlighted sites: {0}", m_SiteList.Objects.Count(s => s.State.IsHighlighted));
            });
            m_BlacklistedTooltip.OnBeforeDisplayTooltip.AddListener(() =>
            {
                m_BlacklistedTooltip.Text = string.Format("Number of blacklisted sites: {0}", m_SiteList.Objects.Count(s => s.State.IsBlackListed));
            });
            m_ColorTooltip.OnBeforeDisplayTooltip.AddListener(() =>
            {
                m_ColorTooltip.Text = string.Format("Number of distinct colors: {0}", m_SiteList.Objects.Select(s => s.State.Color).Distinct().Count());
            });
            foreach (var column in m_Scene.Columns)
            {
                column.OnSelect.AddListener(SetList);
            }
        }
        #endregion
    }
}