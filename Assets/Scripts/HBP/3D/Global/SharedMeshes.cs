using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Class managing the meshes created at runtime (ROI spheres, sites)
    /// </summary>
    public class SharedMeshes : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Mesh of a ROI Sphere
        /// </summary>
        static public Mesh ROISphere { get; private set; }
        /// <summary>
        /// Mesh of a Site
        /// </summary>
        static public Mesh Site { get; private set; }
        #endregion

        #region Private Methods
        private void Awake()
        {
            ROISphere = Geometry.CreateSphereMesh(1, 48, 32);
            Site = Geometry.CreateSphereMesh(1, 10, 7);
        }
        #endregion
    }
}