using d = HBP.Data.Experience.Protocol;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class BlocList : Tools.Unity.Lists.SelectableListWithItemAction<d.Bloc>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, NumberOfWindows, DescendingNumberOfWindows, HasImage, DescendingHasImage }
        OrderBy m_OrderBy = OrderBy.None;

        public enum Sorting { Ascending, Descending }
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_NumberOfWindowsSortingDisplayer;
        public SortingDisplayer m_HasImageSortingDisplayer;
        #endregion

        #region Public Methods
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
            m_NumberOfWindowsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByBlocs(Sorting sorting)
        {
            switch (sorting)
            {
                //case Sorting.Ascending:
                //    m_Objects = m_Objects.OrderBy((elt) => elt.Blocs.Count).ToList();
                //    m_OrderBy = OrderBy.NumberOfWindows;
                //    m_NumberOfWindowsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                //    break;
                //case Sorting.Descending:
                //    m_Objects = m_Objects.OrderByDescending((elt) => elt.Blocs.Count).ToList();
                //    m_OrderBy = OrderBy.DescendingWindows;
                //    m_NumberOfWindowsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                //    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByBlocs()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingNumberOfWindows: SortByBlocs(Sorting.Ascending); break;
                default: SortByBlocs(Sorting.Descending); break;
            }
        }
        #endregion
    }

}