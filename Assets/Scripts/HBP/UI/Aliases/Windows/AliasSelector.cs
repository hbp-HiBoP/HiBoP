using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Alias
{
    public class AliasSelector : ObjectSelector<Data.Preferences.Alias>
    {
        #region Properties
        [SerializeField] AliasList m_List;
        protected override SelectableList<Data.Preferences.Alias> List { get => m_List; }
        #endregion
    }
}