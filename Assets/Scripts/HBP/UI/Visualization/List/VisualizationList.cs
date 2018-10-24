using System.Linq;
using UnityEngine;

namespace HBP.UI.Visualization
{
    public class VisualizationList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Visualization.Visualization>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients, Columns, DescendingColumns }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_PatientsSortingDisplayer;
        [SerializeField] SortingDisplayer m_ColumnsSortingDisplayer;
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
                    m_Objects = m_Objects.OrderBy((elt) => elt.Name).ToList();
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Name;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Name).ToList();
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingName;
                    break;
            }
            Refresh();
            m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by patients.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByPatients(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Patients.Count).ToList();
                    m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Patients;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Patients.Count).ToList();
                    m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingPatients;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by patients.
        /// </summary>
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatients: SortByPatients(Sorting.Ascending); break;
                default: SortByPatients(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by columns.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByColumns(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Columns.Count).ToList();
                    m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Columns;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Columns.Count).ToList();
                    m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingColumns;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by columns.
        /// </summary>
        public void SortByColumns()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingColumns: SortByColumns(Sorting.Ascending); break;
                default: SortByColumns(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}