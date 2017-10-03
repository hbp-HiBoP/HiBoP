using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Visualization.Visualization>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients, Columns, DescendingColumns }
        OrderBy m_OrderBy = OrderBy.None;

        public enum Sorting { Ascending, Descending}
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PatientsSortingDisplayer;
        public SortingDisplayer m_ColumnsSortingDisplayer;
        #endregion

        #region Public Methods
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Name;
                    break;
                case Sorting.Descending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingName;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name: SortByName(Sorting.Descending); break;
                default: SortByName(Sorting.Ascending); break;
            }
        }
        public void SortByPatients(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Patients;
                    break;
                case Sorting.Descending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingPatients;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Patients: SortByPatients(Sorting.Descending); break;
                default: SortByPatients(Sorting.Ascending); break;
            }
        }
        public void SortByColumns(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Columns;
                    break;
                case Sorting.Descending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingColumns;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ColumnsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByColumns()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Columns: SortByColumns(Sorting.Ascending); break;
                default: SortByColumns(Sorting.Descending); break;
            }
        }
        #endregion
    }
}