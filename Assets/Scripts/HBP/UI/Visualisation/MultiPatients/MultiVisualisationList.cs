using HBP.Data.Visualisation;
using System.Linq;

namespace HBP.UI.Visualisation
{
    public class MultiVisualisationList : Tools.Unity.Lists.OneSelectableListWithItemActions<MultiPatientsVisualisation>
    {
        #region Properties
        bool m_sortByName = false;
        bool m_sortByPatients = false;
        bool m_sortByColumns = false;
        #endregion

        #region Public Methods
        public void SortByVisualisationName()
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
        public void SortByNbPatients()
        {
            if (m_sortByPatients)
            {
                m_objects.OrderByDescending(x => x.Patients.Count);
            }
            else
            {
                m_objects.OrderBy(x => x.Patients.Count);
            }
            m_sortByPatients = !m_sortByPatients;
            ApplySort();
        }
        public void SortByNbColumns()
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