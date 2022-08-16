using HBP.UI.Experience.Protocol;
using HBP.UI.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select events.
    /// </summary>
    public class EventSelector : ObjectSelector<Core.Data.Event>
    {
        #region Properties
        [SerializeField] EventList m_List;
        /// <summary>
        /// UI events list.
        /// </summary>
        protected override SelectableList<Core.Data.Event> List => m_List;
        #endregion
    }
}