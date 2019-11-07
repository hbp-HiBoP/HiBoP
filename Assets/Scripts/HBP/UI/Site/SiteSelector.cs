using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class SiteSelector : ObjectSelector<Data.Site>
    {
        #region Properties
        [SerializeField] SiteList m_List;
        protected override SelectableList<Data.Site> List => m_List;
        #endregion
    }
}