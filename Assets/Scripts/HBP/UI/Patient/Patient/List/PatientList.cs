using System.Linq;

namespace HBP.UI.Anatomy
{
	/// <summary>
	/// Manage patient list.
	/// </summary>
	public class PatientList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Patient>
	{
        #region Properties
        enum OrderBy { None, Name, DescendingName, Place, DescendingPlace, Date, DescendingDate, Mesh, DescendingMesh, MRI, DescendingMRI, Implantation, DescendingImplantation, Transformation, DescendingTransformation, Connectivity, DescendingConnectivity }
        OrderBy m_OrderBy = OrderBy.None;
        #endregion

        #region SortingMethods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByPlace()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Place:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Place).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPlace;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Place).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Place;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByDate()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Date:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Date).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingDate;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Date).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Date;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByMesh()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Mesh:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Meshes.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingMesh;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.Meshes.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Mesh;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByMRI()
        {
            switch (m_OrderBy)
            {
                case OrderBy.MRI:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.MRIs.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingMRI;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.MRIs.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.MRI;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByImplantation()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Implantation:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Implantations.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingImplantation;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.Implantations.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Implantation;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByTransformation()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Transformation:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Transformations.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingTransformation;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.Transformations.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Transformation;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByConnectivity()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Connectivity:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Connectivities.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingConnectivity;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.Connectivities.Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Connectivity;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}
