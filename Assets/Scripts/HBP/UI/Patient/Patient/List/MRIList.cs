using System.Linq;

namespace HBP.UI.Anatomy
{
    public class MRIList : Tools.Unity.Lists.SelectableListWithSave<Data.Anatomy.MRI>
    {
        #region Properties
        bool m_SortByName, m_SortByPath;
        #endregion

        #region SortingMethods
        public void SortByName()
        {
            if (!m_SortByName)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Name).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Name).ToList();
            }
            m_SortByName = !m_SortByName;
            m_SortByPath = false;
            Sort();
        }
        public void SortByPath()
        {
            if (!m_SortByPath)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Path).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Path).ToList();
            }
            m_SortByPath = !m_SortByPath;
            m_SortByName = false;
            Sort();
        }
        #endregion
    }
}