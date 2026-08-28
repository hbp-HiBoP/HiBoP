using System;
using HBP.Core.Enums;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;

namespace HBP.Core.Object3D
{
    public enum RuntimeMeshOrigin
    {
        GeneratedFromMRI
    }

    public enum SurfaceRepresentation
    {
        Anatomical,
        Inflated
    }

    public enum SurfaceInflationPreset
    {
        Baseline,
        Inflated,
        Custom
    }

    public readonly struct Mesh3DInflationSettings
    {
        public const int AlgorithmVersion = 1;

        public SurfaceInflationPreset Preset { get; }
        public SurfaceInflationOptions Options { get; }

        public static Mesh3DInflationSettings Baseline => new(SurfaceInflationPreset.Baseline, SurfaceInflationOptions.Baseline);
        public static Mesh3DInflationSettings Inflated => new(SurfaceInflationPreset.Inflated, SurfaceInflationOptions.Inflated);

        public Mesh3DInflationSettings(SurfaceInflationPreset preset, SurfaceInflationOptions options)
        {
            Preset = preset;
            Options = options;
        }

        public static Mesh3DInflationSettings Custom(SurfaceInflationOptions options)
        {
            return new Mesh3DInflationSettings(SurfaceInflationPreset.Custom, options);
        }
    }

    public readonly struct SurfaceInflationCacheKey : IEquatable<SurfaceInflationCacheKey>
    {
        public string SourceGeometryIdentity { get; }
        public int AlgorithmVersion { get; }
        public SurfaceInflationPreset Preset { get; }
        public SurfaceInflationMethod Method { get; }
        public SurfaceInflationRescale Rescale { get; }
        public int IterationCount { get; }
        public double SmoothingStrength { get; }
        public double MetricStrength { get; }
        public double MaximumStepFraction { get; }
        public double ConvergenceTolerance { get; }
        public int MaximumBacktrackingSteps { get; }
        public bool FixBoundaryVertices { get; }

        internal SurfaceInflationCacheKey(string sourceGeometryIdentity, Mesh3DInflationSettings settings)
        {
            SurfaceInflationOptions options = settings.Options;
            SourceGeometryIdentity = sourceGeometryIdentity;
            AlgorithmVersion = Mesh3DInflationSettings.AlgorithmVersion;
            Preset = settings.Preset;
            Method = options.Method;
            Rescale = options.Rescale;
            IterationCount = options.IterationCount;
            SmoothingStrength = options.SmoothingStrength;
            MetricStrength = options.MetricStrength;
            MaximumStepFraction = options.MaximumStepFraction;
            ConvergenceTolerance = options.ConvergenceTolerance;
            MaximumBacktrackingSteps = options.MaximumBacktrackingSteps;
            FixBoundaryVertices = options.FixBoundaryVertices;
        }

        public bool Equals(SurfaceInflationCacheKey other)
        {
            return SourceGeometryIdentity == other.SourceGeometryIdentity && AlgorithmVersion == other.AlgorithmVersion && Preset == other.Preset && Method == other.Method && Rescale == other.Rescale && IterationCount == other.IterationCount && SmoothingStrength.Equals(other.SmoothingStrength) && MetricStrength.Equals(other.MetricStrength) && MaximumStepFraction.Equals(other.MaximumStepFraction) && ConvergenceTolerance.Equals(other.ConvergenceTolerance) && MaximumBacktrackingSteps == other.MaximumBacktrackingSteps && FixBoundaryVertices == other.FixBoundaryVertices;
        }

        public override bool Equals(object obj)
        {
            return obj is SurfaceInflationCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(SourceGeometryIdentity);
            hash.Add(AlgorithmVersion);
            hash.Add(Preset);
            hash.Add(Method);
            hash.Add(Rescale);
            hash.Add(IterationCount);
            hash.Add(SmoothingStrength);
            hash.Add(MetricStrength);
            hash.Add(MaximumStepFraction);
            hash.Add(ConvergenceTolerance);
            hash.Add(MaximumBacktrackingSteps);
            hash.Add(FixBoundaryVertices);
            return hash.ToHashCode();
        }

        public static bool operator ==(SurfaceInflationCacheKey left, SurfaceInflationCacheKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SurfaceInflationCacheKey left, SurfaceInflationCacheKey right)
        {
            return !left.Equals(right);
        }
    }

    public sealed class Mesh3DInflatedRepresentation
    {
        public SurfaceInflationCacheKey CacheKey { get; }
        public DLL.Surface Both { get; }
        public DLL.Surface SimplifiedBoth { get; }
        public DLL.Surface Left { get; }
        public DLL.Surface Right { get; }
        public DLL.Surface SimplifiedLeft { get; }
        public DLL.Surface SimplifiedRight { get; }
        public SurfaceInflationReport? BothReport { get; }
        public SurfaceInflationReport? LeftReport { get; }
        public SurfaceInflationReport? RightReport { get; }
        public SurfaceInflationCoordinateSpace CoordinateSpace { get; }

        internal Mesh3DInflatedRepresentation(SurfaceInflationCacheKey cacheKey, DLL.Surface both, DLL.Surface simplifiedBoth, SurfaceInflationReport? bothReport, SurfaceInflationCoordinateSpace coordinateSpace, DLL.Surface left = null, DLL.Surface right = null, DLL.Surface simplifiedLeft = null, DLL.Surface simplifiedRight = null, SurfaceInflationReport? leftReport = null, SurfaceInflationReport? rightReport = null)
        {
            CacheKey = cacheKey;
            Both = both;
            SimplifiedBoth = simplifiedBoth;
            BothReport = bothReport;
            CoordinateSpace = coordinateSpace;
            Left = left;
            Right = right;
            SimplifiedLeft = simplifiedLeft;
            SimplifiedRight = simplifiedRight;
            LeftReport = leftReport;
            RightReport = rightReport;
        }

