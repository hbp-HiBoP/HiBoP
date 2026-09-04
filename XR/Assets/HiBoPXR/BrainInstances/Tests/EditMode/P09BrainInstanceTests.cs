using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.BrainInstances.Editor;
using CRNL.HiBoP.XR.RemoteAssets;
using CRNL.HiBoP.XR.Sites;
using CRNL.HiBoP.XR.StaticRendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CRNL.HiBoP.XR.BrainInstances.Tests
{
    public class P09BrainInstanceTests
    {
        private static readonly ContractId VisualizationId = new(9, 10);
        private static readonly ContractId FirstColumnId = new(9, 20);
        private static readonly ContractId SecondColumnId = new(9, 21);
        private static readonly ContractId AssetId = new(9, 30);

        [Test]
        public void CreationIsExplicitAndBindingsFollowOnlyTheirDefinedColumnPolicy()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            Assert.That(environment.Registry.Count, Is.Zero, "Snapshot reconciliation must not create instances.");

            Assert.That(environment.Registry.TryCreate(new ContractId(9, 41), BrainInstanceBinding.ForVisualization(VisualizationId), BrainInstanceLayout.Identity, out BrainInstance visualizationBound), Is.True);
            Assert.That(environment.Registry.TryCreate(new ContractId(9, 42), BrainInstanceBinding.ForColumn(VisualizationId, FirstColumnId), BrainInstanceLayout.Identity, out BrainInstance columnBound), Is.True);
            Assert.That(visualizationBound.ActiveColumnId, Is.EqualTo(FirstColumnId));
            Assert.That(columnBound.ActiveColumnId, Is.EqualTo(FirstColumnId));

            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: false, secondSelected: true, revision: 2));
            Assert.That(visualizationBound.ActiveColumnId, Is.EqualTo(SecondColumnId), "Visualization binding must follow canonical selection.");
            Assert.That(columnBound.ActiveColumnId, Is.EqualTo(FirstColumnId), "Column binding must remain pinned.");

            ulong revision = columnBound.LocalRevision;
            Assert.That(environment.Registry.TryRebind(columnBound.InstanceId, BrainInstanceBinding.ForColumn(VisualizationId, SecondColumnId)), Is.True);
            Assert.That(columnBound.ActiveColumnId, Is.EqualTo(SecondColumnId));
            Assert.That(columnBound.LocalRevision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void DesktopClosureReportsEveryRemovedInstanceAndLeavesNoGhostOnReopen()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            environment.Registry.TryCreate(new ContractId(9, 51), BrainInstanceBinding.ForVisualization(VisualizationId), BrainInstanceLayout.Identity, out BrainInstance visualizationBound);
            environment.Registry.TryCreate(new ContractId(9, 52), BrainInstanceBinding.ForColumn(VisualizationId, FirstColumnId), BrainInstanceLayout.Identity, out _);

            BrainReconciliationResult columnClosed = environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: false, secondSelected: true, includeFirstColumn: false, revision: 2));
            Assert.That(columnClosed.Closed.Select(item => item.InstanceId), Is.EquivalentTo(new[] { new ContractId(9, 52) }));
            Assert.That(columnClosed.Closed[0].Reason, Is.EqualTo(BrainInstanceCloseReason.ColumnClosed));
            Assert.That(environment.Registry.Count, Is.EqualTo(1));
            Assert.That(visualizationBound.ActiveColumnId, Is.EqualTo(SecondColumnId));

            BrainReconciliationResult visualizationClosed = environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, includeVisualization: false, revision: 3));
            Assert.That(visualizationClosed.Closed.Count, Is.EqualTo(1));
            Assert.That(visualizationClosed.Closed[0].Reason, Is.EqualTo(BrainInstanceCloseReason.VisualizationClosed));
            Assert.That(environment.Registry.Count, Is.Zero);

            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false, revision: 4));
            Assert.That(environment.Registry.Count, Is.Zero, "A reopened Desktop target cannot resurrect a closed XR instance.");
        }

        [Test]
        public void SameEpochResumePreservesLayoutWhileNewEpochPurgesInstancesAndMemory()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            ContractId instanceId = new(9, 61);
            var layout = new BrainInstanceLayout(new Vector3(0.4f, -0.2f, 1.1f), Quaternion.Euler(12f, 34f, 56f), 1.7f, true);
            environment.Registry.TryCreate(instanceId, BrainInstanceBinding.ForVisualization(VisualizationId), layout, out BrainInstance instance);

            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: false, secondSelected: true, revision: 2));
            AssertLayout(instance, layout);
            Assert.That(instance.ActiveColumnId, Is.EqualTo(SecondColumnId));

            BrainReconciliationResult newEpoch = environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 2, firstSelected: true, secondSelected: false));
            Assert.That(newEpoch.EpochChanged, Is.True);
            Assert.That(newEpoch.Closed.Single().Reason, Is.EqualTo(BrainInstanceCloseReason.NewEpoch));
            Assert.That(environment.Registry.Count, Is.Zero);
            Assert.That(environment.Cache.ResidentBytes, Is.Zero, "New epoch must purge the last active P08 payload after owner release.");
        }

        [Test]
        public void LocalTransformsAreIndependentAndSurfaceAssetAndMeshArePhysicallyShared()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            environment.Registry.TryCreate(new ContractId(9, 71), BrainInstanceBinding.ForVisualization(VisualizationId), BrainInstanceLayout.Identity, out BrainInstance first);
            environment.Registry.TryCreate(new ContractId(9, 72), BrainInstanceBinding.ForColumn(VisualizationId, FirstColumnId), new BrainInstanceLayout(new Vector3(2f, 3f, 4f), Quaternion.Euler(0f, 45f, 0f), 1.5f, true), out BrainInstance second);

            Assert.That(first.View.SurfaceAsset, Is.SameAs(second.View.SurfaceAsset));
            Assert.That(first.View.SharedMesh, Is.SameAs(second.View.SharedMesh));
            Assert.That(first.View.transform.localPosition, Is.Not.EqualTo(second.View.transform.localPosition));
            environment.Registry.TrySetScale(first.InstanceId, 2.25f);
            environment.Registry.TryRecenter(first.InstanceId, new Vector3(-1f, 0.5f, 0.25f), Quaternion.Euler(10f, 20f, 30f));
            BrainInstanceLayout firstLayout = first.Layout;
            environment.Registry.TrySetLayout(first.InstanceId, new BrainInstanceLayout(firstLayout.LocalPosition, firstLayout.LocalRotation, firstLayout.UniformScale, false));
            Assert.That(second.Layout.UniformScale, Is.EqualTo(1.5f));
            Assert.That(second.View.transform.localPosition, Is.EqualTo(new Vector3(2f, 3f, 4f)));
            Assert.That(first.View.gameObject.activeSelf, Is.False);
            Assert.That(second.View.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void MissingReplacementAssetKeepsLastCoherentBindingAndRender()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            environment.Registry.TryCreate(new ContractId(9, 81), BrainInstanceBinding.ForVisualization(VisualizationId), BrainInstanceLayout.Identity, out BrainInstance instance);
            Mesh originalMesh = instance.View.SharedMesh;
            AssetHash originalHash = instance.SurfaceHash;
            AssetHash missingHash = new(90, 91, 92, 93);

            BrainReconciliationResult result = environment.Registry.Reconcile(Snapshot(missingHash, epoch: 1, firstSelected: true, secondSelected: false, revision: 2));
            Assert.That(result.AwaitingAssets, Is.EqualTo(new[] { instance.InstanceId }));
            Assert.That(instance.SurfaceHash, Is.EqualTo(originalHash));
            Assert.That(instance.View.SharedMesh, Is.SameAs(originalMesh));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(8)]
        public void CardinalityMetricsProveConstantTopologyMemory(int instanceCount)
        {
            using TestEnvironment environment = TestEnvironment.CreateD1();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            for (int index = 0; index < instanceCount; index++)
            {
                Assert.That(environment.Registry.TryCreate(new ContractId(100, (ulong)index + 1), BrainInstanceBinding.ForColumn(VisualizationId, FirstColumnId), new BrainInstanceLayout(new Vector3(index * 0.2f, 0f, 0f), Quaternion.identity, 1f, true), out _), Is.True);
            }

            BrainInstanceMetrics metrics = environment.Registry.CaptureMetrics();
            Assert.That(metrics.InstanceCount, Is.EqualTo(instanceCount));
            Assert.That(metrics.RendererCount, Is.EqualTo(instanceCount));
            Assert.That(metrics.DistinctSurfaceAssets, Is.EqualTo(1));
            Assert.That(metrics.DistinctMeshes, Is.EqualTo(1));
            Assert.That(metrics.ResidentAssetBytes, Is.EqualTo(environment.PayloadBytes));
            Assert.That(metrics.SharedMeshBytes, Is.GreaterThan(0));
            Assert.That(metrics.ExpectedDrawCalls, Is.EqualTo(instanceCount));
            TestContext.WriteLine($"P09_METRICS instances={instanceCount} renderers={metrics.RendererCount} distinctAssets={metrics.DistinctSurfaceAssets} distinctMeshes={metrics.DistinctMeshes} payloadBytes={metrics.ResidentAssetBytes} meshBytes={metrics.SharedMeshBytes} expectedDrawCalls={metrics.ExpectedDrawCalls}");

            Assert.That(environment.Registry.CloseSession().Count, Is.EqualTo(instanceCount));
            Assert.That(environment.Cache.ResidentBytes, Is.Zero);
        }

        [Test]
        public void RepeatedCreateCloseCyclesReleaseEveryRendererLease()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            for (int cycle = 0; cycle < 256; cycle++)
            {
                ContractId instanceId = new(200, (ulong)cycle + 1);
                Assert.That(environment.Registry.TryCreate(instanceId, BrainInstanceBinding.ForColumn(VisualizationId, FirstColumnId), BrainInstanceLayout.Identity, out _), Is.True);
                Assert.That(environment.Registry.TryClose(instanceId, out ClosedBrainInstance closed), Is.True);
                Assert.That(closed.Reason, Is.EqualTo(BrainInstanceCloseReason.Requested));
            }

            BrainInstanceMetrics metrics = environment.Registry.CaptureMetrics();
            Assert.That(metrics.InstanceCount, Is.Zero);
            Assert.That(metrics.DistinctMeshes, Is.Zero);
            environment.Registry.CloseSession();
            Assert.That(environment.Cache.ResidentBytes, Is.Zero);
        }

        [Test]
        public void RegistryOwnsTheProductionSiteFrameCommandAndOutcomePath()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            environment.Registry.Reconcile(Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false));
            ContractId instanceId = new(210, 1);
            Assert.That(environment.Registry.TryCreate(instanceId, BrainInstanceBinding.ForColumn(VisualizationId, FirstColumnId), BrainInstanceLayout.Identity, out BrainInstance instance), Is.True);

            ContractId siteId = new(210, 2);
            var siteAsset = new SiteAsset(new AssetHash(210, 3, 4, 5), CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0f, 0f, 0f), new Float3(0f, 0f, 0f)), RenderBuffer<ContractId>.TakeOwnership(new[] { siteId }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0f, 0f, 0f) }));
            var siteFrame = new SiteRenderFrame(siteAsset.Hash, new StateRevision(1), new RenderTemporalSample(0, 0f), TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0f, 0f, 0f) }), RenderBuffer<Rgba32>.TakeOwnership(new[] { new Rgba32(255, 255, 255, 255) }), RenderBuffer<float>.TakeOwnership(new[] { 1f }), RenderBuffer<byte>.TakeOwnership(new byte[] { 1 }), RenderBuffer<SiteRenderFlags>.TakeOwnership(new SiteRenderFlags[1]));
            ContractId forwardedInstance = default;
            Command forwarded = null;
            environment.Registry.SiteSelectionRequested += (source, command) =>
            {
                forwardedInstance = source;
                forwarded = command;
            };

            Assert.That(environment.Registry.TryApplySiteFrame(instanceId, siteAsset, siteFrame), Is.True);
            Assert.That(environment.Registry.TryUpdateSiteProximityHover(instanceId, Vector3.zero, out SitePickResult hover), Is.True);
            Assert.That(hover.SiteId, Is.EqualTo(siteId));
            ContractId commandId = new(210, 6);
            Assert.That(environment.Registry.TryConfirmSiteHover(instanceId, commandId, new ContractId(210, 7)), Is.True);
            Assert.That(forwardedInstance, Is.EqualTo(instanceId));
            Assert.That(forwarded.Kind, Is.EqualTo(CommandKind.SelectSite));
            Assert.That(forwarded.Scope, Is.EqualTo(new ScopeKey(ScopeType.Column, new ContractId(9, FirstColumnId.Low + 200))));

            var metadata = new SiteSelectionMetadata(forwarded.Session, siteId, FirstColumnId, new StateRevision(2), "A1", Array.Empty<SiteSelectionMeasurement>(), true, false, false);
            Assert.That(environment.Registry.TryApplySiteSelectionOutcome(instanceId, CommandOutcome.Accept(commandId, new StateRevision(2), new ScopeRevision(2), Optional<ContractValue>.Some(ContractValue.FromId(siteId))), metadata), Is.True);
            Assert.That(instance.View.SiteSelection.CanonicalSiteId, Is.EqualTo(siteId));
        }

        [Test]
        public void RuntimeContainsNoBusinessCardinalityLimitOrDesktopCommandPath()
        {
            string runtime = Path.Combine(Application.dataPath, "HiBoPXR", "BrainInstances", "Runtime");
            string source = string.Join("\n", Directory.GetFiles(runtime, "*.cs").Select(File.ReadAllText));
            Assert.That(source, Does.Not.Contain("MaximumInstances"));
            Assert.That(source, Does.Not.Contain("MaxInstances"));
            Assert.That(source, Does.Not.Contain("new Command("));
            Assert.That(source, Does.Not.Contain("PlayerPrefs"));
            Assert.That(source, Does.Not.Contain("persistentDataPath"));
        }

        [Test]
        public void PrefabAndSceneOwnTheSerializedGameObjects()
        {
            P09ProjectSetup.Validate();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P09ProjectSetup.InstancePrefabPath);
            Assert.That(prefab.GetComponent<BrainInstanceView>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<MeshFilter>(true).Length, Is.EqualTo(2));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(P09ProjectSetup.ScenePath), Is.Not.Null);
        }

        [Test]
        public void DemoPrefabRunsThreeInstancesFromOneP08Surface()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P09ProjectSetup.DemoPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                P09MultiBrainProbe probe = instance.GetComponent<P09MultiBrainProbe>();
                probe.Run();
                Assert.That(probe.Metrics.InstanceCount, Is.EqualTo(3));
                Assert.That(probe.Metrics.DistinctSurfaceAssets, Is.EqualTo(1));
                Assert.That(probe.Metrics.DistinctMeshes, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void InvalidEntityScopeMappingIsRejectedBeforeRegistryMutation()
        {
            using TestEnvironment environment = TestEnvironment.Create();
            SessionSnapshot invalid = Snapshot(environment.Hash, epoch: 1, firstSelected: true, secondSelected: false, omitColumnParent: true);
            Assert.Throws<InvalidOperationException>(() => environment.Registry.Reconcile(invalid));
            Assert.That(environment.Registry.Count, Is.Zero);
        }

        private static SessionSnapshot Snapshot(AssetHash hash, ulong epoch, bool firstSelected = false, bool secondSelected = false, bool includeVisualization = true, bool includeFirstColumn = true, bool includeSecondColumn = true, ulong revision = 1, bool omitColumnParent = false)
        {
            SessionEpoch session = new(new ContractId(9, epoch), epoch);
            var scopes = new List<ScopeState>();
            scopes.Add(new ScopeState(new ScopeKey(ScopeType.Project, new ContractId(9, 101)), new ScopeRevision(revision), new[]
            {
                new StateProperty(V1PropertyKeys.ProjectVisualizationMembership, ContractValue.FromIds(includeVisualization ? new[] { VisualizationId } : Array.Empty<ContractId>())),
            }));
            if (includeVisualization)
            {
                var membership = new List<ContractId>();
                if (includeFirstColumn)
                    membership.Add(FirstColumnId);
                if (includeSecondColumn)
                    membership.Add(SecondColumnId);
                scopes.Add(new ScopeState(new ScopeKey(ScopeType.Visualization, new ContractId(9, 102)), new ScopeRevision(revision), new[]
                {
                    new StateProperty(V1PropertyKeys.VisualizationEntity, ContractValue.FromId(VisualizationId)),
                    new StateProperty(V1PropertyKeys.VisualizationColumnMembership, ContractValue.FromIds(membership)),
                    new StateProperty(V1PropertyKeys.VisualizationSurfaceAsset, ContractValue.FromId(AssetId)),
                    new StateProperty(V1PropertyKeys.VisualizationSurfaceRepresentation, ContractValue.FromUnsignedInteger((ulong)SurfaceRepresentation.Anatomical)),
                    new StateProperty(V1PropertyKeys.VisualizationTransparentBrain, ContractValue.FromBoolean(false)),
                }));
                if (includeFirstColumn)
                    scopes.Add(ColumnScope(FirstColumnId, firstSelected, revision, omitColumnParent));
                if (includeSecondColumn)
                    scopes.Add(ColumnScope(SecondColumnId, secondSelected, revision, false));
            }

            return new SessionSnapshot(ContractVersion.V1, session, new StateRevision(revision), scopes, includeVisualization ? new[] { new AssetReference(AssetId, hash, SurfaceAssetPayloadCodec.SchemaVersion) } : Array.Empty<AssetReference>());
        }

        private static ScopeState ColumnScope(ContractId columnId, bool selected, ulong revision, bool omitParent)
        {
            var properties = new List<StateProperty>
            {
                new(V1PropertyKeys.ColumnEntity, ContractValue.FromId(columnId)),
                new(V1PropertyKeys.ColumnSelected, ContractValue.FromBoolean(selected)),
            };
            if (!omitParent)
                properties.Add(new StateProperty(V1PropertyKeys.ColumnVisualization, ContractValue.FromId(VisualizationId)));
            return new ScopeState(new ScopeKey(ScopeType.Column, new ContractId(9, columnId.Low + 200)), new ScopeRevision(revision), properties);
        }

        private static void AssertLayout(BrainInstance instance, BrainInstanceLayout expected)
        {
            Assert.That(instance.Layout.LocalPosition, Is.EqualTo(expected.LocalPosition));
            Assert.That(Quaternion.Angle(instance.Layout.LocalRotation, expected.LocalRotation), Is.LessThan(0.0001f));
            Assert.That(instance.Layout.UniformScale, Is.EqualTo(expected.UniformScale));
            Assert.That(instance.Layout.Visible, Is.EqualTo(expected.Visible));
        }

        private sealed class TestEnvironment : IDisposable
        {
            private TestEnvironment(InMemoryRemoteAssetCache cache, BrainInstanceRegistry registry, AssetHash hash, int payloadBytes)
            {
                Cache = cache;
                Registry = registry;
                Hash = hash;
                PayloadBytes = payloadBytes;
            }

            public InMemoryRemoteAssetCache Cache { get; }

            public BrainInstanceRegistry Registry { get; }

            public AssetHash Hash { get; }

            public int PayloadBytes { get; }

            public static TestEnvironment Create()
            {
                BrainInstanceView prefab = AssetDatabase.LoadAssetAtPath<BrainInstanceView>(P09ProjectSetup.InstancePrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Run P09ProjectSetup.Apply before the tests.");
                return CreateFromSurface(prefab, CreateSurface());
            }

            public static TestEnvironment CreateD1()
            {
                BrainInstanceView prefab = AssetDatabase.LoadAssetAtPath<BrainInstanceView>(P09ProjectSetup.InstancePrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Run P09ProjectSetup.Apply before the tests.");
                TextAsset binary = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/HiBoPXR/StaticRendering/Data/P05D1Anatomical.bytes");
                if (binary == null)
                    throw new InvalidOperationException("The P05 D1 anatomical surface must be exported before P09 profiling.");
                return CreateFromSurface(prefab, P05SurfaceAssetBinary.Read(binary));
            }

            private static TestEnvironment CreateFromSurface(BrainInstanceView prefab, SurfaceAsset source)
            {
                byte[] payload = SurfaceAssetPayloadCodec.Encode(source);
                AssetHash hash = SurfaceAssetPayloadCodec.ComputeHash(payload);
                var cache = new InMemoryRemoteAssetCache(payload.Length * 4L);
                int chunkBytes = Math.Min(payload.Length, RemoteAssetDescriptor.MaximumChunkBytes);
                var descriptor = new RemoteAssetDescriptor(new AssetReference(AssetId, hash, SurfaceAssetPayloadCodec.SchemaVersion), RemoteAssetKind.Surface, RemoteAssetVariant.Anatomical, payload.Length, source.Positions.Count, source.Indices.Count, source.StaticUvs.Count, chunkBytes, Array.Empty<RemoteAssetDependency>());
                RemoteAssetTransfer transfer = cache.StartTransfer(descriptor).Transfer;
                for (int chunk = 0; chunk < descriptor.ChunkCount; chunk++)
                {
                    int offset = chunk * chunkBytes;
                    int length = Math.Min(chunkBytes, payload.Length - offset);
                    var bytes = new byte[length];
                    Buffer.BlockCopy(payload, offset, bytes, 0, length);
                    transfer.WriteChunk(chunk, bytes);
                }

                Assert.That(transfer.Complete(), Is.EqualTo(AssetTransferCompletion.Published));
                var store = new RemoteSurfaceAssetStore(cache);
                var registry = new BrainInstanceRegistry(prefab, null, cache, store);
                return new TestEnvironment(cache, registry, hash, payload.Length);
            }

            public void Dispose()
            {
                Registry.Dispose();
                Cache.Dispose();
            }

            private static SurfaceAsset CreateSurface()
            {
                return new SurfaceAsset(new AssetHash(9, 9, 9, 9), SurfaceRepresentation.Anatomical, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(0f, 0f, 0f), new Float3(1f, 1f, 0f)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0f, 0f, 0f), new Float3(1f, 0f, 0f), new Float3(0f, 1f, 0f) }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0f, 0f, 1f), new Float3(0f, 0f, 1f), new Float3(0f, 0f, 1f) }), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2 }), RenderBuffer<Float2>.TakeOwnership(Array.Empty<Float2>()));
            }
        }
    }
}
