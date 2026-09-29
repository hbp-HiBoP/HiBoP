using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Quest;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using NUnit.Framework;
using UnityEngine;
using CoreVolume = HBP.Core.DLL.Volume;
using RoiSphere = HBP.Data.Module3D.Sphere;
using SceneCut = HBP.Core.Object3D.Cut;
using Object = UnityEngine.Object;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class V2SceneMutationBoundaryTests
    {
        private static readonly SceneId SceneIdForT09 = new(Guid.Parse("10000000-0000-0000-0000-000000000009"));
        private static readonly IncarnationId IncarnationIdForT09 = new(Guid.Parse("20000000-0000-0000-0000-000000000009"));
        private static readonly SessionId SessionIdForT09 = new(Guid.Parse("30000000-0000-0000-0000-000000000009"));

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

        [Test]
        public void NestedHighlightChange_OnlyInvalidatesSiteRenderingInTheScene()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.IsHighlighted = true, expectGeneratorUpdate: false);
        }

        [Test]
        public void NestedLabelChange_OnlyInvalidatesSiteRenderingInTheScene()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.AddLabel("reviewed"), expectGeneratorUpdate: false);
        }

        [Test]
        public void SiteStateChangeKind_DistinguishesPresentationFromScientificMask()
        {
            var state = new SiteState();
            SiteStateChangeKind observed = SiteStateChangeKind.Other;
            state.OnChangeState.AddListener(() => observed = state.CurrentChangeKind);

            state.ApplySynchronizedState(true, false, true, Color.green, Array.Empty<string>());
            Assert.That(observed, Is.EqualTo(SiteStateChangeKind.Presentation));

            state.ApplySynchronizedState(false, true, true, Color.green, Array.Empty<string>());
            Assert.That(observed, Is.EqualTo(SiteStateChangeKind.ScientificMask));
        }

        [Test]
        public void SensitiveActivityAdmission_OrdersMutationBeforeStartAndRejectsAfterStart()
        {
            GameObject root = new("activity projection admission test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                using var boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Quest, new TestClock(0));
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.RequestActivityProjection();

                Assert.That(boundary.TryBeginSensitiveActivityOperation(out IDisposable mutationScope), Is.True);
                using (mutationScope)
                {
                    scene.InvalidateActivityField(clearRenderedActivity: false);
                    Assert.That(scene.TryBeginActivityProjection(out _), Is.False);
                }

                Assert.That(scene.TryBeginActivityProjection(out ActivityProjectionInputLease lease), Is.True);
                Assert.That(lease.InputGeneration, Is.EqualTo(scene.ActivityInputGeneration));
                Assert.That(lease.ProjectionGeneration, Is.EqualTo(scene.ProjectionGeneration));
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Computing));
                Assert.That(boundary.TryBeginSensitiveActivityOperation(out _), Is.False);

                scene.InvalidateActivityField(clearRenderedActivity: false);
                Assert.That(scene.IsCurrentActivityProjection(lease), Is.False);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.False);
            }
            finally
            {
                SetPrivateField(scene, "m_UpdatingGenerators", false);
                SetPrivateField(scene, "m_ActiveActivityProjection", null);
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AutomaticStartupProjectionRequest_IsReadyAfterPreparationAndExplicitRemovalStaysRemoved()
        {
            GameObject root = new("automatic startup projection request test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.Initialized = true;
                scene.SceneInformation.CompletelyLoaded = true;
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.SceneInformation.GeneratorNeedsUpdate = false;
                scene.SceneInformation.GeneratorUpdateRequested = false;

                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);

                Assert.That(scene.ProjectionRequested, Is.True);
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Stale));
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.False);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: true), Is.True);

                scene.SetProjectionEnabled(false);
                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);

                Assert.That(scene.ProjectionRequested, Is.False);
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Absent));
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.False);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: true), Is.False);
            }
            finally
            {
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExplicitGeneratorUpdate_RequestsProjectionWhileAutomaticStaleStateRemainsGated()
        {
            GameObject root = new("explicit generator update request test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);
                scene.InvalidateActivityField(clearRenderedActivity: false);

                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.False);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = true;
                scene.UpdateGenerator();
                Assert.That(scene.ProjectionRequested, Is.True);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.True);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.True);
            }
            finally
            {
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void QuestRecalculateProjection_RequestsAnExplicitUpdateWhenAutomaticPolicyIsDisabled()
        {
            GameObject root = new("Quest explicit projection request test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            var view = root.AddComponent<QuestAnatomyView>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);
                scene.InvalidateActivityField(clearRenderedActivity: false);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.False);

                var restoredScene = (RestoredScene)Activator.CreateInstance(typeof(RestoredScene), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, args: new object[] { scene, null, null }, culture: null);
                typeof(QuestAnatomyView).GetField("current", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, restoredScene);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = true;
                view.RecalculateProjection();

                Assert.That(scene.ProjectionRequested, Is.True);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.True);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.True);
            }
            finally
            {
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SafeSceneUpdates_ContinueWhileControlledProjectionIsComputing()
        {
            GameObject root = new("activity projection safe update test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.Initialized = true;
                scene.SceneInformation.CompletelyLoaded = true;
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.SceneInformation.SitesNeedUpdate = true;
                scene.RequestActivityProjection();
                Assert.That(scene.TryBeginActivityProjection(out _), Is.True);

                bool siteRenderingUpdated = false;
                scene.OnSitesRenderingUpdated.AddListener(() => siteRenderingUpdated = true);
                typeof(Base3DScene).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scene, null);

                Assert.That(siteRenderingUpdated, Is.True);
                Assert.That(scene.SceneInformation.SitesNeedUpdate, Is.False);
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Computing));
                Assert.That(scene.CanApplyPreparedState, Is.True);
                Assert.That(scene.CanApplyLegacyStateSnapshot, Is.False);
            }
            finally
            {
                SetPrivateField(scene, "m_UpdatingGenerators", false);
                SetPrivateField(scene, "m_ActiveActivityProjection", null);
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
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
        public void AxialCutWithSignedZeroNormal_PublishesPositionAndCheckpoint()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            float negativeZero = BitConverter.Int32BitsToSingle(int.MinValue);
            source.Cut.Normal = new Vector3(negativeZero, 0f, 1f);
            source.Cut.Orientation = CutOrientation.Axial;
            source.Rebind(updateCut: null);

            source.Cut.Position = 0.8f;

            Assert.That(source.LastProposal, Is.TypeOf<SetCutDefinition>());
            SetCutDefinition proposal = (SetCutDefinition)source.LastProposal;
            Assert.That(proposal.Position, Is.EqualTo(0.8f));
            Assert.That(BitConverter.SingleToInt32Bits(proposal.NormalX), Is.Zero);
            Assert.That(source.Boundary.CaptureCheckpoint().CutDefinitions, Has.Count.EqualTo(1));
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
            source.Timeline.Step = 5;
            source.Timeline.IsPlaying = true;
            var targetProposals = new List<V2Mutation>();
            int timelineCallbacks = 0;
            target.Boundary.MutationProposed += (_, mutation, _) => targetProposals.Add(mutation);
            target.Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                timelineCallbacks++;
                target.Cut.Position = target.Cut.Position == 0.5f ? 0.75f : 0.5f;
            });

            SetTimelineAnchor proposal = (SetTimelineAnchor)source.LastProposal;
            Assert.That(proposal.Intent, Is.EqualTo(V2TimelineAnchorIntent.Play));
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
        public void PausedTimelineAnchor_DoesNotAskTheClockEstimator()
        {
            int estimateCalls = 0;
            using var target = new Fixture("paused-estimator", V2OriginDevice.Quest, new TestClock(6000), timelineTimingEstimate: _ =>
            {
                estimateCalls++;
                return new V2TimelineAnchorTimingEstimate(5d, 0d);
            });
            var anchor = new SetTimelineAnchor(target.ColumnId, 2, false, false, 5, 1000, 1000);

            target.Boundary.Apply(anchor, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(2));
            Assert.That(estimateCalls, Is.Zero);
        }

        [TestCase(V2TimelineAnchorIntent.Pause)]
        [TestCase(V2TimelineAnchorIntent.Seek)]
        [TestCase(V2TimelineAnchorIntent.Step)]
        [TestCase(V2TimelineAnchorIntent.Loop)]
        public void NonPlayTimelineAnchors_PreserveIntentAndApplyExactIndexWithoutEstimating(V2TimelineAnchorIntent intent)
        {
            using var source = new Fixture("intent-source", V2OriginDevice.Desktop, new TestClock(1000, 1000));
            int estimateCalls = 0;
            using var target = new Fixture("intent-target", V2OriginDevice.Quest, new TestClock(6000, 1000), timelineTimingEstimate: _ =>
            {
                estimateCalls++;
                return new V2TimelineAnchorTimingEstimate(5d, 0d);
            });
            source.Timeline.CurrentIndex = 40;
            source.Timeline.Step = 2;
            source.Timeline.IsPlaying = true;
            target.Timeline.CurrentIndex = 80;
            target.Timeline.IsPlaying = true;

            switch (intent)
            {
                case V2TimelineAnchorIntent.Pause:
                    source.Timeline.IsPlaying = false;
                    break;
                case V2TimelineAnchorIntent.Seek:
                    source.Timeline.CurrentIndex = 55;
                    break;
                case V2TimelineAnchorIntent.Step:
                    source.Timeline.Step = 7;
                    break;
                case V2TimelineAnchorIntent.Loop:
                    source.Timeline.IsLooping = true;
                    break;
                default:
                    Assert.Fail("Test case must represent a non-play anchor.");
                    break;
            }

            var published = (SetTimelineAnchor)source.LastProposal;
            Assert.That(published.Intent, Is.EqualTo(intent));
            SetTimelineAnchor received = (SetTimelineAnchor)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(published));
            Assert.That(received.Intent, Is.EqualTo(intent), "The wire payload must preserve the anchor intent.");
            target.Boundary.Apply(received, V2MutationApplicationOrigin.Remote, source.LastOperationId);

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(received.Index));
            Assert.That(target.Timeline.IsPlaying, Is.EqualTo(received.Playing));
            Assert.That(target.Timeline.Step, Is.EqualTo(received.Step));
            Assert.That(target.Timeline.IsLooping, Is.EqualTo(received.Looping));
            Assert.That(estimateCalls, Is.Zero);
        }

        [Test]
        public void PlayingTimelineAnchor_WithUnusableEstimateStartsAtTransmittedFinalIndex()
        {
            int estimateCalls = 0;
            using var target = new Fixture("play-final-index", V2OriginDevice.Quest, new TestClock(6000), timelineTimingEstimate: _ =>
            {
                estimateCalls++;
                return null;
            });
            var anchor = new SetTimelineAnchor(target.ColumnId, target.Timeline.Length - 1, true, false, 5, 1000, 1000, V2TimelineAnchorIntent.Play);

            target.Boundary.Apply(anchor, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(target.Timeline.Length - 1));
            Assert.That(target.Timeline.IsPlaying, Is.True);
            Assert.That(estimateCalls, Is.EqualTo(1));
        }

        [Test]
        public void PlayingTimelineAnchor_OnlyCorrectsDriftBeyondOneSamplePlusUncertainty()
        {
            using var target = new Fixture("playing-threshold", V2OriginDevice.Quest, new TestClock(6000), timelineTimingEstimate: anchor => anchor.Index switch
            {
                10 => new V2TimelineAnchorTimingEstimate(0.1d, 0.05d),
                11 => new V2TimelineAnchorTimingEstimate(0.2d, 0.05d),
                _ => new V2TimelineAnchorTimingEstimate(0.3d, 0.05d)
            });
            target.Timeline.CurrentIndex = 10;
            target.Timeline.IsPlaying = true;

            target.Boundary.Apply(new SetTimelineAnchor(target.ColumnId, 10, true, false, 10, 1000, 1000), V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(10), "One-sample drift must not seek while playing.");

            target.Timeline.CurrentIndex = 14;
            target.Boundary.Apply(new SetTimelineAnchor(target.ColumnId, 11, true, false, 10, 1000, 1000), V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(14), "One-sample drift must not seek while the accepted estimate adds uncertainty to the threshold.");

            target.Timeline.CurrentIndex = 20;
            target.Boundary.Apply(new SetTimelineAnchor(target.ColumnId, 12, true, false, 10, 1000, 1000), V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(15), "Drift beyond one sample plus uncertainty must seek to the estimated position.");
        }

        [Test]
        public void ScenePresentationCheckpoint_AppliesTypedValuesWithoutEchoOrActivityInvalidation()
        {
            using var source = new BoundSceneFixture();
            using var target = new BoundSceneFixture();
            var sourceMutations = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => sourceMutations.Add(mutation);

            source.Scene.StrongCuts = true;
            source.Scene.SiteGain = 1.5f;
            source.Scene.AtlasManager.AtlasAlpha = 0.4f;
            source.Scene.BrainMaterials.SetAlpha(0.6f);

            Assert.That(sourceMutations.Select(mutation => mutation.Type), Is.EquivalentTo(new[]
            {
                V2OperationType.SetSceneBoolean,
                V2OperationType.SetSceneFloat,
                V2OperationType.SetSceneFloat,
                V2OperationType.SetSceneFloat
            }));

            V2SceneMutationCheckpoint captured;
            try
            {
                captured = source.Boundary.CaptureCheckpoint();
            }
            catch (Exception exception)
            {
                throw new AssertionException("checkpoint capture failed: " + exception);
            }

            byte[] encoded;
            try
            {
                encoded = V2SceneMutationCheckpointCodec.Encode(9, captured);
            }
            catch (Exception exception)
            {
                throw new AssertionException("checkpoint encoding failed: " + exception);
            }

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(encoded).Checkpoint;
            Assert.That(checkpoint.T09Records, Has.Count.EqualTo(34));
            Assert.That(checkpoint.T09Records.Select(record => record.Value.Type), Does.Contain(V2OperationType.SetSceneBoolean));
            Assert.That(checkpoint.T09Records.Select(record => record.Value).OfType<SetSceneBoolean>().Any(value => value.Property == V2SceneBooleanProperty.AutomaticCutAroundSelectedSite), Is.True);

            var targetMutations = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => targetMutations.Add(mutation);
            ResetSceneInvalidationFlags(target.Scene);
            target.Scene.SceneInformation.ProjectionGridNeedsUpdate = false;
            target.Scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
            try
            {
                target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));
            }
            catch (Exception exception)
            {
                throw new AssertionException("checkpoint apply failed: " + exception);
            }

            Assert.That(target.Scene.StrongCuts, Is.True);
            Assert.That(target.Scene.SiteGain, Is.EqualTo(1.5f));
            Assert.That(target.Scene.AtlasManager.AtlasAlpha, Is.EqualTo(0.4f));
            Assert.That(target.Scene.BrainMaterials.Alpha, Is.EqualTo(0.6f));
            Assert.That(targetMutations, Is.Empty);
            Assert.That(target.Scene.SceneInformation.GeneratorNeedsUpdate, Is.False);
            Assert.That(target.Scene.SceneInformation.ProjectionGridNeedsUpdate, Is.False);
            Assert.That(target.Scene.SceneInformation.GeometryNeedsUpdate, Is.False);
        }

        [Test]
        public void SelectionAndSitePresentation_CheckpointAppliesWithoutEchoOrActivityInvalidation()
        {
            using var source = new BoundSceneFixture();
            using var target = new BoundSceneFixture();
            var proposals = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            source.Column.IsSelected = true;
            source.Scene.SelectSiteForSynchronization(source.Column, source.Site);
            source.Site.State.IsHighlighted = true;
            source.Site.State.AddLabel("reviewed");
            source.Column.ActivityAlpha = 0.35f;

            Assert.That(proposals.Select(mutation => mutation.Type), Is.EquivalentTo(new[]
            {
                V2OperationType.SetSelectedColumn,
                V2OperationType.SetSelectedSite,
                V2OperationType.SetSiteHighlight,
                V2OperationType.SetSiteLabels,
                V2OperationType.SetActivityAlpha
            }));

            byte[] bytes = V2SceneMutationCheckpointCodec.Encode(12, source.Boundary.CaptureCheckpoint());
            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(bytes).Checkpoint;
            var appliedProposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => appliedProposals.Add(mutation);
            target.Scene.SceneInformation.ProjectionGridNeedsUpdate = false;
            target.Scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
            ulong projectionGeneration = target.Scene.ProjectionGeneration;
            ulong activityInputGeneration = target.Scene.ActivityInputGeneration;

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.Column.IsSelected, Is.True);
            Assert.That(target.Column.SelectedSite, Is.SameAs(target.Site));
            Assert.That(target.Site.State.IsHighlighted, Is.True);
            Assert.That(target.Site.State.Labels, Is.EqualTo(new[] { "reviewed" }));
            Assert.That(target.Column.ActivityAlpha, Is.EqualTo(0.35f));
            Assert.That(appliedProposals, Is.Empty);
            Assert.That(target.Scene.ProjectionGeneration, Is.EqualTo(projectionGeneration));
            Assert.That(target.Scene.ActivityInputGeneration, Is.EqualTo(activityInputGeneration));
            Assert.That(target.Scene.SceneInformation.GeneratorNeedsUpdate, Is.False);
            Assert.That(target.Scene.SceneInformation.ProjectionGridNeedsUpdate, Is.False);
            Assert.That(target.Scene.SceneInformation.SurfaceProjectionNeedsUpdate, Is.False);
        }

        [Test]
        public void PreparedSpansAndFunctionalThresholds_CheckpointApplyAtomicallyWithoutEcho()
        {
            using var source = new BoundSceneFixture();
            using var target = new BoundSceneFixture();
            var proposals = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            source.StaticColumn.StaticParameters.ApplySynchronizedSpanValues(-1f, 0.25f, 1f);
            source.DynamicColumn.DynamicParameters.ApplySynchronizedSpanValues(-2f, 0f, 3f);
            source.FmriColumn.FMRIParameters.ApplySynchronizedCalibration(0.1f, 0.4f, 0.2f, 0.8f);
            source.FmriColumn.FMRIParameters.SetHideValues(true, false, true);
            source.MegColumn.MEGParameters.ApplySynchronizedCalibration(0.05f, 0.45f, 0.3f, 0.95f);
            source.MegColumn.MEGParameters.SetHideValues(false, true, false);

            Assert.That(proposals.Count(mutation => mutation.Type == V2OperationType.SetColumnSpan), Is.EqualTo(2));
            Assert.That(proposals.Count(mutation => mutation.Type == V2OperationType.SetFunctionalDisplay), Is.EqualTo(4));

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(13, source.Boundary.CaptureCheckpoint())).Checkpoint;
            var targetProposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => targetProposals.Add(mutation);
            ulong projectionGeneration = target.Scene.ProjectionGeneration;
            ulong activityInputGeneration = target.Scene.ActivityInputGeneration;

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.StaticColumn.StaticParameters.SpanMin, Is.EqualTo(-1f));
            Assert.That(target.StaticColumn.StaticParameters.Middle, Is.EqualTo(0.25f));
            Assert.That(target.StaticColumn.StaticParameters.SpanMax, Is.EqualTo(1f));
            Assert.That(target.DynamicColumn.DynamicParameters.SpanMin, Is.EqualTo(-2f));
            Assert.That(target.DynamicColumn.DynamicParameters.Middle, Is.EqualTo(0f));
            Assert.That(target.DynamicColumn.DynamicParameters.SpanMax, Is.EqualTo(3f));
            Assert.That(target.FmriColumn.FMRIParameters.FMRINegativeCalMinFactor, Is.EqualTo(0.1f));
            Assert.That(target.FmriColumn.FMRIParameters.HideLowerValues, Is.True);
            Assert.That(target.FmriColumn.FMRIParameters.HideHigherValues, Is.True);
            Assert.That(target.MegColumn.MEGParameters.FMRIPositiveCalMaxFactor, Is.EqualTo(0.95f));
            Assert.That(target.MegColumn.MEGParameters.HideMiddleValues, Is.True);
            Assert.That(targetProposals, Is.Empty);
            Assert.That(target.Scene.ProjectionGeneration, Is.EqualTo(projectionGeneration));
            Assert.That(target.Scene.ActivityInputGeneration, Is.EqualTo(activityInputGeneration));
        }

        [Test]
        public void T09DesktopCanonicalMutations_ConvergeOnQuestDriverWithoutEchoOrActivityInvalidation()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var clock = new TestClock(0);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, clock);
            using var questDriver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var canonicals = new List<V2CanonicalMutation>();
            var questEchoes = new List<V2Mutation>();
            authority.CanonicalReady += canonical =>
            {
                canonicals.Add(canonical);
                Assert.That(questDriver.ReceiveCanonical(canonical), Is.True);
            };
            quest.Boundary.MutationProposed += (_, mutation, _) => questEchoes.Add(mutation);
            V2Mutation[] mutations = CreateT09DriverMutations(desktop, 19);
            ulong projectionGeneration = quest.Scene.ProjectionGeneration;
            ulong activityInputGeneration = quest.Scene.ActivityInputGeneration;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < mutations.Length; i++)
            {
                try
                {
                    desktop.Boundary.Apply(mutations[i], V2MutationApplicationOrigin.LocalDesktop, T09Operation(i + 1));
                }
                catch (Exception exception)
                {
                    throw new Exception($"Desktop T09 operation {i} ({mutations[i].Type}) failed: {exception}");
                }
            }

            stopwatch.Stop();
            Assert.That(canonicals, Has.Count.EqualTo(mutations.Length));
            Assert.That(authority.CanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questDriver.LastObservedCanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questEchoes, Is.Empty);
            AssertT09DriverValuesMatch(desktop, quest, 19);
            Assert.That(quest.Scene.ProjectionGeneration, Is.EqualTo(projectionGeneration));
            Assert.That(quest.Scene.ActivityInputGeneration, Is.EqualTo(activityInputGeneration));
            TestContext.WriteLine($"HBP_SYNC_T09_DESKTOP_TO_QUEST operations={mutations.Length} canonical={canonicals.Count} echoes={questEchoes.Count} elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        [Test]
        public void T09QuestProposals_ConvergeOnDesktopAuthorityWithoutEchoOrActivityInvalidation()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var clock = new TestClock(0);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, clock);
            using var questDriver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var canonicals = new List<V2CanonicalMutation>();
            var desktopEchoes = new List<V2Mutation>();
            authority.CanonicalReady += canonicals.Add;
            desktop.Boundary.MutationProposed += (_, mutation, _) => desktopEchoes.Add(mutation);
            questDriver.ProposalQueued += proposal =>
            {
                V2DesktopProposalResult accepted = authority.AcceptQuestProposal(proposal);
                Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                Assert.That(questDriver.ReceiveCanonical(accepted.CanonicalMutation), Is.False, "A matching T09 proposal is confirmed without applying it a second time.");
            };
            V2Mutation[] mutations = CreateT09DriverMutations(quest, 23);
            ulong desktopProjectionGeneration = desktop.Scene.ProjectionGeneration;
            ulong desktopActivityInputGeneration = desktop.Scene.ActivityInputGeneration;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < mutations.Length; i++)
            {
                try
                {
                    Assert.That(questDriver.ApplyOptimistic(mutations[i], T09Operation(100 + i)), Is.Not.Null);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Quest T09 operation {i} ({mutations[i].Type}) failed: {exception}");
                }
            }

            stopwatch.Stop();
            Assert.That(canonicals, Has.Count.EqualTo(mutations.Length));
            Assert.That(authority.CanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questDriver.LastObservedCanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questDriver.PendingProposalCount, Is.Zero);
            Assert.That(desktopEchoes, Is.Empty);
            AssertT09DriverValuesMatch(quest, desktop, 23);
            Assert.That(desktop.Scene.ProjectionGeneration, Is.EqualTo(desktopProjectionGeneration));
            Assert.That(desktop.Scene.ActivityInputGeneration, Is.EqualTo(desktopActivityInputGeneration));
            TestContext.WriteLine($"HBP_SYNC_T09_QUEST_TO_DESKTOP operations={mutations.Length} canonical={canonicals.Count} echoes={desktopEchoes.Count} elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        [Test]
        public void T09PreparedResourceOperations_RejectCleanlyWithoutASelectedPreparedMesh()
        {
            using var fixture = new BoundSceneFixture();

            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetSceneBoolean(V2SceneBooleanProperty.DisplayMarsAtlas, true), V2MutationApplicationOrigin.Remote, T09Operation(301)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetSceneBoolean(V2SceneBooleanProperty.DisplayJuBrainAtlas, true), V2MutationApplicationOrigin.Remote, T09Operation(302)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetIbcDifumoDisplay(true, "0", false, string.Empty, 0), V2MutationApplicationOrigin.Remote, T09Operation(303)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetIbcDifumoDisplay(false, "invalid-contrast", false, string.Empty, 0), V2MutationApplicationOrigin.Remote, T09Operation(305)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetLocalizerDisplay(true, "prepared-protocol", "prepared-data", "prepared-bloc", 0, 0f, 0.5f, 1f), V2MutationApplicationOrigin.Remote, T09Operation(304)));
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

        [Test]
        [Category("Sync.SceneFocused")]
        public void T10Checkpoint_ReconcilesCutAndRoiRostersBeforeDefinitionsAndSelection()
        {
            using var source = new BoundSceneFixture(V2OriginDevice.Desktop, cutIds: new[] { "checkpoint-created-cut" }, roiId: "checkpoint-created-roi", sphereId: "checkpoint-created-sphere", configureCutCreation: true);
            using var target = new BoundSceneFixture(V2OriginDevice.Quest, cutIds: new[] { "checkpoint-obsolete-cut" }, roiId: "checkpoint-obsolete-roi", sphereId: "checkpoint-obsolete-sphere", configureCutCreation: true);

            SceneCut sourceCut = source.Scene.Cuts.Single();
            sourceCut.Orientation = HBP.Core.Enums.CutOrientation.Custom;
            sourceCut.Flip = true;
            sourceCut.NumberOfCuts = 7;
            sourceCut.Position = 0.73f;
            sourceCut.Normal = new Vector3(0.25f, 0.5f, 0.75f);
            source.Roi.Name = "checkpoint ROI";
            source.Sphere.Position = new Vector3(1.25f, -2.5f, 3.75f);
            source.Sphere.SetInfluenceRadius(4.5f);
            source.Scene.ROIManager.SelectedROI = source.Roi;
            source.Roi.SelectSphere(0);

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(21, source.Boundary.CaptureCheckpoint())).Checkpoint;
            var proposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "checkpoint-created-cut" }));
            SceneCut restoredCut = target.Scene.Cuts.Single();
            Assert.That(restoredCut.Orientation, Is.EqualTo(HBP.Core.Enums.CutOrientation.Custom));
            Assert.That(restoredCut.Flip, Is.True);
            Assert.That(restoredCut.NumberOfCuts, Is.EqualTo(7));
            Assert.That(restoredCut.Position, Is.EqualTo(0.73f));
            Assert.That(restoredCut.Normal, Is.EqualTo(new Vector3(0.25f, 0.5f, 0.75f)));

            Assert.That(target.Scene.ROIManager.ROIs, Has.Count.EqualTo(1));
            ROI restoredRoi = target.Scene.ROIManager.ROIs.Single();
            Assert.That(restoredRoi.ID, Is.EqualTo("checkpoint-created-roi"));
            Assert.That(restoredRoi.Name, Is.EqualTo("checkpoint ROI"));
            Assert.That(restoredRoi.Spheres, Has.Count.EqualTo(1));
            Assert.That(restoredRoi.Spheres[0].ID, Is.EqualTo("checkpoint-created-sphere"));
            Assert.That(restoredRoi.Spheres[0].Position, Is.EqualTo(new Vector3(1.25f, -2.5f, 3.75f)));
            Assert.That(restoredRoi.Spheres[0].InfluenceRadius, Is.EqualTo(4.5f));
            Assert.That(target.Scene.ROIManager.SelectedROI, Is.SameAs(restoredRoi));
            Assert.That(restoredRoi.SelectedSphere, Is.SameAs(restoredRoi.Spheres[0]));
            Assert.That(proposals, Is.Empty, "Applying an authoritative checkpoint must not publish local structural echoes.");
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void RejectedNonActiveRoiDeletion_RestoresFullRosterSelectionAndQuestDriverAcceptsNextOperation()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoi(new RoiId("inactive-roi")), T09Operation(921));
            Assert.That(deletion, Is.Not.Null);
            Assert.That(quest.Scene.ROIManager.ROIs.Select(roi => roi.ID), Is.EqualTo(new[] { "t09-roi-id" }));
            Assert.That(driver.ReceiveRejection(deletion.OperationId, "roi_delete_rejected"), Is.True);

            AssertRoiState(quest, "t09-roi-id", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereFirst, BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            var nextDefinition = TestSphere("inactive-sphere-middle", -1f, 5f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("inactive-roi"), nextDefinition), T09Operation(922));
            Assert.That(next, Is.Not.Null);
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(desktop, "inactive-roi").Spheres[1].Position, Is.EqualTo(new Vector3(-1f, -0.75f, -0.5f)));
            Assert.That(FindRoi(quest, "inactive-roi").Spheres[1].Position, Is.EqualTo(new Vector3(-1f, -0.75f, -0.5f)));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void RejectedNonSelectedNonLastSphereDeletion_RestoresSelectionAndQuestDriverAcceptsNextOperation()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoiSphere(new RoiId("t09-roi-id"), new SphereId("t09-sphere-id")), T09Operation(923));
            Assert.That(deletion, Is.Not.Null);
            AssertRoiState(quest, "t09-roi-id", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ReceiveRejection(deletion.OperationId, "sphere_delete_rejected"), Is.True);

            AssertRoiState(quest, "t09-roi-id", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereFirst, BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            V2RoiSphereDefinition nextDefinition = TestSphere("t09-sphere-id", 8f, 6f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), nextDefinition), T09Operation(924));
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(desktop, "t09-roi-id").Spheres[0].Position, Is.EqualTo(new Vector3(8f, 8.25f, 8.5f)));
            Assert.That(FindRoi(quest, "t09-roi-id").Spheres[0].Position, Is.EqualTo(new Vector3(8f, 8.25f, 8.5f)));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void StaleNonActiveRoiDeletionCorrection_RestoresAuthoritativeRosterAndSelections()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "inactive-roi", "t09-sphere-id", "inactive-sphere-last");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoi(new RoiId("inactive-roi")), T09Operation(925));
            desktop.Boundary.Apply(new RenameRoi(new RoiId("inactive-roi"), "Desktop ROI"), V2MutationApplicationOrigin.LocalDesktop, T09Operation(926));
            V2DesktopProposalResult rejected = authority.AcceptQuestProposal(deletion);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(rejected.Correction, Is.Not.Null);

            V2QuestProposalDecision decoded = V2QuestProposalDecisionCodec.Decode(V2QuestProposalDecisionCodec.EncodeCorrection(rejected.Correction), SceneIdForT09, IncarnationIdForT09);
            Assert.That(driver.ReceiveCorrection(decoded.Correction), Is.True);
            AssertRoiState(quest, "inactive-roi", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereFirst, BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Desktop ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-last"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            V2RoiSphereDefinition nextDefinition = TestSphere("base-sphere-last", -7f, 7f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), nextDefinition), T09Operation(927));
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(quest, "t09-roi-id").Spheres[1].Position, Is.EqualTo(new Vector3(-7f, -6.75f, -6.5f)));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void StaleNonSelectedSphereDeletionCorrection_RestoresAuthoritativeGeometryOrderAndSelections()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "inactive-roi", "t09-sphere-id", "inactive-sphere-middle");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoiSphere(new RoiId("t09-roi-id"), new SphereId("t09-sphere-id")), T09Operation(928));
            V2RoiSphereDefinition desktopDefinition = TestSphere("t09-sphere-id", 12f, 8f);
            desktop.Boundary.Apply(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), desktopDefinition), V2MutationApplicationOrigin.LocalDesktop, T09Operation(929));
            V2DesktopProposalResult rejected = authority.AcceptQuestProposal(deletion);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(rejected.Correction, Is.Not.Null);

            V2QuestProposalDecision decoded = V2QuestProposalDecisionCodec.Decode(V2QuestProposalDecisionCodec.EncodeCorrection(rejected.Correction), SceneIdForT09, IncarnationIdForT09);
            Assert.That(driver.ReceiveCorrection(decoded.Correction), Is.True);
            AssertRoiState(quest, "inactive-roi", ExpectedRoi("t09-roi-id", "Base ROI", new[] { desktopDefinition, BaseSphereLast }, "t09-sphere-id"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            V2RoiSphereDefinition nextDefinition = TestSphere("t09-sphere-id", -12f, 9f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), nextDefinition), T09Operation(930));
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(quest, "t09-roi-id").Spheres[0].Position, Is.EqualTo(new Vector3(-12f, -11.75f, -11.5f)));
        }

        private static readonly V2RoiSphereDefinition BaseSphereFirst = TestSphere("t09-sphere-id", 1f, 2f);
        private static readonly V2RoiSphereDefinition BaseSphereLast = TestSphere("base-sphere-last", 3f, 3f);
        private static readonly V2RoiSphereDefinition InactiveSphereFirst = TestSphere("inactive-sphere-first", 5f, 4f);
        private static readonly V2RoiSphereDefinition InactiveSphereMiddle = TestSphere("inactive-sphere-middle", 7f, 5f);
        private static readonly V2RoiSphereDefinition InactiveSphereLast = TestSphere("inactive-sphere-last", 9f, 6f);

        private static V2RoiSphereDefinition TestSphere(string id, float x, float radius) => new(new SphereId(id), x, x + 0.25f, x + 0.5f, radius);

        private static void SeedDeletionRois(BoundSceneFixture fixture, string activeRoiId, string baseSelectedSphereId, string inactiveSelectedSphereId)
        {
            var baseRoiId = new RoiId("t09-roi-id");
            fixture.Boundary.Apply(new RenameRoi(baseRoiId, "Base ROI"), V2MutationApplicationOrigin.Remote, T09Operation(931));
            fixture.Boundary.Apply(new SetRoiSphereDefinition(baseRoiId, BaseSphereFirst), V2MutationApplicationOrigin.Remote, T09Operation(932));
            fixture.Boundary.Apply(new CreateRoiSphere(baseRoiId, BaseSphereLast, 1), V2MutationApplicationOrigin.Remote, T09Operation(933));
            fixture.Boundary.Apply(new CreateRoi(new RoiId("inactive-roi"), "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, 1), V2MutationApplicationOrigin.Remote, T09Operation(934));
            fixture.Boundary.Apply(new SetSelectedRoiSphere(baseRoiId.Value, baseSelectedSphereId), V2MutationApplicationOrigin.Remote, T09Operation(935));
            fixture.Boundary.Apply(new SetSelectedRoiSphere("inactive-roi", inactiveSelectedSphereId), V2MutationApplicationOrigin.Remote, T09Operation(936));
            fixture.Boundary.Apply(new SetActiveRoi(new RoiId(activeRoiId)), V2MutationApplicationOrigin.Remote, T09Operation(937));
        }

        private static ROI FindRoi(BoundSceneFixture fixture, string roiId) => fixture.Scene.ROIManager.ROIs.Single(roi => roi.ID == roiId);

        private static RoiStateExpectation ExpectedRoi(string roiId, string name, V2RoiSphereDefinition[] spheres, string selectedSphereId) => new(roiId, name, spheres, selectedSphereId);

        private static void AssertRoiState(BoundSceneFixture fixture, string activeRoiId, params RoiStateExpectation[] expected)
        {
            Assert.That(fixture.Scene.ROIManager.ROIs.Select(roi => roi.ID), Is.EqualTo(expected.Select(roi => roi.RoiId)), "ROI identity and order");
            Assert.That(fixture.Scene.ROIManager.SelectedROI?.ID, Is.EqualTo(activeRoiId), "active ROI identity");
            for (int i = 0; i < expected.Length; i++)
            {
                ROI roi = fixture.Scene.ROIManager.ROIs[i];
                RoiStateExpectation expectedRoi = expected[i];
                Assert.That(roi.Name, Is.EqualTo(expectedRoi.Name), expectedRoi.RoiId + " name");
                Assert.That(roi.Spheres.Select(sphere => sphere.ID), Is.EqualTo(expectedRoi.Spheres.Select(sphere => sphere.SphereId.Value)), expectedRoi.RoiId + " sphere identity and order");
                for (int sphereIndex = 0; sphereIndex < expectedRoi.Spheres.Length; sphereIndex++)
                {
                    RoiSphere sphere = roi.Spheres[sphereIndex];
                    V2RoiSphereDefinition definition = expectedRoi.Spheres[sphereIndex];
                    Assert.That(sphere.Position, Is.EqualTo(new Vector3(definition.X, definition.Y, definition.Z)), definition.SphereId.Value + " position");
                    Assert.That(sphere.InfluenceRadius, Is.EqualTo(definition.InfluenceRadius), definition.SphereId.Value + " radius");
                }

                int selectedSphereIndex = roi.SelectedSphereID;
                string selectedSphereId = selectedSphereIndex >= 0 && selectedSphereIndex < roi.Spheres.Count ? roi.Spheres[selectedSphereIndex].ID : null;
                Assert.That(selectedSphereId, Is.EqualTo(expectedRoi.SelectedSphereId), expectedRoi.RoiId + " selected sphere identity");
            }
        }

        private sealed class RoiStateExpectation
        {
            public string RoiId { get; }
            public string Name { get; }
            public V2RoiSphereDefinition[] Spheres { get; }
            public string SelectedSphereId { get; }

            public RoiStateExpectation(string roiId, string name, V2RoiSphereDefinition[] spheres, string selectedSphereId)
            {
                RoiId = roiId;
                Name = name;
                Spheres = spheres;
                SelectedSphereId = selectedSphereId;
            }
        }

        [Test]
        public void T10CutIdentity_SurvivesNonLastDeletionReorderAndBothDriverDirections()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, seedCuts: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, seedCuts: true);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var questScheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var questDriver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, questScheduler);
            int questProposals = 0;
            int desktopProposals = 0;
            int questCutEffects = 0;
            int desktopCutEffects = 0;
            quest.Boundary.MutationProposed += (_, _, device) =>
            {
                if (device == V2OriginDevice.Quest) questProposals++;
            };
            desktop.Boundary.MutationProposed += (_, _, device) =>
            {
                if (device == V2OriginDevice.Desktop) desktopProposals++;
            };
            quest.Scene.OnModifyPlanesCuts.AddListener(() => questCutEffects++);
            desktop.Scene.OnModifyPlanesCuts.AddListener(() => desktopCutEffects++);
            authority.CanonicalReady += canonical => questDriver.ReceiveCanonical(canonical);
            questDriver.ProposalQueued += proposal => Assert.That(authority.AcceptQuestProposal(proposal).Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));

            Assert.That(questDriver.ApplyOptimistic(new DeleteCut(new CutId("t10-cut-middle")), T09Operation(811)), Is.Not.Null);
            Assert.That(quest.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-first", "t10-cut-last" }));
            Assert.That(desktop.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-first", "t10-cut-last" }));

            Assert.That(questDriver.ApplyOptimistic(new SetCutOrder(new[] { new CutId("t10-cut-last"), new CutId("t10-cut-first") }), T09Operation(812)), Is.Not.Null);
            Assert.That(quest.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-last", "t10-cut-first" }));
            Assert.That(desktop.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-last", "t10-cut-first" }));

            var lastDefinition = new SetCutDefinition(new CutId("t10-cut-last"), V2CutOrientation.Custom, true, 7, 0.7f, 0.25f, 0.5f, 0.75f);
            Assert.That(questDriver.ApplyOptimistic(lastDefinition, T09Operation(813)), Is.Not.Null);
            Assert.That(quest.Scene.Cuts[0].ID, Is.EqualTo("t10-cut-last"));
            Assert.That(quest.Scene.Cuts[0].Flip, Is.True);
            Assert.That(desktop.Scene.Cuts[0].ID, Is.EqualTo("t10-cut-last"));
            Assert.That(desktop.Scene.Cuts[0].NumberOfCuts, Is.EqualTo(7));

            var firstDefinition = new SetCutDefinition(new CutId("t10-cut-first"), V2CutOrientation.Custom, false, 2, 0.3f, 0f, 1f, 0f);
            desktop.Boundary.Apply(firstDefinition, V2MutationApplicationOrigin.LocalDesktop, T09Operation(814));
            Assert.That(quest.Scene.Cuts[1].ID, Is.EqualTo("t10-cut-first"));
            Assert.That(quest.Scene.Cuts[1].Position, Is.EqualTo(0.3f));
            Assert.That(questProposals, Is.EqualTo(3), "Remote Quest operations and their canonical echoes must not emit another proposal.");
            Assert.That(desktopProposals, Is.EqualTo(1), "Quest driver applications must remain remote on Desktop; only the explicit Desktop edit is proposed.");
            Assert.That(questDriver.PendingProposalCount, Is.Zero);
            Assert.That(questCutEffects, Is.EqualTo(4), "Matching canonical echoes must not repeat cut or derived-scene effects.");
            Assert.That(desktopCutEffects, Is.EqualTo(4), "Desktop applies each accepted cause once and never re-emits its derived cut effects.");
        }

        private static V2Mutation[] CreateT09DriverMutations(BoundSceneFixture fixture, int timelineIndex)
        {
            var columnId = new ColumnId(fixture.Column.ColumnData.ID);
            var siteId = new SiteId(fixture.Site.Information.FullID);
            return new V2Mutation[]
            {
                new SetSelectedColumn(columnId),
                new SetSelectedSite(columnId, siteId),
                new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, true),
                new SetSceneBoolean(V2SceneBooleanProperty.HideBlacklistedSites, true),
                new SetSceneBoolean(V2SceneBooleanProperty.EdgeMode, true),
                new SetSceneBoolean(V2SceneBooleanProperty.BrainTransparent, true),
                new SetSceneFloat(V2SceneFloatProperty.SiteGain, 1.5f),
                new SetSceneFloat(V2SceneFloatProperty.BrainAlpha, 0.6f),
                new SetSceneFloat(V2SceneFloatProperty.AtlasAlpha, 0.4f),
                new SetSceneColor(V2SceneColorProperty.Brain, (int)ColorType.Hot),
                new SetSceneColor(V2SceneColorProperty.Cut, (int)ColorType.Warm),
                new SetSceneColor(V2SceneColorProperty.Colormap, (int)ColorType.XRain),
                new SetSiteHighlight(columnId, siteId, true),
                new SetSiteLabels(columnId, siteId, new[] { "reviewed", "T09" }),
                new SetActivityAlpha(columnId, 0.35f),
                new SetColumnSpan(new ColumnId(fixture.StaticColumn.ColumnData.ID), V2ColumnSpanKind.Static, -1f, 0.25f, 1f),
                new SetColumnSpan(new ColumnId(fixture.DynamicColumn.ColumnData.ID), V2ColumnSpanKind.Dynamic, -2f, 0f, 3f),
                new SetFunctionalDisplay(new ColumnId(fixture.FmriColumn.ColumnData.ID), V2FunctionalModality.Fmri, 0.1f, 0.4f, 0.2f, 0.8f, true, false, true),
                new SetFunctionalDisplay(new ColumnId(fixture.MegColumn.ColumnData.ID), V2FunctionalModality.Meg, 0.05f, 0.45f, 0.3f, 0.95f, false, true, false),
                new SetTimelineAnchor(new ColumnId(fixture.FmriColumn.ColumnData.ID), timelineIndex, true, true, 3, 0, 1000),
                new SetSelectedRoiSphere(fixture.Roi.ID, fixture.Sphere.ID)
            };
        }

        private static void AssertT09DriverValuesMatch(BoundSceneFixture expected, BoundSceneFixture actual, int timelineIndex)
        {
            Assert.That(actual.Column.IsSelected, Is.True);
            Assert.That(actual.Column.SelectedSite, Is.SameAs(actual.Site));
            Assert.That(actual.Scene.StrongCuts, Is.True);
            Assert.That(actual.Scene.HideBlacklistedSites, Is.True);
            Assert.That(actual.Scene.EdgeMode, Is.True);
            Assert.That(actual.Scene.IsBrainTransparent, Is.True);
            Assert.That(actual.Scene.SiteGain, Is.EqualTo(1.5f));
            Assert.That(actual.Materials.Alpha, Is.EqualTo(0.6f));
            Assert.That(actual.Scene.AtlasManager.AtlasAlpha, Is.EqualTo(0.4f));
            Assert.That(actual.Scene.BrainColor, Is.EqualTo(ColorType.Hot));
            Assert.That(actual.Scene.CutColor, Is.EqualTo(ColorType.Warm));
            Assert.That(actual.Scene.Colormap, Is.EqualTo(ColorType.XRain));
            Assert.That(actual.Scene.AtlasManager.DisplayMarsAtlas, Is.False);
            Assert.That(actual.Scene.AtlasManager.DisplayJuBrainAtlas, Is.False);
            Assert.That(actual.Site.State.IsHighlighted, Is.True);
            Assert.That(actual.Site.State.Labels, Is.EqualTo(new[] { "reviewed", "T09" }));
            Assert.That(actual.Column.ActivityAlpha, Is.EqualTo(0.35f));
            Assert.That(actual.StaticColumn.StaticParameters.SpanMin, Is.EqualTo(-1f));
            Assert.That(actual.DynamicColumn.DynamicParameters.SpanMax, Is.EqualTo(3f));
            Assert.That(actual.FmriColumn.FMRIParameters.FMRINegativeCalMinFactor, Is.EqualTo(0.1f));
            Assert.That(actual.FmriColumn.FMRIParameters.HideLowerValues, Is.True);
            Assert.That(actual.MegColumn.MEGParameters.FMRIPositiveCalMaxFactor, Is.EqualTo(0.95f));
            Assert.That(actual.MegColumn.MEGParameters.HideMiddleValues, Is.True);
            Assert.That(actual.FmriColumn.Timeline.CurrentIndex, Is.EqualTo(timelineIndex));
            Assert.That(actual.FmriColumn.Timeline.IsPlaying, Is.True);
            Assert.That(actual.FmriColumn.Timeline.IsLooping, Is.True);
            Assert.That(actual.FmriColumn.Timeline.Step, Is.EqualTo(3));
            Assert.That(actual.Roi.SelectedSphereID, Is.EqualTo(0));
            Assert.That(actual.Roi.SelectedSphere, Is.SameAs(actual.Sphere));
            Assert.That(actual.Site.State.Color, Is.EqualTo(expected.Site.State.Color));
        }

        private static OperationId T09Operation(int value) => new(Guid.Parse($"40000000-0000-0000-0000-{value:X12}"));

        private sealed class Fixture : IDisposable
        {
            private readonly Action<SceneCut> m_UpdateCut;
            private readonly IMonotonicClock m_Clock;
            private readonly V2OriginDevice m_LocalOrigin;
            private readonly Func<SetTimelineAnchor, double?> m_TimelineAgeSeconds;
            private readonly Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> m_TimelineTimingEstimate;

            public SiteState State { get; } = new SiteState();
            public SceneCut Cut { get; } = new SceneCut();
            public TestTimeline Timeline { get; } = new TestTimeline(100);
            public V2SceneMutationBoundary Boundary { get; private set; }
            public V2Mutation LastProposal { get; private set; }
            public OperationId LastOperationId { get; private set; }
            public ColumnId ColumnId { get; }
            public SiteId SiteId { get; }

            public Fixture(string prefix, V2OriginDevice localOrigin, long clockTicks, string siteId = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null, Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> timelineTimingEstimate = null) : this(prefix, localOrigin, new TestClock(clockTicks, 1000), siteId, timelineAgeSeconds, timelineTimingEstimate)
            {
            }

            public Fixture(string prefix, V2OriginDevice localOrigin, IMonotonicClock clock, string siteId = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null, Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> timelineTimingEstimate = null)
            {
                m_LocalOrigin = localOrigin;
                m_Clock = clock;
                m_TimelineAgeSeconds = timelineAgeSeconds;
                m_TimelineTimingEstimate = timelineTimingEstimate;
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
                Boundary = new V2SceneMutationBoundary(new[] { (State, ColumnId, SiteId) }, new[] { (Cut, new CutId(Cut.ID)) }, new[] { ((BasicTimeline)Timeline, ColumnId) }, m_LocalOrigin, m_Clock, updateCut, m_TimelineAgeSeconds, m_TimelineTimingEstimate);
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

        private sealed class BoundSceneFixture : IDisposable
        {
            public GameObject Root { get; }
            public Base3DScene Scene { get; }
            public BrainMaterials Materials { get; }
            public Column3DAnatomy Column { get; }
            public HBP.Core.Object3D.Site Site { get; }
            public Column3DStatic StaticColumn { get; }
            public TestDynamicColumn DynamicColumn { get; }
            public Column3DFMRI FmriColumn { get; }
            public Column3DMEG MegColumn { get; }
            public ROI Roi { get; }
            public RoiSphere Sphere { get; }
            private SharedMaterials SphereMaterials { get; }
            private CoreVolume CutTestVolume { get; }
            private string CutTestVolumePath { get; }
            private Mesh CutTemplateMesh { get; }
            public V2SceneMutationBoundary Boundary { get; }

            public BoundSceneFixture(V2OriginDevice localOrigin = V2OriginDevice.Quest, bool seedCuts = false, string[] cutIds = null, string roiId = "t09-roi-id", string sphereId = "t09-sphere-id", bool configureCutCreation = false)
            {
                Root = new GameObject("T09 typed scene fixture");
                Root.SetActive(false);
                Scene = Root.AddComponent<Base3DScene>();
                Materials = InitializeTestBrainMaterials(Scene);
                SetPrivateField(Scene, "m_MeshManager", Root.AddComponent<MeshManager>());
                MRIManager mriManager = Root.AddComponent<MRIManager>();
                SetPrivateField(mriManager, "m_Scene", Scene);
                SetPrivateField(Scene, "m_MRIManager", mriManager);
                if (configureCutCreation)
                {
                    CutTestVolumePath = Path.Combine(Application.temporaryCachePath, "sync-checkpoint-cut-" + Guid.NewGuid().ToString("N") + ".nii");
                    CreateMinimalNifti(CutTestVolumePath);
                    CutTestVolume = new CoreVolume();
                    if (!CutTestVolume.LoadNIFTIFile(CutTestVolumePath)) throw new InvalidOperationException("Unable to load the checkpoint cut fixture volume.");
                    mriManager.MRIs.Add(new MRI3D("checkpoint-test", CutTestVolume));
                }

                AtlasManager atlasManager = Root.AddComponent<AtlasManager>();
                SetPrivateField(Scene, "m_AtlasManager", atlasManager);
                SetPrivateField(atlasManager, "m_Scene", Scene);
                FMRIManager fmriManager = Root.AddComponent<FMRIManager>();
                SetPrivateField(Scene, "m_FMRIManager", fmriManager);
                SetPrivateField(fmriManager, "m_Scene", Scene);
                ROIManager roiManager = Root.AddComponent<ROIManager>();
                SetPrivateField(Scene, "m_ROIManager", roiManager);
                SetPrivateField(roiManager, "m_Scene", Scene);
                const string patientId = "60000000-0000-0000-0000-000000000009";
                var patient = new Patient { ID = patientId, Name = "t09-patient" };
                var columnData = new AnatomicColumn("t09-column", new BaseConfiguration(), new AnatomicConfiguration(), "t09-column-id");
                Column = Root.AddComponent<Column3DAnatomy>();
                SetAutoProperty(Column, "ColumnData", columnData);
                var siteObject = new GameObject("t09-site");
                siteObject.transform.SetParent(Root.transform, false);
                Site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
                Site.Information = new SiteInformation { Patient = patient, Name = "t09-site" };
                Site.State = new SiteState();
                SetAutoProperty(Column, "Sites", new List<HBP.Core.Object3D.Site> { Site });
                Site.OnSelectSite.AddListener(selected =>
                {
                    if (selected)
                    {
                        Column.UnselectSite();
                        SetAutoProperty(Column, "SelectedSite", Site);
                    }
                    else if (ReferenceEquals(Column.SelectedSite, Site))
                    {
                        SetAutoProperty(Column, "SelectedSite", null);
                    }

                    Column.OnSelectSite.Invoke(Column.SelectedSite);
                });
                Scene.Columns.Add(Column);
                StaticColumn = Root.AddComponent<Column3DStatic>();
                SetAutoProperty(StaticColumn, "ColumnData", NewColumnData("t09-static-column-id"));
                SetAutoProperty(StaticColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                Scene.Columns.Add(StaticColumn);
                DynamicColumn = Root.AddComponent<TestDynamicColumn>();
                SetAutoProperty(DynamicColumn, "ColumnData", NewColumnData("t09-dynamic-column-id"));
                SetAutoProperty(DynamicColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                Scene.Columns.Add(DynamicColumn);
                FmriColumn = Root.AddComponent<Column3DFMRI>();
                SetAutoProperty(FmriColumn, "ColumnData", NewColumnData("t09-fmri-column-id"));
                SetAutoProperty(FmriColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                FMRITimeline fmriTimeline = new();
                SetAutoProperty(fmriTimeline, "Length", 100);
                SetAutoProperty(FmriColumn, "Timeline", fmriTimeline);
                Scene.Columns.Add(FmriColumn);
                MegColumn = Root.AddComponent<Column3DMEG>();
                SetAutoProperty(MegColumn, "ColumnData", NewColumnData("t09-meg-column-id"));
                SetAutoProperty(MegColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                FMRITimeline megTimeline = new();
                SetAutoProperty(megTimeline, "Length", 100);
                SetAutoProperty(MegColumn, "Timeline", megTimeline);
                Scene.Columns.Add(MegColumn);
                var roiObject = new GameObject("t09-roi");
                roiObject.transform.SetParent(Root.transform, false);
                Roi = roiObject.AddComponent<ROI>();
                Roi.ID = roiId;
                SetAutoProperty(Roi, "SelectedSphereID", -1);
                GameObject sphereObject = new("t09-sphere");
                sphereObject.transform.SetParent(Root.transform, false);
                sphereObject.AddComponent<MeshRenderer>();
                Sphere = sphereObject.AddComponent<RoiSphere>();
                Sphere.ID = sphereId;
                SphereMaterials = ScriptableObject.CreateInstance<SharedMaterials>();
                SetPrivateField(Sphere, "m_SharedMaterials", SphereMaterials);
                SetAutoProperty(Roi, "Spheres", new List<RoiSphere> { Sphere });
                SetAutoProperty(roiManager, "ROIs", new List<ROI> { Roi });
                var prefabContainer = new GameObject("T09 fixture prefab container");
                prefabContainer.transform.SetParent(Root.transform, false);
                prefabContainer.SetActive(false);
                var spherePrefab = new GameObject("T09 fixture sphere prefab");
                spherePrefab.transform.SetParent(prefabContainer.transform, false);
                spherePrefab.AddComponent<MeshFilter>();
                spherePrefab.AddComponent<MeshRenderer>();
                spherePrefab.AddComponent<SphereCollider>();
                SetPrivateField(spherePrefab.AddComponent<RoiSphere>(), "m_SharedMaterials", SphereMaterials);
                SetPrivateField(Roi, "m_SpherePrefab", spherePrefab);
                var roiPrefab = new GameObject("T09 fixture ROI prefab");
                roiPrefab.transform.SetParent(prefabContainer.transform, false);
                SetPrivateField(roiPrefab.AddComponent<ROI>(), "m_SpherePrefab", spherePrefab);
                DisplayedObjects displayedObjects = Root.AddComponent<DisplayedObjects>();
                SetPrivateField(displayedObjects, "m_Scene", Scene);
                SetPrivateField(displayedObjects, "m_ROIParent", Root.transform);
                SetPrivateField(displayedObjects, "m_ROIPrefab", roiPrefab);
                SetAutoProperty(displayedObjects, "Brain", NewRendererObject("t09-brain"));
                SetAutoProperty(displayedObjects, "SimplifiedBrain", NewRendererObject("t09-simplified-brain"));
                SetAutoProperty(displayedObjects, "BrainCutMeshes", new List<GameObject>());
                SetPrivateField(Scene, "m_DisplayedObjects", displayedObjects);
                SetPrivateField(roiManager, "m_DisplayedObjects", displayedObjects);
                if (configureCutCreation)
                {
                    var cutPrefab = new GameObject("T10 fixture cut prefab");
                    cutPrefab.transform.SetParent(prefabContainer.transform, false);
                    CutTemplateMesh = new Mesh();
                    cutPrefab.AddComponent<MeshFilter>().sharedMesh = CutTemplateMesh;
                    cutPrefab.AddComponent<MeshRenderer>();
                    SetPrivateField(displayedObjects, "m_BrainCutMeshesParent", Root.transform);
                    SetPrivateField(displayedObjects, "m_CutPrefab", cutPrefab);
                }

                foreach (Column3D column in Scene.Columns)
                    SetAutoProperty(column, "BrainMesh", NewRendererObject("t09-column-brain-" + column.ColumnData.ID));

                string[] initialCutIds = cutIds ?? (seedCuts ? new[] { "t10-cut-first", "t10-cut-middle", "t10-cut-last" } : Array.Empty<string>());
                if (initialCutIds.Length > 0)
                {
                    // Cut identity and order tests do not need scene columns; removing one should not exercise column rendering.
                    Scene.Columns.Clear();
                    var ownedCutMeshes = (List<Mesh>)typeof(DisplayedObjects).GetField("m_OwnedCutMeshes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(displayedObjects);
                    for (int i = 0; i < initialCutIds.Length; i++)
                    {
                        var cut = new SceneCut(Vector3.zero, Vector3.right) { ID = initialCutIds[i], Index = i, Orientation = HBP.Core.Enums.CutOrientation.Custom };
                        Scene.Cuts.Add(cut);
                        displayedObjects.BrainCutMeshes.Add(NewRendererObject("t10-cut-visual-" + i));
                        ownedCutMeshes.Add(new Mesh());
                    }
                }

                Boundary = new V2SceneMutationBoundary(Scene, localOrigin, new TestClock(0));
            }

            private static AnatomicColumn NewColumnData(string id) => new AnatomicColumn(id, new BaseConfiguration(), new AnatomicConfiguration(), id);

            private GameObject NewRendererObject(string name)
            {
                var gameObject = new GameObject(name);
                gameObject.transform.SetParent(Root.transform, false);
                gameObject.AddComponent<MeshRenderer>();
                return gameObject;
            }

            public void Dispose()
            {
                Boundary.Dispose();
                CutTestVolume?.Dispose();
                DestroyTestBrainMaterials(Materials);
                Object.DestroyImmediate(SphereMaterials);
                Object.DestroyImmediate(Root);
                if (CutTemplateMesh != null) Object.DestroyImmediate(CutTemplateMesh);
                if (!string.IsNullOrEmpty(CutTestVolumePath) && File.Exists(CutTestVolumePath)) File.Delete(CutTestVolumePath);
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

        private static BrainMaterials InitializeTestBrainMaterials(Base3DScene scene)
        {
            var brainMaterials = new BrainMaterials();
            typeof(Base3DScene).GetProperty(nameof(Base3DScene.BrainMaterials)).SetValue(scene, brainMaterials);
            return brainMaterials;
        }

        private static void DestroyTestBrainMaterials(BrainMaterials brainMaterials)
        {
            foreach (string fieldName in new[] { "m_Brain", "m_TransparentBrain", "m_Cut", "m_TransparentCut" })
            {
                Material material = (Material)typeof(BrainMaterials).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(brainMaterials);
                if (material) Object.DestroyImmediate(material);
            }
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
            Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, "Field " + fieldName + " was not found on " + target.GetType().Name + ".");
            field.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            Type current = target.GetType();
            FieldInfo backingField = null;
            while (current != null && backingField == null)
            {
                backingField = current.GetField("<" + propertyName + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                current = current.BaseType;
            }

            Assert.That(backingField, Is.Not.Null, "Missing backing field for " + target.GetType().Name + "." + propertyName + ".");
            backingField.SetValue(target, value);
        }

        private sealed class TestDynamicColumn : Column3DDynamic
        {
            public override Timeline Timeline => null;
            public override Timeline ProjectionTimeline => null;

            protected override void SetActivityData()
            {
            }
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
