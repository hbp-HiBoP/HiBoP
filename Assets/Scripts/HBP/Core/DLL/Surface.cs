using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;
using HBP.Core.Enums;
using System.Linq;

namespace HBP.Core.DLL
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SurfaceSizes
    {
        public int vertexCount;
        public int normalCount;
        public int uvCount;
        public int triangleIndexCount;
        public int colorCount;
    }

    /// <summary>
    /// Class used to handle the different meshes of the scene within the DLL
    /// </summary>
    /// <remarks>
    /// This class allows the loading of mesh files and some heavy computation on the meshes
    /// It is used to get and process information on the vertex / triangle level of the mesh
    /// </remarks>
    public class Surface : CppDLLImportBase, ICloneable
    {
        private NativeBackend m_Backend = NativeBackendOptions.ExperimentalBackend;

        #region Properties
        /// <summary>
        /// Raw array of triangles indices
        /// </summary>
        private int[] m_TriangleIndices = new int[0];
        /// <summary>
        /// Raw array of the vertices of the surface
        /// </summary>
        private Vector3[] m_Vertices = new Vector3[0];
        /// <summary>
        /// Raw array of the normals of the surface (one normal by vertex)
        /// </summary>
        private Vector3[] m_Normals = new Vector3[0];
        /// <summary>
        /// Raw array of the UVs of the surface (one UV value by vertex)
        /// </summary>
        private Vector2[] m_UV = new Vector2[0];
        /// <summary>
        /// Raw array of the colors of the surface (one color by vertex, this is used for Mars and JuBrain atlases)
        /// </summary>
        private Color[] m_Colors = new Color[0];
        /// <summary>
        /// Array of size 6 containing the dimensions of the surface (vertices, triangles, normals, UVs, colors, triangle indices)
        /// </summary>
        private int[] m_Sizes = new int[6];
        /// <summary>
        /// Is this surface completely loaded ?
        /// </summary>
        public bool IsLoaded { get; private set; }
        /// <summary>
        /// Is mars atlas loaded for this surface ?
        /// </summary>
        public bool IsMarsAtlasLoaded { get; private set; }
        internal NativeBackend Backend => m_Backend;

        /// <summary>
        /// Center of this surface
        /// </summary>
        public Vector3 Center
        {
            get
            {
                BBox bbox = BoundingBox;
                Vector3 center = bbox.Center;
                bbox.Dispose();
                return center;
            }
        }
        /// <summary>
        /// Bounding Box of this surface
        /// </summary>
        public BBox BoundingBox
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_surface_get_bounding_box(_handle.Handle, out IntPtr bbox));
                    return new BBox(bbox, NativeBackend.HbpCore);
                }

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
                if (m_Backend == NativeBackend.HbpCore)
                {
                    return GetHbpCoreSizes().vertexCount;
                }

                return vertices_nb_Surface(_handle);
            }
        }
        /// <summary>
        /// Visibility mask of this surface (true if the vertex is visible)
        /// </summary>
        public int[] VisibilityMask
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    int triangleCount = NumberOfTriangles;
                    int[] visibilityMask = new int[triangleCount];
                    ThrowIfFailed(hbp_surface_copy_visibility_mask(_handle.Handle, visibilityMask, visibilityMask.Length));
                    return visibilityMask;
                }
                EnsureHbpExport(nameof(VisibilityMask));
                int[] exportVisibilityMask = new int[NumberOfTriangles];
                retrieve_visibility_mask(_handle, exportVisibilityMask);
                return exportVisibilityMask;
            }
        }
        /// <summary>
        /// Number of triangles of this surface
        /// </summary>
        public int NumberOfTriangles
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_surface_get_triangle_count(_handle.Handle, out int count));
                    return count;
                }

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
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_surface_get_visible_triangle_count(_handle.Handle, out int count));
                    return count;
                }

                m_Sizes = new int[6];
                sizes_Surface(_handle, m_Sizes);
                return m_Sizes[1];
            }
        }

        /// <summary>
        /// Pointer to an array of vectors 3D containing the vertices of the surface
        /// </summary>
        GCHandle m_verticesHandle;
        /// <summary>
        /// Pointer to an array of vectors 3D containing the vertices of the surface
        /// </summary>
        GCHandle m_normalsHandle;
        /// <summary>
        /// Pointer to an array of vectors 2D containing the vertices of the surface
        /// </summary>
        GCHandle m_uvHandle;
        /// <summary>
        /// Pointer to an array of integers containing the vertices of the surface
        /// </summary>
        GCHandle m_triIdHandle;
        /// <summary>
        /// Pointer to an array of colors containing the vertices of the surface
        /// </summary>
        GCHandle m_colorHandle;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the surface by loading an obj mesh file
        /// </summary>
        /// <param name="obj">Path to the obj file</param>
        /// <returns>True if loading is complete</returns>
        public bool LoadOBJFile(string obj)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                IsLoaded = hbp_surface_load_obj(_handle.Handle, obj) == HbpCoreStatus.Ok;
                if (!IsLoaded)
                {
                    Debug.LogError("-ERROR : Surface::loadObjFile -> can't load obj file to surface : " + obj);
                }
                return IsLoaded;
            }

            IsLoaded = load_OBJ_file_Surface(_handle, obj)==1;
            if (!IsLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadObjFile -> can't load obj file to surface : " + obj);
            }
            return IsLoaded;
        }
        /// <summary>
        /// Initialize the surface by loading a GIFTI mesh file and applying the optional transformation (to put the surface and the volume in the same reference)
        /// </summary>
        /// <param name="gii">Path of the GIFTI file</param>
        /// <param name="transformation">Transformation file associated to the GIFTI file</param>
        /// <returns>True if loading is complete</returns>
        public bool LoadGIIFile(string gii, string transformation = "")
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                IsLoaded = hbp_surface_load_gifti(_handle.Handle, gii) == HbpCoreStatus.Ok;
                if (IsLoaded && !string.IsNullOrEmpty(transformation))
                {
                    using Transformation3 hbpCoreTransformation = Transformation3.FromFile(transformation);
                    ThrowIfFailed(hbp_surface_transform(_handle.Handle, hbpCoreTransformation.getHandle().Handle));
                }

                if (!IsLoaded)
                {
                    Debug.LogError("-ERROR : Surface::loadGIIFile -> can't load GII file to surface : " + gii + " " + transformation);
                }
                return IsLoaded;
            }

            IsLoaded = load_GII_file_Surface(_handle, gii, !string.IsNullOrEmpty(transformation) ? 1 : 0, transformation) == 1;
            if (!IsLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadGIIFile -> can't load GII file to surface : " + gii + " " + transformation);
            }
            return IsLoaded;
        }
        /// <summary>
        /// Initialize the surface by loading a TRI mesh file and applying the optional transformation (to put the surface and the volume in the same reference)
        /// </summary>
        /// <param name="tri">Path of the TRI file</param>
        /// <param name="transformation">Transformation file associated to the TRI file</param>
        /// <returns>True if loading is complete</returns>
        public bool LoadTRIFile(string tri, string transformation = "")
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (!string.IsNullOrEmpty(transformation))
                {
                    throw new NotSupportedException("hbp_core does not expose TRI transform loading in step 7.");
                }

                IsLoaded = hbp_surface_load_tri(_handle.Handle, tri) == HbpCoreStatus.Ok;
                if (!IsLoaded)
                {
                    Debug.LogError("-ERROR : Surface::loadTriFile -> can't load tri file to surface : " + tri);
                }
                return IsLoaded;
            }

            IsLoaded = load_TRI_file_Surface(_handle, tri, !string.IsNullOrEmpty(transformation) ? 1 : 0, transformation) == 1;
            if (!IsLoaded)
            {
                Debug.LogError("-ERROR : Surface::loadTriFile -> can't load tri file to surface : " + tri);
            }
            return IsLoaded;
        }
        /// <summary>
        /// Define the mars atlas parcels gii file to be used to color the vertices
        /// </summary>
        /// <param name="index">Reference to the mars atlas index class</param>
        /// <param name="pathMarsParcel">Path of the mars atlas file</param>
        /// <returns>True if loading is complete</returns>
        public bool SearchMarsParcelFileAndUpdateColors(MarsAtlas index, string pathMarsParcel)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (index.Backend != NativeBackend.HbpCore)
                {
                    throw new InvalidOperationException("Surface.SearchMarsParcelFileAndUpdateColors cannot mix hbp_core Surface with hbp_export MarsAtlas.");
                }

                IsMarsAtlasLoaded = hbp_surface_apply_mars_atlas_parcels(_handle.Handle, index.getHandle().Handle, pathMarsParcel) == HbpCoreStatus.Ok;
                return IsMarsAtlasLoaded;
            }

            IsMarsAtlasLoaded = seach_mars_parcel_file_and_update_colors_Surface(_handle, index.getHandle(), pathMarsParcel) == 1;
            return IsMarsAtlasLoaded;
        }
        /// <summary>
        /// Save surface to an obj file
        /// </summary>
        /// <param name="pathOBJFile">Path to the obj file</param>
        /// <param name="textureName">If not empty, also save the texture to another file</param>
        /// <returns></returns>
        public bool SaveToOBJ(string pathOBJFile, string textureName = "")
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                bool coreFileSaved = hbp_surface_save_obj(_handle.Handle, pathOBJFile, textureName) == HbpCoreStatus.Ok;
                if (!coreFileSaved)
                {
                    Debug.LogError("-ERROR : Surface::saveToObj -> can't save surface to obj file. ");
                }
                return coreFileSaved;
            }

            bool fileSaved = save_to_OBJ_Surface(_handle, pathOBJFile, textureName) == 1;
            if (!fileSaved)
            {
                Debug.LogError("-ERROR : Surface::saveToObj -> can't save surface to obj file. ");
            }
            return fileSaved;
        }
        /// <summary>
        /// Compute all normals for the surface
        /// </summary>
        public void ComputeNormals()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_compute_normals(_handle.Handle));
                return;
            }

            compute_normals_Surface(_handle);
        }
        /// <summary>
        /// Flip the side of every triangles
        /// </summary>
        public void FlipTriangles()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_flip(_handle.Handle));
                return;
            }

            flip_Surface(_handle);
        }
        /// <summary>
        /// Update the visibility triangle mask of the surface with the input array and return a new mesh made with invisible triangles
        /// </summary>
        /// <param name="visibilityMask">Array of 0 (triangle is invisible) and 1 (triangle is visible)</param>
        /// <returns>New surface made with invisible triangles</returns>
        public Surface UpdateVisibilityMask(int[] visibilityMask)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                int triangleCount = NumberOfTriangles;
                if (visibilityMask == null || visibilityMask.Length != triangleCount)
                {
                    throw new ArgumentException($"Visibility mask length must match the surface triangle count ({triangleCount}).", nameof(visibilityMask));
                }
                ThrowIfFailed(hbp_surface_update_visibility_mask(_handle.Handle, visibilityMask, visibilityMask.Length, out IntPtr invisibleSurface));
                return new Surface(invisibleSurface, NativeBackend.HbpCore);
            }
            EnsureHbpExport(nameof(UpdateVisibilityMask));
            Surface invisiblePartMesh = new();
            update_visiblity_mask_Surface(_handle, invisiblePartMesh.getHandle(), visibilityMask);
            return invisiblePartMesh;
        }
        /// <summary>
        /// Update the visibility triangle mask of the surface depending the input triangle erasing action
        /// </summary>
        /// <param name="rayDirection">Direction of the triangle erasing action</param>
        /// <param name="hitPoint">Position of the hit of the raycast</param>
        /// <param name="mode">Currently selected triangle erasing mode</param>
        /// <param name="degrees">Maximum angle in degrees for the area mode</param>
        /// <returns>New surface made with invisible triangles</returns>
        public Surface UpdateVisibilityMask(Vector3 rayDirection, Vector3 hitPoint, TriEraserMode mode, float degrees)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                Vec3 nativeRayDirection = Vec3.FromVector3(rayDirection);
                Vec3 nativeHitPoint = Vec3.FromVector3(hitPoint);
                ThrowIfFailed(hbp_surface_update_visibility_mask_with_ray(_handle.Handle, ref nativeRayDirection, ref nativeHitPoint, (int)mode, degrees, out IntPtr invisibleSurface));
                return new Surface(invisibleSurface, NativeBackend.HbpCore);
            }

            EnsureHbpExport(nameof(UpdateVisibilityMask));
            float[] hitPointArray = new float[3], rayDirectionArray = new float[3];
            hitPointArray[0] = hitPoint.x;
            hitPointArray[1] = hitPoint.y;
            hitPointArray[2] = hitPoint.z;
            rayDirectionArray[0] = rayDirection.x;
            rayDirectionArray[1] = rayDirection.y;
            rayDirectionArray[2] = rayDirection.z;

            Surface invisiblePartMesh = new();
            update_visiblity_mask_with_ray(_handle, invisiblePartMesh.getHandle(), rayDirectionArray, hitPointArray, (int) mode, degrees);
            return invisiblePartMesh;
        }
        /// <summary>
        /// Cut the surface with the input cut planes and return the resulting surfaces
        /// </summary>
        /// <param name="cutPlanes">Cut planes used to cut the surface</param>
        /// <param name="noHoles">If true, the cut meshes will have no holes</param>
        /// <param name="strongCuts">If true, everything in front of any cut will be cut; if false, everything in front of every cuts will be cut</param>
        /// <returns>Resulting surfaces array (the first element is the cut mesh, every other elements are the different cut meshes</returns>
        public Surface[] Cut(Object3D.Cut[] cutPlanes, bool noHoles = false, bool strongCuts = true)
        {
            // check planes
            if (cutPlanes.Length <= 0)
            {
                Debug.LogError("-ERROR : Surface::cutSurface -> nb of planes <= 0. ");
                Surface[] returnError = new Surface[1];
                return returnError;
            }

            if (m_Backend == NativeBackend.HbpCore)
            {
                IntPtr[] planesPtr = cutPlanes.Where(cutPlane => cutPlane != null).Select(cutPlane => cutPlane.getHandle().Handle).Where(handle => handle != IntPtr.Zero).ToArray();
                ThrowIfFailed(hbp_surface_cut(_handle.Handle, planesPtr, planesPtr.Length, noHoles ? 1 : 0, strongCuts ? 1 : 0, out IntPtr cutSurfaces));
                return new SurfaceList(cutSurfaces).TakeAllSurfaces().ToArray();
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
            HandleRef pCutMultiSurface = new(this, cut_Surface(_handle, planes, cutPlanes.Length, noHoles?1:0, strongCuts?1:0));

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
        /// Generate the cut surfaces without cutting the mesh
        /// </summary>
        /// <param name="cutPlanes">Cut planes used to cut the surface</param>
        /// <param name="noHoles">If true, the cut meshes will have no holes</param>
        /// <param name="strongCuts">If true, everything in front of any cut will be cut; if false, everything in front of every cuts will be cut</param>
        /// <returns>Array of the cut meshes</returns>
        public List<Surface> GenerateCutSurfaces(List<Object3D.Cut> cutPlanes, bool noHoles = false, bool strongCuts = true)
        {
            // check planes
            if (cutPlanes.Count <= 0)
            {
                Debug.LogError("-ERROR : Surface::cutSurface -> nb of planes <= 0. ");
                List<Surface> returnError = new();
                return returnError;
            }

            if (m_Backend == NativeBackend.HbpCore)
            {
                IntPtr[] planesPtr = cutPlanes.Where(cutPlane => cutPlane != null).Select(cutPlane => cutPlane.getHandle().Handle).Where(handle => handle != IntPtr.Zero).ToArray();
                ThrowIfFailed(hbp_surface_generate_cuts(_handle.Handle, planesPtr, planesPtr.Length, noHoles ? 1 : 0, strongCuts ? 1 : 0, out IntPtr cutSurfaces));
                return new SurfaceList(cutSurfaces).TakeAllSurfaces();
            }

            // init plane
            float[] planes = new float[cutPlanes.Count * 6];
            for (int ii = 0; ii < cutPlanes.Count; ++ii)
            {
                for (int jj = 0; jj < 3; ++jj)
                {
                    planes[ii * 6 + jj] = cutPlanes[ii].Point[jj];
                    planes[ii * 6 + jj + 3] = cutPlanes[ii].Normal[jj];
                }
            }

            // do the cut            
            HandleRef pCutMultiSurface = new(this, generate_cuts_Surface(_handle, planes, cutPlanes.Count, noHoles ? 1 : 0, strongCuts ? 1 : 0));

            // move data            
            int nbMultiSurface = nb_MultiSurface(pCutMultiSurface);
            List<Surface> cuts = new();
            for (int ii = 0; ii < nbMultiSurface; ++ii)
                cuts.Add(new Surface(move_MultiSurface(pCutMultiSurface, ii)));

            // clean the multi surface
            delete_MultiSurface(pCutMultiSurface);
            return cuts;
        }
        /// <summary>
        /// Generate the raw cut surfaces without cutting the mesh
        /// </summary>
        /// <param name="cutPlanes">Cut planes used to cut the surface</param>
        /// <param name="noHoles">If true, the cut meshes will have no holes</param>
        /// <param name="strongCuts">If true, everything in front of any cut will be cut; if false, everything in front of every cuts will be cut</param>
        /// <returns>Array of the cut meshes</returns>
        public List<Surface> GenerateRawCutSurfaces(List<Object3D.Cut> cutPlanes, bool noHoles = false, bool strongCuts = true)
        {
            // check planes
            if (cutPlanes.Count <= 0)
            {
                Debug.LogError("-ERROR : Surface::cutSurface -> nb of planes <= 0. ");
                List<Surface> returnError = new();
                return returnError;
            }

            if (m_Backend == NativeBackend.HbpCore)
            {
                IntPtr[] planesPtr = cutPlanes.Where(cutPlane => cutPlane != null).Select(cutPlane => cutPlane.getHandle().Handle).Where(handle => handle != IntPtr.Zero).ToArray();
                ThrowIfFailed(hbp_surface_generate_raw_cuts(_handle.Handle, planesPtr, planesPtr.Length, out IntPtr cutSurfaces));
                return new SurfaceList(cutSurfaces).TakeAllSurfaces();
            }

            // init plane
            float[] planes = new float[cutPlanes.Count * 6];
            for (int ii = 0; ii < cutPlanes.Count; ++ii)
            {
                for (int jj = 0; jj < 3; ++jj)
                {
                    planes[ii * 6 + jj] = cutPlanes[ii].Point[jj];
                    planes[ii * 6 + jj + 3] = cutPlanes[ii].Normal[jj];
                }
            }

            // do the cut            
            HandleRef pCutMultiSurface = new(this, generate_raw_cuts_Surface(_handle, planes, cutPlanes.Count, noHoles ? 1 : 0, strongCuts ? 1 : 0));

            // move data            
            int nbMultiSurface = nb_MultiSurface(pCutMultiSurface);
            List<Surface> cuts = new();
            for (int ii = 0; ii < nbMultiSurface; ++ii)
                cuts.Add(new Surface(move_MultiSurface(pCutMultiSurface, ii)));

            // clean the multi surface
            delete_MultiSurface(pCutMultiSurface);
            return cuts;
        }
        /// <summary>
        /// Merge the surface with the input one
        /// </summary>
        /// <param name="surfaceToAdd">Surface to be merged with this surface</param>
        public void Append(Surface surfaceToAdd)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (surfaceToAdd.m_Backend != NativeBackend.HbpCore)
                {
                    throw new InvalidOperationException("Cannot merge a hbp_export Surface into a hbp_core Surface.");
                }

                ThrowIfFailed(hbp_surface_merge(_handle.Handle, surfaceToAdd.getHandle().Handle));
                IsLoaded &= surfaceToAdd.IsLoaded;
                IsMarsAtlasLoaded &= surfaceToAdd.IsMarsAtlasLoaded;
                return;
            }

            if (surfaceToAdd.m_Backend != NativeBackend.HbpExport)
            {
                throw new InvalidOperationException("Cannot merge a hbp_core Surface into a hbp_export Surface.");
            }

            merge_Surface(_handle, surfaceToAdd.getHandle());
            IsLoaded &= surfaceToAdd.IsLoaded;
            IsMarsAtlasLoaded &= surfaceToAdd.IsMarsAtlasLoaded;
        }
        /// <summary>
        /// Update the corresponding Unity mesh using the surface
        /// </summary>
        /// <param name="mesh">Mesh to be updated</param>
        /// <param name="all">Do we update evertyhing ?</param>
        /// <param name="vertices">Do we update the vertices ?</param>
        /// <param name="normals">Do we update the normals ?</param>
        /// <param name="uv">Do we update the UVs ?</param>
        /// <param name="triangles">Do we update the triangles ?</param>
        /// <param name="colors">Do we update the colors ?</param>
        public void UpdateMeshFromDLL(Mesh mesh, bool all = true, bool vertices = true, bool normals = true, bool uv = true, bool triangles = true, bool colors = true)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                UpdateMeshFromHbpCore(mesh, all, vertices, normals, uv, triangles, colors);
                return;
            }

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
                if (m_TriangleIndices.Length != nbTri || m_TriangleIndices.Length == 0)
                {
                    if (m_triIdHandle.IsAllocated) m_triIdHandle.Free();
                    m_TriangleIndices = new int[nbTri];
                    m_triIdHandle = GCHandle.Alloc(m_TriangleIndices, GCHandleType.Pinned);
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
                    mesh.triangles = m_TriangleIndices;

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
        /// Debug method used to log the sizes of the surface
        /// </summary>
        public void DisplaySizes()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                SurfaceSizes sizes = GetHbpCoreSizes();
                Debug.Log("debug surface : " + (sizes.vertexCount * 3) + " " + (sizes.normalCount * 3) + " " + sizes.triangleIndexCount + " " + (sizes.uvCount * 2) + " " + (sizes.colorCount * 4) + " " + sizes.triangleIndexCount);
                return;
            }

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
        /// Swap the pointer of this surface with another surface
        /// </summary>
        /// <param name="surface">Surface to swap the pointer with</param>
        public void SwapDLLHandle(Surface surface)
        {
            if (m_Backend != surface.m_Backend)
            {
                throw new InvalidOperationException("Cannot swap Surface handles from different native backends.");
            }

            HandleRef buffer = surface.getHandle();
            surface._handle = _handle;
            _handle = buffer;
        }
        /// <summary>
        /// Simplify this surface
        /// </summary>
        /// <param name="numberOfTriangles">Target number of triangles for the new surface</param>
        /// <param name="agressiveness">Agressiveness of the simplification </param>
        /// <returns></returns>
        public Surface Simplify(int numberOfTriangles = 10000, int agressiveness = 7)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_simplify(_handle.Handle, numberOfTriangles, agressiveness, out IntPtr simplifiedSurface));
                return new Surface(simplifiedSurface, NativeBackend.HbpCore)
                {
                    IsLoaded = IsLoaded,
                    IsMarsAtlasLoaded = false
                };
            }

            EnsureHbpExport(nameof(Simplify));
            Surface surface = new(this);
            simplify_mesh_Surface(surface.getHandle(), numberOfTriangles, agressiveness);
            return surface;
        }
        /// <summary>
        /// Is the site inside of the surface ?
        /// </summary>
        /// <param name="rawSiteList">List of the sites of the scene</param>
        /// <param name="index">Index of the considered site</param>
        /// <returns>True if the site is inside the surface</returns>
        public bool IsPointInside(Vector3 point)
        {
            EnsureHbpExport(nameof(IsPointInside));
            return is_point_inside_Surface(_handle, -point.x, point.y, point.z);
        }
        /// <summary>
        /// Returns a cube bbox around the mesh depending on the cuts used
        /// </summary>
        /// <param name="cuts">Cuts to consider when computing the bounding box</param>
        /// <returns>The cube bounding box created</returns>
        public BBox GetCubeBoundingBox(Object3D.Cut[] cuts)
        {
            EnsureHbpExport(nameof(GetCubeBoundingBox));
            float[] planes = new float[cuts.Length * 6];
            int planesCount = 0;
            for (int ii = 0; ii < cuts.Length; ++ii)
            {
                if (cuts[ii].Orientation != CutOrientation.Custom)
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

        public void SetBuffers(Vector3[] vertices, int[] triangles, Vector3[] normals = null, Vector2[] uv = null, Color[] colors = null)
        {
            if (m_Backend != NativeBackend.HbpCore)
            {
                throw new NotSupportedException("Surface.SetBuffers is exposed for hbp_core surfaces in step 7.");
            }

            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));

            Vec3[] nativeVertices = new Vec3[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                nativeVertices[i] = Vec3.FromVector3(vertices[i]);
            }
            ThrowIfFailed(hbp_surface_set_vertices(_handle.Handle, nativeVertices, nativeVertices.Length));
            ThrowIfFailed(hbp_surface_set_triangles(_handle.Handle, triangles, triangles.Length));

            if (normals != null)
            {
                Vec3[] nativeNormals = new Vec3[normals.Length];
                for (int i = 0; i < normals.Length; ++i)
                {
                    nativeNormals[i] = Vec3.FromVector3(normals[i]);
                }
                ThrowIfFailed(hbp_surface_set_normals(_handle.Handle, nativeNormals, nativeNormals.Length));
            }

            if (uv != null)
            {
                Vec2[] nativeUv = new Vec2[uv.Length];
                for (int i = 0; i < uv.Length; ++i)
                {
                    nativeUv[i] = Vec2.FromVector2(uv[i]);
                }
                ThrowIfFailed(hbp_surface_set_uvs(_handle.Handle, nativeUv, nativeUv.Length));
            }

            if (colors != null)
            {
                Color4[] nativeColors = new Color4[colors.Length];
                for (int i = 0; i < colors.Length; ++i)
                {
                    nativeColors[i] = Color4.FromColor(colors[i]);
                }
                ThrowIfFailed(hbp_surface_set_colors(_handle.Handle, nativeColors, nativeColors.Length));
            }
        }
        #endregion

        #region Memory Management
        public Surface() : base() { }
        public Surface(IntPtr surfaceHandle) : base(surfaceHandle)
        {
            m_Backend = NativeBackend.HbpExport;
        }
        internal Surface(IntPtr surfaceHandle, NativeBackend backend) : base(surfaceHandle)
        {
            m_Backend = backend;
        }
        public Surface(Surface other) : base(CloneNativeSurface(other))
        {
            m_Backend = other.m_Backend;

            m_Vertices = new Vector3[other.m_Vertices.Length];
            other.m_Vertices.CopyTo(m_Vertices, 0);

            m_Normals = new Vector3[other.m_Normals.Length];
            other.m_Normals.CopyTo(m_Normals, 0);

            m_TriangleIndices = new int[other.m_TriangleIndices.Length];
            other.m_TriangleIndices.CopyTo(m_TriangleIndices, 0);

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
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_create(out IntPtr surface));
                _handle = new HandleRef(this, surface);
                return;
            }

            _handle = new HandleRef(this,create_Surface());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_destroy(_handle.Handle));
                return;
            }

            delete_Surface(_handle);
        }
        public object Clone()
        {
            Surface clonedSurface = new(this);
            clonedSurface.IsLoaded = IsLoaded;
            clonedSurface.IsMarsAtlasLoaded = IsMarsAtlasLoaded;
            return clonedSurface;
        }
        #endregion

        private SurfaceSizes GetHbpCoreSizes()
        {
            ThrowIfFailed(hbp_surface_get_sizes(_handle.Handle, out SurfaceSizes sizes));
            return sizes;
        }

        private void UpdateMeshFromHbpCore(Mesh mesh, bool all, bool vertices, bool normals, bool uv, bool triangles, bool colors)
        {
            SurfaceSizes sizes = GetHbpCoreSizes();

            m_Vertices = new Vector3[sizes.vertexCount];
            if (sizes.vertexCount > 0)
            {
                Vec3[] nativeVertices = new Vec3[sizes.vertexCount];
                ThrowIfFailed(hbp_surface_copy_vertices(_handle.Handle, nativeVertices, nativeVertices.Length));
                for (int i = 0; i < nativeVertices.Length; ++i)
                {
                    m_Vertices[i] = nativeVertices[i].ToVector3();
                }
            }

            m_Normals = new Vector3[sizes.normalCount];
            if (sizes.normalCount > 0)
            {
                Vec3[] nativeNormals = new Vec3[sizes.normalCount];
                ThrowIfFailed(hbp_surface_copy_normals(_handle.Handle, nativeNormals, nativeNormals.Length));
                for (int i = 0; i < nativeNormals.Length; ++i)
                {
                    m_Normals[i] = nativeNormals[i].ToVector3();
                }
            }

            m_UV = new Vector2[sizes.uvCount];
            if (sizes.uvCount > 0)
            {
                Vec2[] nativeUv = new Vec2[sizes.uvCount];
                ThrowIfFailed(hbp_surface_copy_uvs(_handle.Handle, nativeUv, nativeUv.Length));
                for (int i = 0; i < nativeUv.Length; ++i)
                {
                    m_UV[i] = nativeUv[i].ToVector2();
                }
            }

            m_Colors = new Color[sizes.colorCount];
            if (sizes.colorCount > 0)
            {
                Color4[] nativeColors = new Color4[sizes.colorCount];
                ThrowIfFailed(hbp_surface_copy_colors(_handle.Handle, nativeColors, nativeColors.Length));
                for (int i = 0; i < nativeColors.Length; ++i)
                {
                    m_Colors[i] = nativeColors[i].ToColor();
                }
            }

            int visibleTriangleIndexCount = NumberOfVisibleTriangles * 3;
            m_TriangleIndices = new int[visibleTriangleIndexCount];
            if (visibleTriangleIndexCount > 0)
            {
                ThrowIfFailed(hbp_surface_copy_visible_triangles(_handle.Handle, m_TriangleIndices, m_TriangleIndices.Length));
            }

            mesh.Clear();
            if (all || vertices) mesh.vertices = m_Vertices;
            if (all || normals) mesh.normals = m_Normals;
            if (all || uv) mesh.uv = m_UV;
            if ((all || colors) && m_Colors.Length > 0) mesh.colors = m_Colors;
            if (all || triangles)
            {
                mesh.indexFormat = m_Vertices.Length > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.triangles = m_TriangleIndices;
            }
        }

        private void EnsureHbpExport(string methodName)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                throw new NotSupportedException($"hbp_core does not expose Surface.{methodName} in step 7.");
            }
        }

        private static IntPtr CloneNativeSurface(Surface other)
        {
            if (other.m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_clone(other.getHandle().Handle, out IntPtr clonedSurface));
                return clonedSurface;
            }

            return clone_Surface(other.getHandle());
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Surface call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Surface();
        [DllImport("hbp_export", EntryPoint = "delete_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Surface(HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "clone_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr clone_Surface(HandleRef surfaceToClone);
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
        [DllImport("hbp_export", EntryPoint = "generate_raw_cuts_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_raw_cuts_Surface(HandleRef handleSurface, float[] planes, int nbPlanes, int noHoles, int strongCuts);
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
        static private extern bool is_point_inside_Surface(HandleRef handleSurface, float x, float y, float z);
        [DllImport("hbp_export", EntryPoint = "update_visiblity_mask_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_visiblity_mask_Surface(HandleRef handleSurface, HandleRef handleInvisiblePartSurface, int[] visibilityMask);
        [DllImport("hbp_export", EntryPoint = "update_visiblity_mask_with_ray", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_visiblity_mask_with_ray(HandleRef handleSurface, HandleRef handleInvisiblePartSurface, float[] rayDirection, float[] hitPoint, int mode, float degrees);
        [DllImport("hbp_export", EntryPoint = "retrieve_visibility_mask", CallingConvention = CallingConvention.Cdecl)]
        static private extern void retrieve_visibility_mask(HandleRef handleSurface, int[] visibilityMask);
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
        [DllImport("hbp_export", EntryPoint = "get_mesh_from_bounding_box_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_mesh_from_bounding_box_Surface(HandleRef handleSurface, int precision);
        [DllImport("hbp_export", EntryPoint = "delete_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MultiSurface(HandleRef handleMultiSurface);
        [DllImport("hbp_export", EntryPoint = "move_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr move_MultiSurface(HandleRef handleMultiSurface, int numSurface);
        [DllImport("hbp_export", EntryPoint = "nb_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern int nb_MultiSurface(HandleRef handleMultiSurface);
        [DllImport("hbp_export", EntryPoint = "data_MultiSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void data_MultiSurface(int numSurface, HandleRef handleMultiSurface, IntPtr verticesArray, IntPtr normalsArray, IntPtr triIndicesArray, IntPtr texturesUVArray);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_create(out IntPtr surface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_destroy(IntPtr surface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_clone", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_clone(IntPtr surface, out IntPtr outSurface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_load_obj", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_load_obj(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_load_tri", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_load_tri(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_load_gifti", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_load_gifti(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_save_obj", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_save_obj(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.LPUTF8Str)] string textureName);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_set_vertices", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_set_vertices(IntPtr surface, [In] Vec3[] vertices, int vertexCount);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_set_normals", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_set_normals(IntPtr surface, [In] Vec3[] normals, int normalCount);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_set_uvs", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_set_uvs(IntPtr surface, [In] Vec2[] uv, int uvCount);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_set_colors", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_set_colors(IntPtr surface, [In] Color4[] colors, int colorCount);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_set_triangles", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_set_triangles(IntPtr surface, [In] int[] triangleIndices, int triangleIndexCount);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_merge", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_merge(IntPtr surface, IntPtr surfaceToAdd);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_get_sizes", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_get_sizes(IntPtr surface, out SurfaceSizes sizes);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_get_triangle_count", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_get_triangle_count(IntPtr surface, out int count);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_get_visible_triangle_count", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_get_visible_triangle_count(IntPtr surface, out int count);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_copy_vertices", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_copy_vertices(IntPtr surface, [Out] Vec3[] vertices, int vertexCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_copy_normals", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_copy_normals(IntPtr surface, [Out] Vec3[] normals, int normalCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_copy_uvs", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_copy_uvs(IntPtr surface, [Out] Vec2[] uv, int uvCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_copy_colors", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_copy_colors(IntPtr surface, [Out] Color4[] colors, int colorCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_copy_triangles", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_copy_triangles(IntPtr surface, [Out] int[] triangleIndices, int triangleIndexCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_copy_visible_triangles", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_copy_visible_triangles(IntPtr surface, [Out] int[] triangleIndices, int triangleIndexCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_copy_visibility_mask", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_copy_visibility_mask(IntPtr surface, [Out] int[] visibilityMask, int maskCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_get_bounding_box", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_get_bounding_box(IntPtr surface, out IntPtr bbox);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_compute_normals", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_compute_normals(IntPtr surface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_flip", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_flip(IntPtr surface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_transform", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_transform(IntPtr surface, IntPtr transformation);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_update_visibility_mask", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_update_visibility_mask(IntPtr surface, [In] int[] visibilityMask, int maskCount, out IntPtr invisibleSurface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_update_visibility_mask_with_ray", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_update_visibility_mask_with_ray(IntPtr surface, ref Vec3 rayDirection, ref Vec3 hitPoint, int mode, float degrees, out IntPtr invisibleSurface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_simplify", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_simplify(IntPtr surface, int targetTriangleCount, int aggressiveness, out IntPtr outSurface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_apply_mars_atlas_parcels", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_apply_mars_atlas_parcels(IntPtr surface, IntPtr marsAtlas, [MarshalAs(UnmanagedType.LPUTF8Str)] string parcelsPath);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_cut", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_cut(IntPtr surface, [In] IntPtr[] planes, int planeCount, int noHoles, int strongCuts, out IntPtr surfaces);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_generate_cuts", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generate_cuts(IntPtr surface, [In] IntPtr[] planes, int planeCount, int noHoles, int strongCuts, out IntPtr surfaces);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_surface_generate_raw_cuts", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generate_raw_cuts(IntPtr surface, [In] IntPtr[] planes, int planeCount, out IntPtr surfaces);
        #endregion
    }
}
