using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using NUnit.Framework;
using UnityEngine;
using CoreVolume = HBP.Core.DLL.Volume;
using SceneCut = HBP.Core.Object3D.Cut;
using Object = UnityEngine.Object;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class V2SceneMutationBoundaryTests
    {
        [Test]
        public void SiteColor_UsesConstantTimeTargetLookupAcrossThirtyThousandSites()
        {
            const int siteCount = 30000;
            var states = Enumerable.Range(0, siteCount).Select(_ => new SiteState()).ToArray();
            var targets = Enumerable.Range(0, siteCount).Select(index => (states[index], new ColumnId("column-" + index), new SiteId("site-" + index))).ToArray();
            using var boundary = new V2SceneMutationBoundary(targets, Array.Empty<(SceneCut, CutId)>(), Array.Empty<(BasicTimeline, ColumnId)>(), V2OriginDevice.Desktop, new TestClock(0));
            var proposals = new List<V2Mutation>();
            boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            states[siteCount - 1].Color = new Color(0.2f, 0.4f, 0.6f, 1f);

            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(proposals[0], Is.TypeOf<SetSiteColor>());
            SetSiteColor proposal = (SetSiteColor)proposals[0];
            Assert.That(proposal.ColumnId.Value, Is.EqualTo("column-29999"));
            Assert.That(proposal.FullSiteId.Value, Is.EqualTo("site-29999"));
        }

        [Test]
        public void NestedColorChange_OnlyInvalidatesSiteRenderingInTheScene()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.Color = Color.green, expectGeneratorUpdate: false);
        }

        [Test]
        public void NestedNonColorChange_InvalidatesActivityInTheSceneEvenDuringColorCallback()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.IsBlackListed = true, expectGeneratorUpdate: true);
        }

        [TestCase(CutOrientation.Axial)]
        [TestCase(CutOrientation.Coronal)]
        [TestCase(CutOrientation.Sagittal)]
        public void NonCustomCutRemoteApply_PreservesDefinitionAndTargetsSceneInvalidation(CutOrientation orientation)
        {
            GameObject root = new("non-custom cut scene test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            var mriManager = root.AddComponent<MRIManager>();
            var meshManager = root.AddComponent<MeshManager>();
            CoreVolume volume = null;
            SceneCut cut = null;
            string volumePath = Path.Combine(Application.temporaryCachePath, "sync-scene-cut-" + Guid.NewGuid().ToString("N") + ".nii");

            try
            {
                CreateMinimalNifti(volumePath);
                volume = new CoreVolume();
                Assert.That(volume.LoadNIFTIFile(volumePath), Is.True);
                mriManager.MRIs.Add(new MRI3D("MNI", volume));
                SetPrivateField(scene, "m_MRIManager", mriManager);
                SetPrivateField(scene, "m_MeshManager", meshManager);

                cut = new SceneCut { ID = "shared-cut" };
                scene.Cuts.Add(cut);
                ResetSceneInvalidationFlags(scene);
                using var boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Quest, new TestClock(1000));
                Vector3 receivedNormal = new(0.25f, 0.5f, 0.75f);
                var mutation = new SetCutDefinition(new CutId(cut.ID), (V2CutOrientation)orientation, true, 3, 0.75f, receivedNormal.x, receivedNormal.y, receivedNormal.z);

                boundary.Apply(mutation, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

                Assert.That(cut.Orientation, Is.EqualTo(orientation));
                Assert.That(cut.Flip, Is.True);
                Assert.That(cut.NumberOfCuts, Is.EqualTo(3));
                Assert.That(cut.Position, Is.EqualTo(0.75f));
                Assert.That(cut.Normal, Is.EqualTo(receivedNormal));
                Assert.That(scene.SceneInformation.CutsNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.BaseCutTexturesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.FunctionalCutTexturesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.GUICutTexturesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeometryNeedsUpdate, Is.False);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.False);
                Assert.That(scene.SceneInformation.SitesNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.CollidersNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.FunctionalSurfaceNeedsUpdate, Is.False);
            }
            finally
            {
                if (cut != null)
                {
                    scene.Cuts.Remove(cut);
                    cut.Dispose();
                }

                volume?.Dispose();
                Object.DestroyImmediate(root);
                if (File.Exists(volumePath)) File.Delete(volumePath);
            }
        }

        [Test]
        public void CutDefinition_AppliesCompleteStateAndKeepsNewestPreview()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            using var target = new Fixture("target", V2OriginDevice.Quest, 1000);
            var sourceProposals = new List<(OperationId Id, V2Mutation Mutation, V2OriginDevice Device)>();
            source.Boundary.MutationProposed += (id, mutation, device) => sourceProposals.Add((id, mutation, device));

            source.Cut.Orientation = HBP.Core.Enums.CutOrientation.Custom;
            source.Cut.Flip = true;
            source.Cut.NumberOfCuts = 321;
            source.Cut.Normal = new Vector3(0.25f, 0.5f, 0.75f);
            source.Cut.Position = 0.25f;
            source.Cut.Position = 0.75f;

            Assert.That(sourceProposals, Has.Count.EqualTo(6));
            SetCutDefinition newest = (SetCutDefinition)sourceProposals[sourceProposals.Count - 1].Mutation;
            Assert.That(newest.Position, Is.EqualTo(0.75f));
            Assert.That(newest.Orientation, Is.EqualTo(V2CutOrientation.Custom));
            Assert.That(newest.Flip, Is.True);
            Assert.That(newest.NumberOfCuts, Is.EqualTo(321));
            Assert.That(newest.NormalX, Is.EqualTo(0.25f));
            Assert.That(newest.NormalY, Is.EqualTo(0.5f));
            Assert.That(newest.NormalZ, Is.EqualTo(0.75f));

            int cutInvalidations = 0;
            target.Boundary.Dispose();
            target.Rebind(updateCut: _ => cutInvalidations++);
            target.Boundary.MutationProposed += (_, _, _) => Assert.Fail("Remote cut application echoed a mutation.");
            target.Boundary.Apply(newest, V2MutationApplicationOrigin.Remote, sourceProposals[sourceProposals.Count - 1].Id);

            Assert.That(target.Cut.Position, Is.EqualTo(0.75f));
            Assert.That(target.Cut.Orientation, Is.EqualTo(source.Cut.Orientation));
            Assert.That(target.Cut.Flip, Is.EqualTo(source.Cut.Flip));
            Assert.That(target.Cut.NumberOfCuts, Is.EqualTo(source.Cut.NumberOfCuts));
            Assert.That(target.Cut.Normal, Is.EqualTo(source.Cut.Normal));
            Assert.That(cutInvalidations, Is.EqualTo(1));
        }

        [Test]
        public void TimelineAnchor_AdvancesAtReceiverAndSuppressesNestedRemoteCallbacks()
        {
            var sourceClock = new TestClock(1000, 1000);
            var targetClock = new TestClock(6000, 1000);
            using var source = new Fixture("source", V2OriginDevice.Desktop, sourceClock);
            using var target = new Fixture("target", V2OriginDevice.Quest, targetClock, timelineAgeSeconds: _ => 5d);
            source.Timeline.CurrentIndex = 2;
            source.Timeline.IsPlaying = true;
            var targetProposals = new List<V2Mutation>();
            int timelineCallbacks = 0;
            target.Boundary.MutationProposed += (_, mutation, _) => targetProposals.Add(mutation);
            target.Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                timelineCallbacks++;
                target.Cut.Position = target.Cut.Position == 0.5f ? 0.75f : 0.5f;
            });

            source.Timeline.Step = 5;
            SetTimelineAnchor proposal = (SetTimelineAnchor)source.LastProposal;
            target.Boundary.Apply(proposal, V2MutationApplicationOrigin.Remote, source.LastOperationId);

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(27));
            Assert.That(target.Timeline.IsPlaying, Is.True);
            Assert.That(target.Timeline.Step, Is.EqualTo(5));
            Assert.That(timelineCallbacks, Is.EqualTo(1));
            Assert.That(targetProposals, Is.Empty);
        }

        [Test]
        public void PausedTimelineAnchor_DoesNotAdvanceWithElapsedClockEstimate()
        {
            using var target = new Fixture("paused", V2OriginDevice.Quest, new TestClock(6000), timelineAgeSeconds: _ => 5d);
            var anchor = new SetTimelineAnchor(target.ColumnId, 2, false, false, 5, 1000, 1000);

            target.Boundary.Apply(anchor, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(2));
            Assert.That(target.Timeline.IsPlaying, Is.False);
        }

        [Test]
        public void TimelineAutomaticPlayback_DoesNotCreateMutationProposals()
        {
            using var fixture = new Fixture("playback", V2OriginDevice.Desktop, 1000);
            fixture.Timeline.IsLooping = true;
            fixture.Timeline.Step = 100;
            fixture.Timeline.IsPlaying = true;
            var proposals = new List<V2Mutation>();
            int nestedCutUpdates = 0;
            fixture.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);
            fixture.Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                nestedCutUpdates++;
                fixture.Cut.Position = fixture.Cut.Position == 0.5f ? 0.75f : 0.5f;
            });
            typeof(BasicTimeline).GetField("m_TimeSinceLastUpdate", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fixture.Timeline, 0.2f);

            fixture.Timeline.Play();

            Assert.That(fixture.Timeline.CurrentIndex, Is.GreaterThan(0));
            Assert.That(nestedCutUpdates, Is.GreaterThan(0));
            Assert.That(proposals, Is.Empty);
        }

        [Test]
        public void TypedCheckpoint_ExportsAndAppliesThroughTheSameRemoteHandlers()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            using var target = new Fixture("target", V2OriginDevice.Quest, 1000);
            source.State.Color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            source.Cut.Orientation = HBP.Core.Enums.CutOrientation.Custom;
            source.Cut.Flip = true;
            source.Cut.NumberOfCuts = 128;
            source.Cut.Position = 0.25f;
            source.Cut.Normal = new Vector3(0.25f, 0.5f, 0.75f);
            source.Timeline.CurrentIndex = 12;
            source.Timeline.Step = 4;
            source.Timeline.IsLooping = true;

            V2SceneMutationCheckpoint checkpoint = source.Boundary.CaptureCheckpoint();
            Assert.That(checkpoint.SiteColors, Has.Count.EqualTo(1));
            Assert.That(checkpoint.CutDefinitions, Has.Count.EqualTo(1));
            Assert.That(checkpoint.TimelineAnchors, Has.Count.EqualTo(1));
            var targetProposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => targetProposals.Add(mutation);

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.State.Color, Is.EqualTo(source.State.Color));
            Assert.That(target.Cut.Orientation, Is.EqualTo(source.Cut.Orientation));
            Assert.That(target.Cut.Flip, Is.EqualTo(source.Cut.Flip));
            Assert.That(target.Cut.NumberOfCuts, Is.EqualTo(source.Cut.NumberOfCuts));
            Assert.That(target.Cut.Position, Is.EqualTo(source.Cut.Position));
            Assert.That(target.Cut.Normal, Is.EqualTo(source.Cut.Normal));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(source.Timeline.CurrentIndex));
            Assert.That(target.Timeline.Step, Is.EqualTo(source.Timeline.Step));
            Assert.That(target.Timeline.IsLooping, Is.EqualTo(source.Timeline.IsLooping));
            Assert.That(targetProposals, Is.Empty);
        }

        [Test]
        public void TypedCheckpoint_PrevalidatesAllTargetsBeforeApplyingAnyRecord()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            using var target = new Fixture("target", V2OriginDevice.Quest, 1000, siteId: "different-site");
            source.State.Color = Color.green;
            source.Cut.Position = 0.25f;
            V2SceneMutationCheckpoint checkpoint = source.Boundary.CaptureCheckpoint();

            Assert.Throws<KeyNotFoundException>(() => target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid())));
            Assert.That(target.Cut.Position, Is.EqualTo(0.5f));
            Assert.That(target.State.Color, Is.EqualTo(SiteState.DefaultColor));
        }

        private sealed class Fixture : IDisposable
        {
            private readonly Action<SceneCut> m_UpdateCut;
            private readonly IMonotonicClock m_Clock;
            private readonly V2OriginDevice m_LocalOrigin;
            private readonly Func<SetTimelineAnchor, double?> m_TimelineAgeSeconds;

            public SiteState State { get; } = new SiteState();
            public SceneCut Cut { get; } = new SceneCut();
            public TestTimeline Timeline { get; } = new TestTimeline(100);
            public V2SceneMutationBoundary Boundary { get; private set; }
            public V2Mutation LastProposal { get; private set; }
            public OperationId LastOperationId { get; private set; }
            public ColumnId ColumnId { get; }
            public SiteId SiteId { get; }

            public Fixture(string prefix, V2OriginDevice localOrigin, long clockTicks, string siteId = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null) : this(prefix, localOrigin, new TestClock(clockTicks, 1000), siteId, timelineAgeSeconds)
            {
            }

            public Fixture(string prefix, V2OriginDevice localOrigin, IMonotonicClock clock, string siteId = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null)
            {
                m_LocalOrigin = localOrigin;
                m_Clock = clock;
                m_TimelineAgeSeconds = timelineAgeSeconds;
                m_UpdateCut = _ => CutInvalidationCount++;
                ColumnId = new ColumnId("shared-column");
                SiteId = new SiteId(siteId ?? "shared-site");
                Cut.ID = "shared-cut";
                Rebind(m_UpdateCut);
            }

            public int CutInvalidationCount { get; private set; }

            public void Rebind(Action<SceneCut> updateCut)
            {
                Boundary?.Dispose();
                Boundary = new V2SceneMutationBoundary(new[] { (State, ColumnId, SiteId) }, new[] { (Cut, new CutId(Cut.ID)) }, new[] { ((BasicTimeline)Timeline, ColumnId) }, m_LocalOrigin, m_Clock, updateCut, m_TimelineAgeSeconds);
                Boundary.MutationProposed += (id, mutation, _) =>
                {
                    LastOperationId = id;
                    LastProposal = mutation;
                };
            }

            public void Dispose()
            {
                Boundary?.Dispose();
                Cut.Dispose();
            }
        }

        private static void AssertNestedSiteStateChangeInvalidation(Action<SiteState> nestedChange, bool expectGeneratorUpdate)
        {
            GameObject root = new("nested site state invalidation test");
            root.SetActive(false);
            try
            {
                Base3DScene scene = root.AddComponent<Base3DScene>();
                Column3D column = root.AddComponent<Column3DAnatomy>();
                HBP.Core.Object3D.Site site = root.AddComponent<HBP.Core.Object3D.Site>();
                site.State = new SiteState();
                ResetSceneInvalidationFlags(scene);

                MethodInfo sceneHandler = typeof(Base3DScene).GetMethod("OnSiteStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(sceneHandler, Is.Not.Null);
                bool nested = false;
                column.OnChangeSiteState.AddListener(_ =>
                {
                    if (nested) return;
                    nested = true;
                    nestedChange(site.State);
                });
                column.OnChangeSiteState.AddListener(changedSite => sceneHandler.Invoke(scene, new object[] { changedSite }));
                site.State.OnChangeState.AddListener(() => column.OnChangeSiteState.Invoke(site));

                site.State.Color = Color.magenta;

                Assert.That(site.State.IsColorChangeInProgress, Is.False);
                Assert.That(scene.SceneInformation.SitesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.EqualTo(expectGeneratorUpdate));
                Assert.That(scene.SceneInformation.CollidersNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.CutsNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.BaseCutTexturesNeedUpdate, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ResetSceneInvalidationFlags(Base3DScene scene)
        {
            scene.SceneInformation.GeometryNeedsUpdate = false;
            scene.SceneInformation.CutsNeedUpdate = false;
            scene.SceneInformation.BaseCutTexturesNeedUpdate = false;
            scene.SceneInformation.FunctionalCutTexturesNeedUpdate = false;
            scene.SceneInformation.GUICutTexturesNeedUpdate = false;
            scene.SceneInformation.SitesNeedUpdate = false;
            scene.SceneInformation.GeneratorNeedsUpdate = false;
            scene.SceneInformation.CollidersNeedUpdate = false;
            scene.SceneInformation.FunctionalSurfaceNeedsUpdate = false;
        }

        private static void CreateMinimalNifti(string path)
        {
            using var stream = new FileStream(path, FileMode.CreateNew);
            using var writer = new BinaryWriter(stream);
            stream.SetLength(376);
            writer.Write(348);
            stream.Position = 40;
            writer.Write((short)3);
            writer.Write((short)2);
            writer.Write((short)3);
            writer.Write((short)4);
            stream.Position = 70;
            writer.Write((short)2);
            writer.Write((short)8);
            stream.Position = 80;
            writer.Write(1f);
            writer.Write(2f);
            writer.Write(3f);
            stream.Position = 108;
            writer.Write(352f);
            stream.Position = 344;
            writer.Write(new byte[] { (byte)'n', (byte)'+', (byte)'1', 0 });
            stream.Position = 352;
            writer.Write(Enumerable.Range(0, 24).Select(i => (byte)i).ToArray());
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(Base3DScene).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class TestTimeline : BasicTimeline
        {
            public TestTimeline(int length) => Length = length;
            public override SubTimeline CurrentSubtimeline => null;
        }

        private sealed class TestClock : IMonotonicClock
        {
            public long Frequency { get; }
            public long Timestamp { get; set; }

            public TestClock(long timestamp, long frequency = 1000)
            {
                Timestamp = timestamp;
                Frequency = frequency;
            }

            public long GetTimestamp() => Timestamp;
        }
    }
}
