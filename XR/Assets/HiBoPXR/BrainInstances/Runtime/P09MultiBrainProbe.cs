using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.RemoteAssets;
using UnityEngine;

namespace CRNL.HiBoP.XR.BrainInstances
{
    public sealed class P09MultiBrainProbe : MonoBehaviour
    {
        [SerializeField] private BrainInstanceView instancePrefab;
        [SerializeField] private Transform instanceRoot;

        private InMemoryRemoteAssetCache m_Cache;
        private BrainInstanceRegistry m_Registry;

        public BrainInstanceMetrics Metrics => m_Registry == null ? default : m_Registry.CaptureMetrics();

        public void Configure(BrainInstanceView prefab, Transform root)
        {
            instancePrefab = prefab;
            instanceRoot = root;
        }

        private void Start()
        {
            Run();
        }

        public void Run()
        {
            if (m_Registry != null)
                return;
            if (instancePrefab == null || instanceRoot == null)
                throw new InvalidOperationException("P09 demo references must be serialized in its prefab.");

            SurfaceAsset surface = CreateSurface();
            byte[] payload = SurfaceAssetPayloadCodec.Encode(surface);
            AssetHash hash = SurfaceAssetPayloadCodec.ComputeHash(payload);
            ContractId assetId = new(9, 1);
            m_Cache = new InMemoryRemoteAssetCache(payload.Length * 2L);
            Publish(m_Cache, assetId, hash, payload, surface);
            var store = new RemoteSurfaceAssetStore(m_Cache);
            m_Registry = new BrainInstanceRegistry(instancePrefab, instanceRoot, m_Cache, store);

            ContractId visualizationId = new(9, 10);
            ContractId columnId = new(9, 20);
            m_Registry.Reconcile(CreateSnapshot(new SessionEpoch(new ContractId(9, 100), 1), visualizationId, columnId, assetId, hash));
            m_Registry.TryCreate(new ContractId(9, 31), BrainInstanceBinding.ForVisualization(visualizationId), new BrainInstanceLayout(new Vector3(-0.24f, 0f, 0f), Quaternion.identity, 1f, true), out _);
            m_Registry.TryCreate(new ContractId(9, 32), BrainInstanceBinding.ForColumn(visualizationId, columnId), new BrainInstanceLayout(Vector3.zero, Quaternion.Euler(0f, 25f, 0f), 1.2f, true), out _);
            m_Registry.TryCreate(new ContractId(9, 33), BrainInstanceBinding.ForColumn(visualizationId, columnId), new BrainInstanceLayout(new Vector3(0.24f, 0f, 0f), Quaternion.Euler(0f, -25f, 0f), 0.8f, true), out _);
            BrainInstanceMetrics metrics = m_Registry.CaptureMetrics();
            Debug.Log($"P09 multi-brain ready | instances={metrics.InstanceCount} surfaceAssets={metrics.DistinctSurfaceAssets} meshes={metrics.DistinctMeshes} payloadBytes={metrics.ResidentAssetBytes} meshBytes={metrics.SharedMeshBytes} expectedDrawCalls={metrics.ExpectedDrawCalls}");
        }

        private void OnDestroy()
        {
            m_Registry?.Dispose();
            m_Cache?.Dispose();
        }

        private static void Publish(InMemoryRemoteAssetCache cache, ContractId assetId, AssetHash hash, byte[] payload, SurfaceAsset surface)
        {
            var descriptor = new RemoteAssetDescriptor(new AssetReference(assetId, hash, SurfaceAssetPayloadCodec.SchemaVersion), RemoteAssetKind.Surface, RemoteAssetVariant.Anatomical, payload.Length, surface.Positions.Count, surface.Indices.Count, surface.StaticUvs.Count, payload.Length, Array.Empty<RemoteAssetDependency>());
            RemoteAssetTransfer transfer = cache.StartTransfer(descriptor).Transfer;
            transfer.WriteChunk(0, payload);
            if (transfer.Complete() != AssetTransferCompletion.Published)
                throw new InvalidOperationException("The P09 demo surface could not be published through P08.");
        }

        private static SessionSnapshot CreateSnapshot(SessionEpoch session, ContractId visualizationId, ContractId columnId, ContractId assetId, AssetHash hash)
        {
            ScopeState project = new(new ScopeKey(ScopeType.Project, new ContractId(9, 101)), new ScopeRevision(1), new[]
            {
                new StateProperty(V1PropertyKeys.ProjectVisualizationMembership, ContractValue.FromIds(new[] { visualizationId })),
            });
            ScopeState visualization = new(new ScopeKey(ScopeType.Visualization, new ContractId(9, 102)), new ScopeRevision(1), new[]
            {
                new StateProperty(V1PropertyKeys.VisualizationEntity, ContractValue.FromId(visualizationId)),
                new StateProperty(V1PropertyKeys.VisualizationColumnMembership, ContractValue.FromIds(new[] { columnId })),
                new StateProperty(V1PropertyKeys.VisualizationSurfaceAsset, ContractValue.FromId(assetId)),
                new StateProperty(V1PropertyKeys.VisualizationSurfaceRepresentation, ContractValue.FromUnsignedInteger((ulong)SurfaceRepresentation.Anatomical)),
                new StateProperty(V1PropertyKeys.VisualizationTransparentBrain, ContractValue.FromBoolean(false)),
            });
            ScopeState column = new(new ScopeKey(ScopeType.Column, new ContractId(9, 103)), new ScopeRevision(1), new[]
            {
                new StateProperty(V1PropertyKeys.ColumnEntity, ContractValue.FromId(columnId)),
                new StateProperty(V1PropertyKeys.ColumnVisualization, ContractValue.FromId(visualizationId)),
                new StateProperty(V1PropertyKeys.ColumnSelected, ContractValue.FromBoolean(true)),
            });
            return new SessionSnapshot(ContractVersion.V1, session, new StateRevision(1), new[] { project, visualization, column }, new[] { new AssetReference(assetId, hash, SurfaceAssetPayloadCodec.SchemaVersion) });
        }

        private static SurfaceAsset CreateSurface()
        {
            return new SurfaceAsset(new AssetHash(9, 9, 9, 9), SurfaceRepresentation.Anatomical, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(-80f, -60f, 0f), new Float3(80f, 60f, 40f)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(-80f, -60f, 0f), new Float3(80f, -60f, 0f), new Float3(0f, 60f, 40f), new Float3(0f, 0f, -30f) }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0f, 0f, 1f), new Float3(0f, 0f, 1f), new Float3(0f, 0f, 1f), new Float3(0f, 0f, -1f) }), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2, 0, 3, 1, 1, 3, 2, 2, 3, 0 }), RenderBuffer<Float2>.TakeOwnership(Array.Empty<Float2>()));
        }
    }
}
