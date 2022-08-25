using System.Linq;
using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// List to display datasets.
    /// </summary>
	public class DatasetList : ActionableList<Core.Data.Dataset>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Protocol, DescendingProtocol, Data, DescendingData }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_ProtocolSortingDisplayer;
        [SerializeField] SortingDisplayer m_DataSortingDisplayer;
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
            m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by protocol.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByProtocol(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Protocol.Name).ToList();
                    m_OrderBy = OrderBy.Protocol;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Protocol.Name).ToList();
                    m_OrderBy = OrderBy.DescendingProtocol;
                    m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by protocol.
        /// </summary>
        public void SortByProtocol()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingProtocol: SortByProtocol(Sorting.Ascending); break;
                default: SortByProtocol(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by data.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByData(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Data.Count).ToList();
                    m_OrderBy = OrderBy.Data;
                    m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Data.Count).ToList();
                    m_OrderBy = OrderBy.DescendingData;
                    m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by data.
        /// </summary>
        public void SortByData()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingData: SortByData(Sorting.Ascending); break;
                default: SortByData(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;

            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProtocolSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DataSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}