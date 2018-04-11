using CielaSpike;
using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SiteConditions : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        [SerializeField] SiteConditionList m_SiteConditionList;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_SiteConditionList.Initialize();
        }
        public void FindSuspiciousSites()
        {
            this.StartCoroutineAsync(c_FindSuspiciousSites());
        }
        public void AddCondition()
        {
            m_SiteConditionList.Add(new SiteCondition());
        }
        public void RemoveSelectedConditions()
        {
            SiteCondition[] siteConditions = m_SiteConditionList.Objects;
            foreach (var siteCondition in siteConditions)
            {
                m_SiteConditionList.Remove(siteCondition);
            }
        }
        #endregion

        #region Coroutines
        IEnumerator c_FindSuspiciousSites()
        {
            foreach (var site in m_Scene.ColumnManager.SelectedColumn.Sites) // TODO visual feedback if it is slow
            {
                if (site.Configuration.NormalizedValues.Length > 0)
                {
                    yield return Ninja.JumpBack;
                    bool isSuspicious = m_SiteConditionList.Objects.All(c => c.Check(site));
                    yield return Ninja.JumpToUnity;
                    if (isSuspicious) site.State.IsSuspicious = isSuspicious;
                }
            }
        }
        #endregion
    }
}