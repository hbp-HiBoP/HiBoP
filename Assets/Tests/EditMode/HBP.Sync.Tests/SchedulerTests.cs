using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using HBP.Sync.Testing;

namespace HBP.Sync.Tests
{
    [Category(SyncTestCategories.Fast)]
    public class SchedulerTests
    {
        private static readonly SceneId Scene = new SceneId(GuidFor(1));
        private static readonly IncarnationId Incarnation = new IncarnationId(GuidFor(2));

        [Test]
        public void RetentionProgress_KeepsOneImmutableFrameAndLatestPendingValue()
        {
            var scheduler = CreateScheduler(new FakeMonotonicClock());
            byte[] Progress(ulong value) => new RetentionProgress(scheduler.SessionId, Scene, Incarnation, OriginDevice.Quest, value, value, 0, value).Encode();
            scheduler.EnqueueRetentionProgress(Progress(1));
            scheduler.EnqueueRetentionProgress(Progress(2));
            var first = Next(scheduler);
            RetentionProgress.TryDecode(first.Frame.GetPayloadCopy(), out RetentionProgress initial);
            Assert.That(initial.AppliedOriginThrough, Is.EqualTo(2UL));
            for (ulong value = 3; value <= 10000; value++) scheduler.EnqueueRetentionProgress(Progress(value));
            Assert.That(scheduler.SnapshotMetrics().PendingSessionControlRecords, Is.Zero);
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
            scheduler.BeginDisconnectGrace();
            Assert.That(scheduler.TryResume(), Is.True);
            var replay = Next(scheduler);
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(replay.Frame.GetPayloadCopy(), Is.EqualTo(first.Frame.GetPayloadCopy()));
            scheduler.Acknowledge(first.Frame.StreamId, first.Frame.ReliableFrameSequence.Value);
            var latest = Next(scheduler);
            RetentionProgress.TryDecode(latest.Frame.GetPayloadCopy(), out RetentionProgress final);
            Assert.That(final.AppliedOriginThrough, Is.EqualTo(10000UL));
        }

        [Test]
        public void ApplicationCompletion_HoldsBulkAndBatchGapsUntilAllPrecedingWorkCompletes()
        {
            var completion = new ApplicationCompletionWatermark(maximumGaps: 2);
            completion.Complete(3);
            completion.Complete(2);
            Assert.That(completion.CompletedThrough, Is.Zero);
            Assert.Throws<System.IO.InvalidDataException>(() => completion.Complete(4));
            completion.Complete(1);
            Assert.That(completion.CompletedThrough, Is.EqualTo(3UL));
            completion.Complete(2);
            Assert.That(completion.CompletedThrough, Is.EqualTo(3UL));
            Assert.That(completion.GapCount, Is.Zero);
        }

        [Test]
        public void ApplicationRetirement_LateDuplicateCommitDoesNotLeakItsMetadata()
        {
            var scheduler = CreateScheduler(new FakeMonotonicClock());
            scheduler.ApplicationRetentionEnabled = true;
            var id = new OperationId(GuidFor(900001));
            var mutation = new SetSiteColor(new ColumnId("column"), new SiteId("site"), 0.1f, 0.2f, 0.3f, 1);
            scheduler.EnqueueMutation(mutation, false, id, 1);
            var first = Next(scheduler);
            scheduler.Acknowledge(first.Frame.StreamId, first.Frame.ReliableFrameSequence.Value);
            scheduler.EnqueueMutation(mutation, false, id, 1);
            Assert.That(scheduler.RetireAppliedSceneOperations(first.Frame.OriginSequence.Value), Has.Count.EqualTo(1));
            var duplicate = Next(scheduler);
            Assert.That(scheduler.ApplicationRetainedCount, Is.EqualTo(1));
            Assert.Throws<System.IO.InvalidDataException>(() => scheduler.RetireAppliedSceneOperations(3));
            scheduler.RetireAppliedSceneOperations(duplicate.Frame.OriginSequence.Value);
            Assert.That(scheduler.ApplicationRetainedCount, Is.Zero);
            Assert.Throws<System.IO.InvalidDataException>(() => scheduler.RetireAppliedSceneOperations(1));
        }

        [Test]
        public void RetentionProgress_RejectsMalformedBoundsVersionAndScope()
        {
            var scheduler = CreateScheduler(new FakeMonotonicClock());
            var progress = new RetentionProgress(scheduler.SessionId, Scene, Incarnation, OriginDevice.Desktop, 10, 3, 5, 7);
            byte[] encoded = progress.Encode();
            Assert.That(encoded.Length, Is.EqualTo(RetentionProgress.EncodedLength));
            Assert.That(RetentionProgress.TryDecode(encoded, out RetentionProgress decoded), Is.True);
            decoded.ValidateScope(scheduler.SessionId, Scene, Incarnation, OriginDevice.Desktop);
            Assert.Throws<System.IO.InvalidDataException>(() => decoded.ValidateScope(scheduler.SessionId, Scene, new IncarnationId(GuidFor(22)), OriginDevice.Desktop));
            encoded[4] = 2;
            Assert.Throws<System.IO.InvalidDataException>(() => RetentionProgress.TryDecode(encoded, out _));
            Assert.Throws<System.IO.InvalidDataException>(() => new RetentionProgress(scheduler.SessionId, Scene, Incarnation, OriginDevice.Quest, 1, 8, 0, 7));
            Assert.Throws<System.IO.InvalidDataException>(() => new RetentionProgress(scheduler.SessionId, Scene, Incarnation, OriginDevice.Desktop, 1, 0, 8, 7));
        }

        [Test]
        public void LedgerRetirement_RecyclesDistinctDeletedEntityKeysAndRejectsStaleProposals()
        {
            var ledger = new OperationLedger(maximumOperations: 2, maximumKeys: 2);
            for (int i = 0; i < 70000; i++)
            {
                var id = new OperationId(GuidFor(100000 + i));
                var mutation = new DeleteCut(new CutId("retired-cut-" + i));
                var descriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, mutation);
                Assert.That(ledger.Accept(id, descriptor, (ulong)i, (ulong)i + 1, MutationPayloadCodec.Encode(mutation)), Is.EqualTo(OperationAdmission.Accepted));
                ledger.RetireOperation(id);
                ledger.AdvanceConflictFloor(Scene, Incarnation, (ulong)i + 1);
            }

            Assert.That(ledger.OperationCount, Is.Zero);
            Assert.That(ledger.IndexedKeyCount, Is.Zero);
            var old = new DeleteCut(new CutId("retired-cut-0"));
            Assert.That(ledger.Accept(new OperationId(GuidFor(999999)), ScheduleDescriptor.ForMutation(Scene, Incarnation, old), 0, 70001, MutationPayloadCodec.Encode(old)), Is.EqualTo(OperationAdmission.Conflicting));
        }

