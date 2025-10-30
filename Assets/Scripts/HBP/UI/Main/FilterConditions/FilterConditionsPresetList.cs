using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Main
{
    public class FilterConditionsPresetList : ActionableList<FilterConditionsPreset>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Conditions, DescendingConditions }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_ConditionsSortingDisplayer;
        #endregion

        #region Private Methods
        /// <summary>
        /// Add alias.
        /// </summary>
        /// <param name="objectToAdd">Alias to add</param>
        /// <returns>True if end without errors, False otherwise</returns>
        protected override void AddObject(FilterConditionsPreset objectToAdd)
        {
            SortByNone();
            base.AddObject(objectToAdd);
        }
        #endregion

        #region Sorting Methods
        /// <summary>
        /// Sort by key.
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
            m_ConditionsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by key.
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
        /// Sort by value.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByConditions(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Conditions.Count).ToList();
                    m_OrderBy = OrderBy.Conditions;
                    m_ConditionsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Conditions.Count).ToList();
                    m_OrderBy = OrderBy.DescendingConditions;
                    m_ConditionsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by value.
        /// </summary>
        public void SortByConditions()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingConditions: SortByConditions(Sorting.Ascending); break;
                default: SortByConditions(Sorting.Descending); break;
            }
        }
        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ConditionsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}