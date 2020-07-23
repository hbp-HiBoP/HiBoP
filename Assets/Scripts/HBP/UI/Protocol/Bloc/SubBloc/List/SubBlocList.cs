using System.Linq;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// List to display subBlocs.
    /// </summary>
    public class SubBlocList : ActionableList<Data.Experience.Protocol.SubBloc>
    {
        #region Properties
        enum OrderBy { None,
            Name, DescendingName,
            Order, DescendingOrder,
            Events, DescendingEvents,
            Icons, DescendingIcons,
            Treatments, DescendingTreatments,
            StartWindow, DescendingStartWindow,
            EndWindow, DescendingEndWindow,
            Type, DescendingType
        }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_OrderSortingDisplayer;

        [SerializeField] SortingDisplayer m_StartWindowSortingDisplayer;
        [SerializeField] SortingDisplayer m_EndWindowSortingDisplayer;

        [SerializeField] SortingDisplayer m_EventsSortingDisplayer;
        [SerializeField] SortingDisplayer m_IconsSortingDisplayer;
        [SerializeField] SortingDisplayer m_TreatmentsSortingDisplayer;
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
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by position.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByOrder(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Order).ToList();
                    m_OrderBy = OrderBy.Order;
                    m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Order).ToList();
                    m_OrderBy = OrderBy.DescendingOrder;
                    m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by position.
        /// </summary>
        public void SortByOrder()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingOrder: SortByOrder(Sorting.Ascending); break;
                default: SortByOrder(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by start window.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByStartWindow(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Window.Start).ToList();
                    m_OrderBy = OrderBy.StartWindow;
                    m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Window.Start).ToList();
                    m_OrderBy = OrderBy.DescendingStartWindow;
                    m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by image.
        /// </summary>
        public void SortByStartWindow()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingStartWindow: SortByStartWindow(Sorting.Ascending); break;
                default: SortByStartWindow(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by end window.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByEndWindow(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Window.End).ToList();
                    m_OrderBy = OrderBy.StartWindow;
                    m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Window.End).ToList();
                    m_OrderBy = OrderBy.DescendingStartWindow;
                    m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by end window.
        /// </summary>
        public void SortByEndWindow()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingStartWindow: SortByEndWindow(Sorting.Ascending); break;
                default: SortByEndWindow(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by events.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByEvents(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Events.Count()).ToList();
                    m_OrderBy = OrderBy.Events;
                    m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Events.Count()).ToList();
                    m_OrderBy = OrderBy.DescendingEvents;
                    m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by events.
        /// </summary>
        public void SortByEvents()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingEvents: SortByEvents(Sorting.Ascending); break;
                default: SortByEvents(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by icons.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByIcons(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Icons.Count()).ToList();
                    m_OrderBy = OrderBy.Icons;
                    m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Icons.Count()).ToList();
                    m_OrderBy = OrderBy.DescendingIcons;
                    m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by icons.
        /// </summary>
        public void SortByIcons()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingIcons: SortByIcons(Sorting.Ascending); break;
                default: SortByIcons(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by treatments.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByTreatments(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Treatments.Count()).ToList();
                    m_OrderBy = OrderBy.Icons;
                    m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Treatments.Count()).ToList();
                    m_OrderBy = OrderBy.DescendingIcons;
                    m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by treatments.
        /// </summary>
        public void SortByTreatments()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingTreatments: SortByTreatments(Sorting.Ascending); break;
                default: SortByTreatments(Sorting.Descending); break;
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
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Type).ToList();
                    m_OrderBy = OrderBy.Type;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Type).ToList();
                    m_OrderBy = OrderBy.DescendingType;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StartWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EndWindowSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EventsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_IconsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TreatmentsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}