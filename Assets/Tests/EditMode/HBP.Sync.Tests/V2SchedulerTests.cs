using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using NUnit.Framework;
using HBP.Sync.Testing;

namespace HBP.Sync.Tests
{
    [Category(SyncTestCategories.Fast)]
    public class V2SchedulerTests
    {
        private static readonly SceneId Scene = new SceneId(GuidFor(1));
        private static readonly IncarnationId Incarnation = new IncarnationId(GuidFor(2));

        [Test]
        public void Scheduler_PreservesSceneOrderAndSeparatesReliableSequences()
        {
            var scheduler = CreateScheduler(new FakeMonotonicClock());
            SetSiteColor first = SiteColor("column-A", "site-A", 0.1f);
            SetSiteColor disjointBeforeBarrier = SiteColor("column-A", "site-B", 0.2f);
            V2ScheduleDescriptor firstDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, first);
            V2ScheduleDescriptor secondDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, disjointBeforeBarrier);
            OperationId firstId = new OperationId(GuidFor(300));
            OperationId secondId = new OperationId(GuidFor(302));
            OperationId barrierId = new OperationId(GuidFor(301));

            Assert.That(scheduler.EnqueueMutation(first, true, firstId).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(disjointBeforeBarrier, true, secondId).Accepted, Is.True);
            V2ScheduleDescriptor barrier = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, new[] { firstDescriptor, secondDescriptor }, V2BarrierScope.TouchedKeys);
            Assert.That(scheduler.EnqueueSceneOperation(new byte[] { 0x42 }, barrier, false, true, false, 1, barrierId).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.8f)).Accepted, Is.True);
            V2EnqueueResult afterBarrier = scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.9f));
            Assert.That(afterBarrier.Disposition, Is.EqualTo(V2EnqueueDisposition.ReplacedUnsent));
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.95f)).Accepted, Is.True);
            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x31 }, V2DeliveryReliability.Reliable).Accepted, Is.True);

            V2ReliableFrame control = Next(scheduler).Frame;
            V2ReliableFrame beforeBarrier = Next(scheduler).Frame;
            V2ReliableFrame secondBeforeBarrier = Next(scheduler).Frame;
            V2ReliableFrame structural = Next(scheduler).Frame;
            V2ReliableFrame afterBarrierA = Next(scheduler).Frame;
            V2ReliableFrame afterBarrierB = Next(scheduler).Frame;

            Assert.That(control.Lane, Is.EqualTo(V2ScheduleLane.SessionControl));
            Assert.That(control.StreamId, Is.EqualTo(scheduler.SessionControlStreamId));
            Assert.That(control.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(control.OriginSequence, Is.Null);
            Assert.That(beforeBarrier.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
            Assert.That(beforeBarrier.StreamId, Is.EqualTo(scheduler.SceneOperationStreamId));
            Assert.That(beforeBarrier.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(beforeBarrier.OriginSequence, Is.EqualTo(1UL));
            Assert.That(beforeBarrier.OperationId, Is.EqualTo(firstId));
            Assert.That(structural.Lane, Is.EqualTo(V2ScheduleLane.SceneControl));
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
            Assert.That(((SetSiteColor)V2MutationPayloadCodec.Decode(afterBarrierA.GetPayloadCopy())).Red, Is.EqualTo(0.9f));
            Assert.That(afterBarrierB.ReliableFrameSequence, Is.EqualTo(5UL));
            Assert.That(afterBarrierB.OriginSequence, Is.EqualTo(5UL));
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
        }

        [Test]
        public void Scheduler_CoalescesUnsentValuesWithoutGapsAndRetriesWrittenPreviewUnchanged()
        {
            var clock = new FakeMonotonicClock();
            var scheduler = CreateScheduler(clock);
            V2EnqueueResult first = scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f));
            V2EnqueueResult replacement = scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.8f));
            Assert.That(first.Disposition, Is.EqualTo(V2EnqueueDisposition.Accepted));
            Assert.That(replacement.Disposition, Is.EqualTo(V2EnqueueDisposition.ReplacedUnsent));

            V2TransmissionAttempt initialAttempt = Next(scheduler);
            V2ReliableFrame committed = initialAttempt.Frame;
            Assert.That(committed.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(committed.OriginSequence, Is.EqualTo(1UL));
            Assert.That(committed.OperationId, Is.EqualTo(replacement.OperationId));
            SetSiteColor committedValue = (SetSiteColor)V2MutationPayloadCodec.Decode(committed.GetPayloadCopy());
            Assert.That(committedValue.Red, Is.EqualTo(0.8f));
            byte[] committedBytes = committed.GetPayloadCopy();

            scheduler.BeginDisconnectGrace();
            clock.Advance(TimeSpan.FromMilliseconds(250));
            Assert.That(scheduler.TryResume(), Is.True);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.4f)).Accepted, Is.True);

            V2TransmissionAttempt retry = Next(scheduler);
            Assert.That(retry.IsReplay, Is.True);
            Assert.That(retry.Frame, Is.SameAs(committed));
            Assert.That(retry.AttemptId, Is.Not.EqualTo(initialAttempt.AttemptId));
            CollectionAssert.AreEqual(committedBytes, retry.Frame.GetPayloadCopy());
            Assert.That(retry.Frame.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(retry.Frame.OriginSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 1), Is.True);

            V2ReliableFrame newest = Next(scheduler).Frame;
            Assert.That(newest.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(newest.OriginSequence, Is.EqualTo(2UL));
            Assert.That(((SetSiteColor)V2MutationPayloadCodec.Decode(newest.GetPayloadCopy())).Red, Is.EqualTo(0.4f));
            Assert.That(scheduler.SnapshotMetrics().CoalescedPreviewCount, Is.EqualTo(1));
        }

        [Test]
        public void Scheduler_HoldsAndCoalescesPreviewUnderWindowPressureWhileControlUsesReserve()
        {
            var limits = new V2SchedulerLimits(bulkChunkBytes: 256, maxReliableFrames: 2, maxReliableBytes: 4096, reservedReliableFrames: 1, reservedReliableBytes: 512);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits);
            scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f));
            V2ReliableFrame first = Next(scheduler).Frame;
            Assert.That(first.ReliableFrameSequence, Is.EqualTo(1UL));

            scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.1f));
            Assert.That(scheduler.TryGetNextTransmission(out _), Is.False);
            V2EnqueueResult coalesced = scheduler.EnqueueMutation(SiteColor("column-A", "site-B", 0.7f));
            Assert.That(coalesced.Disposition, Is.EqualTo(V2EnqueueDisposition.ReplacedUnsent));
            Assert.That(scheduler.SnapshotMetrics().PendingSceneRecords, Is.EqualTo(1));

            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x51 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            V2ReliableFrame reservedControl = Next(scheduler).Frame;
            Assert.That(reservedControl.StreamId, Is.EqualTo(scheduler.SessionControlStreamId));
            Assert.That(reservedControl.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(2));
            Assert.That(scheduler.Acknowledge(scheduler.SessionControlStreamId, 1), Is.True);
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 1), Is.True);

            V2ReliableFrame pendingNewest = Next(scheduler).Frame;
            Assert.That(pendingNewest.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(pendingNewest.OriginSequence, Is.EqualTo(2UL));
            Assert.That(((SetSiteColor)V2MutationPayloadCodec.Decode(pendingNewest.GetPayloadCopy())).Red, Is.EqualTo(0.7f));
            Assert.That(scheduler.SnapshotMetrics().PreviewPressureCount, Is.GreaterThan(0));
        }

        [Test]
        public void Scheduler_ReplacesUnsentBulkPreviewWithinTransferAndBodyCaps()
        {
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.1f));
            AssertBulkPreviewReplacementAtLimits(descriptor, maximumTransfers: 1, maximumRetainedBytes: 1024, guidSeed: 2350);
            AssertBulkPreviewReplacementAtLimits(descriptor, maximumTransfers: 2, maximumRetainedBytes: 512, guidSeed: 2450);
        }

        [Test]
        public void Scheduler_RoutesBodiesAboveMeasuredThresholdToIndependentBoundedBulkChunks()
        {
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 32, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits);
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.2f));
            byte[] inlineBody = MakeBytes(256, 0x11);
            byte[] largeBody = MakeBytes(257, 0x22);

            V2EnqueueResult inlineResult = scheduler.EnqueueSceneOperation(inlineBody, descriptor);
            V2EnqueueResult largeResult = scheduler.EnqueueSceneOperation(largeBody, descriptor, false, false, false, 17);
            Assert.That(inlineResult.Accepted, Is.True);
            Assert.That(inlineResult.BulkStreamId, Is.Null);
            Assert.That(largeResult.Accepted, Is.True);
            Assert.That(largeResult.BulkStreamId, Is.Not.Null);
            largeBody[0] = 0;
            V2EnqueueResult oversize = scheduler.EnqueueSceneOperation(MakeBytes(1025, 0x44), descriptor);
            Assert.That(oversize.Accepted, Is.False);
            Assert.That(oversize.Disposition, Is.EqualTo(V2EnqueueDisposition.Backpressured));
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(257));

            V2ReliableFrame inline = Next(scheduler).Frame;
            Assert.That(inline.PayloadLength, Is.EqualTo(256));
            Assert.That(inline.BulkDescriptor, Is.Null);
            V2ReliableFrame descriptorFrame = Next(scheduler).Frame;
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

            V2ReliableFrame firstChunk = Next(scheduler).Frame;
            Assert.That(firstChunk.StreamId, Is.EqualTo(largeResult.BulkStreamId));
            Assert.That(firstChunk.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(firstChunk.OriginSequence, Is.Null);
            Assert.That(firstChunk.ChunkIndex, Is.EqualTo(0));
            Assert.That(firstChunk.ChunkOffset, Is.EqualTo(0));
            Assert.That(firstChunk.PayloadLength, Is.EqualTo(32));
            Assert.That(firstChunk.GetPayloadCopy()[0], Is.EqualTo(0x22));
            V2ReliableFrame lastChunk = null;
            for (int i = 1; i < descriptorFrame.BulkDescriptor.ChunkCount; i++)
                lastChunk = Next(scheduler).Frame;
            Assert.That(lastChunk.PayloadLength, Is.EqualTo(1));
            Assert.That(lastChunk.ReliableFrameSequence, Is.EqualTo(9UL));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 2), Is.True);
            Assert.That(scheduler.Acknowledge(largeResult.BulkStreamId, 9), Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.Zero);
        }

        [Test]
        public void Scheduler_InterleavesBulkStreamsAndBoundsInteractiveWaitByConfiguredBurst()
        {
            const int burst = V2SchedulerLimits.DefaultInteractiveBurst;
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, interactiveBurst: burst, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, 1400);
            V2ScheduleDescriptor firstDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "bulk-A", 0.1f));
            V2ScheduleDescriptor secondDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "bulk-B", 0.2f));
            V2EnqueueResult firstBulk = scheduler.EnqueueSceneOperation(MakeBytes(512, 0xA1), firstDescriptor, false, false, false, 3);
            V2EnqueueResult secondBulk = scheduler.EnqueueSceneOperation(MakeBytes(512, 0xB2), secondDescriptor, false, false, false, 3);
            Assert.That(firstBulk.Accepted && secondBulk.Accepted, Is.True);

            for (int i = 0; i < 16; i++)
            {
                SetSiteColor value = SiteColor("column-A", "interactive-" + i.ToString(CultureInfo.InvariantCulture), i / 20f);
                V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, value);
                Assert.That(scheduler.EnqueueSceneOperation(new byte[] { (byte)i }, descriptor, true).Accepted, Is.True);
            }

            var bulkStreams = new System.Collections.Generic.List<ReliableStreamId>();
            int interactiveSinceBulk = 0;
            int maximumInteractiveWait = 0;
            int totalChunks = 0;
            var watch = Stopwatch.StartNew();
            while (scheduler.TryGetNextTransmission(out V2TransmissionAttempt transmission))
            {
                if (transmission.Frame.Lane == V2ScheduleLane.Bulk)
                {
                    maximumInteractiveWait = Math.Max(maximumInteractiveWait, interactiveSinceBulk);
                    Assert.That(interactiveSinceBulk, Is.LessThanOrEqualTo(burst));
                    interactiveSinceBulk = 0;
                    bulkStreams.Add(transmission.Frame.StreamId);
                    totalChunks++;
                }
                else if (transmission.Frame.Lane == V2ScheduleLane.Interactive)
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
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var scheduler = CreateScheduler(clock, limits, 1700);
            SetSiteColor bulkValue = SiteColor("column-A", "bulk", 0.1f);
            V2ScheduleDescriptor bulkDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, bulkValue);
            V2EnqueueResult large = scheduler.EnqueueSceneOperation(MakeBytes(512, 0x33), bulkDescriptor, false, false, false, 9);
            V2ReliableFrame bodyDescriptor = Next(scheduler).Frame;
            Assert.That(bodyDescriptor.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, 1), Is.True);
            V2ReliableFrame writtenChunk = Next(scheduler).Frame;
            Assert.That(writtenChunk.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(writtenChunk.PayloadLength, Is.EqualTo(64));

            scheduler.BeginDisconnectGrace();
            clock.Advance(TimeSpan.FromMilliseconds(100));
            Assert.That(scheduler.CancelBulk(large.OperationId), Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.Zero);
            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0xCA }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "interactive", 0.7f)).Accepted, Is.True);
            Assert.That(scheduler.TryResume(), Is.True);

            V2ReliableFrame cancel = Next(scheduler).Frame;
            V2ReliableFrame interactive = Next(scheduler).Frame;
            V2TransmissionAttempt replay = Next(scheduler);

            Assert.That(cancel.Lane, Is.EqualTo(V2ScheduleLane.SessionControl));
            Assert.That(interactive.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
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
            Assert.That(scheduler.State, Is.EqualTo(V2SchedulerState.Offline));
            V2SchedulerMetrics metrics = scheduler.SnapshotMetrics();
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
            Assert.That(ephemeralScheduler.EnqueueSessionControl(new byte[] { 0x01 }, V2DeliveryReliability.Ephemeral).Accepted, Is.True);
            V2ReliableFrame ephemeral = Next(ephemeralScheduler).Frame;
            Assert.That(ephemeral.Reliability, Is.EqualTo(V2DeliveryReliability.Ephemeral));
            Assert.That(ephemeral.StreamId, Is.Null);
            Assert.That(ephemeral.ReliableFrameSequence, Is.Null);
            Assert.That(ephemeralScheduler.EnqueueSessionControl(new byte[] { 0x02 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(Next(ephemeralScheduler).Frame.ReliableFrameSequence, Is.EqualTo(1UL));

            var limits = new V2SchedulerLimits(maxSessionControlQueuedRecords: 2, maxSessionControlQueuedBytes: 4096, reservedSessionControlRecords: 1, reservedSessionControlBytes: 512);
            var overflowScheduler = CreateScheduler(new FakeMonotonicClock(), limits, 2300);
            Assert.That(overflowScheduler.EnqueueSessionControl(new byte[] { 0x01 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(overflowScheduler.EnqueueSessionControl(new byte[] { 0x02 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            V2EnqueueResult overflow = overflowScheduler.EnqueueSessionControl(new byte[] { 0x03 }, V2DeliveryReliability.Reliable);
            Assert.That(overflow.Accepted, Is.False);
            Assert.That(overflow.Disposition, Is.EqualTo(V2EnqueueDisposition.SessionFaulted));
            Assert.That(overflowScheduler.State, Is.EqualTo(V2SchedulerState.Faulted));
            Assert.That(overflowScheduler.SnapshotMetrics().SessionOverflowCount, Is.EqualTo(1));
        }

        [Test]
        public void Scheduler_EphemeralControlBypassesBlockedReliableHeadAtFullRetransmitWindow()
        {
            var limits = new V2SchedulerLimits(bulkChunkBytes: 256, maxReliableFrames: 2, maxReliableBytes: 4096, reservedReliableFrames: 1, reservedReliableBytes: 512);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, 2750);
            Assert.That(scheduler.EnqueueMutation(SiteColor("column-A", "site-A", 0.1f)).Accepted, Is.True);
            V2ReliableFrame sceneFrame = Next(scheduler).Frame;
            Assert.That(sceneFrame.ReliableFrameSequence, Is.EqualTo(1UL));

            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x11 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            V2ReliableFrame firstReliableControl = Next(scheduler).Frame;
            Assert.That(firstReliableControl.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(scheduler.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(2));

            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x22 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(scheduler.EnqueueSessionControl(new byte[] { 0x33 }, V2DeliveryReliability.Ephemeral).Accepted, Is.True);
            V2ReliableFrame ephemeral = Next(scheduler).Frame;
            Assert.That(ephemeral.Reliability, Is.EqualTo(V2DeliveryReliability.Ephemeral));
            Assert.That(ephemeral.StreamId, Is.Null);
            Assert.That(ephemeral.ReliableFrameSequence, Is.Null);
            Assert.That(ephemeral.GetPayloadCopy(), Is.EqualTo(new byte[] { 0x33 }));
            Assert.That(scheduler.SnapshotMetrics().PendingSessionControlRecords, Is.EqualTo(1));
            Assert.That(scheduler.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(2));

            Assert.That(scheduler.Acknowledge(scheduler.SessionControlStreamId, 1), Is.True);
            V2ReliableFrame secondReliableControl = Next(scheduler).Frame;
            Assert.That(secondReliableControl.Reliability, Is.EqualTo(V2DeliveryReliability.Reliable));
            Assert.That(secondReliableControl.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(secondReliableControl.GetPayloadCopy(), Is.EqualTo(new byte[] { 0x22 }));
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, sceneFrame.ReliableFrameSequence.Value), Is.True);
        }

        [Test]
        public void ConflictIndex_DeduplicatesAndChecksAllTouchedKeysAtomically()
        {
            V2ScheduleDescriptor keyA = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.1f));
            V2ScheduleDescriptor keyB = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-B", 0.1f));
            V2ScheduleDescriptor both = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, new[] { keyA, keyB }, V2BarrierScope.TouchedKeys);
            var ledger = new V2OperationLedger(maximumOperations: 8, maximumKeys: 4);
            byte[] payload = new byte[] { 0x10 };

            OperationId first = new OperationId(GuidFor(400));
            Assert.That(ledger.Accept(first, keyA, 0, 1, payload), Is.EqualTo(V2OperationAdmission.Accepted));
            Assert.That(ledger.Accept(first, keyA, 0, 1, payload), Is.EqualTo(V2OperationAdmission.Duplicate));
            Assert.That(ledger.Accept(first, keyA, 0, 2, new byte[] { 0x11 }), Is.EqualTo(V2OperationAdmission.OperationIdReused));
            Assert.That(ledger.Accept(new OperationId(GuidFor(401)), keyA, 0, 2, new byte[] { 0x12 }), Is.EqualTo(V2OperationAdmission.Conflicting));
            Assert.That(ledger.Accept(new OperationId(GuidFor(402)), keyB, 0, 2, new byte[] { 0x13 }), Is.EqualTo(V2OperationAdmission.Accepted));
            Assert.That(ledger.Accept(new OperationId(GuidFor(403)), both, 1, 3, new byte[] { 0x14 }), Is.EqualTo(V2OperationAdmission.Conflicting));
            Assert.That(ledger.TryGetLastAcceptedSequence(keyA.CoalescingKey, out ulong keyAAfterRejectedBatch), Is.True);
            Assert.That(keyAAfterRejectedBatch, Is.EqualTo(1UL));
            Assert.That(ledger.Accept(new OperationId(GuidFor(404)), both, 2, 3, new byte[] { 0x15 }), Is.EqualTo(V2OperationAdmission.Accepted));
            Assert.That(ledger.TryGetLastAcceptedSequence(keyA.CoalescingKey, out ulong keyAAfterBatch), Is.True);
            Assert.That(keyAAfterBatch, Is.EqualTo(3UL));
            Assert.That(ledger.TryGetLastAcceptedSequence(keyB.CoalescingKey, out ulong keyBAfterBatch), Is.True);
            Assert.That(keyBAfterBatch, Is.EqualTo(3UL));

            ledger.CloseIncarnation(Scene, Incarnation);
            Assert.That(ledger.OperationCount, Is.Zero);
            Assert.That(ledger.IndexedKeyCount, Is.Zero);

            var boundedLedger = new V2OperationLedger(maximumOperations: 1, maximumKeys: 1);
            Assert.That(boundedLedger.Accept(new OperationId(GuidFor(405)), keyA, 0, 1, payload), Is.EqualTo(V2OperationAdmission.Accepted));
            Assert.That(boundedLedger.Accept(new OperationId(GuidFor(406)), keyB, 1, 2, payload), Is.EqualTo(V2OperationAdmission.Overflow));
            Assert.That(boundedLedger.OperationCount, Is.EqualTo(1));
            Assert.That(boundedLedger.IndexedKeyCount, Is.EqualTo(1));
        }

        [Test]
        public void ConflictIndex_AllSceneBarriersConflictWithEarlierAndLaterProposalsAcrossKeys()
        {
            V2ScheduleDescriptor keyA = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-A", 0.1f));
            V2ScheduleDescriptor keyB = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-B", 0.2f));
            V2ScheduleDescriptor allScene = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, new V2ScheduleDescriptor[0], V2BarrierScope.AllScene);
            var priorProposalLedger = new V2OperationLedger(maximumOperations: 8, maximumKeys: 4, maximumSceneWatermarks: 1);
            byte[] payload = new byte[] { 0x61 };

            Assert.That(allScene.TouchedKeys.Count, Is.Zero);
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(430)), keyA, 0, 1, payload), Is.EqualTo(V2OperationAdmission.Accepted));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(431)), allScene, 0, 2, payload), Is.EqualTo(V2OperationAdmission.Conflicting));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(432)), allScene, 1, 2, payload), Is.EqualTo(V2OperationAdmission.Accepted));
            Assert.That(priorProposalLedger.SceneWatermarkCount, Is.EqualTo(1));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(433)), keyB, 1, 3, payload), Is.EqualTo(V2OperationAdmission.Conflicting));
            Assert.That(priorProposalLedger.Accept(new OperationId(GuidFor(434)), keyB, 2, 3, payload), Is.EqualTo(V2OperationAdmission.Accepted));

            var laterProposalLedger = new V2OperationLedger(maximumOperations: 8, maximumKeys: 4, maximumSceneWatermarks: 1);
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(435)), allScene, 0, 1, payload), Is.EqualTo(V2OperationAdmission.Accepted));
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(436)), keyB, 0, 2, payload), Is.EqualTo(V2OperationAdmission.Conflicting));
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(437)), keyB, 1, 2, payload), Is.EqualTo(V2OperationAdmission.Accepted));

            SceneId otherScene = new SceneId(GuidFor(438));
            IncarnationId otherIncarnation = new IncarnationId(GuidFor(439));
            V2ScheduleDescriptor otherKey = V2ScheduleDescriptor.ForMutation(otherScene, otherIncarnation, SiteColor("column-A", "site-C", 0.3f));
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(440)), otherKey, 0, 3, payload), Is.EqualTo(V2OperationAdmission.Overflow));
            Assert.That(laterProposalLedger.SceneWatermarkCount, Is.EqualTo(1));
            laterProposalLedger.CloseIncarnation(Scene, Incarnation);
            Assert.That(laterProposalLedger.SceneWatermarkCount, Is.Zero);
            Assert.That(laterProposalLedger.Accept(new OperationId(GuidFor(441)), otherKey, 0, 3, payload), Is.EqualTo(V2OperationAdmission.Accepted));
        }

        [Test]
        public void CheckpointIdentity_ComposesTypedFamiliesIncrementallyAndEnforcesBounds()
        {
            SetSiteColor site = SiteColor("column-A", "site-A", 0.2f);
            var cut = new SetCutDefinition(new CutId("cut-A"), V2CutOrientation.Custom, true, 4, 0.5f, 0f, 1f, 0f);
            var timeline = new SetTimelineAnchor(new ColumnId("column-A"), 5, true, false, 2, 5000, 1000);

            var forward = new V2CheckpointIdentityAccumulator(Scene, Incarnation);
            Assert.That(forward.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(forward.Add(new CutDefinitionCheckpointRecord(cut)), Is.True);
            Assert.That(forward.Add(new TimelineAnchorCheckpointRecord(timeline)), Is.True);
            V2CheckpointIdentity original = forward.GetIdentity();

            var reverse = new V2CheckpointIdentityAccumulator(Scene, Incarnation);
            Assert.That(reverse.Add(new TimelineAnchorCheckpointRecord(timeline)), Is.True);
            Assert.That(reverse.Add(new CutDefinitionCheckpointRecord(cut)), Is.True);
            Assert.That(reverse.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(reverse.GetIdentity(), Is.EqualTo(original));

            Assert.That(forward.Add(new SiteColorCheckpointRecord(SiteColor("column-A", "site-A", 0.9f))), Is.True);
            Assert.That(forward.GetIdentity(), Is.Not.EqualTo(original));
            Assert.That(forward.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(forward.GetIdentity(), Is.EqualTo(original));
            V2TouchedKey siteKey = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, site).CoalescingKey;
            Assert.That(forward.Remove(siteKey), Is.True);
            Assert.That(forward.RecordCount, Is.EqualTo(2));

            var bounded = new V2CheckpointIdentityAccumulator(Scene, Incarnation, maximumRecords: 1, maximumBytes: 1024);
            Assert.That(bounded.Add(new SiteColorCheckpointRecord(site)), Is.True);
            Assert.That(bounded.Add(new SiteColorCheckpointRecord(SiteColor("column-A", "site-B", 0.3f))), Is.False);
            Assert.That(bounded.RecordCount, Is.EqualTo(1));
        }

        [Test]
        public void Jobs_CancelSupersedeRetriesAndRejectStalePublicationAndChunks()
        {
            int nextGuid = 2500;
            var registry = new V2JobGenerationRegistry(4, () => GuidFor(nextGuid++));
            OperationId firstJobId = new OperationId(GuidFor(500));
            V2JobIdentity first = registry.BeginJob(Scene, Incarnation, V2JobType.ActivityProjection, firstJobId, 11);
            V2JobAttempt firstAttempt = registry.BeginAttempt(first);
            V2JobAttempt retryAttempt = registry.BeginAttempt(first);
            Assert.That(registry.IsCurrent(firstAttempt), Is.False);
            Assert.That(registry.IsCurrent(retryAttempt), Is.True);
            Assert.That(registry.Cancel(first), Is.True);
            Assert.That(registry.IsCurrent(retryAttempt), Is.False);
            Assert.That(registry.Cancel(first), Is.False);

            V2JobIdentity replacement = registry.BeginJob(Scene, Incarnation, V2JobType.ActivityProjection, new OperationId(GuidFor(501)), 12);
            Assert.That(replacement.Generation, Is.EqualTo(2UL));
            Assert.That(replacement.InputGeneration, Is.EqualTo(12UL));
            V2JobAttempt currentAttempt = registry.BeginAttempt(replacement);
            Assert.That(registry.IsCurrent(firstAttempt), Is.False);
            Assert.That(registry.IsCurrent(retryAttempt), Is.False);
            Assert.That(registry.IsCurrent(currentAttempt), Is.True);

            registry.CloseIncarnation(Scene, Incarnation);
            Assert.That(registry.IsCurrent(currentAttempt), Is.False);
            Assert.That(registry.TrackedScopeCount, Is.Zero);

            var boundedRegistry = new V2JobGenerationRegistry(1, () => GuidFor(2600));
            Assert.That(boundedRegistry.BeginJob(Scene, Incarnation, V2JobType.Filter, new OperationId(GuidFor(502))), Is.Not.Null);
            Assert.That(boundedRegistry.BeginJob(Scene, Incarnation, V2JobType.Correlation, new OperationId(GuidFor(503))), Is.Null);
            boundedRegistry.CloseIncarnation(Scene, Incarnation);
            Assert.That(boundedRegistry.BeginJob(Scene, Incarnation, V2JobType.Correlation, new OperationId(GuidFor(503))), Is.Not.Null);
        }

        [Test]
        public void Scheduler_TuningMeasurementReportsInlineCostsAndConfiguredFairnessBounds()
        {
            int[] payloadSizes = { 512, 2048, 4096, 4097, 16384 };
            const int iterations = 24;
            var limits = new V2SchedulerLimits();
            Assert.That(limits.InlineThresholdBytes, Is.EqualTo(V2SchedulerLimits.DefaultInlineThresholdBytes));
            Assert.That(limits.BulkChunkBytes, Is.EqualTo(V2SchedulerLimits.DefaultBulkChunkBytes));
            Assert.That(limits.InteractiveBurst, Is.EqualTo(V2SchedulerLimits.DefaultInteractiveBurst));

            for (int sizeIndex = 0; sizeIndex < payloadSizes.Length; sizeIndex++)
            {
                int size = payloadSizes[sizeIndex];
                var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, 3000 + sizeIndex * 1000);
                byte[] body = MakeBytes(size, (byte)sizeIndex);
                for (int warmup = 0; warmup < 8; warmup++)
                {
                    SetSiteColor warmupValue = SiteColor("measure", "warmup-" + warmup.ToString(CultureInfo.InvariantCulture), 0.4f);
                    V2ScheduleDescriptor warmupDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, warmupValue);
                    V2EnqueueResult warmupQueued = scheduler.EnqueueSceneOperation(body, warmupDescriptor);
                    Assert.That(warmupQueued.Accepted, Is.True);
                    DrainOneOperation(scheduler, warmupQueued, Next(scheduler).Frame);
                }

                long started = Stopwatch.GetTimestamp();
                bool bulkRoute = false;
                for (int i = 0; i < iterations; i++)
                {
                    SetSiteColor value = SiteColor("measure", "sample-" + i.ToString(CultureInfo.InvariantCulture), 0.4f);
                    V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, value);
                    V2EnqueueResult queued = scheduler.EnqueueSceneOperation(body, descriptor);
                    Assert.That(queued.Accepted, Is.True);
                    V2ReliableFrame first = Next(scheduler).Frame;
                    bulkRoute = first.BulkDescriptor != null;
                    DrainOneOperation(scheduler, queued, first);
                }

                long elapsedTicks = Stopwatch.GetTimestamp() - started;
                double elapsedMicrosecondsPerRecord = elapsedTicks * 1000000.0 / Stopwatch.Frequency / iterations;
                Assert.That(bulkRoute, Is.EqualTo(size > limits.InlineThresholdBytes));
                TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "HBP_SYNC_T03_TUNING payloadBytes={0} route={1} thresholdBytes={2} iterations={3} warmup=8 usPerRecord={4:F2}", size, bulkRoute ? "descriptor+bulk" : "inline", limits.InlineThresholdBytes, iterations, elapsedMicrosecondsPerRecord));
            }
        }

        private static void DrainOneOperation(V2OutgoingScheduler scheduler, V2EnqueueResult queued, V2ReliableFrame first)
        {
            Assert.That(scheduler.Acknowledge(scheduler.SceneOperationStreamId, first.ReliableFrameSequence.Value), Is.True);
            if (first.BulkDescriptor == null)
                return;

            int sentChunks = 0;
            while (sentChunks < first.BulkDescriptor.ChunkCount)
            {
                V2ReliableFrame chunk = Next(scheduler).Frame;
                Assert.That(chunk.Lane, Is.EqualTo(V2ScheduleLane.Bulk));
                Assert.That(scheduler.Acknowledge(queued.BulkStreamId, chunk.ReliableFrameSequence.Value), Is.True);
                sentChunks++;
            }
        }

        private static V2OutgoingScheduler CreateScheduler(FakeMonotonicClock clock, V2SchedulerLimits limits = null, int guidSeed = 100)
        {
            int next = guidSeed;
            return new V2OutgoingScheduler(new SessionId(GuidFor(1)), Scene, Incarnation, V2OriginDevice.Desktop, clock, limits, () => GuidFor(next++));
        }

        private static void AssertBulkPreviewReplacementAtLimits(V2ScheduleDescriptor descriptor, int maximumTransfers, int maximumRetainedBytes, int guidSeed)
        {
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, maxBulkTransfers: maximumTransfers, maxBulkBodyBytesPerTransfer: 512, maxBulkBodyBytesTotal: maximumRetainedBytes);
            var scheduler = CreateScheduler(new FakeMonotonicClock(), limits, guidSeed);
            V2EnqueueResult first = scheduler.EnqueueSceneOperation(MakeBytes(300, 0xA1), descriptor, coalesciblePreview: true);
            Assert.That(first.Accepted, Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(300));

            V2EnqueueResult replacement = scheduler.EnqueueSceneOperation(MakeBytes(512, 0xB2), descriptor, coalesciblePreview: true);
            Assert.That(replacement.Accepted, Is.True);
            Assert.That(replacement.Disposition, Is.EqualTo(V2EnqueueDisposition.ReplacedUnsent));
            Assert.That(scheduler.CancelBulk(first.OperationId), Is.False);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(512));
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.LessThanOrEqualTo(limits.MaxBulkBodyBytesTotal));
            V2ScheduleDescriptor anotherKey = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, SiteColor("column-A", "site-B", 0.4f));
            V2EnqueueResult overLimit = scheduler.EnqueueSceneOperation(MakeBytes(257, 0xC3), anotherKey, coalesciblePreview: true);
            Assert.That(overLimit.Accepted, Is.False);
            Assert.That(overLimit.Disposition, Is.EqualTo(V2EnqueueDisposition.Backpressured));
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.EqualTo(512));
            Assert.That(scheduler.CancelBulk(replacement.OperationId), Is.True);
            Assert.That(scheduler.SnapshotMetrics().RetainedBulkBodyBytes, Is.Zero);
        }

        private static V2TransmissionAttempt Next(V2OutgoingScheduler scheduler)
        {
            Assert.That(scheduler.TryGetNextTransmission(out V2TransmissionAttempt transmission), Is.True, "Expected a scheduled transmission.");
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
