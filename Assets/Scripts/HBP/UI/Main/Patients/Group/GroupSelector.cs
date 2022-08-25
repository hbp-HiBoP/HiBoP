using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select groups.
    /// </summary>
    public class GroupSelector : ObjectSelector<Core.Data.Group>
    {
        #region Properties
        [SerializeField] GroupList m_List;
        /// <summary>
        /// UI groups list.
        /// </summary>
        protected override SelectableList<Core.Data.Group> List => m_List;
        #endregion
    }
}