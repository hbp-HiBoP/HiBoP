using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select groups.
    /// </summary>
    public class GroupSelector : ObjectSelector<Data.Group>
    {
        #region Properties
        [SerializeField] GroupList m_List;
        /// <summary>
        /// UI groups list.
        /// </summary>
        protected override SelectableList<Data.Group> List => m_List;
        #endregion
    }
}