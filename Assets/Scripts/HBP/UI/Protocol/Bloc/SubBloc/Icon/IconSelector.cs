using HBP.UI.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select icons.
    /// </summary>
    public class IconSelector : ObjectSelector<Core.Data.Icon>
    {
        #region Properties
        [SerializeField] IconList m_List;
        /// <summary>
        /// UI icons list.
        /// </summary>
        protected override SelectableList<Core.Data.Icon> List => m_List;
        #endregion
    }
}