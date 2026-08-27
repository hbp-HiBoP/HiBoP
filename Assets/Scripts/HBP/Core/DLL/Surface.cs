using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using Microsoft.Win32.SafeHandles;
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

    public enum SurfaceInflationMethod
    {
        NonShrinkingSmoothing = 0,
        MetricRegularized = 1
    }

    public enum SurfaceInflationRescale
    {
        None = 0,
        PreserveRmsRadius = 1,
        PreserveArea = 2
    }

    public enum SurfaceInflationCoordinateSpace
    {
        CurrentSurfaceCoordinates,
        NativeGifti,
        NativeGiftiThenTransformed
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SurfaceInflationOptions
    {
        private uint m_StructSize;

        public SurfaceInflationMethod Method;
        public SurfaceInflationRescale Rescale;
        public int IterationCount;
        public double SmoothingStrength;
        public double MetricStrength;
        public double MaximumStepFraction;
        public double ConvergenceTolerance;
        public int MaximumBacktrackingSteps;
        internal int FixBoundaryVerticesValue;

        public uint StructSize => m_StructSize;

        public bool FixBoundaryVertices
        {
            get => FixBoundaryVerticesValue != 0;
            set => FixBoundaryVerticesValue = value ? 1 : 0;
        }

        public static SurfaceInflationOptions Baseline => Create(SurfaceInflationMethod.NonShrinkingSmoothing, 0.0);
        public static SurfaceInflationOptions Inflated => Create(SurfaceInflationMethod.MetricRegularized, 0.12);

        internal SurfaceInflationOptions PrepareForInterop()
        {
            m_StructSize = (uint)Marshal.SizeOf<SurfaceInflationOptions>();
            return this;
        }

        private static SurfaceInflationOptions Create(SurfaceInflationMethod method, double metricStrength)
        {
            return new SurfaceInflationOptions
            {
                m_StructSize = (uint)Marshal.SizeOf<SurfaceInflationOptions>(),
                Method = method,
                Rescale = SurfaceInflationRescale.PreserveRmsRadius,
                IterationCount = 160,
                SmoothingStrength = 0.35,
                MetricStrength = metricStrength,
                MaximumStepFraction = 0.20,
                ConvergenceTolerance = 1e-5,
                MaximumBacktrackingSteps = 8,
                FixBoundaryVertices = true
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SurfaceInflationDistribution
    {
        public double Percentile50;
        public double Percentile90;
        public double Percentile95;
        public double Percentile99;
        public double Maximum;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SurfaceInflationVector3
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3 ToVector3() => new((float)X, (float)Y, (float)Z);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SurfaceInflationReport
    {
        private uint m_StructSize;

        public int VertexCount;
        public int TriangleCount;
        public int EdgeCount;
        public int ComponentCount;
        public int SmallComponentCount;
        public int BoundaryEdgeCount;
        public int NonManifoldEdgeCount;
        public int NonManifoldVertexCount;
        public int IterationCount;
        public int BacktrackingStepCount;
        public int PreventedInversionCount;
        public int FinalInvertedTriangleCount;
        internal int ConvergedValue;
        public double InitialArea;
        public double FinalArea;
        public double AreaRatio;
        public double InitialRmsRadius;
        public double FinalRmsRadius;
        public double RmsRadiusRatio;
        public SurfaceInflationVector3 InitialBoundingBoxSize;
        public SurfaceInflationVector3 FinalBoundingBoxSize;
        public SurfaceInflationVector3 BoundingBoxSizeRatio;
        public SurfaceInflationDistribution EdgeLengthRatio;
        public SurfaceInflationDistribution TriangleAreaRatio;
        public SurfaceInflationDistribution VertexDisplacement;
        public SurfaceInflationDistribution EdgeLengthDistortion;
        public SurfaceInflationDistribution TriangleAreaDistortion;
        public double MaximumVertexDisplacement;
        public double ValidationMilliseconds;
        public double PreparationMilliseconds;
        public double InflationMilliseconds;
        public double FinalizationMilliseconds;

        public uint StructSize => m_StructSize;
        public bool Converged => ConvergedValue != 0;

        internal static SurfaceInflationReport CreateForInterop()
        {
            return new SurfaceInflationReport { m_StructSize = (uint)Marshal.SizeOf<SurfaceInflationReport>() };
        }
    }

    public sealed class SurfaceInflationResult
    {
        public Surface Surface { get; }
        public SurfaceInflationReport Report { get; }

        /// <summary>
        /// Identifies whether inflation used the surface's current coordinates or native GIFTI coordinates.
        /// The caller owns <see cref="Surface"/> and must dispose it.
        /// </summary>
        public SurfaceInflationCoordinateSpace CoordinateSpace { get; }

        internal SurfaceInflationResult(Surface surface, SurfaceInflationReport report, SurfaceInflationCoordinateSpace coordinateSpace)
        {
            Surface = surface;
            Report = report;
            CoordinateSpace = coordinateSpace;
        }
    }

    public sealed class SurfaceInflationException : InvalidOperationException
    {
        public SurfaceInflationReport Report { get; }

        internal SurfaceInflationException(string message, SurfaceInflationReport report) : base(message)
        {
            Report = report;
        }
    }

    public sealed class SurfaceInflationCanceledException : OperationCanceledException
    {
        public SurfaceInflationReport Report { get; }

        internal SurfaceInflationCanceledException(string message, SurfaceInflationReport report, CancellationToken cancellationToken) : base(message, cancellationToken)
        {
            Report = report;
        }
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
        public long GeometryVersion { get; private set; }

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

        internal static Surface FromOwnedLoadedHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero) throw new ArgumentException("A loaded surface requires a non-zero native handle.", nameof(handle));
            return new Surface(handle) { IsLoaded = true };
        }

        internal static HbpCoreStatus DestroyOwnedHandle(IntPtr handle)
        {
            return handle == IntPtr.Zero ? HbpCoreStatus.Ok : hbp_surface_destroy(handle);
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
            GeometryVersion = other.GeometryVersion;
        }

        public bool LoadOBJFile(string obj)
        {
            IsLoaded = hbp_surface_load_obj(_handle.Handle, obj) == HbpCoreStatus.Ok;
            if (IsLoaded) ++GeometryVersion;
            if (!IsLoaded) Debug.LogError("-ERROR : Surface::loadObjFile -> can't load obj file to surface : " + obj);
            return IsLoaded;
        }

        public bool LoadGIIFile(string gii, string transformation = "")
        {
            IsLoaded = hbp_surface_load_gifti(_handle.Handle, gii) == HbpCoreStatus.Ok;
            if (IsLoaded && !string.IsNullOrEmpty(transformation))
            {
                using Transformation3 transform = Transformation3.FromFile(transformation);
                ApplyTransformation(transform);
            }

            if (IsLoaded) ++GeometryVersion;

            if (!IsLoaded) Debug.LogError("-ERROR : Surface::loadGIIFile -> can't load GII file to surface : " + gii + " " + transformation);
            return IsLoaded;
        }

        /// <summary>
        /// Inflates this surface in its current coordinate system. This is the appropriate path for
        /// surfaces that have no persistent GIFTI source.
        /// </summary>
        public UniTask<SurfaceInflationResult> InflateAsync(SurfaceInflationOptions? options = null, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            return InflateCoreAsync(this, options, progress, cancellationToken, SurfaceInflationCoordinateSpace.CurrentSurfaceCoordinates);
        }

        /// <summary>
        /// Loads and inflates a GIFTI surface in native coordinates, then applies the requested
        /// transformation to the completed inflated surface.
        /// </summary>
        public static async UniTask<SurfaceInflationResult> InflateGIIFileAsync(string gii, string transformation = "", SurfaceInflationOptions? options = null, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(gii)) throw new ArgumentException("Expected a GIFTI file path.", nameof(gii));

            using Surface nativeSurface = new();
            if (!nativeSurface.LoadGIIFile(gii))
            {
                throw new InvalidOperationException($"Could not load GIFTI surface '{gii}': {HbpCoreRuntime.LastError}");
            }

            SurfaceInflationCoordinateSpace coordinateSpace = string.IsNullOrWhiteSpace(transformation) ? SurfaceInflationCoordinateSpace.NativeGifti : SurfaceInflationCoordinateSpace.NativeGiftiThenTransformed;
            SurfaceInflationResult result = await InflateCoreAsync(nativeSurface, options, progress, cancellationToken, coordinateSpace);
            try
            {
                if (!string.IsNullOrWhiteSpace(transformation))
                {
                    using Transformation3 transform = Transformation3.FromFile(transformation);
                    result.Surface.ApplyTransformation(transform);
                }

                return result;
            }
            catch
            {
                result.Surface.Dispose();
                throw;
            }
        }

        public bool LoadTRIFile(string tri, string transformation = "")
        {
            IsLoaded = hbp_surface_load_tri(_handle.Handle, tri) == HbpCoreStatus.Ok;
            if (IsLoaded && !string.IsNullOrEmpty(transformation))
            {
                using Transformation3 transform = Transformation3.FromFile(transformation);
                ThrowIfFailed(hbp_surface_transform(_handle.Handle, transform.getHandle().Handle));
            }

            if (IsLoaded) ++GeometryVersion;

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

        public void ApplyTransformation(Transformation3 transformation)
        {
            if (transformation == null) throw new ArgumentNullException(nameof(transformation));
            ThrowIfFailed(hbp_surface_transform(_handle.Handle, transformation.getHandle().Handle));
            ++GeometryVersion;
        }

        public void FlipTriangles()
        {
            ThrowIfFailed(hbp_surface_flip(_handle.Handle));
            ++GeometryVersion;
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
            ++GeometryVersion;
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
            (surface.GeometryVersion, GeometryVersion) = (GeometryVersion, surface.GeometryVersion);
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

            IsLoaded = true;
            ++GeometryVersion;
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
            ThrowIfFailed(DestroyOwnedHandle(_handle.Handle));
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

        private static async UniTask<SurfaceInflationResult> InflateCoreAsync(Surface source, SurfaceInflationOptions? options, IProgress<float> progress, CancellationToken cancellationToken, SurfaceInflationCoordinateSpace coordinateSpace)
        {
            using SurfaceInflationOperation operation = new(source, options);
            using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(operation.RequestCancellation);
            Task<SurfaceInflationExecution> executionTask = Task.Run(operation.Execute);
            Exception progressException = null;

            while (!executionTask.IsCompleted)
            {
                TryReportProgress(progress, operation.Progress, ref progressException);
                await Task.Delay(15);
            }

            SurfaceInflationExecution execution = await executionTask;
            TryReportProgress(progress, operation.Progress, ref progressException);
            if (progressException != null) throw progressException;

            SurfaceInflationReport report = operation.GetReport();
            if (execution.Status == HbpCoreStatus.Cancelled)
            {
                throw new SurfaceInflationCanceledException(execution.Error, report, cancellationToken);
            }

            if (execution.Status != HbpCoreStatus.Ok)
            {
                throw new SurfaceInflationException(execution.Error, report);
            }

            Surface result = operation.TakeResult();
            result.IsMarsAtlasLoaded = source.IsMarsAtlasLoaded;
            return new SurfaceInflationResult(result, report, coordinateSpace);
        }

        private static void TryReportProgress(IProgress<float> progress, float value, ref Exception progressException)
        {
            if (progress == null || progressException != null) return;
            try
            {
                progress.Report(value);
            }
            catch (Exception exception)
            {
                progressException = exception;
            }
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

    internal readonly struct SurfaceInflationExecution
    {
        public HbpCoreStatus Status { get; }
        public string Error { get; }

        public SurfaceInflationExecution(HbpCoreStatus status, string error)
        {
            Status = status;
            Error = error;
        }
    }

    internal sealed class SurfaceInflationOperation : IDisposable
    {
        private readonly SurfaceInflationJobHandle m_Handle;

        public float Progress
        {
            get
            {
                ThrowIfFailed(hbp_surface_inflation_get_progress(m_Handle, out float progress));
                return progress;
            }
        }

        public SurfaceInflationOperation(Surface source, SurfaceInflationOptions? options)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            HbpCoreStatus status;
            if (options.HasValue)
            {
                SurfaceInflationOptions nativeOptions = options.Value.PrepareForInterop();
                status = hbp_surface_inflation_create(source.getHandle().Handle, ref nativeOptions, out m_Handle);
            }
            else
            {
                status = hbp_surface_inflation_create(source.getHandle().Handle, IntPtr.Zero, out m_Handle);
            }

            if (status != HbpCoreStatus.Ok)
            {
                m_Handle?.Dispose();
                ThrowIfFailed(status);
            }
        }

        public SurfaceInflationExecution Execute()
        {
            HbpCoreStatus status = hbp_surface_inflation_execute(m_Handle);
            string error = status == HbpCoreStatus.Ok ? string.Empty : HbpCoreRuntime.LastError;
            return new SurfaceInflationExecution(status, error);
        }

        public void RequestCancellation()
        {
            if (m_Handle == null || m_Handle.IsClosed || m_Handle.IsInvalid) return;
            ThrowIfFailed(hbp_surface_inflation_request_cancel(m_Handle));
        }

        public SurfaceInflationReport GetReport()
        {
            SurfaceInflationReport report = SurfaceInflationReport.CreateForInterop();
            ThrowIfFailed(hbp_surface_inflation_get_report(m_Handle, ref report));
            return report;
        }

        public Surface TakeResult()
        {
            ThrowIfFailed(hbp_surface_inflation_take_result(m_Handle, out IntPtr surface));
            return Surface.FromOwnedLoadedHandle(surface);
        }

        public void Dispose()
        {
            m_Handle?.Dispose();
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core Surface inflation call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        private sealed class SurfaceInflationJobHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SurfaceInflationJobHandle() : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return hbp_surface_inflation_destroy(handle) == HbpCoreStatus.Ok;
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_create(IntPtr source, IntPtr options, out SurfaceInflationJobHandle job);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_create(IntPtr source, ref SurfaceInflationOptions options, out SurfaceInflationJobHandle job);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_execute", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_execute(SurfaceInflationJobHandle job);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_get_progress", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_get_progress(SurfaceInflationJobHandle job, out float progress);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_request_cancel", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_request_cancel(SurfaceInflationJobHandle job);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_take_result", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_take_result(SurfaceInflationJobHandle job, out IntPtr surface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_get_report", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_get_report(SurfaceInflationJobHandle job, ref SurfaceInflationReport report);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_inflation_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_inflation_destroy(IntPtr job);
    }
}
