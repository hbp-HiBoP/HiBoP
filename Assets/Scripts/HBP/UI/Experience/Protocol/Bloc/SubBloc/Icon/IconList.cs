using HBP.Data.Experience.Protocol;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class IconList : Tools.Unity.Lists.SelectableListWithItemAction<Icon>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Path, DescendingPath, Start, DescendingStart, End, DescendingEnd}
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_StartSortingDisplayer;
        [SerializeField] SortingDisplayer m_EndSortingDisplayer;
        [SerializeField] SortingDisplayer m_IllustrationSortingDisplayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Sort by name.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
                default:
                    break;
            }
            Refresh();
            m_IllustrationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by path.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByPath(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.IllustrationPath).ToList();
                    m_OrderBy = OrderBy.Path;
                    m_IllustrationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.IllustrationPath).ToList();
                    m_OrderBy = OrderBy.DescendingPath;
                    m_IllustrationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by path.
        /// </summary>
		public void SortByPath()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPath: SortByPath(Sorting.Ascending); break;
                default: SortByPath(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by start.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByStart(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Window.Start).ToList();
                    m_OrderBy = OrderBy.Start;
                    m_StartSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Window.Start).ToList();
                    m_OrderBy = OrderBy.DescendingStart;
                    m_StartSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IllustrationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by start.
        /// </summary>
        public void SortByStart()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingStart: SortByStart(Sorting.Ascending); break;
                default: SortByStart(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by end.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByEnd(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Window.End).ToList();
                    m_OrderBy = OrderBy.End;
                    m_EndSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Window.End).ToList();
                    m_OrderBy = OrderBy.DescendingEnd;
                    m_EndSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IllustrationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by end.
        /// </summary>
        public void SortByEnd()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingEnd: SortByEnd(Sorting.Ascending); break;
                default: SortByEnd(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IllustrationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}
