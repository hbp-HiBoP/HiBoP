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
                m_objects.OrderByDescending(x => x.Name);
            }
            else
            {
                m_objects.OrderBy(x => x.Name);
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }
        public void SortByBlocs()
        {
            if (m_sortByBlocs)
            {
                m_objects.OrderByDescending(x => x.Blocs.Count);
            }
            else
            {
                m_objects.OrderBy(x => x.Blocs.Count);
            }
            m_sortByBlocs = !m_sortByBlocs;
            ApplySort();
        }
        #endregion
    }
}
