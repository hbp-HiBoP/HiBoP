using HBP.Data.General;
using System.Linq;

namespace HBP.UI
{
    public class ProjectList : Tools.Unity.Lists.SelectableListWithItemAction<ProjectInfo>
    {
        #region Properties
        bool m_SortByName;
        bool m_SortByPatients;
        bool m_SortByGroups;
        bool m_SortByProtocols;
        bool m_SortByDatasets;
        bool m_SortByVisualizations;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            if (m_SortByName) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Settings.Name).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Settings.Name).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByName = !m_SortByName;
            m_SortByPatients = false;
            m_SortByGroups = false;
            m_SortByProtocols = false;
            m_SortByDatasets = false;
            m_SortByVisualizations = false;
        }
        public void SortByPatients()
        {
            if (m_SortByPatients) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Patients).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Patients).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByPatients = !m_SortByPatients;
            m_SortByName = false;
            m_SortByGroups = false;
            m_SortByProtocols = false;
            m_SortByDatasets = false;
            m_SortByVisualizations = false;
        }
        public void SortByGroups()
        {
            if (m_SortByGroups) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Groups).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Groups).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByGroups = !m_SortByGroups;
            m_SortByName = false;
            m_SortByPatients = false;
            m_SortByProtocols = false;
            m_SortByDatasets = false;
            m_SortByVisualizations = false;
        }
        public void SortByProtocols()
        {
            if (m_SortByProtocols) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Protocols).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Protocols).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByProtocols = !m_SortByProtocols;
            m_SortByName = false;
            m_SortByPatients = false;
            m_SortByGroups = false;
            m_SortByDatasets = false;
            m_SortByVisualizations = false;
        }
        public void SortByDatasets()
        {
            if (m_SortByDatasets) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Datasets).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Datasets).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByDatasets = !m_SortByDatasets;
            m_SortByName = false;
            m_SortByPatients = false;
            m_SortByGroups = false;
            m_SortByProtocols = false;
            m_SortByVisualizations = false;
        }
        public void SortByVisualizations()
        {
            if (m_SortByVisualizations) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Visualizations).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Visualizations).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByVisualizations = !m_SortByVisualizations;
            m_SortByName = false;
            m_SortByPatients = false;
            m_SortByGroups = false;
            m_SortByDatasets = false;
        }
        #endregion
    }
}