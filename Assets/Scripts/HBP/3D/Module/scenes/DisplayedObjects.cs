using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// List of all the objects displayed in the scene
    /// </summary>
    public class DisplayedObjects : MonoBehaviour
    {
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
    }
}