using System.Linq;
using HBP.Data.Experience.Protocol;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class EventGestion : Gestion<Data.Experience.Protocol.Event, SubBloc>
    {
        #region Properties
        [SerializeField] EventList m_List;
        protected override SelectableListWithItemAction<Data.Experience.Protocol.Event> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(SubBloc subBloc)
        {
            base.Set(subBloc);
            List.Objects = m_ParentObject.Events.ToArray();
            m_List.SortByName(EventList.Sorting.Descending);
        }
        public override void Save()
        {
            m_ParentObject.Events = List.Objects.ToList();
        }
        #endregion

        #region Private Methods
        protected override void OnSaveModifier(ItemModifier<Data.Experience.Protocol.Event> modifier)
        {
            base.OnSaveModifier(modifier);
            m_List.SortByNone();
        }
        #endregion
    }
}