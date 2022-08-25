using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select sites.
    /// </summary>
    public class SiteSelector : ObjectSelector<Core.Data.Site>
    {
        #region Properties
        [SerializeField] SiteList m_List;
        /// <summary>
        /// UI sites list.
        /// </summary>
        protected override SelectableList<Core.Data.Site> List => m_List;
        #endregion
    }
}