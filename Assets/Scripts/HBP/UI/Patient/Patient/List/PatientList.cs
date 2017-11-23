using System.Linq;

namespace HBP.UI.Anatomy
{
	/// <summary>
	/// Manage patient list.
	/// </summary>
	public class PatientList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Patient>
	{
        #region Properties
        enum OrderBy { None, Name, DescendingName, Place, DescendingPlace, Date, DescendingDate, Mesh, DescendingMesh, MRI, DescendingMRI, Implantation, DescendingImplantation, Connectivity, DescendingConnectivity }
        OrderBy m_OrderBy = OrderBy.None;
        public enum Sorting { Ascending, Descending }
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PlaceSortingDisplayer;
        public SortingDisplayer m_DateSortingDisplayer;
        public SortingDisplayer m_MeshSortingDisplayer;
        public SortingDisplayer m_MRISortingDisplayer;
        public SortingDisplayer m_ImplantationSortingDisplayer;
        public SortingDisplayer m_ConnectivitySortingDisplayer;
        #endregion

        #region SortingMethods
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
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
        public void SortByPlace(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Place).ToList();
                    m_OrderBy = OrderBy.Place;
                    m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Place).ToList();
                    m_OrderBy = OrderBy.DescendingPlace;
                    m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPlace()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPlace: SortByPlace(Sorting.Ascending); break;
                default: SortByPlace(Sorting.Descending); break;
            }
        }
        public void SortByDate(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Date).ToList();
                    m_OrderBy = OrderBy.Date;
                    m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Date).ToList();
                    m_OrderBy = OrderBy.DescendingDate;
                    m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByDate()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDate: SortByDate(Sorting.Ascending); break;
                default: SortByDate(Sorting.Descending); break;
            }
        }
        public void SortByMesh(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Brain.Meshes.FindAll(m => m.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.Mesh;
                    m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Brain.Meshes.FindAll(m => m.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.DescendingMesh;
                    m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByMesh()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMesh: SortByMesh(Sorting.Ascending); break;
                default: SortByMesh(Sorting.Descending); break;
            }
        }
        public void SortByMRI(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Brain.MRIs.FindAll(m => m.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.MRI;
                    m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Brain.MRIs.FindAll(m => m.WasUsable).Count).ToList();
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
            m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByMRI()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMRI: SortByMRI(Sorting.Ascending); break;
                default: SortByMRI(Sorting.Descending); break;
            }
        }
        public void SortByImplantation(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Brain.Implantations.FindAll(i => i.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.Implantation;
                    m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Brain.Implantations.FindAll(i => i.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.DescendingImplantation;
                    m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByImplantation()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingImplantation: SortByImplantation(Sorting.Ascending); break;
                default: SortByImplantation(Sorting.Descending); break;
            }
        }
        public void SortByConnectivity(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Brain.Connectivities.FindAll(c => c.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.Connectivity;
                    m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Brain.Connectivities.FindAll(c => c.WasUsable).Count).ToList();
                    m_OrderBy = OrderBy.DescendingConnectivity;
                    m_ConnectivitySortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
                default:
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MRISortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ImplantationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByConnectivity()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingConnectivity: SortByConnectivity(Sorting.Ascending); break;
                default: SortByConnectivity(Sorting.Descending); break;
            }

        }
        #endregion
    }
}
