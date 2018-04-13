using CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    public class SiteConditions : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        [SerializeField] SiteConditionList m_SiteConditionList;
        Coroutine m_Coroutine;
        #endregion

        #region Events
        public UnityEvent OnBeginFindingSuspiciousSites = new UnityEvent();
        public FindingSuspiciousSitesEvent OnFindingSuspiciousSites = new FindingSuspiciousSitesEvent();
        public UnityEvent OnSuspiciousSiteFound = new UnityEvent();
        public UnityEvent OnEndFindingSuspiciousSites = new UnityEvent();
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_SiteConditionList.Initialize();
        }
        public void FindSuspiciousSites()
        {
            if (m_Coroutine != null) StopCoroutine(m_Coroutine);
            m_Coroutine = this.StartCoroutineAsync(c_FindSuspiciousSites());
        }
        public void AddCondition()
        {
            m_SiteConditionList.Add(new SiteCondition());
        }
        public void RemoveSelectedConditions()
        {
            SiteCondition[] siteConditions = m_SiteConditionList.ObjectsSelected;
            foreach (var siteCondition in siteConditions)
            {
                m_SiteConditionList.Remove(siteCondition);
            }
        }
        #endregion

        #region Coroutines
        IEnumerator c_FindSuspiciousSites()
        {
            yield return Ninja.JumpToUnity;
            OnBeginFindingSuspiciousSites.Invoke();
            List<Site> sites = m_Scene.ColumnManager.SelectedColumn.Sites;
            int length = sites.Count;
            for (int i = 0; i < length; ++i)
            {
                Site site = sites[i];
                if (site.Configuration.NormalizedValues.Length > 0)
                {
                    yield return Ninja.JumpBack;
                    bool isSuspicious = m_SiteConditionList.Objects.All(c => c.Check(site));
                    yield return Ninja.JumpToUnity;
                    if (isSuspicious)
                    {
                        site.State.IsSuspicious = isSuspicious;
                        OnSuspiciousSiteFound.Invoke();
                    }
                    OnFindingSuspiciousSites.Invoke((float)(i + 1) / length);
                }
            }
            OnEndFindingSuspiciousSites.Invoke();
        }
        #endregion
    }

    [Serializable]
    public class FindingSuspiciousSitesEvent : UnityEvent<float> { }
}