using HBP.Data.Visualization;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class SingleVisualizationList : Tools.Unity.Lists.OneSelectableListWithItemActions<SinglePatientVisualization>
    {
        #region Attributs
        bool m_sortByName = false;
        bool m_sortByPatient = false;
        bool m_sortByColumns = false;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            if (m_sortByName)
            {
                m_objects.OrderByDescending(x => x.Name);
            }
            else
            {
                m_objects.OrderBy(x => x.Name);
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }
        public void SortByPatient()
        {
            if (m_sortByPatient)
            {
                m_objects.OrderByDescending(x => x.Patient.Name);
            }
            else
            {
                m_objects.OrderBy(x => x.Patient.Name);
            }
            m_sortByPatient = !m_sortByPatient;
            ApplySort();
        }
        public void SortByColumns()
        {
            if (m_sortByColumns)
            {
                m_objects.OrderByDescending(x => x.Columns.Count);
            }
            else
            {
                m_objects.OrderBy(x => x.Columns.Count);
            }
            m_sortByColumns = !m_sortByColumns;
            ApplySort();
        }
        #endregion
    }
}
