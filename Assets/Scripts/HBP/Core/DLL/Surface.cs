using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using UnityEngine;

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

    public class Surface : CppDLLImportBase, ICloneable
    {
        private int[] m_TriangleIndices = Array.Empty<int>();
        private Vector3[] m_Vertices = Array.Empty<Vector3>();
        private Vector3[] m_Normals = Array.Empty<Vector3>();
        private Vector2[] m_UV = Array.Empty<Vector2>();
        private Color[] m_Colors = Array.Empty<Color>();
        private GCHandle m_VerticesHandle;
        private GCHandle m_NormalsHandle;
        private GCHandle m_UvHandle;
        private GCHandle m_TriangleIndicesHandle;
        private GCHandle m_ColorsHandle;

        public bool IsLoaded { get; private set; }
        public bool IsMarsAtlasLoaded { get; private set; }

        public Vector3 Center
        {
            get
            {
                using BBox bbox = BoundingBox;
                return bbox.Center;
            }
        }

        public BBox BoundingBox
        {
            get
            {
                ThrowIfFailed(hbp_surface_get_bounding_box(_handle.Handle, out IntPtr bbox));
                return new BBox(bbox);
            }
        }

        public int NumberOfVertices => GetSizes().vertexCount;

        public int[] VisibilityMask
        {
            get
            {
                int[] mask = new int[NumberOfTriangles];
                ThrowIfFailed(hbp_surface_copy_visibility_mask(_handle.Handle, mask, mask.Length));
                return mask;
            }
        }

        public int NumberOfTriangles
        {
            get
            {
                ThrowIfFailed(hbp_surface_get_triangle_count(_handle.Handle, out int count));
                return count;
            }
        }

        public int NumberOfVisibleTriangles
        {
            get
            {
                ThrowIfFailed(hbp_surface_get_visible_triangle_count(_handle.Handle, out int count));
                return count;
            }
        }

        public Surface()
        {
        }

        public Surface(IntPtr surfaceHandle) : base(surfaceHandle)
        {
        }

        public Surface(Surface other) : base(CloneNativeSurface(other))
        {
            m_Vertices = (Vector3[])other.m_Vertices.Clone();
            m_Normals = (Vector3[])other.m_Normals.Clone();
            m_TriangleIndices = (int[])other.m_TriangleIndices.Clone();
            m_UV = (Vector2[])other.m_UV.Clone();
            m_Colors = (Color[])other.m_Colors.Clone();
            IsLoaded = other.IsLoaded;
            IsMarsAtlasLoaded = other.IsMarsAtlasLoaded;
        }

        public bool LoadOBJFile(string obj)
        {
            IsLoaded = hbp_surface_load_obj(_handle.Handle, obj) == HbpCoreStatus.Ok;
            if (!IsLoaded) Debug.LogError("-ERROR : Surface::loadObjFile -> can't load obj file to surface : " + obj);
            return IsLoaded;
        }

        public bool LoadGIIFile(string gii, string transformation = "")
        {
            IsLoaded = hbp_surface_load_gifti(_handle.Handle, gii) == HbpCoreStatus.Ok;
            if (IsLoaded && !string.IsNullOrEmpty(transformation))
            {
                using Transformation3 transform = Transformation3.FromFile(transformation);
                ThrowIfFailed(hbp_surface_transform(_handle.Handle, transform.getHandle().Handle));
            }

            if (!IsLoaded) Debug.LogError("-ERROR : Surface::loadGIIFile -> can't load GII file to surface : " + gii + " " + transformation);
            return IsLoaded;
        }

        public bool LoadTRIFile(string tri, string transformation = "")
        {
            IsLoaded = hbp_surface_load_tri(_handle.Handle, tri) == HbpCoreStatus.Ok;
            if (IsLoaded && !string.IsNullOrEmpty(transformation))
            {
                using Transformation3 transform = Transformation3.FromFile(transformation);
                ThrowIfFailed(hbp_surface_transform(_handle.Handle, transform.getHandle().Handle));
            }

            if (!IsLoaded) Debug.LogError("-ERROR : Surface::loadTriFile -> can't load tri file to surface : " + tri + " " + transformation);
            return IsLoaded;
        }

        public bool SearchMarsParcelFileAndUpdateColors(MarsAtlas index, string pathMarsParcel)
        {
            if (index == null) throw new ArgumentNullException(nameof(index));
            IsMarsAtlasLoaded = hbp_surface_apply_mars_atlas_parcels(_handle.Handle, index.getHandle().Handle, pathMarsParcel) == HbpCoreStatus.Ok;
            return IsMarsAtlasLoaded;
        }

        public bool SaveToOBJ(string pathOBJFile, string textureName = "")
        {
            bool saved = hbp_surface_save_obj(_handle.Handle, pathOBJFile, textureName) == HbpCoreStatus.Ok;
            if (!saved) Debug.LogError("-ERROR : Surface::saveToObj -> can't save surface to obj file.");
            return saved;
        }

        public void ComputeNormals()
        {
            ThrowIfFailed(hbp_surface_compute_normals(_handle.Handle));
        }

        public void FlipTriangles()
        {
            ThrowIfFailed(hbp_surface_flip(_handle.Handle));
        }

        public Surface UpdateVisibilityMask(int[] visibilityMask)
        {
            int triangleCount = NumberOfTriangles;
            if (visibilityMask == null || visibilityMask.Length != triangleCount)
            {
                throw new ArgumentException($"Visibility mask length must match the surface triangle count ({triangleCount}).", nameof(visibilityMask));
            }

            ThrowIfFailed(hbp_surface_update_visibility_mask(_handle.Handle, visibilityMask, visibilityMask.Length, out IntPtr invisibleSurface));
            return new Surface(invisibleSurface);
        }

        public Surface UpdateVisibilityMask(Vector3 rayDirection, Vector3 hitPoint, TriEraserMode mode, float degrees)
        {
            Vec3 nativeRayDirection = Vec3.FromVector3(rayDirection);
            Vec3 nativeHitPoint = Vec3.FromVector3(hitPoint);
            ThrowIfFailed(hbp_surface_update_visibility_mask_with_ray(_handle.Handle, ref nativeRayDirection, ref nativeHitPoint, (int)mode, degrees, out IntPtr invisibleSurface));
            return new Surface(invisibleSurface);
        }

        public Surface[] Cut(Object3D.Cut[] cutPlanes, bool noHoles = false, bool strongCuts = true)
        {
            if (cutPlanes == null) throw new ArgumentNullException(nameof(cutPlanes));
            if (cutPlanes.Length == 0)
            {
                Debug.LogError("-ERROR : Surface::cutSurface -> nb of planes <= 0.");
                return new Surface[1];
            }

            IntPtr[] planes = ToPlaneHandles(cutPlanes);
            ThrowIfFailed(hbp_surface_cut(_handle.Handle, planes, planes.Length, noHoles ? 1 : 0, strongCuts ? 1 : 0, out IntPtr surfaces));
            using SurfaceList result = new(surfaces);
            return result.TakeAllSurfaces().ToArray();
        }

        public List<Surface> GenerateCutSurfaces(List<Object3D.Cut> cutPlanes, bool noHoles = false, bool strongCuts = true)
        {
            if (cutPlanes == null) throw new ArgumentNullException(nameof(cutPlanes));
            if (cutPlanes.Count == 0)
            {
                Debug.LogError("-ERROR : Surface::cutSurface -> nb of planes <= 0.");
                return new List<Surface>();
            }

            IntPtr[] planes = ToPlaneHandles(cutPlanes);
            ThrowIfFailed(hbp_surface_generate_cuts(_handle.Handle, planes, planes.Length, noHoles ? 1 : 0, strongCuts ? 1 : 0, out IntPtr surfaces));
            using SurfaceList result = new(surfaces);
            return result.TakeAllSurfaces();
        }

        public List<Surface> GenerateRawCutSurfaces(List<Object3D.Cut> cutPlanes, bool noHoles = false, bool strongCuts = true)
        {
            if (cutPlanes == null) throw new ArgumentNullException(nameof(cutPlanes));
            if (cutPlanes.Count == 0)
            {
                Debug.LogError("-ERROR : Surface::cutSurface -> nb of planes <= 0.");
                return new List<Surface>();
            }

            IntPtr[] planes = ToPlaneHandles(cutPlanes);
            ThrowIfFailed(hbp_surface_generate_raw_cuts(_handle.Handle, planes, planes.Length, out IntPtr surfaces));
            using SurfaceList result = new(surfaces);
            return result.TakeAllSurfaces();
        }

        public void Append(Surface surfaceToAdd)
        {
            if (surfaceToAdd == null) throw new ArgumentNullException(nameof(surfaceToAdd));
            ThrowIfFailed(hbp_surface_merge(_handle.Handle, surfaceToAdd.getHandle().Handle));
            IsLoaded &= surfaceToAdd.IsLoaded;
            IsMarsAtlasLoaded &= surfaceToAdd.IsMarsAtlasLoaded;
        }

        public void UpdateMeshFromDLL(Mesh mesh, bool all = true, bool vertices = true, bool normals = true, bool uv = true, bool triangles = true, bool colors = true)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            UpdateMesh(mesh, all, vertices, normals, uv, triangles, colors);
        }

        public void DisplaySizes()
        {
            SurfaceSizes sizes = GetSizes();
            Debug.Log("debug surface : " + (sizes.vertexCount * 3) + " " + (sizes.normalCount * 3) + " " + sizes.triangleIndexCount + " " + (sizes.uvCount * 2) + " " + (sizes.colorCount * 4) + " " + sizes.triangleIndexCount);
        }

        public void SwapDLLHandle(Surface surface)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            HandleRef buffer = surface.getHandle();
            surface._handle = _handle;
            _handle = buffer;
        }

        public Surface Simplify(int numberOfTriangles = 10000, int agressiveness = 7)
        {
            ThrowIfFailed(hbp_surface_simplify(_handle.Handle, numberOfTriangles, agressiveness, out IntPtr simplifiedSurface));
            return new Surface(simplifiedSurface) { IsLoaded = IsLoaded, IsMarsAtlasLoaded = false };
        }

        public bool IsPointInside(Vector3 point)
        {
            Vec3 nativePoint = Vec3.FromVector3(point);
            ThrowIfFailed(hbp_surface_is_point_inside(_handle.Handle, ref nativePoint, out int inside));
            return inside != 0;
        }

        public void SetBuffers(Vector3[] vertices, int[] triangles, Vector3[] normals = null, Vector2[] uv = null, Color[] colors = null)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));
            Vec3[] nativeVertices = vertices.Select(vertex => Vec3.FromVector3(vertex)).ToArray();
            ThrowIfFailed(hbp_surface_set_vertices(_handle.Handle, nativeVertices, nativeVertices.Length));
            int[] nativeTriangles = ReferenceSystemConversion.ConvertTriangleWinding(triangles);
            ThrowIfFailed(hbp_surface_set_triangles(_handle.Handle, nativeTriangles, nativeTriangles.Length));

            if (normals != null)
            {
                Vec3[] nativeNormals = normals.Select(normal => Vec3.FromVector3(normal)).ToArray();
                ThrowIfFailed(hbp_surface_set_normals(_handle.Handle, nativeNormals, nativeNormals.Length));
            }

            if (uv != null)
            {
                Vec2[] nativeUv = uv.Select(Vec2.FromVector2).ToArray();
                ThrowIfFailed(hbp_surface_set_uvs(_handle.Handle, nativeUv, nativeUv.Length));
            }

            if (colors != null)
            {
                Color4[] nativeColors = colors.Select(Color4.FromColor).ToArray();
                ThrowIfFailed(hbp_surface_set_colors(_handle.Handle, nativeColors, nativeColors.Length));
            }
        }

        public object Clone()
        {
            return new Surface(this) { IsLoaded = IsLoaded, IsMarsAtlasLoaded = IsMarsAtlasLoaded };
        }

        public override void Dispose()
        {
            FreePinnedMeshBuffers();
            base.Dispose();
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_surface_create(out IntPtr surface));
            _handle = new HandleRef(this, surface);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_surface_destroy(_handle.Handle));
        }

        private static IntPtr[] ToPlaneHandles(IEnumerable<Object3D.Cut> cutPlanes)
        {
            return cutPlanes.Where(cut => cut != null).Select(cut => cut.getHandle().Handle).Where(handle => handle != IntPtr.Zero).ToArray();
        }

        private static IntPtr CloneNativeSurface(Surface other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            ThrowIfFailed(hbp_surface_clone(other.getHandle().Handle, out IntPtr clonedSurface));
            return clonedSurface;
        }

        private SurfaceSizes GetSizes()
        {
            ThrowIfFailed(hbp_surface_get_sizes(_handle.Handle, out SurfaceSizes sizes));
            return sizes;
        }

        private void UpdateMesh(Mesh mesh, bool all, bool vertices, bool normals, bool uv, bool triangles, bool colors)
        {
            SurfaceSizes sizes = GetSizes();
            EnsurePinnedArray(ref m_Vertices, ref m_VerticesHandle, sizes.vertexCount);
            EnsurePinnedArray(ref m_Normals, ref m_NormalsHandle, sizes.normalCount);
            EnsurePinnedArray(ref m_UV, ref m_UvHandle, sizes.uvCount);
            EnsurePinnedArray(ref m_Colors, ref m_ColorsHandle, sizes.colorCount);
            EnsurePinnedArray(ref m_TriangleIndices, ref m_TriangleIndicesHandle, NumberOfVisibleTriangles * 3);

            ThrowIfFailed(hbp_surface_copy_unity_mesh(_handle.Handle, Pointer(m_VerticesHandle), m_Vertices.Length, Pointer(m_NormalsHandle), m_Normals.Length, Pointer(m_UvHandle), m_UV.Length, Pointer(m_ColorsHandle), m_Colors.Length, Pointer(m_TriangleIndicesHandle), m_TriangleIndices.Length));

            if (mesh.vertexCount != m_Vertices.Length) mesh.Clear();
            if (all || vertices) mesh.vertices = m_Vertices;
            if (all || normals) mesh.normals = m_Normals;
            if (all || uv) mesh.uv = m_UV;
            if ((all || colors) && m_Colors.Length > 0) mesh.colors = m_Colors;
            if (all || triangles)
            {
                if (m_Vertices.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.triangles = m_TriangleIndices;
            }
        }

        private static void EnsurePinnedArray<T>(ref T[] array, ref GCHandle handle, int length)
        {
            if (array.Length == length && (length == 0 || handle.IsAllocated)) return;
            if (handle.IsAllocated) handle.Free();
            array = new T[length];
            if (length > 0) handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        }

        private static IntPtr Pointer(GCHandle handle) => handle.IsAllocated ? handle.AddrOfPinnedObject() : IntPtr.Zero;

        private void FreePinnedMeshBuffers()
        {
            if (m_VerticesHandle.IsAllocated) m_VerticesHandle.Free();
            if (m_NormalsHandle.IsAllocated) m_NormalsHandle.Free();
            if (m_UvHandle.IsAllocated) m_UvHandle.Free();
            if (m_TriangleIndicesHandle.IsAllocated) m_TriangleIndicesHandle.Free();
            if (m_ColorsHandle.IsAllocated) m_ColorsHandle.Free();
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Surface call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_create(out IntPtr surface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_destroy(IntPtr surface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_clone", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_clone(IntPtr surface, out IntPtr clone);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_load_obj", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_load_obj(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_load_tri", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_load_tri(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_load_gifti", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_load_gifti(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_save_obj", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_save_obj(IntPtr surface, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.LPUTF8Str)] string textureName);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_set_vertices", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_set_vertices(IntPtr surface, [In] Vec3[] vertices, int vertexCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_set_normals", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_set_normals(IntPtr surface, [In] Vec3[] normals, int normalCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_set_uvs", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_set_uvs(IntPtr surface, [In] Vec2[] uvs, int uvCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_set_colors", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_set_colors(IntPtr surface, [In] Color4[] colors, int colorCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_set_triangles", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_set_triangles(IntPtr surface, [In] int[] triangles, int triangleIndexCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_merge", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_merge(IntPtr target, IntPtr source);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_get_sizes", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_get_sizes(IntPtr surface, out SurfaceSizes sizes);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_get_triangle_count", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_get_triangle_count(IntPtr surface, out int count);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_get_visible_triangle_count", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_get_visible_triangle_count(IntPtr surface, out int count);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_copy_unity_mesh", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_copy_unity_mesh(IntPtr surface, IntPtr vertices, int vertexCapacity, IntPtr normals, int normalCapacity, IntPtr uvs, int uvCapacity, IntPtr colors, int colorCapacity, IntPtr triangles, int triangleIndexCapacity);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_copy_visibility_mask", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_copy_visibility_mask(IntPtr surface, [Out] int[] mask, int maskCapacity);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_get_bounding_box", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_get_bounding_box(IntPtr surface, out IntPtr bbox);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_compute_normals", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_compute_normals(IntPtr surface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_flip", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_flip(IntPtr surface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_transform", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_transform(IntPtr surface, IntPtr transform);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_update_visibility_mask", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_update_visibility_mask(IntPtr surface, [In] int[] mask, int maskCount, out IntPtr invisibleSurface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_update_visibility_mask_with_ray", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_update_visibility_mask_with_ray(IntPtr surface, ref Vec3 rayDirection, ref Vec3 hitPoint, int mode, float degrees, out IntPtr invisibleSurface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_simplify", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_simplify(IntPtr surface, int targetTriangleCount, int aggressiveness, out IntPtr simplifiedSurface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_is_point_inside", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_is_point_inside(IntPtr surface, ref Vec3 point, out int inside);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_apply_mars_atlas_parcels", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_apply_mars_atlas_parcels(IntPtr surface, IntPtr atlas, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_cut", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_cut(IntPtr surface, [In] IntPtr[] planes, int planeCount, int noHoles, int strongCuts, out IntPtr surfaces);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generate_cuts", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generate_cuts(IntPtr surface, [In] IntPtr[] planes, int planeCount, int noHoles, int strongCuts, out IntPtr surfaces);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generate_raw_cuts", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generate_raw_cuts(IntPtr surface, [In] IntPtr[] planes, int planeCount, out IntPtr surfaces);
    }
}
