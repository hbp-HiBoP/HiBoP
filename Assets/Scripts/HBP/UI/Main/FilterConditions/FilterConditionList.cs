using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Linq;
using System.Security.Policy;
using UnityEngine;

namespace HBP.UI.Main
{
    public class FilterConditionList : ActionableList<BaseFilterCondition>
    {
        #region Properties

        enum OrderBy
        {
            None,
            Description,
            DescendingDescription
        }

        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_DescriptionSortingDisplayer;

        #endregion

        #region Public Methods

        protected override void AddObject(BaseFilterCondition filterCondition)
        {
            SortByNone();
            base.AddObject(filterCondition);
        }

        #endregion

        #region SortingMethods

        public void SortByDescription(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Description).ToList();
                    m_OrderBy = OrderBy.Description;
                    m_DescriptionSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Description).ToList();
                    m_OrderBy = OrderBy.DescendingDescription;
                    m_DescriptionSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
        }

        public void SortByDescription()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDescription: SortByDescription(Sorting.Ascending); break;
                default: SortByDescription(Sorting.Descending); break;
            }
        }

        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_DescriptionSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        #endregion
    }
}
