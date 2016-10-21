using HBP.Data.Patient;
using System.Linq;

namespace HBP.UI.Patient
{
    /// <summary>
    /// Manage group list
    /// </summary>
    public class GroupList : Tools.Unity.Lists.SelectableListWithItemAction<Group>
    {
        #region Properties
        /// <summary>
        /// The name alphabetical sort.
        /// </summary>
        bool m_sortByName = false;

        /// <summary>
        /// The number of patients sort.
        /// </summary>
        bool m_sortByPatients = false;
        #endregion

        #region Public methods
        /// <summary>
        /// Sort the groups by name.
        /// </summary>
        public void SortByName()
        {
            if (m_sortByName)
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

        /// <summary>
        /// Sort the groups by size.
        /// </summary>
        public void SortByPatients()
        {
            if (m_sortByPatients)
            {
                m_objects = m_objects.OrderByDescending(x => x.Patients.Count).ToList();
            }
            else
            {
                m_objects = m_objects.OrderBy(x => x.Patients.Count).ToList();
            }
            m_sortByPatients = !m_sortByPatients;
            ApplySort();
        }
        #endregion
    }
}