using System.Linq;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Experience.Dataset.DataInfo>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patient, DescendingPatient, State, DescendingState }
        OrderBy m_OrderBy = OrderBy.None;
        
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PatientSortingDisplayer;
        public SortingDisplayer m_StateSortingDisplayer;
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
            }
            Refresh();
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by patient.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByPatient(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Patient.Name).ToList();
                    m_OrderBy = OrderBy.Patient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Patient.Name).ToList();
                    m_OrderBy = OrderBy.DescendingPatient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by patient.
        /// </summary>
        public void SortByPatient()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatient: SortByPatient(Sorting.Ascending); break;
                default: SortByPatient(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by state.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByState(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.isOk).ToList();
                    m_OrderBy = OrderBy.State;
                    m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.isOk).ToList();
                    m_OrderBy = OrderBy.DescendingState;
                    m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by sate.
        /// </summary>
        public void SortByState()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingState: SortByState(Sorting.Ascending); break;
                default: SortByState(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;

            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}