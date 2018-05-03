using CielaSpike;
using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    public class AdvancedSiteConditions : MonoBehaviour // TODO
    {
        #region Properties
        private Base3DScene m_Scene;
        public GenericEvent<Site> OnApplyActionOnSite = new GenericEvent<Site>();
        [SerializeField] SiteConditionList m_SiteConditionList;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_SiteConditionList.Initialize();
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
        public IEnumerator c_FindSites(List<Site> sites, UnityEvent onBegin, UnityEvent onEnd, UnityEvent<float> onProgress)
        {
            yield return Ninja.JumpToUnity;
            onBegin.Invoke();
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
                        OnApplyActionOnSite.Invoke(site);
                    }
                    onProgress.Invoke((float)(i + 1) / length);
                }
            }
            onEnd.Invoke();
        }
        #endregion
    }
}