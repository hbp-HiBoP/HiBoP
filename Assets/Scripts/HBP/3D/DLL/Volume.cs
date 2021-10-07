using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Class representing the bounding box in the DLL
    /// </summary>
    public class BBox : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Minimum point of the bounding box
        /// </summary>
        public Vector3 Min
        {
            get
            {
                float[] min = new float[3];
                getMin_BBox(_handle, min);
                return new Vector3(min[0], min[1], min[2]);
            }
        }
        /// <summary>
        /// Maximum point of the bounding box
        /// </summary>
        public Vector3 Max
        {
            get
            {
                float[] max = new float[3];
                getMax_BBox(_handle, max);
                return new Vector3(max[0], max[1], max[2]);
            }
        }
        /// <summary>
        /// Center point of the bounding box
        /// </summary>
        public Vector3 Center
        {
            get
            {
                float[] center = new float[3];
                getCenter_BBox(_handle, center);
                return new Vector3(center[0], center[1], center[2]);
            }
        }
        /// <summary>
        /// Length of the diagonal Max-Min
        /// </summary>
        public float DiagonalLength
        {
            get
            {
                return (Max - Min).magnitude;
            }
        }
        /// <summary>
        /// List of the points of the bounding box (8 points)
        /// </summary>
        public List<Vector3> Points
        {
            get
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
        }
        /// <summary>
        /// List of the pairs of points composing the edges of the bounding box
        /// </summary>
        public List<Segment3> Segments
        {
            get
            {
                float[] points = new float[3 * 2 * 12];
                getLinesPairPoints_BBox(_handle, points);
                List<Segment3> linesPoints = new List<Segment3>(12);

                for (int ii = 0; ii < 12; ii++)
                {
                    linesPoints.Add(new Segment3(new Vector3(points[3 * ii], points[3 * ii + 1], points[3 * ii + 2]), new Vector3(points[3 * ii + 3], points[3 * ii + 4], points[3 * ii + 5])));
                }

                return linesPoints;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the points of the intersection of a plane and this bounding box
        /// </summary>
        /// <param name="planeIntersec">Plane to intersect with</param>
        /// <returns>List of the points composing the intersection</returns>
        public List<Vector3> IntersectionPointsWithPlane(Plane planeIntersec)
        {
            float[] points = new float[8 * 3];
            getIntersectionsWithPlane_BBox(_handle, planeIntersec.ConvertToArray(), points);
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
        /// <summary>
        /// Get the lines of the intersection of a plane and this bounding box
        /// </summary>
        /// <param name="planeIntersec">Plane to intersect with</param>
        /// <returns>List of the pairs of points composing the lines of the intersection</returns>
        public List<Segment3> IntersectionLinesWithPlane(Plane planeIntersec)
        {
            float[] points = new float[4 * 2 * 3];
            getLinesIntersectionsWithPlane_BBox(_handle, planeIntersec.ConvertToArray(), points);
            List<Segment3> intersecLines = new List<Segment3>(4);

            for (int ii = 0; ii < 4; ++ii)
            {
                intersecLines.Add(new Segment3(new Vector3(points[3 * ii], points[3 * ii + 1], points[3 * ii + 2]), new Vector3(points[3 * ii + 3], points[3 * ii + 4], points[3 * ii + 5])));
            }

            return intersecLines;
        }
        /// <summary>
        /// Get the intersection segment of two planes with the ends of the segment being on the bounding box
        /// </summary>
        /// <param name="planeA">First plane of the intersection</param>
        /// <param name="planeB">Second plane of the intersection</param>
        /// <returns>List of 2 points composing the segment</returns>
        public Segment3 IntersectionSegmentBetweenTwoPlanes(Plane planeA, Plane planeB)
        {
            float[] result = new float[2 * 3];
            bool isOk = find_intersection_segment_BBox(_handle, planeA.ConvertToArray(), planeB.ConvertToArray(), result);
            if (!isOk)
            {
                return null;
            }
            else
            {
                return new Segment3(new Vector3(result[0], result[1], result[2]), new Vector3(result[3], result[4], result[5]));
            }
        }
        #endregion

        #region Memory Management
        public BBox()
        {
            _handle = new HandleRef(this,create_BBox());
        }
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
        [DllImport("hbp_export", EntryPoint = "create_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_BBox();
        [DllImport("hbp_export", EntryPoint = "delete_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_BBox(HandleRef handleBBox);
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
        [DllImport("hbp_export", EntryPoint = "find_intersection_segment_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool find_intersection_segment_BBox(HandleRef handleBBox, float[] planeA, float[] planeB, float[] interPoints);
        [DllImport("hbp_export", EntryPoint = "getCenter_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getCenter_BBox(HandleRef handleBBox, float[] center);
        #endregion
    }

    /// <summary>
    /// Class representing a volumr loaded from a NIFTI file
    /// </summary>
    public class Volume : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Is the volume completely loaded ?
        /// </summary>
        public bool IsLoaded { get; private set; }
        /// <summary>
        /// Center point of the volume
        /// </summary>
        public Vector3 Center
        {
            get
            {
                float[] center = new float[3];
                center_Volume(_handle, center);
                return new Vector3(center[0], center[1], center[2]);
            }
        }
        /// <summary>
        /// Space between two voxels in x, y and z directions
        /// </summary>
        public Vector3 Spacing
        {
            get
            {
                float[] spacing = new float[3];
                spacing_Volume(_handle, spacing);
                return new Vector3(spacing[0], spacing[1], spacing[2]);
            }
        }
        /// <summary>
        /// Get the calibration values of the loaded MRI
        /// </summary>
        public MRICalValues ExtremeValues
        {
            get
            {
                MRICalValues values = new MRICalValues();

                float[] valuesF = new float[6];
                retrieveExtremeValues_Volume(_handle, valuesF);

                values.Min = valuesF[0];
                values.Max = valuesF[1];
                values.LoadedCalMin = valuesF[2];
                values.LoadedCalMax = valuesF[3];
                values.ComputedCalMin = valuesF[4];
                values.ComputedCalMax = valuesF[5];

                return values;
            }
        }
        /// <summary>
        /// Bounding box of this volume
        /// </summary>
        public BBox BoundingBox
        {
            get
            {
                return new BBox(boundingBox_Volume(_handle));
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a NIFTI file to a DLL Volume
        /// </summary>
        /// <param name="path">Path to the NIFTI file</param>
        /// <returns></returns>
        public bool LoadNIFTIFile(string path)
        {
            IsLoaded = (loadNiiFile_Volume(_handle, path) == 1);
            return IsLoaded;
        }
        /// <summary>
        /// Get the offset value for a cut plane given the number of cuts
        /// </summary>
        /// <param name="cutPlane">Cut plane to compute the offset for</param>
        /// <param name="nbCuts">Number of desired cuts</param>
        /// <returns>Value of the offset</returns>
        public float SizeOffsetCutPlane(Plane cutPlane, int nbCuts)
        {
            return sizeOffsetCutPlane_Volume(_handle, cutPlane.ConvertToArray(), nbCuts);
        }
        /// <summary>
        /// Get information for a plane depending on the volume and on the input orientation
        /// </summary>
        /// <param name="plane">Plane to update</param>
        /// <param name="orientation">Orientation of the cut</param>
        /// <param name="flip">Is the cut flipped ?</param>
        public void SetPlaneWithOrientation(Plane plane, Data.Enums.CutOrientation orientation, bool flip)
        {
            plane.Normal = GetOrientationVector(orientation, flip);
        }
        public Vector3 GetOrientationVector(Data.Enums.CutOrientation orientation, bool flip)
        {
            float[] normal = new float[3];
            definePlaneWithOrientation_Volume(_handle, normal, (int)orientation, flip);
            return new Vector3(normal[0], normal[1], normal[2]);
        }
        /// <summary>
        /// Returns a cube bbox around the volume depending on the used cuts
        /// </summary>
        /// <param name="cuts">List of the cuts of the scene</param>
        /// <returns>The cube bounding box around the volume</returns>
        public BBox GetCubeBoundingBox(List<Cut> cuts)
        {
            float[] planes = new float[cuts.Count * 6];
            int planesCount = 0;
            for (int ii = 0; ii < cuts.Count; ++ii)
            {
                if (cuts[ii].Orientation != Data.Enums.CutOrientation.Custom)
                {
                    for (int jj = 0; jj < 3; ++jj)
                    {
                        planes[ii * 6 + jj] = cuts[ii].Point[jj];
                        planes[ii * 6 + jj + 3] = cuts[ii].Normal[jj];
                    }
                    planesCount++;
                }
            }
            return new BBox(cube_bounding_box_Volume(_handle, planes, planesCount));
        }
        /// <summary>
        /// Get values of the closest voxel of the Volume for each vertex of the input surface
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public float[] GetVerticesValues(Surface surface)
        {
            float[] result = new float[surface.NumberOfVertices];
            get_vertices_values_Volume(_handle, surface.getHandle(), result);
            return result;
        }
        public Color[] ConvertValuesToColors(float[] values, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            Color[] colors = new Color[values.Length];
            float[] result = new float[values.Length * 4];
            get_colors_from_values_Volume(_handle, values, values.Length, negativeMin, negativeMax, positiveMin, positiveMax, alpha, result);
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = new Color(result[4 * i], result[4 * i + 1], result[4 * i + 2], result[4 * i + 3]);
            }
            return colors;
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
        [DllImport("hbp_export", EntryPoint = "create_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Volume();
        [DllImport("hbp_export", EntryPoint = "delete_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Volume(HandleRef handleVolume);
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
        [DllImport("hbp_export", EntryPoint = "cube_bounding_box_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr cube_bounding_box_Volume(HandleRef handleSurface, float[] planes, int planesCount);
        [DllImport("hbp_export", EntryPoint = "loadNiiFile_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern int loadNiiFile_Volume(HandleRef handleNii, string pathFile);
        [DllImport("hbp_export", EntryPoint = "get_vertices_values_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_vertices_values_Volume(HandleRef handleVolume, HandleRef surfaceHandle, float[] result);
        [DllImport("hbp_export", EntryPoint = "get_colors_from_values_Volume", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_colors_from_values_Volume(HandleRef handleVolume, float[] values, int valuesLength, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha, float[] result);
        #endregion
    }
}