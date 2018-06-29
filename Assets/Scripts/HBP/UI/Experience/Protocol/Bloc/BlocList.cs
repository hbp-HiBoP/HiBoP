using System.Linq;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class BlocList : Tools.Unity.Lists.SelectableListWithItemAction<d.Bloc>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Position, DescendingPosition , SubBlocs, DescendingSubBlocs, Image, DescendingImage }
        OrderBy m_OrderBy = OrderBy.None;

        public enum Sorting { Ascending, Descending }
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PositionSortingDisplayer;
        public SortingDisplayer m_SubBlocsSortingDisplayer;
        public SortingDisplayer m_ImageSortingDisplayer;
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
            m_PositionSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByPosition(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Position).ToList();
                    m_OrderBy = OrderBy.Position;
                    m_PositionSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Position).ToList();
                    m_OrderBy = OrderBy.DescendingPosition;
                    m_PositionSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPosition()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPosition: SortByPosition(Sorting.Ascending); break;
                default: SortByPosition(Sorting.Descending); break;
            }
        }
        public void SortBySubBlocs(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.SubBlocs.Count()).ToList();
                    m_OrderBy = OrderBy.SubBlocs;
                    m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.SubBlocs.Count()).ToList();
                    m_OrderBy = OrderBy.DescendingSubBlocs;
                    m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PositionSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortBySubBlocs()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingSubBlocs: SortBySubBlocs(Sorting.Ascending); break;
                default: SortBySubBlocs(Sorting.Descending); break;
            }
        }
        public void SortByImage(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.IllustrationPath).ToList();
                    m_OrderBy = OrderBy.Image;
                    m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.IllustrationPath).ToList();
                    m_OrderBy = OrderBy.DescendingImage;
                    m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PositionSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByImage()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingImage: SortByImage(Sorting.Ascending); break;
                default: SortByImage(Sorting.Descending); break;
            }
        }
        #endregion
    }

}