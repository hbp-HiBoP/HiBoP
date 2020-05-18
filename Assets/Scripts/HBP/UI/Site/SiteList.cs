using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// List to display sites.
    /// </summary>
    public class SiteList : Tools.Unity.Lists.ActionableList<Data.Site>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Coordinate, DescendingCoordinate, Tag, DescendingTag }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_CoordinateSortingDisplayer;
        [SerializeField] SortingDisplayer m_TagSortingDisplayer;
        #endregion

        #region Public Methods
        public override bool Add(Data.Site obj)
        {
            SortByNone();
            return base.Add(obj);
        }

        /// <summary>
        /// Sort by name.
        /// </summary>
        /// <param name="sorting">Sorting</param>
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
            m_CoordinateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by name.
        /// </summary>
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by number of coordinates.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByCoordinate(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Coordinates.Count).ToList();
                    m_OrderBy = OrderBy.Coordinate;
                    m_CoordinateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Coordinates.Count).ToList();
                    m_OrderBy = OrderBy.DescendingCoordinate;
                    m_CoordinateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by number of coordinates.
        /// </summary>
        public void SortByCoordinate()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingCoordinate: SortByCoordinate(Sorting.Ascending); break;
                default: SortByCoordinate(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by number of tags.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByTags(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Tags).ToList();
                    m_OrderBy = OrderBy.Tag;
                    m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Tags).ToList();
                    m_OrderBy = OrderBy.DescendingTag;
                    m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_CoordinateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by number of tags.
        /// </summary>
        public void SortByTags()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingTag: SortByTags(Sorting.Ascending); break;
                default: SortByTags(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_CoordinateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderBy = OrderBy.None;
        }
        #endregion
    }
}
