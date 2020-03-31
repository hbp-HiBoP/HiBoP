using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select icons.
    /// </summary>
    public class IconSelector : ObjectSelector<d.Icon>
    {
        #region Properties
        [SerializeField] IconList m_List;
        /// <summary>
        /// UI icons list.
        /// </summary>
        protected override SelectableList<d.Icon> List => m_List;
        #endregion
    }
}