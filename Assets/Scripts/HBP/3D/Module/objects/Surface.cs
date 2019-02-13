
/**
 * \file    Surface.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Surface class
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
    /// A DLL mesh class, for geometry computing purposes, can be converted to a Mesh
    /// </summary>
    public class Surface : CppDLLImportBase, ICloneable
    {
        #region Properties
        private int[] m_TriangleID = new int[0];             /**< raw array of triangles id */
        private Vector3[] m_Vertices = new Vector3[0];  /**< raw array of vertices */
        private Vector3[] m_Normals = new Vector3[0];   /**< raw array of normals for each vertex */
        private Vector2[] m_UV = new Vector2[0];        /**< raw array of texture uv for each vertex */
        private Color[] m_Colors = new Color[0];        /**< raw array of colors for each vertex */

        private int[] m_Sizes = new int[5];             /**< array for containing the sizes the mesh : */

        public bool IsLoaded { get; private set; }
        public bool IsMarsAtlasLoaded { get; private set; }

        /// <summary>
        /// Bounding Box of this surface
        /// </summary>
        public BBox BoundingBox
        {
            get
            {
                return new BBox(bounding_box_Surface(_handle));
            }
        }
        /// <summary>
        /// Number of vertices of this surface
        /// </summary>
        public int NumberOfVertices
        {
            get
            {
                return vertices_nb_Surface(_handle);
            }
        }
        /// <summary>
        /// Visibility mask of this surface
        /// </summary>
        public int[] VisibilityMask
        {
            get
            {
                int[] visibilityMask = new int[NumberOfTriangles];
                retrieve_visibility_mask(_handle, visibilityMask);
                return visibilityMask;
            }
        }
        /// <summary>
        /// Number of triangles of this surface
        /// </summary>
        public int NumberOfTriangles
        {
            get
            {
                m_Sizes = new int[6];
                sizes_Surface(_handle, m_Sizes);
                return m_Sizes[5];
            }
        }
        /// <summary>
        /// Number of visible triangles of this surface
        /// </summary>
        public int NumberOfVisibleTriangles
        {
            get
            {
                m_Sizes = new int[6];
                sizes_Surface(_handle, m_Sizes);
                return m_Sizes[1];
            }
        }

        GCHandle m_verticesHandle;
        GCHandle m_normalsHandle;
        GCHandle m_uvHandle;
        GCHandle m_triIdHandle;
        GCHandle m_colorHandle;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the surface by loading an obj mesh file
        /// </summary>
        /// <param name="pathObjFile"></param>
        /// <returns></returns>
        public bool LoadOBJFile(string pathObjFile)
        {
            IsLoaded = load_OBJ_file_Surface(_handle, pathObjFile)==1;
            if (!IsLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadObjFile -> can't load obj file to surface : " + pathObjFile);
            }
            return IsLoaded;
        }
        /// <summary>
        /// Initialize the surface by loading a GIFTI mesh file and applying the optional transform file
        /// </summary>
        /// <param name="gii">path of the GIFTI file </param>
        /// <param name="transform">if true apply the transform </param>
        /// <param name="transformation">transform file associated to the GIFTI file </param>
        /// <returns>true if sucesse, else false</returns>
        public bool LoadGIIFile(string gii, bool transform = false, string transformation = "")
        {
            if (transformation.Length == 0)
                transform = false;

            IsLoaded = load_GII_file_Surface(_handle, gii, transform ? 1 : 0, transformation) == 1;
            if (!IsLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadGIIFile -> can't load GII file to surface : " + gii + " " + transformation);
            }
            return IsLoaded;
        }
        /// <summary>
        /// Initialize the surface by loading a TRI mesh file and applying the optional transform file
        /// </summary>
        /// <param name="pathGIIFile">path of the TRI file </param>
        /// <param name="transform">if true apply the transform </param>
        /// <param name="pathTransformFile">transform file associated to the TRI file </param>
        /// <returns>true if sucesse, else false</returns>
        public bool LoadTRIFile(string pathTriFile, bool transform = false, string pathTransformFile = "")
        {
            if (pathTransformFile.Length == 0)
                transform = false;

            IsLoaded = load_TRI_file_Surface(_handle, pathTriFile, transform ? 1 : 0, pathTransformFile) == 1;
            if (!IsLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadTriFile -> can't load tri file to surface : " + pathTriFile);
            }
            return IsLoaded;
        }
        /// <summary>
        /// Define the mars atlas parcels gii file to be used for coloring the vertices
        /// </summary>
        /// <param name="pathMarsParcel"></param>
        /// <returns>false if no mars parcles files found</returns>
        public bool SearchMarsParcelFileAndUpdateColors(MarsAtlasIndex index, string pathMarsParcel)
        {
            IsMarsAtlasLoaded = seach_mars_parcel_file_and_update_colors_Surface(_handle, index.getHandle(), pathMarsParcel) == 1;
            return IsMarsAtlasLoaded;
        }
        /// <summary>
        /// Save surface to an obj (wawefront) file
        /// </summary>
        /// <param name="pathOBJFile"> path of the obj file</param>
        /// <param name="textureName"> name of the associated texture specified in the material file</param>
        /// <returns></returns>
        public bool SaveToOBJ(string pathOBJFile, string textureName = "")
        {
            bool fileSaved = save_to_OBJ_Surface( _handle, pathOBJFile, textureName) == 1;
            if (!fileSaved)
            {
                Debug.LogError("-ERROR : Surface::saveToObj -> can't save surface to obj file. ");
            }
            return fileSaved;
        }
        /// <summary>
        /// 
        /// </summary>
        public void ComputeNormals()
        {
            compute_normals_Surface(_handle);
        }
        /// <summary>
        /// Flip the side of all the triangles
        /// </summary>
        public void FlipTriangles()
        {
            flip_Surface(_handle);
        }
        /// <summary>
        /// Split the surface in n sub surfaces (the split is based on the triangles, not the vertices, be careful with the 65K vertices limit for GO)
        /// </summary>
        /// <param name="nbSubSurfaces"></param>
        /// <returns></returns>
        public Surface[] SplitToSurfaces(int nbSubSurfaces)
        {            
            HandleRef pSubSurfaces = new HandleRef(this, split_to_surfaces_Surface(_handle, nbSubSurfaces));

            int nbMultiSurface = nb_MultiSurface(pSubSurfaces);
            Surface[] splits = new Surface[nbMultiSurface];
            for (int ii = 0; ii < nbMultiSurface; ++ii)               
                splits[ii] = new Surface(move_MultiSurface(pSubSurfaces, ii));

            delete_MultiSurface(pSubSurfaces);
            return splits;
        }
        /// <summary>
        /// Update the visibility triangle mask of the mesh with the input array and return a new mesh made with invisible triangles
        /// </summary>
        /// <param name="visibilityMask"></param>
        /// <returns></returns>
        public Surface UpdateVisibilityMask(int[] visibilityMask)
        {
            DLL.Surface invisiblePartMesh = new DLL.Surface();
            update_visiblity_mask_Surface(_handle, invisiblePartMesh.getHandle(), visibilityMask);
            return invisiblePartMesh;
        }
        /// <summary>
        /// Update the visibility triangle mask of the mesh depending the input TriErased action
        /// </summary>
        /// <param name="rayDirection"></param>
        /// <param name="hitPoint"></param>
        /// <param name="mode"></param>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public Surface UpdateVisibilityMask(Vector3 rayDirection, Vector3 hitPoint, Data.Enums.TriEraserMode mode, float degrees)
        {
            float[] hitPointArray = new float[3], rayDirectionArray = new float[3];
            hitPointArray[0] = hitPoint.x;
            hitPointArray[1] = hitPoint.y;
            hitPointArray[2] = hitPoint.z;
            rayDirectionArray[0] = rayDirection.x;
            rayDirectionArray[1] = rayDirection.y;
            rayDirectionArray[2] = rayDirection.z;

            DLL.Surface invisiblePartMesh = new DLL.Surface();
            update_visiblity_mask_with_ray(_handle, invisiblePartMesh.getHandle(), rayDirectionArray, hitPointArray, (int) mode, degrees);
            return invisiblePartMesh;
        }
        /// <summary>
        /// Cut the mesh with the input cut planes and return it:  Surface[0]: cut mesh result,  Surface[1]: plane cut mesh for the first plane, Surface[2]: plane cut mesh for the second plane...
        /// </summary>
        /// <param name="cutPlanes"></param>
        /// <param name="removeFrontPlane"> NOT USED </param>
        /// <param name="noHoles"></param>
        /// <returns></returns>
        public Surface[] Cut(Cut[] cutPlanes, bool noHoles = false, bool strongCuts = true)
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
                    planes[ii * 6 + jj] = cutPlanes[ii].Point[jj];
                    planes[ii * 6 + jj + 3] = cutPlanes[ii].Normal[jj];
                }
            }

            // do the cut            
            HandleRef pCutMultiSurface = new HandleRef(this, cut_Surface(_handle, planes, cutPlanes.Length, noHoles?1:0, strongCuts?1:0));

            // move data            
            int nbMultiSurface = nb_MultiSurface(pCutMultiSurface);
            Surface[] cuts = new Surface[nbMultiSurface];
            for (int ii = 0; ii < nbMultiSurface; ++ii)
                cuts[ii] = new Surface(move_MultiSurface(pCutMultiSurface, ii));

            // clean the multi surface
            delete_MultiSurface(pCutMultiSurface);
            return cuts;
        }
        /// <summary>
        /// Only generate the cut surfaces (without cutting the mesh)
        /// </summary>
        /// <param name="cutPlanes"></param>
        /// <param name="noHoles"></param>
        /// <param name="strongCuts"></param>
        /// <returns></returns>
        public Surface[] GenerateCutSurfaces(Cut[] cutPlanes, bool noHoles = false, bool strongCuts = true)
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
                    planes[ii * 6 + jj] = cutPlanes[ii].Point[jj];
                    planes[ii * 6 + jj + 3] = cutPlanes[ii].Normal[jj];
                }
            }

            // do the cut            
            HandleRef pCutMultiSurface = new HandleRef(this, generate_cuts_Surface(_handle, planes, cutPlanes.Length, noHoles ? 1 : 0, strongCuts ? 1 : 0));

            // move data            
            int nbMultiSurface = nb_MultiSurface(pCutMultiSurface);
            Surface[] cuts = new Surface[nbMultiSurface];
            for (int ii = 0; ii < nbMultiSurface; ++ii)
                cuts[ii] = new Surface(move_MultiSurface(pCutMultiSurface, ii));

            // clean the multi surface
            delete_MultiSurface(pCutMultiSurface);
            return cuts;
        }
        /// <summary>
        /// Merge the mesh with the input one
        /// </summary>
        /// <param name="surfaceToAdd"></param>
        public void Append(Surface surfaceToAdd)
        {
            merge_Surface(_handle, surfaceToAdd.getHandle());
            IsLoaded &= surfaceToAdd.IsLoaded;
            IsMarsAtlasLoaded &= surfaceToAdd.IsMarsAtlasLoaded;
        }
        /// <summary>
        /// Return the computer offset for the input cut plane and the wanted number of cuts
        /// </summary>
        /// <param name="cutPlane"></param>
        /// <param name="nbCuts"></param>
        /// <returns></returns>
        public float SizeOffsetCutPlane(Plane cutPlane, int nbCuts)
        {            
            return size_offset_cut_plane_Surface(_handle, cutPlane.ConvertToArray(), nbCuts);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="all"></param>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="uv"></param>
        /// <param name="triangles"></param>
        public void UpdateMeshFromDLL(Mesh mesh, bool all = true, bool vertices = true, bool normals = true, bool uv = true, bool triangles = true, bool colors = true)
        {
            try
            {
                UnityEngine.Profiling.Profiler.BeginSample("TEST-updateMeshMashall 1");

                m_Sizes = new int[6];
                sizes_Surface(_handle, m_Sizes);

                if (m_Vertices.Length != m_Sizes[0] || m_Vertices.Length == 0)
                {
                    if (m_verticesHandle.IsAllocated) m_verticesHandle.Free();
                    m_Vertices = new Vector3[m_Sizes[0]];
                    m_verticesHandle = GCHandle.Alloc(m_Vertices, GCHandleType.Pinned);
                }

                if (m_Normals.Length != m_Sizes[2] || m_Normals.Length == 0)
                {
                    if (m_normalsHandle.IsAllocated) m_normalsHandle.Free();
                    m_Normals = new Vector3[m_Sizes[2]];
                    m_normalsHandle = GCHandle.Alloc(m_Normals, GCHandleType.Pinned);
                }

                if (m_UV.Length != m_Sizes[3] || m_UV.Length == 0)
                {
                    if (m_uvHandle.IsAllocated) m_uvHandle.Free();
                    m_UV = new Vector2[m_Sizes[3]];
                    m_uvHandle = GCHandle.Alloc(m_UV, GCHandleType.Pinned);
                }

                int nbTri = (m_Sizes[1] * 3);
                if (m_TriangleID.Length != nbTri || m_TriangleID.Length == 0)
                {
                    if (m_triIdHandle.IsAllocated) m_triIdHandle.Free();
                    m_TriangleID = new int[nbTri];
                    m_triIdHandle = GCHandle.Alloc(m_TriangleID, GCHandleType.Pinned);
                }

                if (m_Colors.Length != m_Sizes[4] || m_Colors.Length == 0)
                {
                    if (m_colorHandle.IsAllocated) m_colorHandle.Free();
                    m_Colors = new Color[m_Sizes[4]];
                    m_colorHandle = GCHandle.Alloc(m_Colors, GCHandleType.Pinned);
                }

                UnityEngine.Profiling.Profiler.EndSample();
                UnityEngine.Profiling.Profiler.BeginSample("TEST-updateMeshMashall 3");

                update_mesh_Surface(_handle, m_verticesHandle.AddrOfPinnedObject(), m_normalsHandle.AddrOfPinnedObject(), m_uvHandle.AddrOfPinnedObject(), m_triIdHandle.AddrOfPinnedObject(), m_colorHandle.AddrOfPinnedObject());

                UnityEngine.Profiling.Profiler.EndSample();
                UnityEngine.Profiling.Profiler.BeginSample("TEST-updateMeshMashall 4"); // heavy

                mesh.Clear();

                if (all || vertices)
                {
                    mesh.vertices = m_Vertices;
                }

                if (all || normals)
                    mesh.normals = m_Normals;

                if (all || colors)
                {
                    if (m_Colors.Length > 0)
                    {
                        mesh.colors = m_Colors;
                    }
                }

                if (all || uv)
                    mesh.uv = m_UV;

                if (all || triangles)
                {
                    mesh.triangles = m_TriangleID;

                }

                UnityEngine.Profiling.Profiler.EndSample();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(e.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void DisplaySizes()
        {
            m_Sizes = new int[6];
            sizes_Surface(_handle, m_Sizes);

            int nbFloatVertices = m_Sizes[0] * 3;
            int nbFloatNormals = m_Sizes[2] * 3;
            int nbVisibleIntTriIndices = m_Sizes[1] * 3;
            int nbFloatUV = m_Sizes[3] * 2;
            int nbFLoatColors = m_Sizes[4] * 3;
            int nbAllIntTriIndices = m_Sizes[5] * 3;
            Debug.Log("debug surface : " + nbFloatVertices + " " + nbFloatNormals + " " + nbVisibleIntTriIndices + " " + nbFloatUV + " " + nbFLoatColors + " " + nbAllIntTriIndices);            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="surface"></param>
        public void SwapDLLHandle(Surface surface)
        {
            HandleRef buffer = surface.getHandle();
            surface._handle = _handle;
            _handle = buffer;
        }
        /// <summary>
        /// Simplify mesh
        /// </summary>
        /// <returns></returns>
        public Surface Simplify(int numberOfTriangles = 10000, int agressiveness = 7)
        {
            Surface surface = new Surface(this);
            simplify_mesh_Surface(surface.getHandle(), numberOfTriangles, agressiveness);
            return surface;
        }
        /// <summary>
        /// Returns true if the point is inside the surface
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsPointInside(RawSiteList rawSiteList, int id)
        {
            return is_point_inside_Surface(_handle, rawSiteList.getHandle(), id);
        }
        /// <summary>
        /// Returns a cube bbox around the mesh depending on the cuts used
        /// </summary>
        /// <param name="cuts"></param>
        /// <returns></returns>
        public BBox GetCubeBoundingBox(Cut[] cuts)
        {
            float[] planes = new float[cuts.Length * 6];
            int planesCount = 0;
            for (int ii = 0; ii < cuts.Length; ++ii)
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
            return new BBox(cube_bounding_box_Surface(_handle, planes, planesCount));
        }
        /// <summary>
        /// Get the surface made of precision * precision * precision vertices inside the bounding box
        /// </summary>
        /// <param name="precision"></param>
        /// <returns></returns>
        public Surface GetSurfaceFromBoundingBox(int precision)
        {
            return new Surface(get_mesh_from_bounding_box_Surface(_handle, precision));
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Surface default constructor
        /// </summary>
        public Surface() : base()
        { }
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
            m_Vertices = new Vector3[other.m_Vertices.Length];
            other.m_Vertices.CopyTo(m_Vertices, 0);

            m_Normals = new Vector3[other.m_Normals.Length];
            other.m_Normals.CopyTo(m_Normals, 0);

            m_TriangleID = new int[other.m_TriangleID.Length];
            other.m_TriangleID.CopyTo(m_TriangleID, 0);

            m_UV = new Vector2[other.m_UV.Length];
            other.m_UV.CopyTo(m_UV, 0);

            IsLoaded = other.IsLoaded;
            IsMarsAtlasLoaded = other.IsLoaded;
        }
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this,create_Surface());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_Surface(_handle);
        }
        /// <summary>
        /// Clone the surface
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            Surface clonedSurface = new Surface(this);
            clonedSurface.IsLoaded = IsLoaded;
            clonedSurface.IsMarsAtlasLoaded = IsMarsAtlasLoaded;
            return clonedSurface;
        }
        #endregion

        #region DLLImport
        #region Surface

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Surface();
        [DllImport("hbp_export", EntryPoint = "delete_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "clone_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr clone_Surface(HandleRef surfaceToClone);

        // save / load
        [DllImport("hbp_export", EntryPoint = "save_to_OBJ_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int save_to_OBJ_Surface(HandleRef handleSurface, string pathFile, string textureName);
        [DllImport("hbp_export", EntryPoint = "load_GII_file_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_GII_file_Surface(HandleRef handleSurface, string pathFile, int transform, string pathTranformFile);
        [DllImport("hbp_export", EntryPoint = "load_TRI_file_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_TRI_file_Surface(HandleRef handleSurface, string pathFile, int transform, string pathTranformFile);
        [DllImport("hbp_export", EntryPoint = "load_OBJ_file_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_OBJ_file_Surface(HandleRef handleSurface, string pathFile);
        [DllImport("hbp_export", EntryPoint = "seach_mars_parcel_file_and_update_colors_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int seach_mars_parcel_file_and_update_colors_Surface(HandleRef handleSurface, HandleRef handleMarsIndex, string pathMarsParcelsFile);        

        // actions				
        [DllImport("hbp_export", EntryPoint = "flip_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void flip_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "compute_normals_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_normals_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "merge_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void merge_Surface(HandleRef handleSurface, HandleRef handleSurfaceToAdd);
        [DllImport("hbp_export", EntryPoint = "cut_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr cut_Surface(HandleRef handleSurface, float[] planes, int nbPlanes, int noHoles, int strongCuts);
        [DllImport("hbp_export", EntryPoint = "generate_cuts_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_cuts_Surface(HandleRef handleSurface, float[] planes, int nbPlanes, int noHoles, int strongCuts);
        [DllImport("hbp_export", EntryPoint = "split_to_surfaces_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr split_to_surfaces_Surface(HandleRef handleSurface, int nbSubSurfaces);
        [DllImport("hbp_export", EntryPoint = "middle_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr middle_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "update_mesh_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_mesh_Surface(HandleRef handleSurface, IntPtr vertices, IntPtr normals, IntPtr uv, IntPtr triangles, IntPtr colors);
        [DllImport("hbp_export", EntryPoint = "update_vertices_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_vertices_Surface(HandleRef handleSurface, IntPtr vertices);
        [DllImport("hbp_export", EntryPoint = "update_normals_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_normals_Surface(HandleRef handleSurface, IntPtr normals);
        [DllImport("hbp_export", EntryPoint = "update_UV_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_UV_Surface(HandleRef handleSurface, IntPtr uv);
        [DllImport("hbp_export", EntryPoint = "update_triangles_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_triangles_Surface(HandleRef handleSurface, IntPtr triangles);
        [DllImport("hbp_export", EntryPoint = "simplify_mesh_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void simplify_mesh_Surface(HandleRef handleSurface, int triangleCount, int agressiveness);
        [DllImport("hbp_export", EntryPoint = "is_point_inside_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool is_point_inside_Surface(HandleRef handleSurface, HandleRef handleRawSiteList, int id);

        [DllImport("hbp_export", EntryPoint = "update_visiblity_mask_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_visiblity_mask_Surface(HandleRef handleSurface, HandleRef handleInvisiblePartSurface, int[] visibilityMask);
        [DllImport("hbp_export", EntryPoint = "update_visiblity_mask_with_ray", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_visiblity_mask_with_ray(HandleRef handleSurface, HandleRef handleInvisiblePartSurface, float[] rayDirection, float[] hitPoint, int mode, float degrees);
        [DllImport("hbp_export", EntryPoint = "retrieve_visibility_mask", CallingConvention = CallingConvention.Cdecl)]
        static private extern void retrieve_visibility_mask(HandleRef handleSurface, int[] visibilityMask);



        // retrieve data						
        [DllImport("hbp_export", EntryPoint = "sizes_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int sizes_Surface(HandleRef handleSurface, int[] sizes);
        [DllImport("hbp_export", EntryPoint = "vertices_nb_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int vertices_nb_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "nb_visible_triangles_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int nb_visible_triangles_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "vertices_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void vertices_Surface(HandleRef handleSurface, IntPtr verticesArray);
        [DllImport("hbp_export", EntryPoint = "UV_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void UV_Surface(HandleRef handleSurface, float[] texturesUVArray);
        [DllImport("hbp_export", EntryPoint = "bounding_box_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr bounding_box_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "cube_bounding_box_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr cube_bounding_box_Surface(HandleRef handleSurface, float[] planes, int planesCount);
        [DllImport("hbp_export", EntryPoint = "size_offset_cut_plane_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern float size_offset_cut_plane_Surface(HandleRef handleSurface, float[] planeCut, int nbCuts);
        [DllImport("hbp_export", EntryPoint = "get_mesh_from_bounding_box_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_mesh_from_bounding_box_Surface(HandleRef handleSurface, int precision);


        #endregion
        #region MultiSurface

        // memory management
        [DllImport("hbp_export", EntryPoint = "delete_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MultiSurface(HandleRef handleMultiSurface);
        [DllImport("hbp_export", EntryPoint = "move_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr move_MultiSurface(HandleRef handleMultiSurface, int numSurface);
        // retrieve data
        [DllImport("hbp_export", EntryPoint = "nb_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int nb_MultiSurface(HandleRef handleMultiSurface);

        [DllImport("hbp_export", EntryPoint = "data_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void data_MultiSurface(int numSurface, HandleRef handleMultiSurface, IntPtr verticesArray, IntPtr normalsArray, IntPtr triIndicesArray, IntPtr texturesUVArray);

        #endregion
        #endregion
    }
}