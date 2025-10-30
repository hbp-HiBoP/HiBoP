using UnityEngine;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class managing the meshes created at runtime (ROI spheres, sites)
    /// </summary>
    public static class SharedMeshes
    {
        #region Properties
        /// <summary>
        /// Mesh of a ROI Sphere
        /// </summary>
        public static Mesh ROISphere { get; private set; } = Geometry.CreateSphereMesh(1, 48, 32);
        /// <summary>
        /// Mesh of a Site
        /// </summary>
        public static Mesh Site { get; private set; } = Geometry.CreateSphereMesh(1, 10, 7);
        #endregion
    }
}