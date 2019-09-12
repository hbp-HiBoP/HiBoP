using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// List of all the objects displayed in the scene
    /// </summary>
    public class DisplayedObjects : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Base3DScene m_Scene;
        /// <summary>
        /// Parent of all meshes
        /// </summary>
        public GameObject MeshesParent;
        /// <summary>
        /// Parent of surface meshes
        /// </summary>
        public GameObject BrainSurfaceMeshesParent;
        /// <summary>
        /// Parent of the invisible surface meshes
        /// </summary>
        [SerializeField] private Transform m_InvisibleBrainMeshesParent;
        /// <summary>
        /// Parent of the cut meshes
        /// </summary>
        public GameObject BrainCutMeshesParent;

        /// <summary>
        /// Parent of the sites
        /// </summary>
        [SerializeField] private Transform m_SitesMeshesParent;
        public List<GameObject> SitesPatientParent { get; private set; }

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
        /// Meshes of the brain surface
        /// </summary>
        [HideInInspector] public List<GameObject> BrainSurfaceMeshes { get; private set; } = new List<GameObject>();
        /// <summary>
        /// Meshes of the cuts
        /// </summary>
        [HideInInspector] public List<GameObject> BrainCutMeshes { get; private set; } = new List<GameObject>();
        /// <summary>
        /// Meshes of the invisible surface
        /// </summary>
        [HideInInspector] public List<GameObject> InvisibleBrainSurfaceMeshes { get; private set; } = new List<GameObject>();
        /// <summary>
        /// Simplified brain
        /// </summary>
        [HideInInspector] public GameObject SimplifiedBrain { get; private set; }
        #endregion

        #region Private Methods
        private void Awake()
        {
            // Mark brain mesh as dynamic
            m_BrainPrefab.GetComponent<MeshFilter>().sharedMesh.MarkDynamic();
        }
        #endregion

        #region Public Methods
        public void ResetInvisibleMesh(bool visible)
        {
            // destroy previous GO
            if (InvisibleBrainSurfaceMeshes != null)
                for (int ii = 0; ii < InvisibleBrainSurfaceMeshes.Count; ++ii)
                    Destroy(InvisibleBrainSurfaceMeshes[ii]);

            // create new GO
            InvisibleBrainSurfaceMeshes = new List<GameObject>(BrainSurfaceMeshes.Count);
            for (int ii = 0; ii < BrainSurfaceMeshes.Count; ++ii)
            {
                GameObject invisibleBrainPart = Instantiate(m_InvisibleBrainPrefab, m_InvisibleBrainMeshesParent);
                invisibleBrainPart.name = "erased brain part " + ii;
                invisibleBrainPart.layer = LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);
                invisibleBrainPart.transform.localScale = new Vector3(-1, 1, 1);
                invisibleBrainPart.transform.localPosition = new Vector3(0, 0, 0);
                invisibleBrainPart.SetActive(visible);
                InvisibleBrainSurfaceMeshes.Add(invisibleBrainPart);
            }
        }
        public void InstantiateImplantation(Implantation3D implantation)
        {
            foreach (Transform sitePatient in m_SitesMeshesParent)
            {
                Destroy(sitePatient.gameObject);
            }
            SitesPatientParent = new List<GameObject>();
            
            DLL.PatientElectrodesList electrodesList = implantation.PatientElectrodesList;

            int currPlotNb = 0;
            for (int ii = 0; ii < electrodesList.NumberOfPatients; ++ii)
            {
                int patientSiteID = 0;
                string patientID = electrodesList.PatientName(ii);
                Data.Patient patient = m_Scene.Visualization.Patients.FirstOrDefault((p) => p.ID == patientID);

                GameObject sitePatient = new GameObject(patientID);
                sitePatient.transform.SetParent(m_SitesMeshesParent);
                sitePatient.transform.localPosition = Vector3.zero;
                SitesPatientParent.Add(sitePatient);

                for (int jj = 0; jj < electrodesList.NumberOfElectrodesInPatient(ii); ++jj)
                {
                    GameObject siteElectrode = new GameObject(electrodesList.ElectrodeName(ii, jj));
                    siteElectrode.transform.SetParent(sitePatient.transform);
                    siteElectrode.transform.localPosition = Vector3.zero;

                    for (int kk = 0; kk < electrodesList.NumberOfSitesInElectrode(ii, jj); ++kk)
                    {
                        Vector3 invertedPosition = electrodesList.SitePosition(ii, jj, kk);
                        invertedPosition.x = -invertedPosition.x;

                        GameObject siteGameObject = Instantiate(m_SitePrefab, siteElectrode.transform);
                        siteGameObject.name = electrodesList.SiteName(ii, jj, kk);

                        siteGameObject.transform.localPosition = invertedPosition;
                        siteGameObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.Site;

                        siteGameObject.SetActive(true);
                        siteGameObject.layer = LayerMask.NameToLayer("Inactive");

                        Site site = siteGameObject.GetComponent<Site>();
                        site.Information.Patient = patient;
                        site.Information.Name = siteGameObject.name;
                        site.Information.SitePatientID = patientSiteID++;
                        site.Information.PatientNumber = ii;
                        site.Information.ElectrodeNumber = jj;
                        site.Information.SiteNumber = kk;
                        site.Information.GlobalID = currPlotNb++;
                        site.Information.MarsAtlasIndex = electrodesList.MarsAtlasLabelOfSite(ii, jj, kk);
                        site.Information.FreesurferLabel = electrodesList.FreesurferLabelOfSite(ii, jj, kk).Replace('_', ' ');
                        site.State.IsBlackListed = false;
                        site.State.IsHighlighted = false;
                        site.State.IsOutOfROI = true;
                        site.State.IsMasked = false;
                        site.State.Color = SiteState.DefaultColor;
                        site.IsActive = true;
                    }
                }
            }

            foreach (Column3D column in m_Scene.Columns)
            {
                column.UpdateSites(electrodesList, SitesPatientParent);
                column.UpdateROIMask();
            }
        }
        public void InstantiateSplits(int nbSplits)
        {
            for (int i = 0; i < BrainSurfaceMeshes.Count; i++)
            {
                Destroy(BrainSurfaceMeshes[i]);
            }
            if (SimplifiedBrain != null)
            {
                Destroy(SimplifiedBrain);
            }

            BrainSurfaceMeshes = new List<GameObject>(nbSplits);
            for (int i = 0; i < nbSplits; ++i)
            {
                BrainSurfaceMeshes.Add(Instantiate(m_BrainPrefab));
                BrainSurfaceMeshes[i].GetComponent<Renderer>().sharedMaterial = m_Scene.BrainMaterial;
                BrainSurfaceMeshes[i].name = "brain_" + i;
                BrainSurfaceMeshes[i].transform.parent = BrainSurfaceMeshesParent.transform;
                BrainSurfaceMeshes[i].transform.localPosition = Vector3.zero;
                BrainSurfaceMeshes[i].layer = LayerMask.NameToLayer(HBP3DModule.HIDDEN_MESHES_LAYER);
                BrainSurfaceMeshes[i].SetActive(true);
            }
            SimplifiedBrain = Instantiate(m_SimplifiedBrainPrefab);
            SimplifiedBrain.GetComponent<Renderer>().sharedMaterial = m_Scene.SimplifiedBrainMaterial;
            SimplifiedBrain.transform.name = "brain_simplified";
            SimplifiedBrain.transform.parent = BrainSurfaceMeshesParent.transform;
            SimplifiedBrain.transform.localPosition = Vector3.zero;
            SimplifiedBrain.layer = LayerMask.NameToLayer(HBP3DModule.HIDDEN_MESHES_LAYER);
            SimplifiedBrain.SetActive(true);
        }
        public void InstantiateCut()
        {
            GameObject cutGameObject = Instantiate(m_CutPrefab);
            cutGameObject.GetComponent<Renderer>().sharedMaterial = m_Scene.CutMaterial;
            cutGameObject.name = "Cut";
            cutGameObject.transform.parent = BrainCutMeshesParent.transform;
            cutGameObject.AddComponent<MeshCollider>();
            cutGameObject.layer = LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);
            cutGameObject.transform.localPosition = Vector3.zero;
            BrainCutMeshes.Add(cutGameObject);
            BrainCutMeshes.Last().layer = LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);
        }
        #endregion
    }
}