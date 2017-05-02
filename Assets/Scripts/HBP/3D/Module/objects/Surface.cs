
/**
 * \file    Surface.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Surface class
 */

// system
using System;
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

        private int[] m_triID = new int[0];             /**< raw array of triangles id */
        private Vector3[] m_vertices = new Vector3[0];  /**< raw array of vertices */
        private Vector3[] m_normals = new Vector3[0];   /**< raw array of normals for each vertex */
        private Vector2[] m_uv = new Vector2[0];        /**< raw array of texture uv for each vertex */
        private Color[] m_colors = new Color[0];        /**< raw array of colors for each vertex */

        private int[] m_sizes = new int[5];             /**< array for containing the sizes the mesh : */

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
        public bool load_obj_file(string pathObjFile)
        {
            bool fileLoaded = load_OBJ_file_Surface(_handle, pathObjFile)==1;
            if (!fileLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadObjFile -> can't load obj file to surface : " + pathObjFile);
            }
            return fileLoaded;
        }

        /// <summary>
        /// Initialize the surface by loading a GIFTI mesh file and applying the optional transform file
        /// </summary>
        /// <param name="pathGIIFile">path of the GIFTI file </param>
        /// <param name="transform">if true apply the transform </param>
        /// <param name="pathTransformFile">transform file associated to the GIFTI file </param>
        /// <returns>true if sucesse, else false</returns>
        public bool load_GII_file(string pathGIIFile, bool transform = false, string pathTransformFile = "")
        {
            if (pathTransformFile.Length == 0)
                transform = false;

            bool fileLoaded = load_GII_file_Surface(_handle, pathGIIFile , transform?1:0, pathTransformFile) == 1;
            if (!fileLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadGIIFile -> can't load GII file to surface : " + pathGIIFile + " " + pathTransformFile);
            }
            return fileLoaded;
        }

        /// <summary>
        /// Initialize the surface by loading a TRI mesh file and applying the optional transform file
        /// </summary>
        /// <param name="pathGIIFile">path of the TRI file </param>
        /// <param name="transform">if true apply the transform </param>
        /// <param name="pathTransformFile">transform file associated to the TRI file </param>
        /// <returns>true if sucesse, else false</returns>
        public bool load_tri_file(string pathTriFile, bool transform = false, string pathTransformFile = "")
        {
            if (pathTransformFile.Length == 0)
                transform = false;

            bool fileLoaded = load_TRI_file_Surface(_handle, pathTriFile, transform ? 1 : 0, pathTransformFile) == 1;
            if (!fileLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadTriFile -> can't load tri file to surface : " + pathTriFile);
            }
            return fileLoaded;
        }

        /// <summary>
        /// Define the mars atlas parcels gii file to be used for coloring the vertices
        /// </summary>
        /// <param name="pathMarsParcel"></param>
        /// <returns>false if no mars parcles files found</returns>
        public bool seach_mars_parcel_file_and_update_colors(MarsAtlasIndex index, string pathMarsParcel)
        {
            return seach_mars_parcel_file_and_update_colors_Surface(_handle, index.getHandle(), pathMarsParcel) == 1;
        }

        /// <summary>
        /// Save surface to an obj (wawefront) file
        /// </summary>
        /// <param name="pathOBJFile"> path of the obj file</param>
        /// <param name="textureName"> name of the associated texture specified in the material file</param>
        /// <returns></returns>
        public bool save_to_obj(string pathOBJFile, string textureName = "")
        {
            bool fileSaved = save_to_OBJ_Surface( _handle, pathOBJFile, textureName) == 1;
            if (!fileSaved)
            {
                Debug.LogError("-ERROR : Surface::saveToObj -> can't save surface to obj file. ");
            }
            return fileSaved;
        }

        public void compute_normals()
        {
            compute_normals_Surface(_handle);
        }

        /// <summary>
        /// Flip the side of all the triangles
        /// </summary>
        public void flip_triangles()
        {
            flip_Surface(_handle);
        }

        /// <summary>
        /// Split the surface in n sub surfaces (the split is based on the triangles, not the vertices, be careful with the 65K vertices limit for GO)
        /// </summary>
        /// <param name="nbSubSurfaces"></param>
        /// <returns></returns>
        public Surface[] split_to_surfaces(int nbSubSurfaces)
        {            
            HandleRef pSubSurfaces = new HandleRef(this, split_to_surfaces_Surface(_handle, nbSubSurfaces));

            int nbMultiSurface = nb_MultiSurface(pSubSurfaces);
            Surface[] splits = new Surface[nbMultiSurface];
            for (int ii = 0; ii < nbMultiSurface; ++ii)               
                splits[ii] = new Surface(move_MultiSurface(pSubSurfaces, ii));

            delete_MultiSurface(pSubSurfaces);
            return splits;
        }

        public int vertices_nb()
        {
            return vertices_nb_Surface(_handle);
        }

        public BBox bounding_box()
        {           
            return new BBox(bounding_box_Surface(_handle));
        }

        /// <summary>
        /// Update the visibility triangle mask of the mesh with the input array and return a new mesh made with invisible triangles
        /// </summary>
        /// <param name="visibilityMask"></param>
        /// <returns></returns>
        public Surface update_visibility_mask(int[] visibilityMask)
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
        public Surface update_visibility_mask(Vector3 rayDirection, Vector3 hitPoint, TriEraser.Mode mode, float degrees)
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

        public int[] visibility_mask()
        {
            int[] visibilityMask = new int[triangles_nb()];
            retrieve_visibility_mask(_handle, visibilityMask);
            return visibilityMask;
        }

        /// <summary>
        /// Cut the mesh with the input cut planes and return it:  Surface[0]: cut mesh result,  Surface[1]: plane cut mesh for the first plane, Surface[2]: plane cut mesh for the second plane...
        /// </summary>
        /// <param name="cutPlanes"></param>
        /// <param name="removeFrontPlane"> NOT USED </param>
        /// <param name="noHoles"></param>
        /// <returns></returns>
        public Surface[] cut(Plane[] cutPlanes, int[] removeFrontPlane, bool noHoles = false)
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
            HandleRef pCutMultiSurface = new HandleRef(this, cut_Surface(_handle, removeFrontPlane, planes, cutPlanes.Length, noHoles?1:0));

            // move data            
            int nbMultiSurface =nb_MultiSurface(pCutMultiSurface);
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
        public void add(Surface surfaceToAdd)
        {
            merge_Surface(_handle, surfaceToAdd.getHandle());
        }

        /// <summary>
        /// Return the computer offset for the input cut plane and the wanted number of cuts
        /// </summary>
        /// <param name="cutPlane"></param>
        /// <param name="nbCuts"></param>
        /// <returns></returns>
        public float size_offset_cut_plane(Plane cutPlane, int nbCuts)
        {            
            return size_offset_cut_plane_Surface(_handle, cutPlane.convertToArray(), nbCuts);
        }

        public void update_mesh_from_dll(Mesh mesh, bool all = true, bool vertices = true, bool normals = true, bool uv = true, bool triangles = true)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateMeshMashall 1");

            m_sizes = new int[6];
            sizes_Surface(_handle, m_sizes);

            if (m_vertices.Length != m_sizes[0] || m_vertices.Length == 0)
            {
                m_verticesHandle.Free();
                m_vertices = new Vector3[m_sizes[0]];                
                m_verticesHandle = GCHandle.Alloc(m_vertices, GCHandleType.Pinned);
            }

            if(m_normals.Length != m_sizes[2] || m_normals.Length == 0)
            {
                m_normalsHandle.Free();
                m_normals = new Vector3[m_sizes[2]];
                m_normalsHandle = GCHandle.Alloc(m_normals, GCHandleType.Pinned);                
            }

            if (m_uv.Length != m_sizes[3] || m_uv.Length == 0)
            {
                m_uvHandle.Free();
                m_uv = new Vector2[m_sizes[3]];
                m_uvHandle = GCHandle.Alloc(m_uv, GCHandleType.Pinned);
            }

            int nbTri = (m_sizes[1] * 3);
            if (m_triID.Length != nbTri || m_triID.Length == 0)
            {
                m_triIdHandle.Free();
                m_triID = new int[nbTri];
                m_triIdHandle = GCHandle.Alloc(m_triID, GCHandleType.Pinned);
            }

            if (m_colors.Length != m_sizes[4] || m_colors.Length == 0)
            {
                m_colorHandle.Free();
                m_colors = new Color[m_sizes[4]];
                m_colorHandle = GCHandle.Alloc(m_colors, GCHandleType.Pinned);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateMeshMashall 3");

            update_mesh_Surface(_handle, m_verticesHandle.AddrOfPinnedObject(), m_normalsHandle.AddrOfPinnedObject(), m_uvHandle.AddrOfPinnedObject(), m_triIdHandle.AddrOfPinnedObject(), m_colorHandle.AddrOfPinnedObject());

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateMeshMashall 4"); // heavy

            if(all)
                mesh.Clear();

            if (all || vertices)
            {
                mesh.vertices = m_vertices;
            }

            if(all || normals)
                mesh.normals = m_normals;

            if (all || vertices)
            {
                if (m_colors.Length > 0)
                {
                    mesh.colors = m_colors;
                }
            }

            if (all || uv)
                mesh.uv = m_uv;

            if (all || triangles)
            {
                mesh.triangles = m_triID;

            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
       

        public int triangles_nb()
        {
            m_sizes = new int[6];
            sizes_Surface(_handle, m_sizes);
            return m_sizes[5];
        }

        public int visible_triangles_nb()
        {
            m_sizes = new int[6];
            sizes_Surface(_handle, m_sizes);
            return m_sizes[1];
        }

        public void display_sizes()
        {
            m_sizes = new int[6];
            sizes_Surface(_handle, m_sizes);

            int nbFloatVertices = m_sizes[0] * 3;
            int nbFloatNormals = m_sizes[2] * 3;
            int nbVisibleIntTriIndices = m_sizes[1] * 3;
            int nbFloatUV = m_sizes[3] * 2;
            int nbFLoatColors = m_sizes[4] * 3;
            int nbAllIntTriIndices = m_sizes[5] * 3;
            Debug.Log("debug surface : " + nbFloatVertices + " " + nbFloatNormals + " " + nbVisibleIntTriIndices + " " + nbFloatUV + " " + nbFLoatColors + " " + nbAllIntTriIndices);            
        }

        public void swap_DLL_handle(Surface surface)
        {
            HandleRef buffer = surface.getHandle();
            surface._handle = _handle;
            _handle = buffer;
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
            m_vertices = new Vector3[other.m_vertices.Length];
            other.m_vertices.CopyTo(m_vertices, 0);

            m_normals = new Vector3[other.m_normals.Length];
            other.m_normals.CopyTo(m_normals, 0);

            m_triID = new int[other.m_triID.Length];
            other.m_triID.CopyTo(m_triID, 0);

            m_uv = new Vector2[other.m_uv.Length];
            other.m_uv.CopyTo(m_uv, 0);

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
            return new Surface(this);
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
        static private extern IntPtr cut_Surface(HandleRef handleSurface, int[] removeFrontPlane, float[] planes, int nbPlanes, int noHoles);
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
        [DllImport("hbp_export", EntryPoint = "size_offset_cut_plane_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern float size_offset_cut_plane_Surface(HandleRef handleSurface, float[] planeCut, int nbCuts);


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