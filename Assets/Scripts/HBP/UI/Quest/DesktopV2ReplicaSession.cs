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
    internal sealed class DesktopV2ReplicaSession : IDisposable
    {
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
        private readonly UserPreferences m_UserPreferences;
        private readonly V2PreparedSceneIdentity m_Identity;
        private readonly V2SceneMutationBoundary m_Boundary;
        private readonly V2TimelineClockEstimator m_TimelineClock;
        private readonly V2DesktopMutationAuthority m_Authority;
        private readonly V2PublicationMutationJournal m_Journal;
        private readonly V2OutgoingScheduler m_Scheduler;
        private readonly V2PersistentTransport m_Transport;
        private readonly V2SceneOperationBulkReceiver m_SceneOperationBulkReceiver = new V2SceneOperationBulkReceiver();
        private readonly System.Collections.Generic.Queue<V2TransportRecord> m_DeferredIncoming = new System.Collections.Generic.Queue<V2TransportRecord>();
        private readonly Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task> m_OpenReplica;
        private readonly CancellationTokenSource m_Lifetime = new CancellationTokenSource();
        private readonly CancellationTokenSource m_PublicationAbort = new CancellationTokenSource();
        private readonly TaskCompletionSource<OperationId> m_InitialApplyAcknowledged = new TaskCompletionSource<OperationId>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly V2JobGenerationRegistry m_SiteFilterGenerations = new V2JobGenerationRegistry();
        private readonly V2JobGenerationRegistry m_CorrelationGenerations = new V2JobGenerationRegistry();
        private readonly V2JobGenerationRegistry m_ActivityProjectionGenerations = new V2JobGenerationRegistry();
        private readonly System.Collections.Generic.HashSet<Task> m_SiteFilterTasks = new System.Collections.Generic.HashSet<Task>();
        private readonly System.Collections.Generic.HashSet<Task> m_CorrelationTasks = new System.Collections.Generic.HashSet<Task>();
        private readonly System.Collections.Generic.List<V2CanonicalMutation> m_AfterCheckpoint = new System.Collections.Generic.List<V2CanonicalMutation>();
        private PublicationState m_State = PublicationState.Capturing;
        private bool m_AbortRequiresRestart;
        private bool m_InitialPublicationStarted;
        private bool m_PostCheckpointOverflow;
        private OperationId m_InitialBarrierId;
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

        public DesktopV2ReplicaSession(Base3DScene scene, string globalContextId, string transferId) : this(scene, globalContextId, transferId, null)
        {
        }

        internal DesktopV2ReplicaSession(Base3DScene scene, string globalContextId, string transferId, Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task> openReplica)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            m_Scene = scene;
            m_UserPreferences = PersistentDataManager.IsInitialized ? PersistentDataManager.UserPreferences : null;
            m_Identity = V2PreparedSceneIdentity.Create(globalContextId, scene.Visualization.ID, transferId);
            m_TimelineClock = new V2TimelineClockEstimator(StopwatchMonotonicClock.Instance);
            m_Boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Desktop, timelineTimingEstimate: anchor => m_TimelineClock.TryEstimate(anchor.MonotonicAnchorTicks, anchor.TickFrequency, anchor.Step, out V2TimelineAnchorTimingEstimate estimate) ? estimate : (V2TimelineAnchorTimingEstimate?)null);
            m_Authority = new V2DesktopMutationAuthority(m_Identity.SceneId, m_Identity.IncarnationId, m_Boundary);
            m_Journal = new V2PublicationMutationJournal(m_Identity.SceneId, m_Identity.IncarnationId);
            m_Scheduler = new V2OutgoingScheduler(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, V2OriginDevice.Desktop);
            m_Transport = new V2PersistentTransport(m_Scheduler, shouldProbeClock: () => m_Boundary.IsAnyTimelinePlaying, clockProbeInterval: V2TimelineClockEstimator.ProbeInterval);
            m_Transport.ClockProbeSampleReceived += sample => m_TimelineClock.AddSampleIfPlaying(sample, m_Boundary.IsAnyTimelinePlaying);
            m_OpenReplica = openReplica ?? QuestPairing.OpenV2ReplicaAsync;
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
            m_SiteFilterRequestRegistration = V2SiteFilterRequestRouter.Register(scene, this, HandleLocalSiteFilterRequestAsync);
            m_CorrelationRequestRegistration = V2CorrelationRequestRouter.Register(scene, this, HandleLocalCorrelationRequestAsync);
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
                if (m_InitialPublicationStarted) throw new InvalidOperationException("The initial v2 publication has already started.");
                m_InitialPublicationStarted = true;
            }

            V2PublicationJournalResult journalResult = m_Journal.Complete(m_Boundary.CaptureCheckpoint);
            ulong checkpointSequence = m_Authority.CanonicalSequence;

            if (journalResult.Disposition == V2PublicationJournalDisposition.Replay)
            {
                lock (m_Gate)
                {
                    ThrowIfAbortedLocked();
                    m_State = PublicationState.AwaitingQuestApply;
                    foreach (V2CanonicalMutation mutation in journalResult.Mutations)
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
                byte[] checkpointBytes = await Task.Run(() => V2SceneMutationCheckpointCodec.Encode(checkpointSequence, journalResult.Checkpoint), linked.Token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(linked.Token);
                lock (m_Gate)
                {
                    ThrowIfAbortedLocked();
                    if (m_PostCheckpointOverflow)
                    {
                        AbortLocked(true, "The mutation queue overflowed while the publication checkpoint was being encoded.");
                        throw new V2PublicationRestartException(m_FailureReason);
                    }

                    V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForBarrier(m_Identity.SceneId, m_Identity.IncarnationId, null, V2BarrierScope.AllScene);
                    V2EnqueueResult queued = m_Transport.EnqueueSceneOperation(checkpointBytes, descriptor, structural: true, bodySchema: V2PublicationCheckpointBulkReceiver.BodySchema, operationId: CreateOperationId());
                    if (!queued.Accepted) throw new IOException("The initial publication checkpoint could not be retained by the v2 transport: " + queued.Disposition + ".");
                    m_State = PublicationState.AwaitingQuestApply;
                    foreach (V2CanonicalMutation mutation in m_AfterCheckpoint)
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
                        AbortLocked(false, "The initial v2 publication was cancelled before Quest applied its barrier.");
                m_ConnectionLifetime.Cancel();
                stop.ThrowIfCancellationRequested();
            }

            if (completed == m_IncomingTask)
            {
                if (RequiresPublicationRestart) throw new V2PublicationRestartException(FailureReason);
                await completed.ConfigureAwait(false);
                throw new IOException("The v2 mutation receiver ended before Quest applied the initial publication journal.");
            }

            OperationId acknowledgedBarrier;
            try
            {
                acknowledgedBarrier = await m_InitialApplyAcknowledged.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (RequiresPublicationRestart)
            {
                throw new V2PublicationRestartException(FailureReason);
            }
            catch (OperationCanceledException)
            {
                lock (m_Gate) ThrowIfAbortedLocked();
                throw new IOException("The v2 replica connection ended before Quest applied the initial publication journal.");
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
            V2PreparedSceneIdentity bound = binding.CreateV2Identity();
            if (!bound.SessionId.Equals(m_Identity.SessionId) || !bound.SceneId.Equals(m_Identity.SceneId) || !bound.IncarnationId.Equals(m_Identity.IncarnationId))
                throw new InvalidDataException("The published scene receipt belongs to a different v2 scene incarnation.");
        }

        private void OnCanonicalReady(V2CanonicalMutation mutation)
        {
            lock (m_Gate)
            {
                if (m_State == PublicationState.Disposed || m_State == PublicationState.Aborted) return;
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
                        AbortLocked(true, "Too many v2 mutations arrived while the publication checkpoint was being encoded.");
                        return;
                    }

                    m_AfterCheckpoint.Add(mutation);
                    return;
                }

                EnqueueCanonicalLocked(mutation);
            }
        }

        private void EnqueueCanonicalLocked(V2CanonicalMutation mutation)
        {
            V2EnqueueResult queued = m_Transport.EnqueueMutation(mutation.Mutation, mutation.CanonicalSequence, null, coalesciblePreview: mutation.OriginDevice == V2OriginDevice.Desktop, operationId: mutation.OperationId);
            if (!queued.Accepted)
                AbortLocked(false, "The v2 transport could not retain an accepted Desktop mutation: " + queued.Disposition + ".");
        }

        private void EnqueueInitialBarrierLocked(ulong canonicalSequence)
        {
            m_InitialBarrierId = CreateOperationId();
            byte[] body = V2PublicationControlCodec.EncodeLiveBarrier(canonicalSequence);
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForBarrier(m_Identity.SceneId, m_Identity.IncarnationId, null, V2BarrierScope.AllScene);
            V2EnqueueResult queued = m_Transport.EnqueueSceneOperation(body, descriptor, structural: true, operationId: m_InitialBarrierId);
            if (!queued.Accepted) throw new IOException("The v2 transport could not retain the initial publication barrier: " + queued.Disposition + ".");
        }

        private async Task ReadIncomingAsync(CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                V2TransportRecord record = await m_Transport.ReadIncomingAsync(stop).ConfigureAwait(false);
                if (record == null) continue;
                if (record.Lane == V2ScheduleLane.SessionControl)
                {
                    await ProcessIncomingApplicationRecordAsync(record, stop).ConfigureAwait(false);
                    continue;
                }

                if (m_SceneOperationBulkReceiver.IsActive)
                {
                    if (m_SceneOperationBulkReceiver.TryAppend(record, out V2TransportRecord completedMutation))
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
                    if (!m_SceneOperationBulkReceiver.TryAppendNextBuffered(m_DeferredIncoming, record => record, out _, out V2TransportRecord completedMutation))
                        return;
                    if (completedMutation != null)
                        await ProcessIncomingApplicationRecordAsync(completedMutation, stop).ConfigureAwait(false);
                    continue;
                }

                if (m_DeferredIncoming.Count == 0) return;
                await ProcessIncomingApplicationRecordAsync(m_DeferredIncoming.Dequeue(), stop).ConfigureAwait(false);
            }
        }

        private async Task ProcessIncomingApplicationRecordAsync(V2TransportRecord record, CancellationToken stop)
        {
            if (record.Kind != V2TransportMessageKind.Application) return;
            byte[] payload = record.GetPayloadCopy();
            if (record.Lane == V2ScheduleLane.SessionControl)
            {
                if (V2PublicationControlCodec.TryDecodeAcknowledgement(payload, out OperationId barrierId))
                {
                    m_InitialApplyAcknowledged.TrySetResult(barrierId);
                    return;
                }

                if (V2CorrelationControlCodec.TryDecode(payload, out V2CorrelationControl correlationControl))
                    await ProcessCorrelationControlAsync(correlationControl, stop).ConfigureAwait(false);
                else if (V2SiteFilterControlCodec.TryDecode(payload, out V2SiteFilterControl control))
                    await ProcessSiteFilterControlAsync(control, stop).ConfigureAwait(false);
                else if (V2ActivityProjectionControlCodec.TryDecode(payload, out V2ActivityProjectionControl projectionControl))
                    await ProcessActivityProjectionControlAsync(projectionControl, stop).ConfigureAwait(false);
                else
                    throw new InvalidDataException("Unsupported v2 session-control application message.");
                return;
            }

            if (m_SceneOperationBulkReceiver.IsMutationDescriptor(record))
            {
                if (record.OriginDevice != V2OriginDevice.Quest || !record.ObservedCanonicalSequence.HasValue || record.CanonicalSequence.HasValue)
                    throw new InvalidDataException("A structural Desktop proposal must carry a Quest observed canonical sequence.");
                m_SceneOperationBulkReceiver.Begin(record);
                return;
            }

            if (record.Lane == V2ScheduleLane.SceneControl && record.BodySchema == V2SceneOperationBulkReceiver.BodySchema)
            {
                if (record.OriginDevice != V2OriginDevice.Quest || !record.ObservedCanonicalSequence.HasValue || record.CanonicalSequence.HasValue)
                    throw new InvalidDataException("A structural Desktop proposal must carry a Quest observed canonical sequence.");
                await AcceptQuestMutationAsync(record, V2MutationPayloadCodec.Decode(payload), stop).ConfigureAwait(false);
                return;
            }

            if (record.Lane != V2ScheduleLane.Interactive || record.OriginDevice != V2OriginDevice.Quest || !record.ObservedCanonicalSequence.HasValue || record.CanonicalSequence.HasValue || record.Mutation == null)
                throw new InvalidDataException("Unexpected v2 Quest application record.");

            await AcceptQuestMutationAsync(record, record.Mutation, stop).ConfigureAwait(false);
        }

        private Task<bool> HandleLocalSiteFilterRequestAsync(V2SiteFilterRequest request, CancellationToken stop)
        {
            return RunSiteFilterJobAsync(CreateOperationId(), request, stop, questRequested: false);
        }

        private Task<bool> HandleLocalCorrelationRequestAsync(V2CorrelationRequest request, CancellationToken stop)
        {
            return RunCorrelationJobAsync(CreateOperationId(), request, stop, questRequested: false);
        }

        private async Task ProcessCorrelationControlAsync(V2CorrelationControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == V2CorrelationControlKind.Request)
            {
                V2CorrelationRequest request = V2CorrelationRequestCodec.DecodeComputeRequest(control.Command);
                TrackCorrelationTask(RunCorrelationJobAsync(control.JobId, request, stop, questRequested: true));
                return;
            }

            ActiveCorrelationJob active = m_ActiveCorrelationJob;
            if (active == null || !active.Identity.JobId.Equals(control.JobId) || (control.Generation != 0 && active.Identity.Generation != control.Generation)) return;
            switch (control.Kind)
            {
                case V2CorrelationControlKind.Cancel:
                    active.Cancellation.Cancel();
                    break;
                case V2CorrelationControlKind.Ready:
                    active.QuestReady.TrySetResult(true);
                    break;
                case V2CorrelationControlKind.Failed:
                    active.QuestFailure = new IOException("Quest could not apply the correlation result: " + control.FailureCode + ".");
                    m_CorrelationGenerations.Cancel(active.Identity);
                    m_Transport.CancelBulk(active.Identity.JobId);
                    active.Cancellation.Cancel();
                    break;
                default:
                    throw new InvalidDataException("Unexpected Quest correlation control kind.");
            }
        }

        private async Task ProcessSiteFilterControlAsync(V2SiteFilterControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == V2SiteFilterControlKind.Request)
            {
                V2SiteFilterRequest request = V2SiteFilterRequestCodec.Decode(control.Payload);
                Task<bool> operation = RunSiteFilterJobAsync(control.JobId, request, stop, questRequested: true);
                TrackSiteFilterTask(operation);
                return;
            }

            ActiveSiteFilterJob active = m_ActiveSiteFilterJob;
            if (active == null || !active.Identity.JobId.Equals(control.JobId) || (control.Generation != 0 && active.Identity.Generation != control.Generation))
                return;

            switch (control.Kind)
            {
                case V2SiteFilterControlKind.Cancel:
                    active.Cancellation.Cancel();
                    break;
                case V2SiteFilterControlKind.Ready:
                    active.QuestReady.TrySetResult(true);
                    break;
                case V2SiteFilterControlKind.Failed:
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

        private async Task ProcessActivityProjectionControlAsync(V2ActivityProjectionControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            switch (control.Kind)
            {
                case V2ActivityProjectionControlKind.Request:
                    _ = ObserveRemoteActivityProjectionRequestAsync(control, stop);
                    return;
                case V2ActivityProjectionControlKind.RemoveRequest:
                    ApplyQuestActivityProjectionRemoval(control);
                    return;
                case V2ActivityProjectionControlKind.Progress:
                    if (MatchesActiveActivityProjection(control))
                        m_Scene.OnRemoteActivityProjectionProgress.Invoke(control.Progress, control.Message ?? string.Empty);
                    return;
                case V2ActivityProjectionControlKind.Cancel:
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
                case V2ActivityProjectionControlKind.Ready:
                case V2ActivityProjectionControlKind.Cancelled:
                case V2ActivityProjectionControlKind.Failed:
                    ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
                    if (!MatchesActiveActivityProjection(control)) return;
                    active.Terminals.MarkRemoteTerminal();
                    if (control.Kind == V2ActivityProjectionControlKind.Failed)
                    {
                        m_ActivityProjectionGenerations.Cancel(active.Identity);
                        CancelDesktopActivityProjection(active, notifyPeer: false);
                    }

                    TryFinishDesktopActivityProjection(active);
                    return;
                case V2ActivityProjectionControlKind.Policy:
                    throw new InvalidDataException("Quest cannot set the Desktop-owned activity-projection policy.");
                default:
                    throw new InvalidDataException("Unexpected activity-projection control kind.");
            }
        }

        private async Task ObserveRemoteActivityProjectionRequestAsync(V2ActivityProjectionControl request, CancellationToken stop)
        {
            try
            {
                while (!stop.IsCancellationRequested && m_Authority.CanonicalSequence < request.CanonicalSequence)
                {
                    if (m_CancelledActivityProjectionRequests.Remove(request.JobId.Value))
                    {
                        SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, V2ActivityProjectionControlKind.Cancelled);
                        return;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Initialization, stop);
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                }

                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                if (m_CancelledActivityProjectionRequests.Remove(request.JobId.Value))
                {
                    SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, V2ActivityProjectionControlKind.Cancelled);
                    return;
                }

                if (!IsLive || IsClosed)
                {
                    SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, V2ActivityProjectionControlKind.Failed, "desktop_not_live");
                    return;
                }

                if (m_ActiveActivityProjectionJob != null)
                {
                    SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, V2ActivityProjectionControlKind.Failed, "activity_projection_busy");
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
                SendActivityProjectionTerminal(request.JobId, 1, request.InputGeneration, V2ActivityProjectionControlKind.Failed, "desktop_projection_start_failed");
            }
        }

        private void BeginDesktopActivityProjection(OperationId jobId, bool explicitRequest, bool questRequested = false, ulong requestedInputGeneration = 0)
        {
            if (m_ActiveActivityProjectionJob != null || !m_Scene.TryBeginCoordinatedActivityProjection(out IDisposable activityScope))
            {
                if (questRequested)
                    SendActivityProjectionTerminal(jobId, 1, requestedInputGeneration, V2ActivityProjectionControlKind.Failed, "activity_projection_busy");
                return;
            }

            ulong inputGeneration = m_Scene.ActivityInputGeneration;
            V2JobIdentity identity = m_ActivityProjectionGenerations.BeginJob(m_Identity.SceneId, m_Identity.IncarnationId, V2JobType.ActivityProjection, jobId, inputGeneration);
            if (identity == null)
            {
                activityScope.Dispose();
                SendActivityProjectionTerminal(jobId, 1, inputGeneration, V2ActivityProjectionControlKind.Failed, "projection_generation_exhausted");
                return;
            }

            var active = new ActiveActivityProjectionJob(identity, activityScope);
            m_ActiveActivityProjectionJob = active;
            try
            {
                bool automatic = AutomaticProjectionPolicyEnabled;
                SendActivityProjectionControl(new V2ActivityProjectionControl(V2ActivityProjectionControlKind.Started, identity.JobId, identity.Generation, identity.InputGeneration, m_Authority.CanonicalSequence, projectionRequested: true, automaticPolicyEnabled: automatic, explicitRequest: explicitRequest), V2DeliveryReliability.Reliable);
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
            V2ActivityProjectionControlKind kind = completion switch
            {
                ActivityProjectionCompletionKind.Ready => V2ActivityProjectionControlKind.Ready,
                ActivityProjectionCompletionKind.Cancelled => V2ActivityProjectionControlKind.Cancelled,
                _ => V2ActivityProjectionControlKind.Failed
            };
            string failureCode = kind == V2ActivityProjectionControlKind.Failed ? "desktop_projection_failed" : null;
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
                    SendActivityProjectionControl(new V2ActivityProjectionControl(V2ActivityProjectionControlKind.Cancel, active.Identity.JobId, active.Identity.Generation, active.Identity.InputGeneration), V2DeliveryReliability.Reliable);
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

        private bool MatchesActiveActivityProjection(V2ActivityProjectionControl control)
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
            SendActivityProjectionControl(new V2ActivityProjectionControl(V2ActivityProjectionControlKind.Progress, active.Identity.JobId, active.Identity.Generation, active.Identity.InputGeneration, progress: progress, message: message), V2DeliveryReliability.Ephemeral);
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

        private void ApplyQuestActivityProjectionRemoval(V2ActivityProjectionControl control)
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
            SendActivityProjectionControl(new V2ActivityProjectionControl(V2ActivityProjectionControlKind.Policy, CreateOperationId(), 0, projectionRequested: m_Scene.ProjectionRequested, automaticPolicyEnabled: automatic), V2DeliveryReliability.Reliable);
            // Base3DScene starts a stale projection from Update once its geometry and resources are ready.
            // Starting directly here can emit a false failure for a ready projection or unfinished geometry.
        }

        private void SendActivityProjectionTerminal(OperationId jobId, ulong generation, ulong inputGeneration, V2ActivityProjectionControlKind kind, string failureCode = null)
        {
            if (IsClosed) return;
            try
            {
                SendActivityProjectionControl(new V2ActivityProjectionControl(kind, jobId, generation, inputGeneration, failureCode: failureCode), V2DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Desktop could not retain an activity-projection terminal control: " + exception.Message);
            }
        }

        private void SendActivityProjectionControl(V2ActivityProjectionControl control, V2DeliveryReliability reliability)
        {
            V2EnqueueResult queued = m_Transport.EnqueueSessionControl(V2ActivityProjectionControlCodec.Encode(control), reliability);
            if (!queued.Accepted && reliability == V2DeliveryReliability.Reliable)
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

        private async Task<bool> RunCorrelationJobAsync(OperationId jobId, V2CorrelationRequest request, CancellationToken requestStop, bool questRequested)
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

            V2JobIdentity identity = m_CorrelationGenerations.BeginJob(m_Identity.SceneId, m_Identity.IncarnationId, V2JobType.Correlation, jobId);
            if (identity == null)
            {
                activityScope.Dispose();
                if (questRequested) SendCorrelationFailure(jobId, 0, "generation_unavailable");
                throw new InvalidOperationException("The Desktop could not allocate a new correlation generation.");
            }

            V2JobAttempt attempt = m_CorrelationGenerations.BeginAttempt(identity);
            if (attempt == null)
            {
                activityScope.Dispose();
                if (questRequested) SendCorrelationFailure(jobId, identity.Generation, "attempt_unavailable");
                throw new InvalidOperationException("The Desktop could not start the correlation generation.");
            }

            var active = new ActiveCorrelationJob(identity, attempt, CancellationTokenSource.CreateLinkedTokenSource(requestStop, m_Lifetime.Token), activityScope);
            m_ActiveCorrelationJob = active;
            V2EnqueueResult started = m_Transport.EnqueueSessionControl(V2CorrelationControlCodec.Encode(new V2CorrelationControl(V2CorrelationControlKind.Started, identity.JobId, identity.Generation)), V2DeliveryReliability.Reliable);
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
                    if (request.Kind == V2CorrelationCommandKind.Load)
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
                    m_Boundary.Apply(result, V2MutationApplicationOrigin.LocalDesktop, identity.JobId);
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
                SendCorrelationTerminalControl(V2CorrelationControlKind.Cancel, identity, null);
                throw;
            }
            catch
            {
                m_CorrelationGenerations.Cancel(identity);
                m_Transport.CancelBulk(identity.JobId);
                if (active.QuestFailure != null) throw active.QuestFailure;
                SendCorrelationTerminalControl(V2CorrelationControlKind.Failed, identity, "desktop_correlation_failed");
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

        private void SendCorrelationTerminalControl(V2CorrelationControlKind kind, V2JobIdentity identity, string failureCode)
        {
            if (IsClosed) return;
            try
            {
                m_Transport.EnqueueSessionControl(V2CorrelationControlCodec.Encode(new V2CorrelationControl(kind, identity.JobId, identity.Generation, failureCode: failureCode)), V2DeliveryReliability.Reliable);
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
                m_Transport.EnqueueSessionControl(V2CorrelationControlCodec.Encode(new V2CorrelationControl(V2CorrelationControlKind.Failed, jobId, generation, failureCode: failureCode)), V2DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private sealed class ActiveCorrelationJob : IDisposable
        {
            public V2JobIdentity Identity { get; }
            public V2JobAttempt Attempt { get; }
            public CancellationTokenSource Cancellation { get; }
            public IDisposable ActivityScope { get; }
            public TaskCompletionSource<bool> QuestReady { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public Exception QuestFailure { get; set; }

            public ActiveCorrelationJob(V2JobIdentity identity, V2JobAttempt attempt, CancellationTokenSource cancellation, IDisposable activityScope)
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

        private async Task<bool> RunSiteFilterJobAsync(OperationId jobId, V2SiteFilterRequest request, CancellationToken requestStop, bool questRequested)
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

            V2JobIdentity identity = m_SiteFilterGenerations.BeginJob(m_Identity.SceneId, m_Identity.IncarnationId, V2JobType.Filter, jobId);
            if (identity == null)
            {
                activityScope.Dispose();
                if (questRequested) SendSiteFilterFailure(jobId, 0, "generation_unavailable");
                throw new InvalidOperationException("The Desktop could not allocate a new site-filter generation.");
            }

            V2JobAttempt attempt = m_SiteFilterGenerations.BeginAttempt(identity);
            if (attempt == null)
            {
                activityScope.Dispose();
                if (questRequested) SendSiteFilterFailure(jobId, identity.Generation, "attempt_unavailable");
                throw new InvalidOperationException("The Desktop could not start the site-filter generation.");
            }

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(requestStop, m_Lifetime.Token);
            var active = new ActiveSiteFilterJob(identity, attempt, cancellation, activityScope);
            m_ActiveSiteFilterJob = active;
            V2EnqueueResult started = m_Transport.EnqueueSessionControl(V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(V2SiteFilterControlKind.Started, identity.JobId, identity.Generation)), V2DeliveryReliability.Reliable);
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
                    V2SiteFilterEvaluationResult evaluation = await V2SiteFilterEvaluator.EvaluateAsync(m_Scene, m_Boundary, request, progress => update?.Invoke(progress, 0f, new LoadingText("Filtering sites")), token);
                    token.ThrowIfCancellationRequested();
                    if (!m_SiteFilterGenerations.IsCurrent(attempt)) throw new OperationCanceledException(token);
                    V2Mutation result = m_Boundary.CreateSiteFilterResult(identity.JobId, identity.Generation, evaluation.Included, evaluation.RosterHash);
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, token);
                    if (!m_SiteFilterGenerations.IsCurrent(attempt)) throw new OperationCanceledException(token);
                    m_Boundary.Apply(result, V2MutationApplicationOrigin.LocalDesktop, identity.JobId);
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
                SendSiteFilterTerminalControl(V2SiteFilterControlKind.Cancel, identity, null);
                throw;
            }
            catch
            {
                m_SiteFilterGenerations.Cancel(identity);
                m_Transport.CancelBulk(identity.JobId);
                if (active.QuestFailure != null) throw active.QuestFailure;
                SendSiteFilterTerminalControl(V2SiteFilterControlKind.Failed, identity, "desktop_filter_failed");
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

        private void SendSiteFilterTerminalControl(V2SiteFilterControlKind kind, V2JobIdentity identity, string failureCode)
        {
            if (IsClosed) return;
            try
            {
                byte[] payload = V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(kind, identity.JobId, identity.Generation, failureCode: failureCode));
                m_Transport.EnqueueSessionControl(payload, V2DeliveryReliability.Reliable);
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
                byte[] payload = V2SiteFilterControlCodec.Encode(new V2SiteFilterControl(V2SiteFilterControlKind.Failed, jobId, generation, failureCode: failureCode));
                m_Transport.EnqueueSessionControl(payload, V2DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private sealed class ActiveSiteFilterJob : IDisposable
        {
            public V2JobIdentity Identity { get; }
            public V2JobAttempt Attempt { get; }
            public CancellationTokenSource Cancellation { get; }
            public IDisposable ActivityScope { get; }
            public TaskCompletionSource<bool> QuestReady { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public Exception QuestFailure { get; set; }

            public ActiveSiteFilterJob(V2JobIdentity identity, V2JobAttempt attempt, CancellationTokenSource cancellation, IDisposable activityScope)
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
            public V2JobIdentity Identity { get; }
            public IDisposable ActivityScope { get; }
            public V2ActivityProjectionTerminalBarrier Terminals { get; } = new V2ActivityProjectionTerminalBarrier();
            public ulong LocalProjectionGeneration { get; set; }

            public ActiveActivityProjectionJob(V2JobIdentity identity, IDisposable activityScope)
            {
                Identity = identity;
                ActivityScope = activityScope;
            }

            public void Dispose() => ActivityScope.Dispose();
        }

        private async Task AcceptQuestMutationAsync(V2TransportRecord record, V2Mutation mutation, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            V2DesktopProposalResult result = m_Authority.AcceptQuestProposal(record.SceneId, record.IncarnationId, record.MessageId, mutation, record.ObservedCanonicalSequence.Value);
            if (result.Correction != null)
                EnqueueQuestProposalDecision(record, mutation, V2QuestProposalDecisionCodec.EncodeCorrection(result.Correction));
            else if (result.CanonicalMutation == null && result.Outcome != V2ProposalOutcome.Duplicate)
                EnqueueQuestProposalDecision(record, mutation, V2QuestProposalDecisionCodec.EncodeRejection(record.MessageId, result.RejectionCode ?? "proposal_rejected"));
        }

        private void EnqueueQuestProposalDecision(V2TransportRecord proposal, V2Mutation mutation, byte[] body)
        {
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_Identity.SceneId, m_Identity.IncarnationId, mutation);
            V2EnqueueResult queued = m_Transport.EnqueueSceneOperation(body, descriptor, bodySchema: V2QuestProposalDecisionCodec.BodySchema, operationId: proposal.MessageId);
            if (!queued.Accepted)
                throw new IOException("The v2 transport could not retain the Quest proposal decision: " + queued.Disposition + ".");
        }

        private async Task ObserveConnectionAsync(Task connection)
        {
            try
            {
                await connection.ConfigureAwait(false);
                if (!m_Lifetime.IsCancellationRequested) MarkConnectionClosed("The Quest v2 replica connection ended.");
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (m_Lifetime.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                MarkConnectionClosed("The Quest v2 replica connection failed: " + exception.Message);
                Debug.LogWarning("Quest v2 replica disconnected: " + exception.Message);
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
                        MarkConnectionClosed("The Quest v2 replica connection ended.");
                    return;
                }
                catch (OperationCanceledException) when (m_ConnectionLifetime.IsCancellationRequested || m_Lifetime.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception) when (IsTransientConnectionFailure(exception) && m_Transport.State == V2PersistentTransportState.DisconnectedGrace)
                {
                    Debug.LogWarning("Quest v2 replica disconnected; reconnecting within the 500 ms grace period: " + exception.Message);
                }
                catch (Exception exception)
                {
                    MarkConnectionClosed("The Quest v2 replica connection failed: " + exception.Message);
                    Debug.LogWarning("Quest v2 replica disconnected: " + exception.Message);
                    return;
                }

                Stopwatch grace = Stopwatch.StartNew();
                bool resumed = false;
                while (!m_ConnectionLifetime.IsCancellationRequested && m_Transport.State == V2PersistentTransportState.DisconnectedGrace)
                {
                    TimeSpan remaining = TimeSpan.FromMilliseconds(V2OutgoingScheduler.ReconnectGraceMilliseconds) - grace.Elapsed;
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
                    catch (Exception exception) when (IsTransientConnectionFailure(exception) && m_Transport.State == V2PersistentTransportState.DisconnectedGrace)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(25), m_Lifetime.Token).ConfigureAwait(false);
                    }
                }

                if (resumed) continue;
                if (m_Lifetime.IsCancellationRequested || IsClosed) return;
                MarkConnectionClosed("The Quest v2 replica reconnect grace expired.");
                return;
            }
        }

        private async Task<Task> OpenWithinGraceAsync(string host, byte[] pin, byte[] credential, TimeSpan remaining)
        {
            m_ConnectionLifetime.CancelAfter(remaining);
            Task connection = m_OpenReplica(host, pin, credential, m_ConnectionLifetime.Token, m_Transport);
            while (!connection.IsCompleted)
            {
                if (m_Transport.State == V2PersistentTransportState.Connected)
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
                if (!m_Lifetime.IsCancellationRequested) MarkConnectionClosed("The Quest v2 mutation receiver ended.");
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                MarkConnectionClosed("The Quest v2 mutation receiver failed: " + exception.Message);
                Debug.LogError("Quest v2 mutation receive failed: " + exception);
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
            lock (m_Gate) AbortLocked(false, "The v2 Desktop authority stopped: " + reason + ".");
            CancelActivityProjectionAfterDisconnectAsync().Forget();
        }

        private async UniTaskVoid CancelActivityProjectionAfterDisconnectAsync()
        {
            await UniTask.SwitchToMainThread();
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
                if (m_AbortRequiresRestart) throw new V2PublicationRestartException(m_FailureReason);
                throw new InvalidOperationException(m_FailureReason ?? "The v2 Desktop replica session has stopped.");
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
                SendSiteFilterTerminalControl(V2SiteFilterControlKind.Cancel, active.Identity, null);
                active.Dispose();
            }

            if (m_ActiveCorrelationJob != null)
            {
                ActiveCorrelationJob active = m_ActiveCorrelationJob;
                m_ActiveCorrelationJob = null;
                m_CorrelationGenerations.Cancel(active.Identity);
                m_Transport.CancelBulk(active.Identity.JobId);
                SendCorrelationTerminalControl(V2CorrelationControlKind.Cancel, active.Identity, null);
                active.Dispose();
            }

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

        internal sealed class V2PublicationRestartException : OperationCanceledException
        {
            public V2PublicationRestartException(string message) : base(message)
            {
            }
        }
    }
}
