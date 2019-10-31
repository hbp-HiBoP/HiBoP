using HBP.Data;
using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class GroupListGestion : ListGestion<Group>
    {
        #region Properties
        [SerializeField] protected GroupList m_List;
        public override SelectableListWithItemAction<Group> List => m_List;

        [SerializeField] protected GroupCreator m_ObjectCreator;
        public override ObjectCreator<Group> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}