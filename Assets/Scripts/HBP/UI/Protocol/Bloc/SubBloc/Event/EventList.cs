using System.Linq;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
	/// <summary>
	/// List to display events.
	/// </summary>
	public class EventList : Tools.Unity.Lists.ActionableList<Data.Experience.Protocol.Event> 
	{
        #region Properties
        enum OrderBy { None, Name, DescendingName, Code, DescendingCode, Type, DescendingType }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_CodeSortingDisplayer;
        [SerializeField] SortingDisplayer m_TypeSortingDisplayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Sort by name.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by name.
        /// </summary>
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by code.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByCode(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Codes.Count).ToList();
                    m_OrderBy = OrderBy.Code;
                    m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Codes.Count).ToList();
                    m_OrderBy = OrderBy.DescendingCode;
                    m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by code.
        /// </summary>
		public void SortByCode()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingCode: SortByCode(Sorting.Ascending); break;
                default: SortByCode(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by type.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByType(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Type).ToList();
                    m_OrderBy = OrderBy.Type;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Type).ToList();
                    m_OrderBy = OrderBy.DescendingType;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by type.
        /// </summary>
        public void SortByType()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingType: SortByType(Sorting.Ascending); break;
                default: SortByType(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;

            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_CodeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}