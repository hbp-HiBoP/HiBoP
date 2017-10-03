using HBP.Data;
using System.Linq;

namespace HBP.UI.Anatomy
{
    /// <summary>
    /// Manage group list
    /// </summary>
    public class GroupList : Tools.Unity.Lists.SelectableListWithItemAction<Group>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients }
        OrderBy m_OrderBy = OrderBy.None;

        public enum Sorting { Ascending, Descending}
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PatientsSortingDisplayer;
        #endregion

        #region Public methods
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByPatients(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Patients;
                    m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPatients;
                    m_PatientsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatients: SortByPatients(Sorting.Ascending); break;
                default: SortByPatients(Sorting.Descending); break;
            }
        }
        #endregion
    }
}