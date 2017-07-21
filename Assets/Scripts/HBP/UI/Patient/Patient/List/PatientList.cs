using System.Linq;

namespace HBP.UI.Anatomy
{
	/// <summary>
	/// Manage patient list.
	/// </summary>
	public class PatientList : Tools.Unity.Lists.SelectableListWithItemAction<Data.Patient>
	{
        #region Properties
        bool m_sortByName, m_sortByPlace, m_sortByDate, m_sortByMesh, m_sortByMRI, m_sortByImplantation, m_sortByTransformation, m_sortByConnectivity;
        #endregion

        #region SortingMethods
        public void SortByName()
        {
            if(!m_sortByName)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Name).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Name).ToList();
            }
            m_sortByName = !m_sortByName;
            m_sortByPlace = false;
            m_sortByDate = false;
            m_sortByMesh = false;
            m_sortByMRI = false;
            m_sortByImplantation = false;
            m_sortByTransformation = false;
            Sort();
        }
        public void SortByPlace()
        {
            if (!m_sortByPlace)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Place).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Place).ToList();
            }
            m_sortByPlace = !m_sortByPlace;
            m_sortByName = false;
            m_sortByDate = false;
            m_sortByMesh = false;
            m_sortByMRI = false;
            m_sortByImplantation = false;
            m_sortByTransformation = false;
            Sort();
        }
        public void SortByDate()
        {
            if (!m_sortByDate)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Date).ToList();
            }
            m_sortByDate = !m_sortByDate;
            m_sortByPlace = false;
            m_sortByName = false;
            m_sortByMesh = false;
            m_sortByMRI = false;
            m_sortByImplantation = false;
            m_sortByTransformation = false;
            Sort();
        }
        public void SortByMesh()
        {
            if (!m_sortByMesh)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Brain.Meshes.Count).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Brain.Meshes.Count).ToList();
            }
            m_sortByMesh = !m_sortByMesh;
            m_sortByPlace = false;
            m_sortByDate = false;
            m_sortByName = false;
            m_sortByMRI = false;
            m_sortByImplantation = false;
            m_sortByTransformation = false;
            Sort();
        }
        public void SortByMRI()
        {
            if (!m_sortByMRI)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Brain.MRIs.Count).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Brain.MRIs.Count).ToList();
            }
            m_sortByMRI = !m_sortByMRI;
            m_sortByPlace = false;
            m_sortByDate = false;
            m_sortByMesh = false;
            m_sortByName = false;
            m_sortByImplantation = false;
            m_sortByTransformation = false;
            Sort();
        }
        public void SortByImplantation()
        {
            if (!m_sortByImplantation)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Brain.Implantations.Count).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Brain.Implantations.Count).ToList();
            }
            m_sortByImplantation = !m_sortByImplantation;
            m_sortByPlace = false;
            m_sortByDate = false;
            m_sortByMesh = false;
            m_sortByMRI = false;
            m_sortByName = false;
            m_sortByTransformation = false;
            Sort();
        }
        public void SortByTransformation()
        {
            if (!m_sortByTransformation)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Brain.Transformations.Count).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Brain.Transformations.Count).ToList();
            }
            m_sortByTransformation = !m_sortByTransformation;
            m_sortByPlace = false;
            m_sortByDate = false;
            m_sortByMesh = false;
            m_sortByMRI = false;
            m_sortByImplantation = false;
            m_sortByName = false;
            Sort();
        }
        public void SortByConnectivity()
        {
            if (!m_sortByConnectivity)
            {
                m_Objects = m_Objects.OrderByDescending(x => x.Brain.Connectivities.Count).ToList();
            }
            else
            {
                m_Objects = m_Objects.OrderBy(x => x.Brain.Connectivities.Count).ToList();
            }
            m_sortByConnectivity = !m_sortByConnectivity;
            m_sortByPlace = false;
            m_sortByDate = false;
            m_sortByMesh = false;
            m_sortByMRI = false;
            m_sortByImplantation = false;
            m_sortByName = false;
            m_sortByTransformation = false;
            Sort();
        }
        #endregion
    }
}
