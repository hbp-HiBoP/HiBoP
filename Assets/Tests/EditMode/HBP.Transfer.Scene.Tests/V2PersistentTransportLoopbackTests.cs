using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [TestFixture]
    [Category("Sync.Loopback")]
    public sealed class V2PersistentTransportLoopbackTests
    {
        private static readonly SessionId Session = new SessionId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        private static readonly SceneId Scene = new SceneId(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        private static readonly IncarnationId Incarnation = new IncarnationId(Guid.Parse("30000000-0000-0000-0000-000000000003"));

        [Test]
        public async Task AuthenticatedStreamPeers_ExchangeReliableRecordsInBothDirections()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 1000);
            var quest = CreateTransport(V2OriginDevice.Quest, 2000);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var stop = new CancellationTokenSource();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), stop.Token);
            Task<Exception> questRun = CaptureRunAsync(quest, pair.Server.GetStream(), stop.Token);

            var desktopOperation = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000004"));
            var questOperation = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000005"));
            Assert.That(desktop.EnqueueMutation(Color("desktop-site"), 11UL, null, false, desktopOperation).Accepted, Is.True);
            Assert.That(quest.EnqueueMutation(Color("quest-site"), null, 23UL, false, questOperation).Accepted, Is.True);

            V2TransportRecord toQuest = await ReadIncomingAsync(quest);
            V2TransportRecord toDesktop = await ReadIncomingAsync(desktop);
            Assert.That(toQuest.MessageId, Is.EqualTo(desktopOperation));
            Assert.That(toQuest.OriginDevice, Is.EqualTo(V2OriginDevice.Desktop));
            Assert.That(toQuest.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(toQuest.OriginSequence, Is.EqualTo(1UL));
            Assert.That(toQuest.CanonicalSequence, Is.EqualTo(11UL));
            Assert.That(toQuest.ObservedCanonicalSequence, Is.Null);
            Assert.That(toDesktop.MessageId, Is.EqualTo(questOperation));
            Assert.That(toDesktop.OriginDevice, Is.EqualTo(V2OriginDevice.Quest));
            Assert.That(toDesktop.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(toDesktop.OriginSequence, Is.EqualTo(1UL));
            Assert.That(toDesktop.CanonicalSequence, Is.Null);
            Assert.That(toDesktop.ObservedCanonicalSequence, Is.EqualTo(23UL));

            // An application-level receipt is separate from the transport receipt ACK.
            Assert.That(quest.EnqueueSessionControl(new byte[] { 0x51 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That((await ReadIncomingAsync(desktop)).ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(desktop.SnapshotMetrics().OutstandingReliableFrames, Is.Zero);

            desktop.Dispose();
            quest.Dispose();
            pair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        public async Task BulkStreams_RetireAcrossMoreThan128TransfersAndResumeNearTheWatermark()
        {
            const int transferCount = 132;
            const int reconnectAfter = 124;
            var desktop = CreateTransport(V2OriginDevice.Desktop, 26000);
            var quest = CreateTransport(V2OriginDevice.Quest, 27000);
            LoopbackPeerPair pair = await LoopbackPeerPair.ConnectAsync();
            var duplicateBulkAck = new DuplicateBulkAcknowledgementWriteStream(pair.Server.GetStream());
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, duplicateBulkAck, CancellationToken.None);

            try
            {
                for (int index = 0; index < transferCount; index++)
                {
                    var operationId = new OperationId(GuidFor(28000 + index));
                    V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, Color("bulk-retirement-" + index));
                    V2EnqueueResult transfer = desktop.EnqueueSceneOperation(Bytes(V2SchedulerLimits.DefaultInlineThresholdBytes + 1, (byte)index), descriptor, operationId: operationId, bodySchema: 9);
                    Assert.That(transfer.Accepted, Is.True, $"Transfer {index} was not admitted.");
                    Assert.That(V2BulkStreamIdentityCodec.TryGetOrdinal(Session, V2OriginDevice.Desktop, transfer.BulkStreamId, out ulong ordinal), Is.True);
                    Assert.That(ordinal, Is.EqualTo((ulong)index + 1));

                    V2TransportRecord bulkDescriptor = await ReadIncomingAsync(quest);
                    V2TransportRecord chunk = await ReadIncomingAsync(quest);
                    Assert.That(bulkDescriptor.MessageId, Is.EqualTo(operationId));
                    Assert.That(bulkDescriptor.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
                    Assert.That(chunk.MessageId, Is.EqualTo(operationId));
                    Assert.That(chunk.Lane, Is.EqualTo(V2ScheduleLane.Bulk));
                    Assert.That(chunk.StreamId, Is.EqualTo(transfer.BulkStreamId));
                    Assert.That(chunk.ChunkIndex, Is.Zero);

                    // The peer writes ACK controls before this reliable fence. Seeing
                    // the fence on Desktop proves its reader consumed the bulk ACK.
                    byte[] fencePayload = { 0xFE, (byte)index };
                    Assert.That(quest.EnqueueSessionControl(fencePayload, V2DeliveryReliability.Reliable).Accepted, Is.True);
                    V2TransportRecord fence = await ReadIncomingAsync(desktop);
                    CollectionAssert.AreEqual(fencePayload, fence.GetPayloadCopy());

                    if (index + 1 == reconnectAfter)
                    {
                        pair.Close();
                        await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
                        pair.Dispose();

                        pair = await LoopbackPeerPair.ConnectAsync();
                        desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), CancellationToken.None);
                        questRun = CaptureRunAsync(quest, pair.Server.GetStream(), CancellationToken.None);
                    }
                }

                await AwaitGuardAsync(duplicateBulkAck.Duplicated.Task);
                Assert.That(desktop.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That(quest.State, Is.EqualTo(V2PersistentTransportState.Connected));
            }
            finally
            {
                desktop.Dispose();
                quest.Dispose();
                pair?.Close();
                await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
            }
        }

        [Test]
        public async Task BulkRetirementWatermark_AcceptsMoreThan126UnsentCancelledOrdinals()
        {
            const int cancelledCount = 127;
            var desktop = CreateTransport(V2OriginDevice.Desktop, 33000);
            var quest = CreateTransport(V2OriginDevice.Quest, 34000);

            for (int index = 0; index < cancelledCount; index++)
            {
                var operationId = new OperationId(GuidFor(35000 + index));
                V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, Color("unsent-cancelled-" + index));
                V2EnqueueResult cancelled = desktop.EnqueueSceneOperation(Bytes(V2SchedulerLimits.DefaultInlineThresholdBytes + 1, (byte)index), descriptor, operationId: operationId, bodySchema: 10);
                Assert.That(cancelled.Accepted, Is.True, $"Unsent transfer {index} was not admitted.");
                Assert.That(V2BulkStreamIdentityCodec.TryGetOrdinal(Session, V2OriginDevice.Desktop, cancelled.BulkStreamId, out ulong ordinal), Is.True);
                Assert.That(ordinal, Is.EqualTo((ulong)index + 1));
                Assert.That(desktop.CancelBulk(operationId), Is.True, $"Unsent transfer {index} was not cancelled.");
            }

            var validOperationId = new OperationId(GuidFor(36000));
            V2ScheduleDescriptor validDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, Color("after-unsent-retirement"));
            V2EnqueueResult valid = desktop.EnqueueSceneOperation(Bytes(V2SchedulerLimits.DefaultInlineThresholdBytes + 1, 0xD5), validDescriptor, operationId: validOperationId, bodySchema: 10);
            Assert.That(valid.Accepted, Is.True);
            Assert.That(V2BulkStreamIdentityCodec.TryGetOrdinal(Session, V2OriginDevice.Desktop, valid.BulkStreamId, out ulong validOrdinal), Is.True);
            Assert.That(validOrdinal, Is.EqualTo(cancelledCount + 1UL));

            LoopbackPeerPair pair = await LoopbackPeerPair.ConnectAsync();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, pair.Server.GetStream(), CancellationToken.None);
            try
            {
                V2TransportRecord descriptorRecord = await ReadIncomingAsync(quest);
                V2TransportRecord chunk = await ReadIncomingAsync(quest);
                Assert.That(descriptorRecord.MessageId, Is.EqualTo(validOperationId));
                Assert.That(descriptorRecord.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
                Assert.That(chunk.MessageId, Is.EqualTo(validOperationId));
                Assert.That(chunk.Lane, Is.EqualTo(V2ScheduleLane.Bulk));
                Assert.That(chunk.StreamId, Is.EqualTo(valid.BulkStreamId));
                Assert.That(chunk.ChunkIndex, Is.Zero);

                byte[] fencePayload = { 0xFD };
                Assert.That(quest.EnqueueSessionControl(fencePayload, V2DeliveryReliability.Reliable).Accepted, Is.True);
                CollectionAssert.AreEqual(fencePayload, (await ReadIncomingAsync(desktop)).GetPayloadCopy());
                Assert.That(desktop.SnapshotMetrics().OutstandingReliableFrames, Is.Zero);
                Assert.That(desktop.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That(quest.State, Is.EqualTo(V2PersistentTransportState.Connected));
            }
            finally
            {
                desktop.Dispose();
                quest.Dispose();
                pair.Close();
                await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
                pair.Dispose();
            }
        }

        [Test]
        public async Task Resume_AppliesLostAckWatermarkAndLostPingCreatesNoReliableGap()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 3000);
            var quest = CreateTransport(V2OriginDevice.Quest, 4000);
            using var firstPair = await LoopbackPeerPair.ConnectAsync();
            var pingDrop = new DropMessageKindWriteStream(firstPair.Client.GetStream(), V2TransportMessageKind.Ping);
            var ackDrop = new DropMessageKindWriteStream(firstPair.Server.GetStream(), V2TransportMessageKind.Acknowledgement);
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pingDrop, CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, ackDrop, CancellationToken.None);

            Assert.That(desktop.EnqueueSessionControl(new byte[] { 0x11 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            V2TransportRecord deliveredOnce = await ReadIncomingAsync(quest);
            Assert.That(deliveredOnce.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(desktop.SendLivenessProbe(), Is.Not.EqualTo(Guid.Empty));
            await AwaitGuardAsync(pingDrop.Dropped.Task);

            Assert.That(quest.EnqueueSessionControl(new byte[] { 0x22 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That((await ReadIncomingAsync(desktop)).ReliableFrameSequence, Is.EqualTo(1UL));
            await AwaitGuardAsync(ackDrop.Dropped.Task);
            Assert.That(desktop.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(1));

            firstPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));

            using var secondPair = await LoopbackPeerPair.ConnectAsync();
            desktopRun = CaptureRunAsync(desktop, secondPair.Client.GetStream(), CancellationToken.None);
            questRun = CaptureRunAsync(quest, secondPair.Server.GetStream(), CancellationToken.None);

            Assert.That(desktop.EnqueueSessionControl(new byte[] { 0x33 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            V2TransportRecord next = await ReadIncomingAsync(quest);
            Assert.That(next.ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(next.GetPayloadCopy(), Is.EqualTo(new byte[] { 0x33 }));
            Assert.That(quest.EnqueueSessionControl(new byte[] { 0x44 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That((await ReadIncomingAsync(desktop)).ReliableFrameSequence, Is.EqualTo(2UL));
            Assert.That(desktop.SnapshotMetrics().OutstandingReliableFrames, Is.Zero);

            desktop.Dispose();
            quest.Dispose();
            secondPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        public async Task Resume_ReplaysUnreceivedReliableFrameWithOriginalSequence()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 5000);
            var quest = CreateTransport(V2OriginDevice.Quest, 6000);
            using var firstPair = await LoopbackPeerPair.ConnectAsync();
            var dropped = new DropMessageKindWriteStream(firstPair.Client.GetStream(), V2TransportMessageKind.Application);
            Task<Exception> desktopRun = CaptureRunAsync(desktop, dropped, CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, firstPair.Server.GetStream(), CancellationToken.None);

            Assert.That(desktop.EnqueueSessionControl(new byte[] { 0x61 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            await AwaitGuardAsync(dropped.Dropped.Task);
            Assert.That(desktop.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(1));
            firstPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));

            using var secondPair = await LoopbackPeerPair.ConnectAsync();
            desktopRun = CaptureRunAsync(desktop, secondPair.Client.GetStream(), CancellationToken.None);
            questRun = CaptureRunAsync(quest, secondPair.Server.GetStream(), CancellationToken.None);
            V2TransportRecord replay = await ReadIncomingAsync(quest);

            Assert.That(replay.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(replay.StreamId, Is.EqualTo(dropped.DroppedRecord.StreamId));
            CollectionAssert.AreEqual(new byte[] { 0x61 }, replay.GetPayloadCopy());
            Assert.That(quest.EnqueueSessionControl(new byte[] { 0x62 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That((await ReadIncomingAsync(desktop)).ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(desktop.SnapshotMetrics().OutstandingReliableFrames, Is.Zero);

            desktop.Dispose();
            quest.Dispose();
            secondPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        public async Task Resume_ReplaysMutationWithAuthoritativeCanonicalSequence()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 6500);
            var quest = CreateTransport(V2OriginDevice.Quest, 6600);
            using var firstPair = await LoopbackPeerPair.ConnectAsync();
            var dropped = new DropMessageKindWriteStream(firstPair.Client.GetStream(), V2TransportMessageKind.Application);
            Task<Exception> desktopRun = CaptureRunAsync(desktop, dropped, CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, firstPair.Server.GetStream(), CancellationToken.None);

            Assert.That(desktop.EnqueueMutation(Color("replayed-canonical"), 41UL, null, false).Accepted, Is.True);
            await AwaitGuardAsync(dropped.Dropped.Task);
            firstPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));

            using var secondPair = await LoopbackPeerPair.ConnectAsync();
            desktopRun = CaptureRunAsync(desktop, secondPair.Client.GetStream(), CancellationToken.None);
            questRun = CaptureRunAsync(quest, secondPair.Server.GetStream(), CancellationToken.None);
            V2TransportRecord replay = await ReadIncomingAsync(quest);

            Assert.That(replay.OriginSequence, Is.EqualTo(1UL));
            Assert.That(replay.CanonicalSequence, Is.EqualTo(41UL));
            Assert.That(replay.ObservedCanonicalSequence, Is.Null);
            Assert.That(V2MutationPayloadCodec.Decode(replay.GetPayloadCopy()), Is.TypeOf<SetSiteColor>());
            quest.Dispose();
            desktop.Dispose();
            secondPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        public async Task SchedulerLanes_KeepInteractiveWaitWithinEightRecordsOnTheWire()
        {
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var desktop = CreateTransport(V2OriginDevice.Desktop, 7000, limits);
            var quest = CreateTransport(V2OriginDevice.Quest, 8000, limits);
            SetSiteColor bulkIdentity = Color("bulk-target");
            V2ScheduleDescriptor bulkDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, bulkIdentity);
            V2EnqueueResult bulk = desktop.EnqueueSceneOperation(Bytes(512, 0xA1), bulkDescriptor, bodySchema: 17);
            Assert.That(bulk.Accepted, Is.True);
            for (int i = 0; i < 16; i++)
            {
                SetSiteColor mutation = Color("interactive-" + i);
                V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, mutation);
                Assert.That(desktop.EnqueueSceneOperation(new byte[] { (byte)i }, descriptor, coalesciblePreview: false).Accepted, Is.True);
            }

            using var pair = await LoopbackPeerPair.ConnectAsync();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, pair.Server.GetStream(), CancellationToken.None);
            int interactiveAfterDescriptor = 0;
            bool sawDescriptor = false;
            V2TransportRecord firstBulkChunk = null;
            for (int i = 0; i < 10; i++)
            {
                V2TransportRecord record = await ReadIncomingAsync(quest);
                if (record.MessageId != null && record.MessageId.Equals(bulk.OperationId) && !record.ChunkIndex.HasValue)
                {
                    sawDescriptor = true;
                    Assert.That(record.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
                }
                else if (record.Lane == V2ScheduleLane.Bulk)
                {
                    firstBulkChunk = record;
                    break;
                }
                else if (sawDescriptor && record.Lane == V2ScheduleLane.Interactive)
                    interactiveAfterDescriptor++;
            }

            Assert.That(sawDescriptor, Is.True);
            Assert.That(firstBulkChunk, Is.Not.Null);
            Assert.That(firstBulkChunk.ChunkIndex, Is.EqualTo(0));
            Assert.That(interactiveAfterDescriptor, Is.LessThanOrEqualTo(V2SchedulerLimits.DefaultInteractiveBurst));

            desktop.Dispose();
            quest.Dispose();
            pair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        public async Task UnacknowledgedLargeBulk_PartialCancellationLeavesControlAndInteractiveProgressAheadOfChunks()
        {
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, maxBulkBodyBytesPerTransfer: 8192, maxBulkBodyBytesTotal: 16384);
            var desktop = CreateTransport(V2OriginDevice.Desktop, 9000, limits);
            var quest = CreateTransport(V2OriginDevice.Quest, 10000, limits);
            SetSiteColor identity = Color("partial-bulk");
            V2ScheduleDescriptor bulkDescriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, identity);
            V2EnqueueResult bulk = desktop.EnqueueSceneOperation(Bytes(8192, 0xB2), bulkDescriptor, bodySchema: 7);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            var gate = new GateBulkWriteStream(pair.Client.GetStream());
            var droppedAcknowledgements = new DropMessageKindWriteStream(pair.Server.GetStream(), V2TransportMessageKind.Acknowledgement, true);
            Task<Exception> desktopRun = CaptureRunAsync(desktop, gate, CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, droppedAcknowledgements, CancellationToken.None);
            V2TransportRecord descriptor = await ReadIncomingAsync(quest);
            Assert.That(descriptor.MessageId, Is.EqualTo(bulk.OperationId));
            await AwaitGuardAsync(droppedAcknowledgements.Dropped.Task);
            await AwaitGuardAsync(gate.BulkWriteBlocked.Task);
            Assert.That(desktop.SnapshotMetrics().OutstandingReliableFrames, Is.EqualTo(2));

            Assert.That(desktop.CancelBulk(bulk.OperationId), Is.True);
            Assert.That(desktop.EnqueueSessionControl(new byte[] { 0xCA }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            SetSiteColor interactive = Color("after-cancel");
            Assert.That(desktop.EnqueueMutation(interactive, 19UL, null, false).Accepted, Is.True);
            gate.ReleaseBulkWrite();

            V2TransportRecord firstChunk = await ReadIncomingAsync(quest);
            V2TransportRecord cancel = await ReadIncomingAsync(quest);
            V2TransportRecord nextInteractive = await ReadIncomingAsync(quest);
            Assert.That(firstChunk.Lane, Is.EqualTo(V2ScheduleLane.Bulk));
            Assert.That(firstChunk.ChunkIndex, Is.EqualTo(0));
            Assert.That(cancel.Lane, Is.EqualTo(V2ScheduleLane.SessionControl));
            Assert.That(cancel.ReliableFrameSequence, Is.EqualTo(1UL));
            Assert.That(nextInteractive.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
            Assert.That(nextInteractive.OriginSequence, Is.EqualTo(2UL));
            Assert.That(desktop.SnapshotMetrics().RetainedBulkBodyBytes, Is.Zero);

            desktop.Dispose();
            quest.Dispose();
            pair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        public async Task OriginSequenceGap_FaultsTheScopedProtocolWithoutDeliveringTheRecord()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 11000);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), CancellationToken.None);
            V2TransportRecord clientHello = await V2TransportFrameCodec.ReadAsync(pair.Server.GetStream(), CancellationToken.None);
            Assert.That(clientHello.Kind, Is.EqualTo(V2TransportMessageKind.ResumeHello));
            var peerHello = new V2TransportRecord(V2TransportMessageKind.ResumeHello, Session, originDevice: V2OriginDevice.Quest, payload: V2ResumeWatermarkCodec.Encode(new System.Collections.Generic.Dictionary<ReliableStreamId, ulong>()));
            await V2TransportFrameCodec.WriteAsync(pair.Server.GetStream(), peerHello, CancellationToken.None);
            Task<Exception> pendingRead = CaptureIncomingReadAsync(desktop);
            var gap = new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000099")), new ReliableStreamId(Guid.Parse("50000000-0000-0000-0000-000000000099")), 1, 2, V2OriginDevice.Quest, V2ScheduleLane.Interactive, 1, payload: new byte[] { 0x01 });
            await V2TransportFrameCodec.WriteAsync(pair.Server.GetStream(), gap, CancellationToken.None);

            Exception readFailure = await AwaitGuardResultAsync(pendingRead);
            Exception failure = await AwaitGuardResultAsync(desktopRun);
            Assert.That(readFailure, Is.TypeOf<V2TransportProtocolException>());
            Assert.That(failure, Is.TypeOf<V2TransportProtocolException>());
            Assert.That(desktop.State, Is.EqualTo(V2PersistentTransportState.Faulted));
            desktop.Dispose();
            pair.Close();
        }

        [Test]
        public async Task IncomingBackpressure_DoesNotCommitOriginBeforeResumeAdmission()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 14000);
            var streamId = new ReliableStreamId(GuidFor(15000));
            using var firstPair = await LoopbackPeerPair.ConnectAsync();
            Task<Exception> firstRun = CaptureRunAsync(desktop, firstPair.Client.GetStream(), CancellationToken.None);
            Assert.That((await V2TransportFrameCodec.ReadAsync(firstPair.Server.GetStream(), CancellationToken.None)).Kind, Is.EqualTo(V2TransportMessageKind.ResumeHello));
            await V2TransportFrameCodec.WriteAsync(firstPair.Server.GetStream(), PeerHello(), CancellationToken.None);

            for (int sequence = 1; sequence <= 257; sequence++)
                await V2TransportFrameCodec.WriteAsync(firstPair.Server.GetStream(), IncomingMutation(streamId, sequence), CancellationToken.None);

            Exception backpressure = await AwaitGuardResultAsync(firstRun);
            Assert.That(backpressure, Is.TypeOf<V2TransportBackpressureException>());
            Assert.That(desktop.State, Is.EqualTo(V2PersistentTransportState.DisconnectedGrace));
            Assert.That((await desktop.ReadIncomingAsync(CancellationToken.None)).OriginSequence, Is.EqualTo(1UL));

            using var resumedPair = await LoopbackPeerPair.ConnectAsync();
            Task<Exception> resumedRun = CaptureRunAsync(desktop, resumedPair.Client.GetStream(), CancellationToken.None);
            V2TransportRecord resume = await V2TransportFrameCodec.ReadAsync(resumedPair.Server.GetStream(), CancellationToken.None);
            Assert.That(V2ResumeWatermarkCodec.Decode(resume.GetPayloadCopy()).TryGetValue(streamId, out ulong watermark), Is.True);
            Assert.That(watermark, Is.EqualTo(256UL));
            await V2TransportFrameCodec.WriteAsync(resumedPair.Server.GetStream(), PeerHello(), CancellationToken.None);
            await V2TransportFrameCodec.WriteAsync(resumedPair.Server.GetStream(), IncomingMutation(streamId, 257), CancellationToken.None);

            for (int sequence = 2; sequence <= 256; sequence++)
                Assert.That((await desktop.ReadIncomingAsync(CancellationToken.None)).ReliableFrameSequence, Is.EqualTo((ulong)sequence));
            V2TransportRecord admitted = await ReadIncomingAsync(desktop);
            Assert.That(admitted.ReliableFrameSequence, Is.EqualTo(257UL));
            Assert.That(admitted.OriginSequence, Is.EqualTo(257UL));

            desktop.Dispose();
            resumedPair.Close();
            await AwaitGuardAsync(resumedRun);
        }

        [Test]
        public async Task Reconnect_SendsResumeHelloBeforeQueuedEphemeralControls()
        {
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 64, maxBulkBodyBytesPerTransfer: 1024, maxBulkBodyBytesTotal: 2048);
            var desktop = CreateTransport(V2OriginDevice.Desktop, 16000, limits);
            var quest = CreateTransport(V2OriginDevice.Quest, 17000, limits);
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, Color("resume-control"));
            Assert.That(desktop.EnqueueSceneOperation(Bytes(512, 0xC7), descriptor, bodySchema: 8).Accepted, Is.True);

            using var firstPair = await LoopbackPeerPair.ConnectAsync();
            var gated = new GateBulkWriteStream(firstPair.Client.GetStream());
            Task<Exception> desktopRun = CaptureRunAsync(desktop, gated, CancellationToken.None);
            Task<Exception> questRun = CaptureRunAsync(quest, firstPair.Server.GetStream(), CancellationToken.None);
            Assert.That((await ReadIncomingAsync(quest)).Lane, Is.EqualTo(V2ScheduleLane.Interactive));
            await AwaitGuardAsync(gated.BulkWriteBlocked.Task);
            Assert.That(desktop.SendLivenessProbe(), Is.Not.EqualTo(Guid.Empty));

            firstPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));

            using var resumedPair = await LoopbackPeerPair.ConnectAsync();
            var firstFrame = new FirstFrameCaptureStream(resumedPair.Client.GetStream());
            desktopRun = CaptureRunAsync(desktop, firstFrame, CancellationToken.None);
            questRun = CaptureRunAsync(quest, resumedPair.Server.GetStream(), CancellationToken.None);
            await AwaitGuardAsync(firstFrame.FirstRecord.Task);
            V2TransportRecord first = await firstFrame.FirstRecord.Task;
            Assert.That(first.Kind, Is.EqualTo(V2TransportMessageKind.ResumeHello));

            desktop.Dispose();
            quest.Dispose();
            resumedPair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        public async Task Shutdown_UnblocksReadWithEmptyWriterAndBlockedWriteWithPendingControl()
        {
            var idle = CreateTransport(V2OriginDevice.Desktop, 12000);
            using (var idlePeer = new PeerProvidedStream(Session, V2OriginDevice.Quest, false))
            {
                Task<Exception> idleRun = CaptureRunAsync(idle, idlePeer, CancellationToken.None);
                await AwaitGuardAsync(idlePeer.ReadBlocked.Task);
                idle.Dispose();
                Assert.That(await AwaitGuardResultAsync(idleRun), Is.Null);
            }

            var limits = new V2SchedulerLimits(maxSessionControlQueuedRecords: 2, reservedSessionControlRecords: 0, reservedSessionControlBytes: 0, bulkChunkBytes: 256, maxReliableFrames: 1, reservedReliableFrames: 0, maxReliableBytes: 1024, reservedReliableBytes: 0);
            var full = CreateTransport(V2OriginDevice.Desktop, 13000, limits);
            Assert.That(full.EnqueueSessionControl(new byte[] { 1 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(full.EnqueueSessionControl(new byte[] { 2 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            using (var blockedPeer = new PeerProvidedStream(Session, V2OriginDevice.Quest, true))
            {
                Task<Exception> fullRun = CaptureRunAsync(full, blockedPeer, CancellationToken.None);
                await AwaitGuardAsync(blockedPeer.ApplicationWriteBlocked.Task);
                full.Dispose();
                Assert.That(await AwaitGuardResultAsync(fullRun), Is.Null);
            }
        }

        [Test]
        public async Task RequiredSessionControlOverflow_FaultsAndClosesTheActiveTransport()
        {
            var limits = new V2SchedulerLimits(maxSessionControlQueuedRecords: 2, reservedSessionControlRecords: 0, reservedSessionControlBytes: 0);
            var transport = CreateTransport(V2OriginDevice.Desktop, 19000, limits);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            NetworkStream peerStream = pair.Server.GetStream();
            var gated = new GateApplicationWriteStream(pair.Client.GetStream());
            Task<Exception> run = CaptureRunAsync(transport, gated, CancellationToken.None);

            Assert.That((await V2TransportFrameCodec.ReadAsync(peerStream, timeout.Token)).Kind, Is.EqualTo(V2TransportMessageKind.ResumeHello));
            await V2TransportFrameCodec.WriteAsync(peerStream, PeerHello(), timeout.Token);
            Assert.That(transport.EnqueueSessionControl(new byte[] { 1 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            await AwaitGuardAsync(gated.ApplicationWriteBlocked.Task);

            Assert.That(transport.EnqueueSessionControl(new byte[] { 2 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(transport.EnqueueSessionControl(new byte[] { 3 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Task<V2TransportRecord> peerReadAfterClose = V2TransportFrameCodec.ReadAsync(peerStream, timeout.Token);
            V2EnqueueResult overflow = transport.EnqueueSessionControl(new byte[] { 4 }, V2DeliveryReliability.Reliable);

            Assert.That(overflow.Accepted, Is.False);
            Assert.That(overflow.Disposition, Is.EqualTo(V2EnqueueDisposition.SessionFaulted));
            Assert.That(transport.State, Is.EqualTo(V2PersistentTransportState.Faulted));
            Assert.That(await AwaitGuardResultAsync(run), Is.TypeOf<V2TransportProtocolException>());
            await AwaitGuardAsync(peerReadAfterClose);
            Assert.That(await peerReadAfterClose, Is.Null);
            transport.Dispose();
        }

        [Test]
        public async Task RequiredWrongSceneRejectionOverflow_FaultsAndClosesTheActiveTransport()
        {
            var limits = new V2SchedulerLimits(maxSessionControlQueuedRecords: 2, reservedSessionControlRecords: 0, reservedSessionControlBytes: 0);
            var transport = CreateTransport(V2OriginDevice.Desktop, 20000, limits);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            NetworkStream peerStream = pair.Server.GetStream();
            var gated = new GateApplicationWriteStream(pair.Client.GetStream());
            Task<Exception> run = CaptureRunAsync(transport, gated, CancellationToken.None);

            Assert.That((await V2TransportFrameCodec.ReadAsync(peerStream, timeout.Token)).Kind, Is.EqualTo(V2TransportMessageKind.ResumeHello));
            await V2TransportFrameCodec.WriteAsync(peerStream, PeerHello(), timeout.Token);
            Assert.That(transport.EnqueueSessionControl(new byte[] { 1 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            await AwaitGuardAsync(gated.ApplicationWriteBlocked.Task);
            Assert.That(transport.EnqueueSessionControl(new byte[] { 2 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Assert.That(transport.EnqueueSessionControl(new byte[] { 3 }, V2DeliveryReliability.Reliable).Accepted, Is.True);
            Task<V2TransportRecord> peerReadAfterClose = V2TransportFrameCodec.ReadAsync(peerStream, timeout.Token);

            var wrongScene = new V2TransportRecord(V2TransportMessageKind.Application, Session, new SceneId(Guid.Parse("60000000-0000-0000-0000-000000000006")), Incarnation, new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000099")), new ReliableStreamId(Guid.Parse("50000000-0000-0000-0000-000000000099")), 1, 1, V2OriginDevice.Quest, V2ScheduleLane.Interactive, 1, payload: new byte[] { 0x41 });
            await V2TransportFrameCodec.WriteAsync(peerStream, wrongScene, timeout.Token);

            Assert.That(await AwaitGuardResultAsync(run), Is.TypeOf<V2TransportProtocolException>());
            Assert.That(transport.State, Is.EqualTo(V2PersistentTransportState.Faulted));
            await AwaitGuardAsync(peerReadAfterClose);
            Assert.That(await peerReadAfterClose, Is.Null);
            transport.Dispose();
            pair.Close();
        }

        [Test]
        public async Task Dispose_CompletesAllPendingIncomingReaders()
        {
            var transport = CreateTransport(V2OriginDevice.Desktop, 21000);
            Task<Exception> first = CaptureIncomingReadAsync(transport);
            Task<Exception> second = CaptureIncomingReadAsync(transport);

            transport.Dispose();
            Exception[] results = await Task.WhenAll(first, second);

            Assert.That(results, Has.Length.EqualTo(2));
            Assert.That(results[0], Is.TypeOf<ObjectDisposedException>());
            Assert.That(results[1], Is.TypeOf<ObjectDisposedException>());
        }

        private static V2PersistentTransport CreateTransport(V2OriginDevice origin, int guidSeed, V2SchedulerLimits limits = null)
        {
            int next = guidSeed;
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, origin, limits: limits, guidFactory: () => GuidFor(next++));
            return new V2PersistentTransport(scheduler, TimeSpan.FromHours(1), () => GuidFor(next++));
        }

        private static V2TransportRecord PeerHello() => new V2TransportRecord(V2TransportMessageKind.ResumeHello, Session, originDevice: V2OriginDevice.Quest, payload: V2ResumeWatermarkCodec.Encode(new System.Collections.Generic.Dictionary<ReliableStreamId, ulong>()));

        private static V2TransportRecord IncomingMutation(ReliableStreamId streamId, int sequence) => new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, new OperationId(GuidFor(18000 + sequence)), streamId, (ulong)sequence, (ulong)sequence, V2OriginDevice.Quest, V2ScheduleLane.Interactive, 1, payload: new byte[] { (byte)sequence });

        private static Guid GuidFor(int value) => Guid.Parse("00000000-0000-0000-0000-" + value.ToString("D12"));
        private static SetSiteColor Color(string site) => new SetSiteColor(new ColumnId("column"), new SiteId(site), 0.2f, 0.3f, 0.4f, 1f);

        private static byte[] Bytes(int length, byte value)
        {
            var bytes = new byte[length];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = value;
            return bytes;
        }

        private static async Task<Exception> CaptureRunAsync(V2PersistentTransport transport, Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                await transport.RunConnectionAsync(stream, cancellationToken);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static async Task<Exception> CaptureIncomingReadAsync(V2PersistentTransport transport)
        {
            try
            {
                await transport.ReadIncomingAsync(CancellationToken.None);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static async Task<V2TransportRecord> ReadIncomingAsync(V2PersistentTransport transport)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return await transport.ReadIncomingAsync(timeout.Token);
        }

        private static async Task AwaitGuardAsync(Task task)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
            if (completed != task)
                throw new TimeoutException("The loopback transport did not reach its deterministic barrier.");
            await task;
        }

        private static async Task<Exception> AwaitGuardResultAsync(Task<Exception> task)
        {
            await AwaitGuardAsync(task);
            return await task;
        }

        private sealed class LoopbackPeerPair : IDisposable
        {
            public TcpClient Client { get; }
            public TcpClient Server { get; }

            private LoopbackPeerPair(TcpClient client, TcpClient server)
            {
                Client = client;
                Server = server;
            }

            public static async Task<LoopbackPeerPair> ConnectAsync()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                try
                {
                    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    Task<TcpClient> accepted = listener.AcceptTcpClientAsync();
                    var client = new TcpClient { NoDelay = true };
                    await client.ConnectAsync(IPAddress.Loopback, port);
                    TcpClient server = await accepted;
                    server.NoDelay = true;
                    return new LoopbackPeerPair(client, server);
                }
                finally
                {
                    listener.Stop();
                }
            }

            public void Close()
            {
                Client.Close();
                Server.Close();
            }

            public void Dispose() => Close();
        }

        private sealed class DropMessageKindWriteStream : Stream
        {
            private readonly Stream m_Inner;
            private readonly V2TransportMessageKind m_Kind;
            private readonly bool m_DropEveryMatch;
            private int m_Dropped;
            public TaskCompletionSource<bool> Dropped { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public V2TransportRecord DroppedRecord { get; private set; }

            public DropMessageKindWriteStream(Stream inner, V2TransportMessageKind kind, bool dropEveryMatch = false)
            {
                m_Inner = inner;
                m_Kind = kind;
                m_DropEveryMatch = dropEveryMatch;
            }

            public override bool CanRead => m_Inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => m_Inner.CanWrite;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => m_Inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => m_Inner.Read(buffer, offset, count);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => m_Inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override void Write(byte[] buffer, int offset, int count) => m_Inner.Write(buffer, offset, count);

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (count >= V2TransportFrameCodec.HeaderLength && ReadKind(buffer, offset) == m_Kind && (m_DropEveryMatch || Interlocked.CompareExchange(ref m_Dropped, 1, 0) == 0))
                {
                    var frame = new byte[count];
                    Buffer.BlockCopy(buffer, offset, frame, 0, count);
                    DroppedRecord = V2TransportFrameCodec.Decode(frame);
                    Interlocked.Exchange(ref m_Dropped, 1);
                    Dropped.TrySetResult(true);
                    return;
                }

                await m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) m_Inner.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class DuplicateBulkAcknowledgementWriteStream : Stream
        {
            private readonly Stream m_Inner;
            private int m_Duplicated;
            public TaskCompletionSource<bool> Duplicated { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public DuplicateBulkAcknowledgementWriteStream(Stream inner) => m_Inner = inner;
            public override bool CanRead => m_Inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => m_Inner.CanWrite;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => m_Inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => m_Inner.Read(buffer, offset, count);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => m_Inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override void Write(byte[] buffer, int offset, int count) => m_Inner.Write(buffer, offset, count);

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (count >= V2TransportFrameCodec.HeaderLength && ReadKind(buffer, offset) == V2TransportMessageKind.Acknowledgement)
                {
                    var frame = new byte[count];
                    Buffer.BlockCopy(buffer, offset, frame, 0, count);
                    V2TransportRecord record = V2TransportFrameCodec.Decode(frame);
                    if (V2BulkStreamIdentityCodec.TryGetOrdinal(Session, V2OriginDevice.Desktop, record.StreamId, out _) && Interlocked.CompareExchange(ref m_Duplicated, 1, 0) == 0)
                    {
                        await m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
                        await m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
                        Duplicated.TrySetResult(true);
                        return;
                    }
                }

                await m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) m_Inner.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class FirstFrameCaptureStream : Stream
        {
            private readonly Stream m_Inner;
            private int m_Captured;
            public TaskCompletionSource<V2TransportRecord> FirstRecord { get; } = new TaskCompletionSource<V2TransportRecord>(TaskCreationOptions.RunContinuationsAsynchronously);

            public FirstFrameCaptureStream(Stream inner) => m_Inner = inner;
            public override bool CanRead => m_Inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => m_Inner.CanWrite;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => m_Inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => m_Inner.Read(buffer, offset, count);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => m_Inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override void Write(byte[] buffer, int offset, int count) => m_Inner.Write(buffer, offset, count);

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (count >= V2TransportFrameCodec.HeaderLength && Interlocked.CompareExchange(ref m_Captured, 1, 0) == 0)
                {
                    var frame = new byte[count];
                    Buffer.BlockCopy(buffer, offset, frame, 0, count);
                    FirstRecord.TrySetResult(V2TransportFrameCodec.Decode(frame));
                }

                await m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) m_Inner.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class GateBulkWriteStream : Stream
        {
            private readonly Stream m_Inner;
            private int m_Blocked;
            private readonly TaskCompletionSource<bool> m_Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> BulkWriteBlocked { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public GateBulkWriteStream(Stream inner) => m_Inner = inner;
            public override bool CanRead => m_Inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => m_Inner.CanWrite;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => m_Inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => m_Inner.Read(buffer, offset, count);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => m_Inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override void Write(byte[] buffer, int offset, int count) => m_Inner.Write(buffer, offset, count);

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (count >= V2TransportFrameCodec.HeaderLength && ReadKind(buffer, offset) == V2TransportMessageKind.Application && buffer[offset + 129] == (byte)V2ScheduleLane.Bulk && Interlocked.CompareExchange(ref m_Blocked, 1, 0) == 0)
                {
                    BulkWriteBlocked.TrySetResult(true);
                    using (cancellationToken.Register(() => m_Release.TrySetCanceled()))
                        await AwaitWithCancellationAsync(m_Release.Task, cancellationToken);
                }

                await m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public void ReleaseBulkWrite() => m_Release.TrySetResult(true);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) m_Inner.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class GateApplicationWriteStream : Stream
        {
            private readonly Stream m_Inner;
            private readonly TaskCompletionSource<bool> m_Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private int m_Blocked;
            public TaskCompletionSource<bool> ApplicationWriteBlocked { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public GateApplicationWriteStream(Stream inner) => m_Inner = inner;
            public override bool CanRead => m_Inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => m_Inner.CanWrite;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => m_Inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => m_Inner.Read(buffer, offset, count);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => m_Inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override void Write(byte[] buffer, int offset, int count) => m_Inner.Write(buffer, offset, count);

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (count >= V2TransportFrameCodec.HeaderLength && ReadKind(buffer, offset) == V2TransportMessageKind.Application && Interlocked.CompareExchange(ref m_Blocked, 1, 0) == 0)
                {
                    ApplicationWriteBlocked.TrySetResult(true);
                    using (cancellationToken.Register(() => m_Release.TrySetCanceled()))
                        await AwaitWithCancellationAsync(m_Release.Task, cancellationToken);
                }

                await m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) m_Inner.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class PeerProvidedStream : Stream
        {
            private readonly byte[] m_Hello;
            private readonly bool m_BlockApplicationWrite;
            private int m_ReadOffset;
            public TaskCompletionSource<bool> ReadBlocked { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> ApplicationWriteBlocked { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public PeerProvidedStream(SessionId session, V2OriginDevice origin, bool blockApplicationWrite)
            {
                m_BlockApplicationWrite = blockApplicationWrite;
                m_Hello = V2TransportFrameCodec.Encode(new V2TransportRecord(V2TransportMessageKind.ResumeHello, session, originDevice: origin, payload: V2ResumeWatermarkCodec.Encode(new System.Collections.Generic.Dictionary<ReliableStreamId, ulong>())));
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (m_ReadOffset < m_Hello.Length)
                {
                    int copied = Math.Min(count, m_Hello.Length - m_ReadOffset);
                    Buffer.BlockCopy(m_Hello, m_ReadOffset, buffer, offset, copied);
                    m_ReadOffset += copied;
                    return copied;
                }

                ReadBlocked.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return 0;
            }

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (m_BlockApplicationWrite && count >= V2TransportFrameCodec.HeaderLength && ReadKind(buffer, offset) == V2TransportMessageKind.Application)
                {
                    ApplicationWriteBlocked.TrySetResult(true);
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        private static V2TransportMessageKind ReadKind(byte[] bytes, int offset) => (V2TransportMessageKind)(bytes[offset + 6] | bytes[offset + 7] << 8);

        private static async Task AwaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
        {
            Task cancelled = Task.Delay(Timeout.Infinite, cancellationToken);
            Task completed = await Task.WhenAny(task, cancelled);
            cancellationToken.ThrowIfCancellationRequested();
            await completed;
        }
    }
}
