using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.StaticRendering;
using UnityEngine;

namespace CRNL.HiBoP.XR.BrainInstances
{
    public enum BrainBindingKind : byte
    {
        Unknown = 0,
        VisualizationBound = 1,
        ColumnBound = 2,
    }

    public readonly struct BrainInstanceBinding : IEquatable<BrainInstanceBinding>
    {
        private BrainInstanceBinding(BrainBindingKind kind, ContractId visualizationId, ContractId columnId)
        {
            Kind = kind;
            VisualizationId = visualizationId;
            ColumnId = columnId;
        }

        public BrainBindingKind Kind { get; }

        public ContractId VisualizationId { get; }

        public ContractId ColumnId { get; }

        public bool IsValid => VisualizationId.IsValid && (Kind == BrainBindingKind.VisualizationBound ? !ColumnId.IsValid : Kind == BrainBindingKind.ColumnBound && ColumnId.IsValid);

        public static BrainInstanceBinding ForVisualization(ContractId visualizationId)
        {
            if (!visualizationId.IsValid)
                throw new ArgumentException("A valid visualization ID is required.", nameof(visualizationId));
            return new BrainInstanceBinding(BrainBindingKind.VisualizationBound, visualizationId, default);
        }

        public static BrainInstanceBinding ForColumn(ContractId visualizationId, ContractId columnId)
        {
            if (!visualizationId.IsValid)
                throw new ArgumentException("A valid visualization ID is required.", nameof(visualizationId));
            if (!columnId.IsValid)
                throw new ArgumentException("A valid column ID is required.", nameof(columnId));
            return new BrainInstanceBinding(BrainBindingKind.ColumnBound, visualizationId, columnId);
        }

        public bool Equals(BrainInstanceBinding other)
        {
            return Kind == other.Kind && VisualizationId == other.VisualizationId && ColumnId == other.ColumnId;
        }

        public override bool Equals(object obj) => obj is BrainInstanceBinding other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ VisualizationId.GetHashCode();
                return (hash * 397) ^ ColumnId.GetHashCode();
            }
        }

        public static bool operator ==(BrainInstanceBinding left, BrainInstanceBinding right) => left.Equals(right);

        public static bool operator !=(BrainInstanceBinding left, BrainInstanceBinding right) => !left.Equals(right);
    }

    public readonly struct BrainInstanceLayout
    {
        public BrainInstanceLayout(Vector3 localPosition, Quaternion localRotation, float uniformScale, bool visible)
        {
            if (!IsFinite(localPosition))
                throw new ArgumentOutOfRangeException(nameof(localPosition));
            if (!IsFinite(localRotation) || SquaredMagnitude(localRotation) <= 0f)
                throw new ArgumentOutOfRangeException(nameof(localRotation));
            if (!IsFinite(uniformScale) || uniformScale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(uniformScale));

            LocalPosition = localPosition;
            LocalRotation = Quaternion.Normalize(localRotation);
            UniformScale = uniformScale;
            Visible = visible;
        }

        public Vector3 LocalPosition { get; }

        public Quaternion LocalRotation { get; }

        public float UniformScale { get; }

        public bool Visible { get; }

        public static BrainInstanceLayout Identity => new(Vector3.zero, Quaternion.identity, 1f, true);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        private static bool IsFinite(Quaternion value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

        private static float SquaredMagnitude(Quaternion value) => value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
    }

    public enum BrainInstanceCloseReason : byte
    {
        Unknown = 0,
        Requested = 1,
        VisualizationClosed = 2,
        ColumnClosed = 3,
        NewEpoch = 4,
        SessionClosed = 5,
    }

    public sealed class ClosedBrainInstance
    {
        public ClosedBrainInstance(ContractId instanceId, BrainInstanceCloseReason reason)
        {
            InstanceId = instanceId;
            Reason = reason;
        }

        public ContractId InstanceId { get; }

        public BrainInstanceCloseReason Reason { get; }
    }

    public sealed class BrainReconciliationResult
    {
        internal BrainReconciliationResult(IEnumerable<ClosedBrainInstance> closed, IEnumerable<ContractId> awaitingAssets, bool epochChanged)
        {
            Closed = new ReadOnlyCollection<ClosedBrainInstance>(new List<ClosedBrainInstance>(closed));
            AwaitingAssets = new ReadOnlyCollection<ContractId>(new List<ContractId>(awaitingAssets));
            EpochChanged = epochChanged;
        }

        public IReadOnlyList<ClosedBrainInstance> Closed { get; }

        public IReadOnlyList<ContractId> AwaitingAssets { get; }

        public bool EpochChanged { get; }
    }

    public readonly struct BrainInstanceMetrics
    {
        internal BrainInstanceMetrics(int instanceCount, int rendererCount, int distinctSurfaceAssets, int distinctMeshes, int expectedDrawCalls, long residentAssetBytes, long sharedMeshBytes)
        {
            InstanceCount = instanceCount;
            RendererCount = rendererCount;
            DistinctSurfaceAssets = distinctSurfaceAssets;
            DistinctMeshes = distinctMeshes;
            ExpectedDrawCalls = expectedDrawCalls;
            ResidentAssetBytes = residentAssetBytes;
            SharedMeshBytes = sharedMeshBytes;
        }

        public int InstanceCount { get; }

        public int RendererCount { get; }

        public int DistinctSurfaceAssets { get; }

        public int DistinctMeshes { get; }

        public int ExpectedDrawCalls { get; }

        public long ResidentAssetBytes { get; }

        public long SharedMeshBytes { get; }
    }

    internal readonly struct ResolvedBrainBinding
    {
        public ResolvedBrainBinding(AssetHash surfaceHash, SurfaceRepresentation representation, SurfaceTransparency transparency, ContractId activeColumnId)
        {
            SurfaceHash = surfaceHash;
            Representation = representation;
            Transparency = transparency;
            ActiveColumnId = activeColumnId;
        }

        public AssetHash SurfaceHash { get; }

        public SurfaceRepresentation Representation { get; }

        public SurfaceTransparency Transparency { get; }

        public ContractId ActiveColumnId { get; }
    }
}
