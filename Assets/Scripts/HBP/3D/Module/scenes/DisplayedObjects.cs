using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// List of all the objects displayed in the scene
    /// </summary>
    public class DisplayedObjects : MonoBehaviour
    {
        #region Properties
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
        public GameObject InvisibleBrainMeshesParent;
        /// <summary>
        /// Parent of the cut meshes
        /// </summary>
        public GameObject BrainCutMeshesParent;
        /// <summary>
        /// Parent of the sites
        /// </summary>
        public GameObject SitesMeshesParent;

        /// <summary>
        /// Prefab for the 3D invisible brain mesh
        /// </summary>
        [SerializeField] private GameObject m_InvisibleBrainPrefab;

        /// <summary>
        /// Meshes of the brain surface
        /// </summary>
        [HideInInspector] public List<GameObject> BrainSurfaceMeshes = new List<GameObject>();
        /// <summary>
        /// Meshes of the cuts
        /// </summary>
        [HideInInspector] public List<GameObject> BrainCutMeshes = new List<GameObject>();
        /// <summary>
        /// Meshes of the invisible surface
        /// </summary>
        [HideInInspector] public List<GameObject> InvisibleBrainSurfaceMeshes = new List<GameObject>();
        /// <summary>
        /// Simplified brain
        /// </summary>
        [HideInInspector] public GameObject SimplifiedBrain;
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
                GameObject invisibleBrainPart = Instantiate(m_InvisibleBrainPrefab, InvisibleBrainMeshesParent.transform);
                invisibleBrainPart.name = "erased brain part " + ii;
                invisibleBrainPart.layer = LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);
                invisibleBrainPart.transform.localScale = new Vector3(-1, 1, 1);
                invisibleBrainPart.transform.localPosition = new Vector3(0, 0, 0);
                invisibleBrainPart.SetActive(visible);
                InvisibleBrainSurfaceMeshes.Add(invisibleBrainPart);
            }
        }
        #endregion
    }
}