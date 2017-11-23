using System.Linq;

namespace HBP.UI.Experience.Protocol
{
	/// <summary>
	/// The scripts which manage the event list.
	/// </summary>
	public class EventList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Experience.Protocol.Event> 
	{
        #region Proterties
        enum OrderBy { None, Name, DescendingName, Code, DescendingCode }
        OrderBy m_OrderBy = OrderBy.None;

        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_CodeSortingDisplayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Sort by name.
        /// </summary>
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        /// <summary>
        /// Sort by code.
        /// </summary>
		public void SortByCode()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingCode:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Codes.Min()).ToList();
                    m_OrderBy = OrderBy.Code;
                    m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Codes.Min()).ToList();
                    m_OrderBy = OrderBy.DescendingCode;
                    m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        /// <summary>
        /// Sort by type.
        /// </summary>
        public void SortByType()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingCode:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Type).ToList();
                    m_OrderBy = OrderBy.Code;
                    m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Type).ToList();
                    m_OrderBy = OrderBy.DescendingCode;
                    m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}
