
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

namespace HBP.VISU3D
{
    public class SharedMeshes : MonoBehaviour
    {
        static public Mesh ROIBubble = null;
        static public Mesh Plot = null;
        static public Mesh HighlightedPlot = null;
        static public Mesh PlotSelection = null;


        static public Mesh PlotLOD0 = null;
        static public Mesh PlotLOD1 = null;
        static public Mesh PlotLOD2 = null;

        void Awake()
        {
            ROIBubble = Geometry.createSphereMesh(1f, 48, 32);
            Plot = Geometry.createSphereMesh(1, 12, 8);
            HighlightedPlot = Geometry.createTube();// (3f);
            PlotSelection = Geometry.createTube();// (3f);

            // TODO
            PlotLOD0 = Geometry.createSphereMesh(1, 16, 12);
            PlotLOD1 = Geometry.createSphereMesh(1, 12, 8);
            PlotLOD2 = Geometry.createSphereMesh(1, 4, 3);
        }
    }
}