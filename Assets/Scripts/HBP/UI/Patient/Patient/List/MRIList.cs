using System.Linq;

namespace HBP.UI.Anatomy
{
    public class MRIList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Anatomy.MRI>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Path, DescendingPath }
        OrderBy m_OrderBy = OrderBy.None;
        public enum Sorting { Ascending, Descending }
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PathSortingDisplayer;
        #endregion

        #region SortingMethods
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByPath(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.HasMRI).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Path;
                    m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.HasMRI).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPath;
                    m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPath()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPath: SortByPath(Sorting.Ascending); break;
                default: SortByPath(Sorting.Descending); break;
            }
        }
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}