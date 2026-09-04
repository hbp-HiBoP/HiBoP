using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using UnityEngine;

namespace CRNL.HiBoP.XR.Timeline.Rendering
{
    public sealed class PreloadedTimelineGpuResources : IDisposable
    {
        private readonly ReadOnlyCollection<PreloadedGpuColumn> m_Columns;
        private readonly GraphicsBuffer m_SelectionBuffer;
        private readonly uint[] m_SelectionValue = new uint[1];
        private bool m_Disposed;

        public PreloadedTimelineGpuResources(PreloadedDynamicTimeline timeline, long maximumGpuBytes)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            if (maximumGpuBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumGpuBytes));

            RequiredGpuBytes = EstimateRequiredBytes(timeline);
            if (RequiredGpuBytes > maximumGpuBytes)
                throw new InvalidOperationException($"The preloaded timeline requires {RequiredGpuBytes} GPU bytes, above the explicit budget of {maximumGpuBytes} bytes.");

            var columns = new List<PreloadedGpuColumn>(timeline.Columns.Count);
            try
            {
                for (int index = 0; index < timeline.Columns.Count; index++)
                    columns.Add(new PreloadedGpuColumn(timeline.Columns[index], timeline.IndexCount));
                m_SelectionBuffer = CreateBuffer(1, sizeof(uint));
                m_SelectionBuffer.SetData(m_SelectionValue);
                m_Columns = columns.AsReadOnly();
            }
            catch
            {
                for (int index = 0; index < columns.Count; index++)
                    columns[index].Dispose();
                m_SelectionBuffer?.Dispose();
                throw;
            }
        }

        public PreloadedDynamicTimeline Timeline { get; }
        public int IndexCount => Timeline.IndexCount;
        public int SelectedIndex { get; private set; }
        public long RequiredGpuBytes { get; }
        public GraphicsBuffer SelectionBuffer => m_SelectionBuffer;
        public IReadOnlyList<PreloadedGpuColumn> Columns => m_Columns;

        public void SelectIndex(int index)
        {
            ThrowIfDisposed();
            if (index < 0 || index >= IndexCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_SelectionValue[0] = (uint)index;
            m_SelectionBuffer.SetData(m_SelectionValue);
            SelectedIndex = index;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            for (int index = 0; index < m_Columns.Count; index++)
                m_Columns[index].Dispose();
            m_SelectionBuffer.Dispose();
        }

        public static long EstimateRequiredBytes(PreloadedDynamicTimeline timeline)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            long bytes = sizeof(uint);
            for (int columnIndex = 0; columnIndex < timeline.Columns.Count; columnIndex++)
            {
                PreloadedColumnTimeline column = timeline.Columns[columnIndex];
                bytes = Add(bytes, Estimate(column.SurfaceActivity, sizeof(float)));
                bytes = Add(bytes, Estimate(column.SurfaceOpacity, sizeof(float)));
                bytes = Add(bytes, Estimate(column.SurfaceMask, sizeof(uint)));
                bytes = Add(bytes, Estimate(column.SitePositions, 12));
                bytes = Add(bytes, Estimate(column.SiteColors, sizeof(uint)));
                bytes = Add(bytes, Estimate(column.SiteSizes, sizeof(float)));
                bytes = Add(bytes, EstimateSiteStates(column));
                for (int cutIndex = 0; cutIndex < column.Cuts.Count; cutIndex++)
                {
                    PreloadedTimelineChannel<Rgba32> pixels = column.Cuts[cutIndex].Pixels;
                    int slices = pixels.UniqueSliceCount == 1 ? 1 : pixels.IndexCount;
                    bytes = Add(bytes, checked((long)pixels.ElementCount * sizeof(uint) * slices));
                }
            }

            return bytes;
        }

        private static long Estimate<T>(PreloadedTimelineChannel<T> channel, int gpuStride) where T : struct
        {
            if (channel == null)
                return 0;
            int slices = channel.UniqueSliceCount == 1 ? 1 : channel.IndexCount;
            return checked((long)channel.ElementCount * gpuStride * slices);
        }

        private static long EstimateSiteStates(PreloadedColumnTimeline column)
        {
            if (column.SiteVisibility == null)
                return 0;
            bool invariant = column.SiteVisibility.UniqueSliceCount == 1 && column.SiteFlags.UniqueSliceCount == 1;
            return checked((long)column.SiteVisibility.ElementCount * sizeof(uint) * (invariant ? 1 : column.SiteVisibility.IndexCount));
        }

        private static long Add(long left, long right) => checked(left + right);

        private static GraphicsBuffer CreateBuffer(int count, int stride)
        {
            long byteLength = checked((long)count * stride);
            if (byteLength > SystemInfo.maxGraphicsBufferSize)
                throw new InvalidOperationException($"A timeline GPU buffer requires {byteLength} bytes, above the device limit of {SystemInfo.maxGraphicsBufferSize} bytes.");
            return new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(PreloadedTimelineGpuResources));
        }

        public sealed class PreloadedGpuColumn : IDisposable
        {
            private readonly ReadOnlyCollection<PreloadedGpuCut> m_Cuts;
            private bool m_Disposed;

            internal PreloadedGpuColumn(PreloadedColumnTimeline column, int indexCount)
            {
                ColumnId = column.ColumnId;
                try
                {
                    SurfaceActivity = GpuChannel.Upload(column.SurfaceActivity, sizeof(float));
                    SurfaceOpacity = GpuChannel.Upload(column.SurfaceOpacity, sizeof(float));
                    SurfaceMask = GpuChannel.UploadBytesAsUInt(column.SurfaceMask);
                    SitePositions = GpuChannel.Upload(column.SitePositions, 12);
                    SiteColors = GpuChannel.Upload(column.SiteColors, sizeof(uint));
                    SiteSizes = GpuChannel.Upload(column.SiteSizes, sizeof(float));
                    SiteStates = GpuChannel.UploadSiteStates(column.SiteVisibility, column.SiteFlags);
                    var cuts = new List<PreloadedGpuCut>(column.Cuts.Count);
                    for (int index = 0; index < column.Cuts.Count; index++)
                        cuts.Add(new PreloadedGpuCut(column.Cuts[index], indexCount));
                    m_Cuts = cuts.AsReadOnly();
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public CRNL.HiBoP.Contracts.ContractId ColumnId { get; }
            public GpuChannel SurfaceActivity { get; }
            public GpuChannel SurfaceOpacity { get; }
            public GpuChannel SurfaceMask { get; }
            public GpuChannel SitePositions { get; }
            public GpuChannel SiteColors { get; }
            public GpuChannel SiteSizes { get; }
            public GpuChannel SiteStates { get; }
            public IReadOnlyList<PreloadedGpuCut> Cuts => m_Cuts;

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                SurfaceActivity?.Dispose();
                SurfaceOpacity?.Dispose();
                SurfaceMask?.Dispose();
                SitePositions?.Dispose();
                SiteColors?.Dispose();
                SiteSizes?.Dispose();
                SiteStates?.Dispose();
                if (m_Cuts == null)
                    return;
                for (int index = 0; index < m_Cuts.Count; index++)
                    m_Cuts[index].Dispose();
            }
        }

        public sealed class GpuChannel : IDisposable
        {
            private GpuChannel(GraphicsBuffer buffer, int elementCount, bool invariant)
            {
                Buffer = buffer;
                ElementCount = elementCount;
                IsInvariant = invariant;
            }

            public GraphicsBuffer Buffer { get; }
            public int ElementCount { get; }
            public bool IsInvariant { get; }

            public int GetElementOffset(int timelineIndex)
            {
                if (timelineIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(timelineIndex));
                return IsInvariant ? 0 : checked(timelineIndex * ElementCount);
            }

            public void Dispose() => Buffer.Dispose();

            internal static GpuChannel Upload<T>(PreloadedTimelineChannel<T> channel, int stride) where T : struct
            {
                if (channel == null)
                    return null;
                bool invariant = channel.UniqueSliceCount == 1;
                int gpuSlices = invariant ? 1 : channel.IndexCount;
                int count = checked(channel.ElementCount * gpuSlices);
                GraphicsBuffer buffer = CreateBuffer(count, stride);
                try
                {
                    for (int index = 0; index < gpuSlices; index++)
                    {
                        T[] values = channel.GetSlice(invariant ? 0 : index).ToArray();
                        buffer.SetData(values, 0, index * channel.ElementCount, channel.ElementCount);
                    }

                    return new GpuChannel(buffer, channel.ElementCount, invariant);
                }
                catch
                {
                    buffer.Dispose();
                    throw;
                }
            }

            internal static GpuChannel UploadBytesAsUInt(PreloadedTimelineChannel<byte> channel)
            {
                if (channel == null)
                    return null;
                bool invariant = channel.UniqueSliceCount == 1;
                int gpuSlices = invariant ? 1 : channel.IndexCount;
                int count = checked(channel.ElementCount * gpuSlices);
                GraphicsBuffer buffer = CreateBuffer(count, sizeof(uint));
                try
                {
                    var values = new uint[channel.ElementCount];
                    for (int timelineIndex = 0; timelineIndex < gpuSlices; timelineIndex++)
                    {
                        RenderBuffer<byte> source = channel.GetSlice(invariant ? 0 : timelineIndex);
                        for (int index = 0; index < values.Length; index++)
                            values[index] = source[index];
                        buffer.SetData(values, 0, timelineIndex * channel.ElementCount, channel.ElementCount);
                    }

                    return new GpuChannel(buffer, channel.ElementCount, invariant);
                }
                catch
                {
                    buffer.Dispose();
                    throw;
                }
            }

            internal static GpuChannel UploadSiteStates(PreloadedTimelineChannel<byte> visibility, PreloadedTimelineChannel<SiteRenderFlags> flags)
            {
                if (visibility == null)
                    return null;
                bool invariant = visibility.UniqueSliceCount == 1 && flags.UniqueSliceCount == 1;
                int gpuSlices = invariant ? 1 : visibility.IndexCount;
                int count = checked(visibility.ElementCount * gpuSlices);
                GraphicsBuffer buffer = CreateBuffer(count, sizeof(uint));
                try
                {
                    var values = new uint[visibility.ElementCount];
                    for (int timelineIndex = 0; timelineIndex < gpuSlices; timelineIndex++)
                    {
                        RenderBuffer<byte> visibilitySlice = visibility.GetSlice(invariant ? 0 : timelineIndex);
                        RenderBuffer<SiteRenderFlags> flagSlice = flags.GetSlice(invariant ? 0 : timelineIndex);
                        for (int index = 0; index < values.Length; index++)
                            values[index] = visibilitySlice[index] | ((uint)flagSlice[index] << 8);
                        buffer.SetData(values, 0, timelineIndex * visibility.ElementCount, visibility.ElementCount);
                    }

                    return new GpuChannel(buffer, visibility.ElementCount, invariant);
                }
                catch
                {
                    buffer.Dispose();
                    throw;
                }
            }
        }

        public sealed class PreloadedGpuCut : IDisposable
        {
            private readonly int[] m_LayerByTimelineIndex;

            internal PreloadedGpuCut(PreloadedCutTimeline cut, int indexCount)
            {
                CutId = cut.CutId;
                bool invariant = cut.Pixels.UniqueSliceCount == 1;
                int layers = invariant ? 1 : indexCount;
                if (layers > SystemInfo.maxTextureArraySlices)
                    throw new InvalidOperationException($"The cut timeline requires {layers} texture-array layers, above the device limit of {SystemInfo.maxTextureArraySlices}.");
                Texture = new Texture2DArray(cut.Width, cut.Height, layers, TextureFormat.RGBA32, false, false);
                m_LayerByTimelineIndex = new int[indexCount];
                try
                {
                    for (int index = 0; index < layers; index++)
                        Texture.SetPixelData(cut.Pixels.GetSlice(invariant ? 0 : index).ToArray(), 0, index);
                    for (int index = 0; index < indexCount; index++)
                        m_LayerByTimelineIndex[index] = invariant ? 0 : index;
                    Texture.Apply(false, true);
                }
                catch
                {
                    UnityEngine.Object.Destroy(Texture);
                    throw;
                }
            }

            public CRNL.HiBoP.Contracts.ContractId CutId { get; }
            public Texture2DArray Texture { get; }

            public int GetLayer(int timelineIndex)
            {
                if (timelineIndex < 0 || timelineIndex >= m_LayerByTimelineIndex.Length)
                    throw new ArgumentOutOfRangeException(nameof(timelineIndex));
                return m_LayerByTimelineIndex[timelineIndex];
            }

            public void Dispose() => UnityEngine.Object.Destroy(Texture);
        }
    }
}
