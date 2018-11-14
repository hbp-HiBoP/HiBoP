using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI
{
    public class EventSelector : ObjectSelector<d.Event>
    {
        #region Properties
        [SerializeField] EventList m_EventList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_EventList;
            base.Initialize();
        }
        #endregion
    }
}