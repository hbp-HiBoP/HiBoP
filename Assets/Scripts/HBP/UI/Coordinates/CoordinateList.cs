using HBP.UI.Lists;
using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// List to display coordinates.
    /// </summary>
    public class CoordinateList : ActionableList<Core.Data.Coordinate>
    {
        #region Properties
        enum OrderBy { None, ReferenceSystem, DescendingReferenceSystem, X, DescendingX, Y, DescendingY, Z, DescendingZ }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_ReferenceSystemSortingDisplayer;
        [SerializeField] SortingDisplayer m_XSortingDisplayer;
        [SerializeField] SortingDisplayer m_YSortingDisplayer;
        [SerializeField] SortingDisplayer m_ZSortingDisplayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a coordinate.
        /// </summary>
        /// <param name="obj">Coordinate to add</param>
        /// <returns>True if end without errors, False otherwise</returns>
        public override bool Add(Core.Data.Coordinate obj)
        {
            SortByNone();
            return base.Add(obj);
        }

        /// <summary>
        /// Sort by reference system.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByReferenceSystem(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.ReferenceSystem).ToList();
                    m_OrderBy = OrderBy.ReferenceSystem;
                    m_ReferenceSystemSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.ReferenceSystem).ToList();
                    m_OrderBy = OrderBy.DescendingReferenceSystem;
                    m_ReferenceSystemSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_XSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_YSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ZSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by reference system.
        /// </summary>
        public void SortByReferenceSystem()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingReferenceSystem: SortByReferenceSystem(Sorting.Ascending); break;
                default: SortByReferenceSystem(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by X.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByX(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Position.x).ToList();
                    m_OrderBy = OrderBy.X;
                    m_XSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Position.x).ToList();
                    m_OrderBy = OrderBy.DescendingX;
                    m_XSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_ReferenceSystemSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_YSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ZSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by X.
        /// </summary>
        public void SortByX()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingX: SortByX(Sorting.Ascending); break;
                default: SortByX(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by Y.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByY(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Position.y).ToList();
                    m_OrderBy = OrderBy.Y;
                    m_YSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Position.y).ToList();
                    m_OrderBy = OrderBy.DescendingY;
                    m_YSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_ReferenceSystemSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_XSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ZSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by Y.
        /// </summary>
        public void SortByY()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingY: SortByY(Sorting.Ascending); break;
                default: SortByY(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by Z.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByZ(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Position.z).ToList();
                    m_OrderBy = OrderBy.Z;
                    m_ZSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Position.z).ToList();
                    m_OrderBy = OrderBy.DescendingZ;
                    m_ZSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_ReferenceSystemSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_XSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_YSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by Z.
        /// </summary>
        public void SortByZ()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingZ: SortByY(Sorting.Ascending); break;
                default: SortByZ(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_ReferenceSystemSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_XSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_YSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderBy = OrderBy.None;
        }
        #endregion
    }
}