        [TestCase(SurfaceInflationControlKind.Request)]
        [TestCase(SurfaceInflationControlKind.Started)]
        [TestCase(SurfaceInflationControlKind.Progress)]
        [TestCase(SurfaceInflationControlKind.Ready)]
        [TestCase(SurfaceInflationControlKind.Commit)]
        [TestCase(SurfaceInflationControlKind.Committed)]
        [TestCase(SurfaceInflationControlKind.Cancel)]
        [TestCase(SurfaceInflationControlKind.Failed)]
        [TestCase(SurfaceInflationControlKind.Transition)]
        [TestCase(SurfaceInflationControlKind.Transitioned)]
        public void InflationControls_RoundTripAndRejectMalformedBounds(SurfaceInflationControlKind kind)
        {
            var id = new OperationId(GuidFor(93));
            byte[] parameters = kind is SurfaceInflationControlKind.Request or SurfaceInflationControlKind.Started ? new byte[] { 4, 3, 2, 1 } : null;
            var control = new SurfaceInflationControl(kind, id, kind == SurfaceInflationControlKind.Request ? 0UL : 3UL, 42, parameters, kind == SurfaceInflationControlKind.Progress ? 0.75f : 0);
            byte[] encoded = SurfaceInflationControlCodec.Encode(control);
            Assert.That(SurfaceInflationControlCodec.TryDecode(encoded, out var decoded), Is.True);
            Assert.That(decoded.Kind, Is.EqualTo(kind));
            Assert.That(decoded.JobId, Is.EqualTo(id));
            Assert.That(decoded.Generation, Is.EqualTo(control.Generation));
            Assert.That(decoded.CanonicalSequence, Is.EqualTo(42));
            Assert.That(decoded.Progress, Is.EqualTo(control.Progress));
            Assert.That(decoded.Payload, Is.EqualTo(control.Payload));
            Assert.That(SurfaceInflationControlCodec.TryDecode(encoded.Take(encoded.Length - 1).ToArray(), out _), Is.False);
            Assert.That(SurfaceInflationControlCodec.TryDecode(encoded.Concat(new byte[] { 0 }).ToArray(), out _), Is.False);
            encoded[4] = 2; // Incompatible protocol version.
            Assert.That(SurfaceInflationControlCodec.TryDecode(encoded, out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SurfaceInflationControl(SurfaceInflationControlKind.Progress, id, 1, progress: float.NaN));
            Assert.Throws<ArgumentException>(() => new SurfaceInflationControl(SurfaceInflationControlKind.Started, id, 1, payload: new byte[SurfaceInflationControlCodec.MaximumPayloadBytes + 1]));
        }

        [Test]
        public void PreviewFrames_RetainLastValueAndLeaveWrittenRetriesImmutable()
        {
            var clock = new FakeMonotonicClock();
            var scheduler = CreateScheduler(clock);
            scheduler.PreviewFramePacingEnabled = true;
            scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f));
            ReliableFrame written = Next(scheduler).Frame;
            for (int i = 2; i <= 100; i++)
                scheduler.EnqueueMutation(SiteColor("column-A", "site-A", i / 100f));
            Assert.That(scheduler.NeedsPreviewFrame, Is.True);
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
            Assert.That(scheduler.SnapshotMetrics().PendingSceneRecords, Is.EqualTo(1));
            scheduler.BeginPreviewFrame();
            ReliableFrame final = Next(scheduler).Frame;
            Assert.That(((SetSiteColor)MutationPayloadCodec.Decode(final.GetPayloadCopy())).Red, Is.EqualTo(1f));
            scheduler.BeginDisconnectGrace();
            clock.Advance(TimeSpan.FromMilliseconds(250));
            Assert.That(scheduler.TryResume(), Is.True);
            Assert.That(Next(scheduler).Frame, Is.SameAs(written));
            Assert.That(((SetSiteColor)MutationPayloadCodec.Decode(written.GetPayloadCopy())).Red, Is.EqualTo(0.1f));
        }

        [Test]
        public void TimelineActions_BoundSeekCoalescingAndPreserveTheirOrder()
        {
            var scheduler = CreateScheduler(new FakeMonotonicClock());
            var column = new ColumnId("timeline");
            SetTimelineAnchor Anchor(int index, TimelineAnchorIntent intent) => new SetTimelineAnchor(column, index, intent == TimelineAnchorIntent.Play, false, 1, 0, 1000, intent);
            scheduler.EnqueueMutation(Anchor(1, TimelineAnchorIntent.Seek));
            scheduler.EnqueueMutation(Anchor(2, TimelineAnchorIntent.Seek));
            scheduler.EnqueueMutation(Anchor(2, TimelineAnchorIntent.Play));
            scheduler.EnqueueMutation(Anchor(3, TimelineAnchorIntent.Seek));
            scheduler.EnqueueMutation(Anchor(4, TimelineAnchorIntent.Seek));
            scheduler.EnqueueMutation(Anchor(4, TimelineAnchorIntent.Pause));
            scheduler.EnqueueMutation(Anchor(5, TimelineAnchorIntent.Step));
            scheduler.EnqueueMutation(Anchor(5, TimelineAnchorIntent.Loop));
            var values = new List<SetTimelineAnchor>();
            while (scheduler.TryGetNextTransmission(out TransmissionAttempt attempt))
                values.Add((SetTimelineAnchor)MutationPayloadCodec.Decode(attempt.Frame.GetPayloadCopy()));
            Assert.That(values.Select(value => value.Index), Is.EqualTo(new[] { 2, 2, 4, 4, 5, 5 }));
            Assert.That(values.Select(value => value.Intent), Is.EqualTo(new[] { TimelineAnchorIntent.Seek, TimelineAnchorIntent.Play, TimelineAnchorIntent.Seek, TimelineAnchorIntent.Pause, TimelineAnchorIntent.Step, TimelineAnchorIntent.Loop }));
        }

        [Test]
        public void Scheduler_PreservesSceneOrderAndSeparatesReliableSequences()
        {
            var scheduler = CreateScheduler(new FakeMonotonicClock());
            SetSiteColor first = SiteColor("column-A", "site-A", 0.1f);
            SetSiteColor disjointBeforeBarrier = SiteColor("column-A", "site-B", 0.2f);
            ScheduleDescriptor firstDescriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, first);
            ScheduleDescriptor secondDescriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, disjointBeforeBarrier);
            OperationId firstId = new OperationId(GuidFor(300));
            OperationId secondId = new OperationId(GuidFor(302));
            OperationId barrierId = new OperationId(GuidFor(301));

            Assert.That(scheduler.EnqueueMutation(first, true, firstId).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(disjointBeforeBarrier, true, secondId).Accepted, Is.True);
            ScheduleDescriptor barrier = ScheduleDescriptor.ForBarrier(Scene, Incarnation, new[] { firstDescriptor, secondDescriptor }, BarrierScope.TouchedKeys);
            Assert.That(scheduler.EnqueueSceneOperation(new byte[] { 0x42 }, barrier, false, true, false, 1, barrierId).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.8f)).Accepted, Is.True);
            EnqueueResult afterBarrier = scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.9f));
            Assert.That(afterBarrier.Disposition, Is.EqualTo(EnqueueDisposition.ReplacedUnsent));
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.95f)).Accepted, Is.True);
            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x31 }, DeliveryReliability.Reliable).Accepted, Is.True);

            ReliableFrame control = Next(scheduler).Frame;
            ReliableFrame beforeBarrier = Next(scheduler).Frame;
            ReliableFrame secondBeforeBarrier = Next(scheduler).Frame;
            ReliableFrame structural = Next(scheduler).Frame;
            ReliableFrame afterBarrierA = Next(scheduler).Frame;
            ReliableFrame afterBarrierB = Next(scheduler).Frame;

            Assert.That(control.Lane, Is.EqualTo(ScheduleLane.SessionControl));
            Assert.That(control.StreamId, Is.EqualTo(scheduler.SessionControlStreamId));
            Assert.That(control.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(control.OriginSequence, Is.Null);
            Assert.That(beforeBarrier.Lane, Is.EqualTo(ScheduleLane.Interactive));
            Assert.That(beforeBarrier.StreamId, Is.EqualTo(scheduler.SceneOperationStreamId));
            Assert.That(beforeBarrier.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(beforeBarrier.OriginSequence, Is.EqualTo(1UL));
            Assert.That(beforeBarrier.OperationId, Is.EqualTo(firstId));
            Assert.That(structural.Lane, Is.EqualTo(ScheduleLane.SceneControl));
            Assert.That(structural.StreamId, Is.EqualTo(scheduler.SceneOperationStreamId));
            Assert.That(secondBeforeBarrier.OperationId, Is.EqualTo(secondId));
            Assert.That(secondBeforeBarrier.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(secondBeforeBarrier.OriginSequence, Is.EqualTo(2UL));
            Assert.That(structural.ReliableFrameSequence, Is.EqualTo(3UL));
            Assert.That(structural.OriginSequence, Is.EqualTo(3UL));
            Assert.That(structural.OperationId, Is.EqualTo(barrierId));
            Assert.That(afterBarrierA.ReliableFrameSequence, Is.EqualTo(4UL));
            Assert.That(afterBarrierA.OriginSequence, Is.EqualTo(4UL));
            Assert.That(afterBarrierA.OperationId, Is.EqualTo(afterBarrier.OperationId));
            Assert.That(((SetSiteColor)MutationPayloadCodec.Decode(afterBarrierA.GetPayloadCopy())).Red, Is.EqualTo(0.9f));
            Assert.That(afterBarrierB.ReliableFrameSequence, Is.EqualTo(5UL));
            Assert.That(afterBarrierB.OriginSequence, Is.EqualTo(5UL));
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
        }

        [Test]
        public void Scheduler_CoalescesUnsentValuesWithoutGapsAndRetriesWrittenPreviewUnchanged()
        {
            var clock = new FakeMonotonicClock();
            var scheduler = CreateScheduler(clock);
            EnqueueResult first = scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f));
            EnqueueResult replacement = scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.8f));
            Assert.That(first.Disposition, Is.EqualTo(EnqueueDisposition.Accepted));
            Assert.That(replacement.Disposition, Is.EqualTo(EnqueueDisposition.ReplacedUnsent));
            Assert.That(replacement.ReplacedOperationId, Is.EqualTo(first.OperationId));

            TransmissionAttempt initialAttempt = Next(scheduler);
            ReliableFrame committed = initialAttempt.Frame;
            Assert.That(committed.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(committed.OriginSequence, Is.EqualTo(1UL));
            Assert.That(committed.OperationId, Is.EqualTo(replacement.OperationId));
            SetSiteColor committedValue = (SetSiteColor)MutationPayloadCodec.Decode(committed.GetPayloadCopy());
            Assert.That(committedValue.Red, Is.EqualTo(0.8f));
            byte[] committedBytes = committed.GetPayloadCopy();

            scheduler.BeginDisconnectGrace();
            clock.Advance(TimeSpan.FromMilliseconds(250));
            Assert.That(scheduler.TryResume(), Is.True);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.4f)).Accepted, Is.True);

            TransmissionAttempt retry = Next(scheduler);
            Assert.That(retry.IsReplay, Is.True);
            Assert.That(retry.Frame, Is.SameAs(committed));
            Assert.That(retry.AttemptId, Is.Not.EqualTo(initialAttempt.AttemptId));
            CollectionAssert.AreEqual(committedBytes, retry.Frame.GetPayloadCopy());
            Assert.That(retry.Frame.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(retry.Frame.OriginSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 1), Is.True);

            ReliableFrame newest = Next(scheduler).Frame;
            Assert.That(newest.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(newest.OriginSequence, Is.EqualTo(2UL));
            Assert.That(((SetSiteColor)MutationPayloadCodec.Decode(newest.GetPayloadCopy())).Red, Is.EqualTo(0.4f));
            Assert.That(scheduler.SnapshotMetrics().CoalescedPreviewCount, Is.EqualTo(1));
        }

        [Test]
        public void Scheduler_CoalescedCanonicalPreviewKeepsSurvivingSequenceOrder()
        {
            var scheduler = CreateScheduler(new FakeMonotonicClock());
            OperationId firstId = new OperationId(GuidFor(310));
            OperationId secondId = new OperationId(GuidFor(311));
            OperationId latestId = new OperationId(GuidFor(312));

            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f), true, firstId, canonicalSequence: 1).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.2f), true, secondId, canonicalSequence: 2).Accepted, Is.True);
            EnqueueResult replacement = scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.3f), true, latestId, canonicalSequence: 3);
            Assert.That(replacement.Disposition, Is.EqualTo(EnqueueDisposition.ReplacedUnsent));
            Assert.That(replacement.ReplacedOperationId, Is.EqualTo(firstId));

            ReliableFrame first = Next(scheduler).Frame;
            ReliableFrame second = Next(scheduler).Frame;
            Assert.That(first.OperationId, Is.EqualTo(secondId));
            Assert.That(first.CanonicalSequence, Is.EqualTo(2UL));
            Assert.That(first.OriginSequence, Is.EqualTo(1UL));
            Assert.That(second.OperationId, Is.EqualTo(latestId));
            Assert.That(second.CanonicalSequence, Is.EqualTo(3UL));
            Assert.That(second.OriginSequence, Is.EqualTo(2UL));
        }

        [Test]
        public void Scheduler_HoldsAndCoalescesPreviewUnderWindowPressureWhileControlUsesReserve()
        {
            var limits = new SchedulerLimits(bulkChunkBytes: 256, maxReliableFrames: 2, maxReliableBytes: 4096, reservedReliableFrames: 1, reservedReliableBytes: 512);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits);
            scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f));
            ReliableFrame first = Next(scheduler).Frame;
            Assert.That(first.ReliableFrameSequence, Is.EqualTo(1UL));

            EnqueueResult pending = scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.1f));
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
            EnqueueResult coalesced = scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.7f));
            Assert.That(coalesced.Disposition, Is.EqualTo(EnqueueDisposition.ReplacedUnsent));
            Assert.That(coalesced.ReplacedOperationId, Is.EqualTo(pending.OperationId));
            Assert.That(scheduler.SnapshotMetrics().PendingSceneRecords, Is.EqualTo(1));

            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x51 }, DeliveryReliability.Reliable).Accepted, Is.True);
            ReliableFrame reservedControl = Next(scheduler).Frame;
            Assert.That(reservedControl.StreamId, Is.EqualTo(scheduler.SessionControlStreamId));
            Assert.That(reservedControl.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(2));
            Assert.That(scheduler.Acknowledge(scheduler.SessionControlStreamId, 1), Is.True);
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 1), Is.True);

            ReliableFrame pendingNewest = Next(scheduler).Frame;
            Assert.That(pendingNewest.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(pendingNewest.OriginSequence, Is.EqualTo(2UL));
            Assert.That(((SetSiteColor)MutationPayloadCodec.Decode(pendingNewest.GetPayloadCopy())).Red, Is.EqualTo(0.7f));
            Assert.That(scheduler.SnapshotMetrics().PreviewPressureCount, Is.GreaterThan(0));
        }

        [Test]
        public void Scheduler_ReplacesUnsentBulkPreviewWithinTransferAndBodyCaps()
        {
            ScheduleDescriptor descriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.1f));
            AssertBulkPreviewReplacementAtLimits(descriptor, maximumTransfers: 1, maximumRetainedBytes: 1024, guidSeed: 2350);
            AssertBulkPreviewReplacementAtLimits(descriptor, maximumTransfers: 2, maximumRetainedBytes: 512, guidSeed: 2450);
        }

        [Test]
        public void Scheduler_RoutesBodiesAboveMeasuredThresholdToIndependentBoundedBulkChunks()
        {
            var limits = new SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 32, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits);
            ScheduleDescriptor descriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.2f));
            byte[] inlineBody = MakeBytes(256, 0x11);
            byte[] largeBody = MakeBytes(257, 0x22);

            EnqueueResult inlineResult = scheduler.EnqueueSceneOperation(inlineBody, descriptor);
            EnqueueResult largeResult = scheduler.EnqueueSceneOperation(largeBody, descriptor, false, false, false, 17);
            Assert.That(inlineResult.Accepted, Is.True);
            Assert.That(inlineResult.BulkStreamId, Is.Null);
            Assert.That(largeResult.Accepted, Is.True);
            Assert.That(largeResult.BulkStreamId, Is.Not.Null);
            largeBody[0] = 0;
            EnqueueResult oversize = scheduler.EnqueueSceneOperation(MakeBytes(1025, 0x44), descriptor);
            Assert.That(oversize.Accepted, Is.False);
            Assert.That(oversize.Disposition, Is.EqualTo(EnqueueDisposition.Backpressured));
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(257));

            ReliableFrame inline = Next(scheduler).Frame;
            Assert.That(inline.PayloadLength, Is.EqualTo(256));
            Assert.That(inline.BulkDescriptor, Is.Null);
            ReliableFrame descriptorFrame = Next(scheduler).Frame;
            Assert.That(descriptorFrame.StreamId, Is.EqualTo(scheduler.SceneOperationStreamId));
            Assert.That(descriptorFrame.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(descriptorFrame.OriginSequence, Is.EqualTo(2UL));
            Assert.That(descriptorFrame.BulkDescriptor.OperationId, Is.EqualTo(largeResult.OperationId));
            Assert.That(descriptorFrame.BulkDescriptor.BulkStreamId, Is.EqualTo(largeResult.BulkStreamId));
            Assert.That(descriptorFrame.BulkDescriptor.TotalLength, Is.EqualTo(257));
            Assert.That(descriptorFrame.BulkDescriptor.ChunkCount, Is.EqualTo(9));
            Assert.That(descriptorFrame.PayloadLength, Is.LessThanOrEqualTo(limits.InlineThresholdBytes));
            byte[] expectedDigest;
            using (SHA256 sha = SHA256.Create())
                expectedDigest = sha.ComputeHash(MakeBytes(257, 0x22));
            CollectionAssert.AreEqual(expectedDigest, descriptorFrame.BulkDescriptor.GetContentDigestCopy());

            ReliableFrame firstChunk = Next(scheduler).Frame;
            Assert.That(firstChunk.StreamId, Is.EqualTo(largeResult.BulkStreamId));
            Assert.That(firstChunk.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(firstChunk.OriginSequence, Is.Null);
            Assert.That(firstChunk.ChunkIndex, Is.EqualTo(0));
            Assert.That(firstChunk.ChunkOffset, Is.EqualTo(0));
            Assert.That(firstChunk.PayloadLength, Is.EqualTo(32));
            Assert.That(firstChunk.GetPayloadCopy()[0], Is.EqualTo(0x22));
            ReliableFrame lastChunk = null;
            for (int i = 1; i < descriptorFrame.BulkDescriptor.ChunkCount; i++)
                lastChunk = Next(scheduler).Frame;
            Assert.That(lastChunk.PayloadLength, Is.EqualTo(1));
            Assert.That(lastChunk.ReliableFrameSequence, Is.EqualTo(9UL));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 2), Is.True);
            Assert.That(scheduler.Acknowledge(largeResult.BulkStreamId, 9), Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.Zero);
        }

        [Test]
        public void Scheduler_KeepsOversizedTriangleMaskDescriptorInAllSceneOrder()
        {
            const int triangleCount = 40000;
            var visible = Enumerable.Repeat((byte)0xFF, (triangleCount + 7) / 8).ToArray();
            var mask = new ApplyTriangleMask(new[]
            {
                new TriangleMask(new TopologyId("mesh:part:complete"), triangleCount, visible),
                new TriangleMask(new TopologyId("mesh:part:simplified"), triangleCount, visible)
            });
            var limits = new SchedulerLimits(inlineThresholdBytes: 4096, bulkChunkBytes: 2048, maxBulkBodyBytesPerTransfer: 65536, maxBulkBodyBytesTotal: 131072);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits);
            EnqueueResult maskQueued = scheduler.EnqueueMutation(mask, operationId: new OperationId(GuidFor(4150)), canonicalSequence: 41UL);
            EnqueueResult following = scheduler.EnqueueMutation(new SetMriCalibration(0.2f, 0.8f), operationId: new OperationId(GuidFor(4151)));

            Assert.That(maskQueued.Accepted, Is.True);
            Assert.That(following.Accepted, Is.True);
            ReliableFrame descriptor = Next(scheduler).Frame;
            Assert.That(descriptor.Lane, Is.EqualTo(ScheduleLane.SceneControl));
            Assert.That(descriptor.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(descriptor.OperationId, Is.EqualTo(maskQueued.OperationId));
            Assert.That(descriptor.CanonicalSequence, Is.EqualTo(41UL), "The ordered bulk descriptor must preserve the authority sequence for its structural body.");
            Assert.That(descriptor.BulkDescriptor, Is.Not.Null);
            Assert.That(descriptor.BulkDescriptor.TotalLength, Is.GreaterThan(limits.InlineThresholdBytes));
            Assert.That(descriptor.BulkDescriptor.Scheduling.BarrierScope, Is.EqualTo(BarrierScope.AllScene));
            Assert.That(descriptor.BulkDescriptor.Scheduling.TouchedKeys, Has.Count.EqualTo(2));
            CollectionAssert.AreEqual(new byte[] { (byte)'H', (byte)'B', (byte)'S', (byte)'O' }, descriptor.GetPayloadCopy().Take(4));

            ReliableFrame afterDescriptor = Next(scheduler).Frame;
            Assert.That(afterDescriptor.Lane, Is.EqualTo(ScheduleLane.Interactive));
            Assert.That(afterDescriptor.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(afterDescriptor.OperationId, Is.EqualTo(following.OperationId));
            Assert.That(MutationPayloadCodec.Decode(afterDescriptor.GetPayloadCopy()), Is.TypeOf<SetMriCalibration>());

            int sentChunks = 0;
            while (sentChunks < descriptor.BulkDescriptor.ChunkCount)
            {
                ReliableFrame chunk = Next(scheduler).Frame;
                Assert.That(chunk.Lane, Is.EqualTo(ScheduleLane.Bulk));
                Assert.That(chunk.StreamId, Is.EqualTo(maskQueued.BulkStreamId));
                Assert.That(chunk.ChunkIndex, Is.EqualTo(sentChunks));
                sentChunks++;
            }

            Assert.That(sentChunks, Is.GreaterThan(1));
        }

        [Test]
        public void Scheduler_InterleavesBulkStreamsAndBoundsInteractiveWaitByConfiguredBurst()
        {
            const int burst = SchedulerLimits.DefaultInteractiveBurst;
            var limits = new SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, interactiveBurst: burst, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, 1400);
            ScheduleDescriptor firstDescriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "bulk-A", 0.1f));
            ScheduleDescriptor secondDescriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "bulk-B", 0.2f));
            EnqueueResult firstBulk = scheduler.EnqueueSceneOperation(MakeBytes(512, 0xA1), firstDescriptor, false, false, false, 3);
            EnqueueResult secondBulk = scheduler.EnqueueSceneOperation(MakeBytes(512, 0xB2), secondDescriptor, false, false, false, 3);
            Assert.That(firstBulk.Accepted && secondBulk.Accepted, Is.True);

            for (int i = 0; i < 16; i++)
            {
                SetSiteColor value = SiteColor("column-A", "interactive-" + i.ToString(CultureInfo.InvariantCulture), i / 20f);
                ScheduleDescriptor descriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, value);
                Assert.That(scheduler.EnqueueSceneOperation(new byte[] { (byte)i }, descriptor, true).Accepted, Is.True);
            }

            var bulkStreams = new System.Collections.Generic.List<ReliableStreamId>();
            int interactiveSinceBulk = 0;
            int maximumInteractiveWait = 0;
            int totalChunks = 0;
            var watch = Stopwatch.StartNew();
            while (scheduler.TryGetNextTransmission(out TransmissionAttempt transmission))
            {
                if (transmission.Frame.Lane == ScheduleLane.Bulk)
                {
                    maximumInteractiveWait = Math.Max(maximumInteractiveWait, interactiveSinceBulk);
                    Assert.That(interactiveSinceBulk, Is.LessThanOrEqualTo(burst));
                    interactiveSinceBulk = 0;
                    bulkStreams.Add(transmission.Frame.StreamId);
                    totalChunks++;
                }
                else if (transmission.Frame.Lane == ScheduleLane.Interactive)
                {
                    interactiveSinceBulk++;
                }
            }

            watch.Stop();

            Assert.That(totalChunks, Is.EqualTo(16));
            Assert.That(maximumInteractiveWait, Is.EqualTo(burst));
            Assert.That(bulkStreams[0], Is.EqualTo(firstBulk.BulkStreamId));
            Assert.That(bulkStreams[1], Is.EqualTo(secondBulk.BulkStreamId));
            Assert.That(bulkStreams[2], Is.EqualTo(firstBulk.BulkStreamId));
            Assert.That(bulkStreams[3], Is.EqualTo(secondBulk.BulkStreamId));
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "HBP_SYNC_T03_FAIRNESS burst={0} chunkBytes={1} interactiveWaitBound={2} observedMax={3} bulkStreams={4} elapsedMs={5:F3}", burst, limits.BulkChunkBytes, burst, maximumInteractiveWait, 2, watch.Elapsed.TotalMilliseconds));
        }

        [Test]
        public void Scheduler_ResumeServesCancelAndInteractiveBeforeBulkReplay()
        {
            var clock = new FakeMonotonicClock();
            var limits = new SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var scheduler = CreateScheduler(clock, limits, 1700);
            SetSiteColor bulkValue = SiteColor("column-A", "bulk", 0.1f);
            ScheduleDescriptor bulkDescriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, bulkValue);
            EnqueueResult large = scheduler.EnqueueSceneOperation(MakeBytes(512, 0x33), bulkDescriptor, false, false, false, 9);
            ReliableFrame bodyDescriptor = Next(scheduler).Frame;
            Assert.That(bodyDescriptor.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 1), Is.True);
            ReliableFrame writtenChunk = Next(scheduler).Frame;
            Assert.That(writtenChunk.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(writtenChunk.PayloadLength, Is.EqualTo(64));

            scheduler.BeginDisconnectGrace();
            clock.Advance(TimeSpan.FromMilliseconds(100));
            Assert.That(scheduler.CancelBulk(large.OperationId), Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.Zero);
            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0xCA }, DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "interactive", 0.7f)).Accepted, Is.True);
            Assert.That(scheduler.TryResume(), Is.True);

            ReliableFrame cancel = Next(scheduler).Frame;
            ReliableFrame interactive = Next(scheduler).Frame;
            TransmissionAttempt replay = Next(scheduler);

            Assert.That(cancel.Lane, Is.EqualTo(ScheduleLane.SessionControl));
            Assert.That(interactive.Lane, Is.EqualTo(ScheduleLane.Interactive));
            Assert.That(interactive.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(interactive.OriginSequence, Is.EqualTo(2UL));
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(replay.Frame, Is.SameAs(writtenChunk));
            Assert.That(replay.Frame.StreamId, Is.EqualTo(large.BulkStreamId));
            Assert.That(replay.Frame.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "HBP_SYNC_T03_RESUME cancelSelections=1 interactiveSelections=2 bulkReplaySelection=3"));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void Scheduler_GraceExpiryReportsAbandonedRetriesAndPendingPreviews()
        {
            var clock = new FakeMonotonicClock();
            var scheduler = CreateScheduler(clock);
            scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f));
            Next(scheduler);
            scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.3f));
            scheduler.BeginDisconnectGrace();
            clock.Advance(TimeSpan.FromMilliseconds(500));

            Assert.That(scheduler.TryResume(), Is.False);
            Assert.That(scheduler.State, Is.EqualTo(SchedulerState.Offline));
            SchedulerMetrics metrics = scheduler.SnapshotMetrics();
            Assert.That(metrics.GraceExpiredFrameCount, Is.EqualTo(1));
            Assert.That(metrics.GraceExpiredRecordCount, Is.EqualTo(1));
            Assert.That(metrics.OutstandingReliableFrames, Is.Zero);
            Assert.That(metrics.PendingSceneRecords, Is.Zero);
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
        }

        [Test]
        public void Scheduler_ReportsRequiredOverflowAndKeepsEphemeralTrafficOutsideSequences()
        {
            var ephemeralScheduler = CreateScheduler(new FakeMonotonicClock(), null, 2200);
            Assert.That(ephemeralScheduler.EnqueueSessionControl(new byte[] { 0x01 }, DeliveryReliability.Ephemeral).Accepted, Is.True);
            ReliableFrame ephemeral = Next(ephemeralScheduler).Frame;
            Assert.That(ephemeral.Reliability, Is.EqualTo(DeliveryReliability.Ephemeral));
            Assert.That(ephemeral.StreamId, Is.Null);
            Assert.That(ephemeral.ReliableFrameSequence, Is.Null);
            Assert.That(ephemeralScheduler.EnqueueSessionControl(new byte[] { 0x02 }, DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(Next(ephemeralScheduler).Frame.ReliableFrameSequence, Is.EqualTo(1UL));

            var limits = new SchedulerLimits(maxSessionControlQueuedRecords: 2, maxSessionControlQueuedBytes: 4096, reservedSessionControlRecords: 1, reservedSessionControlBytes: 512);
            var overflowScheduler = CreateScheduler(new FakeMonotonicClock(), limits, 2300);
            Assert.That(overflowScheduler.EnqueueSessionControl(new byte[] { 0x01 }, DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(overflowScheduler.EnqueueSessionControl(new byte[] { 0x02 }, DeliveryReliability.Reliable).Accepted, Is.True);
            EnqueueResult overflow = overflowScheduler.EnqueueSessionControl(new byte[] { 0x03 }, DeliveryReliability.Reliable);
            Assert.That(overflow.Accepted, Is.False);
            Assert.That(overflow.Disposition, Is.EqualTo(EnqueueDisposition.SessionFaulted));
            Assert.That(overflowScheduler.State, Is.EqualTo(SchedulerState.Faulted));
            Assert.That(overflowScheduler.SnapshotMetrics().SessionOverflowCount, Is.EqualTo(1));
        }

        [Test]
        public void Scheduler_EphemeralControlBypassesBlockedReliableHeadAtFullRetransmitWindow()
        {
            var limits = new SchedulerLimits(bulkChunkBytes: 256, maxReliableFrames: 2, maxReliableBytes: 4096, reservedReliableFrames: 1, reservedReliableBytes: 512);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, 2750);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f)).Accepted, Is.True);
            ReliableFrame sceneFrame = Next(scheduler).Frame;
            Assert.That(sceneFrame.ReliableFrameSequence, Is.EqualTo(1UL));

            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x11 }, DeliveryReliability.Reliable).Accepted, Is.True);
            ReliableFrame firstReliableControl = Next(scheduler).Frame;
            Assert.That(firstReliableControl.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(2));

            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x22 }, DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x33 }, DeliveryReliability.Ephemeral).Accepted, Is.True);
            ReliableFrame ephemeral = Next(scheduler).Frame;
            Assert.That(ephemeral.Reliability, Is.EqualTo(DeliveryReliability.Ephemeral));
            Assert.That(ephemeral.StreamId, Is.Null);
            Assert.That(ephemeral.ReliableFrameSequence, Is.Null);
            Assert.That(ephemeral.GetPayloadCopy(), Is.EqualTo(new byte[] { 0x33 }));
            Assert.That(scheduler.SnapshotMetrics().PendingSessionControlRecords, Is.EqualTo(1));
            Assert.That(scheduler.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(2));

            Assert.That(scheduler.Acknowledge(scheduler.SessionControlStreamId, 1), Is.True);
            ReliableFrame secondReliableControl = Next(scheduler).Frame;
            Assert.That(secondReliableControl.Reliability, Is.EqualTo(DeliveryReliability.Reliable));
            Assert.That(secondReliableControl.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(secondReliableControl.GetPayloadCopy(), Is.EqualTo(new byte[] { 0x22 }));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, sceneFrame.ReliableFrameSequence.Value), Is.True);
        }

        [Test]
        public void ConflictIndex_DeduplicatesAndChecksAllTouchedKeysAtomically()
        {
            ScheduleDescriptor keyA = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.1f));
            ScheduleDescriptor keyB = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-B", 0.1f));
            ScheduleDescriptor both = ScheduleDescriptor.ForBarrier(Scene, Incarnation, new[] { keyA, keyB }, BarrierScope.TouchedKeys);
            var ledger = new OperationLedger(maximumOperations: 8, maximumKeys: 4);
            byte[] payload = new byte[] { 0x10 };

            OperationId first = new OperationId(GuidFor(400));
            Assert.That(ledger.Accept(first, keyA, 0, 1, payload), Is.EqualTo(OperationAdmission.Accepted));
            Assert.That(ledger.Accept(first, keyA, 0, 1, payload), Is.EqualTo(OperationAdmission.Duplicate));
            Assert.That(ledger.Accept(first, keyA, 0, 2, new byte[] { 0x11 }), Is.EqualTo(OperationAdmission.OperationIdReused));
            Assert.That(ledger.Accept(new OperationId(GuidFor(401)), keyA, 0, 2, new byte[] { 0x12 }), Is.EqualTo(OperationAdmission.Conflicting));
            Assert.That(ledger.Accept(new OperationId(GuidFor(402)), keyB, 0, 2, new byte[] { 0x13 }), Is.EqualTo(OperationAdmission.Accepted));
            Assert.That(ledger.Accept(new OperationId(GuidFor(403)), both, 1, 3, new byte[] { 0x14 }), Is.EqualTo(OperationAdmission.Conflicting));
            Assert.That(ledger.TryGetLastAcceptedSequence(keyA.CoalescingKey, out ulong keyAAfterRejectedBatch), Is.True);
            Assert.That(keyAAfterRejectedBatch, Is.EqualTo(1UL));
            Assert.That(ledger.Accept(new OperationId(GuidFor(404)), both, 2, 3, new byte[] { 0x15 }), Is.EqualTo(OperationAdmission.Accepted));
            Assert.That(ledger.TryGetLastAcceptedSequence(keyA.CoalescingKey, out ulong keyAAfterBatch), Is.True);
            Assert.That(keyAAfterBatch, Is.EqualTo(3UL));
            Assert.That(ledger.TryGetLastAcceptedSequence(keyB.CoalescingKey, out ulong keyBAfterBatch), Is.True);
            Assert.That(keyBAfterBatch, Is.EqualTo(3UL));

            ledger.CloseIncarnation(Scene, Incarnation);
            Assert.That(ledger.OperationCount, Is.Zero);
            Assert.That(ledger.IndexedKeyCount, Is.Zero);

            var boundedLedger = new OperationLedger(maximumOperations: 1, maximumKeys: 1);
            Assert.That(boundedLedger.Accept(new OperationId(GuidFor(405)), keyA, 0, 1, payload), Is.EqualTo(OperationAdmission.Accepted));
            Assert.That(boundedLedger.Accept(new OperationId(GuidFor(406)), keyB, 1, 2, payload), Is.EqualTo(OperationAdmission.Overflow));
            Assert.That(boundedLedger.OperationCount, Is.EqualTo(1));
            Assert.That(boundedLedger.IndexedKeyCount, Is.EqualTo(1));
        }

        [Test]
        public void ConflictIndex_AllSceneBarriersConflictWithEarlierAndLaterProposalsAcrossKeys()
        {
            ScheduleDescriptor keyA = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.1f));
            ScheduleDescriptor keyB = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-B", 0.2f));
            ScheduleDescriptor allScene = ScheduleDescriptor.ForBarrier(Scene, Incarnation, new ScheduleDescriptor[0], BarrierScope.AllScene);
            var priorProposalLedger = new OperationLedger(maximumOperations: 8, maximumKeys: 4, maximumSceneWatermarks: 1);
            byte[] payload = new byte[] { 0x61 };

            Assert.That(allScene.TouchedKeys.Count, Is.Zero);
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(430)), keyA, 0, 1, payload), Is.EqualTo(OperationAdmission.Accepted));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(431)), allScene, 0, 2, payload), Is.EqualTo(OperationAdmission.Conflicting));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(432)), allScene, 1, 2, payload), Is.EqualTo(OperationAdmission.Accepted));
            Assert.That(priorProposalLedger.SceneWatermarkCount, Is.EqualTo(1));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(433)), keyB, 1, 3, payload), Is.EqualTo(OperationAdmission.Conflicting));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(434)), keyB, 2, 3, payload), Is.EqualTo(OperationAdmission.Accepted));

            var laterProposalLedger = new OperationLedger(maximumOperations: 8, maximumKeys: 4, maximumSceneWatermarks: 1);
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(435)), allScene, 0, 1, payload), Is.EqualTo(OperationAdmission.Accepted));
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(436)), keyB, 0, 2, payload), Is.EqualTo(OperationAdmission.Conflicting));
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(437)), keyB, 1, 2, payload), Is.EqualTo(OperationAdmission.Accepted));

            SceneId otherScene = new SceneId(GuidFor(438));
            IncarnationId otherIncarnation = new IncarnationId(GuidFor(439));
            ScheduleDescriptor otherKey = ScheduleDescriptor.ForMutation(otherScene, otherIncarnation, SiteColor("column-A", "site-C", 0.3f));
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(440)), otherKey, 0, 3, payload), Is.EqualTo(OperationAdmission.Overflow));
            Assert.That(laterProposalLedger.SceneWatermarkCount, Is.EqualTo(1));
            laterProposalLedger.CloseIncarnation(Scene, Incarnation);
            Assert.That(laterProposalLedger.SceneWatermarkCount, Is.Zero);
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(441)), otherKey, 0, 3, payload), Is.EqualTo(OperationAdmission.Accepted));
        }

        [Test]
        public void CheckpointIdentity_ComposesTypedFamiliesIncrementallyAndEnforcesBounds()
        {
            SetSiteColor site = SiteColor("column-A", "site-A", 0.2f);
            var cut = new SetCutDefinition(new CutId("cut-A"), MutationCutOrientation.Custom, true, 4, 0.5f, 0f, 1f, 0f);
            var timeline = new SetTimelineAnchor(new ColumnId("column-A"), 5, true, false, 2, 5000, 1000);

            var forward = new CheckpointIdentityAccumulator(Scene, Incarnation);
            Assert.That(forward.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(forward.Add(new CutDefinitionCheckpointRecord(cut)), Is.True);
            Assert.That(forward.Add(new TimelineAnchorCheckpointRecord(timeline)), Is.True);
            CheckpointIdentity original = forward.GetIdentity();

            var reverse = new CheckpointIdentityAccumulator(Scene, Incarnation);
            Assert.That(reverse.Add(new TimelineAnchorCheckpointRecord(timeline)), Is.True);
            Assert.That(reverse.Add(new CutDefinitionCheckpointRecord(cut)), Is.True);
            Assert.That(reverse.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(reverse.GetIdentity(), Is.EqualTo(original));

            Assert.That(forward.Add(new SiteColorCheckpointRecord(SiteColor("column-A", "site-A", 0.9f))), Is.True);
            Assert.That(forward.GetIdentity(), Is.Not.EqualTo(original));
            Assert.That(forward.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(forward.GetIdentity(), Is.EqualTo(original));
            TouchedKey siteKey = ScheduleDescriptor.ForMutation(Scene, Incarnation, site).CoalescingKey;
            Assert.That(forward.Remove(siteKey), Is.True);
            Assert.That(forward.RecordCount, Is.EqualTo(2));

            var bounded = new CheckpointIdentityAccumulator(Scene, Incarnation, maximumRecords: 1, maximumBytes: 1024);
            Assert.That(bounded.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(bounded.Add(new SiteColorCheckpointRecord(SiteColor("column-A", "site-B", 0.3f))), Is.False);
            Assert.That(bounded.RecordCount, Is.EqualTo(1));
        }

        [Test]
        public void Jobs_CancelSupersedeRetriesAndRejectStalePublicationAndChunks()
        {
            int nextGuid = 2500;
            var registry = new JobGenerationRegistry(4, () => GuidFor(nextGuid++));
            OperationId firstJobId = new OperationId(GuidFor(500));
            JobIdentity first = registry.BeginJob(Scene, Incarnation, JobType.ActivityProjection, firstJobId, 11);
            JobAttempt firstAttempt = registry.BeginAttempt(first);
            JobAttempt retryAttempt = registry.BeginAttempt(first);
            Assert.That(registry.IsCurrent(firstAttempt), Is.False);
            Assert.That(registry.IsCurrent(retryAttempt), Is.True);
            Assert.That(registry.Cancel(first), Is.True);
            Assert.That(registry.IsCurrent(retryAttempt), Is.False);
            Assert.That(registry.Cancel(first), Is.False);

            JobIdentity replacement = registry.BeginJob(Scene, Incarnation, JobType.ActivityProjection, new OperationId(GuidFor(501)), 12);
            Assert.That(replacement.Generation, Is.EqualTo(2UL));
            Assert.That(replacement.InputGeneration, Is.EqualTo(12UL));
            JobAttempt currentAttempt = registry.BeginAttempt(replacement);
            Assert.That(registry.IsCurrent(firstAttempt), Is.False);
            Assert.That(registry.IsCurrent(retryAttempt), Is.False);
            Assert.That(registry.IsCurrent(currentAttempt), Is.True);

            registry.CloseIncarnation(Scene, Incarnation);
            Assert.That(registry.IsCurrent(currentAttempt), Is.False);
            Assert.That(registry.TrackedScopeCount, Is.Zero);

            var boundedRegistry = new JobGenerationRegistry(1, () => GuidFor(2600));
            Assert.That(boundedRegistry.BeginJob(Scene, Incarnation, JobType.Filter, new OperationId(GuidFor(502))), Is.Not.Null);
            Assert.That(boundedRegistry.BeginJob(Scene, Incarnation, JobType.Correlation, new OperationId(GuidFor(503))), Is.Null);
            boundedRegistry.CloseIncarnation(Scene, Incarnation);
            Assert.That(boundedRegistry.BeginJob(Scene, Incarnation, JobType.Correlation, new OperationId(GuidFor(503))), Is.Not.Null);
        }

        [Test]
        public void T12FilterJobGenerationFence_DropsLateAAfterCancelledAAndCompletedB()
        {
            var registry = new JobGenerationRegistry(4, () => GuidFor(1250));
            JobIdentity jobA = registry.BeginJob(Scene, Incarnation, JobType.Filter, new OperationId(GuidFor(1251)));
            JobAttempt attemptA = registry.BeginAttempt(jobA);
            Assert.That(registry.Cancel(jobA), Is.True);

            JobIdentity jobB = registry.BeginJob(Scene, Incarnation, JobType.Filter, new OperationId(GuidFor(1252)));
            JobAttempt attemptB = registry.BeginAttempt(jobB);
            var published = new System.Collections.Generic.List<string>();

            bool TryPublish(JobAttempt attempt, string result)
            {
                if (!registry.IsCurrent(attempt)) return false;
                published.Add(result);
                return true;
            }

            Assert.That(TryPublish(attemptB, "B"), Is.True);
            Assert.That(TryPublish(attemptA, "A-late"), Is.False);
            Assert.That(published, Is.EqualTo(new[] { "B" }));
        }

        [Test]
        public void T13CorrelationJobGenerationFence_DropsCancelledAndStaleResultAttempts()
        {
            var registry = new JobGenerationRegistry(4, () => GuidFor(1300));
            JobIdentity jobA = registry.BeginJob(Scene, Incarnation, JobType.Correlation, new OperationId(GuidFor(1301)));
            JobAttempt attemptA = registry.BeginAttempt(jobA);
            Assert.That(registry.Cancel(jobA), Is.True);

            JobIdentity jobB = registry.BeginJob(Scene, Incarnation, JobType.Correlation, new OperationId(GuidFor(1302)));
            JobAttempt attemptB = registry.BeginAttempt(jobB);
            var published = new List<string>();

            bool TryPublish(JobAttempt attempt, string result)
            {
                if (!registry.IsCurrent(attempt)) return false;
                published.Add(result);
                return true;
            }

            Assert.That(jobB.Generation, Is.EqualTo(jobA.Generation + 1));
            Assert.That(TryPublish(attemptB, "B"), Is.True);
            Assert.That(TryPublish(attemptA, "A-late"), Is.False);
            Assert.That(published, Is.EqualTo(new[] { "B" }));
        }

        [Test]
        public void Scheduler_TuningMeasurementReportsInlineCostsAndConfiguredFairnessBounds()
        {
            int[] payloadSizes = { 512, 2048, 4096, 4097, 16384 };
            const int iterations = 24;
            var limits = new SchedulerLimits();
            Assert.That(limits.InlineThresholdBytes, Is.EqualTo(SchedulerLimits.DefaultInlineThresholdBytes));
            Assert.That(limits.BulkChunkBytes, Is.EqualTo(SchedulerLimits.DefaultBulkChunkBytes));
            Assert.That(limits.InteractiveBurst, Is.EqualTo(SchedulerLimits.DefaultInteractiveBurst));

            for (int sizeIndex = 0; sizeIndex < payloadSizes.Length; sizeIndex++)
            {
                int size = payloadSizes[sizeIndex];
                var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, 3000 + sizeIndex * 1000);
                byte[] body = MakeBytes(size, (byte)sizeIndex);
                for (int warmup = 0; warmup < 8; warmup++)
                {
                    SetSiteColor warmupValue = SiteColor("measure", "warmup-" + warmup.ToString(CultureInfo.InvariantCulture), 0.4f);
                    ScheduleDescriptor warmupDescriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, warmupValue);
                    EnqueueResult warmupQueued = scheduler.EnqueueSceneOperation(body, warmupDescriptor);
                    Assert.That(warmupQueued.Accepted, Is.True);
                    DrainOneOperation(scheduler, warmupQueued, Next(scheduler).Frame);
                }

                long started = Stopwatch.GetTimestamp();
                bool bulkRoute = false;
                for (int i = 0; i < iterations; i++)
                {
                    SetSiteColor value = SiteColor("measure", "sample-" + i.ToString(CultureInfo.InvariantCulture), 0.4f);
                    ScheduleDescriptor descriptor = ScheduleDescriptor.ForMutation(Scene, Incarnation, value);
                    EnqueueResult queued = scheduler.EnqueueSceneOperation(body, descriptor);
                    Assert.That(queued.Accepted, Is.True);
                    ReliableFrame first = Next(scheduler).Frame;
                    bulkRoute = first.BulkDescriptor != null;
                    DrainOneOperation(scheduler, queued, first);
                }

                long elapsedTicks = Stopwatch.GetTimestamp() - started;
                double elapsedMicrosecondsPerRecord = elapsedTicks * 1000000.0 / Stopwatch.Frequency / iterations;
                Assert.That(bulkRoute, Is.EqualTo(size > limits.InlineThresholdBytes));
                TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "HBP_SYNC_T03_TUNING payloadBytes={0} route={1} thresholdBytes={2} iterations={3} warmup=8 usPerRecord={4:F2}", size, bulkRoute ? "descriptor+bulk" : "inline", limits.InlineThresholdBytes, iterations, elapsedMicrosecondsPerRecord));
            }
        }

        private static void DrainOneOperation(OutgoingScheduler scheduler, EnqueueResult queued, ReliableFrame first)
        {
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, first.ReliableFrameSequence.Value), Is.True);
            if (first.BulkDescriptor == null)
                return;

            int sentChunks = 0;
            while (sentChunks < first.BulkDescriptor.ChunkCount)
            {
                ReliableFrame chunk = Next(scheduler).Frame;
                Assert.That(chunk.Lane, Is.EqualTo(ScheduleLane.Bulk));
                Assert.That(scheduler.Acknowledge(queued.BulkStreamId, chunk.ReliableFrameSequence.Value), Is.True);
                sentChunks++;
            }
        }

        private static OutgoingScheduler CreateScheduler(FakeMonotonicClock clock, SchedulerLimits limits = null, int guidSeed = 100)
        {
            int next = guidSeed;
            return new OutgoingScheduler(new SessionId(GuidFor(1)), Scene, Incarnation, OriginDevice.Desktop, clock, limits, () => GuidFor(next++));
        }

        private static void AssertBulkPreviewReplacementAtLimits(ScheduleDescriptor descriptor, int maximumTransfers, int maximumRetainedBytes, int guidSeed)
        {
            var limits = new SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, maxBulkTransfers: maximumTransfers, maxBulkBodyBytesPerTransfer: 512, maxBulkBodyBytesTotal: maximumRetainedBytes);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, guidSeed);
            EnqueueResult first = scheduler.EnqueueSceneOperation(MakeBytes(300, 0xA1), descriptor, coalesciblePreview: true);
            Assert.That(first.Accepted, Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(300));

            EnqueueResult replacement = scheduler.EnqueueSceneOperation(MakeBytes(512, 0xB2), descriptor, coalesciblePreview: true);
            Assert.That(replacement.Accepted, Is.True);
            Assert.That(replacement.Disposition, Is.EqualTo(EnqueueDisposition.ReplacedUnsent));
            Assert.That(replacement.ReplacedOperationId, Is.EqualTo(first.OperationId));
            Assert.That(scheduler.CancelBulk(first.OperationId), Is.False);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(512));
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.LessThanOrEqualTo(limits.MaxBulkBodyBytesTotal));
            ScheduleDescriptor anotherKey = ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-B", 0.4f));
            EnqueueResult overLimit = scheduler.EnqueueSceneOperation(MakeBytes(257, 0xC3), anotherKey, coalesciblePreview: true);
            Assert.That(overLimit.Accepted, Is.False);
            Assert.That(overLimit.Disposition, Is.EqualTo(EnqueueDisposition.Backpressured));
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(512));
            Assert.That(scheduler.CancelBulk(replacement.OperationId), Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.Zero);
        }

        private static TransmissionAttempt Next(OutgoingScheduler scheduler)
        {
            Assert.That(scheduler.TryGetNextTransmission(out TransmissionAttempt transmission), Is.True, "Expected a scheduled transmission.");
            return transmission;
        }

        private static SetSiteColor SiteColor(string column, string site, float red)
        {
            return new SetSiteColor(new ColumnId(column), new SiteId(site), red, 0.2f, 0.3f, 1f);
        }

        private static byte[] MakeBytes(int count, byte value)
        {
            var bytes = new byte[count];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = value;
            return bytes;
        }

        private static Guid GuidFor(int value)
        {
            var bytes = new byte[16];
            bytes[0] = (byte)value;
            bytes[1] = (byte)(value >> 8);
            bytes[2] = (byte)(value >> 16);
            bytes[3] = (byte)(value >> 24);
            bytes[15] = 0x7f;
            return new Guid(bytes);
        }
    }
}
