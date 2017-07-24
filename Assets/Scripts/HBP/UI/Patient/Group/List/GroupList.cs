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
        #endregion

        #region Public methods
        /// <summary>
        /// Sort the groups by name.
        /// </summary>
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Sort the groups by size.
        /// </summary>
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatients:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Patients;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPatients;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}