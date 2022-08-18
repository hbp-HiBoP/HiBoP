using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select tags.
    /// </summary>
    public class TagSelector : ObjectSelector<Core.Data.BaseTag>
    {
        #region Properties
        [SerializeField] TagList m_List;
        /// <summary>
        /// UI tags list.
        /// </summary>
        protected override SelectableList<Core.Data.BaseTag> List => m_List;
        #endregion
    }
}