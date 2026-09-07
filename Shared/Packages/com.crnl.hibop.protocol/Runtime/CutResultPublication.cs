using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.Protocol
{
    public sealed class CutResultManifest
    {
        private readonly ReadOnlyCollection<ContractId> m_ColumnIds;

        public CutResultManifest(IEnumerable<ContractId> columnIds, int width, int height)
        {
            if (columnIds == null)
                throw new ArgumentNullException(nameof(columnIds));
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            var copy = new List<ContractId>(columnIds);
            var unique = new HashSet<ContractId>();
            for (int index = 0; index < copy.Count; index++)
            {
                if (!copy[index].IsValid || !unique.Add(copy[index]))
                    throw new ArgumentException("Expected cut columns must be valid and unique.", nameof(columnIds));
            }

            Width = width;
            Height = height;
            m_ColumnIds = copy.AsReadOnly();
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<ContractId> ColumnIds => m_ColumnIds;

        public void Validate(CutRenderResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (result.Overlays.Count != m_ColumnIds.Count)
                throw new ArgumentException("The cut result does not contain the exact expected column set.", nameof(result));
            if (result.BaseTexture.HasValue)
            {
                TextureAsset texture = result.BaseTexture.Value;
                if (texture.Width != Width || texture.Height != Height || texture.ColorSpace != TextureColorSpace.Srgb)
                    throw new ArgumentException("The cut base must be final RGBA8 sRGB at the manifest dimensions.", nameof(result));
            }

            var actual = new HashSet<ContractId>();
            for (int index = 0; index < result.Overlays.Count; index++)
            {
                CutOverlayFrame overlay = result.Overlays[index];
                if (overlay.TemporalApplication != TemporalApplication.SampleAndHold || overlay.Width != Width || overlay.Height != Height || !actual.Add(overlay.ColumnId))
                    throw new ArgumentException("Cut outputs must be unique, final SampleAndHold RGBA8 images at the manifest dimensions.", nameof(result));
            }

            for (int index = 0; index < m_ColumnIds.Count; index++)
            {
                if (!actual.Contains(m_ColumnIds[index]))
                    throw new ArgumentException("The cut result does not contain the exact expected column set.", nameof(result));
            }
        }
    }

    public readonly struct CutPlanIdentity : IEquatable<CutPlanIdentity>
    {
        public CutPlanIdentity(CutRenderResult result, CutResultManifest manifest)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            manifest.Validate(result);
            CutId = result.CutId;
            CutRevision = result.CutRevision;
            GeometryHash = result.GeometryHash;
            BaseTextureHash = result.BaseTextureHash;
            Width = manifest.Width;
            Height = manifest.Height;
            SourceStateRevision = result.SourceStateRevision;
            ColumnManifestHash = HashColumns(manifest.ColumnIds);
            MappingRevision = result.Overlays.Count == 0 ? result.RenderRevision : result.Overlays[0].MappingRevision;
            for (int index = 1; index < result.Overlays.Count; index++)
            {
                if (result.Overlays[index].MappingRevision != MappingRevision)
                    throw new ArgumentException("Every column image for one cut plan must use the same mapping revision.", nameof(result));
            }
        }

        public ContractId CutId { get; }
        public ScopeRevision CutRevision { get; }
        public AssetHash GeometryHash { get; }
        public AssetHash BaseTextureHash { get; }
        public int Width { get; }
        public int Height { get; }
        public StateRevision SourceStateRevision { get; }
        public AssetHash ColumnManifestHash { get; }
        public ScopeRevision MappingRevision { get; }

        public bool Equals(CutPlanIdentity other)
        {
            return CutId == other.CutId && CutRevision == other.CutRevision && GeometryHash == other.GeometryHash && BaseTextureHash == other.BaseTextureHash && Width == other.Width && Height == other.Height && SourceStateRevision == other.SourceStateRevision && ColumnManifestHash == other.ColumnManifestHash && MappingRevision == other.MappingRevision;
        }

        public override bool Equals(object obj) => obj is CutPlanIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(CutId, CutRevision, GeometryHash, BaseTextureHash, Width, Height, SourceStateRevision, HashCode.Combine(ColumnManifestHash, MappingRevision));
        public static bool operator ==(CutPlanIdentity left, CutPlanIdentity right) => left.Equals(right);
        public static bool operator !=(CutPlanIdentity left, CutPlanIdentity right) => !left.Equals(right);

        private static AssetHash HashColumns(IReadOnlyList<ContractId> columnIds)
        {
            byte[] bytes = new byte[checked(columnIds.Count * ContractId.ByteLength)];
            byte[] idBytes = new byte[ContractId.ByteLength];
            for (int index = 0; index < columnIds.Count; index++)
            {
                columnIds[index].WriteBytes(idBytes);
                Buffer.BlockCopy(idBytes, 0, bytes, index * ContractId.ByteLength, ContractId.ByteLength);
            }

            using SHA256 sha256 = SHA256.Create();
            return AssetHash.FromBytes(sha256.ComputeHash(bytes));
        }
    }

    public readonly struct CutResourceBudget
    {
        public CutResourceBudget(long maximumCpuBytes, long maximumGpuBytes, long activeCpuBytes = 0, long activeGpuBytes = 0)
        {
            if (maximumCpuBytes <= 0 || maximumGpuBytes <= 0 || activeCpuBytes < 0 || activeGpuBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumCpuBytes));
            MaximumCpuBytes = maximumCpuBytes;
            MaximumGpuBytes = maximumGpuBytes;
            ActiveCpuBytes = activeCpuBytes;
            ActiveGpuBytes = activeGpuBytes;
        }

        public long MaximumCpuBytes { get; }
        public long MaximumGpuBytes { get; }
        public long ActiveCpuBytes { get; }
        public long ActiveGpuBytes { get; }
    }

    public readonly struct CutResourceCost
    {
        internal CutResourceCost(long cpuBytes, long gpuBytes, long geometryBytes, long baseBytes, long currentImageBytes, long preloadCpuBytes, long preloadGpuBytes)
        {
            CpuBytes = cpuBytes;
            GpuBytes = gpuBytes;
            GeometryBytes = geometryBytes;
            BaseBytes = baseBytes;
            CurrentImageBytes = currentImageBytes;
            PreloadCpuBytes = preloadCpuBytes;
            PreloadGpuBytes = preloadGpuBytes;
        }

        public long CpuBytes { get; }
        public long GpuBytes { get; }
        public long GeometryBytes { get; }
        public long BaseBytes { get; }
        public long CurrentImageBytes { get; }
        public long PreloadCpuBytes { get; }
        public long PreloadGpuBytes { get; }
    }

    public sealed class CutResourceAdmissionException : InvalidOperationException
    {
        internal CutResourceAdmissionException(CutResourceCost cost, CutResourceBudget budget, int columnCount, int timelineIndexCount) : base($"Cut/preload resource admission refused: CPU {cost.CpuBytes}/{budget.MaximumCpuBytes} bytes, GPU {cost.GpuBytes}/{budget.MaximumGpuBytes} bytes; columns={columnCount}, timelineIndices={timelineIndexCount}; contributors geometry={cost.GeometryBytes}, base={cost.BaseBytes}, currentImages={cost.CurrentImageBytes}, preloadCPU={cost.PreloadCpuBytes}, preloadGPU={cost.PreloadGpuBytes}. No data was truncated or paged.")
        {
            Cost = cost;
            Budget = budget;
        }

        public CutResourceCost Cost { get; }
        public CutResourceBudget Budget { get; }
    }

    public static class CutResourceAdmission
    {
        public static CutResourceCost Require(CutRenderResult result, CutResultManifest manifest, PreloadedDynamicTimeline stablePlanTimeline, CutResourceBudget budget, ISet<AssetHash> residentAssetHashes = null)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            manifest.Validate(result);

            long geometryBytes = result.Geometry.HasValue && (residentAssetHashes == null || !residentAssetHashes.Contains(result.GeometryHash)) ? GeometryBytes(result.Geometry.Value) : 0;
            long baseBytes = result.BaseTexture.HasValue && (residentAssetHashes == null || !residentAssetHashes.Contains(result.BaseTextureHash)) ? checked((long)manifest.Width * manifest.Height * 4) : 0;
            long currentImages = checked((long)manifest.Width * manifest.Height * 4 * result.Overlays.Count);
            long preloadCpu = 0;
            long preloadGpu = 0;
            if (stablePlanTimeline != null)
            {
                if (stablePlanTimeline.Session == default)
                    throw new ArgumentException("A valid stable-plan timeline is required.", nameof(stablePlanTimeline));
                CutPlanIdentity plan = new(result, manifest);
                var preloadedColumns = new HashSet<ContractId>();
                for (int columnIndex = 0; columnIndex < stablePlanTimeline.Columns.Count; columnIndex++)
                {
                    PreloadedColumnTimeline column = stablePlanTimeline.Columns[columnIndex];
                    for (int cutIndex = 0; cutIndex < column.Cuts.Count; cutIndex++)
                    {
                        PreloadedCutTimeline cut = column.Cuts[cutIndex];
                        if (cut.CutId != result.CutId)
                            continue;
                        if (!preloadedColumns.Add(column.ColumnId) || !Contains(manifest.ColumnIds, column.ColumnId) || cut.Width != manifest.Width || cut.Height != manifest.Height || cut.MappingRevision != plan.MappingRevision)
                            throw new ArgumentException("The preloaded cut images belong to a different plan.", nameof(stablePlanTimeline));
                        preloadCpu = checked(preloadCpu + cut.Pixels.UniqueByteLength);
                        int gpuSlices = cut.Pixels.UniqueSliceCount == 1 ? 1 : cut.Pixels.IndexCount;
                        preloadGpu = checked(preloadGpu + (long)cut.Pixels.ElementCount * 4 * gpuSlices);
                    }
                }

                if (preloadedColumns.Count != manifest.ColumnIds.Count)
                    throw new ArgumentException("The stable-plan preload must contain exactly one cut timeline for every expected column.", nameof(stablePlanTimeline));
            }

            long cpu = checked(budget.ActiveCpuBytes + geometryBytes + baseBytes + currentImages + preloadCpu);
            long gpu = checked(budget.ActiveGpuBytes + geometryBytes + baseBytes + currentImages + preloadGpu);
            var cost = new CutResourceCost(cpu, gpu, geometryBytes, baseBytes, currentImages, preloadCpu, preloadGpu);
            if (cpu > budget.MaximumCpuBytes || gpu > budget.MaximumGpuBytes)
                throw new CutResourceAdmissionException(cost, budget, manifest.ColumnIds.Count, stablePlanTimeline?.IndexCount ?? 0);
            return cost;
        }

        private static long GeometryBytes(CutGeometryAsset geometry)
        {
            return checked((long)geometry.Positions.Count * 12 + (long)geometry.Normals.Count * 12 + (long)geometry.Uvs.Count * 8 + (long)geometry.Indices.Count * 4);
        }

        private static bool Contains(IReadOnlyList<ContractId> values, ContractId candidate)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index] == candidate)
                    return true;
            }

            return false;
        }
    }

    public sealed class CutResultPublication
    {
        public CutResultPublication(SessionEpoch session, CutRenderResult result, CutResultManifest manifest, PreloadedDynamicTimeline stablePlanTimeline, CutResourceBudget budget, ISet<AssetHash> residentAssetHashes = null)
        {
            if (!session.IsValid)
                throw new ArgumentException("A valid session is required.", nameof(session));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Manifest.Validate(Result);
            if (stablePlanTimeline != null && (stablePlanTimeline.Session != session || stablePlanTimeline.SourceStateRevision != result.SourceStateRevision))
                throw new ArgumentException("The stable-plan timeline must match the cut session and source state revision.", nameof(stablePlanTimeline));
            Session = session;
            StablePlanTimeline = stablePlanTimeline;
            Plan = new CutPlanIdentity(result, manifest);
            ResourceCost = CutResourceAdmission.Require(result, manifest, stablePlanTimeline, budget, residentAssetHashes);
        }

        public SessionEpoch Session { get; }
        public CutRenderResult Result { get; }
        public CutResultManifest Manifest { get; }
        public PreloadedDynamicTimeline StablePlanTimeline { get; }
        public CutPlanIdentity Plan { get; }
        public CutResourceCost ResourceCost { get; }
    }
}
