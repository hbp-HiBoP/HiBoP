using d = HBP.Data.Experience.Dataset;
using System.Linq;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetList : Tools.Unity.Lists.SelectableListWithItemAction<d.Dataset>
    {
        #region Attributs
        bool m_sortByName = false;
        bool m_sortByData = false;

        #endregion

        #region Public Methods
        public void SortByName()
        {
            if (m_sortByName)
            {
                m_Objects.OrderByDescending(x => x.Name);
            }
            else
            {
                m_Objects.OrderBy(x => x.Name);
            }
            m_sortByName = !m_sortByName;
            Sort();
        }
        public void SortByData()
        {
            if (m_sortByData)
            {
                m_Objects.OrderByDescending(x => x.Data.Count);
            }
            else
            {
                m_Objects.OrderBy(x => x.Data.Count);
            }
            m_sortByData = !m_sortByData;
            Sort();
        }
        #endregion
    }
}
