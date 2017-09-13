using d = HBP.Data.Experience.Protocol;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class ProtocolList : Tools.Unity.Lists.SelectableListWithItemAction<d.Protocol>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Blocs, DescendingBlocs }
        OrderBy m_OrderBy = OrderBy.None;

        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_BlocsSortingDisplayer;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_BlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByBlocs()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingBlocs:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Blocs.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Blocs;
                    m_BlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Blocs.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingBlocs;
                    m_BlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}
