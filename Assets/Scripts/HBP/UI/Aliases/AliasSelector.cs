using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select Aliases.
    /// </summary>
    public class AliasSelector : ObjectSelector<Core.Data.Alias>
    {
        #region Properties
        [SerializeField] AliasList m_List;
        /// <summary>
        /// UI aliases list.
        /// </summary>
        protected override SelectableList<Core.Data.Alias> List { get => m_List; }
        #endregion
    }
}