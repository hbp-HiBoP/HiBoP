using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class EventSelector : ObjectSelector<d.Event>
    {
        #region Properties
        [SerializeField] EventList m_List;
        protected override SelectableList<d.Event> List => m_List;
        #endregion
    }
}