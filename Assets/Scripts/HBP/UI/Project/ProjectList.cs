using HBP.Data.General;
using System.Linq;

namespace HBP.UI
{
    public class ProjectList : Tools.Unity.Lists.SelectableListWithItemAction<ProjectInfo>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients, Groups, DescendingGroups, Protocols, DescendingProtocols, Datasets, DescendingDatasets, Visualizations, DescendingVisualizations }
        OrderBy m_OrderBy = OrderBy.None;
        public enum Sorting { Ascending, Descending };
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
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Settings.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Settings.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            Refresh();
        }
        public void SortByPatients()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatients: SortByPatients(Sorting.Ascending); break;
                default: SortByPatients(Sorting.Descending); break;
            }

        }
        public void SortByPatients(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Patients).ToList();
                    m_OrderBy = OrderBy.Patients;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Patients).ToList();
                    m_OrderBy = OrderBy.DescendingPatients;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
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
                case OrderBy.DescendingGroups: SortByGroups(Sorting.Ascending); break;
                default: SortByGroups(Sorting.Descending); break;
            }
        }
        public void SortByGroups(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Groups).ToList();
                    m_OrderBy = OrderBy.Groups;
                    m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Groups).ToList();
                    m_OrderBy = OrderBy.DescendingGroups;
                    m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
                default:
                    break;
            }
            Refresh();
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
                case OrderBy.DescendingProtocols: SortByProtocols(Sorting.Ascending); break;
                default: SortByProtocols(Sorting.Descending); break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByProtocols(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Protocols).ToList();
                    m_OrderBy = OrderBy.Protocols;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Protocols).ToList();
                    m_OrderBy = OrderBy.DescendingProtocols;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByDatasets()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDatasets: SortByDatasets(Sorting.Ascending); break;
                default: SortByDatasets(Sorting.Descending); break;
            }

        }
        public void SortByDatasets(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Datasets).ToList();
                    m_OrderBy = OrderBy.Datasets;
                    m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Datasets).ToList();
                    m_OrderBy = OrderBy.DescendingDatasets;
                    m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
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
                case OrderBy.DescendingVisualizations: SortByVisualizations(Sorting.Ascending); break;
                default: SortByVisualizations(Sorting.Descending); break;
            }
        }
        public void SortByVisualizations(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Visualizations).ToList();
                    m_OrderBy = OrderBy.Visualizations;
                    m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Visualizations).ToList();
                    m_OrderBy = OrderBy.DescendingVisualizations;
                    m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}