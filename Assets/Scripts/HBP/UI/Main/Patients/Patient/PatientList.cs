using HBP.UI.Tools.Lists;
using System.Linq;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
	/// <summary>
	/// List to display patients.
	/// </summary>
	public class PatientList : ActionableList<Core.Data.Patient>
	{
        #region Properties
        enum OrderBy { None, Name, DescendingName, Place, DescendingPlace, Date, DescendingDate, Mesh, DescendingMesh, MRI, DescendingMRI, Site, DescendingSite, Tag, DescendingTag }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_PlaceSortingDisplayer;
        [SerializeField] SortingDisplayer m_DateSortingDisplayer;
        [SerializeField] SortingDisplayer m_MeshSortingDisplayer;
        [SerializeField] SortingDisplayer m_MRISortingDisplayer;
        [SerializeField] SortingDisplayer m_SiteSortingDisplayer;
        [SerializeField] SortingDisplayer m_TagSortingDisplayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add patient to the list.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Add(Core.Data.Patient obj)
        {
            SortByNone();
            return base.Add(obj);
        }

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
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
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
        /// Sort by place.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByPlace(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Place).ToList();
                    m_OrderBy = OrderBy.Place;
                    m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Place).ToList();
                    m_OrderBy = OrderBy.DescendingPlace;
                    m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by place.
        /// </summary>
        public void SortByPlace()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPlace: SortByPlace(Sorting.Ascending); break;
                default: SortByPlace(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by date.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByDate(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Date).ToList();
                    m_OrderBy = OrderBy.Date;
                    m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Date).ToList();
                    m_OrderBy = OrderBy.DescendingDate;
                    m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by date.
        /// </summary>
        public void SortByDate()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDate: SortByDate(Sorting.Ascending); break;
                default: SortByDate(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by mesh.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByMesh(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Meshes.FindAll(m => m.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.Mesh;
                    m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Meshes.FindAll(m => m.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.DescendingMesh;
                    m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by mesh.
        /// </summary>
        public void SortByMesh()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMesh: SortByMesh(Sorting.Ascending); break;
                default: SortByMesh(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by MRI.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByMRI(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.MRIs.FindAll(m => m.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.MRI;
                    m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.MRIs.FindAll(m => m.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.DescendingMRI;
                    m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
                default:
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by MRI.
        /// </summary>
        public void SortByMRI()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMRI: SortByMRI(Sorting.Ascending); break;
                default: SortByMRI(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by number of sites.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortBySite(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Sites.Count).ToList();
                    m_OrderBy = OrderBy.Site;
                    m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Sites.Count).ToList();
                    m_OrderBy = OrderBy.DescendingSite;
                    m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by site.
        /// </summary>
        public void SortBySite()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingSite: SortBySite(Sorting.Ascending); break;
                default: SortBySite(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by number of tags.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByTag(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Tags.Count).ToList();
                    m_OrderBy = OrderBy.Tag;
                    m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Tags.Count).ToList();
                    m_OrderBy = OrderBy.DescendingTag;
                    m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by tag.
        /// </summary>
        public void SortByTag()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingSite: SortByTag(Sorting.Ascending); break;
                default: SortByTag(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_SiteSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TagSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderBy = OrderBy.None;
        }
        #endregion
    }
}
