using UnityEngine;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI
{
    /// <summary>
    /// List to display Alias.
    /// </summary>
    public class AliasList : ActionableList<Data.Alias>
    {
        #region Properties
        enum OrderBy { None, Key, DescendingKey, Value, DescendingValue }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_KeySortingDisplayer;
        [SerializeField] SortingDisplayer m_ValueSortingDisplayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add alias.
        /// </summary>
        /// <param name="objectToAdd">Alias to add</param>
        /// <returns>True if end without errors, False otherwise</returns>
        public override bool Add(Data.Alias objectToAdd)
        {
            SortByNone();
            return base.Add(objectToAdd);
        }
        #endregion

        #region Sorting Methods
        /// <summary>
        /// Sort by key.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByKey(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Key).ToList();
                    m_OrderBy = OrderBy.Key;
                    m_KeySortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Key).ToList();
                    m_OrderBy = OrderBy.DescendingKey;
                    m_KeySortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by key.
        /// </summary>
        public void SortByKey()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingKey: SortByKey(Sorting.Ascending); break;
                default: SortByKey(Sorting.Descending); break;
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
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Value).ToList();
                    m_OrderBy = OrderBy.Value;
                    m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Value).ToList();
                    m_OrderBy = OrderBy.DescendingValue;
                    m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_KeySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
            m_KeySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ValueSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}