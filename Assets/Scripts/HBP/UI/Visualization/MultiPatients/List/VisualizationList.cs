using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationList : Tools.Unity.Lists.OneSelectableListWithItemActions<Data.Visualization.Visualization>
    {
        #region Properties
        bool m_SortByName = false;
        bool m_SortByPatients = false;
        bool m_SortByColumns = false;
        #endregion

        #region Public Methods
        public void SortByVisualizationName()
        {
            if (m_SortByName)
            {
                m_Objects.OrderByDescending(x => x.Name);
            }
            else
            {
                m_Objects.OrderBy(x => x.Name);
            }
            m_SortByName = !m_SortByName;
            Sort();
        }
        public void SortByNbPatients()
        {
            if (m_SortByPatients)
            {
                m_Objects.OrderByDescending(x => x.Patients.Count);
            }
            else
            {
                m_Objects.OrderBy(x => x.Patients.Count);
            }
            m_SortByPatients = !m_SortByPatients;
            Sort();
        }
        public void SortByNbColumns()
        {
            if (m_SortByColumns)
            {
                m_Objects.OrderByDescending(x => x.Columns.Count);
            }
            else
            {
                m_Objects.OrderBy(x => x.Columns.Count);
            }
            m_SortByColumns = !m_SortByColumns;
            Sort();
        }
        #endregion
    }
}