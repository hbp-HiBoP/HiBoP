using d = HBP.Data.Experience.Protocol;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class ProtocolList : Tools.Unity.Lists.SelectableListWithItemAction<d.Protocol>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Blocs, DescendingBlocs }
        OrderBy m_OrderBy = OrderBy.None;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByBlocs()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Blocs:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Blocs.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingBlocs;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Blocs.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Blocs;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}
