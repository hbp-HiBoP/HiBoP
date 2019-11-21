using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class GroupListGestion : ListGestion<Data.Group>
    {
        #region Properties
        [SerializeField] protected GroupList m_List;
        public override SelectableListWithItemAction<Data.Group> List => m_List;

        [SerializeField] protected GroupCreator m_ObjectCreator;
        public override ObjectCreator<Data.Group> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}