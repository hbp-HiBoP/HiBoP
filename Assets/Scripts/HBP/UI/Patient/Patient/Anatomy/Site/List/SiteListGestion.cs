using HBP.Data.Anatomy;
using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class SiteListGestion : ListGestion<Site>
    {
        #region Properties
        [SerializeField] protected SiteList m_List;
        public override SelectableListWithItemAction<Site> List => m_List;

        [SerializeField] protected SiteCreator m_ObjectCreator;
        public override ObjectCreator<Site> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}