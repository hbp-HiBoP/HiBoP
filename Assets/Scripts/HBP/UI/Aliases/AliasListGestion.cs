using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Alias
{
    public class AliasListGestion : ListGestion<Data.Preferences.Alias>
    {
        #region Properties
        [SerializeField] AliasList m_List;
        public override SelectableListWithItemAction<Data.Preferences.Alias> List => m_List;

        [SerializeField] AliasCreator m_ObjectCreator;
        public override ObjectCreator<Data.Preferences.Alias> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}