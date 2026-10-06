using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

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
        [Category("Sync.SceneFocused")]
        public async Task QuestApplicationCompletion_MatchingEchoCompletesEvenWhenItsCallbackCancelsTheConnection()
        {
            using var fixture = new SessionSceneFixture(1);
            object owner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            using var stop = new CancellationTokenSource();
            try
            {
                var driver = GetPrivateField<V2QuestMutationDriver>(owner, "m_Driver");
                var operation = new OperationId(GuidFor(201001));
                var mutation = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), 0.25f, 0.5f, 0.75f, 1);
                driver.ApplyOptimistic(mutation, operation);
                driver.ProposalConfirmed += _ => stop.Cancel();
                var record = new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, operation, new ReliableStreamId(GuidFor(201002)), 1, 1, V2OriginDevice.Desktop, V2ScheduleLane.Interactive, payload: V2MutationPayloadCodec.Encode(mutation), canonicalSequence: 1, mutation: mutation);
                await (Task)InvokePrivateMethod(owner, "ProcessRecordAsync", record, default(SyncTelemetryPoint), default(SyncTelemetryPoint), stop.Token);
                Assert.That(stop.IsCancellationRequested, Is.True);
                Assert.That(driver.PendingProposalCount, Is.Zero);
                Assert.That(GetPrivateField<V2ApplicationCompletionWatermark>(owner, "m_ApplicationCompletion").CompletedThrough, Is.EqualTo(1));
            }
            finally
            {
                ((IDisposable)owner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestPreviewLookahead_ReceivedFollowingRecordSurvivesConnectionCancellation()
        {
            using var fixture = new SessionSceneFixture(1);
            fixture.AddTimeline("preview-timeline");
            object owner = CreateQuestSession(fixture.Scene, CreatePreparedBinding(fixture.ColumnId, "preview-timeline"));
            var desktop = CreateTransport(V2OriginDevice.Desktop, 201010);
            using var firstPair = await LoopbackPeerPair.ConnectAsync();
            using var firstStop = new CancellationTokenSource();
            using var resumedStop = new CancellationTokenSource();
            Task<Exception> questRun = null, desktopRun = null, receiveRun = null, resumedQuest = null, resumedDesktop = null;
            try
            {
                var driver = GetPrivateField<V2QuestMutationDriver>(owner, "m_Driver");
                var operation = new OperationId(GuidFor(301011));
                var preview = new SetTimelineAnchor(new ColumnId("preview-timeline"), 3, false, false, 1, 0, 1000, V2TimelineAnchorIntent.Seek);
                driver.ApplyOptimistic(preview, operation);
                V2TransportRecord retainedAtCancellation = null;
                driver.ProposalConfirmed += id =>
                {
                    if (!id.Equals(operation)) return;
                    retainedAtCancellation = GetPrivateField<V2TransportRecord>(owner, "m_ReadAheadRecord");
                    firstStop.Cancel();
                };
                var following = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), 0.8f, 0.2f, 0.4f, 1);
                Assert.That(desktop.EnqueueMutation(preview, 1, null, operationId: operation).Accepted, Is.True);
                Assert.That(desktop.EnqueueMutation(following, 2, null, operationId: new OperationId(GuidFor(301012))).Accepted, Is.True);
                // Receive and ACK both records before starting application, so the preview drain reads ahead.
                var questTransport = GetPrivateField<V2PersistentTransport>(owner, "m_Transport");
                desktopRun = CaptureRunAsync(desktop, firstPair.Client.GetStream(), firstStop.Token);
                receiveRun = CaptureRunAsync(questTransport, firstPair.Server.GetStream(), firstStop.Token);
                try
                {
                    await WaitUntilAsync(() => GetPrivateField<ulong>(questTransport, "m_LastOriginSequence") == 2, "Quest did not receive both records.");
                }
                catch (TimeoutException)
                {
                    Assert.Fail($"Quest origin={GetPrivateField<ulong>(questTransport, "m_LastOriginSequence")} fault={GetPrivateField<Exception>(questTransport, "m_FaultException")} Desktop fault={GetPrivateField<Exception>(desktop, "m_FaultException")}");
                }

                questRun = CaptureTaskExceptionAsync((Task)InvokePrivateMethod(owner, "ProcessIncomingAsync", firstStop.Token));
                await AwaitGuardAsync(questRun);
                await AwaitGuardAsync(receiveRun);
                await AwaitGuardAsync(desktopRun);
                Assert.That(retainedAtCancellation, Is.Not.Null);
                Assert.That(GetPrivateField<V2TransportRecord>(owner, "m_ReadAheadRecord"), Is.SameAs(retainedAtCancellation));

                using var resumedPair = await LoopbackPeerPair.ConnectAsync();
                resumedQuest = CaptureTaskExceptionAsync(RunQuestSession(owner, resumedPair.Server.GetStream(), resumedStop.Token));
                resumedDesktop = CaptureRunAsync(desktop, resumedPair.Client.GetStream(), resumedStop.Token);
                await WaitUntilAsync(() => GetPrivateField<V2ApplicationCompletionWatermark>(owner, "m_ApplicationCompletion").CompletedThrough == 2, "The ACKed lookahead record was lost across connection restart.");
                Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(new Color(0.8f, 0.2f, 0.4f, 1)));
                Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
                resumedStop.Cancel();
                resumedPair.Close();
                await AwaitGuardAsync(resumedQuest);
                await AwaitGuardAsync(resumedDesktop);
            }
            finally
            {
                firstStop.Cancel();
                resumedStop.Cancel();
                firstPair.Close();
                ((IDisposable)owner).Dispose();
                desktop.Dispose();
                foreach (Task task in new Task[] { questRun, desktopRun, receiveRun, resumedQuest, resumedDesktop })
                    if (task != null)
                        await AwaitGuardAsync(task);
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task ApplicationRetention_ProductionSessionsConvergeBeyond4096InBothDirections()
        {
            using var desktopFixture = new SessionSceneFixture(2);
            using var questFixture = new SessionSceneFixture(2);
            LiveActivityProjectionSessionPair sessions = await LiveActivityProjectionSessionPair.ConnectAsync(desktopFixture.Scene, questFixture.Scene, CreatePreparedBinding());
            var authority = GetPrivateField<V2DesktopMutationAuthority>(sessions.DesktopOwner, "m_Authority");
            var desktopBoundary = GetPrivateField<V2SceneMutationBoundary>(sessions.DesktopOwner, "m_Boundary");
            var driver = GetPrivateField<V2QuestMutationDriver>(sessions.QuestOwner, "m_Driver");
            // EditMode does not advance the render PlayerLoop reliably. Supply a
            // test frame clock while retaining production preview pacing and sessions.
            using var desktopFrames = new SemaphoreSlim(0);
            using var questFrames = new SemaphoreSlim(0);
            SetPrivateField(sessions.DesktopTransport, "m_PreviewFrameWaiter", new Func<CancellationToken, Task>(stop => desktopFrames.WaitAsync(stop)));
            SetPrivateField(GetPrivateField<V2PersistentTransport>(sessions.QuestOwner, "m_Transport"), "m_PreviewFrameWaiter", new Func<CancellationToken, Task>(stop => questFrames.WaitAsync(stop)));
            try
            {
                for (int batch = 0; batch < 32; batch++)
                {
                    float red = (batch + 1) / 33f;
                    for (int site = 0; site < 256; site++)
                    {
                        var desktopMutation = new SetSiteColor(new ColumnId(desktopFixture.ColumnId), new SiteId(desktopFixture.SiteIds[0]), red, site / 257f, 0.3f, 1);
                        desktopBoundary.Apply(desktopMutation, V2MutationApplicationOrigin.LocalDesktop, new OperationId(GuidFor(200000 + batch * 512 + site)));
                        var questMutation = new SetSiteColor(new ColumnId(questFixture.ColumnId), new SiteId(questFixture.SiteIds[1]), red, 0.3f, site / 257f, 1);
                        driver.ApplyOptimistic(questMutation, new OperationId(GuidFor(200000 + batch * 512 + site + 256)));
                    }

                    desktopFrames.Release();
                    questFrames.Release();
                    try
                    {
                        await WaitForApplicationProgressAsync(() =>
                        {
                            if (desktopFrames.CurrentCount == 0) desktopFrames.Release();
                            if (questFrames.CurrentCount == 0) questFrames.Release();
                            return driver.PendingProposalCount == 0 && questFixture.Sites[0].State.Color.r == red && desktopFixture.Sites[1].State.Color.r == red;
                        }, "Production peers stopped converging during sustained synchronization.", timeoutSeconds: 15);
                    }
                    catch (TimeoutException)
                    {
                        string status = $"batch={batch} canonical={authority.CanonicalSequence} observed={driver.LastObservedCanonicalSequence} pending={driver.PendingProposalCount} decisions={authority.RetainedDecisionCount} keys={authority.IndexedKeyCount}";
                        await sessions.CloseAsync();
                        Exception failure = await GetPrivateField<Task<Exception>>(sessions, "m_QuestRun");
                        Assert.Fail(status + " Quest session failure=" + failure);
                    }
                }

                await WaitForApplicationProgressAsync(() => authority.RetainedDecisionCount == 0 && authority.IndexedKeyCount == 0 && driver.ReceivedOperationCount == 0, "Completed histories were not released by application progress.");
                Assert.That(authority.CanonicalSequence, Is.InRange(8224UL, 16384UL));
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
                Assert.That(GetPrivateField<V2SceneMutationBoundary>(sessions.QuestOwner, "m_Boundary").OptimisticRollbackOrderCount, Is.Zero);
                for (int site = 0; site < 2; site++) Assert.That(questFixture.Sites[site].State.Color, Is.EqualTo(desktopFixture.Sites[site].State.Color));
            }
            catch (Exception exception)
            {
                var questTransport = GetPrivateField<V2PersistentTransport>(sessions.QuestOwner, "m_Transport");
                throw new AssertionException($"{exception} desktopFault={GetPrivateField<Exception>(sessions.DesktopTransport, "m_FaultException")} questFault={GetPrivateField<Exception>(questTransport, "m_FaultException")}");
            }
            finally
            {
                await sessions.CloseAsync();
            }

            async Task WaitForApplicationProgressAsync(Func<bool> condition, string message, int timeoutSeconds = 15)
            {
                var elapsed = Stopwatch.StartNew();
                while (!condition())
                {
                    if (elapsed.Elapsed.TotalSeconds >= timeoutSeconds) throw new TimeoutException(message);
                    await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Initialization);
                }
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void ApplicationRetention_SameIdPreviewRepublishCannotRetireItsUnsentCanonical()
        {
            using var fixture = new SessionSceneFixture(1);
            object owner = CreateDesktopSession(fixture.Scene);
            try
            {
                var authority = GetPrivateField<V2DesktopMutationAuthority>(owner, "m_Authority");
                var boundary = GetPrivateField<V2SceneMutationBoundary>(owner, "m_Boundary");
                var scheduler = GetPrivateField<V2OutgoingScheduler>(owner, "m_Scheduler");
                var id = new OperationId(GuidFor(999001));
                var mutation = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), 0.2f, 0.3f, 0.4f, 1);
                boundary.Apply(mutation, V2MutationApplicationOrigin.LocalDesktop, id);
                Assert.That(authority.AcceptQuestProposal(id, mutation, 0).Outcome, Is.EqualTo(V2ProposalOutcome.Duplicate));
                Assert.That(authority.RetainedDecisionCount, Is.EqualTo(1));
                Assert.That(authority.RetiredCanonicalThrough, Is.Zero);
                Assert.That(scheduler.TryGetNextTransmission(out V2TransmissionAttempt sent), Is.True);
                Assert.That(sent.Frame.OperationId, Is.EqualTo(id));
                Assert.That(sent.Frame.CanonicalSequence, Is.EqualTo(1UL));
            }
            finally
            {
                ((IDisposable)owner).Dispose();
            }
        }

        [Test]
        public void RetentionProgress_ReceiptBurstsRetainOnlyLatestNotificationAndValidateSkippedFrames()
        {
            using var transport = CreateTransport(V2OriginDevice.Desktop, 980000);
            // This fixture isolates notification retention after the scene sender
            // has assigned the origin range covered by these confirmations.
            SetPrivateField(GetPrivateField<V2OutgoingScheduler>(transport, "m_Scheduler"), "m_NextOriginSequence", 10003UL);
            MethodInfo enqueue = typeof(V2PersistentTransport).GetMethod("EnqueueIncoming", BindingFlags.Instance | BindingFlags.NonPublic);
            for (ulong i = 1; i <= 10000; i++) enqueue.Invoke(transport, new object[] { Progress(i) });
            Assert.That(GetPrivateField<ICollection>(transport, "m_Incoming").Count, Is.EqualTo(1));
            Assert.That(transport.TryReadIncoming(out V2TransportRecord latest), Is.True);
            Assert.That(V2RetentionProgress.TryDecode(latest.GetPayloadCopy(), out V2RetentionProgress decoded), Is.True);
            Assert.That(decoded.AppliedOriginThrough, Is.EqualTo(10000UL));
            Assert.That(transport.TryReadIncoming(out _), Is.False);
            var invalid = Assert.Throws<TargetInvocationException>(() => enqueue.Invoke(transport, new object[] { Progress(9999) }));
            Assert.That(invalid.InnerException, Is.TypeOf<InvalidDataException>());
            enqueue.Invoke(transport, new object[] { Progress(10001) });
            Assert.That(transport.TryReadIncoming(out _), Is.True);
            Assert.That(Assert.Throws<TargetInvocationException>(() => enqueue.Invoke(transport, new object[] { Progress(10003) })).InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(Assert.Throws<TargetInvocationException>(() => enqueue.Invoke(transport, new object[] { Progress(0) })).InnerException, Is.TypeOf<InvalidDataException>());
            for (int i = 0; i < 256; i++) enqueue.Invoke(transport, new object[] { IncomingMutation(new ReliableStreamId(GuidFor(980003)), i + 1) });
            Assert.That(Assert.Throws<TargetInvocationException>(() => enqueue.Invoke(transport, new object[] { Progress(10002) })).InnerException, Is.TypeOf<V2TransportBackpressureException>());
            Assert.That(transport.TryReadIncoming(out _), Is.True);
            enqueue.Invoke(transport, new object[] { Progress(10002) });
            V2TransportRecord last = null;
            while (transport.TryReadIncoming(out V2TransportRecord next)) last = next;
            Assert.That(V2RetentionProgress.TryDecode(last.GetPayloadCopy(), out decoded), Is.True);
            Assert.That(decoded.AppliedOriginThrough, Is.EqualTo(10002UL), "A failed queue admission must not consume the progress notification or its retry identity.");

            V2TransportRecord Progress(ulong value) => new V2TransportRecord(V2TransportMessageKind.Application, Session, messageId: new OperationId(GuidFor(980001)), streamId: new ReliableStreamId(GuidFor(980002)), reliableFrameSequence: value, originDevice: V2OriginDevice.Quest, lane: V2ScheduleLane.SessionControl, payload: new V2RetentionProgress(Session, Scene, Incarnation, V2OriginDevice.Quest, value, value, 0, value).Encode());
        }

        [Test]
        public async Task PreviewFrameWait_DoesNotBlockControlTrafficAndFlushesFinalValue()
        {
            var frame = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waiting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Desktop);
            using var desktop = new V2PersistentTransport(scheduler, previewFrameWaiter: _ =>
            {
                waiting.TrySetResult(true);
                return frame.Task;
            });
            using var quest = CreateTransport(V2OriginDevice.Quest, 91000);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var stop = new CancellationTokenSource();
            Task<Exception> desktopRun = CaptureRunAsync(desktop, pair.Client.GetStream(), stop.Token);
            Task<Exception> questRun = CaptureRunAsync(quest, pair.Server.GetStream(), stop.Token);
            try
            {
                desktop.EnqueueMutation(Color("preview-site"), 1, null, true);
                await ReadIncomingAsync(quest);
                desktop.EnqueueMutation(new SetSiteColor(new ColumnId("column"), new SiteId("preview-site"), 0.8f, 0, 0, 1), 2, null, true);
                await AwaitGuardAsync(waiting.Task);
                desktop.EnqueueMutation(new SetSiteColor(new ColumnId("column"), new SiteId("preview-site"), 0.9f, 0, 0, 1), 3, null, true);
                desktop.EnqueueSessionControl(new byte[] { 0x42 }, V2DeliveryReliability.Reliable);
                V2TransportRecord control = await ReadIncomingAsync(quest);
                Assert.That(control.Lane, Is.EqualTo(V2ScheduleLane.SessionControl));
                Assert.That(frame.Task.IsCompleted, Is.False, "Network controls must advance with the PlayerLoop blocked.");
                frame.TrySetResult(true);
                V2TransportRecord final = await ReadIncomingAsync(quest);
                Assert.That(((SetSiteColor)final.Mutation).Red, Is.EqualTo(0.9f));
                Assert.That(final.CanonicalSequence, Is.EqualTo(3UL));
            }
            finally
            {
                frame.TrySetResult(true);
                stop.Cancel();
                desktop.Dispose();
                quest.Dispose();
                pair.Close();
                await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun));
            }
        }

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

        [Category("Sync.SceneFocused")]
        [TestCase(false)]
        [TestCase(true)]
        public async Task ReadyActivityProjectionRemoval_ConvergesFromEitherPeerWithoutClosingTransport(bool removeFromQuest)
        {
            using var desktopFixture = new SessionSceneFixture(0);
            using var questFixture = new SessionSceneFixture(0);
            desktopFixture.InitializeProjectionMaterials();
            questFixture.InitializeProjectionMaterials();
            PrepareReadyActivityProjection(desktopFixture.Scene);
            PrepareReadyActivityProjection(questFixture.Scene);

            LiveActivityProjectionSessionPair sessions = await LiveActivityProjectionSessionPair.ConnectAsync(desktopFixture.Scene, questFixture.Scene, CreatePreparedBinding());
            var desktopRemovalObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var questRemovalObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            UnityAction<bool> desktopListener = requested =>
            {
                if (!requested) desktopRemovalObserved.TrySetResult(true);
            };
            UnityAction<bool> questListener = requested =>
            {
                if (!requested) questRemovalObserved.TrySetResult(true);
            };
            desktopFixture.Scene.OnProjectionRequestedChanged.AddListener(desktopListener);
            questFixture.Scene.OnProjectionRequestedChanged.AddListener(questListener);

            try
            {
                if (removeFromQuest)
                {
                    questFixture.Scene.SetProjectionEnabled(false);
                    await AwaitGuardAsync(desktopRemovalObserved.Task);
                }
                else
                {
                    desktopFixture.Scene.SetProjectionEnabled(false);
                    await AwaitGuardAsync(questRemovalObserved.Task);
                }

                await WaitUntilAsync(() => !desktopFixture.Scene.ProjectionRequested && !questFixture.Scene.ProjectionRequested, "The ready activity-projection removal did not converge between the live sessions.");

                Assert.That(desktopFixture.Scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Absent));
                Assert.That(questFixture.Scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Absent));
                Assert.That(desktopFixture.Scene.IsGeneratorUpToDate, Is.False);
                Assert.That(questFixture.Scene.IsGeneratorUpToDate, Is.False);
                Assert.That(desktopFixture.Scene.BrainMaterials.BrainMaterial.GetInt("_Activity"), Is.Zero, "Desktop must remove the displayed activity projection.");
                Assert.That(questFixture.Scene.BrainMaterials.BrainMaterial.GetInt("_Activity"), Is.Zero, "Quest must remove the displayed activity projection.");
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That((V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner), Is.EqualTo(V2PersistentTransportState.Connected));
            }
            finally
            {
                desktopFixture.Scene.OnProjectionRequestedChanged.RemoveListener(desktopListener);
                questFixture.Scene.OnProjectionRequestedChanged.RemoveListener(questListener);
                await sessions.CloseAsync();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task ActivityProjectionRemovalDuringComputation_RetainsBusyScopeUntilBothTerminals()
        {
            using var desktopFixture = new SessionSceneFixture(0);
            using var questFixture = new SessionSceneFixture(0);
            desktopFixture.InitializeProjectionMaterials();
            questFixture.InitializeProjectionMaterials();
            PrepareReadyActivityProjection(desktopFixture.Scene);
            PrepareReadyActivityProjection(questFixture.Scene);

            LiveActivityProjectionSessionPair sessions = await LiveActivityProjectionSessionPair.ConnectAsync(desktopFixture.Scene, questFixture.Scene, CreatePreparedBinding());
            var desktopBusyReleased = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var questBusyReleased = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            UnityAction<bool> desktopBusyListener = busy =>
            {
                if (!busy) desktopBusyReleased.TrySetResult(true);
            };
            UnityAction<bool> questBusyListener = busy =>
            {
                if (!busy) questBusyReleased.TrySetResult(true);
            };
            desktopFixture.Scene.OnActivityProjectionBusyChanged.AddListener(desktopBusyListener);
            questFixture.Scene.OnActivityProjectionBusyChanged.AddListener(questBusyListener);

            try
            {
                OperationId jobId = new OperationId(Guid.Parse("65000000-0000-0000-0000-000000000014"));
                object desktopActive = InstallSyntheticActiveActivityProjectionJob(sessions.DesktopOwner, desktopFixture.Scene, jobId, 101);
                object questActive = InstallSyntheticActiveActivityProjectionJob(sessions.QuestOwner, questFixture.Scene, jobId, 202);
                questFixture.Scene.SetProjectionEnabled(false);

                await WaitUntilAsync(() => !desktopFixture.Scene.ProjectionRequested && !questFixture.Scene.ProjectionRequested, "The in-flight removal did not reach both peers.");
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True);
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.True);

                // Quest finishes first. Its local terminal must not release either peer while Desktop is still computing.
                SetPrivateField(questFixture.Scene, "m_UpdatingGenerators", false);
                questFixture.Scene.OnActivityProjectionCompleted.Invoke(202, ActivityProjectionCompletionKind.Cancelled);
                await WaitUntilAsync(() => GetTerminalFlag(desktopActive, "RemoteTerminal"), "Desktop did not receive Quest's first terminal.");
                Assert.That(GetTerminalFlag(questActive, "LocalTerminal"), Is.True);
                Assert.That(GetTerminalFlag(questActive, "RemoteTerminal"), Is.False);
                Assert.That(GetTerminalFlag(desktopActive, "LocalTerminal"), Is.False);
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True, "Desktop's local computation still owns the busy scope.");
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.True, "Quest must retain its scope until Desktop's terminal arrives.");

                SetPrivateField(desktopFixture.Scene, "m_UpdatingGenerators", false);
                desktopFixture.Scene.OnActivityProjectionCompleted.Invoke(101, ActivityProjectionCompletionKind.Cancelled);
                await AwaitGuardAsync(Task.WhenAll(desktopBusyReleased.Task, questBusyReleased.Task));
                await WaitUntilAsync(() => GetSessionActivityProjectionJob(sessions.DesktopOwner) == null && GetSessionActivityProjectionJob(sessions.QuestOwner) == null, "The paired cancellation terminals did not release both activity-projection jobs.");

                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.False);
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.False);
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That((V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner), Is.EqualTo(V2PersistentTransportState.Connected));
            }
            finally
            {
                desktopFixture.Scene.OnActivityProjectionBusyChanged.RemoveListener(desktopBusyListener);
                questFixture.Scene.OnActivityProjectionBusyChanged.RemoveListener(questBusyListener);
                await sessions.CloseAsync();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task ActivityProjectionBusyScope_QuestDriverAllowsSafeMutationAndCorrectsSensitiveMutationUntilBothTerminals()
        {
            using var desktopFixture = new SessionSceneFixture(1);
            using var questFixture = new SessionSceneFixture(1);
            desktopFixture.InitializeProjectionMaterials();
            questFixture.InitializeProjectionMaterials();
            PrepareReadyActivityProjection(desktopFixture.Scene);
            PrepareReadyActivityProjection(questFixture.Scene);

            LiveActivityProjectionSessionPair sessions = await LiveActivityProjectionSessionPair.ConnectAsync(desktopFixture.Scene, questFixture.Scene, CreatePreparedBinding());
            V2QuestMutationDriver questDriver = GetPrivateField<V2QuestMutationDriver>(sessions.QuestOwner, "m_Driver");
            var safeConfirmed = new TaskCompletionSource<OperationId>(TaskCreationOptions.RunContinuationsAsynchronously);
            var sensitiveCorrected = new TaskCompletionSource<(OperationId OperationId, V2Mutation Mutation)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var sensitiveConfirmed = new TaskCompletionSource<OperationId>(TaskCreationOptions.RunContinuationsAsynchronously);
            OperationId safeOperation = new OperationId(GuidFor(55414));
            OperationId blockedOperation = new OperationId(GuidFor(55415));
            OperationId acceptedOperation = new OperationId(GuidFor(55416));
            Action<OperationId> confirmationListener = operationId =>
            {
                if (operationId.Equals(safeOperation)) safeConfirmed.TrySetResult(operationId);
                if (operationId.Equals(acceptedOperation)) sensitiveConfirmed.TrySetResult(operationId);
            };
            Action<OperationId, V2Mutation> correctionListener = (operationId, mutation) =>
            {
                if (operationId.Equals(blockedOperation)) sensitiveCorrected.TrySetResult((operationId, mutation));
            };
            questDriver.ProposalConfirmed += confirmationListener;
            questDriver.AuthoritativeCorrectionApplied += correctionListener;

            try
            {
                OperationId jobId = new OperationId(GuidFor(55417));
                object desktopActive = InstallSyntheticActiveActivityProjectionJob(sessions.DesktopOwner, desktopFixture.Scene, jobId, 505);
                object questActive = InstallSyntheticActiveActivityProjectionJob(sessions.QuestOwner, questFixture.Scene, jobId, 606);
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True);
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.True);

                var expectedColor = new Color(0.18f, 0.54f, 0.82f, 1f);
                var safeMutation = new SetSiteColor(new ColumnId(questFixture.ColumnId), new SiteId(questFixture.SiteIds[0]), expectedColor.r, expectedColor.g, expectedColor.b, expectedColor.a);
                Assert.That(V2ActivityProjectionAdmission.RequiresSensitiveAdmission(safeMutation), Is.False);
                V2QuestMutationProposal safeProposal = questDriver.ApplyOptimistic(safeMutation, safeOperation);
                Assert.That(safeProposal, Is.Not.Null, "The safe Quest mutation should be queued through the live session driver.");
                await AwaitGuardAsync(safeConfirmed.Task);
                await WaitUntilAsync(() => desktopFixture.Sites[0].State.Color == expectedColor && questFixture.Sites[0].State.Color == expectedColor, "The safe mutation did not apply on both peers while the paired generation was busy.");

                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True);
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.True);
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That((V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner), Is.EqualTo(V2PersistentTransportState.Connected));

                var sensitiveMutation = new SetSiteBlacklist(new ColumnId(questFixture.ColumnId), new SiteId(questFixture.SiteIds[0]), true);
                Assert.That(V2ActivityProjectionAdmission.RequiresSensitiveAdmission(sensitiveMutation), Is.True);
                V2QuestMutationProposal blockedProposal = questDriver.ApplyOptimistic(sensitiveMutation, blockedOperation);
                Assert.That(blockedProposal, Is.Not.Null, "The sensitive mutation should be optimistically submitted through the Quest driver.");
                (OperationId OperationId, V2Mutation Mutation) correction = await AwaitGuardValueAsync(sensitiveCorrected.Task);

                Assert.That(correction.OperationId, Is.EqualTo(blockedOperation));
                Assert.That(correction.Mutation, Is.TypeOf<SetSiteBlacklist>());
                Assert.That(((SetSiteBlacklist)correction.Mutation).Blacklisted, Is.False, "Quest should apply Desktop's authoritative blacklist correction.");
                Assert.That(desktopFixture.Sites[0].State.IsBlackListed, Is.False);
                Assert.That(questFixture.Sites[0].State.IsBlackListed, Is.False);
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That((V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner), Is.EqualTo(V2PersistentTransportState.Connected));

                V2DesktopMutationAuthority authority = GetPrivateField<V2DesktopMutationAuthority>(sessions.DesktopOwner, "m_Authority");
                V2DesktopProposalResult blockedDecision = authority.AcceptQuestProposal(blockedProposal);
                Assert.That(blockedDecision.Outcome, Is.EqualTo(V2ProposalOutcome.Duplicate));
                Assert.That(blockedDecision.RejectionCode, Is.EqualTo("activity_projection_busy"));
                Assert.That(blockedDecision.Correction.RejectionCode, Is.EqualTo("activity_projection_busy"));
                Assert.That(questDriver.PendingProposalCount, Is.Zero);
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True, "Sensitive rejection must not release Desktop's active projection scope.");
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.True, "Sensitive rejection must not release Quest's active projection scope.");

                questFixture.Scene.SetProjectionEnabled(false);
                await WaitUntilAsync(() => !desktopFixture.Scene.ProjectionRequested && !questFixture.Scene.ProjectionRequested, "The paired generation cancellation did not reach both peers.");
                SetPrivateField(questFixture.Scene, "m_UpdatingGenerators", false);
                questFixture.Scene.OnActivityProjectionCompleted.Invoke(606, ActivityProjectionCompletionKind.Cancelled);
                await WaitUntilAsync(() => GetTerminalFlag(desktopActive, "RemoteTerminal"), "Desktop did not receive Quest's first terminal.");
                Assert.That(GetTerminalFlag(questActive, "LocalTerminal"), Is.True);
                Assert.That(GetTerminalFlag(questActive, "RemoteTerminal"), Is.False);
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True, "Desktop must retain its scope until its local worker is terminal.");
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.True, "Quest must retain its scope until Desktop's terminal arrives.");

                SetPrivateField(desktopFixture.Scene, "m_UpdatingGenerators", false);
                desktopFixture.Scene.OnActivityProjectionCompleted.Invoke(505, ActivityProjectionCompletionKind.Cancelled);
                await WaitUntilAsync(() => GetSessionActivityProjectionJob(sessions.DesktopOwner) == null && GetSessionActivityProjectionJob(sessions.QuestOwner) == null, "Both activity-projection terminals did not release the paired jobs.");
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.False);
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.False);

                Assert.That(questDriver.ApplyOptimistic(sensitiveMutation, acceptedOperation), Is.Not.Null, "The sensitive mutation should be accepted after the paired scope releases.");
                await AwaitGuardAsync(sensitiveConfirmed.Task);
                await WaitUntilAsync(() => desktopFixture.Sites[0].State.IsBlackListed && questFixture.Sites[0].State.IsBlackListed, "The released sensitive mutation did not apply on both peers.");
                Assert.That(questDriver.PendingProposalCount, Is.Zero);
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That((V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner), Is.EqualTo(V2PersistentTransportState.Connected));
            }
            finally
            {
                questDriver.ProposalConfirmed -= confirmationListener;
                questDriver.AuthoritativeCorrectionApplied -= correctionListener;
                await sessions.CloseAsync();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task ActivityProjectionDesktopConnectionLoss_QuestOfflineAndLocalTerminalsGateScopeRelease()
        {
            using var desktopFixture = new SessionSceneFixture(0);
            using var questFixture = new SessionSceneFixture(0);
            desktopFixture.InitializeProjectionMaterials();
            questFixture.InitializeProjectionMaterials();
            PrepareReadyActivityProjection(desktopFixture.Scene);
            PrepareReadyActivityProjection(questFixture.Scene);

            LiveActivityProjectionSessionPair sessions = await LiveActivityProjectionSessionPair.ConnectAsync(desktopFixture.Scene, questFixture.Scene, CreatePreparedBinding());
            try
            {
                OperationId jobId = new OperationId(Guid.Parse("65000000-0000-0000-0000-000000000015"));
                object desktopActive = InstallSyntheticActiveActivityProjectionJob(sessions.DesktopOwner, desktopFixture.Scene, jobId, 303);
                object questActive = InstallSyntheticActiveActivityProjectionJob(sessions.QuestOwner, questFixture.Scene, jobId, 404);
                object desktopLease = GetPrivateField<object>(desktopFixture.Scene, "m_ActiveActivityProjection");
                object questLease = GetPrivateField<object>(questFixture.Scene, "m_ActiveActivityProjection");
                Assert.That(IsCurrentActivityProjection(desktopFixture.Scene, desktopLease), Is.True);
                Assert.That(IsCurrentActivityProjection(questFixture.Scene, questLease), Is.True);

                ((IDisposable)sessions.DesktopOwner).Dispose();
                await WaitUntilAsync(() => (V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner) == V2PersistentTransportState.DisconnectedGrace, "Quest did not observe Desktop's lost connection.");
                ForceQuestOfflineTransition(sessions.QuestOwner);
                await WaitUntilAsync(() => GetTerminalFlag(desktopActive, "RemoteTerminal") && GetTerminalFlag(questActive, "RemoteTerminal"), "Both sessions did not cancel the disconnected projection.");

                Assert.That(IsCurrentActivityProjection(desktopFixture.Scene, desktopLease), Is.False, "Desktop must invalidate the cancelled worker's publication lease immediately.");
                Assert.That(IsCurrentActivityProjection(questFixture.Scene, questLease), Is.False, "Quest must invalidate the cancelled worker's publication lease immediately.");
                Assert.That(GetTerminalFlag(desktopActive, "LocalTerminal"), Is.False);
                Assert.That(GetTerminalFlag(questActive, "LocalTerminal"), Is.False);
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True, "Desktop must retain its scope while its local worker is running.");
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.True, "Quest must retain its scope while its local worker is running.");
                Assert.That(desktopFixture.Scene.IsGeneratorUpToDate, Is.False);
                Assert.That(questFixture.Scene.IsGeneratorUpToDate, Is.False);
                Assert.That(desktopFixture.Scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Stale));
                Assert.That(questFixture.Scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Stale));
                Assert.That(desktopFixture.Scene.BrainMaterials.BrainMaterial.GetInt("_Activity"), Is.Zero);
                Assert.That(questFixture.Scene.BrainMaterials.BrainMaterial.GetInt("_Activity"), Is.Zero);

                // Quest's terminal cannot release Desktop's still-running local worker scope.
                SetPrivateField(questFixture.Scene, "m_UpdatingGenerators", false);
                questFixture.Scene.OnActivityProjectionCompleted.Invoke(404, ActivityProjectionCompletionKind.Cancelled);
                await WaitUntilAsync(() => GetSessionActivityProjectionJob(sessions.QuestOwner) == null, "Quest did not release its scope after its local worker became terminal.");
                Assert.That(questFixture.Scene.IsActivityProjectionBusy, Is.False);
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.True);
                Assert.That(GetTerminalFlag(desktopActive, "LocalTerminal"), Is.False);

                SetPrivateField(desktopFixture.Scene, "m_UpdatingGenerators", false);
                desktopFixture.Scene.OnActivityProjectionCompleted.Invoke(303, ActivityProjectionCompletionKind.Cancelled);
                await WaitUntilAsync(() => GetSessionActivityProjectionJob(sessions.DesktopOwner) == null, "Desktop did not release its scope after its local worker became terminal.");
                Assert.That(desktopFixture.Scene.IsActivityProjectionBusy, Is.False);
                Assert.That(desktopFixture.Scene.IsGeneratorUpToDate, Is.False, "A cancelled worker must not publish stale projection output.");
                Assert.That(questFixture.Scene.IsGeneratorUpToDate, Is.False, "A cancelled worker must not publish stale projection output.");
            }
            finally
            {
                await sessions.CloseAsync();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        [TestCase(false)]
        [TestCase(true)]
        public async Task ActivityProjectionAutomaticPolicy_IsSynchronizedAndGatesStaleAutoStart(bool automaticEnabled)
        {
            using var desktopFixture = new SessionSceneFixture(0);
            using var questFixture = new SessionSceneFixture(0);
            desktopFixture.InitializeProjectionMaterials();
            questFixture.InitializeProjectionMaterials();
            PrepareAutomaticProjectionInputs(desktopFixture.Scene);
            PrepareAutomaticProjectionInputs(questFixture.Scene);
            InvokePrivateMethod(desktopFixture.Scene, "InitializeAutomaticActivityProjection", automaticEnabled);

            LiveActivityProjectionSessionPair sessions = await LiveActivityProjectionSessionPair.ConnectAsync(desktopFixture.Scene, questFixture.Scene, CreatePreparedBinding());
            FieldInfo preferencesField = sessions.DesktopOwner.GetType().GetField("m_UserPreferences", BindingFlags.Instance | BindingFlags.NonPublic);
            object originalPreferences = preferencesField.GetValue(sessions.DesktopOwner);
            try
            {
                var preferences = new UserPreferences();
                preferences.Visualization._3D.AutomaticEEGUpdate = automaticEnabled;
                preferencesField.SetValue(sessions.DesktopOwner, preferences);
                InvokePrivateMethod(sessions.DesktopOwner, "OnDesktopPreferencesSaved");

                await WaitUntilAsync(() => questFixture.Scene.ProjectionRequested == automaticEnabled && questFixture.Scene.AutomaticRecomputeEnabled == automaticEnabled, "Quest did not apply Desktop's automatic projection policy and requested state.");
                Assert.That(desktopFixture.Scene.ProjectionRequested, Is.EqualTo(automaticEnabled));
                Assert.That(questFixture.Scene.ProjectionRequested, Is.EqualTo(automaticEnabled));
                Assert.That(desktopFixture.Scene.AutomaticRecomputeEnabled, Is.EqualTo(automaticEnabled));
                Assert.That(questFixture.Scene.AutomaticRecomputeEnabled, Is.EqualTo(automaticEnabled));
                Assert.That(ShouldStartActivityProjection(desktopFixture.Scene, automaticEnabled), Is.EqualTo(automaticEnabled));
                Assert.That(ShouldStartActivityProjection(questFixture.Scene, automaticEnabled), Is.EqualTo(automaticEnabled));
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That((V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner), Is.EqualTo(V2PersistentTransportState.Connected));
            }
            finally
            {
                preferencesField.SetValue(sessions.DesktopOwner, originalPreferences);
                await sessions.CloseAsync();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestExplicitRequest_StartsPairedGenerationWhenAutomaticRecomputeIsDisabled()
        {
            using var desktopFixture = new SessionSceneFixture(0);
            using var questFixture = new SessionSceneFixture(0);
            desktopFixture.InitializeProjectionMaterials();
            questFixture.InitializeProjectionMaterials();

            LiveActivityProjectionSessionPair sessions = await LiveActivityProjectionSessionPair.ConnectAsync(desktopFixture.Scene, questFixture.Scene, CreatePreparedBinding());
            FieldInfo preferencesField = sessions.DesktopOwner.GetType().GetField("m_UserPreferences", BindingFlags.Instance | BindingFlags.NonPublic);
            object originalPreferences = preferencesField.GetValue(sessions.DesktopOwner);
            try
            {
                var preferences = new UserPreferences();
                preferences.Visualization._3D.AutomaticEEGUpdate = false;
                preferencesField.SetValue(sessions.DesktopOwner, preferences);
                InvokePrivateMethod(sessions.DesktopOwner, "OnDesktopPreferencesSaved");
                await WaitUntilAsync(() => !questFixture.Scene.AutomaticRecomputeEnabled, "Quest did not receive the disabled automatic policy.");

                Assert.That(desktopFixture.Scene.ProjectionRequested, Is.False);
                Assert.That(questFixture.Scene.ProjectionRequested, Is.False);
                Assert.That(GetPrivateField<V2JobGenerationRegistry>(sessions.DesktopOwner, "m_ActivityProjectionGenerations").TrackedScopeCount, Is.Zero);
                Assert.That((ulong)sessions.QuestOwner.GetType().GetField("m_LastDesktopActivityProjectionGeneration", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sessions.QuestOwner), Is.Zero);

                questFixture.Scene.RequestActivityProjection();
                Assert.That((bool)InvokePrivateMethod(sessions.QuestOwner, "HandleLocalActivityProjectionStart"), Is.True, "The explicit Quest start callback should be consumed by the session.");
                await WaitUntilAsync(() => GetPrivateField<V2JobGenerationRegistry>(sessions.DesktopOwner, "m_ActivityProjectionGenerations").TrackedScopeCount > 0 && (ulong)sessions.QuestOwner.GetType().GetField("m_LastDesktopActivityProjectionGeneration", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sessions.QuestOwner) > 0, "The explicit Quest request did not start a paired activity-projection generation.");

                Assert.That(desktopFixture.Scene.AutomaticRecomputeEnabled, Is.False);
                Assert.That(questFixture.Scene.AutomaticRecomputeEnabled, Is.False);
                Assert.That(sessions.DesktopTransport.State, Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That((V2PersistentTransportState)sessions.QuestOwner.GetType().GetProperty("TransportState").GetValue(sessions.QuestOwner), Is.EqualTo(V2PersistentTransportState.Connected));
            }
            finally
            {
                preferencesField.SetValue(sessions.DesktopOwner, originalPreferences);
                await sessions.CloseAsync();
            }
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
        public void SceneOperationBulkReceiver_RoutesBufferedSecondMaskChunksBeforeFollowingMutation()
        {
            const int triangleCount = 40000;
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 128, maxBulkBodyBytesPerTransfer: 65536, maxBulkBodyBytesTotal: 131072);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Desktop, limits: limits);
            var firstMask = CreateTriangleMask(0xFF);
            var secondMask = CreateTriangleMask(0xAA);
            var followingMutation = new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, true);
            Assert.That(scheduler.EnqueueMutation(firstMask, operationId: new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000071")), canonicalSequence: 1UL).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(secondMask, operationId: new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000072")), canonicalSequence: 2UL).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(followingMutation, operationId: new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000073")), canonicalSequence: 3UL).Accepted, Is.True);

            var received = new List<V2TransportRecord>();
            while (scheduler.TryGetNextTransmission(out V2TransmissionAttempt attempt))
            {
                V2ReliableFrame frame = attempt.Frame;
                received.Add(new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, frame.OperationId, frame.StreamId, frame.ReliableFrameSequence ?? 0, frame.OriginSequence ?? 0, V2OriginDevice.Desktop, frame.Lane, frame.BodySchema, chunkIndex: frame.ChunkIndex, payload: frame.GetPayloadCopy(), canonicalSequence: frame.CanonicalSequence, observedCanonicalSequence: frame.ObservedCanonicalSequence, mutation: frame.Lane == V2ScheduleLane.Interactive ? V2MutationPayloadCodec.Decode(frame.GetPayloadCopy()) : null));
                Assert.That(scheduler.Acknowledge(frame.StreamId, frame.ReliableFrameSequence.Value), Is.True);
            }

            var receiver = new V2SceneOperationBulkReceiver();
            var deferred = new Queue<V2TransportRecord>();
            var applied = new List<V2Mutation>();

            void Accept(V2TransportRecord record)
            {
                if (receiver.IsActive)
                {
                    if (!receiver.TryAppend(record, out V2TransportRecord completed))
                    {
                        deferred.Enqueue(record);
                        return;
                    }

                    if (completed != null) applied.Add(V2MutationPayloadCodec.Decode(completed.GetPayloadCopy()));
                }
                else if (receiver.IsMutationDescriptor(record))
                {
                    receiver.Begin(record);
                }
                else
                {
                    applied.Add(record.Mutation ?? V2MutationPayloadCodec.Decode(record.GetPayloadCopy()));
                }

                DrainDeferred();
            }

            void DrainDeferred()
            {
                while (true)
                {
                    if (receiver.IsActive)
                    {
                        if (!receiver.TryAppendNextBuffered(deferred, record => record, out _, out V2TransportRecord completed)) return;
                        if (completed != null) applied.Add(V2MutationPayloadCodec.Decode(completed.GetPayloadCopy()));
                        continue;
                    }

                    if (deferred.Count == 0) return;
                    V2TransportRecord next = deferred.Dequeue();
                    if (receiver.IsMutationDescriptor(next)) receiver.Begin(next);
                    else applied.Add(next.Mutation ?? V2MutationPayloadCodec.Decode(next.GetPayloadCopy()));
                }
            }

            foreach (V2TransportRecord record in received) Accept(record);

            Assert.That(receiver.IsActive, Is.False);
            Assert.That(deferred, Is.Empty);
            Assert.That(applied, Has.Count.EqualTo(3));
            Assert.That(applied[0], Is.TypeOf<ApplyTriangleMask>());
            Assert.That(applied[1], Is.TypeOf<ApplyTriangleMask>());
            Assert.That(V2MutationPayloadCodec.Encode(applied[0]), Is.EqualTo(V2MutationPayloadCodec.Encode(firstMask)));
            Assert.That(V2MutationPayloadCodec.Encode(applied[1]), Is.EqualTo(V2MutationPayloadCodec.Encode(secondMask)));
            Assert.That(applied[2], Is.TypeOf<SetSceneBoolean>());

            ApplyTriangleMask CreateTriangleMask(byte value)
            {
                byte[] visible = Enumerable.Repeat(value, (triangleCount + 7) / 8).ToArray();
                return new ApplyTriangleMask(new[]
                {
                    new V2TriangleMask(new TopologyId("bulk-mask:complete"), triangleCount, visible),
                    new V2TriangleMask(new TopologyId("bulk-mask:simplified"), triangleCount, visible)
                });
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void T13CorrelationResultBulk_InterleavesInteractiveTrafficAndReassemblesOneTypedResult()
        {
            const int resultBytes = 64 * 1024;
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 1024, maxBulkBodyBytesPerTransfer: 1024 * 1024, maxBulkBodyBytesTotal: 2 * 1024 * 1024);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Desktop, limits: limits);
            var jobId = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000074"));
            byte[] resource = Enumerable.Range(0, resultBytes).Select(index => (byte)(index * 17)).ToArray();
            var correlation = new SetCorrelationResult(jobId, 5, resource);
            var interactive = new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, true);
            Assert.That(scheduler.EnqueueMutation(correlation, operationId: jobId, canonicalSequence: 1UL).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(interactive, operationId: new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000075")), canonicalSequence: 2UL).Accepted, Is.True);

            var received = new List<V2TransportRecord>();
            var enqueueWatch = Stopwatch.StartNew();
            while (scheduler.TryGetNextTransmission(out V2TransmissionAttempt attempt))
            {
                V2ReliableFrame frame = attempt.Frame;
                received.Add(new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, frame.OperationId, frame.StreamId, frame.ReliableFrameSequence ?? 0, frame.OriginSequence ?? 0, V2OriginDevice.Desktop, frame.Lane, frame.BodySchema, chunkIndex: frame.ChunkIndex, payload: frame.GetPayloadCopy(), canonicalSequence: frame.CanonicalSequence, observedCanonicalSequence: frame.ObservedCanonicalSequence, mutation: frame.Lane == V2ScheduleLane.Interactive ? V2MutationPayloadCodec.Decode(frame.GetPayloadCopy()) : null));
                Assert.That(scheduler.Acknowledge(frame.StreamId, frame.ReliableFrameSequence.Value), Is.True);
            }

            enqueueWatch.Stop();

            var receiver = new V2SceneOperationBulkReceiver();
            var deferred = new Queue<V2TransportRecord>();
            var applied = new List<V2Mutation>();
            bool interactiveInterleaved = false;
            foreach (V2TransportRecord record in received)
            {
                if (receiver.IsActive)
                {
                    if (receiver.TryAppend(record, out V2TransportRecord completed))
                    {
                        if (completed != null) applied.Add(V2MutationPayloadCodec.Decode(completed.GetPayloadCopy()));
                    }
                    else
                    {
                        Assert.That(record.Lane, Is.EqualTo(V2ScheduleLane.Interactive));
                        interactiveInterleaved = true;
                        deferred.Enqueue(record);
                    }
                }
                else if (receiver.IsMutationDescriptor(record)) receiver.Begin(record);
                else applied.Add(record.Mutation ?? V2MutationPayloadCodec.Decode(record.GetPayloadCopy()));

                if (!receiver.IsActive)
                    while (deferred.Count > 0)
                        applied.Add(deferred.Dequeue().Mutation);
            }

            Assert.That(interactiveInterleaved, Is.True, "An independent interactive mutation should pass while the correlation body is arriving.");
            Assert.That(receiver.IsActive, Is.False);
            Assert.That(deferred, Is.Empty);
            Assert.That(applied, Has.Count.EqualTo(2));
            Assert.That(applied[0], Is.TypeOf<SetCorrelationResult>());
            CollectionAssert.AreEqual(resource, ((SetCorrelationResult)applied[0]).ResultBytes);
            Assert.That(V2MutationPayloadCodec.Encode(applied[1]), Is.EqualTo(V2MutationPayloadCodec.Encode(interactive)));
            int bulkChunks = received.Count(record => record.Lane == V2ScheduleLane.Bulk);
            Assert.That(bulkChunks, Is.GreaterThan(1));
            TestContext.WriteLine($"HBP_SYNC_T13_CORRELATION_BULK resultBytes={resultBytes} bulkChunks={bulkChunks} enqueueMs={enqueueWatch.Elapsed.TotalMilliseconds:F3} interactiveInterleaved={interactiveInterleaved}");
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13CorrelationTransfer_AppliesTypedResultPreservesProvenanceAndSendsExactReady()
        {
            using var desktopFixture = new CorrelationSceneFixture(40);
            using var questFixture = new CorrelationSceneFixture(40);
            using var loading = new LoadingManagerFixture();
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 512, interactiveBurst: 1, maxBulkBodyBytesPerTransfer: 2 * 1024 * 1024, maxBulkBodyBytesTotal: 4 * 1024 * 1024);
            object questOwner = CreateQuestSession(questFixture.Scene, CreatePreparedBinding(questFixture.ColumnId));
            var desktopPeer = CreateTransport(V2OriginDevice.Desktop, 55340, limits);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var stop = new CancellationTokenSource();
            Task<Exception> questRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, new DelayedReadStream(pair.Server.GetStream(), TimeSpan.FromMilliseconds(1)), stop.Token));
            Task<Exception> desktopRun = CaptureRunAsync(desktopPeer, pair.Client.GetStream(), stop.Token);
            var jobId = new OperationId(GuidFor(55341));
            const ulong generation = 23;
            bool controlInterleaved = false;
            bool interactiveInterleaved = false;
            Task<Exception> rejectedFilterRequest = null;

            try
            {
                await WaitUntilAsync(() => (V2PersistentTransportState)questOwner.GetType().GetProperty("TransportState").GetValue(questOwner) == V2PersistentTransportState.Connected && desktopPeer.State == V2PersistentTransportState.Connected, "The typed correlation transfer did not finish the authenticated loopback handshake.");
                Assert.That(V2SiteFilterRequestRouter.TryGetHandler(questFixture.Scene, out Func<V2SiteFilterRequest, CancellationToken, Task<bool>> filterHandler), Is.True);

                Assert.That(desktopPeer.EnqueueSessionControl(V2CorrelationControlCodec.Encode(new V2CorrelationControl(V2CorrelationControlKind.Started, jobId, generation)), V2DeliveryReliability.Reliable).Accepted, Is.True);
                await WaitUntilAsync(() => GetActiveQuestCorrelationJob(questOwner) != null, "Quest did not reserve the typed correlation result generation.");
                object activeCorrelation = GetActiveQuestCorrelationJob(questOwner);
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.EqualTo(1));

                rejectedFilterRequest = CaptureTaskExceptionAsync(filterHandler(V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), CancellationToken.None));
                Exception filterFailure = await AwaitGuardValueAsync(rejectedFilterRequest);
                Assert.That(filterFailure, Is.InstanceOf<InvalidOperationException>(), "A Quest local filter request must reject while correlation is active.");
                Assert.That(GetActiveQuestCorrelationJob(questOwner), Is.SameAs(activeCorrelation));
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.EqualTo(1));

                byte[] resultBytes = desktopFixture.CreateResultBytes();
                Assert.That(resultBytes.Length, Is.GreaterThan(64 * 1024), "The typed fixture must exercise bulk chunking.");
                CorrelationProvenance expectedProvenance = desktopFixture.CreateProvenance();
                var result = new SetCorrelationResult(jobId, generation, resultBytes);
                byte[] mutationPayload = V2MutationPayloadCodec.Encode(result);
                int bulkChunks = (mutationPayload.Length + limits.BulkChunkBytes - 1) / limits.BulkChunkBytes;
                var interactive = new SetSiteColor(new ColumnId(questFixture.ColumnId), new SiteId(questFixture.Sites[0].Information.FullID), 0.2f, 0.7f, 0.4f, 1f);

                using var desktopBoundary = new V2SceneMutationBoundary(desktopFixture.Scene, V2OriginDevice.Desktop);
                SetCorrelationResult canonicalResult = desktopBoundary.CreateCorrelationResult(jobId, generation, resultBytes);
                var transferWatch = Stopwatch.StartNew();
                V2EnqueueResult resultEnqueue = desktopPeer.EnqueueMutation(canonicalResult, 1UL, null, coalesciblePreview: false, operationId: jobId);
                Assert.That(resultEnqueue.Accepted, Is.True, $"The canonical result should be accepted (disposition={resultEnqueue.Disposition}).");
                await WaitUntilAsync(() => (bool)GetQuestSceneOperationBulkReceiver(questOwner).GetType().GetProperty("IsActive").GetValue(GetQuestSceneOperationBulkReceiver(questOwner)), "Quest did not begin receiving the typed correlation body.");

                Assert.That(desktopPeer.EnqueueMutation(interactive, 2UL, null, coalesciblePreview: false, operationId: new OperationId(GuidFor(55342))).Accepted, Is.True);
                await WaitUntilAsync(() => GetDeferredRecords(questOwner).Count > 0 && (bool)GetQuestSceneOperationBulkReceiver(questOwner).GetType().GetProperty("IsActive").GetValue(GetQuestSceneOperationBulkReceiver(questOwner)), "Interactive scene traffic was not deferred while correlation chunks were in flight.");
                V2TransportRecord deferredInteractive = GetDeferredRecords(questOwner).Single();
                Assert.That(deferredInteractive.MessageId, Is.EqualTo(new OperationId(GuidFor(55342))));
                Assert.That(deferredInteractive.Mutation, Is.TypeOf<SetSiteColor>());
                interactiveInterleaved = true;

                var competingFilter = new OperationId(GuidFor(55343));
                Assert.That(desktopPeer.EnqueueSessionControl(V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(V2SiteFilterControlKind.Started, competingFilter, 1)), V2DeliveryReliability.Reliable).Accepted, Is.True);
                V2SiteFilterControl filterRejection = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(filterRejection.Kind, Is.EqualTo(V2SiteFilterControlKind.Failed));
                Assert.That(filterRejection.JobId, Is.EqualTo(competingFilter));
                Assert.That(filterRejection.FailureCode, Is.EqualTo("quest_scene_busy"));
                Assert.That(GetActiveQuestCorrelationJob(questOwner), Is.SameAs(activeCorrelation));
                Assert.That(IsActiveJobCancelled(activeCorrelation), Is.False);
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.EqualTo(1));
                controlInterleaved = true;

                V2CorrelationControl ready = ReadCorrelationControl(await ReadIncomingAsync(desktopPeer));
                transferWatch.Stop();
                Assert.That(ready.Kind, Is.EqualTo(V2CorrelationControlKind.Ready));
                Assert.That(ready.JobId, Is.EqualTo(jobId));
                Assert.That(ready.Generation, Is.EqualTo(generation), "Ready must acknowledge the exact generation whose typed result was applied.");
                await WaitUntilAsync(() => GetActiveQuestCorrelationJob(questOwner) == null && GetDeferredRecords(questOwner).Count == 0 && GetDriverCanonicalWatermark(questOwner) == 2UL && questFixture.Sites[0].State.Color == new Color(0.2f, 0.7f, 0.4f, 1f), "Quest did not complete the typed result and apply interleaved traffic.");

                CollectionAssert.AreEqual(resultBytes, CorrelationResultResource.Capture(questFixture.Scene).Encode());
                Assert.That(questFixture.Column.CorrelationProvenance.Equals(expectedProvenance), Is.True);
                Assert.That(questFixture.Scene.DisplayCorrelations, Is.True);
                Assert.That(questRun.IsCompleted, Is.False, "The Quest transport must remain connected after applying the interleaved mutation.");
                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(2UL));
                Assert.That(questFixture.Sites[0].State.Color, Is.EqualTo(new Color(0.2f, 0.7f, 0.4f, 1f)));
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.Zero);
                Assert.That(interactiveInterleaved, Is.True);
                Assert.That(controlInterleaved, Is.True);
                TestContext.WriteLine($"HBP_SYNC_T13_CORRELATION_TRANSFER resultBytes={resultBytes.Length} mutationBytes={mutationPayload.Length} bulkChunks={bulkChunks} transferMs={transferWatch.Elapsed.TotalMilliseconds:F3} controlInterleaved={controlInterleaved} interactiveInterleaved={interactiveInterleaved} provenance={expectedProvenance.Source}");
            }
            finally
            {
                stop.Cancel();
                pair.Close();
                desktopPeer.Dispose();
                ((IDisposable)questOwner).Dispose();
                await AwaitGuardAsync(Task.WhenAll(questRun, desktopRun));
                if (rejectedFilterRequest != null && !rejectedFilterRequest.IsCompleted)
                    await AwaitGuardAsync(CaptureTaskExceptionAsync(rejectedFilterRequest));
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void SiteConfigurationBulk_StreamsThirtyThousandAssignmentsAroundIndependentInteractiveTraffic()
        {
            const int siteCount = 30000;
            var assignments = Enumerable.Range(0, siteCount).Select(index => new V2SiteConfigurationAssignment(new ColumnId("bulk-column"), new SiteId("bulk-site-" + index), index % 2 == 0, index % 3 == 0, 0.25f, 0.5f, 0.75f, 1f, Array.Empty<string>())).ToArray();
            var batch = new SetSiteConfigurationBatch(assignments);
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256, bulkChunkBytes: 8192, maxBulkBodyBytesPerTransfer: 16 * 1024 * 1024, maxBulkBodyBytesTotal: 32 * 1024 * 1024);
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Desktop, limits: limits);
            var batchOperation = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000081"));
            var interactiveOperation = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000082"));
            var interactive = new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, true);
            var enqueueWatch = Stopwatch.StartNew();
            Assert.That(scheduler.EnqueueMutation(batch, operationId: batchOperation, canonicalSequence: 1UL).Accepted, Is.True);
            enqueueWatch.Stop();
            Assert.That(scheduler.EnqueueMutation(interactive, operationId: interactiveOperation, canonicalSequence: 2UL, coalesciblePreview: false).Accepted, Is.True);

            var received = new List<V2TransportRecord>();
            while (scheduler.TryGetNextTransmission(out V2TransmissionAttempt attempt))
            {
                V2ReliableFrame frame = attempt.Frame;
                received.Add(new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, frame.OperationId, frame.StreamId, frame.ReliableFrameSequence ?? 0, frame.OriginSequence ?? 0, V2OriginDevice.Desktop, frame.Lane, frame.BodySchema, chunkIndex: frame.ChunkIndex, payload: frame.GetPayloadCopy(), canonicalSequence: frame.CanonicalSequence, observedCanonicalSequence: frame.ObservedCanonicalSequence, mutation: frame.Lane == V2ScheduleLane.Interactive ? V2MutationPayloadCodec.Decode(frame.GetPayloadCopy()) : null));
                Assert.That(scheduler.Acknowledge(frame.StreamId, frame.ReliableFrameSequence.Value), Is.True);
            }

            var receiver = new V2SceneOperationBulkReceiver();
            var deferred = new Queue<V2TransportRecord>();
            var applied = new List<V2Mutation>();
            bool independentInteractiveObserved = false;

            void DrainDeferred()
            {
                while (true)
                {
                    if (receiver.IsActive)
                    {
                        if (!receiver.TryAppendNextBuffered(deferred, record => record, out _, out V2TransportRecord completed)) return;
                        if (completed != null) applied.Add(V2MutationPayloadCodec.Decode(completed.GetPayloadCopy()));
                        continue;
                    }

                    if (deferred.Count == 0) return;
                    V2TransportRecord next = deferred.Dequeue();
                    if (receiver.IsMutationDescriptor(next)) receiver.Begin(next);
                    else applied.Add(next.Mutation ?? V2MutationPayloadCodec.Decode(next.GetPayloadCopy()));
                }
            }

            foreach (V2TransportRecord record in received)
            {
                if (receiver.IsActive)
                {
                    if (receiver.TryAppend(record, out V2TransportRecord completed))
                    {
                        if (completed != null) applied.Add(V2MutationPayloadCodec.Decode(completed.GetPayloadCopy()));
                    }
                    else if (record.Lane == V2ScheduleLane.Interactive)
                    {
                        independentInteractiveObserved = true;
                        deferred.Enqueue(record);
                    }
                    else deferred.Enqueue(record);
                }
                else if (receiver.IsMutationDescriptor(record)) receiver.Begin(record);
                else applied.Add(record.Mutation ?? V2MutationPayloadCodec.Decode(record.GetPayloadCopy()));

                DrainDeferred();
            }

            Assert.That(independentInteractiveObserved, Is.True, "The interactive operation should be transmitted while the bulk body is still arriving.");
            Assert.That(receiver.IsActive, Is.False);
            Assert.That(deferred, Is.Empty);
            Assert.That(applied, Has.Count.EqualTo(2));
            Assert.That(applied[0], Is.TypeOf<SetSiteConfigurationBatch>());
            Assert.That(((SetSiteConfigurationBatch)applied[0]).Assignments, Has.Count.EqualTo(siteCount));
            Assert.That(V2MutationPayloadCodec.Encode(applied[1]), Is.EqualTo(V2MutationPayloadCodec.Encode(interactive)));
            Assert.That(received.Count(record => record.Lane == V2ScheduleLane.Bulk), Is.GreaterThan(1));
            TestContext.WriteLine($"HBP_SYNC_T11_CONFIGURATION_BULK sites={siteCount} bulkChunks={received.Count(record => record.Lane == V2ScheduleLane.Bulk)} enqueueMs={enqueueWatch.Elapsed.TotalMilliseconds:F3} interactiveInterleaved={independentInteractiveObserved}");
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
                await WaitUntilAsync(() => GetDeferredRecords(questOwner).Count == 0 && GetPrivateField<V2ApplicationCompletionWatermark>(questOwner, "m_ApplicationCompletion").CompletedThrough >= 3, "Checkpoint replay did not complete application retirement.");
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
        public async Task T11ThirtyThousandSiteCheckpoint_EncodesTransfersAndAppliesWithInterleavedInteractiveTraffic()
        {
            const int siteCount = 30000;
            using var source = new SessionSceneFixture(siteCount);
            using var target = new SessionSceneFixture(siteCount);
            for (int index = 0; index < siteCount; index++)
            {
                float channel = (index % 256) / 255f;
                source.Sites[index].State.ApplySynchronizedState(false, index % 2 == 0, index % 3 == 0, new Color(channel, 1f - channel, (index % 17) / 16f, 1f), new[] { "bulk", index % 2 == 0 ? "even" : "odd" });
            }

            PreparedSceneDeliveryBinding binding = CreatePreparedBinding();
            object questOwner = CreateQuestSession(target.Scene, binding);
            var limits = new V2SchedulerLimits(inlineThresholdBytes: 256);
            var desktopPeer = CreateTransport(V2OriginDevice.Desktop, 5095, limits);
            var checkpointOperation = new OperationId(GuidFor(55131));
            var interactiveOperation = new OperationId(GuidFor(55132));
            var barrierOperation = new OperationId(GuidFor(55133));
            V2ScheduleDescriptor allScene = V2ScheduleDescriptor.ForBarrier(Scene, Incarnation, null, V2BarrierScope.AllScene);
            var stopwatch = Stopwatch.StartNew();
            byte[] checkpoint;
            V2SceneMutationCheckpoint captured;
            using (var sourceBoundary = new V2SceneMutationBoundary(source.Scene, V2OriginDevice.Desktop))
            {
                captured = sourceBoundary.CaptureCheckpoint();
                checkpoint = V2SceneMutationCheckpointCodec.Encode(0, captured);
            }

            Assert.That(captured.SiteColors, Is.Empty, "Persisted site state is represented once by the T11 assignment batch.");
            Assert.That(captured.T09Records.Any(record => record.Value is SetSiteHighlight or SetSiteLabels), Is.False, "New checkpoints omit duplicate persisted presentation records while older records remain decodable.");
            SetSiteConfigurationBatch assignments = captured.T11Records.Select(record => record.Value).OfType<SetSiteConfigurationBatch>().Single();
            Assert.That(assignments.Assignments, Has.Count.EqualTo(siteCount));
            int checkpointRecordCount = captured.SiteColors.Count + captured.CutDefinitions.Count + captured.TimelineAnchors.Count + captured.T09Records.Count + captured.T10Records.Count + captured.T11Records.Count;
            Assert.That(checkpointRecordCount, Is.LessThan(65536));
            Assert.That(checkpoint.Length, Is.GreaterThan(limits.InlineThresholdBytes));
            Assert.That(desktopPeer.EnqueueSceneOperation(checkpoint, allScene, structural: true, bodySchema: V2PublicationCheckpointBulkReceiver.BodySchema, operationId: checkpointOperation).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueMutation(new SetSiteHighlight(new ColumnId(source.ColumnId), new SiteId(source.SiteIds[0]), false), 1UL, null, coalesciblePreview: false, operationId: interactiveOperation).Accepted, Is.True);
            Assert.That(desktopPeer.EnqueueSceneOperation(V2PublicationControlCodec.EncodeLiveBarrier(1), allScene, structural: true, operationId: barrierOperation).Accepted, Is.True);

            using var firstPair = await LoopbackPeerPair.ConnectAsync();
            var pausedBulk = new PauseFirstBulkWriteStream(firstPair.Client.GetStream());
            Task<Exception> desktopRun = CaptureRunAsync(desktopPeer, pausedBulk, CancellationToken.None);
            Task<Exception> questRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, firstPair.Server.GetStream(), CancellationToken.None));
            try
            {
                await AwaitGuardAsync(pausedBulk.Started.Task, timeoutSeconds: 60);
                await WaitUntilAsync(() => GetDeferredRecords(questOwner).Count == 2, "The 30,000-site checkpoint did not retain interleaved interactive traffic.", timeoutSeconds: 60);
                Assert.That(GetCheckpointReceiverActive(questOwner), Is.True);
                Assert.That(GetDeferredRecords(questOwner).Select(record => record.MessageId), Is.EqualTo(new[] { interactiveOperation, barrierOperation }));
                Assert.That(target.Sites[0].State.IsBlackListed, Is.False, "The incomplete checkpoint must not apply a partial batch.");

                pausedBulk.Release.TrySetResult(true);
                V2TransportRecord acknowledgement = await ReadIncomingAsync(desktopPeer, timeoutSeconds: 60);
                Assert.That(V2PublicationControlCodec.TryDecodeAcknowledgement(acknowledgement.GetPayloadCopy(), out OperationId acknowledgedBarrier), Is.True);
                Assert.That(acknowledgedBarrier, Is.EqualTo(barrierOperation));
                await WaitUntilAsync(() => GetDeferredRecords(questOwner).Count == 0 && GetPrivateField<V2ApplicationCompletionWatermark>(questOwner, "m_ApplicationCompletion").CompletedThrough >= 3, "The checkpoint barrier did not finish application retirement.");
                Assert.That(GetCheckpointReceiverActive(questOwner), Is.False);
                Assert.That(GetDeferredRecords(questOwner), Is.Empty);
                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(1UL));

                int blacklistedCount = 0;
                for (int index = 0; index < siteCount; index++)
                {
                    SiteState state = target.Sites[index].State;
                    bool blacklisted = index % 2 == 0;
                    bool highlighted = index % 3 == 0;
                    if (blacklisted) blacklistedCount++;
                    if (index == 0) highlighted = false; // The queued interactive operation follows the checkpoint.
                    Assert.That(state.IsBlackListed, Is.EqualTo(blacklisted), $"site {index} blacklist");
                    Assert.That(state.IsHighlighted, Is.EqualTo(highlighted), $"site {index} highlight");
                    float channel = (index % 256) / 255f;
                    Assert.That(state.Color, Is.EqualTo(new Color(channel, 1f - channel, (index % 17) / 16f, 1f)), $"site {index} color");
                    Assert.That(state.Labels, Is.EqualTo(new[] { "bulk", blacklisted ? "even" : "odd" }), $"site {index} ordered labels");
                }

                stopwatch.Stop();
                TestContext.WriteLine($"HBP_SYNC_T11_REAL_CHECKPOINT sites={siteCount} records={checkpointRecordCount} bytes={checkpoint.Length} chunks={(checkpoint.Length + limits.BulkChunkBytes - 1) / limits.BulkChunkBytes} blacklisted={blacklistedCount} capture_encode_transfer_apply_ms={stopwatch.Elapsed.TotalMilliseconds:F1} interleaved=1");
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
                desktopPeer.Dispose();
                firstPair.Close();
                await AwaitGuardAsync(Task.WhenAll(desktopRun, questRun), timeoutSeconds: 60);
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
                await WaitUntilAsync(() => !GetDeferredDrainPending(questOwner), "The publication acknowledgement must be followed by completion of the deferred drain.");
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

                DropMessageKindWriteStream dropped;
                try
                {
                    dropped = await AwaitGuardValueAsync(firstDroppedBarrier.Task);
                }
                catch (Exception exception)
                {
                    string failureReason = desktopOwnerType.GetProperty("FailureReason").GetValue(desktopOwner) as string;
                    throw new AssertionException($"Initial connection was not opened: owner={failureReason ?? "none"}; publication={startPublication?.Exception}; wait={exception}");
                }

                try
                {
                    await AwaitGuardAsync(dropped.Dropped.Task);
                }
                catch (Exception exception)
                {
                    string failureReason = desktopOwnerType.GetProperty("FailureReason").GetValue(desktopOwner) as string;
                    throw new AssertionException($"Initial barrier was not written: owner={failureReason ?? "none"}; publication={startPublication?.Exception}; wait={exception}");
                }

                Assert.That((bool)desktopOwnerType.GetProperty("IsLive").GetValue(desktopOwner), Is.False, "The production Desktop owner must wait for the barrier acknowledgement.");
                Assert.That(dropped.DroppedRecord.MessageId, Is.Not.Null);

                connectionPairs[0].Close();
                LoopbackPeerPair resumedPair;
                try
                {
                    resumedPair = await AwaitGuardValueAsync(secondPairReady.Task);
                }
                catch (Exception exception)
                {
                    string failureReason = desktopOwnerType.GetProperty("FailureReason").GetValue(desktopOwner) as string;
                    V2PersistentTransport diagnosticTransport = (V2PersistentTransport)desktopOwnerType.GetField("m_Transport", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                    throw new AssertionException($"Replica did not reconnect: opens={openCount}; state={diagnosticTransport.State}; owner={failureReason ?? "none"}; quest={questOwner.GetType().GetProperty("TransportState").GetValue(questOwner)}; wait={exception}");
                }

                try
                {
                    await AwaitGuardAsync(startPublication);
                }
                catch (Exception exception)
                {
                    string failureReason = desktopOwnerType.GetProperty("FailureReason").GetValue(desktopOwner) as string;
                    string questFailures = string.Join("; ", questRuns.Where(task => task.IsCompleted).Select(task => task.Result?.ToString() ?? "completed cleanly"));
                    throw new AssertionException($"Initial publication was not acknowledged: owner={failureReason ?? "none"}; quest={questFailures}; wait={exception}");
                }

                Assert.That((bool)desktopOwnerType.GetProperty("IsLive").GetValue(desktopOwner), Is.True);
                Assert.That((bool)desktopOwnerType.GetProperty("IsClosed").GetValue(desktopOwner), Is.False);
                Assert.That(openCount, Is.EqualTo(2), "Reconnect should reuse the existing Desktop owner and transport.");
                Assert.That(connectionTransports, Has.Count.EqualTo(2));
                Assert.That(ReferenceEquals(connectionTransports[0], connectionTransports[1]), Is.True, "Both production connection attempts must use the same persistent Desktop transport.");
                Assert.That(questAckCount.PublicationAcknowledgementWrites, Is.EqualTo(1), "The retained Quest session must apply and acknowledge the replayed barrier once.");
                Assert.That((V2PersistentTransportState)questOwner.GetType().GetProperty("TransportState").GetValue(questOwner), Is.EqualTo(V2PersistentTransportState.Connected));
                Assert.That(resumedPair, Is.SameAs(connectionPairs[1]));

                var desktopTransport = (V2PersistentTransport)desktopOwnerType.GetField("m_Transport", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                await WaitUntilAsync(() => desktopTransport.SnapshotMetrics().OutstandingReliableFrames == 0, "Retained initial-publication controls were not acknowledged.");
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
        [Category("Sync.SceneFocused")]
        public async Task QuestSiteFilterVisualCancellation_NotifiesDesktopAndReleasesBothActivityScopes()
        {
            using var desktopFixture = new SessionSceneFixture(1);
            using var questFixture = new SessionSceneFixture(1);
            using var loading = new LoadingManagerFixture();
            object questOwner = CreateQuestSession(questFixture.Scene, CreatePreparedBinding());
            Type desktopOwnerType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(desktopOwnerType, Is.Not.Null);
            Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
            ConstructorInfo constructor = desktopOwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
            Assert.That(constructor, Is.Not.Null);
            object desktopOwner = constructor.Invoke(new object[] { desktopFixture.Scene, Session.Value.ToString(), Incarnation.Value.ToString(), null });
            Task desktopOperation = null;

            try
            {
                SetPrivateField(desktopOwner, "m_State", Enum.Parse(desktopOwnerType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, "Live"));
                var jobId = new OperationId(GuidFor(55201));
                MethodInfo runDesktopJob = desktopOwnerType.GetMethod("RunSiteFilterJobAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(runDesktopJob, Is.Not.Null);
                desktopOperation = (Task)runDesktopJob.Invoke(desktopOwner, new object[] { jobId, V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), CancellationToken.None, false });
                await WaitUntilAsync(() => GetDesktopActiveSiteFilterJob(desktopOwner)?.GetType().GetProperty("Identity")?.GetValue(GetDesktopActiveSiteFilterJob(desktopOwner)) != null, "Desktop did not reserve its site-filter scope.");
                object desktopActive = GetDesktopActiveSiteFilterJob(desktopOwner);
                object identity = desktopActive.GetType().GetProperty("Identity").GetValue(desktopActive);
                ulong generation = (ulong)identity.GetType().GetProperty("Generation").GetValue(identity);

                await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Started, jobId, generation), CancellationToken.None);
                await WaitUntilAsync(() => loading.LoadingCircle.gameObject.activeSelf, "The Quest loading visual did not open for the remote site-filter job.");
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.EqualTo(1));
                Assert.That(GetSensitiveActivityCount(desktopFixture.Scene), Is.EqualTo(1));

                loading.CancelVisual();
                await WaitUntilAsync(() => GetActiveQuestSiteFilterJob(questOwner) == null, "Quest did not retire the cancelled remote job.");
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.Zero, "Quest must release its activity scope after loading cancellation.");

                V2SiteFilterControl cancel = ReadNextQuestSiteFilterControl(questOwner);
                Assert.That(cancel.Kind, Is.EqualTo(V2SiteFilterControlKind.Cancel));
                Assert.That(cancel.JobId, Is.EqualTo(jobId));
                Assert.That(cancel.Generation, Is.EqualTo(generation), "The visual cancellation must notify Desktop for the exact active generation.");

                await InvokeDesktopSiteFilterControlAsync(desktopOwner, cancel, CancellationToken.None);
                Exception desktopFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(desktopOperation));
                Assert.That(desktopFailure, Is.InstanceOf<OperationCanceledException>());
                Assert.That(GetSensitiveActivityCount(desktopFixture.Scene), Is.Zero, "Desktop must release its activity scope after Quest cancellation.");
            }
            finally
            {
                if (desktopOwner is IDisposable desktopDisposable) desktopDisposable.Dispose();
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13QuestCorrelationCancellation_ReleasesRemoteActivityScope()
        {
            using var fixture = new SessionSceneFixture(1);
            using var loading = new LoadingManagerFixture();
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            var jobId = new OperationId(GuidFor(55301));
            const ulong generation = 11;
            try
            {
                await InvokeQuestCorrelationControlAsync(questOwner, new V2CorrelationControl(V2CorrelationControlKind.Started, jobId, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestCorrelationJob(questOwner) != null && loading.LoadingCircle.gameObject.activeSelf, "Quest did not reserve the remote correlation job.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(1));

                loading.CancelVisual();
                await WaitUntilAsync(() => GetActiveQuestCorrelationJob(questOwner) == null, "Quest did not retire the cancelled correlation generation.");
                V2CorrelationControl cancel = ReadNextQuestCorrelationControl(questOwner);
                Assert.That(cancel.Kind, Is.EqualTo(V2CorrelationControlKind.Cancel));
                Assert.That(cancel.JobId, Is.EqualTo(jobId));
                Assert.That(cancel.Generation, Is.EqualTo(generation), "Quest loading cancellation must name the active correlation generation.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero, "Cancellation must release the scene activity scope.");
                Assert.That(fixture.Sites[0].State.IsFiltered, Is.True, "A cancelled correlation job must not mutate unrelated scene state.");
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13QuestCorrelationCancellation_RetiresPartialTransferAndDrainsDeferredMutation()
        {
            using var fixture = new SessionSceneFixture(1);
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            var jobId = new OperationId(GuidFor(55305));
            const ulong generation = 12;
            try
            {
                await InvokeQuestCorrelationControlAsync(questOwner, new V2CorrelationControl(V2CorrelationControlKind.Started, jobId, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestCorrelationJob(questOwner) != null, "Quest did not reserve the partial correlation transfer.");

                var limits = new V2SchedulerLimits(inlineThresholdBytes: 128, bulkChunkBytes: 8, interactiveBurst: 1);
                int nextGuid = 55310;
                var senderScheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Desktop, limits: limits, guidFactory: () => GuidFor(nextGuid++));
                using var senderTransport = new V2PersistentTransport(senderScheduler, TimeSpan.FromHours(1), () => GuidFor(nextGuid++));
                var result = new SetCorrelationResult(jobId, generation, Enumerable.Repeat((byte)0xA5, 8192).ToArray());
                V2EnqueueResult enqueue = senderTransport.EnqueueMutation(result, 1UL, null, coalesciblePreview: false, operationId: jobId);
                Assert.That(enqueue.Accepted, Is.True, $"The result descriptor should be accepted (disposition={enqueue.Disposition}).");
                Assert.That(senderScheduler.TryGetNextTransmission(out V2TransmissionAttempt descriptorAttempt), Is.True);
                V2TransportRecord descriptor = CreateTransportRecord(senderTransport, descriptorAttempt);
                Assert.That(descriptor.Lane, Is.EqualTo(V2ScheduleLane.SceneControl));
                Assert.That(senderScheduler.TryGetNextTransmission(out V2TransmissionAttempt chunkAttempt), Is.True);
                V2TransportRecord firstChunk = CreateTransportRecord(senderTransport, chunkAttempt);
                Assert.That(firstChunk.Lane, Is.EqualTo(V2ScheduleLane.Bulk));

                object receiver = GetQuestSceneOperationBulkReceiver(questOwner);
                receiver.GetType().GetMethod("Begin").Invoke(receiver, new object[] { descriptor });
                Assert.That((bool)receiver.GetType().GetMethod("TryAppend").Invoke(receiver, new object[] { firstChunk, null }), Is.True);
                Assert.That((bool)receiver.GetType().GetProperty("IsActive").GetValue(receiver), Is.True);

                var color = new Color(0.15f, 0.75f, 0.35f, 1f);
                var ordinaryMutation = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), color.r, color.g, color.b, color.a);
                var deferredRecord = new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, new OperationId(GuidFor(55320)), new ReliableStreamId(GuidFor(55321)), 1, 1, V2OriginDevice.Desktop, V2ScheduleLane.Interactive, 1, payload: V2MutationPayloadCodec.Encode(ordinaryMutation), canonicalSequence: 2, mutation: ordinaryMutation);
                MethodInfo defer = questOwner.GetType().GetMethod("DeferOrderedRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                Type deferredType = questOwner.GetType().GetNestedType("DeferredRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                ConstructorInfo deferredConstructor = deferredType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(V2TransportRecord) }, null);
                defer.Invoke(questOwner, new[] { deferredConstructor.Invoke(new object[] { deferredRecord }) });
                Assert.That(GetDeferredRecords(questOwner), Has.Count.EqualTo(1));

                await InvokeQuestCorrelationControlAsync(questOwner, new V2CorrelationControl(V2CorrelationControlKind.Cancel, jobId, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetDeferredRecords(questOwner).Count == 0 && GetActiveQuestCorrelationJob(questOwner) == null, "Quest did not retire the partial correlation result and drain deferred scene work.");

                Assert.That((bool)receiver.GetType().GetProperty("IsActive").GetValue(receiver), Is.False, "Cancellation must discard the partial typed result.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
                Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(color), "Independent scene traffic must apply after the partial transfer is abandoned.");
                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(2UL));
                HashSet<Guid> abandoned = (HashSet<Guid>)questOwner.GetType().GetField("m_AbandonedCorrelationTransfers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(questOwner);
                Assert.That(abandoned.Contains(jobId.Value), Is.True);
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13QuestFilterFirst_RejectsCorrelationStartWithoutCancellingFilterOrReleasingScope()
        {
            using var fixture = new SessionSceneFixture(1);
            using var loading = new LoadingManagerFixture();
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            var filterJob = new OperationId(GuidFor(55330));
            var correlationJob = new OperationId(GuidFor(55331));
            const ulong generation = 21;
            try
            {
                await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Started, filterJob, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestSiteFilterJob(questOwner) != null && loading.LoadingCircle.gameObject.activeSelf, "Quest did not reserve the first site-filter job.");
                object activeFilter = GetActiveQuestSiteFilterJob(questOwner);
                bool wasFiltered = fixture.Sites[0].State.IsFiltered;
                int activeScopes = GetSensitiveActivityCount(fixture.Scene);

                await InvokeQuestCorrelationControlAsync(questOwner, new V2CorrelationControl(V2CorrelationControlKind.Started, correlationJob, generation), CancellationToken.None);
                V2CorrelationControl rejection = ReadNextQuestCorrelationControl(questOwner);

                Assert.That(rejection.Kind, Is.EqualTo(V2CorrelationControlKind.Failed));
                Assert.That(rejection.JobId, Is.EqualTo(correlationJob));
                Assert.That(rejection.Generation, Is.EqualTo(generation));
                Assert.That(rejection.FailureCode, Is.EqualTo("quest_scene_busy"));
                Assert.That(GetActiveQuestSiteFilterJob(questOwner), Is.SameAs(activeFilter));
                Assert.That(GetActiveQuestCorrelationJob(questOwner), Is.Null);
                Assert.That(IsActiveJobCancelled(activeFilter), Is.False);
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(activeScopes));
                Assert.That(fixture.Sites[0].State.IsFiltered, Is.EqualTo(wasFiltered));

                await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Cancel, filterJob, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestSiteFilterJob(questOwner) == null, "Quest did not release the original site-filter job.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13QuestCorrelationFirst_RejectsFilterStartWithoutCancellingCorrelationOrReleasingScope()
        {
            using var fixture = new SessionSceneFixture(1);
            using var loading = new LoadingManagerFixture();
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            var correlationJob = new OperationId(GuidFor(55332));
            var filterJob = new OperationId(GuidFor(55333));
            const ulong generation = 22;
            try
            {
                await InvokeQuestCorrelationControlAsync(questOwner, new V2CorrelationControl(V2CorrelationControlKind.Started, correlationJob, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestCorrelationJob(questOwner) != null && loading.LoadingCircle.gameObject.activeSelf, "Quest did not reserve the first correlation job.");
                object activeCorrelation = GetActiveQuestCorrelationJob(questOwner);
                bool wasFiltered = fixture.Sites[0].State.IsFiltered;
                int activeScopes = GetSensitiveActivityCount(fixture.Scene);

                await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Started, filterJob, generation), CancellationToken.None);
                V2SiteFilterControl rejection = ReadNextQuestSiteFilterControl(questOwner);

                Assert.That(rejection.Kind, Is.EqualTo(V2SiteFilterControlKind.Failed));
                Assert.That(rejection.JobId, Is.EqualTo(filterJob));
                Assert.That(rejection.Generation, Is.EqualTo(generation));
                Assert.That(rejection.FailureCode, Is.EqualTo("quest_scene_busy"));
                Assert.That(GetActiveQuestCorrelationJob(questOwner), Is.SameAs(activeCorrelation));
                Assert.That(GetActiveQuestSiteFilterJob(questOwner), Is.Null);
                Assert.That(IsActiveJobCancelled(activeCorrelation), Is.False);
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(activeScopes));
                Assert.That(fixture.Sites[0].State.IsFiltered, Is.EqualTo(wasFiltered));

                await InvokeQuestCorrelationControlAsync(questOwner, new V2CorrelationControl(V2CorrelationControlKind.Cancel, correlationJob, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestCorrelationJob(questOwner) == null, "Quest did not release the original correlation job.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13QuestOfflineFilterAndCorrelationFlags_RejectTheOppositeJob()
        {
            using var fixture = new SessionSceneFixture(1);
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            try
            {
                FieldInfo driverField = questOwner.GetType().GetField("m_Driver", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(driverField, Is.Not.Null);
                object driver = driverField.GetValue(questOwner);
                SetPrivateField(driver, "m_OfflineLocal", true);
                FieldInfo boundaryField = questOwner.GetType().GetField("m_Boundary", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(boundaryField, Is.Not.Null);
                var boundary = (V2SceneMutationBoundary)boundaryField.GetValue(questOwner);
                Assert.That(boundary.TryBeginSensitiveActivityOperation(out IDisposable filterScope), Is.True);
                try
                {
                    SetPrivateField(questOwner, "m_OfflineSiteFilterActive", true);
                    Assert.That(V2CorrelationRequestRouter.TryGetHandler(fixture.Scene, out Func<V2CorrelationRequest, CancellationToken, Task<bool>> correlationHandler), Is.True);
                    Exception correlationFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(correlationHandler(V2CorrelationRequest.Compute(externalLoadingIndicator: true), CancellationToken.None)));
                    Assert.That(correlationFailure, Is.InstanceOf<InvalidOperationException>());
                    StringAssert.Contains("already active", correlationFailure.Message);
                    Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(1));
                    Assert.That((bool)questOwner.GetType().GetField("m_OfflineSiteFilterActive", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(questOwner), Is.True);
                    Assert.That(fixture.Sites[0].State.IsFiltered, Is.True);
                }
                finally
                {
                    SetPrivateField(questOwner, "m_OfflineSiteFilterActive", false);
                    filterScope.Dispose();
                }

                Assert.That(boundary.TryBeginSensitiveActivityOperation(out IDisposable correlationScope), Is.True);
                try
                {
                    SetPrivateField(questOwner, "m_OfflineCorrelationActive", true);
                    Assert.That(V2SiteFilterRequestRouter.TryGetHandler(fixture.Scene, out Func<V2SiteFilterRequest, CancellationToken, Task<bool>> filterHandler), Is.True);
                    Exception filterFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(filterHandler(V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), CancellationToken.None)));
                    Assert.That(filterFailure, Is.InstanceOf<InvalidOperationException>());
                    StringAssert.Contains("already active", filterFailure.Message);
                    Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(1));
                    Assert.That((bool)questOwner.GetType().GetField("m_OfflineCorrelationActive", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(questOwner), Is.True);
                    Assert.That(fixture.Sites[0].State.IsFiltered, Is.True);
                }
                finally
                {
                    SetPrivateField(questOwner, "m_OfflineCorrelationActive", false);
                    correlationScope.Dispose();
                }
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13DesktopFilterFirst_RejectsCorrelationWithoutCancellingFilterOrReleasingScope()
        {
            using var fixture = new CorrelationSceneFixture(1);
            object desktopOwner = CreateDesktopSession(fixture.Scene);
            using var stop = new CancellationTokenSource();
            var filterJob = new OperationId(GuidFor(55334));
            try
            {
                Task<bool> filterTask = InvokeDesktopSiteFilterJobAsync(desktopOwner, filterJob, V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), stop.Token);
                await WaitUntilAsync(() => GetDesktopActiveSiteFilterJob(desktopOwner) != null, "Desktop did not reserve the first site-filter job.");
                object activeFilter = GetDesktopActiveSiteFilterJob(desktopOwner);
                bool wasFiltered = fixture.Sites[0].State.IsFiltered;
                int activeScopes = GetSensitiveActivityCount(fixture.Scene);

                Exception correlationFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(InvokeDesktopCorrelationJobAsync(desktopOwner, new OperationId(GuidFor(55335)), V2CorrelationRequest.Load(new byte[] { 0x01 }, externalLoadingIndicator: true), CancellationToken.None)));
                Assert.That(correlationFailure, Is.InstanceOf<InvalidOperationException>());
                Assert.That(GetDesktopActiveSiteFilterJob(desktopOwner), Is.SameAs(activeFilter));
                Assert.That(GetDesktopActiveCorrelationJob(desktopOwner), Is.Null);
                Assert.That(IsActiveJobCancelled(activeFilter), Is.False);
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(activeScopes));
                Assert.That(fixture.Sites[0].State.IsFiltered, Is.EqualTo(wasFiltered));
                Assert.That(fixture.Column.CorrelationBySitePair, Is.Empty);

                stop.Cancel();
                Exception originalFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(filterTask));
                Assert.That(originalFailure, Is.InstanceOf<OperationCanceledException>());
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
            }
            finally
            {
                if (!stop.IsCancellationRequested) stop.Cancel();
                if (desktopOwner is IDisposable disposable) disposable.Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13DesktopCorrelationFirst_RejectsFilterWithoutCancellingCorrelationOrReleasingScope()
        {
            using var fixture = new CorrelationSceneFixture(1);
            object desktopOwner = CreateDesktopSession(fixture.Scene);
            using var stop = new CancellationTokenSource();
            var correlationJob = new OperationId(GuidFor(55336));
            try
            {
                byte[] resultBytes = fixture.CreateResultBytes();
                Task<bool> correlationTask = InvokeDesktopCorrelationJobAsync(desktopOwner, correlationJob, V2CorrelationRequest.Load(resultBytes, externalLoadingIndicator: true), stop.Token);
                await WaitUntilAsync(() => GetDesktopActiveCorrelationJob(desktopOwner) != null, "Desktop did not reserve the first correlation job.");
                object activeCorrelation = GetDesktopActiveCorrelationJob(desktopOwner);
                bool wasFiltered = fixture.Sites[0].State.IsFiltered;
                float appliedValue = fixture.Column.CorrelationBySitePair[fixture.Sites[0]][fixture.Sites[0]];
                CorrelationProvenance appliedProvenance = fixture.Column.CorrelationProvenance;
                int activeScopes = GetSensitiveActivityCount(fixture.Scene);

                Exception filterFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(InvokeDesktopSiteFilterJobAsync(desktopOwner, new OperationId(GuidFor(55337)), V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), CancellationToken.None)));
                Assert.That(filterFailure, Is.InstanceOf<InvalidOperationException>());
                Assert.That(GetDesktopActiveCorrelationJob(desktopOwner), Is.SameAs(activeCorrelation));
                Assert.That(GetDesktopActiveSiteFilterJob(desktopOwner), Is.Null);
                Assert.That(IsActiveJobCancelled(activeCorrelation), Is.False);
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(activeScopes));
                Assert.That(fixture.Sites[0].State.IsFiltered, Is.EqualTo(wasFiltered));
                Assert.That(fixture.Column.CorrelationBySitePair[fixture.Sites[0]][fixture.Sites[0]], Is.EqualTo(appliedValue));
                Assert.That(fixture.Column.CorrelationProvenance.Equals(appliedProvenance), Is.True);

                stop.Cancel();
                Exception originalFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(correlationTask));
                Assert.That(originalFailure, Is.InstanceOf<OperationCanceledException>());
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
            }
            finally
            {
                if (!stop.IsCancellationRequested) stop.Cancel();
                if (desktopOwner is IDisposable disposable) disposable.Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13DesktopCorrelationFailure_InvalidatesGenerationAndSendsFailedControl()
        {
            using var fixture = new SessionSceneFixture(1);
            Type desktopOwnerType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(desktopOwnerType, Is.Not.Null);
            Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
            ConstructorInfo constructor = desktopOwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
            Assert.That(constructor, Is.Not.Null);
            object desktopOwner = constructor.Invoke(new object[] { fixture.Scene, Session.Value.ToString(), Incarnation.Value.ToString(), null });

            try
            {
                SetPrivateField(desktopOwner, "m_State", Enum.Parse(desktopOwnerType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, "Live"));
                var jobId = new OperationId(GuidFor(55302));
                MethodInfo runCorrelationJob = desktopOwnerType.GetMethod("RunCorrelationJobAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(runCorrelationJob, Is.Not.Null);
                Task<bool> operation = (Task<bool>)runCorrelationJob.Invoke(desktopOwner, new object[] { jobId, V2CorrelationRequest.Load(new byte[] { 0x01 }, externalLoadingIndicator: true), CancellationToken.None, false });
                Exception failure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(operation));

                Assert.That(failure, Is.InstanceOf<InvalidDataException>());
                Assert.That(GetDesktopActiveCorrelationJob(desktopOwner), Is.Null);
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
                var registry = (V2JobGenerationRegistry)desktopOwnerType.GetField("m_CorrelationGenerations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                var controls = ReadDesktopCorrelationControls(desktopOwner);
                Assert.That(controls.Select(control => control.Kind), Is.EqualTo(new[] { V2CorrelationControlKind.Started, V2CorrelationControlKind.Failed }));
                Assert.That(controls[1].FailureCode, Is.EqualTo("desktop_correlation_failed"));
                Assert.That(registry.TrackedScopeCount, Is.EqualTo(1));
                object states = typeof(V2JobGenerationRegistry).GetField("m_States", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(registry);
                object state = ((System.Collections.IDictionary)states).Values.Cast<object>().Single();
                Assert.That((bool)state.GetType().GetField("Cancelled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(state), Is.True, "A failed load must invalidate its generation before releasing the scene scope.");
            }
            finally
            {
                if (desktopOwner is IDisposable disposable) disposable.Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task T13QuestCorrelationResultWithStaleGeneration_IsDroppedBeforeApply()
        {
            using var fixture = new SessionSceneFixture(1);
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            var jobId = new OperationId(GuidFor(55303));
            var result = new SetCorrelationResult(jobId, 1, new byte[] { 0x01 });
            var record = new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, jobId, new ReliableStreamId(GuidFor(55304)), 1, 1, V2OriginDevice.Desktop, V2ScheduleLane.SceneControl, 1, payload: V2MutationPayloadCodec.Encode(result), canonicalSequence: 1, mutation: result);
            try
            {
                MethodInfo apply = questOwner.GetType().GetMethod("ApplyCorrelationResultAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(apply, Is.Not.Null);
                object telemetryDefault = Activator.CreateInstance(apply.GetParameters()[2].ParameterType);
                object telemetryLastDefault = Activator.CreateInstance(apply.GetParameters()[3].ParameterType);
                await (Task)apply.Invoke(questOwner, new[] { record, result, telemetryDefault, telemetryLastDefault, CancellationToken.None });

                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(1UL));
                Assert.That(GetActiveQuestCorrelationJob(questOwner), Is.Null);
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
                Assert.That(fixture.Sites[0].State.IsFiltered, Is.True, "A result from a retired generation must not reach scene application.");
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestRemoteSiteFilter_SurvivesTransientConnectionStopUntilTerminalControl()
        {
            using var fixture = new SessionSceneFixture(1);
            using var loading = new LoadingManagerFixture();
            using var connectionStop = new CancellationTokenSource();
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            var jobId = new OperationId(GuidFor(55203));
            const ulong generation = 8;

            try
            {
                await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Started, jobId, generation), connectionStop.Token);
                await WaitUntilAsync(() => loading.LoadingCircle.gameObject.activeSelf, "The Quest loading visual did not open for the remote site-filter job.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(1));

                connectionStop.Cancel();
                await Task.Delay(30);
                Assert.That(GetQuestActiveJobId(questOwner), Is.EqualTo(jobId), "Ending one connection must not retire a job owned by the still-live replica session.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.EqualTo(1), "The activity scope must remain reserved during reconnect grace.");
                Assert.That(loading.LoadingCircle.gameObject.activeSelf, Is.True, "A transient connection stop must not cancel the Quest loading visual.");

                await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Cancel, jobId, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestSiteFilterJob(questOwner) == null, "Quest did not release the job after its terminal Cancel control.");
                Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero);
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestFailedControlDuringDesktopEvaluation_InvalidatesGenerationBeforeMaskPublication()
        {
            using var desktopFixture = new SessionSceneFixture(4096);
            foreach (HBP.Core.Object3D.Site site in desktopFixture.Sites)
                site.State.IsFiltered = false;
            Type desktopOwnerType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(desktopOwnerType, Is.Not.Null);
            Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
            ConstructorInfo constructor = desktopOwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
            Assert.That(constructor, Is.Not.Null);
            object desktopOwner = constructor.Invoke(new object[] { desktopFixture.Scene, Session.Value.ToString(), Incarnation.Value.ToString(), null });
            Task<bool> desktopOperation = null;

            try
            {
                SetPrivateField(desktopOwner, "m_State", Enum.Parse(desktopOwnerType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic).FieldType, "Live"));
                var jobId = new OperationId(GuidFor(55204));
                MethodInfo runDesktopJob = desktopOwnerType.GetMethod("RunSiteFilterJobAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(runDesktopJob, Is.Not.Null);
                desktopOperation = (Task<bool>)runDesktopJob.Invoke(desktopOwner, new object[] { jobId, V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), CancellationToken.None, false });
                await WaitUntilAsync(() => GetDesktopActiveSiteFilterJob(desktopOwner) != null, "Desktop did not reserve its site-filter generation.");
                object active = GetDesktopActiveSiteFilterJob(desktopOwner);
                object identity = active.GetType().GetProperty("Identity").GetValue(active);
                ulong generation = (ulong)identity.GetType().GetProperty("Generation").GetValue(identity);
                var attempt = (V2JobAttempt)active.GetType().GetProperty("Attempt").GetValue(active);
                var registry = (V2JobGenerationRegistry)desktopOwnerType.GetField("m_SiteFilterGenerations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                Assert.That(registry.IsCurrent(attempt), Is.True);

                await InvokeDesktopSiteFilterControlAsync(desktopOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Failed, jobId, generation, failureCode: "quest_loading_failed"), CancellationToken.None);
                Exception failure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(desktopOperation));

                Assert.That(failure, Is.InstanceOf<IOException>());
                Assert.That(registry.IsCurrent(attempt), Is.False, "A Quest failure must invalidate the Desktop generation before evaluation can publish.");
                Assert.That(desktopFixture.Sites.All(site => !site.State.IsFiltered), Is.True, "The initially excluded mask must remain unchanged when Quest rejects the active generation.");
                Assert.That(GetDesktopActiveSiteFilterJob(desktopOwner), Is.Null);
                Assert.That(GetSensitiveActivityCount(desktopFixture.Scene), Is.Zero);
            }
            finally
            {
                if (desktopOwner is IDisposable desktopDisposable) desktopDisposable.Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestSiteFilterCancellation_RetiresPartialTransferAndDrainsDeferredMutationInOrder()
        {
            using var fixture = new SessionSceneFixture(1024);
            object questOwner = CreateQuestSession(fixture.Scene, CreatePreparedBinding());
            var jobId = new OperationId(GuidFor(55202));
            const ulong generation = 7;
            try
            {
                await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Started, jobId, generation), CancellationToken.None);
                await WaitUntilAsync(() => GetActiveQuestSiteFilterJob(questOwner) != null, "Quest did not reserve the remote site-filter scope.");

                var limits = new V2SchedulerLimits(inlineThresholdBytes: 128, bulkChunkBytes: 8, interactiveBurst: 1);
                int guid = 55210;
                var senderScheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Desktop, limits: limits, guidFactory: () => GuidFor(guid++));
                var senderTransport = new V2PersistentTransport(senderScheduler, TimeSpan.FromHours(1), () => GuidFor(guid++));
                try
                {
                    using var senderBoundary = new V2SceneMutationBoundary(fixture.Scene, V2OriginDevice.Desktop);
                    SetSiteFilterResult result = senderBoundary.CreateSiteFilterResult(jobId, generation, Enumerable.Repeat(false, fixture.Sites.Count).ToArray());
                    V2EnqueueResult filterEnqueue = senderTransport.EnqueueMutation(result, 1UL, null, coalesciblePreview: false, operationId: jobId);
                    Assert.That(filterEnqueue.Accepted, Is.True, $"The filter result should be accepted by the Desktop scheduler (disposition={filterEnqueue.Disposition}, bodyBytes={V2MutationPayloadCodec.Encode(result).Length}).");
                    Assert.That(senderScheduler.TryGetNextTransmission(out V2TransmissionAttempt descriptorAttempt), Is.True, "The scheduler should produce a bulk descriptor for the filter result.");
                    V2TransportRecord descriptor = CreateTransportRecord(senderTransport, descriptorAttempt);
                    Assert.That(descriptor.Lane, Is.EqualTo(V2ScheduleLane.SceneControl));
                    Assert.That(senderScheduler.TryGetNextTransmission(out V2TransmissionAttempt chunkAttempt), Is.True, "The bulk descriptor should be followed by a first result chunk.");
                    V2TransportRecord firstChunk = CreateTransportRecord(senderTransport, chunkAttempt);
                    Assert.That(firstChunk.Lane, Is.EqualTo(V2ScheduleLane.Bulk));

                    object receiver = GetQuestSceneOperationBulkReceiver(questOwner);
                    receiver.GetType().GetMethod("Begin").Invoke(receiver, new object[] { descriptor });
                    object completed = null;
                    Assert.That((bool)receiver.GetType().GetMethod("TryAppend").Invoke(receiver, new object[] { firstChunk, completed }), Is.True, "The receiver should append the first non-final chunk.");
                    Assert.That((bool)receiver.GetType().GetProperty("IsActive").GetValue(receiver), Is.True, "The receiver should hold an incomplete transfer.");

                    var color = new Color(0.8f, 0.2f, 0.4f, 1f);
                    var ordinaryMutation = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), color.r, color.g, color.b, color.a);
                    var deferredRecord = new V2TransportRecord(V2TransportMessageKind.Application, Session, Scene, Incarnation, new OperationId(GuidFor(55211)), new ReliableStreamId(GuidFor(55212)), 1, 1, V2OriginDevice.Desktop, V2ScheduleLane.Interactive, 1, payload: V2MutationPayloadCodec.Encode(ordinaryMutation), canonicalSequence: 2, mutation: ordinaryMutation);
                    MethodInfo defer = questOwner.GetType().GetMethod("DeferOrderedRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                    Type deferredType = questOwner.GetType().GetNestedType("DeferredRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                    ConstructorInfo deferredConstructor = deferredType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(V2TransportRecord) }, null);
                    Assert.That(defer, Is.Not.Null);
                    defer.Invoke(questOwner, new[] { deferredConstructor.Invoke(new object[] { deferredRecord }) });
                    Assert.That(GetDeferredRecords(questOwner), Has.Count.EqualTo(1));
                    Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(SiteState.DefaultColor), "The unrelated mutation must remain deferred until the partial result is abandoned.");

                    await InvokeQuestSiteFilterControlAsync(questOwner, new V2SiteFilterControl(V2SiteFilterControlKind.Cancel, jobId, generation), CancellationToken.None);
                    await WaitUntilAsync(() => GetDeferredRecords(questOwner).Count == 0 && GetActiveQuestSiteFilterJob(questOwner) == null, "Quest did not finish transfer retirement and deferred-record draining.");

                    Assert.That((bool)receiver.GetType().GetProperty("IsActive").GetValue(receiver), Is.False, "Cancellation must retire the partial receiver.");
                    Assert.That(GetSensitiveActivityCount(fixture.Scene), Is.Zero, "Cancellation must release the Quest activity scope.");
                    Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(color), "The unrelated deferred mutation must be applied after retiring the filter transfer.");
                    Assert.That(GetDriverCanonicalWatermark(questOwner), Is.EqualTo(2UL));
                }
                finally
                {
                    senderTransport.Dispose();
                }
            }
            finally
            {
                ((IDisposable)questOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestSiteFilterDelayedStartedForCancelledRequest_IsIgnoredAndCannotSupersedeLaterRequest()
        {
            using var desktopFixture = new SessionSceneFixture(1);
            using var questFixture = new SessionSceneFixture(1);
            PreparedSceneDeliveryBinding binding = CreatePreparedBinding();
            object questOwner = CreateQuestSession(questFixture.Scene, binding);
            var desktopPeer = CreateTransport(V2OriginDevice.Desktop, 55230);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var connectionStop = new CancellationTokenSource();
            using var requestACancellation = new CancellationTokenSource();
            Task<Exception> questRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, pair.Server.GetStream(), connectionStop.Token));
            Task<Exception> desktopRun = CaptureRunAsync(desktopPeer, pair.Client.GetStream(), connectionStop.Token);
            Task<bool> requestA = null;
            Task<bool> requestB = null;

            try
            {
                await WaitUntilAsync(() => (V2PersistentTransportState)questOwner.GetType().GetProperty("TransportState").GetValue(questOwner) == V2PersistentTransportState.Connected && desktopPeer.State == V2PersistentTransportState.Connected, "The Quest session did not finish the authenticated loopback handshake.");
                Assert.That(V2SiteFilterRequestRouter.TryGetHandler(questFixture.Scene, out Func<V2SiteFilterRequest, CancellationToken, Task<bool>> requestHandler), Is.True);
                questFixture.Sites[0].State.IsFiltered = true;

                requestA = requestHandler(V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), requestACancellation.Token);
                V2SiteFilterControl requestControlA = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(requestControlA.Kind, Is.EqualTo(V2SiteFilterControlKind.Request));
                OperationId jobA = requestControlA.JobId;

                requestACancellation.Cancel();
                Exception cancellationFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(requestA));
                Assert.That(cancellationFailure, Is.InstanceOf<OperationCanceledException>());
                Assert.That(GetActiveQuestSiteFilterJob(questOwner), Is.Null);
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.Zero);

                V2SiteFilterControl cancelA = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(cancelA.Kind, Is.EqualTo(V2SiteFilterControlKind.Cancel));
                Assert.That(cancelA.JobId, Is.EqualTo(jobA));
                Assert.That(cancelA.Generation, Is.Zero);

                const ulong generationA = 1;
                Assert.That(desktopPeer.EnqueueSessionControl(V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(V2SiteFilterControlKind.Started, jobA, generationA)), V2DeliveryReliability.Reliable).Accepted, Is.True);
                await WaitUntilAsync(() => GetQuestLastDesktopSiteFilterGeneration(questOwner) == generationA, "Quest did not process the delayed Started for cancelled request A.");
                Assert.That(GetActiveQuestSiteFilterJob(questOwner), Is.Null, "A delayed Started for a retired request must not recreate its job.");
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.Zero, "A retired request must not reserve a replacement activity scope.");
                using (var desktopBoundary = new V2SceneMutationBoundary(desktopFixture.Scene, V2OriginDevice.Desktop))
                {
                    SetSiteFilterResult lateA = desktopBoundary.CreateSiteFilterResult(jobA, generationA, new[] { false });
                    Assert.That(desktopPeer.EnqueueMutation(lateA, 1UL, null, coalesciblePreview: false, operationId: jobA).Accepted, Is.True);
                }

                await WaitUntilAsync(() => GetDriverCanonicalWatermark(questOwner) == 1UL, "Quest did not consume the inline result for retired request A.");
                Assert.That(questFixture.Sites[0].State.IsFiltered, Is.True, "A delayed Started must not recreate the cancelled job or apply its inline result.");
                Assert.That(GetActiveQuestSiteFilterJob(questOwner), Is.Null);
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.Zero);
                using (var noReadyForCancelledA = new CancellationTokenSource(TimeSpan.FromMilliseconds(250)))
                    Assert.That(await ReadIncomingOrNullAsync(desktopPeer, noReadyForCancelledA.Token), Is.Null, "A retired generation must not receive Ready.");

                requestB = requestHandler(V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), CancellationToken.None);
                V2SiteFilterControl requestControlB = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(requestControlB.Kind, Is.EqualTo(V2SiteFilterControlKind.Request));
                OperationId jobB = requestControlB.JobId;
                Assert.That(jobB, Is.Not.EqualTo(jobA));

                const ulong generationB = 2;
                Assert.That(desktopPeer.EnqueueSessionControl(V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(V2SiteFilterControlKind.Started, jobB, generationB)), V2DeliveryReliability.Reliable).Accepted, Is.True);
                await WaitUntilAsync(() => GetQuestActiveJobId(questOwner)?.Equals(jobB) == true && GetQuestActiveJobGeneration(questOwner) == generationB, "Quest did not register later request B.");
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.EqualTo(1));

                Assert.That(desktopPeer.EnqueueSessionControl(V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(V2SiteFilterControlKind.Started, jobA, generationA)), V2DeliveryReliability.Reliable).Accepted, Is.True);
                using (var desktopBoundary = new V2SceneMutationBoundary(desktopFixture.Scene, V2OriginDevice.Desktop))
                {
                    SetSiteFilterResult lateA = desktopBoundary.CreateSiteFilterResult(jobA, generationA, new[] { false });
                    Assert.That(desktopPeer.EnqueueMutation(lateA, 2UL, null, coalesciblePreview: false, operationId: jobA).Accepted, Is.True);
                }

                await WaitUntilAsync(() => GetDriverCanonicalWatermark(questOwner) == 2UL, "Quest did not consume the second late A result.");
                Assert.That(GetQuestActiveJobId(questOwner), Is.EqualTo(jobB), "A retired job must not supersede the later active request B.");
                Assert.That(GetQuestActiveJobGeneration(questOwner), Is.EqualTo(generationB));
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.EqualTo(1));
                Assert.That(questFixture.Sites[0].State.IsFiltered, Is.True, "Late A must not change the mask while B is active.");
                using (var noReadyForLateA = new CancellationTokenSource(TimeSpan.FromMilliseconds(250)))
                    Assert.That(await ReadIncomingOrNullAsync(desktopPeer, noReadyForLateA.Token), Is.Null, "A retired generation must never receive Ready, including while B is active.");

                using (var desktopBoundary = new V2SceneMutationBoundary(desktopFixture.Scene, V2OriginDevice.Desktop))
                {
                    SetSiteFilterResult resultB = desktopBoundary.CreateSiteFilterResult(jobB, generationB, new[] { false });
                    Assert.That(desktopPeer.EnqueueMutation(resultB, 3UL, null, coalesciblePreview: false, operationId: jobB).Accepted, Is.True);
                }

                V2SiteFilterControl readyB = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(readyB.Kind, Is.EqualTo(V2SiteFilterControlKind.Ready));
                Assert.That(readyB.JobId, Is.EqualTo(jobB));
                Assert.That(readyB.Generation, Is.EqualTo(generationB));
                Assert.That(await AwaitGuardValueAsync(requestB), Is.True);
                Assert.That(questFixture.Sites[0].State.IsFiltered, Is.False);
                Assert.That(GetActiveQuestSiteFilterJob(questOwner), Is.Null);
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.Zero);
            }
            finally
            {
                connectionStop.Cancel();
                pair.Close();
                desktopPeer.Dispose();
                ((IDisposable)questOwner).Dispose();
                await AwaitGuardAsync(Task.WhenAll(questRun, desktopRun));
                if (requestB != null && !requestB.IsCompleted)
                    await AwaitGuardAsync(CaptureTaskExceptionAsync(requestB));
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task QuestSiteFilterSupersession_ReplacesPendingRequestAndRejectsLateResultFromA()
        {
            using var desktopFixture = new SessionSceneFixture(1);
            using var questFixture = new SessionSceneFixture(1);
            PreparedSceneDeliveryBinding binding = CreatePreparedBinding();
            object questOwner = CreateQuestSession(questFixture.Scene, binding);
            var desktopPeer = CreateTransport(V2OriginDevice.Desktop, 55220);
            using var pair = await LoopbackPeerPair.ConnectAsync();
            using var stop = new CancellationTokenSource();
            Task<Exception> questRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, pair.Server.GetStream(), stop.Token));
            Task<Exception> desktopRun = CaptureRunAsync(desktopPeer, pair.Client.GetStream(), stop.Token);

            try
            {
                await WaitUntilAsync(() => (V2PersistentTransportState)questOwner.GetType().GetProperty("TransportState").GetValue(questOwner) == V2PersistentTransportState.Connected && desktopPeer.State == V2PersistentTransportState.Connected, "The Quest session did not finish the authenticated loopback handshake.");
                Assert.That(V2SiteFilterRequestRouter.TryGetHandler(questFixture.Scene, out Func<V2SiteFilterRequest, CancellationToken, Task<bool>> requestHandler), Is.True);

                Task<bool> requestA = requestHandler(V2SiteFilterRequest.ResetAll(externalLoadingIndicator: true), CancellationToken.None);
                V2SiteFilterControl requestControl = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(requestControl.Kind, Is.EqualTo(V2SiteFilterControlKind.Request));
                OperationId jobA = requestControl.JobId;

                var jobB = new OperationId(GuidFor(55221));
                Assert.That(desktopPeer.EnqueueSessionControl(V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(V2SiteFilterControlKind.Started, jobB, 1)), V2DeliveryReliability.Reliable).Accepted, Is.True);
                await WaitUntilAsync(() => GetQuestActiveJobId(questOwner)?.Equals(jobB) == true && GetQuestActiveJobGeneration(questOwner) == 1, "Quest did not register the replacement Desktop generation B.");
                Exception requestFailure = await AwaitGuardValueAsync(CaptureTaskExceptionAsync(requestA));
                Assert.That(requestFailure, Is.InstanceOf<OperationCanceledException>(), "The original Quest request must complete as superseded.");
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.EqualTo(1), "Supersession must reserve a fresh scope for job B after releasing A.");

                V2SiteFilterControl cancelA = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(cancelA.Kind, Is.EqualTo(V2SiteFilterControlKind.Cancel));
                Assert.That(cancelA.JobId, Is.EqualTo(jobA));
                Assert.That(cancelA.Generation, Is.Zero);

                using (var desktopBoundary = new V2SceneMutationBoundary(desktopFixture.Scene, V2OriginDevice.Desktop))
                {
                    SetSiteFilterResult resultB = desktopBoundary.CreateSiteFilterResult(jobB, 1, new[] { false });
                    Assert.That(desktopPeer.EnqueueMutation(resultB, 1UL, null, coalesciblePreview: false, operationId: jobB).Accepted, Is.True);
                }

                V2SiteFilterControl readyB = ReadSiteFilterControl(await ReadIncomingAsync(desktopPeer));
                Assert.That(readyB.Kind, Is.EqualTo(V2SiteFilterControlKind.Ready));
                Assert.That(readyB.JobId, Is.EqualTo(jobB));
                Assert.That(readyB.Generation, Is.EqualTo(1UL), "Quest must acknowledge the exact generation whose result it applied.");
                await WaitUntilAsync(() => GetActiveQuestSiteFilterJob(questOwner) == null, "Quest did not release job B after acknowledging its result.");
                Assert.That(questFixture.Sites[0].State.IsFiltered, Is.False);
                Assert.That(GetSensitiveActivityCount(questFixture.Scene), Is.Zero);

                using (var desktopBoundary = new V2SceneMutationBoundary(desktopFixture.Scene, V2OriginDevice.Desktop))
                {
                    SetSiteFilterResult lateA = desktopBoundary.CreateSiteFilterResult(jobA, 1, new[] { true });
                    Assert.That(desktopPeer.EnqueueMutation(lateA, 2UL, null, coalesciblePreview: false, operationId: jobA).Accepted, Is.True);
                }

                await WaitUntilAsync(() => GetDriverCanonicalWatermark(questOwner) == 2UL, "Quest did not observe the late canonical A result.");
                Assert.That(questFixture.Sites[0].State.IsFiltered, Is.False, "A late A result must not overwrite B's completed mask.");
                using var noUnexpectedAck = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
                Assert.That(await ReadIncomingOrNullAsync(desktopPeer, noUnexpectedAck.Token), Is.Null, "A stale A result must not receive Ready.");
            }
            finally
            {
                stop.Cancel();
                pair.Close();
                desktopPeer.Dispose();
                ((IDisposable)questOwner).Dispose();
                await AwaitGuardAsync(Task.WhenAll(questRun, desktopRun));
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void MissingPreparedResourceAndTopologyRejectOnlyThatQuestOperation()
        {
            using var fixture = new SessionSceneFixture(1);
            using var boundary = new V2SceneMutationBoundary(fixture.Scene, V2OriginDevice.Quest);
            boundary.BindPreparedResources(CreatePreparedBinding());
            var scheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(Scene, Incarnation, boundary, scheduler);
            var proposed = new List<V2Mutation>();
            boundary.MutationProposed += (_, mutation, _) => proposed.Add(mutation);

            Assert.Throws<InvalidDataException>(() => driver.ApplyOptimistic(new SetMeshDisplay(new ResourceId("missing-mesh"), V2MeshPart.Both, V2SurfaceRepresentation.Anatomical), new OperationId(GuidFor(55121))));
            Assert.Throws<InvalidOperationException>(() => driver.ApplyOptimistic(new ApplyTriangleMask(new[]
            {
                new V2TriangleMask(new TopologyId("unavailable-topology:complete"), 1, new[] { 0 }),
                new V2TriangleMask(new TopologyId("unavailable-topology:simplified"), 1, new[] { 0 })
            }), new OperationId(GuidFor(55122))));

            Assert.That(proposed, Is.Empty, "A resource or topology preflight failure must not publish or partially apply the operation.");
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            var color = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), 0.6f, 0.3f, 0.8f, 1f);
            OperationId colorOperation = new OperationId(GuidFor(55123));
            Assert.That(driver.ApplyOptimistic(color, colorOperation), Is.Not.Null, "A later valid operation must remain admissible.");
            Assert.That(proposed, Has.Count.EqualTo(1));
            Assert.That(driver.ReceiveRejection(colorOperation, "prepared_resource_unavailable"), Is.True);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(fixture.Sites[0].State.Color, Is.EqualTo(SiteState.DefaultColor), "Only the rejected operation is rolled back.");
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void DesktopReplicaSession_CoalescesCanonicalMutationsBeforeWireCommit()
        {
            using var fixture = new SessionSceneFixture(1);
            using var questFixture = new SessionSceneFixture(1);
            using var questBoundary = new V2SceneMutationBoundary(questFixture.Scene, V2OriginDevice.Quest);
            var questScheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest);
            using var questDriver = new V2QuestMutationDriver(Scene, Incarnation, questBoundary, questScheduler);
            Type desktopOwnerType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(desktopOwnerType, Is.Not.Null);
            Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
            ConstructorInfo constructor = desktopOwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
            Assert.That(constructor, Is.Not.Null);
            object desktopOwner = constructor.Invoke(new object[] { fixture.Scene, Session.Value.ToString(), Incarnation.Value.ToString(), null });

            try
            {
                FieldInfo state = desktopOwnerType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(state, Is.Not.Null);
                state.SetValue(desktopOwner, Enum.Parse(state.FieldType, "Live"));

                var canonical = new List<V2CanonicalMutation>();
                var authority = (V2DesktopMutationAuthority)desktopOwnerType.GetField("m_Authority", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                authority.CanonicalReady += canonical.Add;

                fixture.Sites[0].State.Color = new Color(0.1f, 0.2f, 0.3f, 1f);
                fixture.Sites[0].State.Color = new Color(0.8f, 0.7f, 0.6f, 1f);

                var scheduler = (V2OutgoingScheduler)desktopOwnerType.GetField("m_Scheduler", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                Assert.That(canonical, Has.Count.EqualTo(2));
                Assert.That(scheduler.SnapshotMetrics().PendingSceneRecords, Is.EqualTo(1));
                Assert.That(scheduler.SnapshotMetrics().CoalescedPreviewCount, Is.EqualTo(1));
                Assert.That(scheduler.TryGetNextTransmission(out V2TransmissionAttempt transmission), Is.True);
                if (transmission.Frame.Lane == V2ScheduleLane.SessionControl)
                {
                    Assert.That(V2RetentionProgress.TryDecode(transmission.Frame.GetPayloadCopy(), out _), Is.True);
                    Assert.That(scheduler.TryGetNextTransmission(out transmission), Is.True);
                }

                Assert.That(transmission.Frame.OperationId, Is.EqualTo(canonical[1].OperationId));
                Assert.That(transmission.Frame.CanonicalSequence, Is.EqualTo(canonical[1].CanonicalSequence));
                Assert.That(transmission.Frame.ReliableFrameSequence, Is.EqualTo(1UL));
                Assert.That(transmission.Frame.OriginSequence, Is.EqualTo(1UL));
                SetSiteColor sent = (SetSiteColor)V2MutationPayloadCodec.Decode(transmission.Frame.GetPayloadCopy());
                Assert.That(sent.Red, Is.EqualTo(0.8f));
                Assert.That(sent.Green, Is.EqualTo(0.7f));
                Assert.That(sent.Blue, Is.EqualTo(0.6f));
                Assert.That(questDriver.ReceiveCanonical(canonical[1]), Is.True);
                Assert.That(questFixture.Sites[0].State.Color, Is.EqualTo(fixture.Sites[0].State.Color));
            }
            finally
            {
                ((IDisposable)desktopOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void DesktopReplicaSession_PreservesQuestProposalEchoBeforeLaterDesktopPreview()
        {
            using var fixture = new SessionSceneFixture(1);
            using var questFixture = new SessionSceneFixture(1);
            using var questBoundary = new V2SceneMutationBoundary(questFixture.Scene, V2OriginDevice.Quest);
            var questScheduler = new V2OutgoingScheduler(Session, Scene, Incarnation, V2OriginDevice.Quest);
            using var questDriver = new V2QuestMutationDriver(Scene, Incarnation, questBoundary, questScheduler);
            Type desktopOwnerType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(desktopOwnerType, Is.Not.Null);
            Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
            ConstructorInfo constructor = desktopOwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
            Assert.That(constructor, Is.Not.Null);
            object desktopOwner = constructor.Invoke(new object[] { fixture.Scene, Session.Value.ToString(), Incarnation.Value.ToString(), null });

            try
            {
                FieldInfo state = desktopOwnerType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(state, Is.Not.Null);
                state.SetValue(desktopOwner, Enum.Parse(state.FieldType, "Live"));
                var authority = (V2DesktopMutationAuthority)desktopOwnerType.GetField("m_Authority", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                var scheduler = (V2OutgoingScheduler)desktopOwnerType.GetField("m_Scheduler", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                var questEdit = new SetSiteColor(new ColumnId(fixture.ColumnId), new SiteId(fixture.SiteIds[0]), 0.8f, 0.7f, 0.6f, 1f);
                V2QuestMutationProposal proposal = questDriver.ApplyOptimistic(questEdit, new OperationId(GuidFor(55110)));
                V2DesktopProposalResult accepted = authority.AcceptQuestProposal(proposal);
                Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));

                fixture.Sites[0].State.Color = new Color(0.1f, 0.2f, 0.3f, 1f);
                Assert.That(scheduler.SnapshotMetrics().PendingSceneRecords, Is.EqualTo(2));
                Assert.That(scheduler.TryGetNextTransmission(out V2TransmissionAttempt echo), Is.True);
                Assert.That(echo.Frame.OperationId, Is.EqualTo(proposal.OperationId));
                Assert.That(questDriver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
                Assert.That(questDriver.PendingProposalCount, Is.Zero);
                Assert.That(scheduler.TryGetNextTransmission(out V2TransmissionAttempt desktopPreview), Is.True);
                Assert.That(desktopPreview.Frame.CanonicalSequence, Is.EqualTo(2UL));
            }
            finally
            {
                ((IDisposable)desktopOwner).Dispose();
            }
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public async Task ExistingPreparedCut_PreservesStableIdAndSynchronizesDiscreteAndFinalContinuousEdits()
        {
            using var desktopFixture = new SessionSceneFixture(1);
            using var questFixture = new SessionSceneFixture(1);
            var desktopCut = new HBP.Core.Object3D.Cut
            {
                ID = "desktop-runtime-cut",
                Orientation = HBP.Core.Enums.CutOrientation.Custom,
                Flip = false,
                NumberOfCuts = 321,
                Position = 0.45f
            };
            var questCut = new HBP.Core.Object3D.Cut
            {
                ID = "quest-runtime-cut",
                Orientation = HBP.Core.Enums.CutOrientation.Custom,
                Flip = false,
                NumberOfCuts = 321,
                Position = 0.4f
            };
            Vector3 normal = new Vector3(BitConverter.Int32BitsToSingle(int.MinValue), 0.5f, 0.7f);
            desktopCut.Normal = normal;
            questCut.Normal = normal;
            desktopFixture.Scene.Cuts.Add(desktopCut);
            questFixture.Scene.Cuts.Add(questCut);
            SetPrivateField(desktopFixture.Scene, "m_MeshManager", desktopFixture.Root.AddComponent<MeshManager>());
            SetPrivateField(questFixture.Scene, "m_MeshManager", questFixture.Root.AddComponent<MeshManager>());

            Assert.That(desktopCut.ID, Is.Not.EqualTo(questCut.ID), "The prepared scene begins with independently generated runtime identities.");
            var capturedCut = new HBP.Core.Data.Cut(desktopCut.ID, desktopCut.Normal, desktopCut.Orientation, desktopCut.Flip, desktopCut.Position);
            HBP.Core.Data.Cut restoredCut = JsonConvert.DeserializeObject<HBP.Core.Data.Cut>(JsonConvert.SerializeObject(capturedCut));
            questCut.ID = restoredCut.ID;
            questCut.Normal = restoredCut.Normal.ToVector3();
            questCut.Orientation = restoredCut.Orientation;
            questCut.Flip = restoredCut.Flip;
            questCut.Position = restoredCut.Position;
            Assert.That(restoredCut.ID, Is.EqualTo(desktopCut.ID), "The prepared-scene definition must carry the Desktop Cut.ID through serialization.");
            Assert.That(questCut.ID, Is.EqualTo(desktopCut.ID), "Quest must restore the identity used by the Desktop publisher.");
            var desktopTimeline = desktopFixture.AddTimeline("session-timeline");
            var questTimeline = questFixture.AddTimeline("session-timeline");

            var proposed = new List<(OperationId Id, SetCutDefinition Mutation)>();
            using (var desktopBoundary = new V2SceneMutationBoundary(desktopFixture.Scene, V2OriginDevice.Desktop))
            using (var questBoundary = new V2SceneMutationBoundary(questFixture.Scene, V2OriginDevice.Quest))
            {
                desktopBoundary.MutationProposed += (id, mutation, _) =>
                {
                    if (mutation is SetCutDefinition cutDefinition)
                        proposed.Add((id, cutDefinition));
                };

                desktopCut.Position = 0.63f;
                desktopFixture.Scene.UpdateCutPlane(desktopCut, changedByUser: true, preserveDefinitionNormal: true);
                questBoundary.Apply((SetCutDefinition)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(proposed[^1].Mutation)), V2MutationApplicationOrigin.Remote, proposed[^1].Id);
                Assert.That(questCut.Position, Is.EqualTo(0.63f), "The discrete slider change must resolve to the Quest's preexisting cut.");

                desktopCut.Flip = true;
                desktopCut.Position = 1f - desktopCut.Position;
                desktopFixture.Scene.UpdateCutPlane(desktopCut, changedByUser: true, preserveDefinitionNormal: true);
                questBoundary.Apply((SetCutDefinition)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(proposed[^1].Mutation)), V2MutationApplicationOrigin.Remote, proposed[^1].Id);
                Assert.That(questCut.Flip, Is.True);
                Assert.That(questCut.Position, Is.EqualTo(0.37f));

                foreach (float position in new[] { 0.38f, 0.64f, 0.82f })
                {
                    desktopCut.Position = position;
                    desktopFixture.Scene.UpdateCutPlane(desktopCut, changedByUser: true, preserveDefinitionNormal: true);
                    questBoundary.Apply((SetCutDefinition)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(proposed[^1].Mutation)), V2MutationApplicationOrigin.Remote, proposed[^1].Id);
                }

                SetCutDefinition complete = proposed[^1].Mutation;
                Assert.That(complete.CutId.Value, Is.EqualTo(desktopCut.ID));
                Assert.That(complete.Orientation, Is.EqualTo(V2CutOrientation.Custom));
                Assert.That(complete.Flip, Is.True);
                Assert.That(complete.NumberOfCuts, Is.EqualTo(321u));
                Assert.That(complete.Position, Is.EqualTo(0.82f));
                Assert.That(BitConverter.SingleToInt32Bits(complete.NormalX), Is.Zero, "Native signed zero must be canonicalized before encoding.");
                Assert.That(new Vector3(complete.NormalX, complete.NormalY, complete.NormalZ), Is.EqualTo(normal));
                SetCutDefinition receiverDefinition = (SetCutDefinition)questBoundary.ReadCurrentMutation(complete);
                Assert.That(receiverDefinition.CutId, Is.EqualTo(complete.CutId));
                Assert.That(receiverDefinition.Position, Is.EqualTo(0.82f));
                Assert.That(receiverDefinition.Flip, Is.True);
                Assert.That(questCut.Position, Is.EqualTo(0.82f), "Continuous updates must leave the receiver at the final desktop value.");
                Assert.That(questCut.Flip, Is.True);
                Assert.That(proposed, Has.Count.EqualTo(6), "Each valid definition change must produce one complete typed mutation.");
            }

            // Restore the shared definition from the full prepared-scene transfer before the live
            // session starts. Runtime definition edits below must use only the persistent mutation path.
            desktopCut.Position = 0.4f;
            desktopCut.Flip = false;
            questCut.Position = 0.4f;
            questCut.Flip = false;

            var questWrapperObject = new GameObject("v2 Quest wrapper state");
            questWrapperObject.SetActive(false);
            var questView = questWrapperObject.AddComponent<HBP.Quest.QuestAnatomyView>();
            ConstructorInfo restoredSceneConstructor = typeof(RestoredScene).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(ScenePayload), typeof(SceneArchive) }, null);
            Assert.That(restoredSceneConstructor, Is.Not.Null);
            var wrapperPublishedScene = (RestoredScene)restoredSceneConstructor.Invoke(new object[] { questFixture.Scene, null, null });
            SetPrivateField(questView, "current", wrapperPublishedScene);
            questFixture.Root.transform.SetParent(questWrapperObject.transform, false);
            questView.ToggleSurface();
            Vector3 wrapperPosition = new Vector3(0.21f, -0.13f, 0.37f);
            Quaternion wrapperRotation = Quaternion.Euler(7f, 19f, 3f);
            Vector3 wrapperScale = new Vector3(1.2f, 0.9f, 1.1f);
            questWrapperObject.transform.localPosition = wrapperPosition;
            questWrapperObject.transform.localRotation = wrapperRotation;
            questWrapperObject.transform.localScale = wrapperScale;

            PreparedSceneDeliveryBinding binding = CreatePreparedBinding("scene-focused-column", "session-timeline");
            object questOwner;
            try
            {
                questOwner = CreateQuestSession(questFixture.Scene, binding);
            }
            catch (TargetInvocationException exception)
            {
                throw new AssertionException("Quest session construction failed: " + exception.InnerException);
            }

            Type desktopOwnerType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(desktopOwnerType, Is.Not.Null);
            var connectionPairs = new List<LoopbackPeerPair>();
            var questRuns = new List<Task<Exception>>();
            int openCount = 0;
            Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task> openReplica = async (host, pin, credential, stop, transport) =>
            {
                Interlocked.Increment(ref openCount);
                LoopbackPeerPair pair = await LoopbackPeerPair.ConnectAsync();
                connectionPairs.Add(pair);
                questRuns.Add(CaptureTaskExceptionAsync(RunQuestSession(questOwner, pair.Server.GetStream(), stop)));
                await transport.RunConnectionAsync(pair.Client.GetStream(), stop);
            };

            object desktopOwner = null;
            Task startPublication = null;
            try
            {
                Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
                ConstructorInfo constructor = desktopOwnerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
                Assert.That(constructor, Is.Not.Null);
                try
                {
                    desktopOwner = constructor.Invoke(new object[] { desktopFixture.Scene, Session.Value.ToString(), Incarnation.Value.ToString(), openReplica });
                }
                catch (TargetInvocationException exception)
                {
                    throw new AssertionException("Desktop session construction failed: " + exception.InnerException);
                }

                MethodInfo start = desktopOwnerType.GetMethod("StartAfterPublicationAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.That(start, Is.Not.Null);
                startPublication = (Task)start.Invoke(desktopOwner, new object[] { binding, "loopback", Array.Empty<byte>(), Array.Empty<byte>(), CancellationToken.None });
                try
                {
                    await AwaitGuardAsync(startPublication);
                }
                catch (Exception exception)
                {
                    string failureReason = desktopOwnerType.GetProperty("FailureReason").GetValue(desktopOwner) as string;
                    string questFailures = string.Join("; ", questRuns.Where(task => task.IsCompleted).Select(task => task.Result?.ToString() ?? "completed cleanly"));
                    throw new AssertionException($"Initial publication was not acknowledged: owner={failureReason ?? "none"}; quest={questFailures}; wait={exception}");
                }

                Assert.That((bool)desktopOwnerType.GetProperty("IsLive").GetValue(desktopOwner), Is.True);
                Assert.That(questCut.Position, Is.EqualTo(0.4f), "The persistent session starts from the shared prepared scene definition.");
                Assert.That(questCut.Flip, Is.False);

                desktopCut.Position = 0.25f;
                desktopFixture.Scene.UpdateCutPlane(desktopCut, changedByUser: true, preserveDefinitionNormal: true);
                await WaitUntilAsync(() => questCut.Position == 0.25f, "Quest did not receive the discrete position change.");

                desktopCut.Flip = true;
                desktopCut.Position = 1f - desktopCut.Position;
                desktopFixture.Scene.UpdateCutPlane(desktopCut, changedByUser: true, preserveDefinitionNormal: true);
                await WaitUntilAsync(() => questCut.Flip && questCut.Position == 0.75f, "Quest did not receive the flip and its discrete position change.");

                desktopTimeline.CurrentIndex = 17;
                desktopTimeline.IsLooping = true;
                desktopTimeline.Step = 2;
                await WaitUntilAsync(() => questTimeline.CurrentIndex == 17 && questTimeline.IsLooping && questTimeline.Step == 2, "The existing timeline anchor mutation regressed while synchronizing cuts.");

                foreach (float position in new[] { 0.3f, 0.56f, 0.91f })
                {
                    desktopCut.Position = position;
                    desktopFixture.Scene.UpdateCutPlane(desktopCut, changedByUser: true, preserveDefinitionNormal: true);
                }

                await WaitUntilAsync(() => questCut.Position == 0.91f, "Quest did not converge to the final continuous position.");

                Color synchronizedColor = new Color(0.17f, 0.43f, 0.89f, 1f);
                desktopFixture.Sites[0].State.Color = synchronizedColor;
                await WaitUntilAsync(() => questFixture.Sites[0].State.Color == synchronizedColor, "The existing site-color mutation regressed while synchronizing cuts.");

                Assert.That(openCount, Is.EqualTo(1), "Cut edits must flow over the live session without reopening the replica or sending a new full scene.");
                Assert.That(questView.PublishedScene.Scene, Is.SameAs(questFixture.Scene));
                Assert.That(questView.SurfaceHidden, Is.True);
                Assert.That(Vector3.Distance(questWrapperObject.transform.localPosition, wrapperPosition), Is.LessThan(1e-5f));
                Assert.That(Quaternion.Angle(questWrapperObject.transform.localRotation, wrapperRotation), Is.LessThan(0.01f));
                Assert.That(Vector3.Distance(questWrapperObject.transform.localScale, wrapperScale), Is.LessThan(1e-5f));
                Assert.That(GetDriverCanonicalWatermark(questOwner), Is.GreaterThanOrEqualTo(10UL));
            }
            finally
            {
                if (desktopOwner != null)
                {
                    CancellationTokenSource lifetime = (CancellationTokenSource)desktopOwnerType.GetField("m_Lifetime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                    CancellationTokenSource connectionLifetime = (CancellationTokenSource)desktopOwnerType.GetField("m_ConnectionLifetime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                    lifetime.Cancel();
                    connectionLifetime?.Cancel();
                }

                foreach (LoopbackPeerPair pair in connectionPairs) pair.Close();
                if (desktopOwner != null)
                {
                    var desktopTasks = new List<Task>();
                    foreach (string fieldName in new[] { "m_IncomingTask", "m_ConnectionTask" })
                    {
                        Task pending = (Task)desktopOwnerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                        if (pending != null) desktopTasks.Add(CaptureTaskExceptionAsync(pending));
                    }

                    if (desktopTasks.Count > 0) await AwaitGuardAsync(Task.WhenAll(desktopTasks));
                    ((IDisposable)desktopOwner).Dispose();
                }

                if (questRuns.Count > 0)
                    await AwaitGuardAsync(Task.WhenAll(questRuns));
                ((IDisposable)questOwner).Dispose();
                SetPrivateField(questView, "current", null);
                UnityEngine.Object.DestroyImmediate(questWrapperObject);
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

        private static async Task<V2TransportRecord> ReadIncomingAsync(V2PersistentTransport transport, int timeoutSeconds = 5)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            while (true)
            {
                V2TransportRecord record = await transport.ReadIncomingAsync(timeout.Token);
                if (record.Lane != V2ScheduleLane.SessionControl || !V2RetentionProgress.TryDecode(record.GetPayloadCopy(), out _)) return record;
            }
        }

        private static async Task<V2TransportRecord> ReadIncomingOrNullAsync(V2PersistentTransport transport, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    V2TransportRecord record = await transport.ReadIncomingAsync(cancellationToken);
                    if (record.Lane != V2ScheduleLane.SessionControl || !V2RetentionProgress.TryDecode(record.GetPayloadCopy(), out _)) return record;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        private static V2SiteFilterControl ReadSiteFilterControl(V2TransportRecord record)
        {
            Assert.That(record, Is.Not.Null);
            Assert.That(record.Lane, Is.EqualTo(V2ScheduleLane.SessionControl));
            Assert.That(V2SiteFilterControlCodec.TryDecode(record.GetPayloadCopy(), out V2SiteFilterControl control), Is.True, "The session-control record should contain a T12 site-filter message.");
            return control;
        }

        private static V2CorrelationControl ReadCorrelationControl(V2TransportRecord record)
        {
            Assert.That(record, Is.Not.Null);
            Assert.That(record.Lane, Is.EqualTo(V2ScheduleLane.SessionControl));
            Assert.That(V2CorrelationControlCodec.TryDecode(record.GetPayloadCopy(), out V2CorrelationControl control), Is.True, "The session-control record should contain a T13 correlation message.");
            return control;
        }

        private static V2SiteFilterControl ReadNextQuestSiteFilterControl(object questOwner)
        {
            FieldInfo schedulerField = questOwner.GetType().GetField("m_Scheduler", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(schedulerField, Is.Not.Null);
            var scheduler = (V2OutgoingScheduler)schedulerField.GetValue(questOwner);
            while (scheduler.TryGetNextTransmission(out V2TransmissionAttempt transmission))
            {
                if (transmission.Frame.Lane != V2ScheduleLane.SessionControl) continue;
                Assert.That(V2SiteFilterControlCodec.TryDecode(transmission.Frame.GetPayloadCopy(), out V2SiteFilterControl control), Is.True);
                return control;
            }

            Assert.Fail("Quest did not send a site-filter session control.");
            return null;
        }

        private static V2TransportRecord CreateTransportRecord(V2PersistentTransport transport, V2TransmissionAttempt attempt)
        {
            MethodInfo create = transport.GetType().GetMethod("CreateApplicationRecord", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(create, Is.Not.Null);
            return (V2TransportRecord)create.Invoke(transport, new object[] { attempt.Frame });
        }

        private static object GetQuestSceneOperationBulkReceiver(object questOwner)
        {
            FieldInfo field = questOwner.GetType().GetField("m_SceneOperationBulkReceiver", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(questOwner);
        }

        private static object GetActiveQuestSiteFilterJob(object questOwner)
        {
            FieldInfo field = questOwner.GetType().GetField("m_ActiveSiteFilterJob", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(questOwner);
        }

        private static object GetActiveQuestCorrelationJob(object questOwner)
        {
            FieldInfo field = questOwner.GetType().GetField("m_ActiveCorrelationJob", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(questOwner);
        }

        private static object GetDesktopActiveCorrelationJob(object desktopOwner)
        {
            FieldInfo field = desktopOwner.GetType().GetField("m_ActiveCorrelationJob", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(desktopOwner);
        }

        private static OperationId GetQuestActiveJobId(object questOwner) => GetActiveQuestSiteFilterJob(questOwner)?.GetType().GetProperty("JobId")?.GetValue(GetActiveQuestSiteFilterJob(questOwner)) as OperationId;

        private static ulong GetQuestActiveJobGeneration(object questOwner)
        {
            object active = GetActiveQuestSiteFilterJob(questOwner);
            return active == null ? 0 : (ulong)active.GetType().GetProperty("Generation").GetValue(active);
        }

        private static ulong GetQuestLastDesktopSiteFilterGeneration(object questOwner)
        {
            FieldInfo field = questOwner.GetType().GetField("m_LastDesktopSiteFilterGeneration", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (ulong)field.GetValue(questOwner);
        }

        private static object GetDesktopActiveSiteFilterJob(object desktopOwner)
        {
            FieldInfo field = desktopOwner.GetType().GetField("m_ActiveSiteFilterJob", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(desktopOwner);
        }

        private static int GetSensitiveActivityCount(Base3DScene scene)
        {
            FieldInfo field = typeof(Base3DScene).GetField("m_SensitiveActivityOperationCount", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (int)field.GetValue(scene);
        }

        private static Task InvokeQuestSiteFilterControlAsync(object questOwner, V2SiteFilterControl control, CancellationToken stop)
        {
            MethodInfo method = questOwner.GetType().GetMethod("ProcessSiteFilterControlAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Task)method.Invoke(questOwner, new object[] { control, stop });
        }

        private static Task InvokeDesktopSiteFilterControlAsync(object desktopOwner, V2SiteFilterControl control, CancellationToken stop)
        {
            MethodInfo method = desktopOwner.GetType().GetMethod("ProcessSiteFilterControlAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Task)method.Invoke(desktopOwner, new object[] { control, stop });
        }

        private static Task<bool> InvokeDesktopSiteFilterJobAsync(object desktopOwner, OperationId jobId, V2SiteFilterRequest request, CancellationToken stop)
        {
            MethodInfo method = desktopOwner.GetType().GetMethod("RunSiteFilterJobAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Task<bool>)method.Invoke(desktopOwner, new object[] { jobId, request, stop, false });
        }

        private static Task<bool> InvokeDesktopCorrelationJobAsync(object desktopOwner, OperationId jobId, V2CorrelationRequest request, CancellationToken stop)
        {
            MethodInfo method = desktopOwner.GetType().GetMethod("RunCorrelationJobAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Task<bool>)method.Invoke(desktopOwner, new object[] { jobId, request, stop, false });
        }

        private static Task InvokeQuestCorrelationControlAsync(object questOwner, V2CorrelationControl control, CancellationToken stop)
        {
            MethodInfo method = questOwner.GetType().GetMethod("ProcessCorrelationControlAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Task)method.Invoke(questOwner, new object[] { control, stop });
        }

        private static bool IsActiveJobCancelled(object activeJob)
        {
            Assert.That(activeJob, Is.Not.Null);
            CancellationTokenSource cancellation = (CancellationTokenSource)activeJob.GetType().GetProperty("Cancellation").GetValue(activeJob);
            return cancellation.IsCancellationRequested;
        }

        private static V2CorrelationControl[] ReadDesktopCorrelationControls(object desktopOwner)
        {
            FieldInfo field = desktopOwner.GetType().GetField("m_Scheduler", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var scheduler = (V2OutgoingScheduler)field.GetValue(desktopOwner);
            var controls = new List<V2CorrelationControl>();
            while (scheduler.TryGetNextTransmission(out V2TransmissionAttempt transmission))
            {
                if (transmission.Frame.Lane != V2ScheduleLane.SessionControl) continue;
                Assert.That(V2CorrelationControlCodec.TryDecode(transmission.Frame.GetPayloadCopy(), out V2CorrelationControl control), Is.True);
                controls.Add(control);
            }

            return controls.ToArray();
        }

        private static V2CorrelationControl ReadNextQuestCorrelationControl(object questOwner)
        {
            FieldInfo field = questOwner.GetType().GetField("m_Scheduler", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var scheduler = (V2OutgoingScheduler)field.GetValue(questOwner);
            while (scheduler.TryGetNextTransmission(out V2TransmissionAttempt transmission))
            {
                if (transmission.Frame.Lane != V2ScheduleLane.SessionControl) continue;
                Assert.That(V2CorrelationControlCodec.TryDecode(transmission.Frame.GetPayloadCopy(), out V2CorrelationControl control), Is.True);
                return control;
            }

            Assert.Fail("Quest did not send a correlation session control.");
            return null;
        }

        private static async Task AwaitGuardAsync(Task task, int timeoutSeconds = 5)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
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

        private static async Task WaitUntilAsync(Func<bool> condition, string timeoutMessage, int timeoutSeconds = 5)
        {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (timeout.Elapsed >= TimeSpan.FromSeconds(timeoutSeconds)) throw new TimeoutException(timeoutMessage);
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

        private static object CreateDesktopSession(Base3DScene scene)
        {
            Type sessionType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
            Assert.That(sessionType, Is.Not.Null, "Desktop's production v2 session should be loaded for this integration test.");
            Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
            ConstructorInfo constructor = sessionType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
            Assert.That(constructor, Is.Not.Null);
            object owner = constructor.Invoke(new object[] { scene, Session.Value.ToString(), Incarnation.Value.ToString(), null });
            FieldInfo stateField = sessionType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(stateField, Is.Not.Null);
            SetPrivateField(owner, "m_State", Enum.Parse(stateField.FieldType, "Live"));
            return owner;
        }

        private static void PrepareReadyActivityProjection(Base3DScene scene)
        {
            SetPrivateField(scene, "m_ProjectionRequested", true);
            scene.IsGeneratorUpToDate = true;
            scene.SceneInformation.GeneratorNeedsUpdate = false;
            scene.SceneInformation.GeneratorUpdateRequested = false;
        }

        private static void PrepareAutomaticProjectionInputs(Base3DScene scene)
        {
            scene.SceneInformation.GeometryNeedsUpdate = false;
            scene.SceneInformation.ProjectionGridNeedsUpdate = false;
            scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
        }

        private static object InstallSyntheticActiveActivityProjectionJob(object session, Base3DScene scene, OperationId jobId, ulong localProjectionGeneration)
        {
            Assert.That(scene.TryBeginCoordinatedActivityProjection(out IDisposable activityScope), Is.True);
            scene.IsGeneratorUpToDate = false;
            SetPrivateField(scene, "m_ProjectionGeneration", localProjectionGeneration);
            SetPrivateField(scene, "m_ProjectionState", Enum.Parse(typeof(ActivityProjectionState), "Computing"));
            SetPrivateField(scene, "m_UpdatingGenerators", true);
            Type leaseType = typeof(Base3DScene).Assembly.GetType("HBP.Data.Module3D.ActivityProjectionInputLease");
            ConstructorInfo leaseConstructor = leaseType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(ulong), typeof(ulong), typeof(Column3D[]) }, null);
            Assert.That(leaseConstructor, Is.Not.Null);
            object lease = leaseConstructor.Invoke(new object[] { localProjectionGeneration, scene.ActivityInputGeneration, Array.Empty<Column3D>() });
            SetPrivateField(scene, "m_ActiveActivityProjection", lease);

            Type sessionType = session.GetType();
            Type activeType = sessionType.GetNestedType("ActiveActivityProjectionJob", BindingFlags.NonPublic);
            Assert.That(activeType, Is.Not.Null);
            object active;
            if (sessionType.FullName == "HBP.Quest.Desktop.DesktopV2ReplicaSession")
            {
                var registry = (V2JobGenerationRegistry)sessionType.GetField("m_ActivityProjectionGenerations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                V2JobIdentity identity = registry.BeginJob(Scene, Incarnation, V2JobType.ActivityProjection, jobId, scene.ActivityInputGeneration);
                Assert.That(identity, Is.Not.Null);
                ConstructorInfo constructor = activeType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(V2JobIdentity), typeof(IDisposable) }, null);
                Assert.That(constructor, Is.Not.Null);
                active = constructor.Invoke(new object[] { identity, activityScope });
            }
            else
            {
                ConstructorInfo constructor = activeType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(OperationId), typeof(ulong), typeof(ulong), typeof(IDisposable) }, null);
                Assert.That(constructor, Is.Not.Null);
                active = constructor.Invoke(new object[] { jobId, (ulong)1, scene.ActivityInputGeneration, activityScope });
                activeType.GetProperty("LocalStarted").SetValue(active, true);
            }

            activeType.GetProperty("LocalProjectionGeneration").SetValue(active, localProjectionGeneration);
            sessionType.GetField("m_ActiveActivityProjectionJob", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, active);
            return active;
        }

        private static bool IsCurrentActivityProjection(Base3DScene scene, object lease)
        {
            MethodInfo method = typeof(Base3DScene).GetMethod("IsCurrentActivityProjection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(scene, new[] { lease });
        }

        private static bool ShouldStartActivityProjection(Base3DScene scene, bool automaticPolicyEnabled)
        {
            MethodInfo method = typeof(Base3DScene).GetMethod("ShouldStartActivityProjection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(scene, new object[] { automaticPolicyEnabled });
        }

        private static object InvokePrivateMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {target.GetType().Name}.{methodName}.");
            return method.Invoke(target, arguments);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}.");
            return (T)field.GetValue(target);
        }

        private static void ForceQuestOfflineTransition(object questOwner)
        {
            object driver = GetPrivateField<object>(questOwner, "m_Driver");
            Type driverType = driver.GetType();
            driverType.GetMethod("BeginDisconnectGrace").Invoke(driver, null);
            object scheduler = GetPrivateField<object>(driver, "m_Scheduler");
            // The real transport has already entered its reconnect grace. Expire its monotonic deadline
            // deterministically so the driver's public ConnectionState transition raises OfflineLocalEntered.
            SetPrivateField(scheduler, "m_GraceDeadline", 0L);
            Assert.That((V2QuestMutationConnectionState)driverType.GetProperty("ConnectionState").GetValue(driver), Is.EqualTo(V2QuestMutationConnectionState.OfflineLocal));
        }

        private static object GetSessionActivityProjectionJob(object session)
        {
            return session.GetType().GetField("m_ActiveActivityProjectionJob", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
        }

        private static bool GetTerminalFlag(object active, string propertyName)
        {
            object terminals = active.GetType().GetProperty("Terminals").GetValue(active);
            return (bool)terminals.GetType().GetProperty(propertyName).GetValue(terminals);
        }

        private static PreparedSceneDeliveryBinding CreatePreparedBinding(params string[] columnIds)
        {
            if (columnIds == null || columnIds.Length == 0)
                columnIds = new[] { "scene-focused-column" };
            var metadata = new JObject
            {
                ["TransferId"] = Incarnation.Value.ToString(),
                ["SessionId"] = "published-session",
                ["GlobalContextId"] = Session.Value.ToString(),
                ["Visualization"] = new JObject { ["ID"] = Scene.Value.ToString() },
                ["StandardFiles"] = new JObject(),
                ["Meshes"] = new JArray(),
                ["MRIs"] = new JArray(),
                ["Columns"] = new JArray(columnIds.Select(id => new JObject
                {
                    ["Id"] = id,
                    ["Functional"] = new JArray()
                }))
            };
            PreparedSceneManifest manifest = PreparedSceneManifest.FromMetadata(metadata);
            ConstructorInfo constructor = typeof(PreparedSceneDeliveryBinding).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(PreparedSceneManifest) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (PreparedSceneDeliveryBinding)constructor.Invoke(new object[] { new string('a', 64), manifest });
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

        private sealed class LoadingManagerFixture : IDisposable
        {
            private readonly bool m_OwnsRoot;
            private readonly FieldInfo m_InstanceField;
            private readonly object m_PreviousInstance;

            public GameObject Root { get; }
            public Component LoadingCircle { get; }

            public LoadingManagerFixture()
            {
                Type managerType = FindLoadedType("HBP.UI.Tools.LoadingManager");
                Type loadingCircleType = FindLoadedType("HBP.UI.Tools.LoadingCircle");
                Assert.That(managerType, Is.Not.Null);
                Assert.That(loadingCircleType, Is.Not.Null);
                Type singletonType = managerType.BaseType?.BaseType;
                Assert.That(singletonType, Is.Not.Null);
                m_InstanceField = singletonType.GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(m_InstanceField, Is.Not.Null);
                m_PreviousInstance = m_InstanceField.GetValue(null);
                Root = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(candidate => candidate && candidate.scene.IsValid() && candidate.GetComponent(managerType) != null);
                if (Root == null)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LoadingCircle/Loading Manager.prefab");
                    Assert.That(prefab, Is.Not.Null, "The production LoadingManager prefab is required to test cancellation through the loading visual.");
                    Root = UnityEngine.Object.Instantiate(prefab);
                    m_OwnsRoot = true;
                }

                LoadingCircle = Root.GetComponentInChildren(loadingCircleType, true);
                Assert.That(LoadingCircle, Is.Not.Null);
                m_InstanceField.SetValue(null, Root.GetComponent(managerType));
                if (m_OwnsRoot)
                    LoadingCircle.GetType().GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public).Invoke(LoadingCircle, null);
                LoadingCircle.gameObject.SetActive(false);
            }

            public void CancelVisual()
            {
                MethodInfo cancel = LoadingCircle.GetType().GetMethod("Cancel", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(cancel, Is.Not.Null);
                cancel.Invoke(LoadingCircle, null);
            }

            public void Dispose()
            {
                if (m_OwnsRoot && Root) UnityEngine.Object.DestroyImmediate(Root);
                m_InstanceField.SetValue(null, m_PreviousInstance);
            }
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

        private sealed class CorrelationSceneFixture : IDisposable
        {
            private readonly Patient m_Patient;

            public GameObject Root { get; }
            public Base3DScene Scene { get; }
            public Column3DIEEG Column { get; }
            public List<HBP.Core.Object3D.Site> Sites { get; } = new();
            public string ColumnId => "t13-correlation-column";

            public CorrelationSceneFixture(int siteCount)
            {
                Root = new GameObject("T13 correlation scene");
                Scene = Root.AddComponent<Base3DScene>();
                var mriManager = Root.AddComponent<MRIManager>();
                SetPrivateField(mriManager, "m_Scene", Scene);
                SetPrivateField(Scene, "m_MRIManager", mriManager);
                m_Patient = new Patient { ID = "50000000-0000-0000-0000-000000000013", Name = "T13 correlation patient" };
                var columnData = new IEEGColumn("T13 correlation", new BaseConfiguration(), null, string.Empty, null, new DynamicConfiguration(), ColumnId);
                SetAutoProperty(Scene, "Visualization", new Visualization("T13 correlation", new[] { m_Patient }, new Column[] { columnData }, new VisualizationConfiguration(), V2PersistentTransportLoopbackTests.Scene.Value.ToString()));
                Column = Root.AddComponent<Column3DIEEG>();
                SetAutoProperty(Column, "ColumnData", columnData);
                for (int index = 0; index < siteCount; index++)
                {
                    string name = "t13-site-" + index.ToString("D3");
                    var siteObject = new GameObject(name);
                    siteObject.transform.SetParent(Root.transform, false);
                    HBP.Core.Object3D.Site site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
                    site.Information = new SiteInformation { Patient = m_Patient, Name = name };
                    site.State = new SiteState();
                    Sites.Add(site);
                }

                SetAutoProperty(Column, "Sites", Sites);
                Scene.Columns.Add(Column);
            }

            public CorrelationProvenance CreateProvenance() => new(CorrelationResultSource.Imported, m_Patient.ID, m_Patient.Name, ColumnId, "t13-dataset-id", "T13 dataset", "t13-protocol-id", "T13 protocol", "t13-bloc-id", "T13 bloc", "t13-data-info-id", "T13 data", HBP.Core.Enums.NormalizationType.Trial, 0.025f, true);

            public byte[] CreateResultBytes()
            {
                var correlations = new Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>>(Sites.Count);
                var means = new Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>>(Sites.Count);
                for (int from = 0; from < Sites.Count; from++)
                {
                    var correlationRow = new Dictionary<HBP.Core.Object3D.Site, float>(Sites.Count);
                    var meanRow = new Dictionary<HBP.Core.Object3D.Site, float>(Sites.Count);
                    for (int to = 0; to < Sites.Count; to++)
                    {
                        correlationRow.Add(Sites[to], (from + to) / (float)Math.Max(1, Sites.Count * 2));
                        meanRow.Add(Sites[to], (from + 2 * to) / (float)Math.Max(1, Sites.Count * 3));
                    }

                    correlations.Add(Sites[from], correlationRow);
                    means.Add(Sites[from], meanRow);
                }

                var result = new CorrelationResultData(ColumnId, correlations, means, CreateProvenance());
                return CorrelationResultResource.FromResults(Scene, new[] { result }).Encode();
            }

            public void Dispose()
            {
                if (Root) UnityEngine.Object.DestroyImmediate(Root);
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

        private sealed class LiveActivityProjectionSessionPair
        {
            private readonly LoopbackPeerPair m_Pair;
            private readonly Task m_Publication;
            private readonly Task<Exception> m_DesktopRun;
            private readonly Task<Exception> m_QuestRun;
            private int m_Closed;

            public object DesktopOwner { get; }
            public object QuestOwner { get; }
            public V2PersistentTransport DesktopTransport { get; }

            private LiveActivityProjectionSessionPair(LoopbackPeerPair pair, Task publication, Task<Exception> desktopRun, Task<Exception> questRun, object desktopOwner, object questOwner, V2PersistentTransport desktopTransport)
            {
                m_Pair = pair;
                m_Publication = publication;
                m_DesktopRun = desktopRun;
                m_QuestRun = questRun;
                DesktopOwner = desktopOwner;
                QuestOwner = questOwner;
                DesktopTransport = desktopTransport;
            }

            public static async Task<LiveActivityProjectionSessionPair> ConnectAsync(Base3DScene desktopScene, Base3DScene questScene, PreparedSceneDeliveryBinding binding)
            {
                LoopbackPeerPair pair = await LoopbackPeerPair.ConnectAsync();
                object questOwner = CreateQuestSession(questScene, binding);
                Type desktopType = FindLoadedType("HBP.Quest.Desktop.DesktopV2ReplicaSession");
                Type connectorType = typeof(Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task>);
                ConstructorInfo constructor = desktopType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Base3DScene), typeof(string), typeof(string), connectorType }, null);
                Assert.That(constructor, Is.Not.Null);
                Task<Exception> desktopRun = null;
                Task<Exception> questRun = null;
                Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task> openReplica = (host, pin, credential, connectionStop, transport) =>
                {
                    questRun = CaptureTaskExceptionAsync(RunQuestSession(questOwner, pair.Server.GetStream(), connectionStop));
                    Task connection = transport.RunConnectionAsync(pair.Client.GetStream(), connectionStop);
                    desktopRun = CaptureTaskExceptionAsync(connection);
                    return connection;
                };

                object desktopOwner = null;
                Task publication = null;
                try
                {
                    desktopOwner = constructor.Invoke(new object[] { desktopScene, Session.Value.ToString(), Incarnation.Value.ToString(), openReplica });
                    MethodInfo start = desktopType.GetMethod("StartAfterPublicationAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Assert.That(start, Is.Not.Null);
                    publication = (Task)start.Invoke(desktopOwner, new object[] { binding, "loopback", Array.Empty<byte>(), Array.Empty<byte>(), CancellationToken.None });
                    await AwaitGuardAsync(publication);

                    V2PersistentTransport desktopTransport = (V2PersistentTransport)desktopType.GetField("m_Transport", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktopOwner);
                    await WaitUntilAsync(() => (V2PersistentTransportState)questOwner.GetType().GetProperty("TransportState").GetValue(questOwner) == V2PersistentTransportState.Connected && desktopTransport.State == V2PersistentTransportState.Connected, "The paired activity-projection sessions did not connect.");
                    await WaitUntilAsync(() => GetPrivateField<object>(questScene, "m_CoordinatedAutomaticRecomputePolicy") != null, "Quest did not apply Desktop's initial activity-projection policy.");
                    Assert.That((bool)desktopType.GetProperty("IsLive").GetValue(desktopOwner), Is.True);
                    return new LiveActivityProjectionSessionPair(pair, publication, desktopRun, questRun, desktopOwner, questOwner, desktopTransport);
                }
                catch
                {
                    if (desktopOwner is IDisposable desktopDisposable) desktopDisposable.Dispose();
                    if (publication != null)
                    {
                        try
                        {
                            await AwaitGuardAsync(publication);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (desktopRun != null)
                    {
                        try
                        {
                            await AwaitGuardAsync(desktopRun);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (questRun != null)
                    {
                        try
                        {
                            await AwaitGuardAsync(questRun);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    ((IDisposable)questOwner).Dispose();
                    pair.Close();
                    throw;
                }
            }

            public async Task CloseAsync()
            {
                if (Interlocked.Exchange(ref m_Closed, 1) != 0) return;
                if (DesktopOwner is IDisposable desktopDisposable) desktopDisposable.Dispose();

                if (m_Publication != null && !m_Publication.IsCompleted)
                {
                    try
                    {
                        await AwaitGuardAsync(m_Publication);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (m_DesktopRun != null)
                {
                    try
                    {
                        await AwaitGuardAsync(m_DesktopRun);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (m_QuestRun != null)
                {
                    try
                    {
                        await AwaitGuardAsync(m_QuestRun);
                    }
                    catch (Exception)
                    {
                    }
                }

                ((IDisposable)QuestOwner).Dispose();
                m_Pair.Close();
            }
        }

        private sealed class DelayedReadStream : Stream
        {
            private readonly Stream m_Inner;
            private readonly TimeSpan m_ReadDelay;

            public DelayedReadStream(Stream inner, TimeSpan readDelay)
            {
                m_Inner = inner ?? throw new ArgumentNullException(nameof(inner));
                m_ReadDelay = readDelay;
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
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => m_Inner.Write(buffer, offset, count);

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await Task.Delay(m_ReadDelay, cancellationToken);
                return await m_Inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        private sealed class SessionSceneFixture : IDisposable
        {
            private const string FixturePatientId = "50000000-0000-0000-0000-000000000005";
            private BrainMaterials m_ProjectionMaterials;
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

            public void InitializeProjectionMaterials()
            {
                m_ProjectionMaterials = new BrainMaterials();
                SetAutoProperty(Scene, "BrainMaterials", m_ProjectionMaterials);
            }

            public TestTimeline AddTimeline(string columnId)
            {
                var timeline = new TestTimeline(64);
                var columnData = new AnatomicColumn(columnId, new BaseConfiguration(), new AnatomicConfiguration(), columnId);
                TimelineSessionColumn3D column = Root.AddComponent<TimelineSessionColumn3D>();
                column.Initialize(columnData, timeline);
                Scene.Columns.Add(column);
                return timeline;
            }

            public void Dispose()
            {
                if (Root) UnityEngine.Object.DestroyImmediate(Root);
                if (m_ProjectionMaterials != null)
                {
                    foreach (string fieldName in new[] { "m_Brain", "m_TransparentBrain", "m_Cut", "m_TransparentCut" })
                    {
                        FieldInfo field = typeof(BrainMaterials).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                        UnityEngine.Object material = field?.GetValue(m_ProjectionMaterials) as UnityEngine.Object;
                        if (material) UnityEngine.Object.DestroyImmediate(material);
                    }

                    m_ProjectionMaterials = null;
                }
            }
        }

        private sealed class TimelineSessionColumn3D : Column3D
        {
            private TestTimeline m_Timeline;
            public override HBP.Core.Data.BasicTimeline NavigationTimeline => m_Timeline;

            public void Initialize(Column data, TestTimeline timeline)
            {
                ColumnData = data;
                Sites = new List<HBP.Core.Object3D.Site>();
                m_Timeline = timeline;
            }

            public override void ComputeSurfaceBrainUVWithActivity()
            {
            }
        }

        private sealed class TestTimeline : HBP.Core.Data.BasicTimeline
        {
            public TestTimeline(int length) => Length = length;
            public override HBP.Core.Data.SubTimeline CurrentSubtimeline => null;
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
            private int m_PublicationAcknowledgementWrites;
            public int SessionControlApplicationWrites => Volatile.Read(ref m_SessionControlApplicationWrites);
            public int PublicationAcknowledgementWrites => Volatile.Read(ref m_PublicationAcknowledgementWrites);

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
                {
                    Interlocked.Increment(ref m_SessionControlApplicationWrites);
                    if (V2PublicationControlCodec.TryDecodeAcknowledgement(record.GetPayloadCopy(), out _)) Interlocked.Increment(ref m_PublicationAcknowledgementWrites);
                }
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

        private sealed class PauseFirstBulkWriteStream : Stream
        {
            private readonly Stream m_Inner;
            private int m_BulkWriteSeen;
            public TaskCompletionSource<bool> Started { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Release { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public PauseFirstBulkWriteStream(Stream inner) => m_Inner = inner;
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
                    if (record.Lane == V2ScheduleLane.Bulk && Interlocked.CompareExchange(ref m_BulkWriteSeen, 1, 0) == 0)
                    {
                        Started.TrySetResult(true);
                        Task cancelled = Task.Delay(Timeout.Infinite, cancellationToken);
                        await Task.WhenAny(Release.Task, cancelled);
                        cancellationToken.ThrowIfCancellationRequested();
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
