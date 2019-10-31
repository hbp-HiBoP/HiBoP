using HBP.Data;
using HBP.UI.Anatomy;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class GroupSelector : ObjectSelector<Group>
    {
        #region Properties
        [SerializeField] GroupList m_List;
        protected override SelectableList<Group> List => m_List;
        #endregion
    }
}