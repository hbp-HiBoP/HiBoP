using HBP.Data.Anatomy;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class SiteSelector : ObjectSelector<Site>
    {
        #region Properties
        [SerializeField] SiteList m_List;
        protected override SelectableList<Site> List => m_List;
        #endregion
    }
}