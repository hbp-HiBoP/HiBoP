using HBP.Data;
using HBP.UI.Anatomy;
using UnityEngine;

namespace HBP.UI
{
    public class GroupSelector : ObjectSelector<Group>
    {
        #region Properties
        [SerializeField] GroupList m_GroupList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_GroupList;
            base.Initialize();
        }
        #endregion
    }
}