using ThirdParty.CielaSpike;
using HBP.Module3D;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Tools.Unity;

namespace HBP.UI.Module3D
{
    public class SiteFilters : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;

        [SerializeField] private BasicSiteConditions m_BasicSiteConditions;
        [SerializeField] private AdvancedSiteConditions m_AdvancedSiteConditions;
        [SerializeField] private SiteConditionsProgressBar m_ProgressBar;

        [SerializeField] private Toggle m_OnOffToggle;
        [SerializeField] private GameObject m_FiltersPanel;
        [SerializeField] private Toggle m_BasicToggle;
        [SerializeField] private Toggle m_AdvancedToggle;
        [SerializeField] private Button m_ApplyButton;
        [SerializeField] private Button m_ResetButton;

        private Coroutine m_Coroutine;
        #endregion

        #region Events
        public UnityEvent OnRequestListUpdate = new UnityEvent();
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_BasicSiteConditions.Initialize(scene);
            m_BasicSiteConditions.OnEndFilter.AddListener(StopFiltering);
            m_BasicSiteConditions.OnFilter.AddListener(m_ProgressBar.Progress);
            m_AdvancedSiteConditions.Initialize(scene);
            m_AdvancedSiteConditions.OnEndFilter.AddListener(StopFiltering);
            m_AdvancedSiteConditions.OnFilter.AddListener(m_ProgressBar.Progress);
        }
        public void ApplyFilters()
        {
            foreach (var column in m_Scene.Columns)
            {
                foreach (var site in column.Sites)
                {
                    site.State.IsFiltered = false;
                }
            }

            List<Core.Object3D.Site> sites = new List<Core.Object3D.Site>();
            foreach (var column in m_Scene.Columns)
            {
                sites.AddRange(column.Sites);
            }

            m_ProgressBar.Begin();
            try
            {
                if (m_AdvancedToggle.isOn)
                {
                    m_AdvancedSiteConditions.ParseConditions();
                    m_Coroutine = this.StartCoroutineAsync(m_AdvancedSiteConditions.c_FilterSitesWithConditions(sites));
                }
                else
                {
                    m_Coroutine = this.StartCoroutineAsync(m_BasicSiteConditions.c_FilterSitesWithConditions(sites));
                }
            }
            catch (Exception e)
            {
                StopFiltering(false);
                DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, e.ToString(), e.Message);
            }
        }
        public void ResetFilters()
        {
            foreach (var column in m_Scene.Columns)
            {
                foreach (var site in column.Sites)
                {
                    site.State.IsFiltered = true;
                }
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_OnOffToggle.onValueChanged.AddListener(m_FiltersPanel.gameObject.SetActive);
            m_BasicToggle.onValueChanged.AddListener(m_BasicSiteConditions.gameObject.SetActive);
            m_AdvancedToggle.onValueChanged.AddListener(m_AdvancedSiteConditions.gameObject.SetActive);
            m_ApplyButton.onClick.AddListener(ApplyButtonClicked);
            m_ResetButton.onClick.AddListener(ResetButtonClicked);
        }
        private void StopFiltering(bool filterCompleted)
        {
            m_Coroutine = null;
            m_ProgressBar.End();
            if (!filterCompleted) ResetFilters();
            OnRequestListUpdate.Invoke();
        }
        private void ApplyButtonClicked()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                StopFiltering(false);
            }
            else
            {
                ApplyFilters();
            }
        }
        private void ResetButtonClicked()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                StopFiltering(false);
            }
            else
            {
                ResetFilters();
                StopFiltering(true);
            }
        }
        #endregion
    }
}