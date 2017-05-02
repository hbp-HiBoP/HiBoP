
/**
 * \file    Volume.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define BBox, DLL.BBox and DLL.Volume classes
 */

// system
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// A DLL bounding box class
    /// </summary>
    public class BBox : CppDLLImportBase
    {
        #region Public Methods

        public Vector3 min()
        {
            float[] min = new float[3];
            getMin_BBox(_handle, min);
            return new Vector3(min[0], min[1], min[2]);
        }

        public Vector3 max()
        {
            float[] max = new float[3];
            getMax_BBox(_handle, max);
            return new Vector3(max[0], max[1], max[2]);
        }

        public Vector3 center()
        {
            float[] center = new float[3];
            getCenter_BBox(_handle, center);
            return new Vector3(center[0], center[1], center[2]);
        }

        public float diagLength()
        {
            return (max() - min()).magnitude;
        }

        public List<Vector3> points()
        {
            float[] points = new float[3 * 8];
            getPoints_BBox(_handle, points);
            List<Vector3> pointsV = new List<Vector3>(8);

            for (int ii = 0; ii < 8; ii++)
            {
                pointsV.Add(new Vector3(points[3 * ii], points[3 * ii + 1], points[3 * ii + 2]));
            }

            return pointsV;
        }

        public List<Vector3> lines_pair_points()
        {
            float[] points = new float[3 * 24];
            getLinesPairPoints_BBox(_handle, points);
            List<Vector3> linesPoints = new List<Vector3>(24);

            for (int ii = 0; ii < 24; ii++)
            {
                linesPoints.Add(new Vector3(points[3 * ii], points[3 * ii + 1], points[3 * ii + 2]));
            }

            return linesPoints;
        }

        public List<Vector3> intersection_points_with_plane(Plane planeIntersec)
        {
            // init plane
            float[] plane = new float[6];
            for (int ii = 0; ii < 3; ++ii)
            {
                plane[ii] = planeIntersec.point[ii];
                plane[ii + 3] = planeIntersec.normal[ii];
            }

            float[] points = new float[8 * 3];
            getIntersectionsWithPlane_BBox(_handle, plane, points);
            List<Vector3> intersecPoints = new List<Vector3>(4);

            for (int ii = 0; ii < 8; ++ii)
            {
                Vector3 point = new Vector3(points[3 * ii], points[3 * ii + 1], points[3 * ii + 2]);
                if (point.x == 0 && point.y == 0 && point.z == 0)
                    continue;
                intersecPoints.Add(point);
            }

            return intersecPoints;
        }

        public List<Vector3> intersection_lines_with_plane(Plane planeIntersec)
        {
            float[] points = new float[8 * 3];
            getLinesIntersectionsWithPlane_BBox(_handle, planeIntersec.convertToArray(), points);
            List<Vector3> intersecLines = new List<Vector3>(8);

            for (int ii = 0; ii < 8; ++ii)
            {
                Vector3 point = new Vector3(points[3 * ii], points[3 * ii + 1], points[3 * ii + 2]);
                intersecLines.Add(point);
            }

            return intersecLines;
        }


        #endregion

        #region Memory Management

        /// <summary>
        /// BBox default constructor
        /// </summary>
        public BBox()
        {
            _handle = new HandleRef(this,create_BBox());
        }

        /// <summary>
        /// BBox constructor with an already allocated dll BBox
        /// </summary>
        /// <param name="bBoxPointer"></param>
        public BBox(IntPtr bBoxPointer)
        {
            _handle = new HandleRef(this, bBoxPointer);
        }

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_BBox());
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_BBox(_handle);
        }

        #endregion

        #region DLLImport

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_BBox();

        [DllImport("hbp_export", EntryPoint = "delete_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_BBox(HandleRef handleBBox);

        // retrieve data
        [DllImport("hbp_export", EntryPoint = "getMin_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getMin_BBox(HandleRef handleBBox, float[] min);

        [DllImport("hbp_export", EntryPoint = "getMax_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getMax_BBox(HandleRef handleBBox, float[] max);

        [DllImport("hbp_export", EntryPoint = "getPoints_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getPoints_BBox(HandleRef handleBBox, float[] points);

        [DllImport("hbp_export", EntryPoint = "getLinesPairPoints_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getLinesPairPoints_BBox(HandleRef handleBBox, float[] points);

        [DllImport("hbp_export", EntryPoint = "getIntersectionsWithPlane_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getIntersectionsWithPlane_BBox(HandleRef handleBBox, float[] plane, float[] interPoints);

        [DllImport("hbp_export", EntryPoint = "getLinesIntersectionsWithPlane_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getLinesIntersectionsWithPlane_BBox(HandleRef handleBBox, float[] plane, float[] interPoints);

        [DllImport("hbp_export", EntryPoint = "getCenter_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getCenter_BBox(HandleRef handleBBox, float[] center);

        // memory management
        //delegate IntPtr create_BBox();
        //delegate void delete_BBox(HandleRef handleBBox);

        //// retrieve data
        //delegate void getMin_BBox(HandleRef handleBBox, float[] min);
        //delegate void getMax_BBox(HandleRef handleBBox, float[] max);
        //delegate void getPoints_BBox(HandleRef handleBBox, float[] points);
        //delegate void getLinesPairPoints_BBox(HandleRef handleBBox, float[] points);
        //delegate void getIntersectionsWithPlane_BBox(HandleRef handleBBox, float[] plane, float[] interPoints);
        //delegate void getLinesIntersectionsWithPlane_BBox(HandleRef handleBBox, float[] plane, float[] interPoints);
        //delegate void getCenter_BBox(HandleRef handleBBox, float[] center);

        #endregion
    }

    /// <summary>
    /// A DLL volume class converted from NII files
    /// </summary>
    public class Volume : CppDLLImportBase
    {
        #region Public Methods

        public Vector3 center()
        {
            float[] center = new float[3];
            center_Volume(_handle, center);
            return new Vector3(center[0], center[1], center[2]);
        }

        public Vector3 spacing()
        {
            float[] spacing = new float[3];
            spacing_Volume(_handle, spacing);
            return new Vector3(spacing[0], spacing[1], spacing[2]);
        }

        public float size_offset_cut_plane(Plane cutPlane, int nbCuts)
        {
            return sizeOffsetCutPlane_Volume(_handle, cutPlane.convertToArray(), nbCuts);
        }

        public void set_plane_with_orientation(Plane plane, int idOrientation, bool flip)
        {
            float[] normal = new float[3];
            definePlaneWithOrientation_Volume(_handle, normal, idOrientation, flip);
            plane.normal = new Vector3(normal[0], normal[1], normal[2]);
        }

        public MRICalValues retrieve_extreme_values()
        {
            MRICalValues values = new MRICalValues();

            float[] valuesF = new float[6];
            retrieveExtremeValues_Volume(_handle, valuesF);

            values.min = valuesF[0];
            values.max = valuesF[1];
            values.loadedCalMin = valuesF[2];
            values.loadedCalMax = valuesF[3];
            values.computedCalMin = valuesF[4];
            values.computedCalMax = valuesF[5];

            return values;
        }


        #endregion

        #region Memory Management

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_Volume());
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_Volume(_handle);
        }

        #endregion

        #region DLLimport

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Volume();

        [DllImport("hbp_export", EntryPoint = "delete_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Volume(HandleRef handleVolume);

        // retrieve data
        [DllImport("hbp_export", EntryPoint = "center_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void center_Volume(HandleRef handleVolume, float[] center);

        [DllImport("hbp_export", EntryPoint = "bBox_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void bBox_Volume(HandleRef handleVolume, float[] minMax);

        [DllImport("hbp_export", EntryPoint = "diagonalLenght_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float diagonalLenght_Volume(HandleRef handleVolume);

        [DllImport("hbp_export", EntryPoint = "boundingBox_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr boundingBox_Volume(HandleRef handleVolume);

        [DllImport("hbp_export", EntryPoint = "spacing_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void spacing_Volume(HandleRef handleVolume, float[] spacing);

        [DllImport("hbp_export", EntryPoint = "definePlaneWithOrientation_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void definePlaneWithOrientation_Volume(HandleRef handleVolume, float[] planeNormal, int idOrientation, bool flip);

        [DllImport("hbp_export", EntryPoint = "sizeOffsetCutPlane_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern float sizeOffsetCutPlane_Volume(HandleRef handleVolume, float[] planeCut, int nbCuts);

        [DllImport("hbp_export", EntryPoint = "retrieveExtremeValues_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void retrieveExtremeValues_Volume(HandleRef handleVolume, float[] extremeValues);

        //// memory management        
        //delegate IntPtr create_Volume();
        //delegate void delete_Volume(HandleRef handleVolume);

        //// retrieve data
        //delegate void center_Volume(HandleRef handleVolume, float[] center);
        //delegate void bBox_Volume(HandleRef handleVolume, float[] minMax);
        //delegate float diagonalLenght_Volume(HandleRef handleVolume);
        //delegate IntPtr boundingBox_Volume(HandleRef handleVolume);
        //delegate void spacing_Volume(HandleRef handleVolume, float[] spacing);
        //delegate void definePlaneWithOrientation_Volume(HandleRef handleVolume, float[] planeNormal, int idOrientation, bool flip);
        //delegate float sizeOffsetCutPlane_Volume(HandleRef handleVolume, float[] planeCut, int nbCuts);
        //delegate void retrieveExtremeValues_Volume(HandleRef handleVolume, float[] extremeValues);

        #endregion
    }
}