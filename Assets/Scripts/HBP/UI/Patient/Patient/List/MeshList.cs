using System.Linq;


namespace HBP.UI.Anatomy
{
    public class MeshList : Tools.Unity.Lists.SelectableListWithSave<Data.Anatomy.Mesh>
    {
        #region Properties
        bool m_SortByName, m_SortByType;
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
            m_SortByType = false;
            Sort();
        }
        public void SortByType()
        {
            if (!m_SortByType)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.GetType().ToString()).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.GetType().ToString()).ToList();
            }
            m_SortByType = !m_SortByType;
            m_SortByName = false;
            Sort();
        }
        #endregion
    }
}