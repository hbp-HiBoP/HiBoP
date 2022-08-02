using System.Linq;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// List to display blocs.
    /// </summary>
    public class BlocList : Tools.Unity.Lists.ActionableList<Core.Data.Bloc>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Order, DescendingOrder , SubBlocs, DescendingSubBlocs, Image, DescendingImage }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_OrderSortingDisplayer;
        [SerializeField] SortingDisplayer m_SubBlocsSortingDisplayer;
        [SerializeField] SortingDisplayer m_ImageSortingDisplayer;
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
            m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
            m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort vy sub-blocs.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortBySubBlocs(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.SubBlocs.Count()).ToList();
                    m_OrderBy = OrderBy.SubBlocs;
                    m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.SubBlocs.Count()).ToList();
                    m_OrderBy = OrderBy.DescendingSubBlocs;
                    m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by sub-blocs.
        /// </summary>
        public void SortBySubBlocs()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingSubBlocs: SortBySubBlocs(Sorting.Ascending); break;
                default: SortBySubBlocs(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by image.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByImage(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.IllustrationPath).ToList();
                    m_OrderBy = OrderBy.Image;
                    m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.IllustrationPath).ToList();
                    m_OrderBy = OrderBy.DescendingImage;
                    m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by image.
        /// </summary>
        public void SortByImage()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingImage: SortByImage(Sorting.Ascending); break;
                default: SortByImage(Sorting.Descending); break;
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
            m_SubBlocsSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImageSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}