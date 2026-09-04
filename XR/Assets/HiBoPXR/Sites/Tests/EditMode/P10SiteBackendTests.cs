using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.BrainInstances;
using CRNL.HiBoP.XR.Sites.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CRNL.HiBoP.XR.Sites.Tests
{
    public sealed class P10SiteBackendTests
    {
        private readonly List<UnityEngine.Object> m_OwnedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object owned in m_OwnedObjects)
                UnityEngine.Object.DestroyImmediate(owned);
            m_OwnedObjects.Clear();
            SiteAssetRuntimeCache.ClearForTests();
        }

        [Test]
        public void D0_RayAndProximityReturnExactOpaqueIdsWithDeterministicTies()
        {
            ContractId lowerId = new(1, 1);
            ContractId higherId = new(1, 2);
            SiteAsset asset = Asset(new[] { higherId, lowerId, new ContractId(1, 3) }, new[] { Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 10f) });
            SiteRenderFrame frame = Frame(asset, new[] { 1f, 1f, 1f }, new byte[] { 1, 1, 1 });
            P10SiteRenderer renderer = Renderer(asset, frame);

            Assert.That(renderer.Raycast(new Ray(new Vector3(0f, 0f, -0.01f), Vector3.forward), 1f, out SitePickResult ray), Is.True);
            Assert.That(ray.SiteId, Is.EqualTo(lowerId));
            Assert.That(renderer.FindNearest(Vector3.zero, out SitePickResult nearest), Is.True);
            Assert.That(nearest.SiteId, Is.EqualTo(lowerId));
        }

        [Test]
        public void InvisibleAndZeroRadiusSitesAreNeverSelectable()
        {
            SiteAsset asset = Asset(new[] { new ContractId(2, 1), new ContractId(2, 2), new ContractId(2, 3) }, new[] { Vector3.zero, new Vector3(0f, 0f, 4f), new Vector3(0f, 0f, 8f) });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 1f, 0f, 1f }, new byte[] { 0, 1, 1 }));

            Assert.That(renderer.Raycast(new Ray(new Vector3(0f, 0f, -0.01f), Vector3.forward), 1f, out SitePickResult result), Is.True);
            Assert.That(result.SiteId, Is.EqualTo(new ContractId(2, 3)));
        }

        [Test]
        public void BrainInstanceUniformScaleTransformsRenderingAndPickingTogether()
        {
            ContractId expected = new(3, 1);
            SiteAsset asset = Asset(new[] { expected }, new[] { new Vector3(0f, 0f, 10f) });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 1f }, new byte[] { 1 }));
            renderer.transform.position = new Vector3(0.5f, 0.2f, -0.1f);
            renderer.transform.rotation = Quaternion.Euler(10f, 25f, 5f);
            renderer.transform.localScale = Vector3.one * 2f;
            Vector3 center = renderer.transform.TransformPoint(new Vector3(0f, 0f, 0.01f));

            Assert.That(renderer.FindNearest(center + renderer.transform.right * 0.025f, out SitePickResult near), Is.True);
            Assert.That(near.SiteId, Is.EqualTo(expected));
            Vector3 origin = center - renderer.transform.forward * 0.05f;
            Assert.That(renderer.Raycast(new Ray(origin, renderer.transform.forward), 0.1f, out SitePickResult ray), Is.True);
            Assert.That(ray.SiteId, Is.EqualTo(expected));
        }

        [Test]
        public void DirtyRangeUpdatesOnlyDeclaredEntriesAndNeverRebuildsStaticCache()
        {
            SiteAsset asset = Asset(new[] { new ContractId(4, 1), new ContractId(4, 2) }, new[] { Vector3.zero, new Vector3(30f, 0f, 0f) });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 1f, 1f }, new byte[] { 1, 1 }));
            GraphicsBuffer originalPositions = renderer.SharedPositionBuffer;
            SiteRenderFrame changed = Frame(asset, new[] { 4f, 1f }, new byte[] { 0, 1 });
            renderer.ApplyFrame(changed, new[] { new SiteDirtyRange(0, 1) });

            Assert.That(renderer.SharedPositionBuffer, Is.SameAs(originalPositions));
            Assert.That(renderer.FindNearest(Vector3.zero, out _), Is.False);
            Assert.That(renderer.FindNearest(new Vector3(0.03f, 0f, 0f), out SitePickResult second), Is.True);
            Assert.That(second.SiteId, Is.EqualTo(new ContractId(4, 2)));
        }

        [Test]
        public void FirstFrameMustBeCompleteBeforeDirtyUpdates()
        {
            SiteAsset asset = Asset(new[] { new ContractId(4, 3) }, new[] { Vector3.zero });
            P10SiteRenderer renderer = RendererWithoutFrame(asset);

            Assert.Throws<InvalidOperationException>(() => renderer.ApplyFrame(Frame(asset, new[] { 1f }, new byte[] { 1 }), new[] { new SiteDirtyRange(0, 1) }));
        }

        [Test]
        public void ChangedPositionRequiresNewAssetHash()
        {
            SiteAsset asset = Asset(new[] { new ContractId(5, 1) }, new[] { Vector3.zero });
            P10SiteRenderer renderer = RendererWithoutFrame(asset);
            SiteRenderFrame changed = Frame(asset, new[] { 1f }, new byte[] { 1 }, new[] { Vector3.one });

            Assert.Throws<ArgumentException>(() => renderer.ApplyFrame(changed));
            Assert.That(renderer.StaticPositionsValidated, Is.False);
        }

        [Test]
        public void InvalidDirtyRangesAreRejectedBeforeAnyDynamicMutation()
        {
            SiteAsset asset = Asset(new[] { new ContractId(5, 10), new ContractId(5, 11) }, new[] { Vector3.zero, new Vector3(30f, 0f, 0f) });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 1f, 1f }, new byte[] { 1, 1 }));
            SiteRenderFrame invalid = Frame(asset, new[] { 20f, 1f }, new byte[] { 0, 1 });

            Assert.Throws<ArgumentException>(() => renderer.ApplyFrame(invalid, new[] { new SiteDirtyRange(0, 1), new SiteDirtyRange(0, 2) }));
            Assert.That(renderer.FindNearest(Vector3.zero, out SitePickResult unchanged), Is.True);
            Assert.That(unchanged.SiteId, Is.EqualTo(new ContractId(5, 10)));
            Assert.That(renderer.MaximumRadiusMillimeters, Is.EqualTo(P10SiteRenderer.MinimumRayRadiusMillimeters));
        }

        [Test]
        public void StreamingFramesValidateStaticPositionsOnceWithoutRetainingFrames()
        {
            SiteAsset asset = Asset(new[] { new ContractId(5, 20) }, new[] { Vector3.zero });
            P10SiteRenderer renderer = RendererWithoutFrame(asset);
            renderer.ApplyFrame(Frame(asset, new[] { 1f }, new byte[] { 1 }));
            for (int index = 0; index < 100; index++)
                renderer.ApplyFrame(Frame(asset, new[] { 1f + index }, new byte[] { 1 }), new[] { new SiteDirtyRange(0, 1) });

            Assert.That(renderer.StaticPositionsValidated, Is.True);
            string rendererSource = File.ReadAllText(Path.Combine(Application.dataPath, "HiBoPXR", "Sites", "Runtime", "P10SiteRenderer.cs"));
            Assert.That(rendererSource, Does.Not.Contain("HashSet<SiteRenderFrame>"));
        }

        [Test]
        public void DirtyRangeCanShrinkTheMaximumQueryExpansion()
        {
            SiteAsset asset = Asset(new[] { new ContractId(5, 12), new ContractId(5, 13) }, new[] { Vector3.zero, new Vector3(30f, 0f, 0f) });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 100f, 1f }, new byte[] { 1, 1 }));

            renderer.ApplyFrame(Frame(asset, new[] { 1f, 1f }, new byte[] { 1, 1 }), new[] { new SiteDirtyRange(0, 1) });

            Assert.That(renderer.MaximumRadiusMillimeters, Is.EqualTo(P10SiteRenderer.MinimumRayRadiusMillimeters));
        }

        [Test]
        public void SameHashCannotAliasDifferentStaticSiteContent()
        {
            ContractId id = new(5, 2);
            SiteAsset first = Asset(new[] { id }, new[] { Vector3.zero });
            SiteAsset conflicting = new(first.Hash, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(1f, 0f, 0f), new Float3(1f, 0f, 0f)), RenderBuffer<ContractId>.TakeOwnership(new[] { id }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(1f, 0f, 0f) }));
            P10SiteRenderer renderer = Renderer(first, Frame(first, new[] { 1f }, new byte[] { 1 }));

            Assert.Throws<InvalidOperationException>(() => renderer.SetAsset(conflicting));
            Assert.That(renderer.SiteAsset, Is.SameAs(first));
        }

        [Test]
        public void SameHashCannotAliasDifferentStaticBounds()
        {
            ContractId id = new(5, 3);
            SiteAsset first = Asset(new[] { id }, new[] { Vector3.zero });
            SiteAsset conflicting = new(first.Hash, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(-1f, -1f, -1f), new Float3(1f, 1f, 1f)), RenderBuffer<ContractId>.TakeOwnership(new[] { id }), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(0f, 0f, 0f) }));
            P10SiteRenderer renderer = Renderer(first, Frame(first, new[] { 1f }, new byte[] { 1 }));

            Assert.Throws<InvalidOperationException>(() => renderer.SetAsset(conflicting));
            Assert.That(renderer.SiteAsset, Is.SameAs(first));
        }

        [Test]
        public void D3_ContainsAllSitesSharesStaticStateAndHasNoFunctionalCeiling()
        {
            SiteAsset asset = D3Asset();
            SiteRenderFrame frame = Frame(asset, Enumerable.Repeat(1f, asset.SiteIds.Count).ToArray(), Enumerable.Repeat((byte)1, asset.SiteIds.Count).ToArray());
            var renderers = new List<P10SiteRenderer>();
            for (int index = 0; index < 8; index++)
            {
                P10SiteRenderer renderer = Renderer(asset, frame);
                renderer.transform.localScale = Vector3.one * (0.5f + index * 0.25f);
                renderers.Add(renderer);
            }

            Assert.That(renderers, Has.All.Property(nameof(P10SiteRenderer.SiteCount)).EqualTo(37_500));
            Assert.That(renderers.Sum(renderer => renderer.ExpectedDrawCalls), Is.EqualTo(8));
            Assert.That(renderers.Select(renderer => renderer.SharedPositionBuffer).Distinct().Count(), Is.EqualTo(1));
            Assert.That(SiteAssetRuntimeCache.ActiveAssetCount, Is.EqualTo(1));
            Assert.That(renderers.Sum(renderer => renderer.DynamicBufferBytes), Is.EqualTo(8L * 37_500L * 16L));
        }

        [Test]
        public void D3_PickingIsExactAndP95StaysBelowFiftyMilliseconds()
        {
            SiteAsset asset = D3Asset();
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, Enumerable.Repeat(1f, 37_500).ToArray(), Enumerable.Repeat((byte)1, 37_500).ToArray()));
            const int queryCount = 2_000;
            var microseconds = new double[queryCount * 2];
            int correct = 0;
            for (int query = 0; query < queryCount; query++)
            {
                int expectedIndex = (query * 7919) % 37_500;
                Vector3 localMillimeters = ToVector3(asset.Positions[expectedIndex]);
                Vector3 world = renderer.transform.TransformPoint(localMillimeters * 0.001f);
                long start = Stopwatch.GetTimestamp();
                renderer.FindNearest(world, out SitePickResult proximity);
                microseconds[query * 2] = ElapsedMicroseconds(start);
                start = Stopwatch.GetTimestamp();
                renderer.Raycast(new Ray(world, renderer.transform.forward), 0.003f, out SitePickResult ray);
                microseconds[query * 2 + 1] = ElapsedMicroseconds(start);
                if (proximity.SiteId == asset.SiteIds[expectedIndex] && ray.SiteId == asset.SiteIds[expectedIndex])
                    correct++;
            }

            Array.Sort(microseconds);
            double p95Milliseconds = microseconds[(int)Math.Ceiling(microseconds.Length * 0.95) - 1] / 1000d;
            TestContext.WriteLine($"P10_D3_PICKING correct={correct}/{queryCount} p50_us={microseconds[microseconds.Length / 2]:F3} p95_ms={p95Milliseconds:F6} max_ms={microseconds[^1] / 1000d:F6}");
            Assert.That(correct, Is.EqualTo(queryCount));
            Assert.That(p95Milliseconds, Is.LessThan(50d));
        }

        [Test]
        public void MetadataIsAllowlistedBoundedAndClearable()
        {
            var metadata = new SiteSelectionMetadata(new SessionEpoch(new ContractId(6, 1), 1), new ContractId(6, 2), new ContractId(6, 3), new StateRevision(4), "A1", new[] { new SiteSelectionMeasurement(SiteMeasurementRole.Amplitude, 12.5f, SiteMeasurementUnit.Microvolt) }, true, false, false);
            Assert.That(metadata.SiteLabel, Is.EqualTo("A1"));
            Assert.That(metadata.Measurements, Has.Length.EqualTo(1));
            metadata.Dispose();
            Assert.That(metadata.IsCleared, Is.True);
            Assert.That(metadata.SiteLabel, Is.Null);
            Assert.That(metadata.Measurements, Is.Empty);
            Assert.Throws<ArgumentException>(() => new SiteSelectionMetadata(new SessionEpoch(new ContractId(6, 1), 1), new ContractId(6, 2), new ContractId(6, 3), new StateRevision(4), new string('x', 65), Array.Empty<SiteSelectionMeasurement>(), false, false, false));
            Assert.Throws<ArgumentException>(() => new SiteSelectionMetadata(new SessionEpoch(new ContractId(6, 1), 1), new ContractId(6, 2), new ContractId(6, 3), new StateRevision(4), "A1", Enumerable.Repeat(new SiteSelectionMeasurement(SiteMeasurementRole.Activity, 1f, SiteMeasurementUnit.Volt), 3).ToArray(), false, false, false));
            Assert.Throws<ArgumentException>(() => new SiteSelectionMeasurement(SiteMeasurementRole.Latency, 1f, SiteMeasurementUnit.Microvolt));
            Assert.Throws<ArgumentException>(() => new SiteSelectionMeasurement(SiteMeasurementRole.Amplitude, 1f, SiteMeasurementUnit.Millisecond));
        }

        [Test]
        public void SelectionRemainsPendingUntilCanonicalDesktopOutcome()
        {
            ContractId first = new(7, 1);
            ContractId second = new(7, 2);
            SiteAsset asset = Asset(new[] { first, second }, new[] { Vector3.zero, new Vector3(20f, 0f, 0f) });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 1f, 1f }, new byte[] { 1, 1 }));
            P10SiteSelectionController selection = renderer.gameObject.AddComponent<P10SiteSelectionController>();
            selection.Configure(renderer);
            SessionEpoch session = new(new ContractId(7, 3), 1);
            ContractId column = new(7, 4);
            selection.BindContext(new SiteSelectionContext(session, column, new StateRevision(1), new ScopeRevision(1)));
            Command requested = null;
            selection.SelectionRequested += value => requested = value;

            Assert.That(selection.UpdateProximityHover(Vector3.zero), Is.True);
            ContractId commandId = new(7, 5);
            Assert.That(selection.ConfirmHover(commandId, new ContractId(7, 6)), Is.True);
            Assert.That(requested.Kind, Is.EqualTo(CommandKind.SelectSite));
            Assert.That(requested.Scope, Is.EqualTo(new ScopeKey(ScopeType.Column, column)));
            Assert.That(requested.Payload.Id, Is.EqualTo(first));
            Assert.That(selection.PendingSiteId, Is.EqualTo(first));
            Assert.That(selection.CanonicalSiteId.IsValid, Is.False);

            var metadata = new SiteSelectionMetadata(session, second, column, new StateRevision(2), "B2", Array.Empty<SiteSelectionMeasurement>(), true, false, false);
            CommandOutcome outcome = CommandOutcome.Accept(commandId, new StateRevision(2), new ScopeRevision(2), Optional<ContractValue>.Some(ContractValue.FromId(second)));
            Assert.That(selection.ApplyOutcome(outcome, metadata), Is.True);
            Assert.That(selection.PendingSiteId.IsValid, Is.False);
            Assert.That(selection.CanonicalSiteId, Is.EqualTo(second));
            Assert.That(selection.Metadata, Is.SameAs(metadata));

            renderer.Clear();
            Assert.DoesNotThrow(selection.ClearSelection);
            Assert.That(metadata.IsCleared, Is.True);
        }

        [Test]
        public void LateOutcomeCannotClearOrReplaceANewerPendingSelection()
        {
            ContractId first = new(8, 1);
            ContractId second = new(8, 2);
            SessionEpoch session = new(new ContractId(8, 3), 1);
            ContractId column = new(8, 4);
            SiteAsset asset = Asset(new[] { first, second }, new[] { Vector3.zero, new Vector3(20f, 0f, 0f) });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 1f, 1f }, new byte[] { 1, 1 }));
            P10SiteSelectionController selection = renderer.gameObject.AddComponent<P10SiteSelectionController>();
            selection.Configure(renderer);
            selection.BindContext(new SiteSelectionContext(session, column, new StateRevision(1), new ScopeRevision(1)));

            selection.UpdateProximityHover(Vector3.zero);
            ContractId firstCommand = new(8, 5);
            selection.ConfirmHover(firstCommand, new ContractId(8, 6));
            selection.UpdateProximityHover(new Vector3(0.02f, 0f, 0f));
            ContractId secondCommand = new(8, 7);
            selection.ConfirmHover(secondCommand, new ContractId(8, 8));

            Assert.That(selection.ApplyOutcome(CommandOutcome.Reject(firstCommand, new ContractError(ErrorCode.StateConflict, new ContractId(8, 6), true, Optional<StateRevision>.Some(new StateRevision(1)), Optional<ScopeRevision>.Some(new ScopeRevision(1))))), Is.False);
            Assert.That(selection.PendingCommandId, Is.EqualTo(secondCommand));
            Assert.That(selection.PendingSiteId, Is.EqualTo(second));

            var staleMetadata = new SiteSelectionMetadata(session, first, column, new StateRevision(2), "stale", Array.Empty<SiteSelectionMeasurement>(), true, false, false);
            Assert.That(selection.ApplyOutcome(CommandOutcome.Accept(firstCommand, new StateRevision(2), new ScopeRevision(2)), staleMetadata), Is.False);
            Assert.That(staleMetadata.IsCleared, Is.True);
            Assert.That(selection.PendingCommandId, Is.EqualTo(secondCommand));
            Assert.That(selection.CanonicalSiteId.IsValid, Is.False);
        }

        [Test]
        public void ContextChangeClearsPendingCanonicalAndTransientMetadata()
        {
            ContractId site = new(9, 1);
            SessionEpoch session = new(new ContractId(9, 2), 1);
            ContractId column = new(9, 3);
            SiteAsset asset = Asset(new[] { site }, new[] { Vector3.zero });
            P10SiteRenderer renderer = Renderer(asset, Frame(asset, new[] { 1f }, new byte[] { 1 }));
            P10SiteSelectionController selection = renderer.gameObject.AddComponent<P10SiteSelectionController>();
            selection.Configure(renderer);
            selection.BindContext(new SiteSelectionContext(session, column, new StateRevision(1), new ScopeRevision(1)));
            selection.UpdateProximityHover(Vector3.zero);
            ContractId command = new(9, 4);
            selection.ConfirmHover(command, new ContractId(9, 5));
            var metadata = new SiteSelectionMetadata(session, site, column, new StateRevision(2), "A1", Array.Empty<SiteSelectionMeasurement>(), true, false, false);
            selection.ApplyOutcome(CommandOutcome.Accept(command, new StateRevision(2), new ScopeRevision(2)), metadata);

            selection.BindContext(new SiteSelectionContext(new SessionEpoch(new ContractId(9, 2), 2), column, new StateRevision(1), new ScopeRevision(1)));

            Assert.That(metadata.IsCleared, Is.True);
            Assert.That(selection.CanonicalSiteId.IsValid, Is.False);
            Assert.That(selection.PendingCommandId.IsValid, Is.False);
        }

        [Test]
        public void ProductionBrainInstanceAppliesSiteFramesAndForwardsP07SelectSiteCommands()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HiBoPXR/BrainInstances/Prefabs/P09BrainInstance.prefab");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            m_OwnedObjects.Add(instance);
            BrainInstanceView view = instance.GetComponent<BrainInstanceView>();
            ContractId site = new(10, 1);
            ContractId column = new(10, 2);
            SessionEpoch session = new(new ContractId(10, 3), 1);
            SiteAsset asset = Asset(new[] { site }, new[] { Vector3.zero });
            SiteRenderFrame frame = Frame(asset, new[] { 1f }, new byte[] { 1 });
            SiteSelectionContext context = new(session, column, new StateRevision(1), new ScopeRevision(1));
            Command forwarded = null;
            view.SiteSelectionRequested += command => forwarded = command;

            view.ApplySites(asset, frame, context);
            Assert.That(view.SiteSelection.UpdateProximityHover(Vector3.zero), Is.True);
            Assert.That(view.ConfirmSiteHover(new ContractId(10, 4), new ContractId(10, 5)), Is.True);

            Assert.That(view.SiteRenderer.SiteCount, Is.EqualTo(1));
            Assert.That(forwarded, Is.Not.Null);
            Assert.That(forwarded.Kind, Is.EqualTo(CommandKind.SelectSite));
            Assert.That(forwarded.Scope, Is.EqualTo(new ScopeKey(ScopeType.Column, column)));
            Assert.That(forwarded.Payload.Id, Is.EqualTo(site));
        }

        [Test]
        public void RuntimeContainsNoPerSiteObjectColliderOrCardinalityLimit()
        {
            string runtime = Path.Combine(Application.dataPath, "HiBoPXR", "Sites", "Runtime");
            string source = string.Join("\n", Directory.GetFiles(runtime, "*.cs").Select(File.ReadAllText));
            Assert.That(source, Does.Not.Contain("new GameObject"));
            Assert.That(source, Does.Not.Contain("AddComponent"));
            Assert.That(source, Does.Not.Contain("SphereCollider"));
            Assert.That(source, Does.Not.Contain("MeshRenderer"));
            Assert.That(source, Does.Not.Contain("MaximumSites"));
            Assert.That(source, Does.Not.Contain("MaxSites"));
            Assert.That(source, Does.Not.Contain("PlayerPrefs"));
            Assert.That(source, Does.Not.Contain("persistentDataPath"));
        }

        [Test]
        public void PrefabsAndD0D3ScenesOwnAllSiteSetGameObjects()
        {
            P10ProjectSetup.Validate();
            GameObject siteSet = AssetDatabase.LoadAssetAtPath<GameObject>(P10ProjectSetup.SiteSetPrefabPath);
            Assert.That(siteSet.GetComponent<P10SiteRenderer>(), Is.Not.Null);
            Assert.That(siteSet.GetComponentsInChildren<MeshRenderer>(true), Is.Empty);
            Assert.That(siteSet.GetComponentsInChildren<Collider>(true), Is.Empty);
            Material siteMaterial = AssetDatabase.LoadAssetAtPath<Material>(P10ProjectSetup.MaterialPath);
            Material opaqueBrain = AssetDatabase.LoadAssetAtPath<Material>("Assets/HiBoPXR/StaticRendering/Materials/P05SurfaceOpaque.mat");
            Material transparentBrainDepth = AssetDatabase.LoadAssetAtPath<Material>("Assets/HiBoPXR/StaticRendering/Materials/P05SurfaceTransparentDepth.mat");
            Assert.That(siteMaterial.renderQueue, Is.GreaterThan(opaqueBrain.renderQueue).And.LessThan(transparentBrainDepth.renderQueue));
            GameObject d3 = AssetDatabase.LoadAssetAtPath<GameObject>(P10ProjectSetup.D3PrefabPath);
            Assert.That(d3.GetComponentsInChildren<P10SiteRenderer>(true), Has.Length.EqualTo(8));
            Assert.That(d3.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(9));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(P10ProjectSetup.D0ScenePath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(P10ProjectSetup.D3ScenePath), Is.Not.Null);
        }

        private P10SiteRenderer Renderer(SiteAsset asset, SiteRenderFrame frame)
        {
            P10SiteRenderer renderer = RendererWithoutFrame(asset);
            renderer.ApplyFrame(frame);
            return renderer;
        }

        private P10SiteRenderer RendererWithoutFrame(SiteAsset asset)
        {
            Shader shader = Shader.Find("HiBoP XR/P10/Buffered Sites");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            var gameObject = new GameObject("P10 test renderer");
            P10SiteRenderer renderer = gameObject.AddComponent<P10SiteRenderer>();
            renderer.Configure(mesh, material);
            renderer.SetAsset(asset);
            m_OwnedObjects.Add(gameObject);
            m_OwnedObjects.Add(mesh);
            m_OwnedObjects.Add(material);
            return renderer;
        }

        private static SiteAsset D3Asset()
        {
            const int count = 37_500;
            var ids = new ContractId[count];
            var positions = new Vector3[count];
            for (int index = 0; index < count; index++)
            {
                int x = index % 50;
                int y = index / 50 % 30;
                int z = index / 1500;
                ids[index] = new ContractId(10, (ulong)index + 1);
                positions[index] = new Vector3((x - 24.5f) * 4.5f, (y - 14.5f) * 4.5f, (z - 12f) * 4.5f);
            }

            return Asset(ids, positions);
        }

        private static SiteAsset Asset(ContractId[] ids, Vector3[] positions)
        {
            Float3[] converted = positions.Select(value => new Float3(value.x, value.y, value.z)).ToArray();
            Vector3 minimum = positions.Aggregate(Vector3.Min);
            Vector3 maximum = positions.Aggregate(Vector3.Max);
            ulong suffix = (ulong)ids.Length + ids[0].Low * 100_000UL;
            return new SiteAsset(new AssetHash(11, 12, 13, suffix), CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(new Float3(minimum.x, minimum.y, minimum.z), new Float3(maximum.x, maximum.y, maximum.z)), RenderBuffer<ContractId>.CopyFrom(ids), RenderBuffer<Float3>.TakeOwnership(converted));
        }

        private static SiteRenderFrame Frame(SiteAsset asset, float[] radii, byte[] visibility, Vector3[] positions = null)
        {
            Float3[] framePositions = positions == null ? asset.Positions.ToArray() : positions.Select(value => new Float3(value.x, value.y, value.z)).ToArray();
            Rgba32[] colors = Enumerable.Repeat(new Rgba32(135, 38, 38, 255), radii.Length).ToArray();
            SiteRenderFlags[] flags = new SiteRenderFlags[radii.Length];
            return new SiteRenderFrame(asset.Hash, new StateRevision(1), new RenderTemporalSample(0, 0f), TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(framePositions), RenderBuffer<Rgba32>.TakeOwnership(colors), RenderBuffer<float>.CopyFrom(radii), RenderBuffer<byte>.CopyFrom(visibility), RenderBuffer<SiteRenderFlags>.TakeOwnership(flags));
        }

        private static Vector3 ToVector3(Float3 value) => new(value.X, value.Y, value.Z);

        private static double ElapsedMicroseconds(long start) => (Stopwatch.GetTimestamp() - start) * 1_000_000d / Stopwatch.Frequency;
    }
}
