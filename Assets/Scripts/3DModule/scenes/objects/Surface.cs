
/**
 * \file    Surface.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Surface class
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.VISU3D.DLL
{
    /// <summary>
    /// A DLL mesh class, for geometry computing purposes, can be converted to a Mesh
    /// </summary>
    public class Surface : CppDLLImportBase, ICloneable
    {
        #region members

        private int[] m_triID = new int[0]; /**< raw array of triangles id */
        private float[] m_vertices = new float[0]; /**< raw array of vertices positions */
        private float[] m_normals = new float[0]; /**< raw array of normals for each vertex */
        private float[] m_uv = new float[0]; /**< raw array of texture uv for each vertex */
        Vector2[] m_uvMesh = new Vector2[0]; /**< texture uv for each vertex */

        private List<Vector3> m_verticesL = new List<Vector3>();

        #endregion members

        #region functions



        /// <summary>
        /// Initialize the surface by loading an obj mesh file
        /// </summary>
        /// <param name="pathObjFile"></param>
        /// <returns></returns>
        public bool loadObjFile(string pathObjFile)
        {
            // TODO : add loading code error
            bool fileLoaded = loadObjFile_Surface(_handle, pathObjFile);
            if (!fileLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadObjFile -> can't load obj file to surface : " + pathObjFile);
            }
            return fileLoaded;
        }

        /// <summary>
        /// Initialize the surface by loading a GIFTI mesh file and applying the optional transfrom file
        /// </summary>
        /// <param name="pathGIIFile">path of the GIFTI file </param>
        /// <param name="transform">if true apply the transform </param>
        /// <param name="pathTransformFile">transform file associated to the GIFTI file </param>
        /// <returns>true if sucesse, else false</returns>
        public bool loadGIIFile(string pathGIIFile, bool transform = false, string pathTransformFile = "")
        {
            if (pathTransformFile.Length == 0)
                transform = false;

            bool fileLoaded = loadGiftiFile_Surface(_handle, pathGIIFile, transform, pathTransformFile);
            if (!fileLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadGIIFile -> can't load GII file to surface : " + pathGIIFile + " " + pathTransformFile);
            }
            return fileLoaded;
        }

        /// <summary>
        /// Initialize the surface by loading a TRI mesh file and applying the optional transfrom file
        /// </summary>
        /// <param name="pathGIIFile">path of the TRI file </param>
        /// <param name="transform">if true apply the transform </param>
        /// <param name="pathTransformFile">transform file associated to the TRI file </param>
        /// <returns>true if sucesse, else false</returns>
        public bool loadTriFile(string pathTriFile, bool transform = false, string pathTransformFile = "")
        {
            if (pathTransformFile.Length == 0)
                transform = false;

            bool fileLoaded = loadTriFile_Surface(_handle, pathTriFile, transform, pathTransformFile);
            if (!fileLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadTriFile -> can't load tri file to surface : " + pathTriFile);
            }
            return fileLoaded;
        }

        /// <summary>
        /// Save surface to an obj (wawefront) file
        /// </summary>
        /// <param name="pathOBJFile"> path of the obj file</param>
        /// <param name="textureName"> name of the associated texture specified in the material file</param>
        /// <returns></returns>
        public bool saveToObj(string pathOBJFile, string textureName = "")
        {
            bool fileSaved = saveToObj_Surface(_handle, pathOBJFile, textureName);
            if (!fileSaved)
            {
                Debug.LogError("-ERROR : Surface::saveToObj -> can't save surface to obj file. ");
            }
            return fileSaved;
        }

        /// <summary>
        /// Recompute the normals
        /// </summary>
        public void computeNormals()
        {
            computeNormals_Surface(_handle);
        }

        /// <summary>
        /// Flip the triangles side
        /// </summary>
        public void flipSurface()
        {
            flip_Surface(_handle);
        }

        /// <summary>
        /// Split the surface in n sub surfaces
        /// </summary>
        /// <param name="nbSubSurfaces"></param>
        /// <returns></returns>
        public Surface[] splitToSurfaces(int nbSubSurfaces)
        {
            HandleRef pSubSurfaces = new HandleRef(this, splitToSurfaces_Surface(_handle, nbSubSurfaces));

            Surface[] splits = new Surface[nb_MultiSurface(pSubSurfaces)];
            for (int ii = 0; ii < nb_MultiSurface(pSubSurfaces); ++ii)
            {
                splits[ii] = new Surface(move_MultiSurface(pSubSurfaces, ii));
            }

            delete_MultiSurface(pSubSurfaces);

            return splits;
        }

        /// <summary>
        /// Return the surface number of vertices
        /// </summary>
        /// <returns></returns>
        public int verticesNb() { return verticesNb_Surface(_handle); }

        public BBox boundingBox()
        {
            return new BBox(getBoundingBox_Surface(_handle));
        }

        public Surface middlePointsMesh()
        {
            Surface middlePointsMesh = new Surface(middlePointsMesh_Surface(_handle));
            return middlePointsMesh;
        }

        public Surface[] cutSurface(Plane[] cutPlanes, bool[] removeFrontPlane, bool noHoles = false)
        {
            // check planes
            if (cutPlanes.Length <= 0)
            {
                Debug.LogError("-ERROR : Surface::cutSurface -> nb of planes <= 0. ");
                Surface[] returnError = new Surface[1];
                return returnError;
            }

            // init plane
            float[] planes = new float[cutPlanes.Length * 6];
            for (int ii = 0; ii < cutPlanes.Length; ++ii)
            {
                for (int jj = 0; jj < 3; ++jj)
                {
                    planes[ii * 6 + jj] = cutPlanes[ii].point[jj];
                    planes[ii * 6 + jj + 3] = cutPlanes[ii].normal[jj];
                }
            }

            // do the cut
            HandleRef pCutMultiSurface = new HandleRef(this, cut_Surface(_handle, removeFrontPlane, planes, cutPlanes.Length, noHoles));

            // move data
            Surface[] cuts = new Surface[nb_MultiSurface(pCutMultiSurface)];
            for (int ii = 0; ii < nb_MultiSurface(pCutMultiSurface); ++ii)
            {
                cuts[ii] = new Surface(move_MultiSurface(pCutMultiSurface, ii));
            }

            // clean the multi surface
            delete_MultiSurface(pCutMultiSurface);

            return cuts;
        }

        public void addToSurface(Surface surfaceToAdd)
        {
            merge_Surface(_handle, surfaceToAdd.getHandle());
        }

        public float sizeOffsetCutPlane(Plane cutPlane, int nbCuts)
        {
            return sizeOffsetCutPlane_Surface(_handle, cutPlane.convertToArray(), nbCuts);
        }


        public void updateMeshMashall(Mesh mesh)
        {
            Profiler.BeginSample("TEST-updateMeshMashall 1");

            int[] surfaceSizes = new int[4];
            getSizes_Surface(_handle, surfaceSizes);

            //Vector3[] vertices = mesh.vertices;
            //Array.Resize(ref vertices, surfaceSizes[0]);
            //Vector3[] normals = mesh.normals;
            //Array.Resize(ref normals, surfaceSizes[2]);
            //Vector2[] uv = mesh.uv;
            //Array.Resize(ref uv, surfaceSizes[3]);
            //int[] triId = mesh.triangles;
            //Array.Resize(ref triId, surfaceSizes[1] * 3);

            Vector3[] vertices = new Vector3[surfaceSizes[0]];
            Vector3[] normals = new Vector3[surfaceSizes[2]];
            Vector2[] uv = new Vector2[surfaceSizes[3]];
            int[] triId = new int[surfaceSizes[1] * 3];

            Profiler.EndSample();
            Profiler.BeginSample("TEST-updateMeshMashall 2");

            GCHandle verticesHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            GCHandle normalsHandle = GCHandle.Alloc(normals, GCHandleType.Pinned);
            GCHandle uvHandle = GCHandle.Alloc(uv, GCHandleType.Pinned);
            GCHandle triIdHandle = GCHandle.Alloc(triId, GCHandleType.Pinned);

            Profiler.EndSample();
            Profiler.BeginSample("TEST-updateMeshMashall 3");

            updateMesh_Surface(_handle, verticesHandle.AddrOfPinnedObject(), normalsHandle.AddrOfPinnedObject(), uvHandle.AddrOfPinnedObject(), triIdHandle.AddrOfPinnedObject());

            Profiler.EndSample();
            Profiler.BeginSample("TEST-updateMeshMashall 4");

            verticesHandle.Free();
            normalsHandle.Free();
            uvHandle.Free();
            triIdHandle.Free();

            mesh.Clear();

            Profiler.EndSample();
            Profiler.BeginSample("TEST-updateMeshMashall 5");

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triId;

            Profiler.EndSample();
        }

        public void udpateMeshWithSurface(Mesh mesh)
        {
            Profiler.BeginSample("TEST-udpateMeshWithSurface 1");

            Profiler.BeginSample("TEST-udpateMeshWithSurface 11");

            // retrives buffers size
            int[] surfaceSizes = new int[4];
            getSizes_Surface(_handle, surfaceSizes);
            int nbFloatVertices = surfaceSizes[0] * 3;
            int nbFloatNormals = surfaceSizes[2] * 3;
            int nbIntTriIndices = surfaceSizes[1] * 3;
            int nbFloatUV = surfaceSizes[3] * 2;

            if (nbFloatVertices != nbFloatNormals)
            {
                Debug.LogError("-ERROR : Surface::udpateMeshWithSurface -> vertices number != normals number . " + nbFloatVertices + " " + nbFloatNormals);
                return;
            }

            // allocate mesh data
            Array.Resize(ref m_vertices, nbFloatVertices);
            Array.Resize(ref m_normals, nbFloatVertices);
            Array.Resize(ref m_triID, nbIntTriIndices);
            Array.Resize(ref m_uv, nbFloatUV);
            getData_Surface(_handle, m_vertices, m_normals, m_triID, m_uv);

            Profiler.EndSample();
            Profiler.BeginSample("TEST-udpateMeshWithSurface 12");


            Vector3[] vertices = mesh.vertices;

            //List<Vector3> arrayV = new List<Vector3>(surfaceSizes[0]);
            //Array.Resize(ref arrayV, surfaceSizes[0]);

            Array.Resize(ref vertices, surfaceSizes[0]);
            Vector3[] normals = mesh.normals;
            Array.Resize(ref normals, surfaceSizes[2]);
            Vector2[] uv = mesh.uv;
            Array.Resize(ref uv, surfaceSizes[3]);

            for (int ii = 0; ii < vertices.Length; ++ii)
            {
                vertices[ii] = new Vector3(m_vertices[3 * ii], m_vertices[3 * ii + 1], m_vertices[3 * ii + 2]);
                normals[ii] = new Vector3(m_normals[3 * ii], m_normals[3 * ii + 1], m_normals[3 * ii + 2]);

                if (uv.Length != 0)
                    uv[ii] = new Vector2(m_uv[2 * ii], m_uv[2 * ii + 1]);
            }
            Profiler.EndSample();
            Profiler.BeginSample("TEST-udpateMeshWithSurface 13");


            mesh.Clear();
            //mesh.SetVertices();

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = m_triID;

            // optimize the mesh
            //mesh.Optimize();

            Profiler.EndSample();
            Profiler.EndSample();
        }

        public void displaySizes()
        {
            int[] surfaceSizes = new int[4];
            getSizes_Surface(_handle, surfaceSizes);
            int nbFloatVertices = surfaceSizes[0] * 3;
            int nbFloatNormals = surfaceSizes[2] * 3;
            int nbIntTriIndices = surfaceSizes[1] * 3;
            int nbFloatUV = surfaceSizes[3] * 2;


            Debug.Log("debug surface : " + nbFloatVertices + " " + nbFloatNormals + " " + nbIntTriIndices + " " + nbFloatUV);            
        }


        #endregion functions

        #region memory_management

        /// <summary>
        /// Surface default constructor
        /// </summary>
        public Surface() : base() { }

        /// <summary>
        /// Surface constructor with an already  allocated dll surface
        /// </summary>
        /// <param name="surfaceHandle"></param>
        public Surface(IntPtr surfaceHandle) : base(surfaceHandle) { }

        /// <summary>
        /// Surface copy constructor
        /// </summary>
        /// <param name="other"></param>
        public Surface(Surface other) : base(clone_Surface(other.getHandle()))
        {
            m_vertices = new float[other.m_vertices.Length];
            other.m_vertices.CopyTo(m_vertices, 0);

            m_normals = new float[other.m_normals.Length];
            other.m_normals.CopyTo(m_normals, 0);

            m_triID = new int[other.m_triID.Length];
            other.m_triID.CopyTo(m_triID, 0);

            m_uv = new float[other.m_uv.Length];
            other.m_uv.CopyTo(m_uv, 0);

            m_uvMesh = new Vector2[other.m_uv.Length];
            other.m_uvMesh.CopyTo(m_uvMesh, 0);
        }

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void createDLLClass()
        {
            _handle = new HandleRef(this, create_Surface());
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void deleteDLLClass()
        {
            delete_Surface(_handle);
        }

        /// <summary>
        /// Clone the surface
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new Surface(this);
        }        

        #endregion memory_management

        #region DLLImport
        #region Surface

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Surface();

        [DllImport("hbp_export", EntryPoint = "clone_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr clone_Surface(HandleRef surfaceToClone);

        [DllImport("hbp_export", EntryPoint = "delete_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Surface(HandleRef handleSurface);

        // save / load
        [DllImport("hbp_export", EntryPoint = "saveToObj_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool saveToObj_Surface(HandleRef handleSurface, string pathFile, string textureName);

        [DllImport("hbp_export", EntryPoint = "loadGiftiFile_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool loadGiftiFile_Surface(HandleRef handleSurface, string pathFile, bool transform, string pathTranformFile);

        [DllImport("hbp_export", EntryPoint = "loadTriFile_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool loadTriFile_Surface(HandleRef handleSurface, string pathFile, bool transform, string pathTranformFile);

        [DllImport("hbp_export", EntryPoint = "loadObjFile_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool loadObjFile_Surface(HandleRef handleSurface, string pathFile);

        // actions				
        [DllImport("hbp_export", EntryPoint = "flip_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void flip_Surface(HandleRef handleSurface);

        [DllImport("hbp_export", EntryPoint = "computeNormals_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void computeNormals_Surface(HandleRef handleSurface);

        [DllImport("hbp_export", EntryPoint = "merge_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void merge_Surface(HandleRef handleSurface, HandleRef handleSurfaceToAdd);

        [DllImport("hbp_export", EntryPoint = "cut_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr cut_Surface(HandleRef handleSurface, bool[] removeFrontPlane, float[] planes, int nbPlanes, bool noHoles);

        [DllImport("hbp_export", EntryPoint = "splitToSurfaces_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr splitToSurfaces_Surface(HandleRef handleSurface, int nbSubSurfaces);
 
        [DllImport("hbp_export", EntryPoint = "middlePointsMesh_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr middlePointsMesh_Surface(HandleRef handleSurface);

        // retrieve data						
        [DllImport("hbp_export", EntryPoint = "getSizes_Surface", CallingConvention = CallingConvention.Cdecl)]
        // sizes : 0 -> vertices, 1 -> triangles id, 2 -> normals, 3 -> colors, 4 -> textures uv
        static private extern int getSizes_Surface(HandleRef handleSurface, int[] sizes);

        [DllImport("hbp_export", EntryPoint = "verticesNb_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int verticesNb_Surface(HandleRef handleSurface);

        [DllImport("hbp_export", EntryPoint = "getVertices_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getVertices_Surface(HandleRef handleSurface, IntPtr verticesArray);

        [DllImport("hbp_export", EntryPoint = "getData_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getData_Surface(HandleRef handleSurface, float[] verticesArray, float[] normalsArray, int[] triIndicesArray, float[] texturesUVArray);

        [DllImport("hbp_export", EntryPoint = "getUV_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getUV_Surface(HandleRef handleSurface, float[] texturesUVArray);

        [DllImport("hbp_export", EntryPoint = "getBoundingBox_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr getBoundingBox_Surface(HandleRef handleSurface);

        [DllImport("hbp_export", EntryPoint = "getCenter_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getCenter_BBox(IntPtr bboxClass, float[] center);

        [DllImport("hbp_export", EntryPoint = "sizeOffsetCutPlane_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern float sizeOffsetCutPlane_Surface(HandleRef handleSurface, float[] planeCut, int nbCuts);

        #endregion Surface
        #region MultiSurface

        // memory management
        [DllImport("hbp_export", EntryPoint = "delete_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MultiSurface(HandleRef handleMultiSurface);

        [DllImport("hbp_export", EntryPoint = "move_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr move_MultiSurface(HandleRef handleMultiSurface, int numSurface);

        // retrieve data
        [DllImport("hbp_export", EntryPoint = "nb_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int nb_MultiSurface(HandleRef handleMultiSurface);

        [DllImport("hbp_export", EntryPoint = "getData_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getData_MultiSurface(int numSurface, HandleRef handleMultiSurface, IntPtr verticesArray, IntPtr normalsArray, IntPtr triIndicesArray, IntPtr texturesUVArray);


        [DllImport("hbp_export", EntryPoint = "updateMesh_Surface", CallingConvention = CallingConvention.Cdecl)]
        private static extern void updateMesh_Surface(HandleRef handleSurface, IntPtr vertices, IntPtr normals, IntPtr uv, IntPtr triangles);

        [DllImport("hbp_export", EntryPoint = "updateVertices_Surface", CallingConvention = CallingConvention.Cdecl)]
        private static extern void updateVertices_Surface(HandleRef handleSurface, IntPtr vertices);

        [DllImport("hbp_export", EntryPoint = "updateNormals_Surface", CallingConvention = CallingConvention.Cdecl)]
        private static extern void updateNormals_Surface(HandleRef handleSurface, IntPtr normals);

        [DllImport("hbp_export", EntryPoint = "updateTextCoord_Surface", CallingConvention = CallingConvention.Cdecl)]
        private static extern void updateTextCoord_Surface(HandleRef handleSurface, IntPtr uv);

        [DllImport("hbp_export", EntryPoint = "updateTriangles_Surface", CallingConvention = CallingConvention.Cdecl)]
        private static extern void updateTriangles_Surface(HandleRef handleSurface, IntPtr triangles);

        #endregion MultiSurface
        #endregion DLLImport        
    }
}