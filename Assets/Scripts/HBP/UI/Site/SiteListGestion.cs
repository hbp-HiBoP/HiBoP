using System.Linq;
using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class SiteListGestion : ListGestion<Data.Site>
    {
        #region Properties
        [SerializeField] protected SiteList m_List;
        public override ActionableList<Data.Site> List => m_List;

        [SerializeField] protected SiteCreator m_ObjectCreator;
        public override ObjectCreator<Data.Site> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_ObjectCreator.OnTryMergeSite.AddListener(OnTryMergeSite);
        }
        private void OnTryMergeSite(Data.Site site)
        {
            Data.Site existingSite = m_List.Objects.FirstOrDefault(s => s.Name == site.Name);
            if (existingSite != null)
            {
                existingSite.Coordinates.AddRange(site.Coordinates);
            }
            else
            {
                m_List.Add(site);
            }
        }
        #endregion
    }
}