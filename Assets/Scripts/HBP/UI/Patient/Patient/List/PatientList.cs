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
                case OrderBy.DescendingMesh:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.Meshes.FindAll(m => m.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Mesh;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Meshes.FindAll(m => m.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingMesh;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByMRI()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMRI:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.MRIs.FindAll(m => m.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.MRI;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.MRIs.FindAll(m => m.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingMRI;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByImplantation()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingImplantation:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Implantations.FindAll(i => i.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Implantation;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Implantations.FindAll(i => i.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingImplantation;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByTransformation()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingTransformation:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.Transformations.FindAll(t => t.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Transformation;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Transformations.FindAll(t => t.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingTransformation;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByConnectivity()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingConnectivity:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Brain.Connectivities.FindAll(c => c.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Connectivity;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Brain.Connectivities.FindAll(c => c.isUsable).Count).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingConnectivity;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}
