using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    public class DisplayedObjects : MonoBehaviour
    {
        /// <summary>
        /// Parent of all meshes
        /// </summary>
        public GameObject MeshesParent = null;
        /// <summary>
        /// Parent of surface meshes
        /// </summary>
        public GameObject BrainSurfaceMeshesParent = null;
        /// <summary>
        /// Parent of the invisible surface meshes
        /// </summary>
        public GameObject InvisibleBrainMeshesParent = null;
        /// <summary>
        /// Parent of the cut meshes
        /// </summary>
        public GameObject BrainCutMeshesParent = null;
        /// <summary>
        /// Parent of the sites
        /// </summary>
        public GameObject SitesMeshesParent = null;

        /// <summary>
        /// Meshes of the brain surface
        /// </summary>
        [HideInInspector] public List<GameObject> BrainSurfaceMeshes = new List<GameObject>();
        /// <summary>
        /// Meshes of the cuts
        /// </summary>
        [HideInInspector] public List<GameObject> BrainCutMeshes = null;
        /// <summary>
        /// Meshes of the invisible surface
        /// </summary>
        [HideInInspector] public List<GameObject> InvisibleBrainSurfaceMeshes = null;
        /// <summary>
        /// Simplified brain
        /// </summary>
        [HideInInspector] public GameObject SimplifiedBrain;
    }
}