using HBP.Data.Anatomy;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI.Anatomy
{
    public class ImplantationList : SelectableListWithItemAction<Implantation>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Path, DescendingPath }
        OrderBy m_OrderBy = OrderBy.None;
        public enum Sorting { Ascending, Descending}
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PathSortingDisplayer;
        public SortingDisplayer m_MarsAtlasSortingDisplayer;
        #endregion

        #region SortingMethods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPath()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPath: SortByPath(Sorting.Ascending); break;
                default: SortByPath(Sorting.Descending); break;
            }
        }
        public void SortByPath(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.File).ToList();
                    m_OrderBy = OrderBy.Path;
                    m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.File).ToList();
                    m_OrderBy = OrderBy.DescendingPath;
                    m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByMarsAtlas()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPath: SortByMarsAtlas(Sorting.Ascending); break;
                default: SortByMarsAtlas(Sorting.Descending); break;
            }
        }
        public void SortByMarsAtlas(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.MarsAtlas).ToList();
                    m_OrderBy = OrderBy.Path;
                    m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.MarsAtlas).ToList();
                    m_OrderBy = OrderBy.DescendingPath;
                    m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PathSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}