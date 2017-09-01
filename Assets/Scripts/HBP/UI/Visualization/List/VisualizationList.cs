using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Visualization.Visualization>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients, Columns, DescendingColumns }
        OrderBy m_OrderBy = OrderBy.None;
        #endregion

        #region Public Methods
        public void SortByVisualizationName()
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
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Patients:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPatients;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Patients;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByColumns()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Columns:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingColumns;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Columns;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}