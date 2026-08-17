using HBP.Data.Informations;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class ChannelStructsGroupList : ActionableList<ChannelStructsGroup>
    {
        #region Properties

        enum OrderBy
        {
            None,
            Name,
            DescendingName,
            Channels,
            DescendingChannels
        }

        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_ChannelsSortingDisplayer;

        #endregion

        #region Private Methods

        protected override void AddObject(ChannelStructsGroup objectToAdd)
        {
            SortByNone();
            base.AddObject(objectToAdd);
        }

        #endregion

        #region Sorting Methods

        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_ChannelsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name:
                    SortByName(Sorting.Ascending);
                    break;
                case OrderBy.DescendingName:
                    SortByName(Sorting.Descending);
                    break;
                default:
                    SortByName(Sorting.Ascending);
                    break;
            }
        }

        public void SortByChannels(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Channels.Count).ToList();
                    m_OrderBy = OrderBy.Channels;
                    m_ChannelsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Channels.Count).ToList();
                    m_OrderBy = OrderBy.DescendingChannels;
                    m_ChannelsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        public void SortByChannels()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Channels:
                    SortByChannels(Sorting.Ascending);
                    break;
                case OrderBy.DescendingChannels:
                    SortByChannels(Sorting.Descending);
                    break;
                default:
                    SortByChannels(Sorting.Ascending);
                    break;
            }
        }

        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ChannelsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        #endregion
    }
}