        internal void Dispose()
        {
            Mesh3D.DisposeSurfaces(Both, SimplifiedBoth, Left, Right, SimplifiedLeft, SimplifiedRight);
        }
    }

    /// <summary>
    /// This class contains information about a mesh and can load meshes to DLL objects
    /// </summary>
    public abstract class Mesh3D : ICloneable
    {
        #region Properties

        /// <summary>
        /// Name of the mesh
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the mesh (Patient or MNI)
        /// </summary>
        public MeshType Type { get; protected set; }

        protected DLL.Surface m_Both;

        /// <summary>
        /// DLL surface containing data for the whole brain mesh
        /// </summary>
        public DLL.Surface Both
        {
            get
            {
                ThrowIfLoadingSynchronously();
                if (!IsLoaded) Load();
                return m_Both;
            }
            protected set { m_Both = value; }
        }

        protected DLL.Surface m_SimplifiedBoth;

        /// <summary>
        /// DLL surface containing data for the whole simplified brain mesh
        /// </summary>
        public DLL.Surface SimplifiedBoth
        {
            get
            {
                ThrowIfLoadingSynchronously();
                if (!IsLoaded) Load();
                return m_SimplifiedBoth;
            }
            protected set { m_SimplifiedBoth = value; }
        }

        /// <summary>
        /// Is the 3D mesh completely loaded ?
        /// </summary>
        public bool IsLoaded
        {
            get { return m_Both != null ? m_Both.IsLoaded : false; }
        }

        /// <summary>
        /// Is mars atlas loaded for this mesh ?
        /// </summary>
        public bool IsMarsAtlasLoaded
        {
            get { return m_Both != null ? m_Both.IsMarsAtlasLoaded : false; }
        }

        /// <summary>
        /// Whether this mesh can display MarsAtlas information.
        /// </summary>
        public virtual bool SupportsMarsAtlas => Type == MeshType.MNI || IsMarsAtlasLoaded;

        /// <summary>
        /// Whether this mesh can display JuBrain and other MNI-only resources.
        /// </summary>
        public virtual bool SupportsMNIResources => Type == MeshType.MNI;

        /// <summary>
        /// Whether this mesh exposes independent left and right surfaces.
        /// </summary>
        public virtual bool SupportsHemispheres => this is LeftRightMesh3D;

        /// <summary>
        /// Representation selected on this mesh. Scene display integration is handled by MeshManager.
        /// </summary>
        public SurfaceRepresentation Representation { get; private set; } = SurfaceRepresentation.Anatomical;

        public bool HasInflatedRepresentation => TryGetActiveInflatedRepresentation(out _);
        public bool IsInflationInProgress { get; private set; }
        public string KnownInflationInadmissibility { get; private set; }

        public int InflatedRepresentationCacheCount
        {
            get
            {
                lock (m_InflatedRepresentations)
                {
                    return m_InflatedRepresentations.Count;
                }
            }
        }

        public Mesh3DInflatedRepresentation ActiveInflatedRepresentation
        {
            get
            {
                TryGetActiveInflatedRepresentation(out Mesh3DInflatedRepresentation representation);
                return representation;
            }
        }

        /// <summary>
        /// Is the mesh currently loading ?
        /// </summary>
        protected volatile bool m_IsLoading = false;

        /// <summary>
        /// Does the mesh have been loaded outside of a scene and copied to the scene (e.g. MNI objects) ?
        /// </summary>
        public bool HasBeenLoadedOutside { get; protected set; }

        /// <summary>
        /// Data of the mesh (paths etc.)
        /// </summary>
        protected Data.BaseMesh m_Mesh;

        private readonly Dictionary<SurfaceInflationCacheKey, Mesh3DInflatedRepresentation> m_InflatedRepresentations = new();
        private readonly SemaphoreSlim m_InflationGate = new(1, 1);
        private CancellationTokenSource m_InflationLifetime = new();
        private Mesh3DInflatedRepresentation m_ActiveInflatedRepresentation;
        private int m_InflationGeneration;
        private string m_InflationInadmissibilitySourceIdentity;

        #endregion

        #region Constructors

        public Mesh3D(Data.BaseMesh mesh, MeshType type, bool load)
        {
            m_Mesh = mesh;
            Name = mesh.Name;
            Type = type;
            if (load) Load();
            HasBeenLoadedOutside = false;
        }

        public Mesh3D()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// Waits for an in-progress load without blocking the Unity thread, then loads the mesh if needed.
        /// </summary>
        public async UniTask EnsureLoadedAsync(CancellationToken cancellationToken = default)
        {
            while (m_IsLoading)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!IsLoaded) Load();

            while (m_IsLoading)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }

