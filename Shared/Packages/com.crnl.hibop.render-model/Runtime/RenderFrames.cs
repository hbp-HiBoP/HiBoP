using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.RenderModel
{
    public enum TemporalApplication : byte
    {
        Unknown = 0,
        SampleAndHold = 1,
        Linear = 2,
    }

    [Flags]
    public enum SiteRenderFlags : byte
    {
        None = 0,
        Selected = 1,
        Highlighted = 2,
        Blacklisted = 4,
        Masked = 8,
        OutOfRoi = 16,
        Filtered = 32,
    }

    public readonly struct RenderTemporalSample : IEquatable<RenderTemporalSample>
    {
        public RenderTemporalSample(int index, float temporalAlpha)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!RenderMath.IsFinite(temporalAlpha) || temporalAlpha < 0f || temporalAlpha > 1f)
                throw new ArgumentOutOfRangeException(nameof(temporalAlpha));
            Index = index;
            TemporalAlpha = temporalAlpha;
        }

        public int Index { get; }
        public float TemporalAlpha { get; }

        public float EvaluateLinear(float lower, float upper)
        {
            return lower + (upper - lower) * TemporalAlpha;
        }

        public bool Equals(RenderTemporalSample other) => Index == other.Index && TemporalAlpha.Equals(other.TemporalAlpha);
        public override bool Equals(object obj) => obj is RenderTemporalSample other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Index, TemporalAlpha);
        public static bool operator ==(RenderTemporalSample left, RenderTemporalSample right) => left.Equals(right);
        public static bool operator !=(RenderTemporalSample left, RenderTemporalSample right) => !left.Equals(right);
    }

    public sealed class SurfaceFrame
    {
        public SurfaceFrame(AssetHash surfaceAssetHash, StateRevision sourceStateRevision, RenderTemporalSample sample, TemporalApplication temporalApplication, RenderBuffer<float> activityValues, RenderBuffer<float> opacityValues, RenderBuffer<byte> activeMask)
        {
            SurfaceAsset.EnsureHash(surfaceAssetHash, nameof(surfaceAssetHash));
            if (temporalApplication == TemporalApplication.Unknown)
                throw new ArgumentOutOfRangeException(nameof(temporalApplication));
            ActivityValues = activityValues ?? throw new ArgumentNullException(nameof(activityValues));
            OpacityValues = opacityValues ?? throw new ArgumentNullException(nameof(opacityValues));
            ActiveMask = activeMask ?? throw new ArgumentNullException(nameof(activeMask));
            if (ActivityValues.Count == 0 || ActivityValues.Count != OpacityValues.Count || ActivityValues.Count != ActiveMask.Count)
                throw new ArgumentException("Surface frame buffers must have the same non-zero vertex count.");
            for (int index = 0; index < ActivityValues.Count; index++)
            {
                if (!RenderMath.IsFinite(ActivityValues[index]) || !RenderMath.IsFinite(OpacityValues[index]))
                    throw new ArgumentOutOfRangeException(nameof(activityValues), "Surface frame values must be finite.");
                if (ActiveMask[index] > 1)
                    throw new ArgumentOutOfRangeException(nameof(activeMask), "Active mask values must be zero or one.");
            }

            SurfaceAssetHash = surfaceAssetHash;
            SourceStateRevision = sourceStateRevision;
            Sample = sample;
            TemporalApplication = temporalApplication;
        }

        public AssetHash SurfaceAssetHash { get; }
        public StateRevision SourceStateRevision { get; }
        public RenderTemporalSample Sample { get; }
        public TemporalApplication TemporalApplication { get; }
        public RenderBuffer<float> ActivityValues { get; }
        public RenderBuffer<float> OpacityValues { get; }
        public RenderBuffer<byte> ActiveMask { get; }
        public int VertexCount => ActivityValues.Count;
    }

    public sealed class SiteRenderFrame
    {
        public SiteRenderFrame(AssetHash siteAssetHash, StateRevision sourceStateRevision, RenderTemporalSample sample, TemporalApplication temporalApplication, RenderBuffer<Float3> positions, RenderBuffer<Rgba32> colors, RenderBuffer<float> sizes, RenderBuffer<byte> visibility, RenderBuffer<SiteRenderFlags> flags)
        {
            SurfaceAsset.EnsureHash(siteAssetHash, nameof(siteAssetHash));
            if (temporalApplication == TemporalApplication.Unknown)
                throw new ArgumentOutOfRangeException(nameof(temporalApplication));
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            Sizes = sizes ?? throw new ArgumentNullException(nameof(sizes));
            Visibility = visibility ?? throw new ArgumentNullException(nameof(visibility));
            Flags = flags ?? throw new ArgumentNullException(nameof(flags));
            if (Positions.Count == 0 || Positions.Count != Colors.Count || Colors.Count != Sizes.Count || Colors.Count != Visibility.Count || Colors.Count != Flags.Count)
                throw new ArgumentException("Site frame buffers must have the same non-zero count.");
            SurfaceAsset.ValidateFinite(Positions, nameof(positions));
            for (int index = 0; index < Colors.Count; index++)
            {
                if (!RenderMath.IsFinite(Sizes[index]) || Sizes[index] < 0f)
                    throw new ArgumentOutOfRangeException(nameof(sizes));
                if (Visibility[index] > 1)
                    throw new ArgumentOutOfRangeException(nameof(visibility));
            }

            SiteAssetHash = siteAssetHash;
            SourceStateRevision = sourceStateRevision;
            Sample = sample;
            TemporalApplication = temporalApplication;
        }

        public AssetHash SiteAssetHash { get; }
        public StateRevision SourceStateRevision { get; }
        public RenderTemporalSample Sample { get; }
        public TemporalApplication TemporalApplication { get; }
        public RenderBuffer<Float3> Positions { get; }
        public RenderBuffer<Rgba32> Colors { get; }
        public RenderBuffer<float> Sizes { get; }
        public RenderBuffer<byte> Visibility { get; }
        public RenderBuffer<SiteRenderFlags> Flags { get; }
        public int SiteCount => Colors.Count;
    }

    public sealed class CutOverlayFrame
    {
        public CutOverlayFrame(ContractId cutId, ContractId columnId, StateRevision sourceStateRevision, int width, int height, RenderTemporalSample sample, TemporalApplication temporalApplication, ScopeRevision mappingRevision, RenderBuffer<Rgba32> pixels)
        {
            if (!cutId.IsValid || !columnId.IsValid)
                throw new ArgumentException("Valid cut and column IDs are required.");
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (temporalApplication == TemporalApplication.Unknown)
                throw new ArgumentOutOfRangeException(nameof(temporalApplication));
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
            if (Pixels.Count != checked(width * height))
                throw new ArgumentException("Pixel count must match overlay dimensions.", nameof(pixels));
            CutId = cutId;
            ColumnId = columnId;
            SourceStateRevision = sourceStateRevision;
            Width = width;
            Height = height;
            Sample = sample;
            TemporalApplication = temporalApplication;
            MappingRevision = mappingRevision;
        }

        public ContractId CutId { get; }
        public ContractId ColumnId { get; }
        public StateRevision SourceStateRevision { get; }
        public int Width { get; }
        public int Height { get; }
        public RenderTemporalSample Sample { get; }
        public TemporalApplication TemporalApplication { get; }
        public ScopeRevision MappingRevision { get; }
        public RenderBuffer<Rgba32> Pixels { get; }
    }

    public sealed class CutRenderResult
    {
        private readonly ReadOnlyCollection<CutOverlayFrame> m_Overlays;

        public CutRenderResult(ContractId cutId, ContractId interactionId, InteractionSequence sequence, ScopeRevision cutRevision, ScopeRevision renderRevision, StateRevision sourceStateRevision, RenderTemporalSample sample, Plane3F plane, AssetHash geometryHash, Optional<CutGeometryAsset> geometry, AssetHash baseTextureHash, Optional<TextureAsset> baseTexture, IEnumerable<CutOverlayFrame> overlays)
        {
            if (!cutId.IsValid || !interactionId.IsValid)
                throw new ArgumentException("Valid cut and interaction IDs are required.");
            if (!sequence.IsValid)
                throw new ArgumentException("A valid interaction sequence is required.", nameof(sequence));
            SurfaceAsset.EnsureHash(geometryHash, nameof(geometryHash));
            SurfaceAsset.EnsureHash(baseTextureHash, nameof(baseTextureHash));
            if (geometry.HasValue && geometry.Value.Hash != geometryHash)
                throw new ArgumentException("Inline geometry must match its hash.", nameof(geometry));
            if (baseTexture.HasValue && baseTexture.Value.Hash != baseTextureHash)
                throw new ArgumentException("Inline base texture must match its hash.", nameof(baseTexture));
            if (overlays == null)
                throw new ArgumentNullException(nameof(overlays));
            List<CutOverlayFrame> overlayCopy = new(overlays);
            if (overlayCopy.Exists(overlay => overlay == null))
                throw new ArgumentException("Cut overlays cannot contain null.", nameof(overlays));
            HashSet<ContractId> columnIds = new();
            int width = overlayCopy.Count == 0 ? 0 : overlayCopy[0].Width;
            int height = overlayCopy.Count == 0 ? 0 : overlayCopy[0].Height;
            foreach (CutOverlayFrame overlay in overlayCopy)
            {
                if (overlay.CutId != cutId || overlay.SourceStateRevision != sourceStateRevision || overlay.Sample != sample)
                    throw new ArgumentException("Every overlay must match the cut, source state revision and temporal sample.", nameof(overlays));
                if (!columnIds.Add(overlay.ColumnId))
                    throw new ArgumentException("A cut result can contain at most one overlay per column.", nameof(overlays));
                if (overlay.Width != width || overlay.Height != height)
                    throw new ArgumentException("All overlays in a cut result must have identical dimensions.", nameof(overlays));
            }

            CutId = cutId;
            InteractionId = interactionId;
            Sequence = sequence;
            CutRevision = cutRevision;
            RenderRevision = renderRevision;
            SourceStateRevision = sourceStateRevision;
            Sample = sample;
            Plane = plane;
            GeometryHash = geometryHash;
            Geometry = geometry;
            BaseTextureHash = baseTextureHash;
            BaseTexture = baseTexture;
            m_Overlays = overlayCopy.AsReadOnly();
        }

        public ContractId CutId { get; }
        public ContractId InteractionId { get; }
        public InteractionSequence Sequence { get; }
        public ScopeRevision CutRevision { get; }
        public ScopeRevision RenderRevision { get; }
        public StateRevision SourceStateRevision { get; }
        public RenderTemporalSample Sample { get; }
        public Plane3F Plane { get; }
        public AssetHash GeometryHash { get; }
        public Optional<CutGeometryAsset> Geometry { get; }
        public AssetHash BaseTextureHash { get; }
        public Optional<TextureAsset> BaseTexture { get; }
        public IReadOnlyList<CutOverlayFrame> Overlays => m_Overlays;
    }

    public sealed class ColumnFrame
    {
        private readonly ReadOnlyCollection<CutOverlayFrame> m_CutOverlays;

        public ColumnFrame(ContractId columnId, AssetHash surfaceAssetHash, ScopeRevision visualParametersRevision, Optional<SurfaceFrame> surface, Optional<SiteRenderFrame> sites, IEnumerable<CutOverlayFrame> cutOverlays)
        {
            if (!columnId.IsValid)
                throw new ArgumentException("A valid column ID is required.", nameof(columnId));
            SurfaceAsset.EnsureHash(surfaceAssetHash, nameof(surfaceAssetHash));
            if (surface.HasValue && surface.Value.SurfaceAssetHash != surfaceAssetHash)
                throw new ArgumentException("Surface frame must reference the column surface asset.", nameof(surface));
            if (cutOverlays == null)
                throw new ArgumentNullException(nameof(cutOverlays));
            List<CutOverlayFrame> overlayCopy = new(cutOverlays);
            if (overlayCopy.Exists(overlay => overlay == null || overlay.ColumnId != columnId))
                throw new ArgumentException("Cut overlays must be non-null and belong to the column.", nameof(cutOverlays));
            HashSet<ContractId> cutIds = new();
            if (overlayCopy.Exists(overlay => !cutIds.Add(overlay.CutId)))
                throw new ArgumentException("A column frame can contain at most one overlay per cut.", nameof(cutOverlays));
            ColumnId = columnId;
            SurfaceAssetHash = surfaceAssetHash;
            VisualParametersRevision = visualParametersRevision;
            Surface = surface;
            Sites = sites;
            m_CutOverlays = overlayCopy.AsReadOnly();
        }

        public ContractId ColumnId { get; }
        public AssetHash SurfaceAssetHash { get; }
        public ScopeRevision VisualParametersRevision { get; }
        public Optional<SurfaceFrame> Surface { get; }
        public Optional<SiteRenderFrame> Sites { get; }
        public IReadOnlyList<CutOverlayFrame> CutOverlays => m_CutOverlays;
    }

    public sealed class DynamicFrameBundle
    {
        private readonly ReadOnlyCollection<ContractId> m_ExpectedColumnIds;
        private readonly ReadOnlyCollection<ColumnFrame> m_ColumnFrames;

        public DynamicFrameBundle(SessionEpoch session, ContractId timelineId, ScopeRevision playbackRevision, double logicalTime, RenderTemporalSample sample, StateRevision sourceStateRevision, IEnumerable<ContractId> expectedColumnIds, IEnumerable<ColumnFrame> columnFrames)
        {
            if (!session.IsValid || !timelineId.IsValid)
                throw new ArgumentException("A valid session and timeline ID are required.");
            if (!RenderMath.IsFinite(logicalTime))
                throw new ArgumentOutOfRangeException(nameof(logicalTime));
            if (expectedColumnIds == null || columnFrames == null)
                throw new ArgumentNullException(expectedColumnIds == null ? nameof(expectedColumnIds) : nameof(columnFrames));
            List<ContractId> expectedCopy = new(expectedColumnIds);
            List<ColumnFrame> frameCopy = new(columnFrames);
            if (expectedCopy.Count == 0 || expectedCopy.Count != frameCopy.Count)
                throw new ArgumentException("An atomic bundle must contain exactly one frame per expected column.");
            HashSet<ContractId> expectedSet = new();
            foreach (ContractId id in expectedCopy)
            {
                if (!id.IsValid || !expectedSet.Add(id))
                    throw new ArgumentException("Expected column IDs must be valid and unique.", nameof(expectedColumnIds));
            }

            HashSet<ContractId> actualSet = new();
            foreach (ColumnFrame frame in frameCopy)
            {
                if (frame == null || !expectedSet.Contains(frame.ColumnId) || !actualSet.Add(frame.ColumnId))
                    throw new ArgumentException("Column frames must match expected columns exactly once.", nameof(columnFrames));
                if (frame.Surface.HasValue && (frame.Surface.Value.Sample != sample || frame.Surface.Value.SourceStateRevision != sourceStateRevision))
                    throw new ArgumentException("Every surface frame must match the bundle sample and state revision.", nameof(columnFrames));
                if (frame.Sites.HasValue && (frame.Sites.Value.Sample != sample || frame.Sites.Value.SourceStateRevision != sourceStateRevision))
                    throw new ArgumentException("Every site frame must match the bundle sample and state revision.", nameof(columnFrames));
                foreach (CutOverlayFrame overlay in frame.CutOverlays)
                {
                    if (overlay.Sample != sample || overlay.SourceStateRevision != sourceStateRevision)
                        throw new ArgumentException("Every cut overlay must match the bundle sample and state revision.", nameof(columnFrames));
                }
            }

            Session = session;
            TimelineId = timelineId;
            PlaybackRevision = playbackRevision;
            LogicalTime = logicalTime;
            Sample = sample;
            SourceStateRevision = sourceStateRevision;
            m_ExpectedColumnIds = expectedCopy.AsReadOnly();
            m_ColumnFrames = frameCopy.AsReadOnly();
        }

        public SessionEpoch Session { get; }
        public ContractId TimelineId { get; }
        public ScopeRevision PlaybackRevision { get; }
        public double LogicalTime { get; }
        public RenderTemporalSample Sample { get; }
        public StateRevision SourceStateRevision { get; }
        public IReadOnlyList<ContractId> ExpectedColumnIds => m_ExpectedColumnIds;
        public IReadOnlyList<ColumnFrame> ColumnFrames => m_ColumnFrames;
    }
}
