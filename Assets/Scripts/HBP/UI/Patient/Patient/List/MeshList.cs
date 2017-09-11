using System.Linq;


namespace HBP.UI.Anatomy
{
    public class MeshList : Tools.Unity.Lists.SelectableListWithSave<Data.Anatomy.Mesh>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Type, DescendingType }
        OrderBy m_OrderBy = OrderBy.None;

        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_TypeSortingDisplayer;
        #endregion

        #region SortingMethods
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
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByType()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingType:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.GetType().ToString()).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Type;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.GetType().ToString()).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingType;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;

                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}