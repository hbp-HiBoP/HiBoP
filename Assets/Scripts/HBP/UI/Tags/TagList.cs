using UnityEngine;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI
{
    public class TagList : SelectableListWithItemAction<Data.BaseTag>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Type, DescendingType }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_TypeSortingDisplayer;
        #endregion

        #region Public Methods
        public override bool Add(Data.BaseTag objectToAdd)
        {
            SortByNone();
            return base.Add(objectToAdd);
        }
        #endregion

        #region SortingMethods
        /// <summary>
        /// Sort by name.
        /// </summary>
        /// <param name="sorting">Sorting</param>
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
        /// Sort by mesh.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByType(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.Type;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.DescendingType;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by mesh.
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
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}