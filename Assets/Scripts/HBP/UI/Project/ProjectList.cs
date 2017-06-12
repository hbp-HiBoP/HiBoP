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
            if (m_sortByName)
            {
                m_objects = m_objects.OrderByDescending(x => x.Settings.Name).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Settings.Name).ToList();
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }

        public void SortByPatients()
        {
            if (m_sortByPatients)
            {
                m_objects = m_objects.OrderByDescending(x => x.Patients).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Patients).ToList();
            }
            m_sortByPatients = !m_sortByPatients;
            ApplySort();
        }

        public void SortByGroups()
        {
            if (m_sortByGroups)
            {
                m_objects = m_objects.OrderByDescending(x => x.Groups).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Groups).ToList();
            }
            m_sortByGroups = !m_sortByGroups;
            ApplySort();
        }

        public void SortByProtocols()
        {
            if (m_sortByProtocols)
            {
                m_objects = m_objects.OrderByDescending(x => x.Protocols).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Protocols).ToList();
            }
            m_sortByProtocols = !m_sortByProtocols;
            ApplySort();
        }

        public void SortByDatasets()
        {
            if (m_sortByDatasets)
            {
                m_objects = m_objects.OrderByDescending(x => x.Datasets).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Datasets).ToList();
            }
            m_sortByDatasets = !m_sortByDatasets;
            ApplySort();
        }

        public void SortByVisualizations()
        {
            if (m_sortByVisualizations)
            {
                m_objects = m_objects.OrderByDescending(x => x.Visualizations).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Visualizations).ToList();
            }
            m_sortByVisualizations = !m_sortByVisualizations;
            ApplySort();
        }
        #endregion
    }
}