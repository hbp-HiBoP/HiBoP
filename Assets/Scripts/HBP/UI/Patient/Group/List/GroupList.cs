using HBP.Data;
using System.Linq;

namespace HBP.UI.Anatomy
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
                m_Objects = m_Objects.OrderByDescending(x => x.Name).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Name).ToList();
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
                m_Objects = m_Objects.OrderByDescending(x => x.Patients.Count).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Patients.Count).ToList();
            }
            m_sortByPatients = !m_sortByPatients;
            ApplySort();
        }
        #endregion
    }
}