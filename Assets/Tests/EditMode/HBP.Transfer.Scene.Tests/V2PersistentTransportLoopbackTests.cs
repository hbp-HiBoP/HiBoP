using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

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
        [Category("Sync.SceneFocused")]
        public async Task InlineSceneOperation_PreservesItsBodySchemaOnTheWire()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 1500);
            var quest = CreateTransport(V2OriginDevice.Quest, 1600);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var stop = new CancellationTokenSource();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), stop.Token);
            Task<Exception> questRun = CaptureRunAsync(quest, pair.Server.GetStream(), stop.Token);
            var operation = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000016"));
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, null, V2BarrierScope.AllScene);

            Assert.That(desktop.EnqueueSceneOperation(new byte[] { 0x48, 0x42, 0x43, 0x50 }, descriptor, structural: true, bodySchema: 2, operationId: operation).Accepted, Is.True);
            V2TransportRecord received = await ReadIncomingAsync(quest);

            Assert.That(received.MessageId, Is.EqualTo(operation));
            Assert.That(received.Lane, Is.EqualTo(V2ScheduleLane.SceneControl));
            Assert.That(received.BodySchema, Is.EqualTo(2));
            CollectionAssert.AreEqual(new byte[] { 0x48, 0x42, 0x43, 0x50 }, received.GetPayloadCopy());

            desktop.Dispose();
            quest.Dispose();
            pair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestProposalRejection_DeliversSchemaThreeDecisionOnInteractiveLane()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 1550);
            var quest = CreateTransport(V2OriginDevice.Quest, 1650);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var stop = new CancellationTokenSource();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), stop.Token);
            Task<Exception> questRun = CaptureRunAsync(quest, pair.Server.GetStream(), stop.Token);
            var operation = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000056"));
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(Scene, Incarnation, Color("conflicting-site"));
            byte[] body = V2QuestProposalDecisionCodec.EncodeRejection(operation, "stale_sequence");

            Assert.That(desktop.EnqueueSceneOperation(body, descriptor, bodySchema: V2QuestProposalDecisionCodec.BodySchema, operationId: operation).Accepted, Is.True);
            V2TransportRecord received = await ReadIncomingAsync(quest);
            V2QuestProposalDecision decision = V2QuestProposalDecisionCodec.Decode(received.GetPayloadCopy(), Scene, Incarnation);

            Assert.That(received.MessageId, Is.EqualTo(operation));
            Assert.That(received.OriginDevice, Is.EqualTo(V2OriginDevice.Desktop));
            Assert.That(received.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
            Assert.That(received.BodySchema, Is.EqualTo(V2QuestProposalDecisionCodec.BodySchema));
            Assert.That(decision.OperationId, Is.EqualTo(operation));
            Assert.That(decision.RejectionCode, Is.EqualTo("stale_sequence"));
            Assert.That(decision.Correction, Is.Null);

            desktop.Dispose();
            quest.Dispose();
            pair.Close();
            await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task CheckpointBulkReceiver_ReassemblesBoundedSchemaTwoBody()
        {
            var desktop = CreateTransport(V2OriginDevice.Desktop, 1700);
            var quest = CreateTransport(V2OriginDevice.Quest, 1800);
            byte[] expected = Bytes(V2SchedulerLimits.DefaultInlineThresholdBytes + 904, 0x6A);
            var operation = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000017"));
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, null, V2BarrierScope.AllScene);
            Assert.That(desktop.EnqueueSceneOperation(expected, descriptor, structural: true, bodySchema: V2PublicationCheckpointBulkReceiver.BodySchema, operationId: operation).Accepted, Is.True);

            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var stop = new CancellationTokenSource();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), stop.Token);
            Task<Exception> questRun = CaptureRunAsync(quest, pair.Server.GetStream(), stop.Token);
            var receiver = new V2PublicationCheckpointBulkReceiver();
            byte[] actual = null;
            OperationId completedOperation = null;
            while (actual == null)
            {
                V2TransportRecord record = await ReadIncomingAsync(quest);
                if (!receiver.IsActive)
                {
                    Assert.That(receiver.IsCheckpointDescriptor(record), Is.True);
                    receiver.Begin(record, V2OriginDevice.Desktop);
                    continue;
                }

                Assert.That(receiver.TryAppend(record, out actual, out completedOperation), Is.True);
            }

            Assert.That(completedOperation, Is.EqualTo(operation));
            CollectionAssert.AreEqual(expected, actual);
            Assert.That(receiver.IsActive, Is.False);
            receiver.Reset();
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
        [Category("Sync.SceneFocused")]
        public async Task QuestSession_CheckpointMutationReceivedBeforeDisconnectIsAppliedOnceAfterReplay()
        {
            using var fixture = new SessionSceneFixture(16);
            PreparedSceneDeliveryBinding binding = CreatePreparedBinding();
            object questOwner = CreateQuestSession(fixture.Scene, binding);
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 128);
            var desktopPeer = CreateTransport(V2OriginDevice.Desktop, 5070, limits);
            var appliedColor = new Color(0.2f, 0.7f, 0.4f, 1f);
            SetSiteColor mutation = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), appliedColor.r, appliedColor.g, appliedColor.b, appliedColor.a);
            OperationId checkpointId = new OperationId(GuidFor(55071));
            OperationId mutationId = new OperationId(GuidFor(55072));
            OperationId barrierId = new OperationId(GuidFor(55073));
            V2ScheduleDescriptor allScene = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, null, V2BarrierScope.AllScene);
            byte[] checkpoint;
            using (var sourceBoundary = new V2SceneMutationBoundary(fixture.Scene, V2OriginDevice.Desktop))
                checkpoint = V2SceneMutationCheckpointCodec.Encode(0, sourceBoundary.CaptureCheckpoint());

            Assert.That(checkpoint.Length, Is.GreaterThan(limits.InlineThresholdBytes), "The fixture must exercise the checkpoint bulk path.");
            Assert.That(desktopPeer.EnqueueSceneOperation(checkpoint, allScene, structural: true, bodySchema: V2PublicationCheckpointBulkReceiver.BodySchema, operationId: checkpointId).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueMutation(mutation, 1UL, null, coalesciblePreview: false, operationId: mutationId).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueSceneOperation(V2PublicationControlCodec.EncodeLiveBarrier(1), allScene, structural: true, operationId: barrierId).Accepted, Is.True);

            int matchingColorApplications = 0;
            Action<SiteState> countMutationApply = state =>
            {
                if (ReferenceEquals(state, fixture.Sites[0].State) && state.Color == appliedColor) matchingColorApplications++;
            };
            SiteState.ColorChanged += countMutationApply;
            LoopbackPeerPair firstPair = await LoopbackPeerPair.ConnectAsync();
            var droppedBulk = new DropBulkWriteStream(firstPair.Client.GetStream());
            Task<Exception> firstDesktopRun = CaptureRunAsync(desktopPeer, droppedBulk, CancellationToken.None);
            Task<Exception> firstQuestRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, firstPair.Server.GetStream(), CancellationToken.None));

            try
            {
                await AwaitGuardAsync(droppedBulk.Dropped.Task);
                await WaitUntilAsync(() => GetDeferredRecords(questOwner).Count == 2, "Quest did not retain the interleaved mutation and barrier before checkpoint completion.");
                List<V2TransportRecord> beforeDisconnect = GetDeferredRecords(questOwner);
                Assert.That(beforeDisconnect[0].MessageId, Is.EqualTo(mutationId));
                Assert.That(beforeDisconnect[1].MessageId, Is.EqualTo(barrierId));
                Assert.That(GetCheckpointReceiverActive(questOwner), Is.True);
                Assert.That(matchingColorApplications, Is.Zero, "The interleaved mutation must wait for the incomplete checkpoint.");
                Assert.That(GetDeferredByteCount(questOwner), Is.GreaterThan(0));

                firstPair.Close();
                await AwaitGuardAsync(Task.WhenAll(firstDesktopRun, firstQuestRun));
                Assert.That(desktopPeer.State, Is.EqualTo(V2PersistentTransportState.DisconnectedGrace));
                Assert.That((bool)questOwner.GetType().GetProperty("CanResumeConnection").GetValue(questOwner), Is.True);
                Assert.That(GetDeferredRecords(questOwner), Has.Count.EqualTo(2), "Disconnect must not discard the queue whose records have transport ACKs.");
                Assert.That(GetCheckpointReceiverActive(questOwner), Is.True, "The partial checkpoint receiver must remain on the retained Quest owner.");

                using var resumedPair = await LoopbackPeerPair.ConnectAsync();
                Task<Exception> resumedDesktopRun = CaptureRunAsync(desktopPeer, resumedPair.Client.GetStream(), CancellationToken.None);
                Task<Exception> resumedQuestRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, resumedPair.Server.GetStream(), CancellationToken.None));
                V2TransportRecord acknowledgement = await ReadIncomingAsync(desktopPeer);
                Assert.That(V2PublicationControlCodec.TryDecodeAcknowledgement(acknowledgement.GetPayloadCopy(), out OperationId acknowledgedBarrier), Is.True);
                Assert.That(acknowledgedBarrier, Is.EqualTo(barrierId));
                Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(appliedColor));
                Assert.That(matchingColorApplications, Is.EqualTo(1), "The retained interleaved canonical mutation must apply once after checkpoint replay.");
                Assert.That(GetDeferredRecords(questOwner), Is.Empty);
                Assert.That(GetDeferredByteCount(questOwner), Is.Zero);
                Assert.That(GetCheckpointReceiverActive(questOwner), Is.False);
                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(1UL));

                ((IDisposable)questOwner).Dispose();
                desktopPeer.Dispose();
                resumedPair.Close();
                await AwaitGuardAsync(Task.WhenAll(resumedDesktopRun, resumedQuestRun));
            }
            finally
            {
                SiteState.ColorChanged -= countMutationApply;
                ((IDisposable)questOwner).Dispose();
                desktopPeer.Dispose();
                firstPair.Close();
                await AwaitGuardAsync(Task.WhenAll(firstDesktopRun, firstQuestRun));
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestSession_DisconnectAfterFinalCheckpointChunkRetainsCheckpointUntilReplay()
        {
            using var fixture = new SessionSceneFixture(16);
            PreparedSceneDeliveryBinding binding = CreatePreparedBinding();
            var checkpointApplyStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int checkpointApplyAttempts = 0;
            Func<CancellationToken, Task> beforeCheckpointApply = async stop =>
            {
                if (Interlocked.Increment(ref checkpointApplyAttempts) == 1)
                {
                    checkpointApplyStarted.TrySetResult(true);
                    await Task.Delay(Timeout.Infinite, stop);
                }
            };
            object questOwner = CreateQuestSession(fixture.Scene, binding, beforeCheckpointApply, null);
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 128);
            var desktopPeer = CreateTransport(V2OriginDevice.Desktop, 5080, limits);
            Color appliedColor = new Color(0.31f, 0.62f, 0.83f, 1f);
            var mutation = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), appliedColor.r, appliedColor.g, appliedColor.b, appliedColor.a);
            OperationId checkpointId = new OperationId(GuidFor(55081));
            OperationId mutationId = new OperationId(GuidFor(55082));
            OperationId barrierId = new OperationId(GuidFor(55083));
            V2ScheduleDescriptor allScene = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, null, V2BarrierScope.AllScene);
            byte[] checkpoint;
            using (var sourceBoundary = new V2SceneMutationBoundary(fixture.Scene, V2OriginDevice.Desktop))
                checkpoint = V2SceneMutationCheckpointCodec.Encode(0, sourceBoundary.CaptureCheckpoint());

            Assert.That(checkpoint.Length, Is.GreaterThan(limits.InlineThresholdBytes));
            Assert.That(desktopPeer.EnqueueSceneOperation(checkpoint, allScene, structural: true, bodySchema: V2PublicationCheckpointBulkReceiver.BodySchema, operationId: checkpointId).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueMutation(mutation, 1UL, null, coalesciblePreview: false, operationId: mutationId).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueSceneOperation(V2PublicationControlCodec.EncodeLiveBarrier(1), allScene, structural: true, operationId: barrierId).Accepted, Is.True);

            int matchingColorApplications = 0;
            Action<SiteState> countMutationApply = state =>
            {
                if (ReferenceEquals(state, fixture.Sites[0].State) && state.Color == appliedColor) matchingColorApplications++;
            };
            SiteState.ColorChanged += countMutationApply;
            LoopbackPeerPair firstPair = await LoopbackPeerPair.ConnectAsync();
            Task<Exception> firstDesktopRun = CaptureRunAsync(desktopPeer, firstPair.Client.GetStream(), CancellationToken.None);
            Task<Exception> firstQuestRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, firstPair.Server.GetStream(), CancellationToken.None));
            LoopbackPeerPair resumedPair = null;
            Task<Exception> resumedDesktopRun = null;
            Task<Exception> resumedQuestRun = null;

            try
            {
                await AwaitGuardAsync(checkpointApplyStarted.Task);
                Assert.That(GetCheckpointReceiverActive(questOwner), Is.False, "The final chunk must have completed and reset the bulk receiver.");
                Assert.That(GetCompletedCheckpoint(questOwner), Is.Not.Null, "The completed bytes must be retained before application starts.");
                Assert.That(GetDeferredDrainPending(questOwner), Is.True);
                List<V2TransportRecord> deferred = GetDeferredRecords(questOwner);
                Assert.That(deferred, Has.Count.EqualTo(2));
                Assert.That(deferred[0].MessageId, Is.EqualTo(mutationId));
                Assert.That(deferred[1].MessageId, Is.EqualTo(barrierId));
                Assert.That(matchingColorApplications, Is.Zero);

                firstPair.Close();
                await AwaitGuardAsync(Task.WhenAll(firstDesktopRun, firstQuestRun));
                Assert.That((bool)questOwner.GetType().GetProperty("CanResumeConnection").GetValue(questOwner), Is.True);
                Assert.That(GetCompletedCheckpoint(questOwner), Is.Not.Null, "A disconnect before application must leave the completed checkpoint with the same owner.");
                Assert.That(GetDeferredRecords(questOwner), Has.Count.EqualTo(2));

                resumedPair = await LoopbackPeerPair.ConnectAsync();
                resumedDesktopRun = CaptureRunAsync(desktopPeer, resumedPair.Client.GetStream(), CancellationToken.None);
                resumedQuestRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, resumedPair.Server.GetStream(), CancellationToken.None));
                V2TransportRecord acknowledgement = await ReadIncomingAsync(desktopPeer);
                Assert.That(V2PublicationControlCodec.TryDecodeAcknowledgement(acknowledgement.GetPayloadCopy(), out OperationId acknowledgedBarrier), Is.True);
                Assert.That(acknowledgedBarrier, Is.EqualTo(barrierId));
                Assert.That(checkpointApplyAttempts, Is.EqualTo(2), "The retained checkpoint is resumed once on reconnect.");
                Assert.That(GetCompletedCheckpoint(questOwner), Is.Null);
                Assert.That(GetDeferredDrainPending(questOwner), Is.False);
                Assert.That(GetDeferredRecords(questOwner), Is.Empty);
                Assert.That(GetDeferredByteCount(questOwner), Is.Zero);
                Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(appliedColor));
                Assert.That(matchingColorApplications, Is.EqualTo(1), "The interleaved canonical mutation is applied once after the checkpoint.");
                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(1UL));
            }
            finally
            {
                SiteState.ColorChanged -= countMutationApply;
                ((IDisposable)questOwner).Dispose();
                desktopPeer.Dispose();
                firstPair.Close();
                resumedPair?.Close();
                await AwaitGuardAsync(Task.WhenAll(firstDesktopRun, firstQuestRun));
                if (resumedDesktopRun != null && resumedQuestRun != null)
                    await AwaitGuardAsync(Task.WhenAll(resumedDesktopRun, resumedQuestRun));
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestSession_DisconnectDuringDeferredDrainResumesAtNextRecord()
        {
            using var fixture = new SessionSceneFixture(16);
            PreparedSceneDeliveryBinding binding = CreatePreparedBinding();
            var deferredRecordProcessed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            OperationId firstMutationId = new OperationId(GuidFor(55091));
            int pauseCount = 0;
            Func<V2TransportRecord, CancellationToken, Task> afterDeferredRecordProcessed = async (record, stop) =>
            {
                if (record.MessageId.Equals(firstMutationId) && Interlocked.Increment(ref pauseCount) == 1)
                {
                    deferredRecordProcessed.TrySetResult(true);
                    await Task.Delay(Timeout.Infinite, stop);
                }
            };
            object questOwner = CreateQuestSession(fixture.Scene, binding, null, afterDeferredRecordProcessed);
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 128);
            var desktopPeer = CreateTransport(V2OriginDevice.Desktop, 5090, limits);
            Color firstColor = new Color(0.23f, 0.51f, 0.76f, 1f);
            Color secondColor = new Color(0.76f, 0.41f, 0.19f, 1f);
            SiteId site = new SiteId(fixture.SiteIds[0]);
            var firstMutation = new SetSiteColor(new ColumnId(fixture.ColumnId), site, firstColor.r, firstColor.g, firstColor.b, firstColor.a);
            var secondMutation = new SetSiteColor(new ColumnId(fixture.ColumnId), site, secondColor.r, secondColor.g, secondColor.b, secondColor.a);
            OperationId checkpointId = new OperationId(GuidFor(55101));
            OperationId secondMutationId = new OperationId(GuidFor(55102));
            OperationId barrierId = new OperationId(GuidFor(55103));
            V2ScheduleDescriptor allScene = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, null, V2BarrierScope.AllScene);
            byte[] checkpoint;
            using (var sourceBoundary = new V2SceneMutationBoundary(fixture.Scene, V2OriginDevice.Desktop))
                checkpoint = V2SceneMutationCheckpointCodec.Encode(0, sourceBoundary.CaptureCheckpoint());

            Assert.That(checkpoint.Length, Is.GreaterThan(limits.InlineThresholdBytes));
            Assert.That(desktopPeer.EnqueueSceneOperation(checkpoint, allScene, structural: true, bodySchema: V2PublicationCheckpointBulkReceiver.BodySchema, operationId: checkpointId).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueMutation(firstMutation, 1UL, null, coalesciblePreview: false, operationId: firstMutationId).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueMutation(secondMutation, 2UL, null, coalesciblePreview: false, operationId: secondMutationId).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueSceneOperation(V2PublicationControlCodec.EncodeLiveBarrier(2), allScene, structural: true, operationId: barrierId).Accepted, Is.True);

            int firstApplications = 0;
            int secondApplications = 0;
            Action<SiteState> countMutationApply = state =>
            {
                if (!ReferenceEquals(state, fixture.Sites[0].State)) return;
                if (state.Color == firstColor) firstApplications++;
                if (state.Color == secondColor) secondApplications++;
            };
            SiteState.ColorChanged += countMutationApply;
            LoopbackPeerPair firstPair = await LoopbackPeerPair.ConnectAsync();
            Task<Exception> firstDesktopRun = CaptureRunAsync(desktopPeer, firstPair.Client.GetStream(), CancellationToken.None);
            Task<Exception> firstQuestRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, firstPair.Server.GetStream(), CancellationToken.None));
            LoopbackPeerPair resumedPair = null;
            Task<Exception> resumedDesktopRun = null;
            Task<Exception> resumedQuestRun = null;

            try
            {
                await AwaitGuardAsync(deferredRecordProcessed.Task);
                Assert.That(GetCompletedCheckpoint(questOwner), Is.Null, "The checkpoint must already be applied before the drain interruption.");
                Assert.That(GetDeferredDrainPending(questOwner), Is.True);
                List<V2TransportRecord> remaining = GetDeferredRecords(questOwner);
                Assert.That(remaining, Has.Count.EqualTo(2));
                Assert.That(remaining[0].MessageId, Is.EqualTo(secondMutationId));
                Assert.That(remaining[1].MessageId, Is.EqualTo(barrierId));
                Assert.That(firstApplications, Is.EqualTo(1));
                Assert.That(secondApplications, Is.Zero);

                firstPair.Close();
                await AwaitGuardAsync(Task.WhenAll(firstDesktopRun, firstQuestRun));
                Assert.That((bool)questOwner.GetType().GetProperty("CanResumeConnection").GetValue(questOwner), Is.True);
                Assert.That(GetDeferredDrainPending(questOwner), Is.True);
                Assert.That(GetDeferredRecords(questOwner), Has.Count.EqualTo(2));

                resumedPair = await LoopbackPeerPair.ConnectAsync();
                resumedDesktopRun = CaptureRunAsync(desktopPeer, resumedPair.Client.GetStream(), CancellationToken.None);
                resumedQuestRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, resumedPair.Server.GetStream(), CancellationToken.None));
                V2TransportRecord acknowledgement = await ReadIncomingAsync(desktopPeer);
                Assert.That(V2PublicationControlCodec.TryDecodeAcknowledgement(acknowledgement.GetPayloadCopy(), out OperationId acknowledgedBarrier), Is.True);
                Assert.That(acknowledgedBarrier, Is.EqualTo(barrierId));
                Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(secondColor));
                Assert.That(firstApplications, Is.EqualTo(1), "The already drained record must not be applied again.");
                Assert.That(secondApplications, Is.EqualTo(1), "The next ordered record must be applied exactly once after reconnect.");
                Assert.That(GetDeferredRecords(questOwner), Is.Empty);
                Assert.That(GetDeferredByteCount(questOwner), Is.Zero);
                Assert.That(GetDeferredDrainPending(questOwner), Is.False);
                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(2UL));
            }
            finally
            {
                SiteState.ColorChanged -= countMutationApply;
                ((IDisposable)questOwner).Dispose();
                desktopPeer.Dispose();
                firstPair.Close();
                resumedPair?.Close();
                await AwaitGuardAsync(Task.WhenAll(firstDesktopRun, firstQuestRun));
                if (resumedDesktopRun != null && resumedQuestRun != null)
                    await AwaitGuardAsync(Task.WhenAll(resumedDesktopRun, resumedQuestRun));
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task DesktopAndQuestSessions_ReconnectInitialBarrierBeforeReportingLive()
        {
            using var desktopFixture = new SessionSceneFixture(0);
            using var questFixture = new SessionSceneFixture(0);
            PreparedSceneDeliveryBinding binding = CreatePreparedBinding();
            object questOwner = CreateQuestSession(questFixture.Scene, binding);
            Type desktopOwnerType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(desktopOwnerType, Is.Not.Null);

            var firstDroppedBarrier = new TaskCompletionSource<DropMessageKindWriteStream>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondPairReady = new TaskCompletionSource<LoopbackPeerPair>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionTransports = new List<V2PersistentTransport>();
            var connectionPairs = new List<LoopbackPeerPair>();
            var questRuns = new List<Task<Exception>>();
            int openCount = 0;
            var questAckCount = new CountingSessionControlWriteStream();
            Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task> openReplica = async (host, pin, credential, stop, transport) =>
            {
                int attempt = Interlocked.Increment(ref openCount);
                LoopbackPeerPair pair = await LoopbackPeerPair.ConnectAsync();
                connectionTransports.Add(transport);
                connectionPairs.Add(pair);
                Stream desktopStream = pair.Client.GetStream();
                Stream questStream = pair.Server.GetStream();
                if (attempt == 1)
                {
                    var drop = new DropMessageKindWriteStream(desktopStream, V2TransportMessageKind.Application);
                    firstDroppedBarrier.TrySetResult(drop);
                    desktopStream = drop;
                }
                else if (attempt == 2)
                {
                    questStream = questAckCount.Wrap(questStream);
                    secondPairReady.TrySetResult(pair);
                }

                questRuns.Add(CaptureTaskExceptionAsync(RunQuestSession(questOwner, questStream, stop)));
                await transport.RunConnectionAsync(desktopStream, stop);
            };

            object desktopOwner = null;
            Task startPublication = null;
            try
            {
                Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
                ConstructorInfo constructor = desktopOwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
                Assert.That(constructor, Is.Not.Null, "The Desktop owner should expose its connector seam for session-level reconnect verification.");
                desktopOwner = constructor.Invoke(new object[] { desktopFixture.Scene, Session.Value.ToString(), Incarnation.Value.ToString(), openReplica });
                MethodInfo start = desktopOwnerType.GetMethod("StartAfterPublicationAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.That(start, Is.Not.Null);
                startPublication = (Task)start.Invoke(desktopOwner, new object[] { binding, "loopback", Array.Empty<byte>(), Array.Empty<byte>(), CancellationToken.None });

                DropMessageKindWriteStream dropped = await AwaitGuardValueAsync(firstDroppedBarrier.Task);
                await AwaitGuardAsync(dropped.Dropped.Task);
                Assert.That((bool)desktopOwnerType.GetProperty("IsLive").GetValue(desktopOwner), Is.False, "The production Desktop owner must wait for the barrier acknowledgement.");
                Assert.That(dropped.DroppedRecord.MessageId, Is.Not.Null);

                connectionPairs[0].Close();
                LoopbackPeerPair resumedPair = await AwaitGuardValueAsync(secondPairReady.Task);
                await AwaitGuardAsync(startPublication);
                Assert.That((bool)desktopOwnerType.GetProperty("IsLive").GetValue(desktopOwner), Is.True);
                Assert.That((bool)desktopOwnerType.GetProperty("IsClosed").GetValue(desktopOwner), Is.False);
                Assert.That(openCount, Is.EqualTo(2), "Reconnect should reuse the existing Desktop owner and transport.");
                Assert.That(connectionTransports, Has.Count.EqualTo(2));
                Assert.That(ReferenceEquals(connectionTransports[0], connectionTransports[1]), Is.True, "Both production connection attempts must use the same persistent Desktop transport.");
                Assert.That(questAckCount.SessionControlApplicationWrites, Is.EqualTo(1), "The retained Quest session must apply and acknowledge the replayed barrier once.");
                Assert.That((V2PersistentTransportState)questOwner.GetType().GetProperty("TransportState").GetValue(questOwner), Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That(resumedPair, Is.SameAs(connectionPairs[1]));

                var desktopTransport = (V2PersistentTransport)desktopOwnerType.GetField("m_Transport", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                Assert.That(desktopTransport.SnapshotMetrics().OutstandingReliableFrames, Is.Zero, "The initial barrier is acknowledged before the owner reports the scene live.");
            }
            finally
            {
                if (desktopOwner is IDisposable desktopDisposable) desktopDisposable.Dispose();
                ((IDisposable)questOwner).Dispose();
                foreach (LoopbackPeerPair pair in connectionPairs) pair.Close();
                if (startPublication != null && !startPublication.IsCompleted)
                {
                    try
                    {
                        await AwaitGuardAsync(startPublication);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (questRuns.Count > 0)
                    await AwaitGuardAsync(Task.WhenAll(questRuns));
            }
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

        private static async Task<T> AwaitGuardValueAsync<T>(Task<T> task)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
            if (completed != task)
                throw new TimeoutException("The loopback session did not reach its deterministic barrier.");
            return await task;
        }

        private static async Task WaitUntilAsync(Func<bool> condition, string timeoutMessage)
        {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (timeout.Elapsed >= TimeSpan.FromSeconds(5)) throw new TimeoutException(timeoutMessage);
                await Task.Delay(5);
            }
        }

        private static object CreateQuestSession(Base3DScene scene, PreparedSceneDeliveryBinding binding, Func<CancellationToken, Task> beforeCheckpointApply = null, Func<V2TransportRecord, CancellationToken, Task> afterDeferredRecordProcessed = null)
        {
            Type sessionType = FindLoadedType("HBP.Quest.QuestV2ReplicaSession");
            Assert.That(sessionType, Is.Not.Null, "Quest's production v2 session should be loaded for this integration test.");
            ConstructorInfo constructor = sessionType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(PreparedSceneDeliveryBinding), typeof(Func<CancellationToken, Task>), typeof(Func<V2TransportRecord, CancellationToken, Task>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { scene, binding, beforeCheckpointApply, afterDeferredRecordProcessed });
        }

        private static PreparedSceneDeliveryBinding CreatePreparedBinding()
        {
            var metadata = new JObject
            {
                ["TransferId"] = Incarnation.Value.ToString(),
                ["SessionId"] = "published-session",
                ["GlobalContextId"] = Session.Value.ToString(),
                ["Visualization"] = new JObject { ["ID"] = Scene.Value.ToString() },
                ["StandardFiles"] = new JObject(),
                ["Meshes"] = new JArray(),
                ["MRIs"] = new JArray(),
                ["Columns"] = new JArray()
            };
            PreparedSceneManifest manifest = PreparedSceneManifest.FromMetadata(metadata);
            ConstructorInfo constructor = typeof(PreparedSceneDeliveryBinding).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(PreparedSceneManifest) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (PreparedSceneDeliveryBinding)constructor.Invoke(new object[] { "loopback-manifest-hash", manifest });
        }

        private static Type FindLoadedType(string name)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, throwOnError: false);
                if (type != null) return type;
            }

            return null;
        }

        private static Task RunQuestSession(object owner, Stream stream, CancellationToken stop)
        {
            MethodInfo run = owner.GetType().GetMethod("RunConnectionAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(run, Is.Not.Null);
            return (Task)run.Invoke(owner, new object[] { stream, stop });
        }

        private static List<V2TransportRecord> GetDeferredRecords(object questOwner)
        {
            FieldInfo queueField = questOwner.GetType().GetField("m_DeferredRecords", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(queueField, Is.Not.Null, "The bounded deferred records must belong to the retained Quest session.");
            var records = new List<V2TransportRecord>();
            foreach (object deferred in (IEnumerable)queueField.GetValue(questOwner))
            {
                FieldInfo recordField = deferred.GetType().GetField("Record", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(recordField, Is.Not.Null);
                records.Add((V2TransportRecord)recordField.GetValue(deferred));
            }

            return records;
        }

        private static int GetDeferredByteCount(object questOwner)
        {
            FieldInfo bytes = questOwner.GetType().GetField("m_DeferredBytes", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bytes, Is.Not.Null);
            return (int)bytes.GetValue(questOwner);
        }

        private static bool GetCheckpointReceiverActive(object questOwner)
        {
            FieldInfo receiverField = questOwner.GetType().GetField("m_CheckpointBulkReceiver", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(receiverField, Is.Not.Null);
            return (bool)receiverField.FieldType.GetProperty("IsActive").GetValue(receiverField.GetValue(questOwner));
        }

        private static byte[] GetCompletedCheckpoint(object questOwner)
        {
            FieldInfo checkpoint = questOwner.GetType().GetField("m_CompletedCheckpoint", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(checkpoint, Is.Not.Null);
            return (byte[])checkpoint.GetValue(questOwner);
        }

        private static bool GetDeferredDrainPending(object questOwner)
        {
            FieldInfo pending = questOwner.GetType().GetField("m_DeferredDrainPending", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(pending, Is.Not.Null);
            return (bool)pending.GetValue(questOwner);
        }

        private static ulong GetDriverCanonicalWatermark(object questOwner)
        {
            FieldInfo driverField = questOwner.GetType().GetField("m_Driver", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(driverField, Is.Not.Null);
            return (ulong)driverField.FieldType.GetProperty("LastObservedCanonicalSequence").GetValue(driverField.GetValue(questOwner));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}.");
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

            Assert.That(backingField, Is.Not.Null, $"Missing backing field for {target.GetType().Name}.{propertyName}.");
            backingField.SetValue(target, value);
        }

        private static async Task<Exception> CaptureTaskExceptionAsync(Task task)
        {
            try
            {
                await task;
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
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

        private sealed class SessionSceneFixture : IDisposable
        {
            private const string FixturePatientId = "50000000-0000-0000-0000-000000000005";
            public GameObject Root { get; }
            public Base3DScene Scene { get; }
            public Column3DAnatomy Column { get; }
            public List<HBP.Core.Object3D.Site> Sites { get; } = new List<HBP.Core.Object3D.Site>();
            public List<string> SiteIds { get; } = new List<string>();
            public string ColumnId => "scene-focused-column";

            public SessionSceneFixture(int siteCount)
            {
                Root = new GameObject("v2 session loopback scene");
                Scene = Root.AddComponent<Base3DScene>();
                var mriManager = Root.AddComponent<MRIManager>();
                SetPrivateField(mriManager, "m_Scene", Scene);
                SetPrivateField(Scene, "m_MRIManager", mriManager);

                var patient = new Patient { ID = FixturePatientId, Name = "loopback-patient" };
                var columnData = new AnatomicColumn("loopback-column", new BaseConfiguration(), new AnatomicConfiguration(), ColumnId);
                SetAutoProperty(Scene, "Visualization", new Visualization("loopback-scene", new[] { patient }, new Column[] { columnData }, new VisualizationConfiguration(), V2PersistentTransportLoopbackTests.Scene.Value.ToString()));
                Column = Root.AddComponent<Column3DAnatomy>();
                SetAutoProperty(Column, "ColumnData", columnData);
                for (int index = 0; index < siteCount; index++)
                {
                    string rawName = "site-" + index.ToString("D3");
                    var siteObject = new GameObject(rawName);
                    siteObject.transform.SetParent(Root.transform, false);
                    HBP.Core.Object3D.Site site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
                    site.Information = new SiteInformation { Patient = patient, Name = rawName };
                    site.State = new SiteState();
                    Sites.Add(site);
                    SiteIds.Add(FixturePatientId + "_" + rawName);
                }

                SetAutoProperty(Column, "Sites", Sites);
                Scene.Columns.Add(Column);
            }

            public void Dispose()
            {
                if (Root) UnityEngine.Object.DestroyImmediate(Root);
            }
        }

        private sealed class DropBulkWriteStream : Stream
        {
            private readonly Stream m_Inner;
            public TaskCompletionSource<bool> Dropped { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public DropBulkWriteStream(Stream inner) => m_Inner = inner;
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
                if (count >= V2TransportFrameCodec.HeaderLength && ReadKind(buffer, offset) == V2TransportMessageKind.Application)
                {
                    var frame = new byte[count];
                    Buffer.BlockCopy(buffer, offset, frame, 0, count);
                    V2TransportRecord record = V2TransportFrameCodec.Decode(frame);
                    if (record.Lane == V2ScheduleLane.Bulk)
                    {
                        Dropped.TrySetResult(true);
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

        private sealed class CountingSessionControlWriteStream
        {
            private int m_SessionControlApplicationWrites;
            public int SessionControlApplicationWrites => Volatile.Read(ref m_SessionControlApplicationWrites);

            public Stream Wrap(Stream stream) => new CountingStream(this, stream);

            private void Observe(byte[] buffer, int offset, int count)
            {
                if (count < V2TransportFrameCodec.HeaderLength || ReadKind(buffer, offset) != V2TransportMessageKind.Application)
                    return;
                if (buffer[offset] != (byte)'H' || buffer[offset + 1] != (byte)'B' || buffer[offset + 2] != (byte)'T' || buffer[offset + 3] != (byte)'2')
                    return;
                var frame = new byte[count];
                Buffer.BlockCopy(buffer, offset, frame, 0, count);
                V2TransportRecord record = V2TransportFrameCodec.Decode(frame);
                if (record.Lane == V2ScheduleLane.SessionControl)
                    Interlocked.Increment(ref m_SessionControlApplicationWrites);
            }

            private sealed class CountingStream : Stream
            {
                private readonly CountingSessionControlWriteStream m_Owner;
                private readonly Stream m_Inner;

                public CountingStream(CountingSessionControlWriteStream owner, Stream inner)
                {
                    m_Owner = owner;
                    m_Inner = inner;
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

                public override void Write(byte[] buffer, int offset, int count)
                {
                    m_Owner.Observe(buffer, offset, count);
                    m_Inner.Write(buffer, offset, count);
                }

                public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    m_Owner.Observe(buffer, offset, count);
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
