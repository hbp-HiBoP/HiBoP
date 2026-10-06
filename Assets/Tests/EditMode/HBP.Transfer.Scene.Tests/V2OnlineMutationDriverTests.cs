using System;
using System.Collections.Generic;
using System.Diagnostics;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using NUnit.Framework;
using UnityEngine;
using SceneCut = HBP.Core.Object3D.Cut;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class V2OnlineMutationDriverTests
    {
        private static readonly SceneId Scene = new SceneId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        private static readonly IncarnationId Incarnation = new IncarnationId(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        private static readonly SessionId Session = new SessionId(Guid.Parse("30000000-0000-0000-0000-000000000003"));

        [Test]
        public void ApplicationRetirement_ConvergesFor100000OperationsInEachDirection()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary, maximumOperations: 8, maximumKeys: 8);
            var desktopScheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Desktop) { ApplicationRetentionEnabled = true };
            var questScheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(Scene, Incarnation, quest.Boundary, questScheduler);
            authority.CanonicalReady += canonical => desktopScheduler.EnqueueMutation(canonical.Mutation, false, canonical.OperationId, canonical.CanonicalSequence);

            for (int i = 0; i < 100000; i++)
            {
                float value = (i % 251) / 251f;
                desktop.Boundary.Apply(Color("site-a", value, 0.25f, 0.5f), V2MutationApplicationOrigin.LocalDesktop, Operation(100000 + i * 2));
                DrainCanonical();
                var proposal = driver.ApplyOptimistic(Color("site-b", value, 0.5f, 0.25f), Operation(100001 + i * 2));
                Assert.That(questScheduler.TryGetNextTransmission(out V2TransmissionAttempt proposed), Is.True);
                var decision = authority.AcceptQuestProposal(proposal.OperationId, proposal.Mutation, proposal.ObservedCanonicalSequence, proposed.Frame.OriginSequence.Value);
                Assert.That(decision.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                questScheduler.Acknowledge(proposed.Frame.StreamId, proposed.Frame.ReliableFrameSequence.Value);
                DrainCanonical();
            }

            Assert.That(authority.CanonicalSequence, Is.EqualTo(200000UL));
            Assert.That(authority.State, Is.EqualTo(V2DesktopMutationAuthorityState.Active));
            Assert.That(authority.RetainedDecisionCount, Is.Zero);
            Assert.That(authority.IndexedKeyCount, Is.Zero);
            Assert.That(driver.ReceivedOperationCount, Is.Zero);
            Assert.That(driver.AppliedKeyCount, Is.Zero);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(quest.Boundary.OptimisticRollbackOrderCount, Is.Zero);
            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(quest.SiteB.Color, Is.EqualTo(desktop.SiteB.Color));
            TestContext.WriteLine("HBP_SYNC_RETENTION desktop=100000 quest=100000 retainedDecisions=0 retainedKeys=0 retainedReceived=0 retainedRollbackOrder=0");

            void DrainCanonical()
            {
                Assert.That(desktopScheduler.TryGetNextTransmission(out V2TransmissionAttempt sent), Is.True);
                V2ReliableFrame frame = sent.Frame;
                driver.ReceiveCanonical(Scene, Incarnation, frame.OperationId, frame.CanonicalSequence.Value, V2MutationPayloadCodec.Decode(frame.GetPayloadCopy()));
                driver.RecordApplicationOrigin(frame.OperationId, frame.OriginSequence.Value);
                desktopScheduler.Acknowledge(frame.StreamId, frame.ReliableFrameSequence.Value);
                Assert.That(authority.RetainedDecisionCount, Is.EqualTo(1), "Receipt ACK alone must not retire a decision.");
                foreach (OperationId id in desktopScheduler.RetireAppliedSceneOperations(frame.OriginSequence.Value)) authority.RetireOperation(id);
                authority.AdvanceConflictFloor(driver.MinimumPendingObservedSequence);
                driver.RetireCanonicalHistory(authority.RetiredCanonicalThrough, frame.OriginSequence.Value);
            }
        }

        [Test]
        public void CorrectionAtMatchingEchoVersion_ResolvesLaterPreviewAndRetainsIdentityUntilApplicationEcho()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(Scene, Incarnation, quest.Boundary, scheduler);
            var first = driver.ApplyOptimistic(Color("site-a", 0.1f, 0.2f, 0.3f), Operation(90021));
            Assert.That(scheduler.TryGetNextTransmission(out V2TransmissionAttempt sent), Is.True);
            var latest = driver.ApplyOptimistic(Color("site-a", 0.8f, 0.7f, 0.6f), Operation(90022));
            var accepted = authority.AcceptQuestProposal(first);
            driver.ReceiveCanonical(accepted.CanonicalMutation);
            driver.RetireCanonicalHistory(1);
            var rejected = authority.AcceptQuestProposal(latest);
            Assert.That(rejected.Correction.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.ReceiveCorrection(rejected.Correction), Is.True);
            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            driver.RecordApplicationOrigin(latest.OperationId, 2);
            driver.RetireCanonicalHistory(1, 1);
            Assert.That(driver.ReceivedOperationCount, Is.EqualTo(1));
            authority.RetireOperation(latest.OperationId);
            desktop.Boundary.Apply(Color("site-a", 0.4f, 0.5f, 0.6f), V2MutationApplicationOrigin.LocalDesktop, Operation(90023));
            V2MutationCorrection reused = authority.AcceptQuestProposal(latest).Correction;
            Assert.Throws<System.IO.InvalidDataException>(() => driver.ReceiveCorrection(reused));
            driver.RetireCanonicalHistory(1, 2);
            Assert.That(driver.ReceivedOperationCount, Is.Zero);
            scheduler.Acknowledge(sent.Frame.StreamId, sent.Frame.ReliableFrameSequence.Value);
        }

        [Test]
        public void RetiredProposalOrigin_IsRejectedWithoutApplyingAgain()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary, maximumOperations: 1);
            V2Mutation mutation = Color("site-a", 0.2f, 0.3f, 0.4f);
            var accepted = authority.AcceptQuestProposal(Operation(90001), mutation, 0, 1);
            authority.RetireOperation(Operation(90001));
            authority.AdvanceConflictFloor(1);
            var retry = authority.AcceptQuestProposal(Operation(90001), mutation, 0, 1);
            Assert.That(retry.RejectionCode, Is.EqualTo("retired_proposal"));
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(authority.AcceptQuestProposal(Operation(90002), mutation, 1, 2).Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
        }

        [Test]
        public void DeferredProposal_PreventsConflictFloorAdvancingPastItsObservation()
        {
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            var limits = new V2SchedulerLimits(maxSceneQueuedRecords: 2, reservedSceneControlRecords: 1);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest, limits: limits);
            using var driver = new V2QuestMutationDriver(Scene, Incarnation, quest.Boundary, scheduler);
            var first = driver.ApplyOptimistic(Color("site-a", 0.1f, 0.2f, 0.3f), Operation(90011));
            driver.ApplyOptimistic(Color("site-b", 0.2f, 0.3f, 0.4f), Operation(90012));
            Assert.That(driver.DeferredProposalCount, Is.EqualTo(1));
            driver.AdvanceCanonicalWatermark(10);
            driver.ReceiveCanonical(new V2CanonicalMutation(Scene, Incarnation, first.OperationId, 1, first.Mutation));
            Assert.That(driver.MinimumPendingObservedSequence, Is.Zero);
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(10UL));
        }

        [Test]
        public void RejectionOnlyTraffic_RetiresDecisionsWithoutCanonicalAdvance()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary, maximumOperations: 2);
            for (int i = 0; i < 10000; i++)
            {
                OperationId id = Operation(500000 + i);
                var result = authority.AcceptQuestProposal(id, Color("missing-site", 0.1f, 0.2f, 0.3f), 0, (ulong)i + 1);
                Assert.That(result.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
                authority.RetireOperation(id);
            }

            Assert.That(authority.RetainedDecisionCount, Is.Zero);
            Assert.That(authority.CanonicalSequence, Is.Zero);
            Assert.That(authority.State, Is.EqualTo(V2DesktopMutationAuthorityState.Active));
        }

        [Test]
        public void RetiredCanonicalFloor_PreservesCorrectionForPendingOptimisticValue()
        {
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var driver = CreateQuestDriver(quest, new TestClock());
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var original = Color("site-a", 0.8f, 0.2f, 0.3f);
            desktop.Boundary.Apply(original, V2MutationApplicationOrigin.LocalDesktop, Operation(91000));
            driver.ReceiveCanonical(new V2CanonicalMutation(Scene, Incarnation, Operation(91000), 10, Color("site-b", 0.4f, 0.2f, 0.3f)));
            var pending = driver.ApplyOptimistic(Color("site-a", 0.1f, 0.2f, 0.3f), Operation(91001));
            driver.RetireCanonicalHistory(10);
            Assert.That(driver.MinimumPendingObservedSequence, Is.EqualTo(10UL));
            var decision = authority.AcceptQuestProposal(pending.OperationId, pending.Mutation, 0);
            Assert.That(decision.Correction.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.ReceiveCorrection(decision.Correction), Is.True);
            Assert.That(quest.SiteA.Color.r, Is.EqualTo(0.8f));
            driver.RetireCanonicalHistory(10);
            Assert.That(driver.ReceivedOperationCount, Is.Zero);
            Assert.That(driver.ReceiveCanonical(new V2CanonicalMutation(Scene, Incarnation, Operation(91000), 10, original)), Is.False);
        }

        [Test]
        public void DesktopCanonicalMutations_ConvergeForColorCutAndTimeline()
        {
            var stopwatch = Stopwatch.StartNew();
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var canonicals = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicals.Add;
            using var driver = CreateQuestDriver(quest, new TestClock());

            desktop.Boundary.Apply(Color("site-a", 0.15f, 0.25f, 0.35f), V2MutationApplicationOrigin.LocalDesktop, Operation(101));
            desktop.Boundary.Apply(Cut(V2CutOrientation.Custom, true, 7, 0.75f, 0.25f, 0.5f, 0.75f), V2MutationApplicationOrigin.LocalDesktop, Operation(102));
            desktop.Boundary.Apply(new SetTimelineAnchor(desktop.ColumnId, 19, false, true, 3, 0, 1000), V2MutationApplicationOrigin.LocalDesktop, Operation(103));

            Assert.That(canonicals, Has.Count.EqualTo(3));
            Assert.That(canonicals[0].CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(canonicals[1].CanonicalSequence, Is.EqualTo(2UL));
            Assert.That(canonicals[2].CanonicalSequence, Is.EqualTo(3UL));
            foreach (V2CanonicalMutation canonical in canonicals)
                driver.ReceiveCanonical(canonical);

            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(quest.Cut.Orientation, Is.EqualTo(desktop.Cut.Orientation));
            Assert.That(quest.Cut.Flip, Is.EqualTo(desktop.Cut.Flip));
            Assert.That(quest.Cut.NumberOfCuts, Is.EqualTo(desktop.Cut.NumberOfCuts));
            Assert.That(quest.Cut.Position, Is.EqualTo(desktop.Cut.Position));
            Assert.That(quest.Cut.Normal, Is.EqualTo(desktop.Cut.Normal));
            Assert.That(quest.Timeline.CurrentIndex, Is.EqualTo(desktop.Timeline.CurrentIndex));
            Assert.That(quest.Timeline.IsLooping, Is.EqualTo(desktop.Timeline.IsLooping));
            Assert.That(quest.Timeline.Step, Is.EqualTo(desktop.Timeline.Step));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(3UL));
            Assert.That(authority.CanonicalSequence, Is.EqualTo(3UL));
            TestContext.WriteLine($"HBP_SYNC_T06_DESKTOP_TO_QUEST operations=3 elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        [Test]
        public void QuestOptimisticProposals_ConvergeAndMatchingEchoesDoNotApplyTwice()
        {
            var stopwatch = Stopwatch.StartNew();
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            using var driver = CreateQuestDriver(quest, new TestClock());
            int siteChanges = 0;
            int cutUpdates = 0;
            int timelineIndexChanges = 0;
            quest.SiteA.OnChangeState.AddListener(() => siteChanges++);
            driver.ProposalQueued += proposal =>
            {
                V2DesktopProposalResult result = authority.AcceptQuestProposal(proposal);
                Assert.That(result.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                driver.ReceiveCanonical(result.CanonicalMutation);
            };
            quest.CutChanged += () => cutUpdates++;
            quest.Timeline.OnUpdateCurrentIndex.AddListener(() => timelineIndexChanges++);

            Assert.That(driver.ApplyOptimistic(Color("site-a", 0.45f, 0.55f, 0.65f), Operation(201)), Is.Not.Null);
            Assert.That(driver.ApplyOptimistic(Cut(V2CutOrientation.Sagittal, true, 4, 0.6f, 0.25f, 0.5f, 0.75f), Operation(202)), Is.Not.Null);
            Assert.That(driver.ApplyOptimistic(new SetTimelineAnchor(quest.ColumnId, 23, false, false, 4, 0, 1000), Operation(203)), Is.Not.Null);

            Assert.That(siteChanges, Is.EqualTo(1));
            Assert.That(cutUpdates, Is.EqualTo(1));
            Assert.That(timelineIndexChanges, Is.EqualTo(1));
            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(quest.Cut.Position, Is.EqualTo(desktop.Cut.Position));
            Assert.That(quest.Timeline.CurrentIndex, Is.EqualTo(desktop.Timeline.CurrentIndex));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(3UL));
            TestContext.WriteLine($"HBP_SYNC_T06_QUEST_TO_DESKTOP operations=3 duplicateApplies=0 elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        [Test]
        public void LostCanonicalAcknowledgement_RetryReusesFrameAndDoesNotDoubleApply()
        {
            var clock = new TestClock();
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, clock);
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            using var driver = CreateQuestDriver(quest, clock);
            int questTimelineChanges = 0;
            int desktopTimelineChanges = 0;
            quest.Timeline.OnUpdateCurrentIndex.AddListener(() => questTimelineChanges++);
            desktop.Timeline.OnUpdateCurrentIndex.AddListener(() => desktopTimelineChanges++);

            V2QuestMutationProposal proposal = driver.ApplyOptimistic(new SetTimelineAnchor(quest.ColumnId, 31, false, false, 2, 0, 1000), Operation(301));
            Assert.That(proposal, Is.Not.Null);
            Assert.That(driver.TryGetNextTransmission(out V2TransmissionAttempt firstAttempt), Is.True);
            V2DesktopProposalResult first = authority.AcceptQuestProposal(proposal);
            Assert.That(first.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1), "The same Quest driver must retain its unacknowledged proposal across the disconnect.");

            driver.BeginDisconnectGrace();
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(driver.TryReconnect(), Is.True);
            Assert.That(driver.TryGetNextTransmission(out V2TransmissionAttempt replay), Is.True);
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(replay.Frame, Is.SameAs(firstAttempt.Frame));
            Assert.That(replay.Frame.OperationId, Is.EqualTo(proposal.OperationId));
            Assert.That(replay.Frame.ObservedCanonicalSequence, Is.EqualTo(proposal.ObservedCanonicalSequence));
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1), "Reconnect must reuse the original owner and pending proposal until canonical replay is applied.");

            V2DesktopProposalResult retry = authority.AcceptQuestProposal(proposal);
            Assert.That(retry.Outcome, Is.EqualTo(V2ProposalOutcome.Duplicate));
            Assert.That(retry.CanonicalMutation.CanonicalSequence, Is.EqualTo(first.CanonicalMutation.CanonicalSequence));
            driver.ReceiveCanonical(first.CanonicalMutation);
            driver.ReceiveCanonical(retry.CanonicalMutation);

            Assert.That(questTimelineChanges, Is.EqualTo(1));
            Assert.That(desktopTimelineChanges, Is.EqualTo(1));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(driver.AbandonedProposalCount, Is.Zero);
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL), "Replaying the Quest proposal must not create a second Desktop canonical mutation.");
            TestContext.WriteLine($"HBP_SYNC_T06_LOST_ACK owner=retained unacknowledgedProposal=1 canonicalReplay=1 duplicateApplies=0");
        }

        [Test]
        public void StaleSameKeyProposalGetsDesktopCorrectionWhileDisjointProposalIsAccepted()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            using var driver = CreateQuestDriver(quest, new TestClock());
            var queued = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += queued.Add;

            desktop.Boundary.Apply(Color("site-a", 0.9f, 0.1f, 0.2f), V2MutationApplicationOrigin.LocalDesktop, Operation(401));
            driver.ApplyOptimistic(Color("site-a", 0.1f, 0.9f, 0.2f), Operation(402));
            driver.ApplyOptimistic(Color("site-b", 0.2f, 0.3f, 0.9f), Operation(403));
            Assert.That(queued, Has.Count.EqualTo(2));
            Assert.That(queued[0].ObservedCanonicalSequence, Is.Zero);
            Assert.That(queued[1].ObservedCanonicalSequence, Is.Zero);

            V2DesktopProposalResult conflict = authority.AcceptQuestProposal(queued[0]);
            Assert.That(conflict.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(conflict.RejectionCode, Is.EqualTo("stale_sequence"));
            Assert.That(conflict.Correction.AuthoritativeMutation, Is.TypeOf<SetSiteColor>());
            Assert.That(((SetSiteColor)conflict.Correction.AuthoritativeMutation).FullSiteId, Is.EqualTo(quest.SiteAId));

            V2DesktopProposalResult disjoint = authority.AcceptQuestProposal(queued[1]);
            Assert.That(disjoint.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            driver.ReceiveCanonical(disjoint.CanonicalMutation);

            byte[] decisionBytes = V2QuestProposalDecisionCodec.EncodeCorrection(conflict.Correction);
            V2QuestProposalDecision decision = V2QuestProposalDecisionCodec.Decode(decisionBytes, Scene, Incarnation);
            Assert.That(decision.OperationId, Is.EqualTo(queued[0].OperationId));
            Assert.That(decision.RejectionCode, Is.EqualTo("stale_sequence"));
            Assert.That(decision.Correction.CanonicalSequence, Is.EqualTo(conflict.Correction.CanonicalSequence));
            Assert.That(driver.ReceiveCorrection(decision.Correction), Is.True, "A same-key conflict must replace Quest's optimistic value with Desktop's authoritative value.");

            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(quest.SiteB.Color, Is.EqualTo(desktop.SiteB.Color));
            Assert.That(authority.CanonicalSequence, Is.EqualTo(2UL));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(2UL));
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [Test]
        public void MatchingCorrectionWatermark_PreventsOlderCanonicalFromRollingBackOptimisticValue()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            using var driver = CreateQuestDriver(quest, new TestClock());
            var canonicals = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicals.Add;
            int questSiteChanges = 0;
            quest.SiteA.OnChangeState.AddListener(() => questSiteChanges++);

            desktop.Boundary.Apply(Color("site-a", 0.2f, 0.3f, 0.4f), V2MutationApplicationOrigin.LocalDesktop, Operation(451));
            desktop.Boundary.Apply(Color("site-a", 0.8f, 0.7f, 0.6f), V2MutationApplicationOrigin.LocalDesktop, Operation(452));
            V2QuestMutationProposal proposal = driver.ApplyOptimistic(Color("site-a", 0.8f, 0.7f, 0.6f), Operation(453));

            V2DesktopProposalResult result = authority.AcceptQuestProposal(proposal);
            Assert.That(result.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(result.Correction.CanonicalSequence, Is.EqualTo(2UL));
            Assert.That(driver.ReceiveCorrection(result.Correction), Is.False, "The matching optimistic value must not be applied a second time.");
            Assert.That(driver.ReceiveCanonical(canonicals[0]), Is.False, "A canonical value older than the correction watermark must be ignored.");

            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(questSiteChanges, Is.EqualTo(1));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(2UL));
        }

        [Test]
        public void InvalidTimelineProposal_IsRejectedBeforeLedgerAdmission()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock(), timelineLength: 101);
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var canonicals = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicals.Add;
            using var driver = CreateQuestDriver(quest, new TestClock());

            V2QuestMutationProposal invalid = driver.ApplyOptimistic(new SetTimelineAnchor(quest.ColumnId, 100, false, false, 1, 0, 1000), Operation(461));
            V2DesktopProposalResult rejected = authority.AcceptQuestProposal(invalid);

            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(rejected.RejectionCode, Is.EqualTo("timeline_index_out_of_range"));
            Assert.That(rejected.Correction.OperationId, Is.EqualTo(invalid.OperationId));
            Assert.That(driver.ReceiveCorrection(rejected.Correction), Is.True);
            Assert.That(quest.Timeline.CurrentIndex, Is.Zero);
            Assert.That(authority.CanonicalSequence, Is.Zero);
            Assert.That(canonicals, Is.Empty);

            V2QuestMutationProposal valid = driver.ApplyOptimistic(new SetTimelineAnchor(quest.ColumnId, 99, false, false, 1, 0, 1000), Operation(462));
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(valid);

            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(accepted.CanonicalMutation.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(desktop.Timeline.CurrentIndex, Is.EqualTo(99));
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(canonicals, Has.Count.EqualTo(1));
        }

        [Test]
        public void ResourceOrTopologyRejection_RollsBackOnlyThatOptimisticOperation()
        {
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var driver = CreateQuestDriver(quest, new TestClock());
            Color priorSiteA = quest.SiteA.Color;
            Color priorSiteB = quest.SiteB.Color;
            var rejected = new List<string>();
            driver.ProposalRejected += (_, code) => rejected.Add(code);

            V2QuestMutationProposal proposal = driver.ApplyOptimistic(Color("site-a", 0.9f, 0.1f, 0.3f), Operation(463));
            Assert.That(proposal, Is.Not.Null);
            Assert.That(quest.SiteA.Color, Is.Not.EqualTo(priorSiteA));
            Assert.That(driver.ReceiveRejection(proposal.OperationId, "topology_unavailable"), Is.True);

            Assert.That(quest.SiteA.Color, Is.EqualTo(priorSiteA));
            Assert.That(quest.SiteB.Color, Is.EqualTo(priorSiteB));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(rejected, Is.EqualTo(new[] { "topology_unavailable" }));
        }

        [Test]
        public void QuestSchedulerPressure_DefersProposalUntilCapacityReturns()
        {
            var clock = new TestClock();
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, clock);
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest, clock, new V2SchedulerLimits(maxSceneQueuedRecords: 1, reservedSceneControlRecords: 0));
            using var driver = new V2QuestMutationDriver(Scene, Incarnation, quest.Boundary, scheduler);
            int deferred = 0;
            int queued = 0;
            int notQueued = 0;
            driver.ProposalDeferred += (_, disposition) =>
            {
                Assert.That(disposition, Is.EqualTo(V2EnqueueDisposition.Backpressured));
                deferred++;
            };
            driver.ProposalQueued += proposal =>
            {
                queued++;
                V2DesktopProposalResult accepted = authority.AcceptQuestProposal(proposal);
                Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                driver.ReceiveCanonical(accepted.CanonicalMutation);
            };
            driver.ProposalNotQueued += (_, _) => notQueued++;

            Assert.That(scheduler.EnqueueMutation(Color("site-b", 0.1f, 0.2f, 0.3f), coalesciblePreview: false, operationId: Operation(471), observedCanonicalSequence: 0).Accepted, Is.True);
            V2QuestMutationProposal proposal = driver.ApplyOptimistic(Color("site-a", 0.6f, 0.5f, 0.4f), Operation(472));

            Assert.That(proposal, Is.Not.Null);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.DeferredProposalCount, Is.EqualTo(1));
            Assert.That(deferred, Is.EqualTo(1));
            Assert.That(queued, Is.Zero);

            Assert.That(driver.TryGetNextTransmission(out V2TransmissionAttempt blocker), Is.True);
            Assert.That(blocker.Frame.OperationId, Is.EqualTo(Operation(471)));
            Assert.That(driver.DeferredProposalCount, Is.EqualTo(1));

            Assert.That(driver.TryGetNextTransmission(out V2TransmissionAttempt recovered), Is.True);
            Assert.That(recovered.Frame.OperationId, Is.EqualTo(proposal.OperationId));
            Assert.That(driver.DeferredProposalCount, Is.Zero);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(queued, Is.EqualTo(1));
            Assert.That(notQueued, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
        }

        [Test]
        public void QuestLatestUnsentProposal_ReplacesSchedulerRecordAndClearsSupersededPendingId()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest, new TestClock());
            using var driver = new V2QuestMutationDriver(Scene, Incarnation, quest.Boundary, scheduler);

            V2QuestMutationProposal superseded = driver.ApplyOptimistic(Color("site-a", 0.1f, 0.2f, 0.3f), Operation(472));
            V2QuestMutationProposal latest = driver.ApplyOptimistic(Color("site-a", 0.8f, 0.7f, 0.6f), Operation(473));

            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.DeferredProposalCount, Is.Zero);
            Assert.That(scheduler.SnapshotMetrics().PendingSceneRecords, Is.EqualTo(1));
            Assert.That(scheduler.SnapshotMetrics().CoalescedPreviewCount, Is.EqualTo(1));
            Assert.That(driver.TryGetNextTransmission(out V2TransmissionAttempt transmission), Is.True);
            Assert.That(transmission.Frame.OperationId, Is.EqualTo(latest.OperationId));
            Assert.That(transmission.Frame.OperationId, Is.Not.EqualTo(superseded.OperationId));
            Assert.That(transmission.Frame.OriginSequence, Is.EqualTo(1UL));
            SetSiteColor sent = (SetSiteColor)V2MutationPayloadCodec.Decode(transmission.Frame.GetPayloadCopy());
            Assert.That(sent.Red, Is.EqualTo(0.8f));
            Assert.That(sent.Green, Is.EqualTo(0.7f));

            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(latest);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
        }

        [Test]
        public void QuestLatestDeferredProposal_ReplacesSameKeyWithoutLeakingPendingIds()
        {
            var clock = new TestClock();
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, clock);
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest, clock, new V2SchedulerLimits(maxSceneQueuedRecords: 1, reservedSceneControlRecords: 0));
            using var driver = new V2QuestMutationDriver(Scene, Incarnation, quest.Boundary, scheduler);
            Assert.That(scheduler.EnqueueMutation(Color("site-b", 0.1f, 0.2f, 0.3f), coalesciblePreview: false, operationId: Operation(474), observedCanonicalSequence: 0).Accepted, Is.True);

            V2QuestMutationProposal superseded = driver.ApplyOptimistic(Color("site-a", 0.1f, 0.2f, 0.3f), Operation(475));
            V2QuestMutationProposal latest = driver.ApplyOptimistic(Color("site-a", 0.8f, 0.7f, 0.6f), Operation(476));

            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.DeferredProposalCount, Is.EqualTo(1));
            Assert.That(driver.TryGetNextTransmission(out V2TransmissionAttempt blocker), Is.True);
            Assert.That(blocker.Frame.OperationId, Is.EqualTo(Operation(474)));
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.TryGetNextTransmission(out V2TransmissionAttempt transmission), Is.True);
            Assert.That(transmission.Frame.OperationId, Is.EqualTo(latest.OperationId));
            Assert.That(transmission.Frame.OperationId, Is.Not.EqualTo(superseded.OperationId));
            Assert.That(transmission.Frame.OriginSequence, Is.EqualTo(2UL));
            SetSiteColor sent = (SetSiteColor)V2MutationPayloadCodec.Decode(transmission.Frame.GetPayloadCopy());
            Assert.That(sent.Red, Is.EqualTo(0.8f));
            Assert.That(driver.DeferredProposalCount, Is.Zero);

            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(latest);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
        }

        [Test]
        public void DesktopLedgerOverflow_FaultsAuthorityAndRequestsSessionDisconnect()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary, maximumOperations: 1);
            int disconnectRequests = 0;
            string disconnectReason = null;
            bool sessionConnected = true;
            var canonical = new List<V2CanonicalMutation>();
            authority.SessionMustDisconnect += reason =>
            {
                disconnectRequests++;
                disconnectReason = reason;
                sessionConnected = false;
            };
            authority.CanonicalReady += canonical.Add;

            desktop.Boundary.Apply(Color("site-a", 0.1f, 0.2f, 0.3f), V2MutationApplicationOrigin.LocalDesktop, Operation(481));
            Assert.That(authority.State, Is.EqualTo(V2DesktopMutationAuthorityState.Active));
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));

            desktop.Boundary.Apply(Color("site-b", 0.8f, 0.7f, 0.6f), V2MutationApplicationOrigin.LocalDesktop, Operation(482));

            Assert.That(desktop.SiteB.Color, Is.EqualTo(new Color(0.8f, 0.7f, 0.6f, 1f)));
            Assert.That(authority.State, Is.EqualTo(V2DesktopMutationAuthorityState.Faulted));
            Assert.That(authority.FailureCode, Is.EqualTo("operation_history_full"));
            Assert.That(disconnectRequests, Is.EqualTo(1));
            Assert.That(disconnectReason, Is.EqualTo("operation_history_full"));
            Assert.That(sessionConnected, Is.False, "The session owner must close this replica connection after local sequencing fails.");
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(canonical, Has.Count.EqualTo(1));

            V2DesktopProposalResult blocked = authority.AcceptQuestProposal(Operation(483), Color("site-a", 0.4f, 0.5f, 0.6f), 1);
            Assert.That(blocked.Outcome, Is.EqualTo(V2ProposalOutcome.Overflow));
            Assert.That(blocked.RejectionCode, Is.EqualTo("authority_faulted"));
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
        }

        [Test]
        public void GraceReconnectIsSilentAndExpiryAbandonsHistoryWithoutChangingTheView()
        {
            var reconnectClock = new TestClock();
            using (var reconnectQuest = new Fixture(V2OriginDevice.Quest, reconnectClock))
            using (var reconnectDriver = CreateQuestDriver(reconnectQuest, reconnectClock))
            {
                int offlineNotices = 0;
                reconnectDriver.OfflineLocalEntered += () => offlineNotices++;
                reconnectDriver.ApplyOptimistic(Color("site-a", 0.7f, 0.6f, 0.5f), Operation(501));
                Assert.That(reconnectDriver.TryGetNextTransmission(out V2TransmissionAttempt sent), Is.True);
                reconnectDriver.BeginDisconnectGrace();
                reconnectClock.Advance(TimeSpan.FromMilliseconds(499));
                Assert.That(reconnectDriver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
                Assert.That(offlineNotices, Is.Zero);
                Assert.That(reconnectDriver.TryReconnect(), Is.True);
                Assert.That(reconnectDriver.TryGetNextTransmission(out V2TransmissionAttempt replay), Is.True);
                Assert.That(replay.Frame, Is.SameAs(sent.Frame));
                Assert.That(replay.IsReplay, Is.True);
                Assert.That(offlineNotices, Is.Zero);
                TestContext.WriteLine("HBP_SYNC_T06_GRACE reconnectAtMs=499 offlineNotice=0 sameFrame=true");
            }

            var expiryClock = new TestClock();
            using var quest = new Fixture(V2OriginDevice.Quest, expiryClock);
            using var driver = CreateQuestDriver(quest, expiryClock);
            int expiredNotices = 0;
            driver.OfflineLocalEntered += () => expiredNotices++;
            driver.ApplyOptimistic(Color("site-a", 0.3f, 0.4f, 0.8f), Operation(502));
            Assert.That(driver.TryGetNextTransmission(out _), Is.True);
            driver.BeginDisconnectGrace();
            expiryClock.Advance(TimeSpan.FromMilliseconds(500));

            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.OfflineLocal));
            Assert.That(expiredNotices, Is.EqualTo(1));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.AbandonedProposalCount, Is.EqualTo(1));
            Assert.That(quest.SiteA.Color, Is.EqualTo(new Color(0.3f, 0.4f, 0.8f, 1f)));
            Assert.That(driver.TryReconnect(), Is.False);
            Assert.That(driver.TryGetNextTransmission(out _), Is.False);
            TestContext.WriteLine("HBP_SYNC_T06_GRACE expiryAtMs=500 offlineNotice=1 abandoned=1 viewPreserved=true");
        }

        [Test]
        public void InitialPublicationJournal_ReplaysAcceptedMutationsInCanonicalOrder()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            using var driver = CreateQuestDriver(quest, new TestClock());
            var journal = new V2PublicationMutationJournal(Scene, Incarnation);
            var accepted = new List<V2CanonicalMutation>();
            authority.CanonicalReady += accepted.Add;

            desktop.Boundary.Apply(Color("site-a", 0.15f, 0.25f, 0.35f), V2MutationApplicationOrigin.LocalDesktop, Operation(601));
            desktop.Boundary.Apply(Cut(V2CutOrientation.Custom, true, 7, 0.75f, 0.25f, 0.5f, 0.75f), V2MutationApplicationOrigin.LocalDesktop, Operation(602));
            desktop.Boundary.Apply(new SetTimelineAnchor(desktop.ColumnId, 19, false, true, 3, 0, 1000), V2MutationApplicationOrigin.LocalDesktop, Operation(603));
            foreach (V2CanonicalMutation mutation in accepted)
                Assert.That(journal.TryRecord(mutation), Is.True);

            V2PublicationJournalResult replay = journal.Complete(() => throw new AssertionException("Replay must not capture a checkpoint."));
            Assert.That(replay.Disposition, Is.EqualTo(V2PublicationJournalDisposition.Replay));
            Assert.That(replay.Mutations, Has.Count.EqualTo(3));
            Assert.That(replay.Mutations[0].CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(replay.Mutations[1].CanonicalSequence, Is.EqualTo(2UL));
            Assert.That(replay.Mutations[2].CanonicalSequence, Is.EqualTo(3UL));
            foreach (V2CanonicalMutation mutation in replay.Mutations)
                driver.ReceiveCanonical(mutation);

            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(quest.Cut.Normal, Is.EqualTo(desktop.Cut.Normal));
            Assert.That(quest.Timeline.CurrentIndex, Is.EqualTo(desktop.Timeline.CurrentIndex));
        }

        [Test]
        public void InitialPublicationJournal_OverflowUsesOneTypedCheckpointFallback()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            var journal = new V2PublicationMutationJournal(Scene, Incarnation, maximumMutations: 1);
            var accepted = new List<V2CanonicalMutation>();
            authority.CanonicalReady += accepted.Add;

            desktop.Boundary.Apply(Color("site-a", 0.15f, 0.25f, 0.35f), V2MutationApplicationOrigin.LocalDesktop, Operation(611));
            desktop.Boundary.Apply(Cut(V2CutOrientation.Custom, true, 7, 0.75f, 0.25f, 0.5f, 0.75f), V2MutationApplicationOrigin.LocalDesktop, Operation(612));
            Assert.That(journal.TryRecord(accepted[0]), Is.True);
            Assert.That(journal.TryRecord(accepted[1]), Is.False);

            int checkpointCaptures = 0;
            V2PublicationJournalResult fallback = journal.Complete(() =>
            {
                checkpointCaptures++;
                return desktop.Boundary.CaptureCheckpoint();
            });
            Assert.That(fallback.Disposition, Is.EqualTo(V2PublicationJournalDisposition.Checkpoint));
            Assert.That(fallback.Mutations, Is.Empty);
            Assert.That(checkpointCaptures, Is.EqualTo(1));

            byte[] encoded = V2SceneMutationCheckpointCodec.Encode(authority.CanonicalSequence, fallback.Checkpoint);
            V2PublishedSceneCheckpoint decoded = V2SceneMutationCheckpointCodec.Decode(encoded);
            quest.Boundary.ApplyCheckpoint(decoded.Checkpoint, Operation(613));
            Assert.That(decoded.CanonicalSequence, Is.EqualTo(2UL));
            Assert.That(quest.SiteA.Color, Is.EqualTo(desktop.SiteA.Color));
            Assert.That(quest.Cut.Normal, Is.EqualTo(desktop.Cut.Normal));
        }

        [Test]
        public void InitialPublicationControl_RoundTripsLiveBarrierAndAcknowledgement()
        {
            OperationId barrier = Operation(621);
            byte[] live = V2PublicationControlCodec.EncodeLiveBarrier(37);
            Assert.That(V2PublicationControlCodec.TryDecodeLiveBarrier(live, out ulong sequence), Is.True);
            Assert.That(sequence, Is.EqualTo(37UL));

            byte[] acknowledgement = V2PublicationControlCodec.EncodeAcknowledgement(barrier);
            Assert.That(V2PublicationControlCodec.TryDecodeAcknowledgement(acknowledgement, out OperationId decoded), Is.True);
            Assert.That(decoded, Is.EqualTo(barrier));
        }

        private static V2QuestMutationDriver CreateQuestDriver(Fixture fixture, IMonotonicClock clock)
        {
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest, clock);
            return new V2QuestMutationDriver(Scene, Incarnation, fixture.Boundary, scheduler);
        }

        private static SetSiteColor Color(string site, float red, float green, float blue) => new SetSiteColor(new ColumnId("shared-column"), new SiteId(site), red, green, blue, 1f);

        [Test]
        public void ReceivedPreviews_ApplySurvivorsInOrderAndPreserveBarriers()
        {
            using var fixture = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var driver = CreateQuestDriver(fixture, new TestClock());
            var batch = new List<V2CanonicalMutation>();
            for (int i = 1; i <= 100; i++)
                batch.Add(new V2CanonicalMutation(Scene, Incarnation, Operation(7000 + i), (ulong)i, Cut(V2CutOrientation.Custom, false, 1, i / 100f, 1, 0, 0)));
            batch.Add(new V2CanonicalMutation(Scene, Incarnation, Operation(7200), 101, Color("site-a", 0.7f, 0, 0)));
            batch.Add(new V2CanonicalMutation(Scene, Incarnation, Operation(7201), 102, Cut(V2CutOrientation.Custom, false, 1, 0.2f, 1, 0, 0)));
            batch.Add(new V2CanonicalMutation(Scene, Incarnation, Operation(7202), 103, Cut(V2CutOrientation.Custom, false, 1, 0.3f, 1, 0, 0)));
            driver.ReceiveCanonicalBatch(batch);
            Assert.That(fixture.CutInvalidations, Is.EqualTo(2));
            Assert.That(fixture.Cut.Position, Is.EqualTo(0.3f));
            Assert.That(fixture.SiteA.Color.r, Is.EqualTo(0.7f));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(103UL));
            Assert.That(driver.ReceiveCanonical(batch[0]), Is.False);
        }

        [Test]
        public void FailedCanonicalApplication_CanBeRetriedWithTheSameIdentity()
        {
            using var fixture = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var driver = CreateQuestDriver(fixture, new TestClock());
            bool fail = true;
            fixture.CutChanged += () =>
            {
                if (fail) throw new InvalidOperationException("fixture apply failure");
            };
            var canonical = new V2CanonicalMutation(Scene, Incarnation, Operation(7300), 1, Cut(V2CutOrientation.Custom, false, 1, 0.7f, 1, 0, 0));
            Assert.Throws<InvalidOperationException>(() => driver.ReceiveCanonical(canonical));
            Assert.That(driver.LastObservedCanonicalSequence, Is.Zero);
            fail = false;
            Assert.That(driver.ReceiveCanonical(canonical), Is.True);
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));
        }

        [Test]
        public void TimelinePause_ProtectsItsIndexFromAnOlderSeek()
        {
            using var fixture = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var driver = CreateQuestDriver(fixture, new TestClock());
            var pause = new V2CanonicalMutation(Scene, Incarnation, Operation(7310), 3, new SetTimelineAnchor(fixture.ColumnId, 22, false, false, 1, 0, 1000, V2TimelineAnchorIntent.Pause));
            var oldSeek = new V2CanonicalMutation(Scene, Incarnation, Operation(7311), 2, new SetTimelineAnchor(fixture.ColumnId, 11, false, false, 1, 0, 1000, V2TimelineAnchorIntent.Seek));
            Assert.That(driver.ReceiveCanonical(pause), Is.True);
            Assert.That(driver.ReceiveCanonical(oldSeek), Is.False);
            Assert.That(fixture.Timeline.CurrentIndex, Is.EqualTo(22));
        }

        [Test]
        public void FailedCorrection_RetainsPendingProposalUntilApplicationSucceeds()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            using var driver = CreateQuestDriver(quest, new TestClock());
            var proposal = driver.ApplyOptimistic(Cut(V2CutOrientation.Custom, false, 1, 0.2f, 1, 0, 0), Operation(7320));
            desktop.Boundary.Apply(Cut(V2CutOrientation.Custom, false, 1, 0.7f, 1, 0, 0), V2MutationApplicationOrigin.LocalDesktop, Operation(7321));
            V2MutationCorrection correction = authority.AcceptQuestProposal(proposal).Correction;
            Assert.That(correction, Is.Not.Null);
            bool fail = true;
            quest.CutChanged += () =>
            {
                if (fail) throw new InvalidOperationException("fixture correction failure");
            };
            Assert.Throws<InvalidOperationException>(() => driver.ReceiveCorrection(correction));
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.LastObservedCanonicalSequence, Is.Zero);
            fail = false;
            Assert.That(driver.ReceiveCorrection(correction), Is.True);
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [Test]
        public void DisjointSiteBatch_DoesNotSupersedeEarlierProposalCorrection()
        {
            using var desktop = new Fixture(V2OriginDevice.Desktop, new TestClock());
            using var quest = new Fixture(V2OriginDevice.Quest, new TestClock());
            using var authority = new V2DesktopMutationAuthority(Scene, Incarnation, desktop.Boundary);
            using var driver = CreateQuestDriver(quest, new TestClock());
            SetSiteConfigurationBatch Batch(string site, float red) => new SetSiteConfigurationBatch(new[] { new V2SiteConfigurationAssignment(quest.ColumnId, new SiteId(site), false, false, red, 0, 0, 1, Array.Empty<string>()) });
            var canonicals = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicals.Add;
            var proposal = driver.ApplyOptimistic(Batch("site-a", 0.2f), Operation(7340));
            desktop.Boundary.Apply(Batch("site-a", 0.7f), V2MutationApplicationOrigin.LocalDesktop, Operation(7341));
            V2MutationCorrection correction = authority.AcceptQuestProposal(proposal).Correction;
            Assert.That(correction, Is.Not.Null);
            desktop.Boundary.Apply(Batch("site-b", 0.9f), V2MutationApplicationOrigin.LocalDesktop, Operation(7342));
            driver.ReceiveCanonical(canonicals[1]);
            Assert.That(driver.ReceiveCorrection(correction), Is.True);
            Assert.That(quest.SiteA.Color.r, Is.EqualTo(0.7f));
            Assert.That(quest.SiteB.Color.r, Is.EqualTo(0.9f));
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        private static SetCutDefinition Cut(V2CutOrientation orientation, bool flip, uint count, float position, float x, float y, float z) => new SetCutDefinition(new CutId("shared-cut"), orientation, flip, count, position, x, y, z);

        private static OperationId Operation(int value) => new OperationId(Guid.Parse($"40000000-0000-0000-0000-{value:X12}"));

        private sealed class Fixture : IDisposable
        {
            public SiteState SiteA { get; } = new SiteState();
            public SiteState SiteB { get; } = new SiteState();
            public SceneCut Cut { get; } = new SceneCut { ID = "shared-cut" };
            public TestTimeline Timeline { get; }
            public ColumnId ColumnId { get; } = new ColumnId("shared-column");
            public SiteId SiteAId { get; } = new SiteId("site-a");
            public SiteId SiteBId { get; } = new SiteId("site-b");
            public V2SceneMutationBoundary Boundary { get; }
            public int CutInvalidations { get; private set; }
            public event Action CutChanged;

            public Fixture(V2OriginDevice origin, IMonotonicClock clock, int timelineLength = 100)
            {
                Timeline = new TestTimeline(timelineLength);
                Boundary = new V2SceneMutationBoundary(new[] { (SiteA, ColumnId, SiteAId), (SiteB, ColumnId, SiteBId) }, new[] { (Cut, new CutId(Cut.ID)) }, new[] { ((BasicTimeline)Timeline, ColumnId) }, origin, clock, _ =>
                {
                    CutInvalidations++;
                    CutChanged?.Invoke();
                });
            }

            public void Dispose()
            {
                Boundary.Dispose();
                Cut.Dispose();
            }
        }

        private sealed class TestTimeline : BasicTimeline
        {
            public TestTimeline(int length) => Length = length;
            public override SubTimeline CurrentSubtimeline => null;
        }

        private sealed class TestClock : IMonotonicClock
        {
            private long m_Timestamp;

            public long Frequency => TimeSpan.TicksPerSecond;
            public long GetTimestamp() => m_Timestamp;

            public void Advance(TimeSpan duration) => m_Timestamp = checked(m_Timestamp + duration.Ticks);
        }
    }
}
