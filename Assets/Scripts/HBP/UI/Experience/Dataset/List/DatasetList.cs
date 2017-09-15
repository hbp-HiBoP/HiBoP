using d = HBP.Data.Experience.Dataset;
using System.Linq;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetList : Tools.Unity.Lists.SelectableListWithItemAction<d.Dataset>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Data, DescendingData }
        OrderBy m_OrderBy = OrderBy.None;

        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_DataSortingDisplayer;
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
            m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByData()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingData:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Data.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Data;
                    m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Data.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingData;
                    m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}
