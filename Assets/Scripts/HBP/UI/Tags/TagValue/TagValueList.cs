using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class TagValueList : ActionableList<Data.BaseTagValue>
    {
        #region Properties
        enum OrderBy { None, Tag, DescendingTag, Value, DescendingValue }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_TagSortingDisplayer;
        [SerializeField] SortingDisplayer m_ValueSortingDisplayer;

        #endregion

        #region Public Methods
        public override bool Add(Data.BaseTagValue objectToAdd)
        {
            SortByNone();
            return base.Add(objectToAdd);
        }
        #endregion

        #region SortingMethods
        /// <summary>
        /// Sort by tag.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByTag(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Tag.Name).ToList();
                    m_OrderBy = OrderBy.Tag;
                    m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Tag.Name).ToList();
                    m_OrderBy = OrderBy.DescendingTag;
                    m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by tag.
        /// </summary>
        public void SortByTag()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingTag: SortByTag(Sorting.Ascending); break;
                default: SortByTag(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by value.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByValue(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.Value;
                    m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.DescendingValue;
                    m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by value.
        /// </summary>
        public void SortByValue()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingValue: SortByValue(Sorting.Ascending); break;
                default: SortByValue(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}