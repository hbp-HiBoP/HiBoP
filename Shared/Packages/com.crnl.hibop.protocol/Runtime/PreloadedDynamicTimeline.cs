using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.Protocol
{
    public readonly struct PreloadedTimelineIndex
    {
        public PreloadedTimelineIndex(double logicalTime, RenderTemporalSample sample)
        {
            if (double.IsNaN(logicalTime) || double.IsInfinity(logicalTime))
                throw new ArgumentOutOfRangeException(nameof(logicalTime));
            LogicalTime = logicalTime;
            Sample = sample;
        }

        public double LogicalTime { get; }
        public RenderTemporalSample Sample { get; }
    }

    public sealed class PreloadedTimelineChannel<T> where T : struct
    {
        private readonly ReadOnlyCollection<RenderBuffer<T>> m_UniqueSlices;
        private readonly int[] m_SliceIndexByTimelineIndex;

        internal PreloadedTimelineChannel(int elementCount, int elementByteSize, IReadOnlyList<RenderBuffer<T>> uniqueSlices, int[] sliceIndexByTimelineIndex)
        {
            if (elementCount <= 0 || elementByteSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            if (uniqueSlices == null || uniqueSlices.Count == 0)
                throw new ArgumentException("At least one unique timeline slice is required.", nameof(uniqueSlices));
            if (sliceIndexByTimelineIndex == null || sliceIndexByTimelineIndex.Length == 0)
                throw new ArgumentException("At least one timeline index is required.", nameof(sliceIndexByTimelineIndex));

            var slices = new List<RenderBuffer<T>>(uniqueSlices.Count);
            for (int index = 0; index < uniqueSlices.Count; index++)
            {
                RenderBuffer<T> slice = uniqueSlices[index] ?? throw new ArgumentException("Timeline slices cannot contain null.", nameof(uniqueSlices));
                if (slice.Count != elementCount)
                    throw new ArgumentException("Every unique slice must have the declared element count.", nameof(uniqueSlices));
                slices.Add(slice);
            }

            m_SliceIndexByTimelineIndex = (int[])sliceIndexByTimelineIndex.Clone();
            for (int index = 0; index < m_SliceIndexByTimelineIndex.Length; index++)
            {
                if (m_SliceIndexByTimelineIndex[index] < 0 || m_SliceIndexByTimelineIndex[index] >= slices.Count)
                    throw new ArgumentOutOfRangeException(nameof(sliceIndexByTimelineIndex));
            }

            ElementCount = elementCount;
            ElementByteSize = elementByteSize;
            m_UniqueSlices = slices.AsReadOnly();
        }

        public int ElementCount { get; }
        public int ElementByteSize { get; }
        public int IndexCount => m_SliceIndexByTimelineIndex.Length;
        public int UniqueSliceCount => m_UniqueSlices.Count;
        public long NaiveByteLength => checked((long)ElementCount * ElementByteSize * IndexCount);
        public long UniqueByteLength => checked((long)ElementCount * ElementByteSize * UniqueSliceCount);
        public IReadOnlyList<RenderBuffer<T>> UniqueSlices => m_UniqueSlices;

        public int GetSliceIndex(int timelineIndex)
        {
            ValidateTimelineIndex(timelineIndex);
            return m_SliceIndexByTimelineIndex[timelineIndex];
        }

        public RenderBuffer<T> GetSlice(int timelineIndex)
        {
            return m_UniqueSlices[GetSliceIndex(timelineIndex)];
        }

        internal int[] CopySliceIndices() => (int[])m_SliceIndexByTimelineIndex.Clone();

        private void ValidateTimelineIndex(int timelineIndex)
        {
            if (timelineIndex < 0 || timelineIndex >= IndexCount)
                throw new ArgumentOutOfRangeException(nameof(timelineIndex));
        }
    }

    public sealed class PreloadedCutTimeline
    {
        internal PreloadedCutTimeline(ContractId cutId, int width, int height, ScopeRevision mappingRevision, PreloadedTimelineChannel<Rgba32> pixels)
        {
            if (!cutId.IsValid)
                throw new ArgumentException("A valid cut ID is required.", nameof(cutId));
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
            if (pixels.ElementCount != checked(width * height))
                throw new ArgumentException("Cut pixel count must match its dimensions.", nameof(pixels));
            CutId = cutId;
            Width = width;
            Height = height;
            MappingRevision = mappingRevision;
        }

        public ContractId CutId { get; }
        public int Width { get; }
        public int Height { get; }
        public ScopeRevision MappingRevision { get; }
        public PreloadedTimelineChannel<Rgba32> Pixels { get; }
    }

    public sealed class PreloadedColumnTimeline
    {
        private readonly ReadOnlyCollection<ContractId> m_CutIds;
        private readonly ReadOnlyCollection<PreloadedCutTimeline> m_Cuts;

        internal PreloadedColumnTimeline(ContractId columnId, DynamicColumnContent content, AssetHash surfaceAssetHash, ScopeRevision visualParametersRevision, AssetHash siteAssetHash, IReadOnlyList<ContractId> cutIds, PreloadedTimelineChannel<float> surfaceActivity, PreloadedTimelineChannel<float> surfaceOpacity, PreloadedTimelineChannel<byte> surfaceMask, PreloadedTimelineChannel<Float3> sitePositions, PreloadedTimelineChannel<Rgba32> siteColors, PreloadedTimelineChannel<float> siteSizes, PreloadedTimelineChannel<byte> siteVisibility, PreloadedTimelineChannel<SiteRenderFlags> siteFlags, IReadOnlyList<PreloadedCutTimeline> cuts)
        {
            if (!columnId.IsValid)
                throw new ArgumentException("A valid column ID is required.", nameof(columnId));
            if ((content & ~(DynamicColumnContent.Surface | DynamicColumnContent.Sites)) != 0)
                throw new ArgumentOutOfRangeException(nameof(content));
            if (cutIds == null || cuts == null || cutIds.Count != cuts.Count)
                throw new ArgumentException("Cut IDs and cut timelines must match.");

            bool hasSurface = (content & DynamicColumnContent.Surface) != 0;
            bool hasSites = (content & DynamicColumnContent.Sites) != 0;
            if (hasSurface != (surfaceActivity != null && surfaceOpacity != null && surfaceMask != null))
                throw new ArgumentException("Surface channels must match the column manifest.");
            if (hasSites != (sitePositions != null && siteColors != null && siteSizes != null && siteVisibility != null && siteFlags != null))
                throw new ArgumentException("Site channels must match the column manifest.");

            var cutIdCopy = new List<ContractId>(cutIds.Count);
            var cutCopy = new List<PreloadedCutTimeline>(cuts.Count);
            var uniqueCuts = new HashSet<ContractId>();
            for (int index = 0; index < cutIds.Count; index++)
            {
                ContractId cutId = cutIds[index];
                PreloadedCutTimeline cut = cuts[index] ?? throw new ArgumentException("Cut timelines cannot contain null.", nameof(cuts));
                if (!cutId.IsValid || cut.CutId != cutId || !uniqueCuts.Add(cutId))
                    throw new ArgumentException("Cut timelines must match valid, unique manifest IDs.", nameof(cuts));
                cutIdCopy.Add(cutId);
                cutCopy.Add(cut);
            }

            ColumnId = columnId;
            Content = content;
            SurfaceAssetHash = surfaceAssetHash;
            VisualParametersRevision = visualParametersRevision;
            SiteAssetHash = siteAssetHash;
            SurfaceActivity = surfaceActivity;
            SurfaceOpacity = surfaceOpacity;
            SurfaceMask = surfaceMask;
            SitePositions = sitePositions;
            SiteColors = siteColors;
            SiteSizes = siteSizes;
            SiteVisibility = siteVisibility;
            SiteFlags = siteFlags;
            m_CutIds = cutIdCopy.AsReadOnly();
            m_Cuts = cutCopy.AsReadOnly();
        }

        public ContractId ColumnId { get; }
        public DynamicColumnContent Content { get; }
        public AssetHash SurfaceAssetHash { get; }
        public ScopeRevision VisualParametersRevision { get; }
        public AssetHash SiteAssetHash { get; }
        public IReadOnlyList<ContractId> CutIds => m_CutIds;
        public PreloadedTimelineChannel<float> SurfaceActivity { get; }
        public PreloadedTimelineChannel<float> SurfaceOpacity { get; }
        public PreloadedTimelineChannel<byte> SurfaceMask { get; }
        public PreloadedTimelineChannel<Float3> SitePositions { get; }
        public PreloadedTimelineChannel<Rgba32> SiteColors { get; }
        public PreloadedTimelineChannel<float> SiteSizes { get; }
        public PreloadedTimelineChannel<byte> SiteVisibility { get; }
        public PreloadedTimelineChannel<SiteRenderFlags> SiteFlags { get; }
        public IReadOnlyList<PreloadedCutTimeline> Cuts => m_Cuts;
    }

    public sealed class PreloadedDynamicTimeline
    {
        private readonly ReadOnlyCollection<PreloadedTimelineIndex> m_Indices;
        private readonly ReadOnlyCollection<PreloadedColumnTimeline> m_Columns;

        internal PreloadedDynamicTimeline(SessionEpoch session, ContractId timelineId, StateRevision sourceStateRevision, IReadOnlyList<PreloadedTimelineIndex> indices, IReadOnlyList<PreloadedColumnTimeline> columns)
        {
            if (!session.IsValid || !timelineId.IsValid)
                throw new ArgumentException("A valid session and timeline ID are required.");
            if (indices == null || indices.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(indices), "At least one timeline index is required.");
            if (columns == null || columns.Count == 0)
                throw new ArgumentException("At least one column is required.", nameof(columns));

            var indexCopy = new List<PreloadedTimelineIndex>(indices);
            var columnCopy = new List<PreloadedColumnTimeline>(columns.Count);
            var columnIds = new HashSet<ContractId>();
            for (int index = 0; index < columns.Count; index++)
            {
                PreloadedColumnTimeline column = columns[index] ?? throw new ArgumentException("Columns cannot contain null.", nameof(columns));
                if (!columnIds.Add(column.ColumnId))
                    throw new ArgumentException("Column IDs must be unique.", nameof(columns));
                ValidateChannelLengths(column, indexCopy.Count);
                columnCopy.Add(column);
            }

            Session = session;
            TimelineId = timelineId;
            SourceStateRevision = sourceStateRevision;
            m_Indices = indexCopy.AsReadOnly();
            m_Columns = columnCopy.AsReadOnly();
            NaivePayloadBytes = CalculateBytes(columnCopy, false);
            UniquePayloadBytes = CalculateBytes(columnCopy, true);
        }

        public SessionEpoch Session { get; }
        public ContractId TimelineId { get; }
        public StateRevision SourceStateRevision { get; }
        public int IndexCount => m_Indices.Count;
        public IReadOnlyList<PreloadedTimelineIndex> Indices => m_Indices;
        public IReadOnlyList<PreloadedColumnTimeline> Columns => m_Columns;
        public long NaivePayloadBytes { get; }
        public long UniquePayloadBytes { get; }

        private static void ValidateChannelLengths(PreloadedColumnTimeline column, int indexCount)
        {
            void Validate<T>(PreloadedTimelineChannel<T> channel) where T : struct
            {
                if (channel != null && channel.IndexCount != indexCount)
                    throw new ArgumentException("Every timeline channel must cover every index.");
            }

            Validate(column.SurfaceActivity);
            Validate(column.SurfaceOpacity);
            Validate(column.SurfaceMask);
            Validate(column.SitePositions);
            Validate(column.SiteColors);
            Validate(column.SiteSizes);
            Validate(column.SiteVisibility);
            Validate(column.SiteFlags);
            for (int index = 0; index < column.Cuts.Count; index++)
                Validate(column.Cuts[index].Pixels);
        }

        private static long CalculateBytes(IReadOnlyList<PreloadedColumnTimeline> columns, bool unique)
        {
            long bytes = 0;

            void Add<T>(PreloadedTimelineChannel<T> channel) where T : struct
            {
                if (channel != null)
                    bytes = checked(bytes + (unique ? channel.UniqueByteLength : channel.NaiveByteLength));
            }

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                PreloadedColumnTimeline column = columns[columnIndex];
                Add(column.SurfaceActivity);
                Add(column.SurfaceOpacity);
                Add(column.SurfaceMask);
                Add(column.SitePositions);
                Add(column.SiteColors);
                Add(column.SiteSizes);
                Add(column.SiteVisibility);
                Add(column.SiteFlags);
                for (int cutIndex = 0; cutIndex < column.Cuts.Count; cutIndex++)
                    Add(column.Cuts[cutIndex].Pixels);
            }

            return bytes;
        }
    }

    public sealed class PreloadedDynamicTimelineBuilder
    {
        private readonly long m_MaximumUniquePayloadBytes;
        private readonly List<PreloadedTimelineIndex> m_Indices = new();
        private SessionEpoch m_Session;
        private ContractId m_TimelineId;
        private StateRevision m_SourceStateRevision;
        private ColumnAccumulator[] m_Columns;
        private bool m_Built;
        private bool m_Faulted;

        public PreloadedDynamicTimelineBuilder(long maximumUniquePayloadBytes)
        {
            if (maximumUniquePayloadBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumUniquePayloadBytes));
            m_MaximumUniquePayloadBytes = maximumUniquePayloadBytes;
        }

        public int IndexCount => m_Indices.Count;
        public long MaximumUniquePayloadBytes => m_MaximumUniquePayloadBytes;

        public void AddFrame(DynamicFrameBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (m_Built)
                throw new InvalidOperationException("The timeline builder has already produced its archive.");
            if (m_Faulted)
                throw new InvalidOperationException("The timeline builder is faulted and cannot produce a partial archive.");
            try
            {
                bool firstFrame = m_Indices.Count == 0;
                if (firstFrame)
                    Initialize(bundle);
                else
                    ValidateBundleIdentity(bundle);

                if (!firstFrame)
                {
                    for (int index = 0; index < m_Columns.Length; index++)
                        m_Columns[index].Add(FindFrame(bundle, m_Columns[index].ColumnId));
                }

                long uniquePayloadBytes = CalculateUniquePayloadBytes();
                if (uniquePayloadBytes > m_MaximumUniquePayloadBytes)
                    throw new NotSupportedException($"Timeline unique payload requires {uniquePayloadBytes} bytes, exceeding the explicit {m_MaximumUniquePayloadBytes}-byte memory budget.");
                m_Indices.Add(new PreloadedTimelineIndex(bundle.LogicalTime, bundle.Sample));
            }
            catch
            {
                m_Faulted = true;
                throw;
            }
        }

        public PreloadedDynamicTimeline Build()
        {
            if (m_Built)
                throw new InvalidOperationException("The timeline builder can build only once.");
            if (m_Faulted)
                throw new InvalidOperationException("The timeline builder is faulted and cannot produce a partial archive.");
            if (m_Indices.Count == 0)
                throw new InvalidOperationException("At least one frame is required.");
            m_Built = true;

            var columns = new PreloadedColumnTimeline[m_Columns.Length];
            for (int index = 0; index < columns.Length; index++)
                columns[index] = m_Columns[index].Build();
            return new PreloadedDynamicTimeline(m_Session, m_TimelineId, m_SourceStateRevision, m_Indices, columns);
        }

        private void Initialize(DynamicFrameBundle bundle)
        {
            m_Session = bundle.Session;
            m_TimelineId = bundle.TimelineId;
            m_SourceStateRevision = bundle.SourceStateRevision;
            m_Columns = new ColumnAccumulator[bundle.Expectations.Count];
            for (int index = 0; index < bundle.Expectations.Count; index++)
            {
                DynamicColumnExpectation expectation = bundle.Expectations[index];
                m_Columns[index] = new ColumnAccumulator(expectation, FindFrame(bundle, expectation.ColumnId));
            }
        }

        private void ValidateBundleIdentity(DynamicFrameBundle bundle)
        {
            if (bundle.Session != m_Session || bundle.TimelineId != m_TimelineId || bundle.SourceStateRevision != m_SourceStateRevision)
                throw new ArgumentException("Every preloaded frame must belong to the same immutable session/timeline state.", nameof(bundle));
            if (bundle.Expectations.Count != m_Columns.Length)
                throw new ArgumentException("Every preloaded frame must have the same complete column manifest.", nameof(bundle));
            for (int index = 0; index < m_Columns.Length; index++)
            {
                DynamicColumnExpectation expectation = bundle.Expectations[index];
                if (!m_Columns[index].Matches(expectation))
                    throw new ArgumentException("Every preloaded frame must have the same ordered column and cut manifest.", nameof(bundle));
            }
        }

        private long CalculateUniquePayloadBytes()
        {
            long bytes = 0;
            for (int index = 0; index < m_Columns.Length; index++)
                bytes = checked(bytes + m_Columns[index].UniquePayloadBytes);
            return bytes;
        }

        private static ColumnFrame FindFrame(DynamicFrameBundle bundle, ContractId columnId)
        {
            for (int index = 0; index < bundle.ColumnFrames.Count; index++)
            {
                if (bundle.ColumnFrames[index].ColumnId == columnId)
                    return bundle.ColumnFrames[index];
            }

            throw new ArgumentException("A preloaded frame is missing a manifest column.", nameof(bundle));
        }

        private sealed class ColumnAccumulator
        {
            private readonly DynamicColumnContent m_Content;
            private readonly ContractId[] m_CutIds;
            private readonly ChannelAccumulator<float> m_SurfaceActivity;
            private readonly ChannelAccumulator<float> m_SurfaceOpacity;
            private readonly ChannelAccumulator<byte> m_SurfaceMask;
            private readonly ChannelAccumulator<Float3> m_SitePositions;
            private readonly ChannelAccumulator<Rgba32> m_SiteColors;
            private readonly ChannelAccumulator<float> m_SiteSizes;
            private readonly ChannelAccumulator<byte> m_SiteVisibility;
            private readonly ChannelAccumulator<SiteRenderFlags> m_SiteFlags;
            private readonly CutAccumulator[] m_Cuts;
            private readonly AssetHash m_SurfaceAssetHash;
            private readonly ScopeRevision m_VisualParametersRevision;
            private readonly AssetHash m_SiteAssetHash;

            public ColumnAccumulator(DynamicColumnExpectation expectation, ColumnFrame first)
            {
                ColumnId = expectation.ColumnId;
                m_Content = expectation.Content;
                m_CutIds = new ContractId[expectation.CutIds.Count];
                for (int index = 0; index < m_CutIds.Length; index++)
                    m_CutIds[index] = expectation.CutIds[index];
                m_SurfaceAssetHash = first.SurfaceAssetHash;
                m_VisualParametersRevision = first.VisualParametersRevision;

                if ((m_Content & DynamicColumnContent.Surface) != 0)
                {
                    SurfaceFrame surface = first.Surface.Value;
                    m_SurfaceActivity = new ChannelAccumulator<float>(surface.ActivityValues, sizeof(float));
                    m_SurfaceOpacity = new ChannelAccumulator<float>(surface.OpacityValues, sizeof(float));
                    m_SurfaceMask = new ChannelAccumulator<byte>(surface.ActiveMask, sizeof(byte));
                }

                if ((m_Content & DynamicColumnContent.Sites) != 0)
                {
                    SiteRenderFrame sites = first.Sites.Value;
                    m_SiteAssetHash = sites.SiteAssetHash;
                    m_SitePositions = new ChannelAccumulator<Float3>(sites.Positions, 12);
                    m_SiteColors = new ChannelAccumulator<Rgba32>(sites.Colors, 4);
                    m_SiteSizes = new ChannelAccumulator<float>(sites.Sizes, sizeof(float));
                    m_SiteVisibility = new ChannelAccumulator<byte>(sites.Visibility, sizeof(byte));
                    m_SiteFlags = new ChannelAccumulator<SiteRenderFlags>(sites.Flags, sizeof(byte));
                }

                m_Cuts = new CutAccumulator[m_CutIds.Length];
                for (int index = 0; index < m_Cuts.Length; index++)
                    m_Cuts[index] = new CutAccumulator(FindOverlay(first, m_CutIds[index]));
            }

            public ContractId ColumnId { get; }

            public long UniquePayloadBytes
            {
                get
                {
                    long bytes = 0;
                    bytes = AddUniqueBytes(bytes, m_SurfaceActivity);
                    bytes = AddUniqueBytes(bytes, m_SurfaceOpacity);
                    bytes = AddUniqueBytes(bytes, m_SurfaceMask);
                    bytes = AddUniqueBytes(bytes, m_SitePositions);
                    bytes = AddUniqueBytes(bytes, m_SiteColors);
                    bytes = AddUniqueBytes(bytes, m_SiteSizes);
                    bytes = AddUniqueBytes(bytes, m_SiteVisibility);
                    bytes = AddUniqueBytes(bytes, m_SiteFlags);
                    for (int index = 0; index < m_Cuts.Length; index++)
                        bytes = checked(bytes + m_Cuts[index].UniqueByteLength);
                    return bytes;
                }
            }

            public bool Matches(DynamicColumnExpectation expectation)
            {
                if (expectation.ColumnId != ColumnId || expectation.Content != m_Content || expectation.CutIds.Count != m_CutIds.Length)
                    return false;
                for (int index = 0; index < m_CutIds.Length; index++)
                {
                    if (expectation.CutIds[index] != m_CutIds[index])
                        return false;
                }

                return true;
            }

            public void Add(ColumnFrame frame)
            {
                if (frame.SurfaceAssetHash != m_SurfaceAssetHash || frame.VisualParametersRevision != m_VisualParametersRevision)
                    throw new ArgumentException("Asset and visual parameter revisions must remain stable inside a preloaded timeline.", nameof(frame));
                if ((m_Content & DynamicColumnContent.Surface) != 0)
                {
                    m_SurfaceActivity.Add(frame.Surface.Value.ActivityValues);
                    m_SurfaceOpacity.Add(frame.Surface.Value.OpacityValues);
                    m_SurfaceMask.Add(frame.Surface.Value.ActiveMask);
                }

                if ((m_Content & DynamicColumnContent.Sites) != 0)
                {
                    SiteRenderFrame sites = frame.Sites.Value;
                    if (sites.SiteAssetHash != m_SiteAssetHash)
                        throw new ArgumentException("The site asset must remain stable inside a preloaded timeline.", nameof(frame));
                    m_SitePositions.Add(sites.Positions);
                    m_SiteColors.Add(sites.Colors);
                    m_SiteSizes.Add(sites.Sizes);
                    m_SiteVisibility.Add(sites.Visibility);
                    m_SiteFlags.Add(sites.Flags);
                }

                for (int index = 0; index < m_Cuts.Length; index++)
                    m_Cuts[index].Add(FindOverlay(frame, m_CutIds[index]));
            }

            public PreloadedColumnTimeline Build()
            {
                var cuts = new PreloadedCutTimeline[m_Cuts.Length];
                for (int index = 0; index < cuts.Length; index++)
                    cuts[index] = m_Cuts[index].Build();
                return new PreloadedColumnTimeline(ColumnId, m_Content, m_SurfaceAssetHash, m_VisualParametersRevision, m_SiteAssetHash, m_CutIds, m_SurfaceActivity?.Build(), m_SurfaceOpacity?.Build(), m_SurfaceMask?.Build(), m_SitePositions?.Build(), m_SiteColors?.Build(), m_SiteSizes?.Build(), m_SiteVisibility?.Build(), m_SiteFlags?.Build(), cuts);
            }

            private static CutOverlayFrame FindOverlay(ColumnFrame frame, ContractId cutId)
            {
                for (int index = 0; index < frame.CutOverlays.Count; index++)
                {
                    if (frame.CutOverlays[index].CutId == cutId)
                        return frame.CutOverlays[index];
                }

                throw new ArgumentException("A preloaded frame is missing a manifest cut.", nameof(frame));
            }

            private static long AddUniqueBytes<T>(long bytes, ChannelAccumulator<T> channel) where T : struct
            {
                return channel == null ? bytes : checked(bytes + channel.UniqueByteLength);
            }
        }

        private sealed class CutAccumulator
        {
            private readonly ContractId m_CutId;
            private readonly int m_Width;
            private readonly int m_Height;
            private readonly ScopeRevision m_MappingRevision;
            private readonly ChannelAccumulator<Rgba32> m_Pixels;

            public CutAccumulator(CutOverlayFrame first)
            {
                m_CutId = first.CutId;
                m_Width = first.Width;
                m_Height = first.Height;
                m_MappingRevision = first.MappingRevision;
                m_Pixels = new ChannelAccumulator<Rgba32>(first.Pixels, 4);
            }

            public void Add(CutOverlayFrame overlay)
            {
                if (overlay.CutId != m_CutId || overlay.Width != m_Width || overlay.Height != m_Height || overlay.MappingRevision != m_MappingRevision)
                    throw new ArgumentException("Cut identity, dimensions and mapping must remain stable inside a preloaded timeline.", nameof(overlay));
                m_Pixels.Add(overlay.Pixels);
            }

            public long UniqueByteLength => m_Pixels.UniqueByteLength;

            public PreloadedCutTimeline Build() => new(m_CutId, m_Width, m_Height, m_MappingRevision, m_Pixels.Build());
        }

        private sealed class ChannelAccumulator<T> where T : struct
        {
            private readonly int m_ElementByteSize;
            private readonly int m_ElementCount;
            private readonly Dictionary<ulong, List<int>> m_UniqueIndicesByHash = new();
            private readonly List<RenderBuffer<T>> m_UniqueSlices = new();
            private readonly List<int> m_SliceIndices = new();

            public ChannelAccumulator(RenderBuffer<T> first, int elementByteSize)
            {
                m_ElementByteSize = elementByteSize;
                m_ElementCount = first?.Count ?? throw new ArgumentNullException(nameof(first));
                if (m_ElementCount == 0)
                    throw new ArgumentException("Timeline channels cannot be empty.", nameof(first));
                Add(first);
            }

            public void Add(RenderBuffer<T> slice)
            {
                if (slice == null || slice.Count != m_ElementCount)
                    throw new ArgumentException("Every timeline slice must have the same non-zero element count.", nameof(slice));
                ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(slice.AsReadOnlySpan());
                ulong hash = Hash(bytes);
                if (m_UniqueIndicesByHash.TryGetValue(hash, out List<int> candidates))
                {
                    for (int index = 0; index < candidates.Count; index++)
                    {
                        int candidate = candidates[index];
                        if (bytes.SequenceEqual(MemoryMarshal.AsBytes(m_UniqueSlices[candidate].AsReadOnlySpan())))
                        {
                            m_SliceIndices.Add(candidate);
                            return;
                        }
                    }
                }
                else
                {
                    candidates = new List<int>();
                    m_UniqueIndicesByHash.Add(hash, candidates);
                }

                int uniqueIndex = m_UniqueSlices.Count;
                m_UniqueSlices.Add(slice);
                candidates.Add(uniqueIndex);
                m_SliceIndices.Add(uniqueIndex);
            }

            public long UniqueByteLength => checked((long)m_ElementCount * m_ElementByteSize * m_UniqueSlices.Count);

            public PreloadedTimelineChannel<T> Build() => new(m_ElementCount, m_ElementByteSize, m_UniqueSlices, m_SliceIndices.ToArray());

            private static ulong Hash(ReadOnlySpan<byte> bytes)
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;
                ulong result = offset;
                for (int index = 0; index < bytes.Length; index++)
                    result = (result ^ bytes[index]) * prime;
                return result;
            }
        }
    }
}
