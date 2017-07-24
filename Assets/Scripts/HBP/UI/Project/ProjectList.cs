using HBP.Data.General;
using System.Linq;

namespace HBP.UI
{
    public class ProjectList : Tools.Unity.Lists.SelectableListWithItemAction<ProjectInfo>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients, Groups, DescendingGroups, Protocols, DescendingProtocols, Datasets, DescendingDatasets, Visualizations, DescendingVisualizations }
        OrderBy m_OrderBy = OrderBy.None;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Settings.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Settings.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatients:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Patients;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPatients;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByGroups()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingGroups:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Groups).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Groups;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Groups).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingGroups;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByProtocols()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingProtocols:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Protocols).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Protocols;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Protocols).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingProtocols;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByDatasets()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDatasets:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Datasets).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Datasets;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Datasets).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingDatasets;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByVisualizations()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingVisualizations:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Visualizations).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Visualizations;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Visualizations).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingVisualizations;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}