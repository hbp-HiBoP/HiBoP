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
                m_objects.OrderByDescending(x => x.Name);
            }
            else
            {
                m_objects.OrderBy(x => x.Name);
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }
        public void SortByData()
        {
            if (m_sortByData)
            {
                m_objects.OrderByDescending(x => x.Data.Count);
            }
            else
            {
                m_objects.OrderBy(x => x.Data.Count);
            }
            m_sortByData = !m_sortByData;
            ApplySort();
        }
        #endregion
    }
}
