using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class GroupSelector : ObjectSelector<Data.Group>
    {
        #region Properties
        [SerializeField] GroupList m_List;
        protected override SelectableList<Data.Group> List => m_List;
        #endregion
    }
}