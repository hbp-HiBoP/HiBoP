using HBP.Data.General;
using System.Linq;

namespace HBP.UI
{
    public class ProjectList : Tools.Unity.Lists.OneSelectableListWithItemActions<ProjectInfo>
    {
        #region Properties
        bool m_sortByName;
        bool m_sortByPatients;
        bool m_sortByGroups;
        bool m_sortByProtocols;
        bool m_sortByDatasets;
        bool m_sortByVisualizations;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            if (!m_sortByName)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Settings.Name).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Settings.Name).ToList();
            }
            m_sortByName = !m_sortByName;
            m_sortByPatients = false;
            m_sortByGroups = false;
            m_sortByProtocols = false;
            m_sortByDatasets = false;
            m_sortByVisualizations = false;
            Sort();
        }
        public void SortByPatients()
        {
            if (!m_sortByPatients)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Patients).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Patients).ToList();
            }
            m_sortByPatients = !m_sortByPatients;
            m_sortByName = false;
            m_sortByGroups = false;
            m_sortByProtocols = false;
            m_sortByDatasets = false;
            m_sortByVisualizations = false;
            Sort();
        }
        public void SortByGroups()
        {
            if (!m_sortByGroups)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Groups).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Groups).ToList();
            }
            m_sortByGroups = !m_sortByGroups;
            m_sortByName = false;
            m_sortByPatients = false;
            m_sortByProtocols = false;
            m_sortByDatasets = false;
            m_sortByVisualizations = false;
            Sort();
        }
        public void SortByProtocols()
        {
            if (!m_sortByProtocols)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Protocols).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Protocols).ToList();
            }
            m_sortByProtocols = !m_sortByProtocols;
            m_sortByName = false;
            m_sortByPatients = false;
            m_sortByGroups = false;
            m_sortByDatasets = false;
            m_sortByVisualizations = false;
            Sort();
        }
        public void SortByDatasets()
        {
            if (!m_sortByDatasets)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Datasets).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Datasets).ToList();
            }
            m_sortByDatasets = !m_sortByDatasets;
            m_sortByName = false;
            m_sortByPatients = false;
            m_sortByGroups = false;
            m_sortByProtocols = false;
            m_sortByVisualizations = false;
            Sort();
        }
        public void SortByVisualizations()
        {
            if (!m_sortByVisualizations)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Visualizations).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Visualizations).ToList();
            }
            m_sortByVisualizations = !m_sortByVisualizations;
            m_sortByName = false;
            m_sortByPatients = false;
            m_sortByGroups = false;
            m_sortByDatasets = false;
            Sort();
        }
        #endregion
    }
}