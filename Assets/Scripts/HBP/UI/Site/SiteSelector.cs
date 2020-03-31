using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select sites.
    /// </summary>
    public class SiteSelector : ObjectSelector<Data.Site>
    {
        #region Properties
        [SerializeField] SiteList m_List;
        /// <summary>
        /// UI sites list.
        /// </summary>
        protected override SelectableList<Data.Site> List => m_List;
        #endregion
    }
}