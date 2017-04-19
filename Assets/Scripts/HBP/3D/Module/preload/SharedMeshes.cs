
/* \file SharedMeshes.cs
 * \author Lance Florian
 * \date    22/04/2016
 * \brief Define SharedMeshes
 */

// system
using System;
using System.Text;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Shared procedural meshes used at runtime
    /// </summary>
    public class SharedMeshes : MonoBehaviour
    {
        static public Mesh ROIBubble = null;
        static public Mesh Site = null;
        static public Mesh HighlightedSite = null;
        static public Mesh SiteSelection = null;

        static public Mesh SiteLOD0 = null;
        static public Mesh SiteLOD1 = null;
        static public Mesh SiteLOD2 = null;

        void Awake()
        {
            ROIBubble = Geometry.create_sphere_mesh(1f, 48, 32);
            Site = Geometry.create_sphere_mesh(1, 12, 8);
            HighlightedSite = Geometry.create_tube();// (3f);
            SiteSelection = Geometry.create_tube();// (3f);

            // TODO: use level of details for sites
            SiteLOD0 = Geometry.create_sphere_mesh(1, 16, 12);
            SiteLOD1 = Geometry.create_sphere_mesh(1, 12, 8);
            SiteLOD2 = Geometry.create_sphere_mesh(1, 4, 3);
        }
    }
}