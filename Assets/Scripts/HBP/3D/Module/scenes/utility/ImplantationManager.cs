using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Class responsible for managing the implantations of the scene
    /// </summary>
    /// <remarks>
    /// This class can load and store implantations for the corresponding scene.
    /// It is also used to select which implantation to display on the scene.
    /// </remarks>
    public class ImplantationManager : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent scene of the manager
        /// </summary>
        [SerializeField] private Base3DScene m_Scene;
        /// <summary>
        /// Component containing references to GameObjects of the 3D scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        /// <summary>
        /// List of the implantation3Ds of the scene
        /// </summary>
        public List<Implantation3D> Implantations { get; } = new List<Implantation3D>();
        /// <summary>
        /// Selected implantation3D ID
        /// </summary>
        public int SelectedImplantationID { get; set; }
        /// <summary>
        /// Selected implantation3D
        /// </summary>
        public Implantation3D SelectedImplantation
        {
            get
            {
                return Implantations[SelectedImplantationID];
            }
        }
        #endregion

        #region Private Methods
        private void OnDestroy()
        {
            foreach (var implantation in Implantations)
            {
                implantation.Clean();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a new implantation to the manager
        /// </summary>
        /// <param name="name">Name of the implantation</param>
        /// <param name="pts">List of pts files (one by patient)</param>
        /// <param name="marsAtlas">List of Mars Atlas files (one by patient)</param>
        /// <param name="patientIDs">List of patients IDs</param>
        public void Add(string name, IEnumerable<string> pts, IEnumerable<string> marsAtlas, IEnumerable<string> patientIDs)
        {
            Implantation3D implantation3D = new Implantation3D(name, pts, marsAtlas, patientIDs);
            if (implantation3D.IsLoaded)
            {
                Implantations.Add(implantation3D);
            }
            else
            {
                throw new CanNotLoadImplantation(name);
            }
        }
        /// <summary>
        /// Set the implantation to be used
        /// </summary>
        /// <param name="implantationName">Name of the implantation to use</param>
        public void Select(string implantationName)
        {
            int implantationID = Implantations.FindIndex(i => i.Name == implantationName);
            SelectedImplantationID = implantationID > 0 ? implantationID : 0;
            m_DisplayedObjects.InstantiateImplantation(SelectedImplantation);
            
            // reset selected site
            for (int ii = 0; ii < m_Scene.Columns.Count; ++ii)
            {
                m_Scene.Columns[ii].UnselectSite();
            }

            m_Scene.ResetIEEG();
            foreach (Column3D column in m_Scene.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
        }
        #endregion
    }
}