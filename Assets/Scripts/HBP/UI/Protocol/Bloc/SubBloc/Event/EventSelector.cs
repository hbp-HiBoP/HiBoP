using HBP.UI.Tools.Lists;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
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