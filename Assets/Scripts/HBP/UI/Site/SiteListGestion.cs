using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class SiteListGestion : ListGestion<Data.Site>
    {
        #region Properties
        [SerializeField] protected SiteList m_List;
        public override SelectableListWithItemAction<Data.Site> List => m_List;

        [SerializeField] protected SiteCreator m_ObjectCreator;
        public override ObjectCreator<Data.Site> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}