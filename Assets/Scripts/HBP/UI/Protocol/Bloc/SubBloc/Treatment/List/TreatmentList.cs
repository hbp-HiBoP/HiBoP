using Tools.Unity.Lists;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentList : ActionableList<d.Treatment>
    {
        #region Properties
        enum OrderBy { None, Type, DescendingType, StartWindow, DescendingStartWindow, EndWindow, DescendingEndWindow, Order, DescendingOrder }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_TypeSortingDisplayer;
        [SerializeField] SortingDisplayer m_StartWindowSortingDisplayer;
        [SerializeField] SortingDisplayer m_EndWindowSortingDisplayer;
        [SerializeField] SortingDisplayer m_OrderSortingDisplayer;
        #endregion

        #region Public Methods
        public void SortByType(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.GetType().ToString()).ToList();
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Type;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.GetType().ToString()).ToList();
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingType;
                    break;
            }
            Refresh();
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByType()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingType: SortByType(Sorting.Ascending); break;
                default: SortByType(Sorting.Descending); break;
            }
        }

        public void SortByStartWindow(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Window.Start).ToList();
                    m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.StartWindow;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Window.Start).ToList();
                    m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingStartWindow;
                    break;
            }
            Refresh();
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByStartWindow()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingStartWindow: SortByStartWindow(Sorting.Ascending); break;
                default: SortByStartWindow(Sorting.Descending); break;
            }
        }


        public void SortByEndWindow(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Window.End).ToList();
                    m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.EndWindow;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Window.End).ToList();
                    m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingEndWindow;
                    break;
            }
            Refresh();
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByEndWindow()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingEndWindow: SortByEndWindow(Sorting.Ascending); break;
                default: SortByEndWindow(Sorting.Descending); break;
            }
        }

        public void SortByOrder(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Order).ToList();
                    m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    m_OrderBy = OrderBy.Order;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Order).ToList();
                    m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    m_OrderBy = OrderBy.DescendingOrder;
                    break;
            }
            Refresh();
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByOrder()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingOrder: SortByOrder(Sorting.Ascending); break;
                default: SortByOrder(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}