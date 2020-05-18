using d = HBP.Data.Experience.Protocol;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// List to display protocols.
    /// </summary>
    public class ProtocolList : Tools.Unity.Lists.ActionableList<d.Protocol>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Blocs, DescendingBlocs }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_BlocsSortingDisplayer;
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
            m_BlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by blocs.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByBlocs(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Blocs.Count).ToList();
                    m_OrderBy = OrderBy.Blocs;
                    m_BlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Blocs.Count).ToList();
                    m_OrderBy = OrderBy.DescendingBlocs;
                    m_BlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by blocs
        /// </summary>
        public void SortByBlocs()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingBlocs: SortByBlocs(Sorting.Ascending); break;
                default: SortByBlocs(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_BlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}