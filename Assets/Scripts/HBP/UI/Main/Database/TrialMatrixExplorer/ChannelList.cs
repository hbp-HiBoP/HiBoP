using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Database
{
    public class ChannelList : SelectableList<ChannelStruct>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Place, DescendingPlace, Date, DescendingDate }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        #endregion

        #region Public Methods
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Channel).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Channel).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderBy = OrderBy.None;
        }
        #endregion

        #region Protected Methods
        protected override IEnumerable<ChannelStruct> DefaultSorting(IEnumerable<ChannelStruct> objects)
        {
            return objects.OrderBy(c => c.Channel, new SiteNameComparer());
        }
        #endregion
    }
}