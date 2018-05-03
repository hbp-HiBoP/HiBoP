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
    public class SiteConditions : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        [SerializeField] BasicSiteConditions m_BasicSiteConditions;
        [SerializeField] AdvancedSiteConditions m_AdvancedSiteConditions;
        Coroutine m_Coroutine;

        public bool UseAdvanced { get; set; }
        public bool AllColumns { get; set; }

        [SerializeField] Toggle m_Exclude;
        [SerializeField] Toggle m_Unexclude;
        [SerializeField] Toggle m_Highlight;
        [SerializeField] Toggle m_Unhighlight;
        [SerializeField] Toggle m_Blacklist;
        [SerializeField] Toggle m_Unblacklist;
        [SerializeField] Toggle m_Mark;
        [SerializeField] Toggle m_Unmark;
        [SerializeField] Toggle m_Suspect;
        [SerializeField] Toggle m_Unsuspect;
        #endregion

        #region Events
        public UnityEvent OnBeginApply = new UnityEvent();
        public ApplyingActionEvent OnApplyingActions = new ApplyingActionEvent();
        public UnityEvent OnSiteFound = new UnityEvent();
        public UnityEvent OnEndApply = new UnityEvent();
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_BasicSiteConditions.Initialize(scene);
            m_BasicSiteConditions.OnApplyActionOnSite.AddListener(Apply);
            m_AdvancedSiteConditions.Initialize(scene);
            m_AdvancedSiteConditions.OnApplyActionOnSite.AddListener(Apply);
            OnEndApply.AddListener(() =>
            {
                m_Coroutine = null;
            });
        }
        public void OnClickApply()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                OnEndApply.Invoke();
            }
            else
            {
                List<Site> sites = new List<Site>();

                if (AllColumns)
                {
                    foreach (var column in m_Scene.ColumnManager.Columns)
                    {
                        sites.AddRange(column.Sites);
                    }
                }
                else
                {
                    sites.AddRange(m_Scene.ColumnManager.SelectedColumn.Sites);
                }

                if (UseAdvanced)
                {
                    m_Coroutine = this.StartCoroutineAsync(m_AdvancedSiteConditions.c_FindSites(sites, OnBeginApply, OnEndApply, OnApplyingActions));
                }
                else
                {
                    m_Coroutine = this.StartCoroutineAsync(m_BasicSiteConditions.c_FindSites(sites, OnBeginApply, OnEndApply, OnApplyingActions));
                }
            }
        }
        #endregion

        #region Private Methods
        private void Apply(Site site)
        {
            if (m_Exclude.isOn) site.State.IsExcluded = true;
            if (m_Unexclude.isOn) site.State.IsExcluded = false;
            if (m_Highlight.isOn) site.State.IsHighlighted = true;
            if (m_Unhighlight.isOn) site.State.IsHighlighted = false;
            if (m_Blacklist.isOn) site.State.IsBlackListed = true;
            if (m_Unblacklist.isOn) site.State.IsBlackListed = false;
            if (m_Mark.isOn) site.State.IsMarked = true;
            if (m_Unmark.isOn) site.State.IsMarked = false;
            if (m_Suspect.isOn) site.State.IsSuspicious = true;
            if (m_Unsuspect.isOn) site.State.IsSuspicious = false;
            OnSiteFound.Invoke();
        }
        #endregion
    }

    [Serializable]
    public class ApplyingActionEvent : UnityEvent<float> { }
}