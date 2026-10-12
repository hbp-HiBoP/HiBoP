using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Owns one published Quest scene's sync mutation boundary and duplex transport.</summary>
    internal sealed class QuestReplicaSession : IDisposable
    {
        public event Action<SceneAction> ActionReceived;

        public void SendAction(SceneAction action)
        {
            if (!action.Scene.Equals(m_Identity.SceneId) || !action.Incarnation.Equals(m_Identity.IncarnationId)) throw new InvalidDataException("Wrong scene action incarnation.");
            var queued = m_Transport.EnqueueSessionControl(action.Encode(), DeliveryReliability.Reliable);
            if (!queued.Accepted) throw new IOException("Scene action queue is full.");
        }

        private const int MaximumDeferredRecords = 512;
        private const int MaximumDeferredBytes = 4 * 1024 * 1024;
        private const int MaximumPendingVisibilityTelemetry = 1024;

        private readonly PreparedSceneIdentity m_Identity;
        private readonly Base3DScene m_Scene;
        private readonly SceneReconciliationRecord m_Reconciliation;
        private readonly HBP.Core.Preferences.UserPreferences m_SessionPreferences;
        private readonly SceneMutationBoundary m_Boundary;
        private readonly TimelineClockEstimator m_TimelineClock;
        private readonly OutgoingScheduler m_Scheduler;
        private readonly PersistentTransport m_Transport;
        private readonly SurfaceInflationCoordinator m_SurfaceInflation;
        private readonly QuestMutationDriver m_Driver;
        private readonly PublicationCheckpointBulkReceiver m_CheckpointBulkReceiver = new PublicationCheckpointBulkReceiver();
        private readonly SceneOperationBulkReceiver m_SceneOperationBulkReceiver = new SceneOperationBulkReceiver();

        private readonly Queue<DeferredRecord> m_DeferredRecords = new Queue<DeferredRecord>();

        // Receipt ACKs precede application. These slots therefore belong to the session,
        // including the non-preview record read ahead while optimizing a preview batch.
        private TransportRecord m_ProcessingRecord;
        private TransportRecord m_ReadAheadRecord;
        private TransportRecord m_CompletedSceneOperation;
#if DEVELOPMENT_BUILD && UNITY_ANDROID
        private string m_LastIncomingFailure;
#endif
        private readonly HashSet<Guid> m_AbandonedSiteFilterTransfers = new HashSet<Guid>();
        private readonly Queue<Guid> m_AbandonedSiteFilterOrder = new Queue<Guid>();
        private readonly HashSet<Guid> m_AbandonedCorrelationTransfers = new HashSet<Guid>();
        private readonly Queue<Guid> m_AbandonedCorrelationOrder = new Queue<Guid>();
        private readonly SemaphoreSlim m_DeferredDrainGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim m_IncomingPumpGate = new SemaphoreSlim(1, 1);
        private readonly Func<CancellationToken, Task> m_BeforeCheckpointApply;
        private readonly Func<TransportRecord, CancellationToken, Task> m_AfterDeferredRecordProcessed;
        private readonly object m_VisibilityGate = new object();
        private readonly Queue<PendingVisibilityTelemetry> m_PendingVisibilityTelemetry = new Queue<PendingVisibilityTelemetry>();
        private readonly SemaphoreSlim m_VisibilityAvailable = new SemaphoreSlim(0, 1);
        private byte[] m_CompletedCheckpoint;
        private OperationId m_CompletedCheckpointOperation;
        private readonly ApplicationCompletionWatermark m_ApplicationCompletion = new ApplicationCompletionWatermark();
        private ulong m_CheckpointOriginSequence;
        private ulong m_SceneBulkOriginSequence;
        private ulong m_DesktopRetiredCanonicalThrough;
        private ulong m_DesktopProgressOriginThrough;
        private ulong m_DesktopProgressCanonicalSequence;
        private ulong m_DesktopMinimumObservedSequence;
        private int m_DeferredBytes;
        private bool m_DeferredDrainPending;
        private bool m_Disposed;
        private IDisposable m_SiteFilterRequestRegistration;
        private IDisposable m_CorrelationRequestRegistration;
        private ActiveSiteFilterJob m_ActiveSiteFilterJob;
        private ActiveCorrelationJob m_ActiveCorrelationJob;
        private ActiveActivityProjectionJob m_ActiveActivityProjectionJob;
        private ulong m_LastDesktopActivityProjectionGeneration;
        private bool m_ApplyingRemoteProjectionRequest;
        private bool m_ProjectionEventsRemoved;
        private ulong m_LastDesktopSiteFilterGeneration;
        private ulong m_LastDesktopCorrelationGeneration;
        private ulong m_OfflineSiteFilterGeneration;
        private ulong m_OfflineCorrelationGeneration;
        private bool m_OfflineSiteFilterActive;
        private bool m_OfflineCorrelationActive;

        private readonly struct DeferredRecord
        {
            public readonly TransportRecord Record;
            public readonly SyncTelemetryPoint FirstReceived;
            public readonly SyncTelemetryPoint LastReceived;

            public DeferredRecord(TransportRecord record)
            {
                Record = record;
                FirstReceived = record.FirstReceived;
                LastReceived = record.LastReceived;
            }
        }

        private readonly struct PendingVisibilityTelemetry
        {
            public readonly SyncReceiveTelemetry Telemetry;
            public readonly SyncProfile Profile;
            public readonly SyncTelemetryIdentity Identity;

            public PendingVisibilityTelemetry(SyncReceiveTelemetry telemetry, SyncProfile profile, SyncTelemetryIdentity identity)
            {
                Telemetry = telemetry;
                Profile = profile;
                Identity = identity;
            }
        }

        public string TransferId { get; }
        public string ManifestHash { get; }
        public PersistentTransportState TransportState => m_Transport.State;
        public QuestMutationConnectionState ConnectionState => m_Driver.ConnectionState;
        public bool CanResumeConnection => !m_Disposed && m_Transport.State == PersistentTransportState.DisconnectedGrace && m_Driver.ConnectionState != QuestMutationConnectionState.OfflineLocal;

        public QuestReplicaSession(Base3DScene scene, PreparedSceneDeliveryBinding binding) : this(scene, binding, null, null)
        {
        }

        internal QuestReplicaSession(Base3DScene scene, PreparedSceneDeliveryBinding binding, Func<CancellationToken, Task> beforeCheckpointApply, Func<TransportRecord, CancellationToken, Task> afterDeferredRecordProcessed)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            m_Identity = binding.CreateIdentity();
            SceneReconciliationRecord.Get(scene)?.ReleaseReconciliationGuard();
            m_Scene = scene;
            m_Reconciliation = SceneReconciliationRecord.Retain(scene, binding);
            m_SessionPreferences = HBP.Core.Preferences.PersistentDataManager.IsInitialized ? HBP.Core.Preferences.PersistentDataManager.UserPreferences : null;
            m_SessionPreferences?.OnSavePreferences.AddListener(OnSessionPreferencesApplied);
            TransferId = binding.TransferId;
            ManifestHash = binding.ManifestHash;
            m_TimelineClock = new TimelineClockEstimator(StopwatchMonotonicClock.Instance);
            m_Boundary = new SceneMutationBoundary(scene, OriginDevice.Quest, timelineTimingEstimate: anchor => m_TimelineClock.TryEstimate(anchor.MonotonicAnchorTicks, anchor.TickFrequency, anchor.Step, out TimelineAnchorTimingEstimate estimate) ? estimate : (TimelineAnchorTimingEstimate?)null);
            m_Boundary.BindPreparedResources(binding);
            m_Scheduler = new OutgoingScheduler(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, OriginDevice.Quest);
            m_Transport = new PersistentTransport(m_Scheduler, shouldProbeClock: () => m_Boundary.IsAnyTimelinePlaying, clockProbeInterval: TimelineClockEstimator.ProbeInterval, previewFrameWaiter: WaitForPreviewFrameAsync);
            m_Transport.ClockProbeSampleReceived += sample => m_TimelineClock.AddSampleIfPlaying(sample, m_Boundary.IsAnyTimelinePlaying);
            m_Driver = new QuestMutationDriver(m_Identity.SceneId, m_Identity.IncarnationId, m_Boundary, m_Scheduler, 0, proposal => m_Transport.EnqueueMutation(proposal.Mutation, null, proposal.ObservedCanonicalSequence, coalesciblePreview: ScheduleDescriptor.ForMutation(m_Identity.SceneId, m_Identity.IncarnationId, proposal.Mutation).CoalescingKey != null, operationId: proposal.OperationId));
            m_SurfaceInflation = new SurfaceInflationCoordinator(scene, m_Boundary, m_Identity, false, () => !m_Disposed && m_Driver.ConnectionState == QuestMutationConnectionState.Connected && m_Transport.State == PersistentTransportState.Connected, () => m_Driver.LastObservedCanonicalSequence, SendSurfaceInflationControl, () => m_Driver.ConnectionState == QuestMutationConnectionState.OfflineLocal);
            m_SurfaceInflation.PreparationHandler = (prepare, token) => SurfaceInflationLoading.PrepareAsync(m_SurfaceInflation, prepare, token);
            m_SurfaceInflation.RemoteJobStarted += completion => SurfaceInflationLoading.ObserveAsync(completion).Forget();
            m_BeforeCheckpointApply = beforeCheckpointApply;
            m_AfterDeferredRecordProcessed = afterDeferredRecordProcessed;
            m_Driver.ProposalQueued += OnProposalQueued;
            m_Driver.RetentionProgressChanged += PublishRetentionProgress;
            m_Driver.OfflineLocalEntered += OnOfflineLocalEntered;
            m_Scene.ActivityProjectionStartHandler = HandleLocalActivityProjectionStart;
            m_Scene.OnActivityProjectionCompleted.AddListener(OnActivityProjectionCompleted);
            m_Scene.OnProgressUpdateGenerator.AddListener(OnActivityProjectionProgress);
            m_Scene.OnProjectionRequestedChanged.AddListener(OnProjectionRequestedChanged);
            m_SiteFilterRequestRegistration = SiteFilterRequestRouter.Register(scene, this, RequestSiteFilterAsync);
            m_CorrelationRequestRegistration = CorrelationRequestRouter.Register(scene, this, RequestCorrelationAsync);
        }

        public bool Matches(PreparedSceneDeliveryBinding binding) => binding != null && TransferId == binding.TransferId && ManifestHash == binding.ManifestHash;

        public async Task RunConnectionAsync(Stream stream, CancellationToken stop)
        {
            ThrowIfDisposed();
            if (m_Driver.ConnectionState == QuestMutationConnectionState.OfflineLocal)
                throw new InvalidOperationException("The Quest replica reconnect grace expired; publish a fresh scene before reconnecting.");
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop);
            Task incoming = ProcessIncomingAsync(lifetime.Token);
            Task connection = m_Transport.RunConnectionAsync(stream, lifetime.Token);
            Task visibility = ObserveVisibilityAsync(lifetime.Token);
            Task completed = await Task.WhenAny(incoming, connection, visibility).ConfigureAwait(false);
            try
            {
                await completed.ConfigureAwait(false);
            }
            finally
            {
                lifetime.Cancel();
                try
                {
                    await Task.WhenAll(incoming, connection, visibility).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        private void OnSessionPreferencesApplied()
        {
            if (!m_Disposed && SessionAtlasCatalog.Active) m_Scene.SetCoordinatedAutomaticRecomputePolicy(m_SessionPreferences.Visualization._3D.AutomaticEEGUpdate);
        }

        private void OnProposalQueued(QuestMutationProposal proposal) => m_Transport.NotifySchedulerChanged();

        private void PublishRetentionProgress()
        {
            if (m_Disposed || m_Driver.ConnectionState == QuestMutationConnectionState.OfflineLocal) return;
            var progress = new RetentionProgress(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, OriginDevice.Quest, m_ApplicationCompletion.CompletedThrough, m_Driver.MinimumPendingObservedSequence, 0, m_Driver.LastObservedCanonicalSequence);
            if (!m_Transport.EnqueueRetentionProgress(progress).Accepted)
                throw new IOException("Quest could not retain application completion progress.");
        }

        private void CompleteApplicationRecord(TransportRecord record)
        {
            if (record.OriginSequence == 0) return;
            if (record.CanonicalSequence.HasValue)
            {
                Mutation applied = record.Mutation;
                if (applied == null && record.BodySchema == SceneOperationBulkReceiver.BodySchema && !m_SceneOperationBulkReceiver.IsMutationDescriptor(record)) applied = MutationPayloadCodec.Decode(record.GetPayloadCopy());
                if (applied != null) m_Reconciliation.ApplyCanonical(record.CanonicalSequence.Value, applied);
            }

            m_Driver.RecordApplicationOrigin(record.MessageId, record.OriginSequence);
            m_ApplicationCompletion.Complete(record.OriginSequence);
            if (m_SceneBulkOriginSequence == record.OriginSequence) m_SceneBulkOriginSequence = 0;
            PublishRetentionProgress();
        }

        private void ResetSceneOperationBulkReceiver()
        {
            m_SceneOperationBulkReceiver.Reset();
            if (m_SceneBulkOriginSequence == 0) return;
            m_ApplicationCompletion.Complete(m_SceneBulkOriginSequence);
            m_SceneBulkOriginSequence = 0;
            PublishRetentionProgress();
        }

        private static async Task WaitForPreviewFrameAsync(CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (Application.isPlaying) await UniTask.NextFrame(cancellationToken: stop);
            else await UniTask.Yield(PlayerLoopTiming.Initialization, stop);
            await UniTask.SwitchToThreadPool();
        }

        private async Task ProcessIncomingAsync(CancellationToken stop)
        {
            await m_IncomingPumpGate.WaitAsync(stop).ConfigureAwait(false);
            try
            {
                await ProcessIncomingCoreAsync(stop).ConfigureAwait(false);
            }
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                string failure = exception.ToString();
                if (failure != m_LastIncomingFailure)
                {
                    m_LastIncomingFailure = failure;
                    Debug.LogWarning("Quest retained incoming application failure: " + failure);
                }
                throw;
            }
#endif
            finally
            {
                m_IncomingPumpGate.Release();
            }
        }

        private async Task ProcessIncomingCoreAsync(CancellationToken stop)
        {
            await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
            while (!stop.IsCancellationRequested)
            {
                if (m_ProcessingRecord == null)
                {
                    m_ProcessingRecord = m_ReadAheadRecord;
                    m_ReadAheadRecord = null;
                }

                m_ProcessingRecord ??= await m_Transport.ReadIncomingAsync(stop).ConfigureAwait(false);
                TransportRecord record = m_ProcessingRecord;
                if (record == null) continue;
                if (m_DeferredDrainPending && !m_SceneOperationBulkReceiver.IsActive)
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);

                if (record.Lane == ScheduleLane.SessionControl)
                {
                    if (!record.SessionId.Equals(m_Identity.SessionId)) throw new InvalidDataException("A sync session-control record belongs to another session.");
                    await ProcessRecordAsync(record, record.FirstReceived, record.LastReceived, stop).ConfigureAwait(false);
                    m_ProcessingRecord = null;
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                    continue;
                }

                if (IsAbandonedSiteFilterChunk(record))
                {
                    m_ProcessingRecord = null;
                    continue;
                }

                ValidateScope(record);
                var received = new DeferredRecord(record);

                if (m_CheckpointBulkReceiver.IsActive)
                {
                    if (m_CheckpointBulkReceiver.TryAppend(record, out byte[] completedCheckpoint, out OperationId checkpointOperation))
                    {
                        m_ProcessingRecord = null;
                        if (completedCheckpoint != null)
                        {
                            RetainCompletedCheckpoint(completedCheckpoint, checkpointOperation);
                            await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                        }

                        continue;
                    }

                    int size = checked(TransportFrameCodec.HeaderLength + record.PayloadLength);
                    if (m_DeferredRecords.Count >= MaximumDeferredRecords || size > MaximumDeferredBytes - m_DeferredBytes)
                        throw new InvalidDataException("Too many ordered records arrived during the initial sync checkpoint transfer.");
                    m_DeferredRecords.Enqueue(received);
                    m_DeferredBytes += size;
                    m_ProcessingRecord = null;
                    continue;
                }

                if (m_SceneOperationBulkReceiver.IsActive)
                {
                    if (m_SceneOperationBulkReceiver.TryAppend(record, out TransportRecord completedMutation))
                    {
                        m_ProcessingRecord = null;
                        if (completedMutation != null)
                        {
                            m_CompletedSceneOperation = completedMutation;
                            await ApplyCompletedSceneOperationAsync(stop).ConfigureAwait(false);
                            await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                        }

                        continue;
                    }

                    DeferOrderedRecord(received);
                    m_ProcessingRecord = null;
                    continue;
                }

                if (IsCanonicalPreview(record))
                {
                    // Wait for one application frame, then drain only already retained records.
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                    if (Application.isPlaying) await UniTask.NextFrame(cancellationToken: stop);
                    else await UniTask.Yield(PlayerLoopTiming.Initialization, stop);
                    var records = new List<TransportRecord> { record };
                    while (records.Count < 256 && m_Transport.TryReadIncoming(out TransportRecord next))
                    {
                        if (!IsCanonicalPreview(next))
                        {
                            m_ReadAheadRecord = next;
                            break;
                        }

                        ValidateScope(next);
                        records.Add(next);
                    }

                    var canonical = records.Select(item => new CanonicalMutation(item.SceneId, item.IncarnationId, item.MessageId, item.CanonicalSequence.Value, item.Mutation)).ToArray();
                    m_Driver.ReceiveCanonicalBatch(canonical, index =>
                    {
                        TransportRecord item = records[index];
                        ApplyTrackedMutation(item, item.Mutation, () => m_Driver.ReceiveCanonical(canonical[index]), item.FirstReceived, item.LastReceived);
                    });
                    foreach (TransportRecord item in records) CompleteApplicationRecord(item);
                }
                else
                    await ProcessRecordAsync(record, received.FirstReceived, received.LastReceived, stop).ConfigureAwait(false);

                m_ProcessingRecord = null;

                if (m_CompletedCheckpoint != null || m_DeferredDrainPending)
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
            }
        }

        private static bool IsCanonicalPreview(TransportRecord record) => record.Kind == TransportMessageKind.Application && record.Lane == ScheduleLane.Interactive && record.OriginDevice == OriginDevice.Desktop && record.CanonicalSequence.HasValue && !record.ObservedCanonicalSequence.HasValue && record.Mutation != null && QuestMutationDriver.IsReplaceablePreview(record.Mutation);

        private async Task ApplyCompletedSceneOperationAsync(CancellationToken stop)
        {
            TransportRecord completed = m_CompletedSceneOperation;
            if (completed == null) return;
            await ProcessRecordAsync(completed, completed.FirstReceived, completed.LastReceived, stop).ConfigureAwait(false);
            if (ReferenceEquals(m_CompletedSceneOperation, completed)) m_CompletedSceneOperation = null;
        }

        private async Task ProcessRecordAsync(TransportRecord record, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            await ProcessRecordCoreAsync(record, firstReceived, lastReceived, stop).ConfigureAwait(false);
            if (record.OriginSequence == 0) return;
            // Once application succeeds, connection cancellation must not lose its completion.
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, CancellationToken.None);
            if (m_Disposed) return;
            if (m_CheckpointBulkReceiver.IsCheckpointDescriptor(record) || record.Lane == ScheduleLane.SceneControl && record.BodySchema == PublicationCheckpointBulkReceiver.BodySchema && IsCheckpointBody(record))
            {
                m_CheckpointOriginSequence = record.OriginSequence;
                return;
            }

            if (m_SceneOperationBulkReceiver.IsMutationDescriptor(record) && m_SceneOperationBulkReceiver.IsActive)
            {
                m_SceneBulkOriginSequence = record.OriginSequence;
                return;
            }

            CompleteApplicationRecord(record);
        }

        private async Task ProcessRecordCoreAsync(TransportRecord record, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            if (record.Lane == ScheduleLane.SessionControl)
            {
                if (!record.SessionId.Equals(m_Identity.SessionId) || record.Kind != TransportMessageKind.Application)
                    throw new InvalidDataException("Unexpected sync session-control record.");
                byte[] controlPayload = record.GetPayloadCopy();
                if (SceneAction.TryDecode(controlPayload, out var action))
                {
                    if (!action.Scene.Equals(m_Identity.SceneId) || !action.Incarnation.Equals(m_Identity.IncarnationId)) throw new InvalidDataException("Wrong scene action incarnation.");
                    await UniTask.SwitchToMainThread(stop);
                    ActionReceived?.Invoke(action);
                    return;
                }

                if (RetentionProgress.TryDecode(controlPayload, out RetentionProgress progress))
                {
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                    progress.ValidateScope(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, OriginDevice.Desktop);
                    if (progress.AppliedOriginThrough < m_DesktopProgressOriginThrough || progress.AppliedOriginThrough > m_ApplicationCompletion.CompletedThrough || progress.RetiredCanonicalThrough < m_DesktopRetiredCanonicalThrough || progress.CurrentCanonicalSequence < m_DesktopProgressCanonicalSequence || progress.MinimumObservedCanonicalSequence < m_DesktopMinimumObservedSequence || progress.MinimumObservedCanonicalSequence > m_Driver.MinimumPendingObservedSequence)
                        throw new InvalidDataException("Invalid Desktop application completion progression.");
                    m_Driver.RetireCanonicalHistory(progress.RetiredCanonicalThrough, progress.AppliedOriginThrough);
                    m_DesktopProgressOriginThrough = progress.AppliedOriginThrough;
                    m_DesktopRetiredCanonicalThrough = progress.RetiredCanonicalThrough;
                    m_DesktopProgressCanonicalSequence = progress.CurrentCanonicalSequence;
                    m_DesktopMinimumObservedSequence = progress.MinimumObservedCanonicalSequence;
                }
                else if (CorrelationControlCodec.TryDecode(controlPayload, out CorrelationControl correlationControl))
                    await ProcessCorrelationControlAsync(correlationControl, stop).ConfigureAwait(false);
                else if (SiteFilterControlCodec.TryDecode(controlPayload, out SiteFilterControl filterControl))
                    await ProcessSiteFilterControlAsync(filterControl, stop).ConfigureAwait(false);
                else if (SurfaceInflationControlCodec.TryDecode(controlPayload, out SurfaceInflationControl surfaceControl))
                {
                    await UniTask.SwitchToMainThread(stop);
                    m_SurfaceInflation.Receive(surfaceControl);
                }
                else if (ActivityProjectionControlCodec.TryDecode(controlPayload, out ActivityProjectionControl projectionControl))
                    await ProcessActivityProjectionControlAsync(projectionControl, stop).ConfigureAwait(false);
                else
                    throw new InvalidDataException("Unsupported sync Quest session-control application message.");

                return;
            }

            ValidateScope(record);
            if (record.Kind != TransportMessageKind.Application)
                throw new InvalidDataException("Unexpected non-application sync record in the scene mutation stream.");

            byte[] payload = record.GetPayloadCopy();

            if (m_CheckpointBulkReceiver.IsCheckpointDescriptor(record))
            {
                m_CheckpointBulkReceiver.Begin(record, OriginDevice.Desktop);
                return;
            }

            if (m_SceneOperationBulkReceiver.IsMutationDescriptor(record))
            {
                if (record.MessageId != null && (m_AbandonedSiteFilterTransfers.Contains(record.MessageId.Value) || m_AbandonedCorrelationTransfers.Contains(record.MessageId.Value)))
                {
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                    m_Reconciliation.InvalidateCommon();
                    ResetSceneOperationBulkReceiver();
                    return;
                }

                if (record.OriginDevice != OriginDevice.Desktop || (record.BodySchema == SceneOperationBulkReceiver.BodySchema) != record.CanonicalSequence.HasValue || record.ObservedCanonicalSequence.HasValue)
                    throw new InvalidDataException($"A structural Quest mutation descriptor must carry a Desktop canonical sequence (origin={record.OriginDevice}, canonical={record.CanonicalSequence?.ToString() ?? "none"}, observed={record.ObservedCanonicalSequence?.ToString() ?? "none"}, schema={record.BodySchema}, length={record.PayloadLength}).");
                m_SceneOperationBulkReceiver.Begin(record);
                m_DeferredDrainPending = true;
                return;
            }

            if (record.Lane == ScheduleLane.SceneControl && record.BodySchema == PublicationCheckpointBulkReceiver.BodySchema && IsCheckpointBody(record))
            {
                RetainCompletedCheckpoint(record.GetPayloadCopy(), record.MessageId);
                return;
            }

            if (record.Lane == ScheduleLane.SceneControl && PublicationControlCodec.TryDecodeLiveBarrier(payload, out ulong barrierSequence))
            {
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                if (m_Driver.LastObservedCanonicalSequence != barrierSequence)
                    throw new InvalidDataException("Quest reached the initial live barrier at a different canonical watermark.");
                m_Reconciliation.SetStatus(ReconciliationStatus.Synchronized);
                byte[] acknowledgement = PublicationControlCodec.EncodeAcknowledgement(record.MessageId);
                EnqueueResult queued = m_Transport.EnqueueSessionControl(acknowledgement, DeliveryReliability.Reliable);
                if (!queued.Accepted)
                    throw new IOException("Quest could not retain the initial publication acknowledgement: " + queued.Disposition + ".");
                return;
            }

            if (record.Lane == ScheduleLane.SceneControl && record.BodySchema == SceneOperationBulkReceiver.BodySchema)
            {
                if (record.OriginDevice != OriginDevice.Desktop || !record.CanonicalSequence.HasValue || record.ObservedCanonicalSequence.HasValue)
                    throw new InvalidDataException($"A structural Quest mutation body must carry a Desktop canonical sequence (origin={record.OriginDevice}, canonical={record.CanonicalSequence?.ToString() ?? "none"}, observed={record.ObservedCanonicalSequence?.ToString() ?? "none"}, schema={record.BodySchema}, length={record.PayloadLength}).");
                Mutation structuralMutation = MutationPayloadCodec.Decode(payload);
                if (structuralMutation is SetSiteFilterResult filterResult)
                {
                    await ApplySiteFilterResultAsync(record, filterResult, firstReceived, lastReceived, stop).ConfigureAwait(false);
                    return;
                }

                if (structuralMutation is SetCorrelationResult correlationResult)
                {
                    await ApplyCorrelationResultAsync(record, correlationResult, firstReceived, lastReceived, stop).ConfigureAwait(false);
                    return;
                }

                await ApplyTrackedMutationAsync(record, structuralMutation, () => m_Driver.ReceiveCanonical(record.SceneId, record.IncarnationId, record.MessageId, record.CanonicalSequence.Value, structuralMutation), firstReceived, lastReceived, stop).ConfigureAwait(false);
                return;
            }

            if (record.Lane == ScheduleLane.Interactive && record.OriginDevice == OriginDevice.Desktop && record.BodySchema == QuestProposalDecisionCodec.BodySchema)
            {
                QuestProposalDecision decision = QuestProposalDecisionCodec.Decode(payload, record.SceneId, record.IncarnationId);
                if (!decision.OperationId.Equals(record.MessageId))
                    throw new InvalidDataException("A Quest proposal decision does not match its scene operation identity.");
                if (decision.Correction == null)
                {
                    await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                    m_Driver.ReceiveRejection(decision.OperationId, decision.RejectionCode);
                }
                else
                {
                    await ApplyTrackedMutationAsync(record, decision.Correction.AuthoritativeMutation, () => m_Driver.ReceiveCorrection(decision.Correction), firstReceived, lastReceived, stop).ConfigureAwait(false);
                }

                return;
            }

            if (record.Lane != ScheduleLane.Interactive || record.OriginDevice != OriginDevice.Desktop || !record.CanonicalSequence.HasValue || record.ObservedCanonicalSequence.HasValue || record.Mutation == null)
                throw new InvalidDataException("Unexpected sync Desktop scene mutation record.");

            await ApplyTrackedMutationAsync(record, record.Mutation, () => m_Driver.ReceiveCanonical(record.SceneId, record.IncarnationId, record.MessageId, record.CanonicalSequence.Value, record.Mutation), firstReceived, lastReceived, stop).ConfigureAwait(false);
        }

        private async Task ProcessSiteFilterControlAsync(SiteFilterControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == SiteFilterControlKind.Started)
            {
                if (m_AbandonedSiteFilterTransfers.Contains(control.JobId.Value))
                {
                    if (control.Generation > m_LastDesktopSiteFilterGeneration)
                        m_LastDesktopSiteFilterGeneration = control.Generation;
                    return;
                }

                if (control.Generation <= m_LastDesktopSiteFilterGeneration)
                {
                    if (m_ActiveSiteFilterJob?.JobId.Equals(control.JobId) == true && m_ActiveSiteFilterJob.Generation == control.Generation) return;
                    return;
                }

                ActiveSiteFilterJob active = m_ActiveSiteFilterJob;
                if (active != null && !active.JobId.Equals(control.JobId))
                {
                    EndSiteFilterJob(active, new OperationCanceledException("A newer site-filter generation superseded this job."));
                    active = null;
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                }

                if (active == null)
                {
                    if (m_ActiveCorrelationJob != null || !m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope))
                    {
                        SendSiteFilterControl(new SiteFilterControl(SiteFilterControlKind.Failed, control.JobId, control.Generation, failureCode: "quest_scene_busy"));
                        m_LastDesktopSiteFilterGeneration = control.Generation;
                        return;
                    }

                    // This job belongs to the published session, not one transport connection.
                    // A transient stream end must preserve it until reconnect or confirmed OfflineLocal.
                    active = new ActiveSiteFilterJob(control.JobId, control.Generation, new CancellationTokenSource(), activityScope, localRequest: false);
                    m_ActiveSiteFilterJob = active;
                    _ = ObserveRemoteSiteFilterJobAsync(active);
                }
                else
                {
                    active.Generation = control.Generation;
                }

                m_LastDesktopSiteFilterGeneration = control.Generation;
                return;
            }

            ActiveSiteFilterJob current = m_ActiveSiteFilterJob;
            if (current == null || !current.JobId.Equals(control.JobId) || (current.Generation != 0 && control.Generation != 0 && current.Generation != control.Generation)) return;
            switch (control.Kind)
            {
                case SiteFilterControlKind.Cancel:
                    EndSiteFilterJob(current, new OperationCanceledException("The Desktop cancelled this site-filter generation."));
                    break;
                case SiteFilterControlKind.Failed:
                    EndSiteFilterJob(current, new IOException("Desktop site filtering failed: " + control.FailureCode + "."));
                    break;
                default:
                    throw new InvalidDataException("Unexpected Desktop site-filter control kind.");
            }

            if (m_DeferredDrainPending)
                await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
        }

        private bool HandleLocalActivityProjectionStart()
        {
            if (m_Disposed) return false;
            if (m_Driver.ConnectionState == QuestMutationConnectionState.OfflineLocal)
                return false;
            if (m_Transport.State != PersistentTransportState.Connected)
                return true;
            // Desktop sequences automatic generations; Quest only proposes an explicit manual request.
            if (!m_Scene.ExplicitProjectionRequestPending) return true;
            if (m_ActiveActivityProjectionJob != null || !m_Scene.TryBeginCoordinatedActivityProjection(out IDisposable activityScope)) return true;

            var active = new ActiveActivityProjectionJob(CreateActivityProjectionOperationId(), 0, m_Scene.ActivityInputGeneration, activityScope);
            m_ActiveActivityProjectionJob = active;
            try
            {
                var request = new ActivityProjectionControl(ActivityProjectionControlKind.Request, active.JobId, 0, active.InputGeneration, m_Driver.LastObservedCanonicalSequence, projectionRequested: true, automaticPolicyEnabled: m_Scene.AutomaticRecomputeEnabled, explicitRequest: m_Scene.ExplicitProjectionRequestPending);
                SendActivityProjectionControl(request, DeliveryReliability.Reliable);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Quest could not retain the activity-projection request: " + exception.Message);
                m_ActiveActivityProjectionJob = null;
                active.Dispose();
                m_Scene.CancelActivityProjection();
            }

            return true;
        }

        private async Task ProcessActivityProjectionControlAsync(ActivityProjectionControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            switch (control.Kind)
            {
                case ActivityProjectionControlKind.Policy:
                    m_Scene.SetCoordinatedAutomaticRecomputePolicy(SessionAtlasCatalog.Active && m_SessionPreferences != null ? m_SessionPreferences.Visualization._3D.AutomaticEEGUpdate : control.AutomaticPolicyEnabled);
                    m_ApplyingRemoteProjectionRequest = true;
                    try
                    {
                        m_Scene.ApplyCoordinatedProjectionRequest(control.ProjectionRequested, explicitRequest: false);
                    }
                    finally
                    {
                        m_ApplyingRemoteProjectionRequest = false;
                    }

                    return;
                case ActivityProjectionControlKind.Started:
                    AcceptDesktopActivityProjectionStart(control);
                    return;
                case ActivityProjectionControlKind.Progress:
                    if (MatchesActiveActivityProjection(control))
                        m_Scene.OnRemoteActivityProjectionProgress.Invoke(control.Progress, control.Message ?? string.Empty);
                    return;
                case ActivityProjectionControlKind.Cancel:
                    ActiveActivityProjectionJob cancelTarget = m_ActiveActivityProjectionJob;
                    if (cancelTarget == null || !cancelTarget.JobId.Equals(control.JobId) || (cancelTarget.Generation != 0 && cancelTarget.Generation != control.Generation)) return;
                    if (cancelTarget.Generation == 0)
                    {
                        cancelTarget.Generation = control.Generation;
                        cancelTarget.InputGeneration = m_Scene.ActivityInputGeneration;
                    }

                    ApplyRemoteProjectionRemoval(cancelTarget);
                    return;
                case ActivityProjectionControlKind.Ready:
                case ActivityProjectionControlKind.Cancelled:
                case ActivityProjectionControlKind.Failed:
                    ActiveActivityProjectionJob terminalTarget = m_ActiveActivityProjectionJob;
                    if (terminalTarget == null || !terminalTarget.JobId.Equals(control.JobId)) return;
                    if (terminalTarget.Generation == 0)
                    {
                        terminalTarget.Generation = control.Generation;
                        terminalTarget.InputGeneration = m_Scene.ActivityInputGeneration;
                        // Desktop rejected the request before admitting a paired job. Clear the explicit
                        // request so Update does not resend it on every frame; a new user request can retry.
                        if (control.Kind != ActivityProjectionControlKind.Ready)
                            m_Scene.CancelActivityProjection();
                        terminalTarget.Terminals.MarkLocalTerminal();
                        terminalTarget.Terminals.MarkRemoteTerminal();
                        TryFinishQuestActivityProjection(terminalTarget);
                        return;
                    }

                    if (!MatchesActiveActivityProjection(control)) return;
                    terminalTarget.Terminals.MarkRemoteTerminal();
                    if (control.Kind == ActivityProjectionControlKind.Failed)
                    {
                        terminalTarget.Failed = true;
                        CancelQuestActivityProjection(terminalTarget, notifyPeer: false);
                    }

                    TryFinishQuestActivityProjection(terminalTarget);
                    return;
                case ActivityProjectionControlKind.Request:
                    throw new InvalidDataException("Desktop cannot request an activity projection from Quest.");
                default:
                    throw new InvalidDataException("Unexpected activity-projection control kind.");
            }
        }

        private void AcceptDesktopActivityProjectionStart(ActivityProjectionControl control)
        {
            if (control.Generation <= m_LastDesktopActivityProjectionGeneration)
            {
                if (m_ActiveActivityProjectionJob?.JobId.Equals(control.JobId) == true && m_ActiveActivityProjectionJob.Generation == control.Generation) return;
                SendActivityProjectionTerminal(control.JobId, control.Generation, control.InputGeneration, ActivityProjectionControlKind.Cancelled);
                return;
            }

            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active != null && !active.JobId.Equals(control.JobId))
            {
                SendActivityProjectionTerminal(control.JobId, control.Generation, control.InputGeneration, ActivityProjectionControlKind.Failed, "quest_projection_busy");
                m_LastDesktopActivityProjectionGeneration = control.Generation;
                return;
            }

            if (active == null)
            {
                if (!m_Scene.TryBeginCoordinatedActivityProjection(out IDisposable activityScope))
                {
                    SendActivityProjectionTerminal(control.JobId, control.Generation, control.InputGeneration, ActivityProjectionControlKind.Failed, "quest_projection_busy");
                    m_LastDesktopActivityProjectionGeneration = control.Generation;
                    return;
                }

                active = new ActiveActivityProjectionJob(control.JobId, control.Generation, m_Scene.ActivityInputGeneration, activityScope);
                m_ActiveActivityProjectionJob = active;
            }
            else
            {
                active.Generation = control.Generation;
            }

            m_LastDesktopActivityProjectionGeneration = control.Generation;
            active.CanonicalSequence = control.CanonicalSequence;
            active.ExplicitRequest = control.ExplicitRequest;
            active.AutomaticPolicyEnabled = control.AutomaticPolicyEnabled;
            m_Scene.SetCoordinatedAutomaticRecomputePolicy(control.AutomaticPolicyEnabled);
            m_ApplyingRemoteProjectionRequest = true;
            try
            {
                m_Scene.ApplyCoordinatedProjectionRequest(control.ProjectionRequested, control.ExplicitRequest);
            }
            finally
            {
                m_ApplyingRemoteProjectionRequest = false;
            }

            _ = ObserveQuestActivityProjectionStartAsync(active);
        }

        private async Task ObserveQuestActivityProjectionStartAsync(ActiveActivityProjectionJob active)
        {
            try
            {
                while (!active.Cancellation.IsCancellationRequested && m_Driver.LastObservedCanonicalSequence < active.CanonicalSequence)
                    await UniTask.Yield(PlayerLoopTiming.Initialization, active.Cancellation.Token);

                await UniTask.SwitchToMainThread();
                if (!ReferenceEquals(active, m_ActiveActivityProjectionJob) || active.Cancellation.IsCancellationRequested) return;
                active.InputGeneration = m_Scene.ActivityInputGeneration;

                if (!m_Scene.StartCoordinatedActivityProjection(active.ExplicitRequest))
                {
                    active.Failed = true;
                    CompleteLocalActivityProjection(active, ActivityProjectionCompletionKind.Failed);
                    return;
                }

                active.LocalStarted = true;
                active.LocalProjectionGeneration = m_Scene.ProjectionGeneration;
            }
            catch (OperationCanceledException) when (active.Cancellation.IsCancellationRequested)
            {
                await UniTask.SwitchToMainThread();
                if (!active.LocalStarted && !active.Terminals.LocalTerminal)
                    CompleteLocalActivityProjection(active, ActivityProjectionCompletionKind.Cancelled);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                await UniTask.SwitchToMainThread();
                active.Failed = true;
                CompleteLocalActivityProjection(active, ActivityProjectionCompletionKind.Failed);
            }
        }

        private void ApplyRemoteProjectionRemoval(ActiveActivityProjectionJob active)
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

            CancelQuestActivityProjection(active, notifyPeer: false);
        }

        private void CancelQuestActivityProjection(ActiveActivityProjectionJob active, bool notifyPeer)
        {
            if (active == null || !ReferenceEquals(active, m_ActiveActivityProjectionJob)) return;
            if (notifyPeer)
            {
                try
                {
                    SendActivityProjectionControl(new ActivityProjectionControl(ActivityProjectionControlKind.Cancel, active.JobId, active.Generation, active.InputGeneration), DeliveryReliability.Reliable);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Quest could not retain the activity-projection cancellation: " + exception.Message);
                }
            }

            if (!active.Cancellation.IsCancellationRequested) active.Cancellation.Cancel();
            m_Scene.CancelActivityProjection();
            if (!active.LocalStarted || !m_Scene.IsActivityProjectionComputing)
                CompleteLocalActivityProjection(active, ActivityProjectionCompletionKind.Cancelled);
        }

        private void OnActivityProjectionCompleted(ulong projectionGeneration, ActivityProjectionCompletionKind completion)
        {
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active == null || !active.LocalStarted || active.LocalProjectionGeneration != projectionGeneration || active.Terminals.LocalTerminal) return;
            CompleteLocalActivityProjection(active, completion);
        }

        private void CompleteLocalActivityProjection(ActiveActivityProjectionJob active, ActivityProjectionCompletionKind completion)
        {
            if (!ReferenceEquals(active, m_ActiveActivityProjectionJob) || active.Terminals.LocalTerminal) return;
            active.Terminals.MarkLocalTerminal();
            if (active.Generation == 0)
            {
                TryFinishQuestActivityProjection(active);
                return;
            }

            ActivityProjectionControlKind kind = active.Failed || completion == ActivityProjectionCompletionKind.Failed ? ActivityProjectionControlKind.Failed : completion == ActivityProjectionCompletionKind.Ready ? ActivityProjectionControlKind.Ready : ActivityProjectionControlKind.Cancelled;
            SendActivityProjectionTerminal(active.JobId, active.Generation, active.InputGeneration, kind, kind == ActivityProjectionControlKind.Failed ? "quest_projection_failed" : null);
            TryFinishQuestActivityProjection(active);
        }

        private bool MatchesActiveActivityProjection(ActivityProjectionControl control)
        {
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            return active != null && active.JobId.Equals(control.JobId) && active.Generation == control.Generation;
        }

        private void TryFinishQuestActivityProjection(ActiveActivityProjectionJob active)
        {
            if (!ReferenceEquals(active, m_ActiveActivityProjectionJob) || !active.Terminals.IsComplete) return;
            m_ActiveActivityProjectionJob = null;
            active.Dispose();
            if (m_Disposed) RemoveActivityProjectionListeners();
        }

        private void OnActivityProjectionProgress(float progress, string message)
        {
            if (m_Disposed) return;
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active == null || !active.LocalStarted || active.Terminals.LocalTerminal) return;
            SendActivityProjectionControl(new ActivityProjectionControl(ActivityProjectionControlKind.Progress, active.JobId, active.Generation, active.InputGeneration, progress: progress, message: message), DeliveryReliability.Ephemeral);
        }

        private void OnProjectionRequestedChanged(bool requested)
        {
            if (m_Disposed || requested || m_ApplyingRemoteProjectionRequest) return;
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active != null)
            {
                CancelQuestActivityProjection(active, notifyPeer: true);
                return;
            }

            // Without an active generation this is an idle shared-state request. Desktop decides
            // the authoritative value and returns it using the Policy control.
            if (m_Driver.ConnectionState != QuestMutationConnectionState.OfflineLocal)
            {
                var removal = new ActivityProjectionControl(ActivityProjectionControlKind.RemoveRequest, CreateActivityProjectionOperationId(), generation: 0, inputGeneration: m_Scene.ActivityInputGeneration, canonicalSequence: m_Driver.LastObservedCanonicalSequence, projectionRequested: false, automaticPolicyEnabled: m_Scene.AutomaticRecomputeEnabled);
                try
                {
                    SendActivityProjectionControl(removal, DeliveryReliability.Reliable);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Quest could not retain the activity-projection removal request: " + exception.Message);
                }
            }
        }

        private void SendActivityProjectionTerminal(OperationId jobId, ulong generation, ulong inputGeneration, ActivityProjectionControlKind kind, string failureCode = null)
        {
            if (m_Disposed || m_Driver.ConnectionState != QuestMutationConnectionState.Connected) return;
            try
            {
                SendActivityProjectionControl(new ActivityProjectionControl(kind, jobId, generation, inputGeneration, failureCode: failureCode), DeliveryReliability.Reliable);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Quest could not retain an activity-projection terminal control: " + exception.Message);
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
                throw new IOException("Quest could not retain an activity-projection session control: " + queued.Disposition + ".");
        }

        private static OperationId CreateActivityProjectionOperationId()
        {
            Guid value;
            do value = Guid.NewGuid();
            while (value == Guid.Empty);
            return new OperationId(value);
        }

        private async Task ProcessCorrelationControlAsync(CorrelationControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == CorrelationControlKind.Started)
            {
                if (m_AbandonedCorrelationTransfers.Contains(control.JobId.Value))
                {
                    if (control.Generation > m_LastDesktopCorrelationGeneration) m_LastDesktopCorrelationGeneration = control.Generation;
                    return;
                }

                if (control.Generation <= m_LastDesktopCorrelationGeneration)
                {
                    if (m_ActiveCorrelationJob?.JobId.Equals(control.JobId) == true && m_ActiveCorrelationJob.Generation == control.Generation) return;
                    return;
                }

                ActiveCorrelationJob active = m_ActiveCorrelationJob;
                if (active != null && !active.JobId.Equals(control.JobId))
                {
                    EndCorrelationJob(active, new OperationCanceledException("A newer correlation generation superseded this job."));
                    active = null;
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                }

                if (active == null)
                {
                    if (m_ActiveSiteFilterJob != null || !m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope))
                    {
                        SendCorrelationControl(new CorrelationControl(CorrelationControlKind.Failed, control.JobId, control.Generation, failureCode: "quest_scene_busy"));
                        m_LastDesktopCorrelationGeneration = control.Generation;
                        return;
                    }

                    active = new ActiveCorrelationJob(control.JobId, control.Generation, new CancellationTokenSource(), activityScope, localRequest: false);
                    m_ActiveCorrelationJob = active;
                    _ = ObserveRemoteCorrelationJobAsync(active);
                }
                else active.Generation = control.Generation;

                m_LastDesktopCorrelationGeneration = control.Generation;
                return;
            }

            ActiveCorrelationJob current = m_ActiveCorrelationJob;
            if (current == null || !current.JobId.Equals(control.JobId) || (current.Generation != 0 && control.Generation != 0 && current.Generation != control.Generation)) return;
            switch (control.Kind)
            {
                case CorrelationControlKind.Cancel:
                    EndCorrelationJob(current, new OperationCanceledException("The Desktop cancelled this correlation generation."));
                    break;
                case CorrelationControlKind.Failed:
                    EndCorrelationJob(current, new IOException("Desktop correlation processing failed: " + control.FailureCode + "."));
                    break;
                default:
                    throw new InvalidDataException("Unexpected Desktop correlation control kind.");
            }

            if (m_DeferredDrainPending) await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
        }

        private async Task ApplyCorrelationResultAsync(TransportRecord record, SetCorrelationResult result, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            ActiveCorrelationJob active = m_ActiveCorrelationJob;
            if (record.CanonicalSequence == null || !result.JobId.Equals(record.MessageId) || active == null || !active.JobId.Equals(result.JobId) || active.Generation != result.Generation || active.Cancellation.IsCancellationRequested)
            {
                m_Reconciliation.InvalidateCommon();
                if (record.CanonicalSequence.HasValue && record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence) m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                return;
            }

            try
            {
                await ApplyTrackedMutationAsync(record, result, () => m_Driver.ReceiveCanonical(record.SceneId, record.IncarnationId, record.MessageId, record.CanonicalSequence.Value, result), firstReceived, lastReceived, stop).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                SendCorrelationControl(new CorrelationControl(CorrelationControlKind.Ready, result.JobId, result.Generation));
                EndCorrelationJob(active, null);
            }
            catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException || exception is InvalidOperationException || exception is KeyNotFoundException)
            {
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                m_Reconciliation.InvalidateCommon();
                if (record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence) m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                SendCorrelationControl(new CorrelationControl(CorrelationControlKind.Failed, result.JobId, result.Generation, failureCode: "quest_apply_failed"));
                EndCorrelationJob(active, new InvalidDataException("Quest rejected the canonical correlation result.", exception));
            }
        }

        private Task<bool> RequestCorrelationAsync(CorrelationRequest request, CancellationToken stop) => RequestCorrelationCoreAsync(request, stop);

        private async Task<bool> RequestCorrelationCoreAsync(CorrelationRequest request, CancellationToken stop)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (m_Driver.ConnectionState == QuestMutationConnectionState.OfflineLocal)
            {
                if (m_OfflineCorrelationActive || m_OfflineSiteFilterActive) throw new InvalidOperationException("A local site-filter or correlation job is already active for this scene.");
                if (request.Kind == CorrelationCommandKind.Compute && !CorrelationOfflineCapabilityGate.CanCompute(RequireScene(), out string explanation)) throw new InvalidOperationException(explanation);
                if (m_ActiveSiteFilterJob != null || m_ActiveCorrelationJob != null || !m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable offlineScope))
                    throw new InvalidOperationException("Correlation processing cannot start while another sensitive scene job is active.");
                m_OfflineCorrelationActive = true;
                try
                {
                    using (offlineScope)
                    {
                        if (m_OfflineCorrelationGeneration == ulong.MaxValue) throw new InvalidOperationException("The offline correlation generation is exhausted.");
                        ulong generation = ++m_OfflineCorrelationGeneration;
                        OperationId jobId = CreateCorrelationOperationId();

                        async UniTask Evaluate(Action<float, float, LoadingText> update, CancellationToken token)
                        {
                            byte[] resultBytes;
                            if (request.Kind == CorrelationCommandKind.Load) resultBytes = request.ResultBytes;
                            else
                            {
                                IReadOnlyList<CorrelationResultData> computed = await RequireScene().ComputeCorrelationResultsAsync(update, token);
                                token.ThrowIfCancellationRequested();
                                resultBytes = CorrelationResultResource.FromResults(RequireScene(), computed).Encode();
                            }

                            SetCorrelationResult result = m_Boundary.CreateCorrelationResult(jobId, generation, resultBytes);
                            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, token);
                            m_Boundary.Apply(result, MutationApplicationOrigin.Remote, jobId);
                        }

                        if (request.ExternalLoadingIndicator) await Evaluate(request.Progress, stop);
                        else await LoadingManager.LoadDelayedAsync(Evaluate, stop, showInformations: false);
                    }
                }
                finally
                {
                    m_OfflineCorrelationActive = false;
                }

                return true;
            }

            if (request.Kind != CorrelationCommandKind.Compute) throw new InvalidOperationException("Loaded correlation data may only be imported by the Desktop while online.");
            if (m_Transport.State != PersistentTransportState.Connected) throw new IOException("The Quest connection is recovering; retry the correlation request after reconnection.");
            if (m_ActiveCorrelationJob != null || m_ActiveSiteFilterJob != null) throw new InvalidOperationException("A scene-wide filter or correlation job is already active.");
            if (!m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope)) throw new InvalidOperationException("Correlation processing cannot start while the scene is updating its activity projection.");

            OperationId requestId = CreateCorrelationOperationId();
            var activeJob = new ActiveCorrelationJob(requestId, 0, CancellationTokenSource.CreateLinkedTokenSource(stop), activityScope, localRequest: true);
            m_ActiveCorrelationJob = activeJob;
            try
            {
                byte[] command = CorrelationRequestCodec.EncodeComputeRequest(request);
                SendCorrelationControl(new CorrelationControl(CorrelationControlKind.Request, requestId, 0, command));
                if (request.ExternalLoadingIndicator) await WaitWithCancellationAsync(activeJob.Completion.Task, activeJob.Cancellation.Token);
                else await LoadingManager.LoadDelayedAsync(async (_, token) => await WaitWithCancellationAsync(activeJob.Completion.Task, token), activeJob.Cancellation.Token, showInformations: false);
                return true;
            }
            catch (OperationCanceledException)
            {
                SendCorrelationControl(new CorrelationControl(CorrelationControlKind.Cancel, activeJob.JobId, activeJob.Generation));
                EndCorrelationJob(activeJob, new OperationCanceledException());
                throw;
            }
            finally
            {
                if (ReferenceEquals(m_ActiveCorrelationJob, activeJob)) EndCorrelationJob(activeJob, null);
            }
        }

        private async Task ObserveRemoteCorrelationJobAsync(ActiveCorrelationJob active)
        {
            Exception terminalFailure = null;
            try
            {
                await LoadingManager.LoadDelayedAsync(async (_, token) => await WaitWithCancellationAsync(active.Completion.Task, token), active.Cancellation.Token, showInformations: false);
            }
            catch (Exception exception)
            {
                terminalFailure = exception;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (ReferenceEquals(m_ActiveCorrelationJob, active))
                {
                    bool shouldNotifyDesktop = terminalFailure != null && !active.Cancellation.IsCancellationRequested;
                    if (shouldNotifyDesktop)
                    {
                        CorrelationControlKind kind = terminalFailure is OperationCanceledException ? CorrelationControlKind.Cancel : CorrelationControlKind.Failed;
                        string code = kind == CorrelationControlKind.Failed ? "quest_loading_failed" : null;
                        try
                        {
                            SendCorrelationControl(new CorrelationControl(kind, active.JobId, active.Generation, failureCode: code));
                        }
                        catch (Exception exception)
                        {
                            terminalFailure = exception;
                        }
                    }

                    EndCorrelationJob(active, terminalFailure);
                }

                if (!m_Disposed && m_Driver.ConnectionState != QuestMutationConnectionState.OfflineLocal && (m_CompletedCheckpoint != null || m_DeferredDrainPending))
                    await ResumeCompletedCheckpointAndDrainAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private void EndCorrelationJob(ActiveCorrelationJob active, Exception failure)
        {
            if (active == null) return;
            if (failure != null)
            {
                RememberAbandonedCorrelationTransfer(active.JobId);
                if (m_SceneOperationBulkReceiver.ActiveOperationId?.Equals(active.JobId) == true)
                {
                    ResetSceneOperationBulkReceiver();
                    m_DeferredDrainPending = true;
                }
            }

            if (failure is OperationCanceledException) active.Completion.TrySetCanceled();
            else if (failure != null) active.Completion.TrySetException(failure);
            else active.Completion.TrySetResult(true);
            if (failure != null && !active.Cancellation.IsCancellationRequested) active.Cancellation.Cancel();
            if (ReferenceEquals(m_ActiveCorrelationJob, active)) m_ActiveCorrelationJob = null;
            active.Dispose();
        }

        private void RememberAbandonedCorrelationTransfer(OperationId jobId)
        {
            if (jobId == null || !m_AbandonedCorrelationTransfers.Add(jobId.Value)) return;
            m_AbandonedCorrelationOrder.Enqueue(jobId.Value);
            while (m_AbandonedCorrelationOrder.Count > 4096) m_AbandonedCorrelationTransfers.Remove(m_AbandonedCorrelationOrder.Dequeue());
        }

        private void SendCorrelationControl(CorrelationControl control)
        {
            EnqueueResult queued = m_Transport.EnqueueSessionControl(CorrelationControlCodec.Encode(control), DeliveryReliability.Reliable);
            if (!queued.Accepted) throw new IOException("Quest could not retain a correlation session control: " + queued.Disposition + ".");
        }

        private static OperationId CreateCorrelationOperationId()
        {
            Guid value;
            do value = Guid.NewGuid();
            while (value == Guid.Empty);
            return new OperationId(value);
        }

        private async Task ApplySiteFilterResultAsync(TransportRecord record, SetSiteFilterResult result, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            ActiveSiteFilterJob active = m_ActiveSiteFilterJob;
            if (record.CanonicalSequence == null || !result.JobId.Equals(record.MessageId) || active == null || !active.JobId.Equals(result.JobId) || active.Generation != result.Generation || active.Cancellation.IsCancellationRequested)
            {
                m_Reconciliation.InvalidateCommon();
                if (record.CanonicalSequence.HasValue && record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence)
                    m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                return;
            }

            try
            {
                await ApplyTrackedMutationAsync(record, result, () => m_Driver.ReceiveCanonical(record.SceneId, record.IncarnationId, record.MessageId, record.CanonicalSequence.Value, result), firstReceived, lastReceived, stop).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                SendSiteFilterControl(new SiteFilterControl(SiteFilterControlKind.Ready, result.JobId, result.Generation));
                EndSiteFilterJob(active, null);
            }
            catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException || exception is InvalidOperationException || exception is KeyNotFoundException)
            {
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                m_Reconciliation.InvalidateCommon();
                if (record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence)
                    m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                SendSiteFilterControl(new SiteFilterControl(SiteFilterControlKind.Failed, result.JobId, result.Generation, failureCode: "quest_apply_failed"));
                EndSiteFilterJob(active, new InvalidDataException("Quest rejected the canonical site-filter result.", exception));
            }
        }

        private Task<bool> RequestSiteFilterAsync(SiteFilterRequest request, CancellationToken stop)
        {
            return RequestSiteFilterCoreAsync(request, stop);
        }

        private async Task<bool> RequestSiteFilterCoreAsync(SiteFilterRequest request, CancellationToken stop)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (m_Driver.ConnectionState == QuestMutationConnectionState.OfflineLocal)
            {
                if (m_OfflineSiteFilterActive || m_OfflineCorrelationActive) throw new InvalidOperationException("A local site-filter or correlation job is already active for this scene.");
                if (!m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable offlineScope))
                    throw new InvalidOperationException("Site filtering cannot start while the scene is updating its activity projection.");
                m_OfflineSiteFilterActive = true;
                try
                {
                    using (offlineScope)
                    {
                        if (m_OfflineSiteFilterGeneration == ulong.MaxValue) throw new InvalidOperationException("The offline site-filter generation is exhausted.");
                        ulong generation = ++m_OfflineSiteFilterGeneration;
                        OperationId jobId = CreateSiteFilterOperationId();
                        Func<Action<float, float, LoadingText>, CancellationToken, UniTask> evaluate = async (update, token) =>
                        {
                            if (!SiteFilterOfflineCapabilityGate.CanEvaluate(request, RequireScene(), m_Boundary, out string explanation))
                                throw new InvalidOperationException(explanation);
                            SiteFilterEvaluationResult evaluation = await SiteFilterEvaluator.EvaluateAsync(RequireScene(), m_Boundary, request, progress => update?.Invoke(progress, 0f, new LoadingText("Filtering sites")), token);
                            SetSiteFilterResult result = m_Boundary.CreateSiteFilterResult(jobId, generation, evaluation.Included, evaluation.RosterHash);
                            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, token);
                            m_Boundary.Apply(result, MutationApplicationOrigin.Remote, jobId);
                        };
                        if (request.ExternalLoadingIndicator) await evaluate(null, stop);
                        else await LoadingManager.LoadDelayedAsync(evaluate, stop, showInformations: false);
                    }
                }
                finally
                {
                    m_OfflineSiteFilterActive = false;
                }

                return true;
            }

            if (m_Transport.State != PersistentTransportState.Connected)
                throw new IOException("The Quest connection is recovering; retry the filter after the session reconnects.");
            if (m_ActiveSiteFilterJob != null || m_ActiveCorrelationJob != null) throw new InvalidOperationException("A site-filter or correlation job is already active for this scene.");
            if (!m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope))
                throw new InvalidOperationException("Site filtering cannot start while the scene is updating its activity projection.");

            OperationId requestId = CreateSiteFilterOperationId();
            var activeJob = new ActiveSiteFilterJob(requestId, 0, CancellationTokenSource.CreateLinkedTokenSource(stop), activityScope, localRequest: true);
            m_ActiveSiteFilterJob = activeJob;
            try
            {
                byte[] command = SiteFilterRequestCodec.Encode(request);
                SendSiteFilterControl(new SiteFilterControl(SiteFilterControlKind.Request, requestId, 0, command));
                if (request.ExternalLoadingIndicator) await WaitWithCancellationAsync(activeJob.Completion.Task, activeJob.Cancellation.Token);
                else await LoadingManager.LoadDelayedAsync(async (_, token) => await WaitWithCancellationAsync(activeJob.Completion.Task, token), activeJob.Cancellation.Token, showInformations: false);
                return true;
            }
            catch (OperationCanceledException)
            {
                SendSiteFilterControl(new SiteFilterControl(SiteFilterControlKind.Cancel, activeJob.JobId, activeJob.Generation));
                EndSiteFilterJob(activeJob, new OperationCanceledException());
                throw;
            }
            finally
            {
                if (ReferenceEquals(m_ActiveSiteFilterJob, activeJob)) EndSiteFilterJob(activeJob, null);
            }
        }

        private async Task ObserveRemoteSiteFilterJobAsync(ActiveSiteFilterJob active)
        {
            Exception terminalFailure = null;
            try
            {
                await LoadingManager.LoadDelayedAsync(async (_, token) => await WaitWithCancellationAsync(active.Completion.Task, token), active.Cancellation.Token, showInformations: false);
            }
            catch (Exception exception)
            {
                terminalFailure = exception;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (ReferenceEquals(m_ActiveSiteFilterJob, active))
                {
                    bool shouldNotifyDesktop = terminalFailure != null && !active.Cancellation.IsCancellationRequested;
                    if (shouldNotifyDesktop)
                    {
                        SiteFilterControlKind terminalKind = terminalFailure is OperationCanceledException ? SiteFilterControlKind.Cancel : SiteFilterControlKind.Failed;
                        string failureCode = terminalKind == SiteFilterControlKind.Failed ? "quest_loading_failed" : null;
                        try
                        {
                            SendSiteFilterControl(new SiteFilterControl(terminalKind, active.JobId, active.Generation, failureCode: failureCode));
                        }
                        catch (Exception exception)
                        {
                            terminalFailure = exception;
                        }
                    }

                    EndSiteFilterJob(active, terminalFailure);
                }

                if (!m_Disposed && m_Driver.ConnectionState != QuestMutationConnectionState.OfflineLocal && (m_CompletedCheckpoint != null || m_DeferredDrainPending))
                    await ResumeCompletedCheckpointAndDrainAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private void EndSiteFilterJob(ActiveSiteFilterJob active, Exception failure)
        {
            if (active == null) return;
            if (failure != null)
            {
                RememberAbandonedSiteFilterTransfer(active.JobId);
                if (m_SceneOperationBulkReceiver.ActiveOperationId?.Equals(active.JobId) == true)
                {
                    ResetSceneOperationBulkReceiver();
                    m_DeferredDrainPending = true;
                }
            }

            if (failure is OperationCanceledException) active.Completion.TrySetCanceled();
            else if (failure != null) active.Completion.TrySetException(failure);
            else active.Completion.TrySetResult(true);
            if (failure != null && !active.Cancellation.IsCancellationRequested) active.Cancellation.Cancel();
            if (ReferenceEquals(m_ActiveSiteFilterJob, active)) m_ActiveSiteFilterJob = null;
            active.Dispose();
        }

        private void RememberAbandonedSiteFilterTransfer(OperationId jobId)
        {
            if (jobId == null || !m_AbandonedSiteFilterTransfers.Add(jobId.Value)) return;
            m_AbandonedSiteFilterOrder.Enqueue(jobId.Value);
            while (m_AbandonedSiteFilterOrder.Count > 4096)
                m_AbandonedSiteFilterTransfers.Remove(m_AbandonedSiteFilterOrder.Dequeue());
        }

        private bool IsAbandonedSiteFilterChunk(TransportRecord record)
        {
            return record != null && record.Lane == ScheduleLane.Bulk && record.MessageId != null && (m_AbandonedSiteFilterTransfers.Contains(record.MessageId.Value) || m_AbandonedCorrelationTransfers.Contains(record.MessageId.Value));
        }

        private void SendSiteFilterControl(SiteFilterControl control)
        {
            EnqueueResult queued = m_Transport.EnqueueSessionControl(SiteFilterControlCodec.Encode(control), DeliveryReliability.Reliable);
            if (!queued.Accepted) throw new IOException("Quest could not retain a site-filter session control: " + queued.Disposition + ".");
        }

        private Base3DScene RequireScene() => m_Scene;

        private static OperationId CreateSiteFilterOperationId()
        {
            Guid value;
            do value = Guid.NewGuid();
            while (value == Guid.Empty);
            return new OperationId(value);
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

        private void OnOfflineLocalEntered()
        {
            m_Reconciliation.SetStatus(ReconciliationStatus.Local, "Connection interrupted; your local changes are preserved.");
            CancelSiteFilterJobAfterDisconnectAsync().Forget();
            CancelCorrelationJobAfterDisconnectAsync().Forget();
            CancelActivityProjectionAfterDisconnectAsync().Forget();
        }

        private async UniTaskVoid CancelActivityProjectionAfterDisconnectAsync()
        {
            await UniTask.SwitchToMainThread();
            m_SurfaceInflation.Cancel();
            m_Scene.ClearCoordinatedAutomaticRecomputePolicy();
            ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
            if (active == null) return;
            active.Terminals.MarkRemoteTerminal();
            CancelQuestActivityProjection(active, notifyPeer: false);
            TryFinishQuestActivityProjection(active);
        }

        private async UniTaskVoid CancelSiteFilterJobAfterDisconnectAsync()
        {
            await UniTask.SwitchToMainThread();
            if (m_ActiveSiteFilterJob != null) EndSiteFilterJob(m_ActiveSiteFilterJob, new OperationCanceledException("The Quest connection grace expired."));
        }

        private async UniTaskVoid CancelCorrelationJobAfterDisconnectAsync()
        {
            await UniTask.SwitchToMainThread();
            if (m_ActiveCorrelationJob != null) EndCorrelationJob(m_ActiveCorrelationJob, new OperationCanceledException("The Quest connection grace expired."));
        }

        private sealed class ActiveSiteFilterJob : IDisposable
        {
            public OperationId JobId { get; }
            public ulong Generation { get; set; }
            public CancellationTokenSource Cancellation { get; }
            public IDisposable ActivityScope { get; }
            public bool LocalRequest { get; }
            public TaskCompletionSource<bool> Completion { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public ActiveSiteFilterJob(OperationId jobId, ulong generation, CancellationTokenSource cancellation, IDisposable activityScope, bool localRequest)
            {
                JobId = jobId;
                Generation = generation;
                Cancellation = cancellation;
                ActivityScope = activityScope;
                LocalRequest = localRequest;
            }

            public void Dispose()
            {
                Cancellation.Dispose();
                ActivityScope.Dispose();
            }
        }

        private sealed class ActiveActivityProjectionJob : IDisposable
        {
            public OperationId JobId { get; }
            public ulong Generation { get; set; }
            public ulong InputGeneration { get; set; }
            public IDisposable ActivityScope { get; }
            public CancellationTokenSource Cancellation { get; } = new CancellationTokenSource();
            public ActivityProjectionTerminalBarrier Terminals { get; } = new ActivityProjectionTerminalBarrier();
            public ulong CanonicalSequence { get; set; }
            public ulong LocalProjectionGeneration { get; set; }
            public bool ExplicitRequest { get; set; }
            public bool AutomaticPolicyEnabled { get; set; }
            public bool LocalStarted { get; set; }
            public bool Failed { get; set; }

            public ActiveActivityProjectionJob(OperationId jobId, ulong generation, ulong inputGeneration, IDisposable activityScope)
            {
                JobId = jobId;
                Generation = generation;
                InputGeneration = inputGeneration;
                ActivityScope = activityScope;
            }

            public void Dispose()
            {
                if (!Cancellation.IsCancellationRequested) Cancellation.Cancel();
                Cancellation.Dispose();
                ActivityScope.Dispose();
            }
        }

        private sealed class ActiveCorrelationJob : IDisposable
        {
            public OperationId JobId { get; }
            public ulong Generation { get; set; }
            public CancellationTokenSource Cancellation { get; }
            public IDisposable ActivityScope { get; }
            public bool LocalRequest { get; }
            public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ActiveCorrelationJob(OperationId jobId, ulong generation, CancellationTokenSource cancellation, IDisposable activityScope, bool localRequest)
            {
                JobId = jobId;
                Generation = generation;
                Cancellation = cancellation;
                ActivityScope = activityScope;
                LocalRequest = localRequest;
            }

            public void Dispose()
            {
                Cancellation.Dispose();
                ActivityScope.Dispose();
            }
        }

        private void RetainCompletedCheckpoint(byte[] checkpoint, OperationId operationId)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            if (m_CompletedCheckpoint != null)
                throw new InvalidDataException("A completed checkpoint is already waiting to be applied.");

            m_CompletedCheckpoint = checkpoint;
            m_CompletedCheckpointOperation = operationId;
            m_DeferredDrainPending = true;
        }

        private void DeferOrderedRecord(DeferredRecord record)
        {
            int size = checked(TransportFrameCodec.HeaderLength + record.Record.PayloadLength);
            if (m_DeferredRecords.Count >= MaximumDeferredRecords || size > MaximumDeferredBytes - m_DeferredBytes)
                throw new InvalidDataException("Too many ordered records arrived during a structural scene-operation transfer.");
            m_DeferredRecords.Enqueue(record);
            m_DeferredBytes += size;
        }

        private async Task ResumeCompletedCheckpointAndDrainAsync(CancellationToken stop)
        {
            await m_DeferredDrainGate.WaitAsync(stop).ConfigureAwait(false);
            try
            {
                await ApplyCompletedSceneOperationAsync(stop).ConfigureAwait(false);
                await ApplyRetainedCheckpointAsync(stop).ConfigureAwait(false);

                if (!m_DeferredDrainPending) return;
                while (true)
                {
                    if (m_CompletedCheckpoint != null)
                    {
                        await ApplyRetainedCheckpointAsync(stop).ConfigureAwait(false);
                        continue;
                    }

                    if (m_SceneOperationBulkReceiver.IsActive)
                    {
                        if (!m_SceneOperationBulkReceiver.TryAppendNextBuffered(m_DeferredRecords, pending => pending.Record, out DeferredRecord buffered, out TransportRecord completedMutation))
                            break;

                        m_DeferredBytes -= TransportFrameCodec.HeaderLength + buffered.Record.PayloadLength;
                        if (completedMutation != null)
                        {
                            m_CompletedSceneOperation = completedMutation;
                            await ApplyCompletedSceneOperationAsync(stop).ConfigureAwait(false);
                        }

                        if (m_CompletedCheckpoint != null)
                            await ApplyRetainedCheckpointAsync(stop).ConfigureAwait(false);
                        if (m_AfterDeferredRecordProcessed != null)
                            await m_AfterDeferredRecordProcessed(buffered.Record, stop).ConfigureAwait(false);
                        continue;
                    }

                    if (m_DeferredRecords.Count == 0) break;
                    DeferredRecord pending = m_DeferredRecords.Peek();
                    if (!IsAbandonedSiteFilterChunk(pending.Record))
                        await ProcessRecordAsync(pending.Record, pending.FirstReceived, pending.LastReceived, stop).ConfigureAwait(false);
                    m_DeferredRecords.Dequeue();
                    m_DeferredBytes -= TransportFrameCodec.HeaderLength + pending.Record.PayloadLength;
                    if (m_CompletedCheckpoint != null)
                        await ApplyRetainedCheckpointAsync(stop).ConfigureAwait(false);
                    if (m_AfterDeferredRecordProcessed != null)
                        await m_AfterDeferredRecordProcessed(pending.Record, stop).ConfigureAwait(false);
                }

                m_DeferredDrainPending = m_DeferredRecords.Count > 0 || m_SceneOperationBulkReceiver.IsActive;
            }
            finally
            {
                m_DeferredDrainGate.Release();
            }
        }

        private async Task ApplyRetainedCheckpointAsync(CancellationToken stop)
        {
            if (m_CompletedCheckpoint == null) return;
            if (m_BeforeCheckpointApply != null)
                await m_BeforeCheckpointApply(stop).ConfigureAwait(false);
            await ApplyCheckpointAsync(m_CompletedCheckpoint, m_CompletedCheckpointOperation, stop).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, CancellationToken.None);
            if (m_Disposed) return;
            m_ApplicationCompletion.Complete(m_CheckpointOriginSequence);
            m_CheckpointOriginSequence = 0;
            PublishRetentionProgress();
            m_CompletedCheckpoint = null;
            m_CompletedCheckpointOperation = null;
        }

        private async Task ApplyTrackedMutationAsync(TransportRecord record, Mutation mutation, Func<bool> apply, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            ApplyTrackedMutation(record, mutation, apply, firstReceived, lastReceived);
        }

        private void ApplyTrackedMutation(TransportRecord record, Mutation mutation, Func<bool> apply, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived)
        {
            bool measured = SyncTelemetry.Enabled && TryGetTelemetryProfile(mutation, out _);
            SyncProfile profile = measured ? GetTelemetryProfile(mutation) : default;
            SyncTelemetryIdentity identity = measured ? CreateTelemetryIdentity(record.MessageId) : default;
            var telemetry = measured ? new SyncReceiveTelemetry(firstReceived, lastReceived, TransportFrameCodec.HeaderLength + record.PayloadLength) : null;
            try
            {
                telemetry?.CaptureApplyStart();
                bool applied = apply();
                telemetry?.CaptureApplyEnd();
                if (applied && telemetry != null) QueueVisibilityTelemetry(telemetry, profile, identity);
            }
            finally
            {
                telemetry?.Publish(profile, identity);
            }
        }

        private static bool TryGetTelemetryProfile(Mutation mutation, out SyncProfile profile)
        {
            switch (mutation)
            {
                case SetSiteColor:
                    profile = SyncProfile.SiteColor;
                    return true;
                case SetCutDefinition:
                    profile = SyncProfile.CutDefinition;
                    return true;
                case SetTimelineAnchor:
                    profile = SyncProfile.TimelineAnchor;
                    return true;
                default:
                    profile = default;
                    return false;
            }
        }

        private static SyncProfile GetTelemetryProfile(Mutation mutation)
        {
            if (TryGetTelemetryProfile(mutation, out SyncProfile profile)) return profile;
            throw new ArgumentOutOfRangeException(nameof(mutation));
        }

        private SyncTelemetryIdentity CreateTelemetryIdentity(OperationId operationId)
        {
            string scope = m_Identity.IncarnationId.Value.ToString("N") + ":" + operationId.Value.ToString("N");
            return SyncTelemetryIdentity.Unknown(scope);
        }

        private void QueueVisibilityTelemetry(SyncReceiveTelemetry telemetry, SyncProfile profile, SyncTelemetryIdentity identity)
        {
            lock (m_VisibilityGate)
            {
                if (m_PendingVisibilityTelemetry.Count >= MaximumPendingVisibilityTelemetry) return;
                m_PendingVisibilityTelemetry.Enqueue(new PendingVisibilityTelemetry(telemetry, profile, identity));
                if (m_VisibilityAvailable.CurrentCount == 0) m_VisibilityAvailable.Release();
            }
        }

        private async Task ObserveVisibilityAsync(CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                await m_VisibilityAvailable.WaitAsync(stop).ConfigureAwait(false);
                PendingVisibilityTelemetry[] ready;
                lock (m_VisibilityGate)
                {
                    ready = m_PendingVisibilityTelemetry.ToArray();
                    m_PendingVisibilityTelemetry.Clear();
                }

                if (ready.Length == 0) continue;
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                await UniTask.NextFrame(cancellationToken: stop);
                SyncTelemetryPoint visible = SyncTelemetry.CapturePoint();
                foreach (PendingVisibilityTelemetry pending in ready)
                {
                    pending.Telemetry.CaptureNextVisible(visible);
                    SyncTelemetry.MarkAt(pending.Profile, pending.Identity, SyncMilestone.NextVisible, visible, pending.Telemetry.PayloadBytes);
                }
            }
        }

        private async Task ApplyCheckpointAsync(byte[] encodedCheckpoint, OperationId operationId, CancellationToken stop)
        {
            PublishedSceneCheckpoint checkpoint = await Task.Run(() => SceneMutationCheckpointCodec.Decode(encodedCheckpoint), stop).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            m_Boundary.ApplyCheckpoint(checkpoint.Checkpoint, operationId);
            m_Reconciliation.InstallCommon(checkpoint.Checkpoint, checkpoint.CanonicalSequence);
            m_Driver.AdvanceCanonicalWatermark(checkpoint.CanonicalSequence, includesSelection: true);
        }

        private void ValidateScope(TransportRecord record)
        {
            if (record == null || !record.SessionId.Equals(m_Identity.SessionId) || record.SceneId == null || !record.SceneId.Equals(m_Identity.SceneId) || record.IncarnationId == null || !record.IncarnationId.Equals(m_Identity.IncarnationId))
                throw new InvalidDataException("An incoming sync record belongs to a different published scene incarnation.");
        }

        private static bool IsCheckpointBody(TransportRecord record)
        {
            if (record.PayloadLength < 4) return false;
            byte[] bytes = record.GetPayloadCopy();
            return bytes[0] == (byte)'H' && bytes[1] == (byte)'B' && bytes[2] == (byte)'C' && bytes[3] == (byte)'P';
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed) throw new ObjectDisposedException(nameof(QuestReplicaSession));
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Driver.ProposalQueued -= OnProposalQueued;
            m_Driver.RetentionProgressChanged -= PublishRetentionProgress;
            m_Driver.OfflineLocalEntered -= OnOfflineLocalEntered;
            if (m_Scene.ActivityProjectionStartHandler == HandleLocalActivityProjectionStart)
                m_Scene.ActivityProjectionStartHandler = null;
            m_Scene.ClearCoordinatedAutomaticRecomputePolicy();
            if (m_ActiveActivityProjectionJob != null)
            {
                ActiveActivityProjectionJob active = m_ActiveActivityProjectionJob;
                active.Terminals.MarkRemoteTerminal();
                CancelQuestActivityProjection(active, notifyPeer: false);
                TryFinishQuestActivityProjection(active);
            }

            if (m_ActiveActivityProjectionJob == null) RemoveActivityProjectionListeners();
            m_SiteFilterRequestRegistration?.Dispose();
            m_CorrelationRequestRegistration?.Dispose();
            if (m_ActiveSiteFilterJob != null) EndSiteFilterJob(m_ActiveSiteFilterJob, new OperationCanceledException("The Quest replica session was disposed."));
            if (m_ActiveCorrelationJob != null) EndCorrelationJob(m_ActiveCorrelationJob, new OperationCanceledException("The Quest replica session was disposed."));
            m_SessionPreferences?.OnSavePreferences.RemoveListener(OnSessionPreferencesApplied);
            m_SurfaceInflation.Dispose();
            m_Transport.Dispose();
            m_Driver.Dispose();
            m_Boundary.Dispose();
            m_CheckpointBulkReceiver.Reset();
            ResetSceneOperationBulkReceiver();
            m_DeferredRecords.Clear();
            m_ProcessingRecord = null;
            m_ReadAheadRecord = null;
            m_CompletedSceneOperation = null;
            m_DeferredBytes = 0;
            m_CompletedCheckpoint = null;
            m_CompletedCheckpointOperation = null;
            m_DeferredDrainPending = false;
            m_VisibilityAvailable.Dispose();
        }

        private void RemoveActivityProjectionListeners()
        {
            if (m_ProjectionEventsRemoved) return;
            m_ProjectionEventsRemoved = true;
            m_Scene.OnActivityProjectionCompleted.RemoveListener(OnActivityProjectionCompleted);
            m_Scene.OnProgressUpdateGenerator.RemoveListener(OnActivityProjectionProgress);
            m_Scene.OnProjectionRequestedChanged.RemoveListener(OnProjectionRequestedChanged);
        }
    }
}
