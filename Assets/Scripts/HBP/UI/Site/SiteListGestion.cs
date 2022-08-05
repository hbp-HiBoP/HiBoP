using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class SiteListGestion : ListGestion<Core.Data.Site>
    {
        #region Properties
        [SerializeField] protected SiteList m_List;
        public override ActionableList<Core.Data.Site> List => m_List;

        [SerializeField] protected SiteCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Site> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Private Methods
        protected override void OnObjectCreated(Core.Data.Site site)
        {
            Core.Data.Site existingSite = m_List.Objects.FirstOrDefault(s => s.Name == site.Name);
            if (existingSite != null)
            {
                existingSite.Coordinates.AddRange(site.Coordinates);
                existingSite.Tags.AddRange(site.Tags);
            }
            else
            {
                m_List.Add(site);
            }
        }
        #endregion
    }
}