using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using HBP.UI.Tools;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace HBP.Quest.Desktop
{
    internal sealed class DesktopReplicaSession : IDisposable
    {
        public event Action<SceneAction> ActionReceived;
        public ulong FinalSequence => m_Authority.CanonicalSequence;
        public ulong BulkThrough => m_Scheduler.LastAllocatedBulkStream;

        public void SendAction(SceneAction action)
        {
            if (!action.Scene.Equals(m_Identity.SceneId) || !action.Incarnation.Equals(m_Identity.IncarnationId)) throw new InvalidDataException("Wrong scene action incarnation.");
            var queued = m_Transport.EnqueueSessionControl(action.Encode(), DeliveryReliability.Reliable);
            if (!queued.Accepted) throw new IOException("Scene action queue is full.");
        }

        private enum PublicationState
        {
            Capturing,
            EncodingCheckpoint,
            AwaitingQuestApply,
            Live,
            Aborted,
            Disposed
        }

        private const int MaximumMutationsDuringCheckpointEncoding = 256;
        private readonly object m_Gate = new object();
        private readonly Base3DScene m_Scene;
        private SceneReconciliationRecord m_Reconciliation;
        private SceneMutationCheckpoint m_InitialCommon;
        private ulong m_InitialCommonSequence;
        private readonly UserPreferences m_UserPreferences;
        private readonly PreparedSceneIdentity m_Identity;
        private readonly SceneMutationBoundary m_Boundary;
        private readonly TimelineClockEstimator m_TimelineClock;
        private readonly DesktopMutationAuthority m_Authority;
        private readonly PublicationMutationJournal m_Journal;
        private readonly OutgoingScheduler m_Scheduler;
        private readonly PersistentTransport m_Transport;
        private readonly SurfaceInflationCoordinator m_SurfaceInflation;
        private readonly SceneOperationBulkReceiver m_SceneOperationBulkReceiver = new SceneOperationBulkReceiver();
        private readonly System.Collections.Generic.Queue<TransportRecord> m_DeferredIncoming = new System.Collections.Generic.Queue<TransportRecord>();
        private readonly Func<string, byte[], byte[], CancellationToken, PersistentTransport, Task> m_OpenReplica;
        private readonly CancellationTokenSource m_Lifetime = new CancellationTokenSource();
        private readonly CancellationTokenSource m_PublicationAbort = new CancellationTokenSource();
        private readonly TaskCompletionSource<OperationId> m_InitialApplyAcknowledged = new TaskCompletionSource<OperationId>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly JobGenerationRegistry m_SiteFilterGenerations = new JobGenerationRegistry();
        private readonly JobGenerationRegistry m_CorrelationGenerations = new JobGenerationRegistry();
        private readonly JobGenerationRegistry m_ActivityProjectionGenerations = new JobGenerationRegistry();
        private readonly System.Collections.Generic.HashSet<Task> m_SiteFilterTasks = new System.Collections.Generic.HashSet<Task>();
        private readonly System.Collections.Generic.HashSet<Task> m_CorrelationTasks = new System.Collections.Generic.HashSet<Task>();
        private readonly System.Collections.Generic.List<CanonicalMutation> m_AfterCheckpoint = new System.Collections.Generic.List<CanonicalMutation>();
        private PublicationState m_State = PublicationState.Capturing;
        private bool m_AbortRequiresRestart;
        private bool m_InitialPublicationStarted;
        private bool m_PostCheckpointOverflow;
        private OperationId m_InitialBarrierId;
        private ulong m_InitialBarrierSequence;
        private ulong m_PeerAppliedOriginThrough;
        private ulong m_PeerMinimumObservedSequence;
        private ulong m_PeerCurrentCanonicalSequence;
        private Task m_ConnectionTask;
        private Task m_IncomingTask;
        private CancellationTokenSource m_ConnectionLifetime;
        private string m_FailureReason;
        private IDisposable m_SiteFilterRequestRegistration;
        private IDisposable m_CorrelationRequestRegistration;
        private ActiveSiteFilterJob m_ActiveSiteFilterJob;
        private ActiveCorrelationJob m_ActiveCorrelationJob;
        private ActiveActivityProjectionJob m_ActiveActivityProjectionJob;
        private bool m_ProjectionEventsRemoved;
        private bool m_ApplyingRemoteProjectionRequest;
        private readonly System.Collections.Generic.HashSet<Guid> m_CancelledActivityProjectionRequests = new System.Collections.Generic.HashSet<Guid>();

        internal Base3DScene Scene => m_Scene;

        public CancellationToken PublicationAbortToken => m_PublicationAbort.Token;

        public bool RequiresPublicationRestart
        {
            get
            {
                lock (m_Gate) return m_AbortRequiresRestart;
            }
        }

        public bool IsLive
        {
            get
            {
                lock (m_Gate) return m_State == PublicationState.Live;
            }
        }

        public bool IsClosed
        {
            get
            {
                lock (m_Gate) return m_State == PublicationState.Disposed || m_State == PublicationState.Aborted;
            }
        }

        public string FailureReason
        {
            get
            {
                lock (m_Gate) return m_FailureReason;
            }
        }

        public bool CanRetryTransfer
        {
            get
            {
                lock (m_Gate) return !m_InitialPublicationStarted && m_State != PublicationState.Disposed && m_State != PublicationState.Aborted;
            }
        }

        public DesktopReplicaSession(Base3DScene scene, string globalContextId, string transferId) : this(scene, globalContextId, transferId, null)
        {
        }

        internal DesktopReplicaSession(Base3DScene scene, string globalContextId, string transferId, Func<string, byte[], byte[], CancellationToken, PersistentTransport, Task> openReplica)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            SceneReconciliationRecord.Get(scene)?.ReleaseReconciliationGuard();
            m_Scene = scene;
            m_UserPreferences = PersistentDataManager.IsInitialized ? PersistentDataManager.UserPreferences : null;
            m_Identity = PreparedSceneIdentity.Create(globalContextId, scene.Visualization.ID, transferId);
            m_TimelineClock = new TimelineClockEstimator(StopwatchMonotonicClock.Instance);
            m_Boundary = new SceneMutationBoundary(scene, OriginDevice.Desktop, timelineTimingEstimate: anchor => m_TimelineClock.TryEstimate(anchor.MonotonicAnchorTicks, anchor.TickFrequency, anchor.Step, out TimelineAnchorTimingEstimate estimate) ? estimate : (TimelineAnchorTimingEstimate?)null);
            m_Authority = new DesktopMutationAuthority(m_Identity.SceneId, m_Identity.IncarnationId, m_Boundary);
            m_Journal = new PublicationMutationJournal(m_Identity.SceneId, m_Identity.IncarnationId);
            m_Scheduler = new OutgoingScheduler(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, OriginDevice.Desktop) { ApplicationRetentionEnabled = true };
            m_Transport = new PersistentTransport(m_Scheduler, shouldProbeClock: () => m_Boundary.IsAnyTimelinePlaying, clockProbeInterval: TimelineClockEstimator.ProbeInterval, previewFrameWaiter: WaitForPreviewFrameAsync);
            m_Transport.ClockProbeSampleReceived += sample => m_TimelineClock.AddSampleIfPlaying(sample, m_Boundary.IsAnyTimelinePlaying);
            m_SurfaceInflation = new SurfaceInflationCoordinator(scene, m_Boundary, m_Identity, true, () => IsLive && !IsClosed, () => m_Authority.CanonicalSequence, SendSurfaceInflationControl);
            m_SurfaceInflation.PreparationHandler = (prepare, token) => SurfaceInflationLoading.PrepareAsync(m_SurfaceInflation, prepare, token);
            m_SurfaceInflation.RemoteJobStarted += completion => SurfaceInflationLoading.ObserveAsync(completion).Forget();
            m_OpenReplica = openReplica ?? ((host, pin, credential, token, transport) => QuestPairing.OpenReplicaAsync(host, pin, credential, token, async (stream, stop) =>
            {
                await ReplicaRoute.WriteAsync(stream, m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, stop).ConfigureAwait(false);
                await transport.RunConnectionAsync(stream, stop).ConfigureAwait(false);
            }));
            m_Authority.CanonicalReady += OnCanonicalReady;
            m_Authority.SessionMustDisconnect += OnAuthorityFailure;
            m_Scene.OnAddCut.AddListener(OnCutTopologyChanged);
            m_Scene.OnRemoveCut.AddListener(OnCutTopologyChanged);
            m_Scene.OnSurfaceRepresentationChanged.AddListener(OnUnsupportedResourceChanged);
            m_Scene.OnSelectCCEPSource.AddListener(OnUnsupportedResourceChanged);
            m_Scene.MRIManager.ResourceSelectionChanged += OnUnsupportedResourceChanged;
            if (m_Scene.MeshManager != null) m_Scene.MeshManager.ResourceSelectionChanged += OnUnsupportedResourceChanged;
            if (m_Scene.ImplantationManager != null) m_Scene.ImplantationManager.ResourceSelectionChanged += OnUnsupportedResourceChanged;
            Module3DMain.OnRemoveScene.AddListener(OnSceneRemoved);
            m_Scene.ActivityProjectionStartHandler = HandleLocalActivityProjectionStart;
            m_Scene.OnActivityProjectionCompleted.AddListener(OnActivityProjectionCompleted);
            m_Scene.OnProgressUpdateGenerator.AddListener(OnActivityProjectionProgress);
            m_Scene.OnProjectionRequestedChanged.AddListener(OnProjectionRequestedChanged);
            m_UserPreferences?.OnSavePreferences.AddListener(OnDesktopPreferencesSaved);
            m_SiteFilterRequestRegistration = SiteFilterRequestRouter.Register(scene, this, HandleLocalSiteFilterRequestAsync);
            m_CorrelationRequestRegistration = CorrelationRequestRouter.Register(scene, this, HandleLocalCorrelationRequestAsync);
        }

        public async Task StartAfterPublicationAsync(PreparedSceneDeliveryBinding binding, string host, byte[] pin, byte[] credential, CancellationToken stop)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            ValidateBinding(binding);
            stop.ThrowIfCancellationRequested();
            m_Boundary.BindPreparedResources(binding);
            lock (m_Gate)
            {
                ThrowIfAbortedLocked();
                if (m_InitialPublicationStarted) throw new InvalidOperationException("The initial sync publication has already started.");
                m_InitialPublicationStarted = true;
            }

            PublicationJournalResult journalResult = m_Journal.Complete(m_Boundary.CaptureCheckpoint, forceCheckpoint: true);
            ulong checkpointSequence = m_Authority.CanonicalSequence;
            m_Reconciliation = SceneReconciliationRecord.Retain(m_Scene, binding);
            m_Reconciliation.BeginPublication();
            m_InitialCommon = journalResult.Checkpoint;
            m_InitialCommonSequence = checkpointSequence;

            if (journalResult.Disposition == PublicationJournalDisposition.Replay)
            {
                lock (m_Gate)
                {
                    ThrowIfAbortedLocked();
                    m_State = PublicationState.AwaitingQuestApply;
                    foreach (CanonicalMutation mutation in journalResult.Mutations)
                        EnqueueCanonicalLocked(mutation);
                    EnqueueInitialBarrierLocked(checkpointSequence);
                }
            }
            else
            {
                lock (m_Gate)
                {
                    ThrowIfAbortedLocked();
                    m_State = PublicationState.EncodingCheckpoint;
                }

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stop, m_PublicationAbort.Token, m_Lifetime.Token);
                byte[] checkpointBytes = await Task.Run(() => SceneMutationCheckpointCodec.Encode(checkpointSequence, journalResult.Checkpoint), linked.Token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(linked.Token);
                lock (m_Gate)
                {
                    ThrowIfAbortedLocked();
                    if (m_PostCheckpointOverflow)
                    {
                        AbortLocked(true, "The mutation queue overflowed while the publication checkpoint was being encoded.");
                        throw new PublicationRestartException(m_FailureReason);
                    }

                    ScheduleDescriptor descriptor = ScheduleDescriptor.ForBarrier(m_Identity.SceneId, m_Identity.IncarnationId, null, BarrierScope.AllScene);
                    EnqueueResult queued = m_Transport.EnqueueSceneOperation(checkpointBytes, descriptor, structural: true, bodySchema: PublicationCheckpointBulkReceiver.BodySchema, operationId: CreateOperationId());
                    if (!queued.Accepted) throw new IOException("The initial publication checkpoint could not be retained by the sync transport: " + queued.Disposition + ".");
                    m_State = PublicationState.AwaitingQuestApply;
                    foreach (CanonicalMutation mutation in m_AfterCheckpoint)
                        EnqueueCanonicalLocked(mutation);
                    checkpointSequence = m_AfterCheckpoint.Count == 0 ? checkpointSequence : m_AfterCheckpoint[m_AfterCheckpoint.Count - 1].CanonicalSequence;
                    m_AfterCheckpoint.Clear();
                    EnqueueInitialBarrierLocked(checkpointSequence);
                }
            }

            m_ConnectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(m_Lifetime.Token);
            m_IncomingTask = ReadIncomingAsync(m_ConnectionLifetime.Token);
            Task initialConnection = m_OpenReplica(host, pin, credential, m_ConnectionLifetime.Token, m_Transport);
            m_ConnectionTask = MaintainConnectionAsync(initialConnection, host, pin, credential);
            _ = ObserveConnectionAsync(m_ConnectionTask);
            _ = ObserveIncomingAsync(m_IncomingTask, m_ConnectionLifetime.Token);
            Task cancellation = stop.CanBeCanceled ? Task.Delay(Timeout.Infinite, stop) : Task.Delay(Timeout.Infinite);
            Task completed = await Task.WhenAny(m_InitialApplyAcknowledged.Task, m_IncomingTask, cancellation).ConfigureAwait(false);
            if (completed == cancellation)
            {
                lock (m_Gate)
                    if (m_State != PublicationState.Live)
                        AbortLocked(false, "The initial sync publication was cancelled before Quest applied its barrier.");
                m_ConnectionLifetime.Cancel();
                stop.ThrowIfCancellationRequested();
            }

            if (completed == m_IncomingTask)
            {
                if (RequiresPublicationRestart) throw new PublicationRestartException(FailureReason);
                await completed.ConfigureAwait(false);
                throw new IOException("The sync mutation receiver ended before Quest applied the initial publication journal.");
            }

            OperationId acknowledgedBarrier;
            try
            {
                acknowledgedBarrier = await m_InitialApplyAcknowledged.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (RequiresPublicationRestart)
            {
                throw new PublicationRestartException(FailureReason);
            }
            catch (OperationCanceledException)
            {
                lock (m_Gate) ThrowIfAbortedLocked();
                throw new IOException("The sync replica connection ended before Quest applied the initial publication journal.");
            }

            lock (m_Gate)
            {
                ThrowIfAbortedLocked();
                if (!acknowledgedBarrier.Equals(m_InitialBarrierId))
                    throw new InvalidDataException("Quest acknowledged a different initial publication barrier.");
                m_State = PublicationState.Live;
            }

            PublishActivityProjectionPolicy();
        }

        private void ValidateBinding(PreparedSceneDeliveryBinding binding)
        {
            PreparedSceneIdentity bound = binding.CreateIdentity();
            if (!bound.SessionId.Equals(m_Identity.SessionId) || !bound.SceneId.Equals(m_Identity.SceneId) || !bound.IncarnationId.Equals(m_Identity.IncarnationId))
                throw new InvalidDataException("The published scene receipt belongs to a different sync scene incarnation.");
        }

        private void OnCanonicalReady(CanonicalMutation mutation)
        {
            lock (m_Gate)
            {
                if (m_State == PublicationState.Disposed || m_State == PublicationState.Aborted) return;
                m_Reconciliation?.QueueCanonical(mutation.CanonicalSequence, mutation.Mutation);
                if (m_State == PublicationState.Capturing)
                {
                    m_Journal.TryRecord(mutation);
                    return;
                }

                if (m_State == PublicationState.EncodingCheckpoint)
                {
                    if (m_AfterCheckpoint.Count == MaximumMutationsDuringCheckpointEncoding)
                    {
                        m_PostCheckpointOverflow = true;
                        AbortLocked(true, "Too many sync mutations arrived while the publication checkpoint was being encoded.");
                        return;
                    }

                    m_AfterCheckpoint.Add(mutation);
                    return;
                }

                EnqueueCanonicalLocked(mutation);
            }
        }

        private void EnqueueCanonicalLocked(CanonicalMutation mutation)
        {
            EnqueueResult queued = m_Transport.EnqueueMutation(mutation.Mutation, mutation.CanonicalSequence, null, coalesciblePreview: mutation.OriginDevice == OriginDevice.Desktop, operationId: mutation.OperationId);
            if (!queued.Accepted)
                AbortLocked(false, "The sync transport could not retain an accepted Desktop mutation: " + queued.Disposition + ".");
            else if (queued.ReplacedOperationId != null && !queued.ReplacedOperationId.Equals(mutation.OperationId))
            {
                m_Authority.RetireOperation(queued.ReplacedOperationId);
                PublishRetentionProgress();
            }
        }

        private void PublishRetentionProgress()
        {
            if (m_State == PublicationState.Disposed || m_State == PublicationState.Aborted) return;
            var progress = new RetentionProgress(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, OriginDevice.Desktop, m_PeerAppliedOriginThrough, m_PeerMinimumObservedSequence, m_Authority.RetiredCanonicalThrough, m_Authority.CanonicalSequence);
            if (!m_Transport.EnqueueRetentionProgress(progress).Accepted)
                throw new IOException("Desktop could not retain application completion progress.");
        }

        private async Task ProcessRetentionProgressAsync(RetentionProgress progress, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            progress.ValidateScope(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, OriginDevice.Quest);
            if (progress.AppliedOriginThrough < m_PeerAppliedOriginThrough || progress.MinimumObservedCanonicalSequence < m_PeerMinimumObservedSequence || progress.CurrentCanonicalSequence < m_PeerCurrentCanonicalSequence || progress.CurrentCanonicalSequence > m_Authority.CanonicalSequence)
                throw new InvalidDataException("Invalid Quest application completion progression.");
            foreach (OperationId id in m_Transport.RetireAppliedSceneOperations(progress.AppliedOriginThrough)) m_Authority.RetireOperation(id);
            m_Authority.AdvanceConflictFloor(progress.MinimumObservedCanonicalSequence);
            m_PeerAppliedOriginThrough = progress.AppliedOriginThrough;
            m_PeerMinimumObservedSequence = progress.MinimumObservedCanonicalSequence;
            m_PeerCurrentCanonicalSequence = progress.CurrentCanonicalSequence;
            m_Reconciliation?.ConfirmThrough(progress.CurrentCanonicalSequence);
            PublishRetentionProgress();
        }

        private void EnqueueInitialBarrierLocked(ulong canonicalSequence)
        {
            m_InitialBarrierId = CreateOperationId();
            m_InitialBarrierSequence = canonicalSequence;
            byte[] body = PublicationControlCodec.EncodeLiveBarrier(canonicalSequence);
            ScheduleDescriptor descriptor = ScheduleDescriptor.ForBarrier(m_Identity.SceneId, m_Identity.IncarnationId, null, BarrierScope.AllScene);
            EnqueueResult queued = m_Transport.EnqueueSceneOperation(body, descriptor, structural: true, operationId: m_InitialBarrierId);
            if (!queued.Accepted) throw new IOException("The sync transport could not retain the initial publication barrier: " + queued.Disposition + ".");
        }

        private static async Task WaitForPreviewFrameAsync(CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (Application.isPlaying) await UniTask.NextFrame(cancellationToken: stop);
            else await UniTask.Yield(PlayerLoopTiming.Initialization, stop);
            await UniTask.SwitchToThreadPool();
        }

        private async Task ReadIncomingAsync(CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                TransportRecord record = await m_Transport.ReadIncomingAsync(stop).ConfigureAwait(false);
                if (record == null) continue;
                if (record.Lane == ScheduleLane.SessionControl)
                {
                    await ProcessIncomingApplicationRecordAsync(record, stop).ConfigureAwait(false);
                    continue;
                }

                if (m_SceneOperationBulkReceiver.IsActive)
                {
                    if (m_SceneOperationBulkReceiver.TryAppend(record, out TransportRecord completedMutation))
                    {
                        if (completedMutation != null)
                        {
                            await ProcessIncomingApplicationRecordAsync(completedMutation, stop).ConfigureAwait(false);
                            await DrainDeferredIncomingAsync(stop).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        m_DeferredIncoming.Enqueue(record);
                    }

                    continue;
                }

                await ProcessIncomingApplicationRecordAsync(record, stop).ConfigureAwait(false);
                await DrainDeferredIncomingAsync(stop).ConfigureAwait(false);
            }
        }

        private async Task DrainDeferredIncomingAsync(CancellationToken stop)
        {
            while (true)
            {
                if (m_SceneOperationBulkReceiver.IsActive)
                {
                    if (!m_SceneOperationBulkReceiver.TryAppendNextBuffered(m_DeferredIncoming, record => record, out _, out TransportRecord completedMutation))
                        return;
                    if (completedMutation != null)
                        await ProcessIncomingApplicationRecordAsync(completedMutation, stop).ConfigureAwait(false);
                    continue;
                }

                if (m_DeferredIncoming.Count == 0) return;
                await ProcessIncomingApplicationRecordAsync(m_DeferredIncoming.Dequeue(), stop).ConfigureAwait(false);
            }
        }

        private async Task ProcessIncomingApplicationRecordAsync(TransportRecord record, CancellationToken stop)
        {
            if (record.Kind != TransportMessageKind.Application) return;
            byte[] payload = record.GetPayloadCopy();
            if (record.Lane == ScheduleLane.SessionControl)
            {
                if (SceneAction.TryDecode(payload, out var action))
                {
                    if (!action.Scene.Equals(m_Identity.SceneId) || !action.Incarnation.Equals(m_Identity.IncarnationId)) throw new InvalidDataException("Wrong scene action incarnation.");
                    await UniTask.SwitchToMainThread(stop);
                    ActionReceived?.Invoke(action);
                    return;
                }

                if (RetentionProgress.TryDecode(payload, out RetentionProgress progress))
                {
                    await ProcessRetentionProgressAsync(progress, stop).ConfigureAwait(false);
                    return;
                }

                if (PublicationControlCodec.TryDecodeAcknowledgement(payload, out OperationId barrierId))
                {
                    if (!barrierId.Equals(m_InitialBarrierId)) throw new InvalidDataException("Unexpected initial publication acknowledgement.");
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                    m_Authority.RetirePublishedCheckpoint(m_InitialBarrierSequence);
                    m_Reconciliation.InstallCommon(m_InitialCommon, m_InitialCommonSequence);
                    m_Reconciliation.ConfirmThrough(m_InitialBarrierSequence);
                    m_Reconciliation.SetStatus(ReconciliationStatus.Synchronized);
                    PublishRetentionProgress();
                    m_InitialApplyAcknowledged.TrySetResult(barrierId);
                    return;
                }

                if (CorrelationControlCodec.TryDecode(payload, out CorrelationControl correlationControl))
                    await ProcessCorrelationControlAsync(correlationControl, stop).ConfigureAwait(false);
                else if (SiteFilterControlCodec.TryDecode(payload, out SiteFilterControl control))
                    await ProcessSiteFilterControlAsync(control, stop).ConfigureAwait(false);
                else if (SurfaceInflationControlCodec.TryDecode(payload, out SurfaceInflationControl surfaceControl))
                {
                    await UniTask.SwitchToMainThread(stop);
                    m_SurfaceInflation.Receive(surfaceControl);
                }
                else if (ActivityProjectionControlCodec.TryDecode(payload, out ActivityProjectionControl projectionControl))
                    await ProcessActivityProjectionControlAsync(projectionControl, stop).ConfigureAwait(false);
                else
                    throw new InvalidDataException("Unsupported sync session-control application message.");

                return;
            }

            if (m_SceneOperationBulkReceiver.IsMutationDescriptor(record))
            {
                if (record.OriginDevice != OriginDevice.Quest || !record.ObservedCanonicalSequence.HasValue || record.CanonicalSequence.HasValue)
                    throw new InvalidDataException("A structural Desktop proposal must carry a Quest observed canonical sequence.");
                m_SceneOperationBulkReceiver.Begin(record);
                return;
            }

            if (record.Lane == ScheduleLane.SceneControl && record.BodySchema == SceneOperationBulkReceiver.BodySchema)
            {
                if (record.OriginDevice != OriginDevice.Quest || !record.ObservedCanonicalSequence.HasValue || record.CanonicalSequence.HasValue)
                    throw new InvalidDataException("A structural Desktop proposal must carry a Quest observed canonical sequence.");
                await AcceptQuestMutationAsync(record, MutationPayloadCodec.Decode(payload), stop).ConfigureAwait(false);
                return;
            }

            if (record.Lane != ScheduleLane.Interactive || record.OriginDevice != OriginDevice.Quest || !record.ObservedCanonicalSequence.HasValue || record.CanonicalSequence.HasValue || record.Mutation == null)
                throw new InvalidDataException("Unexpected sync Quest application record.");

            await AcceptQuestMutationAsync(record, record.Mutation, stop).ConfigureAwait(false);
        }

        private Task<bool> HandleLocalSiteFilterRequestAsync(SiteFilterRequest request, CancellationToken stop)
        {
            return RunSiteFilterJobAsync(CreateOperationId(), request, stop, questRequested: false);
        }

        private Task<bool> HandleLocalCorrelationRequestAsync(CorrelationRequest request, CancellationToken stop)
        {
            return RunCorrelationJobAsync(CreateOperationId(), request, stop, questRequested: false);
        }

        private async Task ProcessCorrelationControlAsync(CorrelationControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == CorrelationControlKind.Request)
            {
                CorrelationRequest request = CorrelationRequestCodec.DecodeComputeRequest(control.Command);
                TrackCorrelationTask(RunCorrelationJobAsync(control.JobId, request, stop, questRequested: true));
                return;
            }

            ActiveCorrelationJob active = m_ActiveCorrelationJob;
            if (active == null || !active.Identity.JobId.Equals(control.JobId) || (control.Generation != 0 && active.Identity.Generation != control.Generation)) return;
            switch (control.Kind)
            {
                case CorrelationControlKind.Cancel:
                    active.Cancellation.Cancel();
                    break;
                case CorrelationControlKind.Ready:
                    active.QuestReady.TrySetResult(true);
                    break;
                case CorrelationControlKind.Failed:
                    active.QuestFailure = new IOException("Quest could not apply the correlation result: " + control.FailureCode + ".");
                    m_CorrelationGenerations.Cancel(active.Identity);
                    m_Transport.CancelBulk(active.Identity.JobId);
                    active.Cancellation.Cancel();
                    break;
                default:
                    throw new InvalidDataException("Unexpected Quest correlation control kind.");
            }
        }

        private async Task ProcessSiteFilterControlAsync(SiteFilterControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == SiteFilterControlKind.Request)
            {
                SiteFilterRequest request = SiteFilterRequestCodec.Decode(control.Payload);
                Task<bool> operation = RunSiteFilterJobAsync(control.JobId, request, stop, questRequested: true);
                TrackSiteFilterTask(operation);
                return;
            }

            ActiveSiteFilterJob active = m_ActiveSiteFilterJob;
            if (active == null || !active.Identity.JobId.Equals(control.JobId) || (control.Generation != 0 && active.Identity.Generation != control.Generation))
                return;

            switch (control.Kind)
            {
                case SiteFilterControlKind.Cancel:
                    active.Cancellation.Cancel();
                    break;
                case SiteFilterControlKind.Ready:
                    active.QuestReady.TrySetResult(true);
                    break;
                case SiteFilterControlKind.Failed:
                    active.QuestFailure = new IOException("Quest could not apply the site-filter result: " + control.FailureCode + ".");
                    m_SiteFilterGenerations.Cancel(active.Identity);
                    m_Transport.CancelBulk(active.Identity.JobId);
                    active.Cancellation.Cancel();
                    break;
                default:
                    throw new InvalidDataException("Unexpected Quest site-filter control kind.");
            }
        }

        private bool HandleLocalActivityProjectionStart()
        {
            if (IsClosed) return false;
            if (!IsLive || m_ActiveActivityProjectionJob != null) return true;
            BeginDesktopActivityProjection(CreateOperationId(), m_Scene.ExplicitProjectionRequestPending);
            return true;
        }

        private async Task ProcessActivityProjectionControlAsync(ActivityProjectionControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            switch (control.Kind)
            {
                case ActivityProjectionControlKind.Request:
                    _ = ObserveRemoteActivityProjectionRequestAsync(control, stop);
                    return;
                case ActivityProjectionControlKind.RemoveRequest:
                    ApplyQuestActivityProjectionRemoval(control);
                    return;
                case ActivityProjectionControlKind.Progress:
                    if (MatchesActiveActivityProjection(control))
                        m_Scene.OnRemoteActivityProjectionProgress.Invoke(control.Progress, control.Message ?? string.Empty);
                    return;
                case ActivityProjectionControlKind.Cancel:
                    if (MatchesActiveActivityProjection(control))
                    {
                        m_ApplyingRemoteProjectionRequest = true;
                        try
                        {
                            m_Scene.SetProjectionEnabled(false);
                        }
                        finally
                        {
                            m_ApplyingRemoteProjectionRequest = false;
                        }

                        CancelDesktopActivityProjection(m_ActiveActivityProjectionJob, notifyPeer: false);
                    }
                    else m_CancelledActivityProjectionRequests.Add(control.JobId.Value);

                    return;
                case ActivityProjectionControlKind.Ready:
                case ActivityProjectionControlKind.Cancelled:
                case ActivityProjectionControlKind.Failed:
                    ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
                    if (!MatchesActiveActivityProjection(control)) return;
                    active.Terminals.MarkRemoteTerminal();
                    if (control.Kind == ActivityProjectionControlKind.Failed)
                    {
                        m_ActivityProjectionGenerations.Cancel(active.Identity);
                        CancelDesktopActivityProjection(active, notifyPeer: false);
                    }

                    TryFinishDesktopActivityProjection(active);
                    return;
                case ActivityProjectionControlKind.Policy:
                    throw new InvalidDataException("Quest cannot set the Desktop-owned activity-projection policy.");
                default:
                    throw new InvalidDataException("Unexpected activity-projection control kind.");
            }
        }

        private async Task ObserveRemoteActivityProjectionRequestAsync(ActivityProjectionControl request, CancellationToken stop)
        {
            try
            {
                while (!stop.IsCancellationRequested && m_Authority.CanonicalSequence < request.CanonicalSequence)
                {
                    if (m_CancelledActivityProjectionRequests.Remove(request.JobId.Value))
                    {
                        SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, ActivityProjectionControlKind.Cancelled);
                        return;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Initialization, stop);
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                }

                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                if (m_CancelledActivityProjectionRequests.Remove(request.JobId.Value))
                {
                    SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, ActivityProjectionControlKind.Cancelled);
                    return;
                }

                if (!IsLive || IsClosed)
                {
                    SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, ActivityProjectionControlKind.Failed, "desktop_not_live");
                    return;
                }

                if (m_ActiveActivityProjectionJob != null)
                {
                    SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, ActivityProjectionControlKind.Failed, "activity_projection_busy");
                    return;
                }

                BeginDesktopActivityProjection(request.JobId, request.ExplicitRequest, questRequested: true, requestedInputGeneration: request.InputGeneration);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                // A reconnect replays the reliable request after the canonical watermark is available.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, ActivityProjectionControlKind.Failed, "desktop_projection_start_failed");
            }
        }

        private void BeginDesktopActivityProjection(OperationId jobId, bool explicitRequest, bool questRequested = false, ulong requestedInputGeneration = 0)
        {
            if (m_ActiveActivityProjectionJob != null || !m_Scene.TryBeginCoordinatedActivityProjection(out IDisposable activityScope))
            {
                if (questRequested)
                    SendActivityProjectionTerminal(jobId, 1, requestedInputGeneration, ActivityProjectionControlKind.Failed, "activity_projection_busy");
                return;
            }

            ulong inputGeneration = m_Scene.ActivityInputGeneration;
            JobIdentity identity = m_ActivityProjectionGenerations.BeginJob(m_Identity.SceneId, m_Identity.IncarnationId, JobType.ActivityProjection, jobId, inputGeneration);
            if (identity == null)
            {
                activityScope.Dispose();
                SendActivityProjectionTerminal(jobId, 1, inputGeneration, ActivityProjectionControlKind.Failed, "projection_generation_exhausted");
                return;
            }

            var active = new ActiveActivityProjectionJob(identity, activityScope);
            m_ActiveActivityProjectionJob = active;
            try
            {
                bool automatic = AutomaticProjectionPolicyEnabled;
                SendActivityProjectionControl(new ActivityProjectionControl(ActivityProjectionControlKind.Started, identity.JobId, identity.Generation, identity.InputGeneration, m_Authority.CanonicalSequence, projectionRequested: true, automaticPolicyEnabled: automatic, explicitRequest: explicitRequest), DeliveryReliability.Reliable);
                m_Scene.SetCoordinatedAutomaticRecomputePolicy(automatic);
                if (!m_Scene.ProjectionRequested) m_Scene.RequestActivityProjection();
                if (!m_Scene.StartCoordinatedActivityProjection(explicitRequest))
                    CompleteLocalActivityProjection(active, ActivityProjectionCompletionKind.Failed);
                else
                    active.LocalProjectionGeneration = m_Scene.ProjectionGeneration;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CompleteLocalActivityProjection(active, ActivityProjectionCompletionKind.Failed);
            }
        }

        private void OnActivityProjectionCompleted(ulong projectionGeneration, ActivityProjectionCompletionKind completion)
        {
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active == null || active.LocalProjectionGeneration != projectionGeneration || active.Terminals.LocalTerminal) return;
            CompleteLocalActivityProjection(active, completion);
        }

        private void CompleteLocalActivityProjection(ActiveActivityProjectionJob active, ActivityProjectionCompletionKind completion)
        {
            if (!ReferenceEquals(active, m_ActiveActivityProjectionJob) || active.Terminals.LocalTerminal) return;
            active.Terminals.MarkLocalTerminal();
            ActivityProjectionControlKind kind = completion switch
            {
                ActivityProjectionCompletionKind.Ready => ActivityProjectionControlKind.Ready,
                ActivityProjectionCompletionKind.Cancelled => ActivityProjectionControlKind.Cancelled,
                _ => ActivityProjectionControlKind.Failed
            };
            string failureCode = kind == ActivityProjectionControlKind.Failed ? "desktop_projection_failed" : null;
            SendActivityProjectionTerminal(active.Identity.JobId, active.Identity.Generation, active.Identity.InputGeneration, kind, failureCode);
            TryFinishDesktopActivityProjection(active);
        }

        private void CancelDesktopActivityProjection(ActiveActivityProjectionJob active, bool notifyPeer)
        {
            if (active == null || !ReferenceEquals(active, m_ActiveActivityProjectionJob)) return;
            if (notifyPeer && active.Identity.Generation != 0)
            {
                try
                {
                    SendActivityProjectionControl(new ActivityProjectionControl(ActivityProjectionControlKind.Cancel, active.Identity.JobId, active.Identity.Generation, active.Identity.InputGeneration), DeliveryReliability.Reliable);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Desktop could not retain the activity-projection cancellation: " + exception.Message);
                }
            }

            m_Scene.CancelActivityProjection();
            if (!m_Scene.IsActivityProjectionComputing)
                CompleteLocalActivityProjection(active, ActivityProjectionCompletionKind.Cancelled);
        }

        private bool MatchesActiveActivityProjection(ActivityProjectionControl control)
        {
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            return active != null && active.Identity.JobId.Equals(control.JobId) && active.Identity.Generation == control.Generation;
        }

        private void TryFinishDesktopActivityProjection(ActiveActivityProjectionJob active)
        {
            if (!ReferenceEquals(active, m_ActiveActivityProjectionJob) || !active.Terminals.IsComplete) return;
            m_ActivityProjectionGenerations.Cancel(active.Identity);
            m_ActiveActivityProjectionJob = null;
            active.Dispose();
            if (IsClosed) RemoveActivityProjectionListeners();
        }

        private void OnActivityProjectionProgress(float progress, string message)
        {
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active == null || active.Terminals.LocalTerminal) return;
            SendActivityProjectionControl(new ActivityProjectionControl(ActivityProjectionControlKind.Progress, active.Identity.JobId, active.Identity.Generation, active.Identity.InputGeneration, progress: progress, message: message), DeliveryReliability.Ephemeral);
        }

        private void OnProjectionRequestedChanged(bool requested)
        {
            if (requested) return;
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active != null)
            {
                CancelDesktopActivityProjection(active, notifyPeer: !m_ApplyingRemoteProjectionRequest);
                return;
            }

            // Idle removal is still shared state. Desktop remains authoritative, so Quest receives
            // the same bounded policy control used at session start and preference changes.
            if (!m_ApplyingRemoteProjectionRequest)
                PublishActivityProjectionPolicy();
        }

        private void ApplyQuestActivityProjectionRemoval(ActivityProjectionControl control)
        {
            if (!IsLive || IsClosed) return;
            if (control.CanonicalSequence > m_Authority.CanonicalSequence)
                throw new InvalidDataException("A Quest activity-projection removal references a future canonical watermark.");

            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            m_ApplyingRemoteProjectionRequest = true;
            try
            {
                m_Scene.SetProjectionEnabled(false);
            }
            finally
            {
                m_ApplyingRemoteProjectionRequest = false;
            }

            if (active != null)
            {
                // A generation-free removal means Quest has no active projection job. Treat that
                // peer as terminal, but retain the scope until Desktop's native work also ends.
                active.Terminals.MarkRemoteTerminal();
                CancelDesktopActivityProjection(active, notifyPeer: false);
                TryFinishDesktopActivityProjection(active);
            }

            PublishActivityProjectionPolicy();
        }

        private void PublishActivityProjectionPolicy()
        {
            if (!IsLive || IsClosed) return;
            bool automatic = AutomaticProjectionPolicyEnabled;
            m_Scene.SetCoordinatedAutomaticRecomputePolicy(automatic);
            SendActivityProjectionControl(new ActivityProjectionControl(ActivityProjectionControlKind.Policy, CreateOperationId(), 0, projectionRequested: m_Scene.ProjectionRequested, automaticPolicyEnabled: automatic), DeliveryReliability.Reliable);
            // Base3DScene starts a stale projection from Update once its geometry and resources are ready.
            // Starting directly here can emit a false failure for a ready projection or unfinished geometry.
        }

        private void SendActivityProjectionTerminal(OperationId jobId, ulong generation, ulong inputGeneration, ActivityProjectionControlKind kind, string failureCode = null)
        {
            if (IsClosed) return;
            try
            {
                SendActivityProjectionControl(new ActivityProjectionControl(kind, jobId, generation, inputGeneration, failureCode: failureCode), DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Desktop could not retain an activity-projection terminal control: " + exception.Message);
            }
        }

        private void SendSurfaceInflationControl(SurfaceInflationControl control, bool ephemeral)
        {
            EnqueueResult queued = m_Transport.EnqueueSessionControl(SurfaceInflationControlCodec.Encode(control), ephemeral ? DeliveryReliability.Ephemeral : DeliveryReliability.Reliable);
            if (!ephemeral && !queued.Accepted)
                throw new IOException("The inflation control could not be queued.");
        }

        private void SendActivityProjectionControl(ActivityProjectionControl control, DeliveryReliability reliability)
        {
            EnqueueResult queued = m_Transport.EnqueueSessionControl(ActivityProjectionControlCodec.Encode(control), reliability);
            if (!queued.Accepted && reliability == DeliveryReliability.Reliable)
                throw new IOException("The Desktop could not retain an activity-projection session control: " + queued.Disposition + ".");
        }

        private void OnDesktopPreferencesSaved()
        {
            PublishActivityProjectionPolicy();
        }

        private bool AutomaticProjectionPolicyEnabled => m_UserPreferences?.Visualization?._3D?.AutomaticEEGUpdate ?? false;

        private void TrackSiteFilterTask(Task<bool> task)
        {
            lock (m_Gate) m_SiteFilterTasks.Add(task);
            _ = ObserveSiteFilterTaskAsync(task);
        }

        private void TrackCorrelationTask(Task<bool> task)
        {
            lock (m_Gate) m_CorrelationTasks.Add(task);
            _ = ObserveCorrelationTaskAsync(task);
        }

        private async Task ObserveCorrelationTaskAsync(Task<bool> task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                /* The job sends a typed terminal control before it completes. */
            }
            finally
            {
                lock (m_Gate) m_CorrelationTasks.Remove(task);
            }
        }

        private async Task<bool> RunCorrelationJobAsync(OperationId jobId, CorrelationRequest request, CancellationToken requestStop, bool questRequested)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (request == null) throw new ArgumentNullException(nameof(request));
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, requestStop);
            if (!IsLive || IsClosed)
            {
                if (questRequested) SendCorrelationFailure(jobId, 0, "desktop_not_live");
                throw new IOException("The Desktop scene session is not live yet; retry the correlation job when synchronization is ready.");
            }

            if (m_ActiveCorrelationJob != null || m_ActiveSiteFilterJob != null)
            {
                if (questRequested) SendCorrelationFailure(jobId, 0, "correlation_busy");
                throw new InvalidOperationException("A scene-wide filter or correlation job is already active.");
            }

            if (!m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope))
            {
                if (questRequested) SendCorrelationFailure(jobId, 0, "scene_busy");
                throw new InvalidOperationException("Correlations cannot start while the scene is updating its activity projection.");
            }

            JobIdentity identity = m_CorrelationGenerations.BeginJob(m_Identity.SceneId, m_Identity.IncarnationId, JobType.Correlation, jobId);
            if (identity == null)
            {
                activityScope.Dispose();
                if (questRequested) SendCorrelationFailure(jobId, 0, "generation_unavailable");
                throw new InvalidOperationException("The Desktop could not allocate a new correlation generation.");
            }

            JobAttempt attempt = m_CorrelationGenerations.BeginAttempt(identity);
            if (attempt == null)
            {
                activityScope.Dispose();
                if (questRequested) SendCorrelationFailure(jobId, identity.Generation, "attempt_unavailable");
                throw new InvalidOperationException("The Desktop could not start the correlation generation.");
            }

            var active = new ActiveCorrelationJob(identity, attempt, CancellationTokenSource.CreateLinkedTokenSource(requestStop, m_Lifetime.Token), activityScope);
            m_ActiveCorrelationJob = active;
            EnqueueResult started = m_Transport.EnqueueSessionControl(CorrelationControlCodec.Encode(new CorrelationControl(CorrelationControlKind.Started, identity.JobId, identity.Generation)), DeliveryReliability.Reliable);
            if (!started.Accepted)
            {
                m_ActiveCorrelationJob = null;
                active.Dispose();
                if (questRequested) SendCorrelationFailure(jobId, identity.Generation, "start_control_rejected");
                throw new IOException("Desktop could not notify Quest that correlation processing started.");
            }

            try
            {
                async UniTask Evaluate(Action<float, float, LoadingText> update, CancellationToken token)
                {
                    byte[] resultBytes;
                    if (request.Kind == CorrelationCommandKind.Load)
                    {
                        resultBytes = request.ResultBytes;
                        CorrelationResultResource.Decode(resultBytes).ValidateFor(m_Scene, m_Scene.Columns.SelectMany(column => column.Sites).Select(site => site.Information.FullID).ToArray());
                    }
                    else
                    {
                        IReadOnlyList<CorrelationResultData> computed = await m_Scene.ComputeCorrelationResultsAsync(update, token);
                        token.ThrowIfCancellationRequested();
                        if (!m_CorrelationGenerations.IsCurrent(attempt)) throw new OperationCanceledException(token);
                        resultBytes = CorrelationResultResource.FromResults(m_Scene, computed).Encode();
                    }

                    token.ThrowIfCancellationRequested();
                    if (!m_CorrelationGenerations.IsCurrent(attempt)) throw new OperationCanceledException(token);
                    SetCorrelationResult result = m_Boundary.CreateCorrelationResult(identity.JobId, identity.Generation, resultBytes);
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, token);
                    if (!m_CorrelationGenerations.IsCurrent(attempt)) throw new OperationCanceledException(token);
                    m_Boundary.Apply(result, MutationApplicationOrigin.LocalDesktop, identity.JobId);
                    await WaitWithCancellationAsync(active.QuestReady.Task, token);
                }

                if (request.ExternalLoadingIndicator) await Evaluate(request.Progress, active.Cancellation.Token);
                else await LoadingManager.LoadDelayedAsync(Evaluate, active.Cancellation.Token, showInformations: false);
                return true;
            }
            catch (OperationCanceledException)
            {
                m_CorrelationGenerations.Cancel(identity);
                m_Transport.CancelBulk(identity.JobId);
                if (active.QuestFailure != null) throw active.QuestFailure;
                SendCorrelationTerminalControl(CorrelationControlKind.Cancel, identity, null);
                throw;
            }
            catch
            {
                m_CorrelationGenerations.Cancel(identity);
                m_Transport.CancelBulk(identity.JobId);
                if (active.QuestFailure != null) throw active.QuestFailure;
                SendCorrelationTerminalControl(CorrelationControlKind.Failed, identity, "desktop_correlation_failed");
                throw;
            }
            finally
            {
                if (ReferenceEquals(m_ActiveCorrelationJob, active))
                {
                    m_ActiveCorrelationJob = null;
                    active.Dispose();
                }
            }
        }

        private void SendCorrelationTerminalControl(CorrelationControlKind kind, JobIdentity identity, string failureCode)
        {
            if (IsClosed) return;
            try
            {
                m_Transport.EnqueueSessionControl(CorrelationControlCodec.Encode(new CorrelationControl(kind, identity.JobId, identity.Generation, failureCode: failureCode)), DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void SendCorrelationFailure(OperationId jobId, ulong generation, string failureCode)
        {
            if (IsClosed) return;
            try
            {
                m_Transport.EnqueueSessionControl(CorrelationControlCodec.Encode(new CorrelationControl(CorrelationControlKind.Failed, jobId, generation, failureCode: failureCode)), DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private sealed class ActiveCorrelationJob : IDisposable
        {
            public JobIdentity Identity { get; }
            public JobAttempt Attempt { get; }
            public CancellationTokenSource Cancellation { get; }
            public IDisposable ActivityScope { get; }
            public TaskCompletionSource<bool> QuestReady { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public Exception QuestFailure { get; set; }

            public ActiveCorrelationJob(JobIdentity identity, JobAttempt attempt, CancellationTokenSource cancellation, IDisposable activityScope)
            {
                Identity = identity;
                Attempt = attempt;
                Cancellation = cancellation;
                ActivityScope = activityScope;
            }

            public void Dispose()
            {
                Cancellation.Cancel();
                Cancellation.Dispose();
                ActivityScope.Dispose();
                QuestReady.TrySetCanceled();
            }
        }

        private async Task ObserveSiteFilterTaskAsync(Task<bool> task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                // RunSiteFilterJobAsync sends the typed terminal failure before it completes.
            }
            finally
            {
                lock (m_Gate) m_SiteFilterTasks.Remove(task);
            }
        }

        private async Task<bool> RunSiteFilterJobAsync(OperationId jobId, SiteFilterRequest request, CancellationToken requestStop, bool questRequested)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (request == null) throw new ArgumentNullException(nameof(request));
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, requestStop);
            if (!IsLive || IsClosed)
            {
                if (questRequested)
                {
                    SendSiteFilterFailure(jobId, 0, "desktop_not_live");
                    return false;
                }

                throw new IOException("The Desktop scene session is not live yet; retry the filter when synchronization is ready.");
            }

            if (m_ActiveSiteFilterJob != null || m_ActiveCorrelationJob != null)
            {
                if (questRequested) SendSiteFilterFailure(jobId, 0, "site_filter_busy");
                throw new InvalidOperationException("A site-filter or correlation job is already active for this scene.");
            }

            if (!m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope))
            {
                if (questRequested) SendSiteFilterFailure(jobId, 0, "scene_busy");
                throw new InvalidOperationException("Site filtering cannot start while the scene is updating its activity projection.");
            }

            JobIdentity identity = m_SiteFilterGenerations.BeginJob(m_Identity.SceneId, m_Identity.IncarnationId, JobType.Filter, jobId);
            if (identity == null)
            {
                activityScope.Dispose();
                if (questRequested) SendSiteFilterFailure(jobId, 0, "generation_unavailable");
                throw new InvalidOperationException("The Desktop could not allocate a new site-filter generation.");
            }

            JobAttempt attempt = m_SiteFilterGenerations.BeginAttempt(identity);
            if (attempt == null)
            {
                activityScope.Dispose();
                if (questRequested) SendSiteFilterFailure(jobId, identity.Generation, "attempt_unavailable");
                throw new InvalidOperationException("The Desktop could not start the site-filter generation.");
            }

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(requestStop, m_Lifetime.Token);
            var active = new ActiveSiteFilterJob(identity, attempt, cancellation, activityScope);
            m_ActiveSiteFilterJob = active;
            EnqueueResult started = m_Transport.EnqueueSessionControl(SiteFilterControlCodec.Encode(new SiteFilterControl(SiteFilterControlKind.Started, identity.JobId, identity.Generation)), DeliveryReliability.Reliable);
            if (!started.Accepted)
            {
                m_ActiveSiteFilterJob = null;
                active.Dispose();
                if (questRequested) SendSiteFilterFailure(jobId, identity.Generation, "start_control_rejected");
                throw new IOException("Desktop could not notify Quest that site filtering started.");
            }

            try
            {
                Func<Action<float, float, LoadingText>, CancellationToken, UniTask> evaluate = async (update, token) =>
                {
                    SiteFilterEvaluationResult evaluation = await SiteFilterEvaluator.EvaluateAsync(m_Scene, m_Boundary, request, progress => update?.Invoke(progress, 0f, new LoadingText("Filtering sites")), token);
                    token.ThrowIfCancellationRequested();
                    if (!m_SiteFilterGenerations.IsCurrent(attempt)) throw new OperationCanceledException(token);
                    Mutation result = m_Boundary.CreateSiteFilterResult(identity.JobId, identity.Generation, evaluation.Included, evaluation.RosterHash);
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, token);
                    if (!m_SiteFilterGenerations.IsCurrent(attempt)) throw new OperationCanceledException(token);
                    m_Boundary.Apply(result, MutationApplicationOrigin.LocalDesktop, identity.JobId);
                    await WaitWithCancellationAsync(active.QuestReady.Task, token);
                };
                if (request.ExternalLoadingIndicator) await evaluate(null, cancellation.Token);
                else await LoadingManager.LoadDelayedAsync(evaluate, cancellation.Token, showInformations: false);
                return true;
            }
            catch (OperationCanceledException)
            {
                m_SiteFilterGenerations.Cancel(identity);
                m_Transport.CancelBulk(identity.JobId);
                if (active.QuestFailure != null) throw active.QuestFailure;
                SendSiteFilterTerminalControl(SiteFilterControlKind.Cancel, identity, null);
                throw;
            }
            catch
            {
                m_SiteFilterGenerations.Cancel(identity);
                m_Transport.CancelBulk(identity.JobId);
                if (active.QuestFailure != null) throw active.QuestFailure;
                SendSiteFilterTerminalControl(SiteFilterControlKind.Failed, identity, "desktop_filter_failed");
                throw;
            }
            finally
            {
                if (ReferenceEquals(m_ActiveSiteFilterJob, active))
                {
                    m_ActiveSiteFilterJob = null;
                    active.Dispose();
                }
            }
        }

        private static async Task WaitWithCancellationAsync(Task task, CancellationToken stop)
        {
            Task cancelled = Task.Delay(Timeout.Infinite, stop);
            Task completed = await Task.WhenAny(task, cancelled).ConfigureAwait(false);
            if (completed == task || task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                return;
            }

            stop.ThrowIfCancellationRequested();
        }

        private void SendSiteFilterTerminalControl(SiteFilterControlKind kind, JobIdentity identity, string failureCode)
        {
            if (IsClosed) return;
            try
            {
                byte[] payload = SiteFilterControlCodec.Encode(new SiteFilterControl(kind, identity.JobId, identity.Generation, failureCode: failureCode));
                m_Transport.EnqueueSessionControl(payload, DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void SendSiteFilterFailure(OperationId jobId, ulong generation, string failureCode)
        {
            if (IsClosed) return;
            try
            {
                byte[] payload = SiteFilterControlCodec.Encode(new SiteFilterControl(SiteFilterControlKind.Failed, jobId, generation, failureCode: failureCode));
                m_Transport.EnqueueSessionControl(payload, DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private sealed class ActiveSiteFilterJob : IDisposable
        {
            public JobIdentity Identity { get; }
            public JobAttempt Attempt { get; }
            public CancellationTokenSource Cancellation { get; }
            public IDisposable ActivityScope { get; }
            public TaskCompletionSource<bool> QuestReady { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public Exception QuestFailure { get; set; }

            public ActiveSiteFilterJob(JobIdentity identity, JobAttempt attempt, CancellationTokenSource cancellation, IDisposable activityScope)
            {
                Identity = identity;
                Attempt = attempt;
                Cancellation = cancellation;
                ActivityScope = activityScope;
            }

            public void Dispose()
            {
                Cancellation.Cancel();
                Cancellation.Dispose();
                ActivityScope.Dispose();
                QuestReady.TrySetCanceled();
            }
        }

        private sealed class ActiveActivityProjectionJob : IDisposable
        {
            public JobIdentity Identity { get; }
            public IDisposable ActivityScope { get; }
            public ActivityProjectionTerminalBarrier Terminals { get; } = new ActivityProjectionTerminalBarrier();
            public ulong LocalProjectionGeneration { get; set; }

            public ActiveActivityProjectionJob(JobIdentity identity, IDisposable activityScope)
            {
                Identity = identity;
                ActivityScope = activityScope;
            }

            public void Dispose() => ActivityScope.Dispose();
        }

        private async Task AcceptQuestMutationAsync(TransportRecord record, Mutation mutation, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (!record.SceneId.Equals(m_Identity.SceneId) || !record.IncarnationId.Equals(m_Identity.IncarnationId)) throw new InvalidDataException("Wrong Quest proposal incarnation.");
            DesktopProposalResult result = m_Authority.AcceptQuestProposal(record.MessageId, mutation, record.ObservedCanonicalSequence.Value, record.OriginSequence);
            if (result.Correction != null)
                EnqueueQuestProposalDecision(record, result.Correction.AuthoritativeMutation, QuestProposalDecisionCodec.EncodeCorrection(result.Correction));
            else if (result.CanonicalMutation == null && result.Outcome != ProposalOutcome.Duplicate)
                EnqueueQuestProposalDecision(record, mutation, QuestProposalDecisionCodec.EncodeRejection(record.MessageId, result.RejectionCode ?? "proposal_rejected"));
        }

        private void EnqueueQuestProposalDecision(TransportRecord proposal, Mutation mutation, byte[] body)
        {
            ScheduleDescriptor descriptor = ScheduleDescriptor.ForMutation(m_Identity.SceneId, m_Identity.IncarnationId, mutation);
            EnqueueResult queued = m_Transport.EnqueueSceneOperation(body, descriptor, bodySchema: QuestProposalDecisionCodec.BodySchema, operationId: proposal.MessageId);
            if (!queued.Accepted)
                throw new IOException("The sync transport could not retain the Quest proposal decision: " + queued.Disposition + ".");
        }

        private async Task ObserveConnectionAsync(Task connection)
        {
            try
            {
                await connection.ConfigureAwait(false);
                if (!m_Lifetime.IsCancellationRequested) MarkConnectionClosed("The Quest sync replica connection ended.");
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (m_Lifetime.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                MarkConnectionClosed("The Quest sync replica connection failed: " + exception.Message);
                Debug.LogWarning("Quest sync replica disconnected: " + exception.Message);
            }
        }

        private async Task MaintainConnectionAsync(Task connection, string host, byte[] pin, byte[] credential)
        {
            Task activeConnection = connection;
            while (!m_ConnectionLifetime.IsCancellationRequested)
            {
                try
                {
                    await activeConnection.ConfigureAwait(false);
                    if (!m_ConnectionLifetime.IsCancellationRequested)
                        MarkConnectionClosed("The Quest sync replica connection ended.");
                    return;
                }
                catch (OperationCanceledException) when (m_ConnectionLifetime.IsCancellationRequested || m_Lifetime.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception) when (IsTransientConnectionFailure(exception) && m_Transport.State == PersistentTransportState.DisconnectedGrace)
                {
                    Debug.LogWarning("Quest sync replica disconnected; reconnecting within the 500 ms grace period: " + exception.Message);
                }
                catch (Exception exception)
                {
                    MarkConnectionClosed("The Quest sync replica connection failed: " + exception.Message);
                    Debug.LogWarning("Quest sync replica disconnected: " + exception.Message);
                    return;
                }

                Stopwatch grace = Stopwatch.StartNew();
                bool resumed = false;
                while (!m_ConnectionLifetime.IsCancellationRequested && m_Transport.State == PersistentTransportState.DisconnectedGrace)
                {
                    TimeSpan remaining = TimeSpan.FromMilliseconds(OutgoingScheduler.ReconnectGraceMilliseconds) - grace.Elapsed;
                    if (remaining <= TimeSpan.Zero) break;

                    try
                    {
                        Task reconnect = await OpenWithinGraceAsync(host, pin, credential, remaining).ConfigureAwait(false);
                        if (reconnect != null)
                        {
                            activeConnection = reconnect;
                            resumed = true;
                            break;
                        }
                    }
                    catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested || IsClosed)
                    {
                        return;
                    }
                    catch (OperationCanceledException) when (m_ConnectionLifetime.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception) when (IsTransientConnectionFailure(exception) && m_Transport.State == PersistentTransportState.DisconnectedGrace)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(25), m_Lifetime.Token).ConfigureAwait(false);
                    }
                }

                if (resumed) continue;
                if (m_Lifetime.IsCancellationRequested || IsClosed) return;
                MarkConnectionClosed("The Quest sync replica reconnect grace expired.");
                return;
            }
        }

        private async Task<Task> OpenWithinGraceAsync(string host, byte[] pin, byte[] credential, TimeSpan remaining)
        {
            m_ConnectionLifetime.CancelAfter(remaining);
            Task connection = m_OpenReplica(host, pin, credential, m_ConnectionLifetime.Token, m_Transport);
            while (!connection.IsCompleted)
            {
                // A TLS socket can open while Quest has already retired this live owner.
                // Only a confirmed resume may end this attempt's bounded grace period.
                if (m_Transport.IsConnectionReady && !connection.IsCompleted)
                {
                    m_ConnectionLifetime.CancelAfter(Timeout.InfiniteTimeSpan);
                    return connection;
                }

                Task tick = Task.Delay(TimeSpan.FromMilliseconds(10), m_ConnectionLifetime.Token);
                if (await Task.WhenAny(connection, tick).ConfigureAwait(false) == tick)
                    await tick.ConfigureAwait(false);
            }

            await connection.ConfigureAwait(false);
            return null;
        }

        private static bool IsTransientConnectionFailure(Exception exception) => exception is IOException || exception is SocketException || exception is ObjectDisposedException;

        private async Task ObserveIncomingAsync(Task incoming, CancellationToken stop)
        {
            try
            {
                await incoming.ConfigureAwait(false);
                if (!m_Lifetime.IsCancellationRequested) MarkConnectionClosed("The Quest sync mutation receiver ended.");
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                MarkConnectionClosed("The Quest sync mutation receiver failed: " + exception.Message);
                Debug.LogError("Quest sync mutation receive failed: " + exception);
            }
        }

        private void MarkConnectionClosed(string reason)
        {
            lock (m_Gate)
            {
                if (m_State == PublicationState.AwaitingQuestApply || m_State == PublicationState.Live)
                    AbortLocked(false, reason);
            }

            CancelActivityProjectionAfterDisconnectAsync().Forget();
        }

        private void OnAuthorityFailure(string reason)
        {
            lock (m_Gate) AbortLocked(false, "The sync Desktop authority stopped: " + reason + ".");
            CancelActivityProjectionAfterDisconnectAsync().Forget();
        }

        private async UniTaskVoid CancelActivityProjectionAfterDisconnectAsync()
        {
            await UniTask.SwitchToMainThread();
            if (m_Reconciliation?.Status == ReconciliationStatus.Synchronized) m_Reconciliation.SetStatus(ReconciliationStatus.Local, "Connection interrupted; local changes will be merged on reconnection.");
            m_SurfaceInflation.Cancel();
            m_Scene.ClearCoordinatedAutomaticRecomputePolicy();
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active == null) return;
            active.Terminals.MarkRemoteTerminal();
            CancelDesktopActivityProjection(active, notifyPeer: false);
            TryFinishDesktopActivityProjection(active);
        }

        private void OnCutTopologyChanged(Core.Object3D.Cut cut) => AbortForUnsupportedMutation("Cut membership changed during initial publication.");
        private void OnUnsupportedResourceChanged() => AbortForUnsupportedMutation("A resource selection changed during initial publication.");
        private void OnUnsupportedResourceChanged(Core.Object3D.SurfaceRepresentation _) => AbortForUnsupportedMutation("A surface representation changed during initial publication.");

        private void OnSceneRemoved(Base3DScene removed)
        {
            if (removed != m_Scene) return;
            lock (m_Gate)
                AbortLocked(false, "The source scene was closed during initial publication.");
        }

        private void AbortForUnsupportedMutation(string reason)
        {
            lock (m_Gate)
            {
                if (m_State == PublicationState.Live || m_State == PublicationState.Disposed || m_State == PublicationState.Aborted) return;
                AbortLocked(true, reason);
            }
        }

        private void AbortLocked(bool requiresRestart, string reason)
        {
            if (m_State == PublicationState.Aborted || m_State == PublicationState.Disposed) return;
            m_State = PublicationState.Aborted;
            m_AbortRequiresRestart = requiresRestart;
            m_FailureReason = reason;
            m_PublicationAbort.Cancel();
            m_ConnectionLifetime?.Cancel();
            m_InitialApplyAcknowledged.TrySetCanceled();
        }

        private void ThrowIfAbortedLocked()
        {
            if (m_State == PublicationState.Aborted)
            {
                if (m_AbortRequiresRestart) throw new PublicationRestartException(m_FailureReason);
                throw new InvalidOperationException(m_FailureReason ?? "The sync Desktop replica session has stopped.");
            }
        }

        private static OperationId CreateOperationId() => new OperationId(Guid.NewGuid());

        private void RemoveTopologyListeners()
        {
            m_Scene.OnAddCut.RemoveListener(OnCutTopologyChanged);
            m_Scene.OnRemoveCut.RemoveListener(OnCutTopologyChanged);
            m_Scene.OnSurfaceRepresentationChanged.RemoveListener(OnUnsupportedResourceChanged);
            m_Scene.OnSelectCCEPSource.RemoveListener(OnUnsupportedResourceChanged);
            m_Scene.MRIManager.ResourceSelectionChanged -= OnUnsupportedResourceChanged;
            if (m_Scene.MeshManager != null) m_Scene.MeshManager.ResourceSelectionChanged -= OnUnsupportedResourceChanged;
            if (m_Scene.ImplantationManager != null) m_Scene.ImplantationManager.ResourceSelectionChanged -= OnUnsupportedResourceChanged;
            Module3DMain.OnRemoveScene.RemoveListener(OnSceneRemoved);
        }

        public void Dispose()
        {
            lock (m_Gate)
            {
                if (m_State == PublicationState.Disposed) return;
                m_State = PublicationState.Disposed;
            }

            RemoveTopologyListeners();
            m_Authority.CanonicalReady -= OnCanonicalReady;
            m_Authority.SessionMustDisconnect -= OnAuthorityFailure;
            m_UserPreferences?.OnSavePreferences.RemoveListener(OnDesktopPreferencesSaved);
            if (m_Scene.ActivityProjectionStartHandler == HandleLocalActivityProjectionStart)
                m_Scene.ActivityProjectionStartHandler = null;
            m_Scene.ClearCoordinatedAutomaticRecomputePolicy();
            if (m_ActiveActivityProjectionJob != null)
            {
                ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
                active.Terminals.MarkRemoteTerminal();
                CancelDesktopActivityProjection(active, notifyPeer: false);
                TryFinishDesktopActivityProjection(active);
            }

            if (m_ActiveActivityProjectionJob == null) RemoveActivityProjectionListeners();
            m_Lifetime.Cancel();
            m_PublicationAbort.Cancel();
            m_ConnectionLifetime?.Cancel();
            m_SiteFilterRequestRegistration?.Dispose();
            m_CorrelationRequestRegistration?.Dispose();
            if (m_ActiveSiteFilterJob != null)
            {
                ActiveSiteFilterJob active = m_ActiveSiteFilterJob;
                m_ActiveSiteFilterJob = null;
                m_SiteFilterGenerations.Cancel(active.Identity);
                m_Transport.CancelBulk(active.Identity.JobId);
                SendSiteFilterTerminalControl(SiteFilterControlKind.Cancel, active.Identity, null);
                active.Dispose();
            }

            if (m_ActiveCorrelationJob != null)
            {
                ActiveCorrelationJob active = m_ActiveCorrelationJob;
                m_ActiveCorrelationJob = null;
                m_CorrelationGenerations.Cancel(active.Identity);
                m_Transport.CancelBulk(active.Identity.JobId);
                SendCorrelationTerminalControl(CorrelationControlKind.Cancel, active.Identity, null);
                active.Dispose();
            }

            m_SurfaceInflation.Dispose();
            m_Transport.Dispose();
            m_SceneOperationBulkReceiver.Reset();
            m_DeferredIncoming.Clear();
            m_Authority.Dispose();
            m_Boundary.Dispose();
            m_Lifetime.Dispose();
            m_PublicationAbort.Dispose();
            m_ConnectionLifetime?.Dispose();
        }

        private void RemoveActivityProjectionListeners()
        {
            if (m_ProjectionEventsRemoved) return;
            m_ProjectionEventsRemoved = true;
            m_Scene.OnActivityProjectionCompleted.RemoveListener(OnActivityProjectionCompleted);
            m_Scene.OnProgressUpdateGenerator.RemoveListener(OnActivityProjectionProgress);
            m_Scene.OnProjectionRequestedChanged.RemoveListener(OnProjectionRequestedChanged);
        }

        internal sealed class PublicationRestartException : OperationCanceledException
        {
            public PublicationRestartException(string message) : base(message)
            {
            }
        }
    }
}
