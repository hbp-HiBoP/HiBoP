using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class AliasSelector : ObjectSelector<Data.Alias>
    {
        #region Properties
        [SerializeField] AliasList m_List;
        protected override SelectableList<Data.Alias> List { get => m_List; }
        #endregion
    }
}