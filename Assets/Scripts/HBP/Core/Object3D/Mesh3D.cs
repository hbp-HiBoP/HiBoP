using System;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Core.Object3D
{
    public enum RuntimeMeshOrigin
    {
        GeneratedFromMRI
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
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_Both;
            }
            protected set
            {
                m_Both = value;
            }
        }

        protected DLL.Surface m_SimplifiedBoth;
        /// <summary>
        /// DLL surface containing data for the whole simplified brain mesh
        /// </summary>
        public DLL.Surface SimplifiedBoth
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_SimplifiedBoth;
            }
            protected set
            {
                m_SimplifiedBoth = value;
            }
        }

        /// <summary>
        /// Is the 3D mesh completely loaded ?
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return m_Both != null ? m_Both.IsLoaded : false;
            }
        }
        /// <summary>
        /// Is mars atlas loaded for this mesh ?
        /// </summary>
        public bool IsMarsAtlasLoaded
        {
            get
            {
                return m_Both != null ? m_Both.IsMarsAtlasLoaded : false;
            }
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
        /// Is the mesh currently loading ?
        /// </summary>
        protected bool m_IsLoading = false;
        /// <summary>
        /// Does the mesh have been loaded outside of a scene and copied to the scene (e.g. MNI objects) ?
        /// </summary>
        public bool HasBeenLoadedOutside { get; protected set; }
        /// <summary>
        /// Data of the mesh (paths etc.)
        /// </summary>
        protected Data.BaseMesh m_Mesh;
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
        public Mesh3D() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public abstract void Load();
        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public virtual void Clean()
        {
            m_Both?.Dispose();
            m_SimplifiedBoth?.Dispose();
        }
        public abstract object Clone();
        #endregion
    }

    /// <summary>
    /// Subclass of <see cref="Mesh3D"/> that contains data for a mesh in one piece
    /// </summary>
    public class SingleMesh3D : Mesh3D
    {
        #region Constructors
        public SingleMesh3D(Data.SingleMesh mesh, MeshType type, bool load) : base(mesh, type, load) { }
        public SingleMesh3D() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public override void Load()
        {
            m_IsLoading = true;
            Data.SingleMesh mesh = m_Mesh as Data.SingleMesh;

            m_Both = new DLL.Surface();
            if (m_Both.LoadGIIFile(mesh.Path, mesh.Transformation))
            {
                m_Both.FlipTriangles();
                m_Both.ComputeNormals();
                if (Object3DManager.MarsAtlas.Loaded)
                    m_Both.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, mesh.MarsAtlasPath);
                SimplifiedBoth = m_Both.Simplify();
            }
            m_IsLoading = false;
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
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mesh;
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

        public RuntimeSingleMesh3D(
            MRI3D sourceMRI,
            DLL.Surface surface,
            DLL.PreviewSurfaceReport generationReport,
            DLL.Surface simplifiedSurface = null)
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
            return surface.NumberOfTriangles > 10000
                ? surface.Simplify()
                : (DLL.Surface)surface.Clone();
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

        internal RuntimePreviewDistanceReport(
            int siteCount,
            int sitesBeyondInfluence,
            float influenceDistance,
            float percentile50,
            float percentile90,
            float percentile95)
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
        public static RuntimePreviewDistanceReport Evaluate(
            Vector3[] surfaceVertices,
            Vector3[] sitePositions,
            float influenceDistance)
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
            return new RuntimePreviewDistanceReport(
                distances.Length,
                sitesBeyondInfluence,
                influenceDistance,
                Percentile(distances, 0.50f),
                Percentile(distances, 0.90f),
                Percentile(distances, 0.95f));
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
        #region Properties
        protected DLL.Surface m_Left;
        /// <summary>
        /// DLL surface containing data for the left part of the mesh
        /// </summary>
        public DLL.Surface Left
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_Left;
            }
            protected set
            {
                m_Left = value;
            }
        }

        protected DLL.Surface m_Right;
        /// <summary>
        /// DLL surface containing data for the right part of the mesh
        /// </summary>
        public DLL.Surface Right
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_Right;
            }
            protected set
            {
                m_Right = value;
            }
        }

        protected DLL.Surface m_SimplifiedLeft;
        /// <summary>
        /// DLL surface containing data for the left simplified part of the mesh
        /// </summary>
        public DLL.Surface SimplifiedLeft
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_SimplifiedLeft;
            }
            protected set
            {
                m_SimplifiedLeft = value;
            }
        }

        protected DLL.Surface m_SimplifiedRight;
        /// <summary>
        /// DLL surface containing data for the right simplified part of the mesh
        /// </summary>
        public DLL.Surface SimplifiedRight
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_SimplifiedRight;
            }
            protected set
            {
                m_SimplifiedRight = value;
            }
        }
        #endregion

        #region Constructors
        public LeftRightMesh3D(Data.LeftRightMesh mesh, MeshType type, bool load) : base(mesh, type, load) { }
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
        public LeftRightMesh3D() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public override void Load()
        {
            m_IsLoading = true;
            Data.LeftRightMesh mesh = m_Mesh as Data.LeftRightMesh;
            m_Left = new DLL.Surface();
            if (m_Left.LoadGIIFile(mesh.LeftHemisphere, mesh.Transformation))
            {
                m_Left.FlipTriangles();
                m_Left.ComputeNormals();
                if (Object3DManager.MarsAtlas.Loaded)
                    m_Left.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, mesh.LeftMarsAtlasHemisphere);
                SimplifiedLeft = m_Left.Simplify();
            }

            m_Right = new DLL.Surface();
            if (m_Right.LoadGIIFile(mesh.RightHemisphere, mesh.Transformation))
            {
                m_Right.FlipTriangles();
                m_Right.ComputeNormals();
                if (Object3DManager.MarsAtlas.Loaded)
                    m_Right.SearchMarsParcelFileAndUpdateColors(Object3DManager.MarsAtlas, mesh.RightMarsAtlasHemisphere);
                SimplifiedRight = m_Right.Simplify();
            }

            if (m_Left.IsLoaded && m_Right.IsLoaded)
            {
                m_Both = (DLL.Surface)m_Left.Clone();
                m_Both.Append(m_Right);
                SimplifiedBoth = m_Both.Simplify();
            }
            m_IsLoading = false;
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
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mesh;
        }
        #endregion
    }
}
