using UnityEngine;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class managing the meshes created at runtime (ROI spheres, sites)
    /// </summary>
    public static class SharedMeshes
    {
        #region Fields

        private static Mesh s_ROISphere;
        private static Mesh s_Site;

        #endregion

        #region Properties

        /// <summary>
        /// Mesh of a ROI Sphere
        /// </summary>
        public static Mesh ROISphere
        {
            get
            {
                if (!s_ROISphere)
                    s_ROISphere = Geometry.CreateSphereMesh(1, 48, 32);

                return s_ROISphere;
            }
        }

        /// <summary>
        /// Mesh of a Site
        /// </summary>
        public static Mesh Site
        {
            get
            {
                if (!s_Site)
                    s_Site = Geometry.CreateSphereMesh(1, 10, 7);

                return s_Site;
            }
        }

        #endregion
    }
}
