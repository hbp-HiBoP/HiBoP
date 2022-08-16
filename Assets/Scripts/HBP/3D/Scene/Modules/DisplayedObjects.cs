using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Display.Module3D
{
    /// <summary>
    /// List of all the objects displayed in the scene
    /// </summary>
    public class DisplayedObjects : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent scene
        /// </summary>
        [SerializeField] private Base3DScene m_Scene;

        [SerializeField] private Transform m_BrainSurfaceMeshesParent;
        /// <summary>
        /// Parent of surface meshes
        /// </summary>
        public Transform BrainSurfaceMeshesParent
        {
            get
            {
                return m_BrainSurfaceMeshesParent;
            }
            set
            {
                m_BrainSurfaceMeshesParent = value;
            }
        }
        
        /// <summary>
        /// Parent of the cut meshes
        /// </summary>
        [SerializeField] private Transform m_BrainCutMeshesParent;
        /// <summary>
        /// Parent of the sites
        /// </summary>
        [SerializeField] private Transform m_SitesMeshesParent;
        /// <summary>
        /// List of every patient parents for the sites
        /// </summary>
        public List<GameObject> SitesPatientParent { get; private set; }

        /// <summary>
        /// Mesh of the brain surface
        /// </summary>
        public GameObject Brain { get; private set; }
        /// <summary>
        /// Meshes of the cuts
        /// </summary>
        public List<GameObject> BrainCutMeshes { get; private set; } = new List<GameObject>();
        /// <summary>
        /// Mesh of the invisible surface
        /// </summary>
        public GameObject InvisibleBrain { get; private set; }
        /// <summary>
        /// Mesh of the simplified brain
        /// </summary>
        public GameObject SimplifiedBrain { get; private set; }

        /// <summary>
        /// Parent for ROI GameObjects
        /// </summary>
        [SerializeField] protected Transform m_ROIParent;

        /// <summary>
        /// Prefab for the 3D brain mesh part
        /// </summary>
        [SerializeField] private GameObject m_BrainPrefab;
        /// <summary>
        /// Prefab for the 3D simplified brain mesh
        /// </summary>
        [SerializeField] private GameObject m_SimplifiedBrainPrefab;
        /// <summary>
        /// Prefab for the 3D invisible brain mesh
        /// </summary>
        [SerializeField] private GameObject m_InvisibleBrainPrefab;
        /// <summary>
        /// Prefab for the 3D cut
        /// </summary>
        [SerializeField] private GameObject m_CutPrefab;
        /// <summary>
        /// Prefab for the 3D site
        /// </summary>
        [SerializeField] private GameObject m_SitePrefab;
        /// <summary>
        /// Prefab for the ROI
        /// </summary>
        [SerializeField] private GameObject m_ROIPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            // Mark brain mesh as dynamic
            m_BrainPrefab.GetComponent<MeshFilter>().sharedMesh.MarkDynamic();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Instantiate the gameObjects responsible for the invisible brain
        /// </summary>
        /// <param name="visible">Is the mesh corresponding to the invisible brain visible at instantiation ?</param>
        public void InstantiateInvisibleMesh(bool visible)
        {
            if (InvisibleBrain != null)
                Destroy(InvisibleBrain);

            InvisibleBrain = Instantiate(m_InvisibleBrainPrefab, BrainSurfaceMeshesParent);
            InvisibleBrain.layer = LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);
            InvisibleBrain.transform.localScale = new Vector3(-1, 1, 1);
            InvisibleBrain.transform.localPosition = new Vector3(0, 0, 0);
            InvisibleBrain.SetActive(visible);
        }
        /// <summary>
        /// Instantiate all the gameObjects representing the sites on the scene
        /// </summary>
        /// <param name="implantation">Implantation to be instantiated</param>
        public void InstantiateImplantation(Core.Object3D.Implantation3D implantation)
        {
            foreach (Transform sitePatient in m_SitesMeshesParent)
            {
                Destroy(sitePatient.gameObject);
            }
            SitesPatientParent = new List<GameObject>();

            if (implantation == null) return;

            int siteIndex = 0;
            foreach (var patient in m_Scene.Visualization.Patients)
            {
                GameObject sitePatient = new GameObject(patient.ID);
                sitePatient.transform.SetParent(m_SitesMeshesParent);
                sitePatient.transform.localPosition = Vector3.zero;
                SitesPatientParent.Add(sitePatient);
                var siteInfos = implantation.GetSitesOfPatient(patient);
                // Instantiate electrodes containers
                Dictionary<string, Transform> electrodeTransforms = new Dictionary<string, Transform>();
                var electrodes = siteInfos.Select(s => s.Electrode).Distinct();
                foreach (var electrode in electrodes)
                {
                    GameObject electrodeGameObject = new GameObject(electrode);
                    electrodeGameObject.transform.SetParent(sitePatient.transform);
                    electrodeGameObject.transform.localPosition = Vector3.zero;
                    electrodeTransforms.Add(electrode, electrodeGameObject.transform);
                }
                // Instantiate sites
                foreach (var siteInfo in implantation.GetSitesOfPatient(patient))
                {
                    GameObject siteGameObject = Instantiate(m_SitePrefab, electrodeTransforms[siteInfo.Electrode]);
                    siteGameObject.name = siteInfo.Name;

                    siteGameObject.transform.localPosition = siteInfo.UnityPosition;
                    siteGameObject.GetComponent<MeshFilter>().sharedMesh = Core.Object3D.SharedMeshes.Site;

                    siteGameObject.SetActive(true);
                    siteGameObject.layer = LayerMask.NameToLayer("Inactive");

                    Core.Object3D.Site site = siteGameObject.GetComponent<Core.Object3D.Site>();
                    site.Information.Patient = patient;
                    site.Information.Name = siteInfo.Name;
                    site.Information.Index = siteIndex++;
                    site.Information.SiteData = siteInfo.SiteData;
                    site.Information.DefaultPosition = siteInfo.UnityPosition;
                    site.State.IsBlackListed = false;
                    site.State.IsHighlighted = false;
                    site.State.IsOutOfROI = true;
                    site.State.IsMasked = false;
                    site.State.Color = Core.Object3D.SiteState.DefaultColor;
                    site.IsActive = true;
                }
            }

            foreach (Column3D column in m_Scene.Columns)
            {
                column.UpdateSites(implantation, SitesPatientParent);
            }
            m_Scene.ROIManager.UpdateROIMasks();
        }
        /// <summary>
        /// Instantiate the brain mesh
        /// </summary>
        public void InstantiateBrain()
        {
            if (Brain != null)
                Destroy(Brain);

            Brain = Instantiate(m_BrainPrefab, BrainSurfaceMeshesParent);
            Brain.GetComponent<Renderer>().sharedMaterial = m_Scene.BrainMaterials.BrainMaterial;
            Brain.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            Brain.transform.localPosition = Vector3.zero;
            Brain.layer = LayerMask.NameToLayer(HBP3DModule.HIDDEN_MESHES_LAYER);
            Brain.SetActive(true);
        }
        /// <summary>
        /// Instantiate the simplified brain mesh
        /// </summary>
        public void InstantiateSimplifiedBrain()
        {
            if (SimplifiedBrain != null)
                Destroy(SimplifiedBrain);

            SimplifiedBrain = Instantiate(m_SimplifiedBrainPrefab, BrainSurfaceMeshesParent);
            SimplifiedBrain.transform.localPosition = Vector3.zero;
            SimplifiedBrain.layer = LayerMask.NameToLayer(HBP3DModule.HIDDEN_MESHES_LAYER);
            SimplifiedBrain.SetActive(true);
        }
        /// <summary>
        /// Instantiate the gameObject for a new cut in the scene
        /// </summary>
        public void InstantiateCut()
        {
            GameObject cut = Instantiate(m_CutPrefab, m_BrainCutMeshesParent);
            cut.GetComponent<Renderer>().sharedMaterial = m_Scene.BrainMaterials.CutMaterial;
            cut.layer = LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);
            cut.transform.localPosition = Vector3.zero;
            BrainCutMeshes.Add(cut);
        }
        /// <summary>
        /// Instantiate a ROI on the scene
        /// </summary>
        /// <returns>The ROI object that has been instantiated</returns>
        public Core.Object3D.ROI InstantiateROI()
        {
            return Instantiate(m_ROIPrefab, m_ROIParent).GetComponent<Core.Object3D.ROI>();
        }
        #endregion
    }
}