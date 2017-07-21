using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Visualization.Visualization>
    {
        #region Properties
        bool m_SortByName = false;
        bool m_SortByPatients = false;
        bool m_SortByColumns = false;
        #endregion

        #region Public Methods
        public void SortByVisualizationName()
        {
            if (m_SortByName) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Name).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Name).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByName = !m_SortByName;
            m_SortByPatients = false;
            m_SortByColumns = false;
        }
        public void SortByNbPatients()
        {
            if (m_SortByPatients) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Patients).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Patients).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByPatients = !m_SortByPatients;
            m_SortByName = false;
            m_SortByColumns = false;
        }
        public void SortByNbColumns()
        {
            if (m_SortByColumns) m_ObjectsToItems = m_ObjectsToItems.OrderBy(x => x.Key.Columns).ToDictionary((k) => k.Key, (v) => v.Value);
            else m_ObjectsToItems = m_ObjectsToItems.OrderByDescending(x => x.Key.Columns).ToDictionary((k) => k.Key, (v) => v.Value);
            foreach (var item in m_ObjectsToItems) item.Value.transform.SetAsLastSibling();

            m_SortByColumns = !m_SortByColumns;
            m_SortByName = false;
            m_SortByPatients = false;
        }
        #endregion
    }
}