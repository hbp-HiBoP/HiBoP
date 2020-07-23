using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Component to manage a list of aliases (create, remove, list).
    /// </summary>
    public class AliasListGestion : ListGestion<Data.Alias>
    {
        #region Properties
        [SerializeField] AliasList m_List;
        public override ActionableList<Data.Alias> List => m_List;

        [SerializeField] AliasCreator m_ObjectCreator;
        public override ObjectCreator<Data.Alias> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}