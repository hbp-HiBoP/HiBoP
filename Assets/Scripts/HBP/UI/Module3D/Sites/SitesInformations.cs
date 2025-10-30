using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Preferences;
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
        [SerializeField] private Button m_SiteFiltersButton;
        [SerializeField] private Button m_SiteToolsButton;
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
            Module3DMain.OnRequestUpdateInSiteList.AddListener(UpdateList);
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
        private void OpenSiteTools()
        {
            var siteTools = WindowsManager.Open("Site Tools window", null).GetComponent<SiteToolsWindow>();
            siteTools.Scene = m_Scene;
            siteTools.OnToolApplied.AddListener(UpdateList);
        }
        private void OpenSiteFilters()
        {
            var siteFilters = WindowsManager.Open("Site Filters window", null).GetComponent<SiteFiltersWindow>();
            siteFilters.FilteringObjects = m_Scene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).Select(s => (object)s).ToList();
            siteFilters.SetPreset(PersistentDataManager.FilterConditionsPresets.GetCurrentPreset(typeof(Site)));
            siteFilters.OnApplyFilters.AddListener(mask =>
            {
                for (int i = 0; i < mask.Length; i++) ((Site)siteFilters.FilteringObjects[i]).State.IsFiltered = mask[i];
                UpdateList();
            });
        }
        private void OnSelectSite(Core.Object3D.Site site)
        {
            UpdateList();
            m_SiteList.ScrollToObject(site);
        }

        private void CountSites()
        {
            m_SiteTooltip.Text = string.Format("Number of sites: {0}", m_SiteList.Objects.Count);
        }
        private void CountPatients()
        {
            m_PatientsTooltip.Text = string.Format("Number of distinct patients: {0}", m_SiteList.Objects.Select(s => s.Information.Patient).Distinct().Count());
        }
        private void CountLabels()
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
        }
        private void CountHighlighted()
        {
            m_HighlightedTooltip.Text = string.Format("Number of highlighted sites: {0}", m_SiteList.Objects.Count(s => s.State.IsHighlighted));
        }
        private void CountBlacklisted()
        {
            m_BlacklistedTooltip.Text = string.Format("Number of blacklisted sites: {0}", m_SiteList.Objects.Count(s => s.State.IsBlackListed));
        }
        private void CountColors()
        {
            m_ColorTooltip.Text = string.Format("Number of distinct colors: {0}", m_SiteList.Objects.Select(s => s.State.Color).Distinct().Count());
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;

            m_SiteFiltersButton.onClick.AddListener(OpenSiteFilters);
            m_SiteToolsButton.onClick.AddListener(OpenSiteTools);

            m_Scene.OnSelect.AddListener(SetList);
            m_Scene.OnSitesRenderingUpdated.AddListener(SetList);
            m_Scene.OnSelectSite.AddListener(OnSelectSite);

            m_SiteTooltip.OnBeforeDisplayTooltip.AddListener(CountSites);
            m_PatientsTooltip.OnBeforeDisplayTooltip.AddListener(CountPatients);
            m_LabelsTooltip.OnBeforeDisplayTooltip.AddListener(CountLabels);
            m_HighlightedTooltip.OnBeforeDisplayTooltip.AddListener(CountHighlighted);
            m_BlacklistedTooltip.OnBeforeDisplayTooltip.AddListener(CountBlacklisted);
            m_ColorTooltip.OnBeforeDisplayTooltip.AddListener(CountColors);

            foreach (var column in m_Scene.Columns)
            {
                column.OnSelect.AddListener(SetList);
            }
        }
        #endregion
    }
}