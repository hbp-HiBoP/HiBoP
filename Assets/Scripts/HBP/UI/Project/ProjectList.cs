using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// List to display projects.
    /// </summary>
    public class ProjectList : Tools.Unity.Lists.ActionableList<Data.ProjectInfo>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patients, DescendingPatients, Groups, DescendingGroups, Protocols, DescendingProtocols, Datasets, DescendingDatasets, Visualizations, DescendingVisualizations }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_PatientSortingDisplayer;
        [SerializeField] SortingDisplayer m_GroupSortingDisplayer;
        [SerializeField] SortingDisplayer m_ProtocolSortingDisplayer;
        [SerializeField] SortingDisplayer m_DatasetSortingDisplayer;
        [SerializeField] SortingDisplayer m_VisualizationSortingDisplayer;
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
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            Refresh();
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
        /// Sort by groups.
        /// </summary>
        /// <param name="sorting">Sorting</param>
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
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            Refresh();
        }
        /// <summary>
        /// Sort by groups.
        /// </summary>
        public void SortByGroups()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingGroups: SortByGroups(Sorting.Ascending); break;
                default: SortByGroups(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by protocols.
        /// </summary>
        /// <param name="sorting">Sorting</param>
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
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            Refresh();
        }
        /// <summary>
        /// Sort by protocols.
        /// </summary>
        public void SortByProtocols()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingProtocols: SortByProtocols(Sorting.Ascending); break;
                default: SortByProtocols(Sorting.Descending); break;
            }
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            Refresh();
        }

        /// <summary>
        /// Sort by datasets.
        /// </summary>
        /// <param name="sorting">Sorting</param>
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
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            Refresh();
        }
        /// <summary>
        /// Sort by datasets.
        /// </summary>
        public void SortByDatasets()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDatasets: SortByDatasets(Sorting.Ascending); break;
                default: SortByDatasets(Sorting.Descending); break;
            }

        }

        /// <summary>
        /// Sort by visualizations.
        /// </summary>
        /// <param name="sorting">Sorting</param>
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
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            Refresh();
        }
        /// <summary>
        /// Sort by visualizations.
        /// </summary>
        public void SortByVisualizations()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingVisualizations: SortByVisualizations(Sorting.Ascending); break;
                default: SortByVisualizations(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_GroupSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DatasetSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_VisualizationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderBy = OrderBy.None;
         }
        #endregion
    }
}