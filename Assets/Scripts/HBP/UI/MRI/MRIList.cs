using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// List to display MRIs.
    /// </summary>
    public class MRIList : Tools.Unity.Lists.ActionableList<Data.MRI>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, HasMRI, DescendingHasMRI }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_HasMRISortingDisplayer;
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
            m_HasMRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by path.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByHasMRI(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.HasMRI).ToList();
                    m_OrderBy = OrderBy.HasMRI;
                    m_HasMRISortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.HasMRI).ToList();
                    m_OrderBy = OrderBy.DescendingHasMRI;
                    m_HasMRISortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by path.
        /// </summary>
        public void SortByHasMRI()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingHasMRI: SortByHasMRI(Sorting.Ascending); break;
                default: SortByHasMRI(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_HasMRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}