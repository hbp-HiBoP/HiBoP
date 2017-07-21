using d = HBP.Data.Experience.Protocol;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class ProtocolList : Tools.Unity.Lists.SelectableListWithItemAction<d.Protocol>
    {
        #region Properties
        bool m_sortByName = false;
        bool m_sortByBlocs = false;
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
        public void SortByBlocs()
        {
            if (m_sortByBlocs)
            {
                m_Objects.OrderByDescending(x => x.Blocs.Count);
            }
            else
            {
                m_Objects.OrderBy(x => x.Blocs.Count);
            }
            m_sortByBlocs = !m_sortByBlocs;
            Sort();
        }
        #endregion
    }
}
