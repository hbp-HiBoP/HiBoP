using HBP.Data.General;
using System.Linq;

namespace HBP.UI
{
    public class ProjectList : Tools.Unity.Lists.SelectableListWithItemAction<ProjectInfo>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients, Groups, DescendingGroups, Protocols, DescendingProtocols, Datasets, DescendingDatasets, Visualizations, DescendingVisualizations }
        OrderBy m_OrderBy = OrderBy.None;

        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PatientSortingDisplayer;
        public SortingDisplayer m_GroupSortingDisplayer;
        public SortingDisplayer m_ProtocolSortingDisplayer;
        public SortingDisplayer m_DatasetSortingDisplayer;
        public SortingDisplayer m_VisualizationSortingDisplayer;

        #endregion

        #region Public Methods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Settings.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Settings.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatients:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patients).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Patients;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;

                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patients).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPatients;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByGroups()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingGroups:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Groups).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Groups;
                    m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Groups).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingGroups;
                    m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByProtocols()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingProtocols:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Protocols).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Protocols;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Protocols).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingProtocols;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByDatasets()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDatasets:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Datasets).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Datasets;
                    m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Datasets).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingDatasets;
                    m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByVisualizations()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingVisualizations:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Visualizations).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Visualizations;
                    m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Visualizations).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingVisualizations;
                    m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}