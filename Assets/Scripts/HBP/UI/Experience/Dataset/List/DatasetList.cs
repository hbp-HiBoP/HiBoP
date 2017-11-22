using d = HBP.Data.Experience.Dataset;
using System.Linq;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetList : Tools.Unity.Lists.SelectableListWithItemAction<d.Dataset>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Protocol, DescendingProtocol, Data, DescendingData }
        OrderBy m_OrderBy = OrderBy.None;

        public enum Sorting { Ascending, Descending }
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_ProtocolSortingDisplayer;
        public SortingDisplayer m_DataSortingDisplayer;
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
            ApplySort();
            m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByProtocol(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Protocol.Name).ToList();
                    m_OrderBy = OrderBy.Protocol;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Protocol.Name).ToList();
                    m_OrderBy = OrderBy.DescendingProtocol;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            ApplySort();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByProtocol()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingProtocol: SortByProtocol(Sorting.Ascending); break;
                default: SortByProtocol(Sorting.Descending); break;
            }
        }
        public void SortByData(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Data.Length).ToList();
                    m_OrderBy = OrderBy.Data;
                    m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Data.Length).ToList();
                    m_OrderBy = OrderBy.DescendingData;
                    m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            ApplySort();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByData()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingData: SortByData(Sorting.Ascending); break;
                default: SortByData(Sorting.Descending); break;
            }
        }
        #endregion
    }
}
