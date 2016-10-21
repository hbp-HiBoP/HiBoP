using HBP.Data.Patient;
using System.Linq;

namespace HBP.UI.Patient
{
    public class ConnectivityList : Tools.Unity.Lists.SelectableListWithSave<Connectivity>
    {
        #region SortAttributs
        bool m_sortByName = false;
        bool m_sortByPath = false;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            if (m_sortByName)
            {
                m_objects.OrderByDescending(x => x.Label);
            }
            else
            {
                m_objects.OrderBy(x => x.Label);
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }

        public void SortByPath()
        {
            if (m_sortByPath)
            {
                m_objects.OrderByDescending(x => x.Path);
            }
            else
            {
                m_objects.OrderBy(x => x.Path);
            }
            m_sortByPath = !m_sortByPath;
            ApplySort();
        }
        #endregion
    }
}