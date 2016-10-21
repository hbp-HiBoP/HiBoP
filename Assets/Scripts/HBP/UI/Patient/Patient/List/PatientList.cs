using d = HBP.Data.Patient;
using System.Linq;

namespace HBP.UI.Patient
{
	/// <summary>
	/// Manage patient list.
	/// </summary>
	public class PatientList : Tools.Unity.Lists.SelectableListWithItemAction<d.Patient>
	{
        #region Properties
        bool m_sortByName, m_sortByPlace, m_sortByDate, m_sortByPaths;
        #endregion

        #region SortingMethods
        public void SortByName()
        {
            if(m_sortByName)
            {
                m_objects = m_objects.OrderByDescending(x => x.Name).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Name).ToList();
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }
        public void SortByPlace()
        {
            if (m_sortByPlace)
            {
                m_objects = m_objects.OrderByDescending(x => x.Place).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Place).ToList();
            }
            m_sortByPlace = !m_sortByPlace;
            ApplySort();
        }
        public void SortByDate()
        {
            if (m_sortByDate)
            {
                m_objects = m_objects.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Date).ToList();
            }
            m_sortByDate = !m_sortByDate;
            ApplySort();
        }
        public void SortByPaths()
        {
            if (m_sortByPaths)
            {
                m_objects = m_objects.OrderByDescending(x => x.Brain.NotEmptyPaths).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Brain.NotEmptyPaths).ToList();
            }
            m_sortByPaths = !m_sortByPaths;
            ApplySort();
        }
        #endregion
    }
}
