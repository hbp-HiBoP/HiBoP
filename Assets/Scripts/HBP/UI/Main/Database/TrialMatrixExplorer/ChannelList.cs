using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Database
{
    public class ChannelList : SelectableList<ChannelStruct>
    {
        #region Properties

        enum OrderBy
        {
            None,
            Name,
            DescendingName,
            Place,
            DescendingPlace,
            Date,
            DescendingDate
        }

        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;

        public bool CanSelectNext
        {
            get
            {
                var selected = ObjectsSelected;
                if (selected.Length == 0) return m_DisplayedObjects.Count > 0;
                int maxIndex = m_DisplayedObjects.Where(o => selected.Contains(o)).Select(o => m_DisplayedObjects.IndexOf(o)).DefaultIfEmpty(-1).Max();
                return maxIndex < m_DisplayedObjects.Count - 1;
            }
        }

        public bool CanSelectPrevious
        {
            get
            {
                var selected = ObjectsSelected;
                if (selected.Length == 0) return false;
                int minIndex = m_DisplayedObjects.Where(o => selected.Contains(o)).Select(o => m_DisplayedObjects.IndexOf(o)).DefaultIfEmpty(int.MaxValue).Min();
                return minIndex > 0;
            }
        }

        #endregion

        #region Events

        public UnityEvent OnReachEnd = new();
        public UnityEvent OnReachBeginning = new();

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

        public override void SelectNext(bool scroll = true)
        {
            if (CanSelectNext)
            {
                base.SelectNext(scroll);
            }
            else
            {
                OnReachEnd.Invoke();
            }
        }

        public override void SelectPrevious(bool scroll = true)
        {
            if (CanSelectPrevious)
            {
                base.SelectPrevious(scroll);
            }
            else
            {
                OnReachBeginning.Invoke();
            }
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