            if (!IsLoaded) throw new InvalidOperationException($"Mesh '{Name}' could not be loaded.");
        }

        public UniTask<Mesh3DInflatedRepresentation> GenerateInflatedRepresentationAsync(IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            return GenerateInflatedRepresentationAsync(Mesh3DInflationSettings.Inflated, progress, cancellationToken);
        }

        /// <summary>
        /// Reports whether inflation can currently be requested and explains known failures.
        /// Full geometric validation remains native and is learned after the first rejected request.
        /// </summary>
        public bool TryGetInflationAvailability(out string reason)
        {
            reason = null;
            if (IsInflationInProgress)
            {
                reason = "Surface inflation is already in progress.";
                return false;
            }

            if (!IsLoaded)
            {
                reason = "The selected mesh is not loaded.";
                return false;
            }

            string sourceIdentity = CreateSourceGeometryIdentity();
            if (!string.IsNullOrWhiteSpace(KnownInflationInadmissibility) && m_InflationInadmissibilitySourceIdentity == sourceIdentity)
            {
                reason = KnownInflationInadmissibility;
                return false;
            }

            if (m_Both.NumberOfVertices < 3 || m_Both.NumberOfTriangles < 1)
            {
                reason = "The selected mesh does not contain enough vertices and triangles to be inflated.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a cached inflated representation or generates and publishes one transactionally.
        /// </summary>
        public async UniTask<Mesh3DInflatedRepresentation> GenerateInflatedRepresentationAsync(Mesh3DInflationSettings settings, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            await m_InflationGate.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);
                SurfaceInflationCacheKey cacheKey;
                Mesh3DInflatedRepresentation cached;
                Mesh3DInflatedRepresentation[] staleRepresentations;
                int generation;
                CancellationToken lifetimeToken;
                lock (m_InflatedRepresentations)
                {
                    string sourceGeometryIdentity = CreateSourceGeometryIdentity();
                    List<SurfaceInflationCacheKey> staleKeys = new();
                    List<Mesh3DInflatedRepresentation> staleValues = new();
                    foreach (KeyValuePair<SurfaceInflationCacheKey, Mesh3DInflatedRepresentation> entry in m_InflatedRepresentations)
                    {
                        if (entry.Key.SourceGeometryIdentity == sourceGeometryIdentity) continue;
                        staleKeys.Add(entry.Key);
                        staleValues.Add(entry.Value);
                    }

                    foreach (SurfaceInflationCacheKey staleKey in staleKeys)
                    {
                        m_InflatedRepresentations.Remove(staleKey);
                    }

                    staleRepresentations = staleValues.ToArray();
                    if (m_ActiveInflatedRepresentation != null && m_ActiveInflatedRepresentation.CacheKey.SourceGeometryIdentity != sourceGeometryIdentity)
                    {
                        m_ActiveInflatedRepresentation = null;
                        Representation = SurfaceRepresentation.Anatomical;
                    }

                    cacheKey = new SurfaceInflationCacheKey(sourceGeometryIdentity, settings);
                    m_InflatedRepresentations.TryGetValue(cacheKey, out cached);
                    generation = m_InflationGeneration;
                    lifetimeToken = m_InflationLifetime.Token;
                    IsInflationInProgress = cached == null;
                }

                foreach (Mesh3DInflatedRepresentation staleRepresentation in staleRepresentations)
                {
                    staleRepresentation.Dispose();
                }

                if (cached != null)
                {
                    progress?.Report(1.0f);
                    lock (m_InflatedRepresentations)
                    {
                        if (generation != m_InflationGeneration || !m_InflatedRepresentations.TryGetValue(cacheKey, out Mesh3DInflatedRepresentation current) || !ReferenceEquals(current, cached))
                        {
                            throw new OperationCanceledException("The inflated representation cache changed before the result could be returned.", cancellationToken);
                        }

                        m_ActiveInflatedRepresentation = cached;
                    }

                    return cached;
                }

                using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetimeToken);
                Mesh3DInflatedRepresentation candidate = await CreateInflatedRepresentationAsync(cacheKey, settings.Options, progress, linkedCancellation.Token);
                try
                {
                    lock (m_InflatedRepresentations)
                    {
                        linkedCancellation.Token.ThrowIfCancellationRequested();
                        if (generation != m_InflationGeneration || cacheKey.SourceGeometryIdentity != CreateSourceGeometryIdentity())
                        {
                            throw new OperationCanceledException("The source mesh changed before the inflated representation could be published.", linkedCancellation.Token);
                        }

                        m_InflatedRepresentations.Add(cacheKey, candidate);
                        m_ActiveInflatedRepresentation = candidate;
                    }

                    Mesh3DInflatedRepresentation published = candidate;
                    candidate = null;
                    return published;
                }
                finally
                {
                    candidate?.Dispose();
                }
            }
            catch (SurfaceInflationException exception)
            {
                lock (m_InflatedRepresentations)
                {
                    KnownInflationInadmissibility = exception.Message;
                    m_InflationInadmissibilitySourceIdentity = CreateSourceGeometryIdentity();
                }

                throw;
            }
            finally
            {
                IsInflationInProgress = false;
                m_InflationGate.Release();
            }
        }

        public void SelectRepresentation(SurfaceRepresentation representation)
        {
            if (representation == SurfaceRepresentation.Inflated && !TryGetActiveInflatedRepresentation(out _))
                throw new InvalidOperationException("An inflated representation must be generated before it can be selected.");

            Representation = representation;
        }

        public DLL.Surface GetSurface(MeshPart part = MeshPart.Both, bool simplified = false)
        {
            return GetSurface(Representation, part, simplified);
        }

        public DLL.Surface GetSurface(SurfaceRepresentation representation, MeshPart part = MeshPart.Both, bool simplified = false)
        {
            return representation switch
            {
                SurfaceRepresentation.Anatomical => GetAnatomicalSurface(part, simplified),
                SurfaceRepresentation.Inflated => GetInflatedSurface(part, simplified),
                _ => throw new ArgumentOutOfRangeException(nameof(representation))
            };
        }

        /// <summary>
        /// Invalidates and disposes only scene-owned derived surfaces. Anatomical surfaces are untouched.
        /// </summary>
        public void ClearInflatedRepresentations()
        {
            CancellationTokenSource inflationLifetime;
            Mesh3DInflatedRepresentation[] representations;
            lock (m_InflatedRepresentations)
            {
                ++m_InflationGeneration;
                inflationLifetime = m_InflationLifetime;
                m_InflationLifetime = new CancellationTokenSource();
                Representation = SurfaceRepresentation.Anatomical;
                m_ActiveInflatedRepresentation = null;
                KnownInflationInadmissibility = null;
                m_InflationInadmissibilitySourceIdentity = null;
                representations = new Mesh3DInflatedRepresentation[m_InflatedRepresentations.Count];
                m_InflatedRepresentations.Values.CopyTo(representations, 0);
                m_InflatedRepresentations.Clear();
            }

            inflationLifetime.Cancel();
            inflationLifetime.Dispose();
            foreach (Mesh3DInflatedRepresentation representation in representations)
            {
                representation.Dispose();
            }
        }

        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public virtual void Clean()
        {
            ClearInflatedRepresentations();
            m_Both?.Dispose();
            m_SimplifiedBoth?.Dispose();
            m_Both = null;
            m_SimplifiedBoth = null;
        }

        public abstract object Clone();

        protected virtual async UniTask<Mesh3DInflatedRepresentation> CreateInflatedRepresentationAsync(SurfaceInflationCacheKey cacheKey, SurfaceInflationOptions options, IProgress<float> progress, CancellationToken cancellationToken)
        {
            SurfaceInflationResult result = await m_Both.InflateAsync(options, progress, cancellationToken);
            return CreateSingleInflatedRepresentation(cacheKey, result);
        }

        protected virtual DLL.Surface GetAnatomicalSurface(MeshPart part, bool simplified)
        {
            if (part != MeshPart.Both) throw new ArgumentException("This mesh has no independent hemispheres.", nameof(part));
            return simplified ? m_SimplifiedBoth : m_Both;
        }

        protected virtual DLL.Surface GetInflatedSurface(MeshPart part, bool simplified)
        {
            Mesh3DInflatedRepresentation representation = ActiveInflatedRepresentation ?? throw new InvalidOperationException("No current inflated representation has been generated.");
            if (part != MeshPart.Both) throw new ArgumentException("This mesh has no independent hemispheres.", nameof(part));
            return simplified ? representation.SimplifiedBoth : representation.Both;
        }

        protected static Mesh3DInflatedRepresentation CreateSingleInflatedRepresentation(SurfaceInflationCacheKey cacheKey, SurfaceInflationResult result)
        {
            DLL.Surface simplified = null;
            try
            {
                simplified = result.Surface.Simplify();
                return new Mesh3DInflatedRepresentation(cacheKey, result.Surface, simplified, result.Report, result.CoordinateSpace);
            }
            catch
            {
                simplified?.Dispose();
                result.Surface.Dispose();
                throw;
            }
        }

        protected virtual string CreateSourceGeometryIdentity()
        {
            return CreateSurfaceIdentity(m_Both);
        }

        protected static string CreateSurfaceIdentity(DLL.Surface surface)
        {
            if (surface == null || !surface.IsLoaded) return "unloaded";
            return string.Join(":", surface.getHandle().Handle.ToInt64().ToString(CultureInfo.InvariantCulture), surface.GeometryVersion.ToString(CultureInfo.InvariantCulture), surface.NumberOfVertices.ToString(CultureInfo.InvariantCulture), surface.NumberOfTriangles.ToString(CultureInfo.InvariantCulture));
        }

        protected static string CreateFileIdentity(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "none";
            string fullPath = Path.GetFullPath(path);
            FileInfo file = new(fullPath);
            return file.Exists ? string.Join("|", fullPath, file.Length.ToString(CultureInfo.InvariantCulture), file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)) : fullPath;
        }

        protected static IProgress<float> ScaleProgress(IProgress<float> progress, float offset, float scale)
        {
            return progress == null ? null : new ScaledProgress(progress, offset, scale);
        }

        internal static void DisposeSurfaces(params DLL.Surface[] surfaces)
        {
            HashSet<DLL.Surface> disposed = new();
            foreach (DLL.Surface surface in surfaces)
            {
                if (surface != null && disposed.Add(surface)) surface.Dispose();
            }
        }

        protected void ThrowIfLoadingSynchronously()
        {
            if (m_IsLoading)
                throw new InvalidOperationException($"Mesh '{Name}' is loading. Await EnsureLoadedAsync instead of blocking the Unity thread.");
        }

        private bool TryGetActiveInflatedRepresentation(out Mesh3DInflatedRepresentation representation)
        {
            lock (m_InflatedRepresentations)
            {
                representation = m_ActiveInflatedRepresentation;
                if (representation == null) return false;
                if (representation.CacheKey.SourceGeometryIdentity == CreateSourceGeometryIdentity()) return true;

                m_ActiveInflatedRepresentation = null;
                Representation = SurfaceRepresentation.Anatomical;
                representation = null;
                return false;
            }
        }

        private sealed class ScaledProgress : IProgress<float>
        {
            private readonly IProgress<float> m_Progress;
            private readonly float m_Offset;
            private readonly float m_Scale;

            public ScaledProgress(IProgress<float> progress, float offset, float scale)
            {
                m_Progress = progress;
                m_Offset = offset;
                m_Scale = scale;
            }

            public void Report(float value)
            {
                m_Progress.Report(m_Offset + Mathf.Clamp01(value) * m_Scale);
            }
        }

        #endregion
    }

    /// <summary>
    /// Subclass of <see cref="Mesh3D"/> that contains data for a mesh in one piece
    /// </summary>
    public class SingleMesh3D : Mesh3D
    {
        private string m_LoadedGiftiPath;
        private string m_LoadedTransformationPath;
        private string m_LoadedMarsAtlasPath;

        #region Constructors

        public SingleMesh3D(Data.SingleMesh mesh, MeshType type, bool load) : base(mesh, type, load)
        {
        }

        public SingleMesh3D()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public override void Load()
        {
            m_IsLoading = true;
            DLL.Surface loadedBoth = null;
            DLL.Surface loadedSimplifiedBoth = null;
            try
            {
                Data.SingleMesh mesh = m_Mesh as Data.SingleMesh;
                loadedBoth = new DLL.Surface();
                if (loadedBoth.LoadGIIFile(mesh.Path, mesh.Transformation))
                {
                    loadedBoth.FlipTriangles();
                    loadedBoth.ComputeNormals();
                    if (Object3DManager.MarsAtlas.Loaded)
                        loadedBoth.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, mesh.MarsAtlasPath);
                    loadedSimplifiedBoth = loadedBoth.Simplify();

                    DLL.Surface previousBoth = m_Both;
                    DLL.Surface previousSimplifiedBoth = m_SimplifiedBoth;
                    ClearInflatedRepresentations();
                    m_Both = loadedBoth;
                    m_SimplifiedBoth = loadedSimplifiedBoth;
                    m_LoadedGiftiPath = mesh.Path;
                    m_LoadedTransformationPath = mesh.Transformation;
                    m_LoadedMarsAtlasPath = mesh.MarsAtlasPath;
                    loadedBoth = null;
                    loadedSimplifiedBoth = null;
                    DisposeSurfaces(previousBoth, previousSimplifiedBoth);
                }
            }
            finally
            {
                DisposeSurfaces(loadedBoth, loadedSimplifiedBoth);
                m_IsLoading = false;
            }
        }

        public override object Clone()
        {
            SingleMesh3D mesh = new()
            {
                Name = Name,
                Type = Type,
                Both = Both,
                SimplifiedBoth = SimplifiedBoth,
                m_Mesh = m_Mesh,
                m_LoadedGiftiPath = m_LoadedGiftiPath,
                m_LoadedTransformationPath = m_LoadedTransformationPath,
                m_LoadedMarsAtlasPath = m_LoadedMarsAtlasPath,
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mesh;
        }

        protected override async UniTask<Mesh3DInflatedRepresentation> CreateInflatedRepresentationAsync(SurfaceInflationCacheKey cacheKey, SurfaceInflationOptions options, IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(m_LoadedGiftiPath))
                return await base.CreateInflatedRepresentationAsync(cacheKey, options, progress, cancellationToken);

            SurfaceInflationResult result = await DLL.Surface.InflateGIIFileAsync(m_LoadedGiftiPath, m_LoadedTransformationPath, options, progress, cancellationToken);
            try
            {
                result.Surface.FlipTriangles();
                result.Surface.ComputeNormals();
                if (m_Both.IsMarsAtlasLoaded && Object3DManager.MarsAtlas.Loaded)
                    result.Surface.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, m_LoadedMarsAtlasPath);
                return CreateSingleInflatedRepresentation(cacheKey, result);
            }
            catch
            {
                result.Surface.Dispose();
                throw;
            }
        }

        protected override string CreateSourceGeometryIdentity()
        {
            if (string.IsNullOrWhiteSpace(m_LoadedGiftiPath))
                return base.CreateSourceGeometryIdentity();
            return string.Join(";", CreateFileIdentity(m_LoadedGiftiPath), CreateFileIdentity(m_LoadedTransformationPath));
        }

        #endregion
    }

    /// <summary>
    /// Scene-owned, non-persistent mesh generated directly from an MRI volume.
    /// </summary>
    public sealed class RuntimeSingleMesh3D : SingleMesh3D
    {
        public MRI3D SourceMRI { get; }
        public string SourceMRIName => SourceMRI.Name;
        public RuntimeMeshOrigin Origin => RuntimeMeshOrigin.GeneratedFromMRI;
        public bool IsTransient => true;
        public override bool SupportsMarsAtlas => false;
        public override bool SupportsMNIResources => false;
        public override bool SupportsHemispheres => false;
        public DLL.PreviewSurfaceReport GenerationReport { get; }

        public RuntimeSingleMesh3D(MRI3D sourceMRI, DLL.Surface surface, DLL.PreviewSurfaceReport generationReport, DLL.Surface simplifiedSurface = null)
        {
            if (sourceMRI == null) throw new ArgumentNullException(nameof(sourceMRI));
            if (string.IsNullOrWhiteSpace(sourceMRI.Name)) throw new ArgumentException("The source MRI name is required.", nameof(sourceMRI));
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            if (!surface.IsLoaded) throw new ArgumentException("The runtime surface must already be loaded.", nameof(surface));
            if (simplifiedSurface != null && !simplifiedSurface.IsLoaded)
                throw new ArgumentException("The simplified runtime surface must already be loaded.", nameof(simplifiedSurface));
            if (ReferenceEquals(surface, simplifiedSurface))
                throw new ArgumentException("The complete and simplified runtime surfaces must have distinct ownership.", nameof(simplifiedSurface));

            SourceMRI = sourceMRI;
            Name = $"MRI preview – {sourceMRI.Name}";
            Type = MeshType.Patient;
            GenerationReport = generationReport;
            HasBeenLoadedOutside = false;
            m_Both = surface;

            try
            {
                m_SimplifiedBoth = simplifiedSurface ?? CreateSimplifiedSurface(surface);
            }
            catch
            {
                m_Both = null;
                surface.Dispose();
                throw;
            }
        }

        public override void Load()
        {
            if (m_Both == null || !m_Both.IsLoaded || m_SimplifiedBoth == null || !m_SimplifiedBoth.IsLoaded)
            {
                throw new InvalidOperationException("The transient MRI preview has lost its scene-owned surfaces and cannot be loaded from disk.");
            }
        }

        public override void Clean()
        {
            ClearInflatedRepresentations();
            DLL.Surface surface = m_Both;
            DLL.Surface simplifiedSurface = m_SimplifiedBoth;
            m_Both = null;
            m_SimplifiedBoth = null;

            surface?.Dispose();
            if (!ReferenceEquals(surface, simplifiedSurface)) simplifiedSurface?.Dispose();
        }

        public override object Clone()
        {
            throw new NotSupportedException("Transient MRI preview meshes are scene-owned and cannot be cloned.");
        }

        private static DLL.Surface CreateSimplifiedSurface(DLL.Surface surface)
        {
            return surface.NumberOfTriangles > 10000 ? surface.Simplify() : (DLL.Surface)surface.Clone();
        }
    }

    public readonly struct RuntimePreviewDistanceReport
    {
        public int SiteCount { get; }
        public int SitesBeyondInfluence { get; }
        public float InfluenceDistance { get; }
        public float Percentile50 { get; }
        public float Percentile90 { get; }
        public float Percentile95 { get; }
        public float FractionBeyondInfluence => SiteCount == 0 ? 0f : (float)SitesBeyondInfluence / SiteCount;
        public float SuggestedInfluenceDistance => Mathf.Ceil(Percentile90 + 2f);
        public bool ShouldWarn => SiteCount > 0 && FractionBeyondInfluence >= 0.25f;

        internal RuntimePreviewDistanceReport(int siteCount, int sitesBeyondInfluence, float influenceDistance, float percentile50, float percentile90, float percentile95)
        {
            SiteCount = siteCount;
            SitesBeyondInfluence = sitesBeyondInfluence;
            InfluenceDistance = influenceDistance;
            Percentile50 = percentile50;
            Percentile90 = percentile90;
            Percentile95 = percentile95;
        }
    }

    public static class RuntimePreviewDistanceDiagnostic
    {
        public static RuntimePreviewDistanceReport Evaluate(Vector3[] surfaceVertices, Vector3[] sitePositions, float influenceDistance)
        {
            if (surfaceVertices == null) throw new ArgumentNullException(nameof(surfaceVertices));
            if (sitePositions == null) throw new ArgumentNullException(nameof(sitePositions));
            if (surfaceVertices.Length == 0) throw new ArgumentException("At least one surface vertex is required.", nameof(surfaceVertices));
            if (float.IsNaN(influenceDistance) || float.IsInfinity(influenceDistance) || influenceDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(influenceDistance));
            if (sitePositions.Length == 0)
                return new RuntimePreviewDistanceReport(0, 0, influenceDistance, 0f, 0f, 0f);

            float[] distances = new float[sitePositions.Length];
            int sitesBeyondInfluence = 0;
            for (int siteIndex = 0; siteIndex < sitePositions.Length; ++siteIndex)
            {
                float minimumSquaredDistance = float.PositiveInfinity;
                for (int vertexIndex = 0; vertexIndex < surfaceVertices.Length; ++vertexIndex)
                {
                    float squaredDistance = (sitePositions[siteIndex] - surfaceVertices[vertexIndex]).sqrMagnitude;
                    if (squaredDistance < minimumSquaredDistance) minimumSquaredDistance = squaredDistance;
                }

                float distance = Mathf.Sqrt(minimumSquaredDistance);
                distances[siteIndex] = distance;
                if (distance > influenceDistance) ++sitesBeyondInfluence;
            }

            Array.Sort(distances);
            return new RuntimePreviewDistanceReport(distances.Length, sitesBeyondInfluence, influenceDistance, Percentile(distances, 0.50f), Percentile(distances, 0.90f), Percentile(distances, 0.95f));
        }

        private static float Percentile(float[] sortedValues, float percentile)
        {
            int index = Mathf.Clamp(Mathf.CeilToInt(percentile * sortedValues.Length) - 1, 0, sortedValues.Length - 1);
            return sortedValues[index];
        }
    }

    /// <summary>
    /// Subclass of <see cref="Mesh3D"/> that contains data for a mesh divided in two hemispheres
    /// </summary>
    public class LeftRightMesh3D : Mesh3D
    {
        private string m_LoadedLeftGiftiPath;
        private string m_LoadedRightGiftiPath;
        private string m_LoadedTransformationPath;
        private string m_LoadedLeftMarsAtlasPath;
        private string m_LoadedRightMarsAtlasPath;

        #region Properties

        protected DLL.Surface m_Left;

        /// <summary>
        /// DLL surface containing data for the left part of the mesh
        /// </summary>
        public DLL.Surface Left
        {
            get
            {
                ThrowIfLoadingSynchronously();
                if (!IsLoaded) Load();
                return m_Left;
            }
            protected set { m_Left = value; }
        }

        protected DLL.Surface m_Right;

        /// <summary>
        /// DLL surface containing data for the right part of the mesh
        /// </summary>
        public DLL.Surface Right
        {
            get
            {
                ThrowIfLoadingSynchronously();
                if (!IsLoaded) Load();
                return m_Right;
            }
            protected set { m_Right = value; }
        }

        protected DLL.Surface m_SimplifiedLeft;

        /// <summary>
        /// DLL surface containing data for the left simplified part of the mesh
        /// </summary>
        public DLL.Surface SimplifiedLeft
        {
            get
            {
                ThrowIfLoadingSynchronously();
                if (!IsLoaded) Load();
                return m_SimplifiedLeft;
            }
            protected set { m_SimplifiedLeft = value; }
        }

        protected DLL.Surface m_SimplifiedRight;

        /// <summary>
        /// DLL surface containing data for the right simplified part of the mesh
        /// </summary>
        public DLL.Surface SimplifiedRight
        {
            get
            {
                ThrowIfLoadingSynchronously();
                if (!IsLoaded) Load();
                return m_SimplifiedRight;
            }
            protected set { m_SimplifiedRight = value; }
        }

        #endregion

        #region Constructors

        public LeftRightMesh3D(Data.LeftRightMesh mesh, MeshType type, bool load) : base(mesh, type, load)
        {
        }

        public LeftRightMesh3D(string name, DLL.Surface left, DLL.Surface right, DLL.Surface both, MeshType type)
        {
            Name = name;
            Type = type;
            Left = left;
            Right = right;
            Both = both;
            SimplifiedLeft = left.Simplify();
            SimplifiedRight = right.Simplify();
            SimplifiedBoth = both.Simplify();
            HasBeenLoadedOutside = true;
        }

        public LeftRightMesh3D()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public override void Load()
        {
            m_IsLoading = true;
            DLL.Surface loadedLeft = null;
            DLL.Surface loadedRight = null;
            DLL.Surface loadedBoth = null;
            DLL.Surface loadedSimplifiedLeft = null;
            DLL.Surface loadedSimplifiedRight = null;
            DLL.Surface loadedSimplifiedBoth = null;
            try
            {
                Data.LeftRightMesh mesh = m_Mesh as Data.LeftRightMesh;
                loadedLeft = new DLL.Surface();
                if (!loadedLeft.LoadGIIFile(mesh.LeftHemisphere, mesh.Transformation)) return;
                loadedLeft.FlipTriangles();
                loadedLeft.ComputeNormals();
                if (Object3DManager.MarsAtlas.Loaded)
                    loadedLeft.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, mesh.LeftMarsAtlasHemisphere);
                loadedSimplifiedLeft = loadedLeft.Simplify();

                loadedRight = new DLL.Surface();
                if (!loadedRight.LoadGIIFile(mesh.RightHemisphere, mesh.Transformation)) return;
                loadedRight.FlipTriangles();
                loadedRight.ComputeNormals();
                if (Object3DManager.MarsAtlas.Loaded)
                    loadedRight.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, mesh.RightMarsAtlasHemisphere);
                loadedSimplifiedRight = loadedRight.Simplify();

                loadedBoth = (DLL.Surface)loadedLeft.Clone();
                loadedBoth.Append(loadedRight);
                loadedSimplifiedBoth = loadedBoth.Simplify();

                DLL.Surface previousLeft = m_Left;
                DLL.Surface previousRight = m_Right;
                DLL.Surface previousBoth = m_Both;
                DLL.Surface previousSimplifiedLeft = m_SimplifiedLeft;
                DLL.Surface previousSimplifiedRight = m_SimplifiedRight;
                DLL.Surface previousSimplifiedBoth = m_SimplifiedBoth;
                ClearInflatedRepresentations();
                m_Left = loadedLeft;
                m_Right = loadedRight;
                m_Both = loadedBoth;
                m_SimplifiedLeft = loadedSimplifiedLeft;
                m_SimplifiedRight = loadedSimplifiedRight;
                m_SimplifiedBoth = loadedSimplifiedBoth;
                m_LoadedLeftGiftiPath = mesh.LeftHemisphere;
                m_LoadedRightGiftiPath = mesh.RightHemisphere;
                m_LoadedTransformationPath = mesh.Transformation;
                m_LoadedLeftMarsAtlasPath = mesh.LeftMarsAtlasHemisphere;
                m_LoadedRightMarsAtlasPath = mesh.RightMarsAtlasHemisphere;
                loadedLeft = null;
                loadedRight = null;
                loadedBoth = null;
                loadedSimplifiedLeft = null;
                loadedSimplifiedRight = null;
                loadedSimplifiedBoth = null;
                DisposeSurfaces(previousLeft, previousRight, previousBoth, previousSimplifiedLeft, previousSimplifiedRight, previousSimplifiedBoth);
            }
            finally
            {
                DisposeSurfaces(loadedLeft, loadedRight, loadedBoth, loadedSimplifiedLeft, loadedSimplifiedRight, loadedSimplifiedBoth);
                m_IsLoading = false;
            }
        }

        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public override void Clean()
        {
            base.Clean();
            m_Left?.Dispose();
            m_Right?.Dispose();
            m_SimplifiedLeft?.Dispose();
            m_SimplifiedRight?.Dispose();
            m_Left = null;
            m_Right = null;
            m_SimplifiedLeft = null;
            m_SimplifiedRight = null;
        }

        public override object Clone()
        {
            LeftRightMesh3D mesh = new()
            {
                Name = Name,
                Type = Type,
                Both = Both,
                SimplifiedBoth = SimplifiedBoth,
                Left = Left,
                Right = Right,
                SimplifiedLeft = SimplifiedLeft,
                SimplifiedRight = SimplifiedRight,
                m_Mesh = m_Mesh,
                m_LoadedLeftGiftiPath = m_LoadedLeftGiftiPath,
                m_LoadedRightGiftiPath = m_LoadedRightGiftiPath,
                m_LoadedTransformationPath = m_LoadedTransformationPath,
                m_LoadedLeftMarsAtlasPath = m_LoadedLeftMarsAtlasPath,
                m_LoadedRightMarsAtlasPath = m_LoadedRightMarsAtlasPath,
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mesh;
        }

        protected override async UniTask<Mesh3DInflatedRepresentation> CreateInflatedRepresentationAsync(SurfaceInflationCacheKey cacheKey, SurfaceInflationOptions options, IProgress<float> progress, CancellationToken cancellationToken)
        {
            SurfaceInflationResult leftResult = null;
            SurfaceInflationResult rightResult = null;
            DLL.Surface both = null;
            DLL.Surface simplifiedLeft = null;
            DLL.Surface simplifiedRight = null;
            DLL.Surface simplifiedBoth = null;
            try
            {
                leftResult = await InflateHemisphereAsync(m_Left, m_LoadedLeftGiftiPath, m_LoadedTransformationPath, m_LoadedLeftMarsAtlasPath, options, ScaleProgress(progress, 0.0f, 0.5f), cancellationToken);
                rightResult = await InflateHemisphereAsync(m_Right, m_LoadedRightGiftiPath, m_LoadedTransformationPath, m_LoadedRightMarsAtlasPath, options, ScaleProgress(progress, 0.5f, 0.5f), cancellationToken);

                both = (DLL.Surface)leftResult.Surface.Clone();
                both.Append(rightResult.Surface);
                both.ComputeNormals();
                simplifiedLeft = leftResult.Surface.Simplify();
                simplifiedRight = rightResult.Surface.Simplify();
                simplifiedBoth = both.Simplify();

                Mesh3DInflatedRepresentation representation = new(cacheKey, both, simplifiedBoth, null, leftResult.CoordinateSpace, leftResult.Surface, rightResult.Surface, simplifiedLeft, simplifiedRight, leftResult.Report, rightResult.Report);
                leftResult = null;
                rightResult = null;
                both = null;
                simplifiedLeft = null;
                simplifiedRight = null;
                simplifiedBoth = null;
                return representation;
            }
            finally
            {
                leftResult?.Surface.Dispose();
                rightResult?.Surface.Dispose();
                both?.Dispose();
                simplifiedLeft?.Dispose();
                simplifiedRight?.Dispose();
                simplifiedBoth?.Dispose();
            }
        }

        protected override DLL.Surface GetAnatomicalSurface(MeshPart part, bool simplified)
        {
            return part switch
            {
                MeshPart.Left => simplified ? m_SimplifiedLeft : m_Left,
                MeshPart.Right => simplified ? m_SimplifiedRight : m_Right,
                MeshPart.Both => simplified ? m_SimplifiedBoth : m_Both,
                _ => throw new ArgumentOutOfRangeException(nameof(part))
            };
        }

        protected override DLL.Surface GetInflatedSurface(MeshPart part, bool simplified)
        {
            Mesh3DInflatedRepresentation representation = ActiveInflatedRepresentation ?? throw new InvalidOperationException("No inflated representation has been generated.");
            return part switch
            {
                MeshPart.Left => simplified ? representation.SimplifiedLeft : representation.Left,
                MeshPart.Right => simplified ? representation.SimplifiedRight : representation.Right,
                MeshPart.Both => simplified ? representation.SimplifiedBoth : representation.Both,
                _ => throw new ArgumentOutOfRangeException(nameof(part))
            };
        }

        protected override string CreateSourceGeometryIdentity()
        {
            if (!string.IsNullOrWhiteSpace(m_LoadedLeftGiftiPath) && !string.IsNullOrWhiteSpace(m_LoadedRightGiftiPath))
            {
                return string.Join(";", CreateFileIdentity(m_LoadedLeftGiftiPath), CreateFileIdentity(m_LoadedRightGiftiPath), CreateFileIdentity(m_LoadedTransformationPath));
            }

            return string.Join(";", CreateSurfaceIdentity(m_Left), CreateSurfaceIdentity(m_Right));
        }

        private async UniTask<SurfaceInflationResult> InflateHemisphereAsync(DLL.Surface source, string giftiPath, string transformationPath, string marsAtlasPath, SurfaceInflationOptions options, IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(giftiPath))
                return await source.InflateAsync(options, progress, cancellationToken);

            SurfaceInflationResult result = await DLL.Surface.InflateGIIFileAsync(giftiPath, transformationPath, options, progress, cancellationToken);
            try
            {
                result.Surface.FlipTriangles();
                result.Surface.ComputeNormals();
                if (source.IsMarsAtlasLoaded && Object3DManager.MarsAtlas.Loaded)
                    result.Surface.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, marsAtlasPath);
                return result;
            }
            catch
            {
                result.Surface.Dispose();
                throw;
            }
        }

        #endregion
    }
}
