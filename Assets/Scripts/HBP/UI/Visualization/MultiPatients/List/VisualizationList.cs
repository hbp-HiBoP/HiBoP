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
                m_objects.OrderByDescending(x => x.Name);
            }
            else
            {
                m_objects.OrderBy(x => x.Name);
            }
            m_SortByName = !m_SortByName;
            ApplySort();
        }
        public void SortByNbPatients()
        {
            if (m_SortByPatients)
            {
                m_objects.OrderByDescending(x => x.Patients.Count);
            }
            else
            {
                m_objects.OrderBy(x => x.Patients.Count);
            }
            m_SortByPatients = !m_SortByPatients;
            ApplySort();
        }
        public void SortByNbColumns()
        {
            if (m_SortByColumns)
            {
                m_objects.OrderByDescending(x => x.Columns.Count);
            }
            else
            {
                m_objects.OrderBy(x => x.Columns.Count);
            }
            m_SortByColumns = !m_SortByColumns;
            ApplySort();
        }
        #endregion
    }
}