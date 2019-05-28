using CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteFilters : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;

        [SerializeField] private BasicSiteConditions m_BasicSiteConditions;
        [SerializeField] private AdvancedSiteConditions m_AdvancedSiteConditions;
        [SerializeField] private SiteConditionsProgressBar m_ProgressBar;

        [SerializeField] private Toggle m_OnOffButton;
        [SerializeField] private GameObject m_FiltersPanel;
        [SerializeField] private Toggle m_BasicToggle;
        [SerializeField] private Toggle m_AdvancedToggle;
        [SerializeField] private Button m_ApplyButton;
        [SerializeField] private Button m_ResetButton;

        private Coroutine m_Coroutine;
        #endregion

        #region Events
        public GenericEvent<float> OnFilter = new GenericEvent<float>();
        public GenericEvent<bool> OnEndFilter = new GenericEvent<bool>();
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_BasicSiteConditions.Initialize(scene);
            m_AdvancedSiteConditions.Initialize(scene);
        }
        public void ApplyFilters()
        {
            foreach (var column in m_Scene.ColumnManager.Columns)
            {
                foreach (var site in column.Sites)
                {
                    site.State.IsFiltered = false;
                }
            }

            List<Site> sites = new List<Site>();
            foreach (var column in m_Scene.ColumnManager.Columns)
            {
                sites.AddRange(column.Sites);
            }

            m_ProgressBar.Begin();
            try
            {
                if (m_AdvancedToggle.isOn)
                {
                    m_AdvancedSiteConditions.ParseConditions();
                    m_Coroutine = this.StartCoroutineAsync(m_AdvancedSiteConditions.c_FilterSitesWithConditions(sites, OnFilter, OnEndFilter));
                }
                else
                {
                    m_Coroutine = this.StartCoroutineAsync(m_BasicSiteConditions.c_FilterSitesWithConditions(sites, OnFilter, OnEndFilter));
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, e.ToString(), e.Message);
            }
        }
        public void ResetFilters()
        {
            foreach (var column in m_Scene.ColumnManager.Columns)
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
            m_OnOffButton.onValueChanged.AddListener((isOn) =>
            {
                m_FiltersPanel.gameObject.SetActive(isOn);
            });
            m_BasicToggle.onValueChanged.AddListener((isOn) =>
            {
                m_BasicSiteConditions.gameObject.SetActive(isOn);
            });
            m_AdvancedToggle.onValueChanged.AddListener((isOn) =>
            {
                m_AdvancedSiteConditions.gameObject.SetActive(isOn);
            });
            m_ApplyButton.onClick.AddListener(() =>
            {
                if (m_Coroutine != null)
                {
                    StopCoroutine(m_Coroutine);
                    OnEndFilter.Invoke(false);
                }
                else
                {
                    ApplyFilters();
                }
            });
            m_ResetButton.onClick.AddListener(() =>
            {
                if (m_Coroutine != null)
                {
                    StopCoroutine(m_Coroutine);
                    OnEndFilter.Invoke(false);
                }
                else
                {
                    ResetFilters();
                    OnEndFilter.Invoke(true);
                }
            });
            OnFilter.AddListener((progress) =>
            {
                m_ProgressBar.Progress(progress);
            });
            OnEndFilter.AddListener((finished) =>
            {
                m_Coroutine = null;
                m_ProgressBar.End();
                if (!finished) ResetFilters();
            });
        }
        #endregion
    }
}