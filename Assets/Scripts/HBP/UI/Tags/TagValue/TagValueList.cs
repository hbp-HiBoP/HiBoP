using System.Linq;
using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI
{
    /// <summary>
    /// List to display TagValues.
    /// </summary>
    public class TagValueList : ActionableList<Core.Data.BaseTagValue>
    {
        #region Properties
        enum OrderBy { None, Tag, DescendingTag, Value, DescendingValue }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_TagSortingDisplayer;
        [SerializeField] SortingDisplayer m_ValueSortingDisplayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add TagValue.
        /// </summary>
        /// <param name="objectToAdd">TagValue to add</param>
        /// <returns>True if end without error, False otherwise</returns>
        public override bool Add(Core.Data.BaseTagValue objectToAdd)
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
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Tag.Name).ToList();
                    m_OrderBy = OrderBy.Tag;
                    m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Tag.Name).ToList();
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
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.Value;
                    m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.GetType().ToString()).ToList();
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