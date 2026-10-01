using System;
using System.Collections.Generic;
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

namespace HBP.Quest
{
    /// <summary>Owns one published Quest scene's v2 mutation boundary and duplex transport.</summary>
    internal sealed class QuestV2ReplicaSession : IDisposable
    {
        private const int MaximumDeferredRecords = 512;
        private const int MaximumDeferredBytes = 4 * 1024 * 1024;
        private const int MaximumPendingVisibilityTelemetry = 1024;

        private readonly V2PreparedSceneIdentity m_Identity;
        private readonly Base3DScene m_Scene;
        private readonly V2SceneMutationBoundary m_Boundary;
        private readonly V2TimelineClockEstimator m_TimelineClock;
        private readonly V2OutgoingScheduler m_Scheduler;
        private readonly V2PersistentTransport m_Transport;
        private readonly V2QuestMutationDriver m_Driver;
        private readonly V2PublicationCheckpointBulkReceiver m_CheckpointBulkReceiver = new V2PublicationCheckpointBulkReceiver();
        private readonly V2SceneOperationBulkReceiver m_SceneOperationBulkReceiver = new V2SceneOperationBulkReceiver();
        private readonly Queue<DeferredRecord> m_DeferredRecords = new Queue<DeferredRecord>();
        private readonly HashSet<Guid> m_AbandonedSiteFilterTransfers = new HashSet<Guid>();
        private readonly Queue<Guid> m_AbandonedSiteFilterOrder = new Queue<Guid>();
        private readonly HashSet<Guid> m_AbandonedCorrelationTransfers = new HashSet<Guid>();
        private readonly Queue<Guid> m_AbandonedCorrelationOrder = new Queue<Guid>();
        private readonly SemaphoreSlim m_DeferredDrainGate = new SemaphoreSlim(1, 1);
        private readonly Func<CancellationToken, Task> m_BeforeCheckpointApply;
        private readonly Func<V2TransportRecord, CancellationToken, Task> m_AfterDeferredRecordProcessed;
        private readonly object m_VisibilityGate = new object();
        private readonly Queue<PendingVisibilityTelemetry> m_PendingVisibilityTelemetry = new Queue<PendingVisibilityTelemetry>();
        private readonly SemaphoreSlim m_VisibilityAvailable = new SemaphoreSlim(0, 1);
        private byte[] m_CompletedCheckpoint;
        private OperationId m_CompletedCheckpointOperation;
        private int m_DeferredBytes;
        private bool m_DeferredDrainPending;
        private bool m_Disposed;
        private IDisposable m_SiteFilterRequestRegistration;
        private IDisposable m_CorrelationRequestRegistration;
        private ActiveSiteFilterJob m_ActiveSiteFilterJob;
        private ActiveCorrelationJob m_ActiveCorrelationJob;
        private ulong m_LastDesktopSiteFilterGeneration;
        private ulong m_LastDesktopCorrelationGeneration;
        private ulong m_OfflineSiteFilterGeneration;
        private ulong m_OfflineCorrelationGeneration;
        private bool m_OfflineSiteFilterActive;
        private bool m_OfflineCorrelationActive;

        private readonly struct DeferredRecord
        {
            public readonly V2TransportRecord Record;
            public readonly SyncTelemetryPoint FirstReceived;
            public readonly SyncTelemetryPoint LastReceived;

            public DeferredRecord(V2TransportRecord record)
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
        public V2PersistentTransportState TransportState => m_Transport.State;
        public V2QuestMutationConnectionState ConnectionState => m_Driver.ConnectionState;
        public bool CanResumeConnection => !m_Disposed && m_Transport.State == V2PersistentTransportState.DisconnectedGrace && m_Driver.ConnectionState != V2QuestMutationConnectionState.OfflineLocal;

        public QuestV2ReplicaSession(Base3DScene scene, PreparedSceneDeliveryBinding binding) : this(scene, binding, null, null)
        {
        }

        internal QuestV2ReplicaSession(Base3DScene scene, PreparedSceneDeliveryBinding binding, Func<CancellationToken, Task> beforeCheckpointApply, Func<V2TransportRecord, CancellationToken, Task> afterDeferredRecordProcessed)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            m_Identity = binding.CreateV2Identity();
            m_Scene = scene;
            TransferId = binding.TransferId;
            ManifestHash = binding.ManifestHash;
            m_TimelineClock = new V2TimelineClockEstimator(StopwatchMonotonicClock.Instance);
            m_Boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Quest, timelineTimingEstimate: anchor => m_TimelineClock.TryEstimate(anchor.MonotonicAnchorTicks, anchor.TickFrequency, anchor.Step, out V2TimelineAnchorTimingEstimate estimate) ? estimate : (V2TimelineAnchorTimingEstimate?)null);
            m_Boundary.BindPreparedResources(binding);
            m_Scheduler = new V2OutgoingScheduler(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, V2OriginDevice.Quest);
            m_Transport = new V2PersistentTransport(m_Scheduler, shouldProbeClock: () => m_Boundary.IsAnyTimelinePlaying, clockProbeInterval: V2TimelineClockEstimator.ProbeInterval);
            m_Transport.ClockProbeSampleReceived += sample => m_TimelineClock.AddSampleIfPlaying(sample, m_Boundary.IsAnyTimelinePlaying);
            m_Driver = new V2QuestMutationDriver(m_Identity.SceneId, m_Identity.IncarnationId, m_Boundary, m_Scheduler);
            m_BeforeCheckpointApply = beforeCheckpointApply;
            m_AfterDeferredRecordProcessed = afterDeferredRecordProcessed;
            m_Driver.ProposalQueued += OnProposalQueued;
            m_Driver.OfflineLocalEntered += OnOfflineLocalEntered;
            m_SiteFilterRequestRegistration = V2SiteFilterRequestRouter.Register(scene, this, RequestSiteFilterAsync);
            m_CorrelationRequestRegistration = V2CorrelationRequestRouter.Register(scene, this, RequestCorrelationAsync);
        }

        public bool Matches(PreparedSceneDeliveryBinding binding) => binding != null && TransferId == binding.TransferId && ManifestHash == binding.ManifestHash;

        public async Task RunConnectionAsync(Stream stream, CancellationToken stop)
        {
            ThrowIfDisposed();
            if (m_Driver.ConnectionState == V2QuestMutationConnectionState.OfflineLocal)
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

        private void OnProposalQueued(V2QuestMutationProposal proposal) => m_Transport.NotifySchedulerChanged();

        private async Task ProcessIncomingAsync(CancellationToken stop)
        {
            await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
            while (!stop.IsCancellationRequested)
            {
                V2TransportRecord record = await m_Transport.ReadIncomingAsync(stop).ConfigureAwait(false);
                if (record == null) continue;
                if (m_DeferredDrainPending && !m_SceneOperationBulkReceiver.IsActive)
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);

                if (record.Lane == V2ScheduleLane.SessionControl)
                {
                    if (!record.SessionId.Equals(m_Identity.SessionId)) throw new InvalidDataException("A v2 session-control record belongs to another session.");
                    await ProcessRecordAsync(record, record.FirstReceived, record.LastReceived, stop).ConfigureAwait(false);
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                    continue;
                }

                if (IsAbandonedSiteFilterChunk(record)) continue;

                ValidateScope(record);
                var received = new DeferredRecord(record);

                if (m_CheckpointBulkReceiver.IsActive)
                {
                    if (m_CheckpointBulkReceiver.TryAppend(record, out byte[] completedCheckpoint, out OperationId checkpointOperation))
                    {
                        if (completedCheckpoint != null)
                        {
                            RetainCompletedCheckpoint(completedCheckpoint, checkpointOperation);
                            await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                        }

                        continue;
                    }

                    int size = checked(V2TransportFrameCodec.HeaderLength + record.PayloadLength);
                    if (m_DeferredRecords.Count >= MaximumDeferredRecords || size > MaximumDeferredBytes - m_DeferredBytes)
                        throw new InvalidDataException("Too many ordered records arrived during the initial v2 checkpoint transfer.");
                    m_DeferredRecords.Enqueue(received);
                    m_DeferredBytes += size;
                    continue;
                }

                if (m_SceneOperationBulkReceiver.IsActive)
                {
                    if (m_SceneOperationBulkReceiver.TryAppend(record, out V2TransportRecord completedMutation))
                    {
                        if (completedMutation != null)
                        {
                            await ProcessRecordAsync(completedMutation, completedMutation.FirstReceived, completedMutation.LastReceived, stop).ConfigureAwait(false);
                            await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                        }

                        continue;
                    }

                    DeferOrderedRecord(received);
                    continue;
                }

                await ProcessRecordAsync(record, received.FirstReceived, received.LastReceived, stop).ConfigureAwait(false);
                if (m_CompletedCheckpoint != null || m_DeferredDrainPending)
                    await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
            }
        }

        private async Task ProcessRecordAsync(V2TransportRecord record, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            if (record.Lane == V2ScheduleLane.SessionControl)
            {
                if (!record.SessionId.Equals(m_Identity.SessionId) || record.Kind != V2TransportMessageKind.Application)
                    throw new InvalidDataException("Unexpected v2 session-control record.");
                byte[] controlPayload = record.GetPayloadCopy();
                if (V2CorrelationControlCodec.TryDecode(controlPayload, out V2CorrelationControl correlationControl))
                    await ProcessCorrelationControlAsync(correlationControl, stop).ConfigureAwait(false);
                else if (V2SiteFilterControlCodec.TryDecode(controlPayload, out V2SiteFilterControl filterControl))
                    await ProcessSiteFilterControlAsync(filterControl, stop).ConfigureAwait(false);
                else
                    throw new InvalidDataException("Unsupported v2 Quest session-control application message.");
                return;
            }

            ValidateScope(record);
            if (record.Kind != V2TransportMessageKind.Application)
                throw new InvalidDataException("Unexpected non-application v2 record in the scene mutation stream.");

            byte[] payload = record.GetPayloadCopy();

            if (m_CheckpointBulkReceiver.IsCheckpointDescriptor(record))
            {
                m_CheckpointBulkReceiver.Begin(record, V2OriginDevice.Desktop);
                return;
            }

            if (m_SceneOperationBulkReceiver.IsMutationDescriptor(record))
            {
                if (record.MessageId != null && (m_AbandonedSiteFilterTransfers.Contains(record.MessageId.Value) || m_AbandonedCorrelationTransfers.Contains(record.MessageId.Value)))
                {
                    m_SceneOperationBulkReceiver.Reset();
                    return;
                }

                if (record.OriginDevice != V2OriginDevice.Desktop || !record.CanonicalSequence.HasValue || record.ObservedCanonicalSequence.HasValue)
                    throw new InvalidDataException($"A structural Quest mutation descriptor must carry a Desktop canonical sequence (origin={record.OriginDevice}, canonical={record.CanonicalSequence?.ToString() ?? "none"}, observed={record.ObservedCanonicalSequence?.ToString() ?? "none"}, schema={record.BodySchema}, length={record.PayloadLength}).");
                m_SceneOperationBulkReceiver.Begin(record);
                m_DeferredDrainPending = true;
                return;
            }

            if (record.Lane == V2ScheduleLane.SceneControl && record.BodySchema == V2PublicationCheckpointBulkReceiver.BodySchema && IsCheckpointBody(record))
            {
                RetainCompletedCheckpoint(record.GetPayloadCopy(), record.MessageId);
                return;
            }

            if (record.Lane == V2ScheduleLane.SceneControl && V2PublicationControlCodec.TryDecodeLiveBarrier(payload, out ulong barrierSequence))
            {
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                if (m_Driver.LastObservedCanonicalSequence != barrierSequence)
                    throw new InvalidDataException("Quest reached the initial live barrier at a different canonical watermark.");
                byte[] acknowledgement = V2PublicationControlCodec.EncodeAcknowledgement(record.MessageId);
                V2EnqueueResult queued = m_Transport.EnqueueSessionControl(acknowledgement, V2DeliveryReliability.Reliable);
                if (!queued.Accepted)
                    throw new IOException("Quest could not retain the initial publication acknowledgement: " + queued.Disposition + ".");
                return;
            }

            if (record.Lane == V2ScheduleLane.SceneControl && record.BodySchema == V2SceneOperationBulkReceiver.BodySchema)
            {
                if (record.OriginDevice != V2OriginDevice.Desktop || !record.CanonicalSequence.HasValue || record.ObservedCanonicalSequence.HasValue)
                    throw new InvalidDataException($"A structural Quest mutation body must carry a Desktop canonical sequence (origin={record.OriginDevice}, canonical={record.CanonicalSequence?.ToString() ?? "none"}, observed={record.ObservedCanonicalSequence?.ToString() ?? "none"}, schema={record.BodySchema}, length={record.PayloadLength}).");
                V2Mutation structuralMutation = V2MutationPayloadCodec.Decode(payload);
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

            if (record.Lane == V2ScheduleLane.Interactive && record.OriginDevice == V2OriginDevice.Desktop && record.BodySchema == V2QuestProposalDecisionCodec.BodySchema)
            {
                V2QuestProposalDecision decision = V2QuestProposalDecisionCodec.Decode(payload, record.SceneId, record.IncarnationId);
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

            if (record.Lane != V2ScheduleLane.Interactive || record.OriginDevice != V2OriginDevice.Desktop || !record.CanonicalSequence.HasValue || record.ObservedCanonicalSequence.HasValue || record.Mutation == null)
                throw new InvalidDataException("Unexpected v2 Desktop scene mutation record.");

            await ApplyTrackedMutationAsync(record, record.Mutation, () => m_Driver.ReceiveCanonical(record.SceneId, record.IncarnationId, record.MessageId, record.CanonicalSequence.Value, record.Mutation), firstReceived, lastReceived, stop).ConfigureAwait(false);
        }

        private async Task ProcessSiteFilterControlAsync(V2SiteFilterControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == V2SiteFilterControlKind.Started)
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
                        SendSiteFilterControl(new V2SiteFilterControl(V2SiteFilterControlKind.Failed, control.JobId, control.Generation, failureCode: "quest_scene_busy"));
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
                case V2SiteFilterControlKind.Cancel:
                    EndSiteFilterJob(current, new OperationCanceledException("The Desktop cancelled this site-filter generation."));
                    break;
                case V2SiteFilterControlKind.Failed:
                    EndSiteFilterJob(current, new IOException("Desktop site filtering failed: " + control.FailureCode + "."));
                    break;
                default:
                    throw new InvalidDataException("Unexpected Desktop site-filter control kind.");
            }

            if (m_DeferredDrainPending)
                await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
        }

        private async Task ProcessCorrelationControlAsync(V2CorrelationControl control, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (control.Kind == V2CorrelationControlKind.Started)
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
                        SendCorrelationControl(new V2CorrelationControl(V2CorrelationControlKind.Failed, control.JobId, control.Generation, failureCode: "quest_scene_busy"));
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
                case V2CorrelationControlKind.Cancel:
                    EndCorrelationJob(current, new OperationCanceledException("The Desktop cancelled this correlation generation."));
                    break;
                case V2CorrelationControlKind.Failed:
                    EndCorrelationJob(current, new IOException("Desktop correlation processing failed: " + control.FailureCode + "."));
                    break;
                default:
                    throw new InvalidDataException("Unexpected Desktop correlation control kind.");
            }

            if (m_DeferredDrainPending) await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
        }

        private async Task ApplyCorrelationResultAsync(V2TransportRecord record, SetCorrelationResult result, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            ActiveCorrelationJob active = m_ActiveCorrelationJob;
            if (record.CanonicalSequence == null || !result.JobId.Equals(record.MessageId) || active == null || !active.JobId.Equals(result.JobId) || active.Generation != result.Generation || active.Cancellation.IsCancellationRequested)
            {
                if (record.CanonicalSequence.HasValue && record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence) m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                return;
            }

            try
            {
                await ApplyTrackedMutationAsync(record, result, () => m_Driver.ReceiveCanonical(record.SceneId, record.IncarnationId, record.MessageId, record.CanonicalSequence.Value, result), firstReceived, lastReceived, stop).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                SendCorrelationControl(new V2CorrelationControl(V2CorrelationControlKind.Ready, result.JobId, result.Generation));
                EndCorrelationJob(active, null);
            }
            catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException || exception is InvalidOperationException || exception is KeyNotFoundException)
            {
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                if (record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence) m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                SendCorrelationControl(new V2CorrelationControl(V2CorrelationControlKind.Failed, result.JobId, result.Generation, failureCode: "quest_apply_failed"));
                EndCorrelationJob(active, new InvalidDataException("Quest rejected the canonical correlation result.", exception));
            }
        }

        private Task<bool> RequestCorrelationAsync(V2CorrelationRequest request, CancellationToken stop) => RequestCorrelationCoreAsync(request, stop);

        private async Task<bool> RequestCorrelationCoreAsync(V2CorrelationRequest request, CancellationToken stop)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (m_Driver.ConnectionState == V2QuestMutationConnectionState.OfflineLocal)
            {
                if (m_OfflineCorrelationActive || m_OfflineSiteFilterActive) throw new InvalidOperationException("A local site-filter or correlation job is already active for this scene.");
                if (request.Kind == V2CorrelationCommandKind.Compute && !V2CorrelationOfflineCapabilityGate.CanCompute(RequireScene(), out string explanation)) throw new InvalidOperationException(explanation);
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
                            if (request.Kind == V2CorrelationCommandKind.Load) resultBytes = request.ResultBytes;
                            else
                            {
                                IReadOnlyList<CorrelationResultData> computed = await RequireScene().ComputeCorrelationResultsAsync(update, token);
                                token.ThrowIfCancellationRequested();
                                resultBytes = CorrelationResultResource.FromResults(RequireScene(), computed).Encode();
                            }

                            SetCorrelationResult result = m_Boundary.CreateCorrelationResult(jobId, generation, resultBytes);
                            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, token);
                            m_Boundary.Apply(result, V2MutationApplicationOrigin.Remote, jobId);
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

            if (request.Kind != V2CorrelationCommandKind.Compute) throw new InvalidOperationException("Loaded correlation data may only be imported by the Desktop while online.");
            if (m_Transport.State != V2PersistentTransportState.Connected) throw new IOException("The Quest connection is recovering; retry the correlation request after reconnection.");
            if (m_ActiveCorrelationJob != null || m_ActiveSiteFilterJob != null) throw new InvalidOperationException("A scene-wide filter or correlation job is already active.");
            if (!m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope)) throw new InvalidOperationException("Correlation processing cannot start while the scene is updating its activity projection.");

            OperationId requestId = CreateCorrelationOperationId();
            var activeJob = new ActiveCorrelationJob(requestId, 0, CancellationTokenSource.CreateLinkedTokenSource(stop), activityScope, localRequest: true);
            m_ActiveCorrelationJob = activeJob;
            try
            {
                byte[] command = V2CorrelationRequestCodec.EncodeComputeRequest(request);
                SendCorrelationControl(new V2CorrelationControl(V2CorrelationControlKind.Request, requestId, 0, command));
                if (request.ExternalLoadingIndicator) await WaitWithCancellationAsync(activeJob.Completion.Task, activeJob.Cancellation.Token);
                else await LoadingManager.LoadDelayedAsync(async (_, token) => await WaitWithCancellationAsync(activeJob.Completion.Task, token), activeJob.Cancellation.Token, showInformations: false);
                return true;
            }
            catch (OperationCanceledException)
            {
                SendCorrelationControl(new V2CorrelationControl(V2CorrelationControlKind.Cancel, activeJob.JobId, activeJob.Generation));
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
                        V2CorrelationControlKind kind = terminalFailure is OperationCanceledException ? V2CorrelationControlKind.Cancel : V2CorrelationControlKind.Failed;
                        string code = kind == V2CorrelationControlKind.Failed ? "quest_loading_failed" : null;
                        try
                        {
                            SendCorrelationControl(new V2CorrelationControl(kind, active.JobId, active.Generation, failureCode: code));
                        }
                        catch (Exception exception)
                        {
                            terminalFailure = exception;
                        }
                    }

                    EndCorrelationJob(active, terminalFailure);
                }

                if (!m_Disposed && m_Driver.ConnectionState != V2QuestMutationConnectionState.OfflineLocal && (m_CompletedCheckpoint != null || m_DeferredDrainPending))
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
                    m_SceneOperationBulkReceiver.Reset();
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

        private void SendCorrelationControl(V2CorrelationControl control)
        {
            V2EnqueueResult queued = m_Transport.EnqueueSessionControl(V2CorrelationControlCodec.Encode(control), V2DeliveryReliability.Reliable);
            if (!queued.Accepted) throw new IOException("Quest could not retain a correlation session control: " + queued.Disposition + ".");
        }

        private static OperationId CreateCorrelationOperationId()
        {
            Guid value;
            do value = Guid.NewGuid();
            while (value == Guid.Empty);
            return new OperationId(value);
        }

        private async Task ApplySiteFilterResultAsync(V2TransportRecord record, SetSiteFilterResult result, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            ActiveSiteFilterJob active = m_ActiveSiteFilterJob;
            if (record.CanonicalSequence == null || !result.JobId.Equals(record.MessageId) || active == null || !active.JobId.Equals(result.JobId) || active.Generation != result.Generation || active.Cancellation.IsCancellationRequested)
            {
                if (record.CanonicalSequence.HasValue && record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence)
                    m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                return;
            }

            try
            {
                await ApplyTrackedMutationAsync(record, result, () => m_Driver.ReceiveCanonical(record.SceneId, record.IncarnationId, record.MessageId, record.CanonicalSequence.Value, result), firstReceived, lastReceived, stop).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                SendSiteFilterControl(new V2SiteFilterControl(V2SiteFilterControlKind.Ready, result.JobId, result.Generation));
                EndSiteFilterJob(active, null);
            }
            catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException || exception is InvalidOperationException || exception is KeyNotFoundException)
            {
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                if (record.CanonicalSequence.Value > m_Driver.LastObservedCanonicalSequence)
                    m_Driver.AdvanceCanonicalWatermark(record.CanonicalSequence.Value);
                SendSiteFilterControl(new V2SiteFilterControl(V2SiteFilterControlKind.Failed, result.JobId, result.Generation, failureCode: "quest_apply_failed"));
                EndSiteFilterJob(active, new InvalidDataException("Quest rejected the canonical site-filter result.", exception));
            }
        }

        private Task<bool> RequestSiteFilterAsync(V2SiteFilterRequest request, CancellationToken stop)
        {
            return RequestSiteFilterCoreAsync(request, stop);
        }

        private async Task<bool> RequestSiteFilterCoreAsync(V2SiteFilterRequest request, CancellationToken stop)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            if (m_Driver.ConnectionState == V2QuestMutationConnectionState.OfflineLocal)
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
                            if (!V2SiteFilterOfflineCapabilityGate.CanEvaluate(request, RequireScene(), m_Boundary, out string explanation))
                                throw new InvalidOperationException(explanation);
                            V2SiteFilterEvaluationResult evaluation = await V2SiteFilterEvaluator.EvaluateAsync(RequireScene(), m_Boundary, request, progress => update?.Invoke(progress, 0f, new LoadingText("Filtering sites")), token);
                            SetSiteFilterResult result = m_Boundary.CreateSiteFilterResult(jobId, generation, evaluation.Included, evaluation.RosterHash);
                            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, token);
                            m_Boundary.Apply(result, V2MutationApplicationOrigin.Remote, jobId);
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

            if (m_Transport.State != V2PersistentTransportState.Connected)
                throw new IOException("The Quest connection is recovering; retry the filter after the session reconnects.");
            if (m_ActiveSiteFilterJob != null || m_ActiveCorrelationJob != null) throw new InvalidOperationException("A site-filter or correlation job is already active for this scene.");
            if (!m_Boundary.TryBeginSensitiveActivityOperation(out IDisposable activityScope))
                throw new InvalidOperationException("Site filtering cannot start while the scene is updating its activity projection.");

            OperationId requestId = CreateSiteFilterOperationId();
            var activeJob = new ActiveSiteFilterJob(requestId, 0, CancellationTokenSource.CreateLinkedTokenSource(stop), activityScope, localRequest: true);
            m_ActiveSiteFilterJob = activeJob;
            try
            {
                byte[] command = V2SiteFilterRequestCodec.Encode(request);
                SendSiteFilterControl(new V2SiteFilterControl(V2SiteFilterControlKind.Request, requestId, 0, command));
                if (request.ExternalLoadingIndicator) await WaitWithCancellationAsync(activeJob.Completion.Task, activeJob.Cancellation.Token);
                else await LoadingManager.LoadDelayedAsync(async (_, token) => await WaitWithCancellationAsync(activeJob.Completion.Task, token), activeJob.Cancellation.Token, showInformations: false);
                return true;
            }
            catch (OperationCanceledException)
            {
                SendSiteFilterControl(new V2SiteFilterControl(V2SiteFilterControlKind.Cancel, activeJob.JobId, activeJob.Generation));
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
                        V2SiteFilterControlKind terminalKind = terminalFailure is OperationCanceledException ? V2SiteFilterControlKind.Cancel : V2SiteFilterControlKind.Failed;
                        string failureCode = terminalKind == V2SiteFilterControlKind.Failed ? "quest_loading_failed" : null;
                        try
                        {
                            SendSiteFilterControl(new V2SiteFilterControl(terminalKind, active.JobId, active.Generation, failureCode: failureCode));
                        }
                        catch (Exception exception)
                        {
                            terminalFailure = exception;
                        }
                    }

                    EndSiteFilterJob(active, terminalFailure);
                }

                if (!m_Disposed && m_Driver.ConnectionState != V2QuestMutationConnectionState.OfflineLocal && (m_CompletedCheckpoint != null || m_DeferredDrainPending))
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
                    m_SceneOperationBulkReceiver.Reset();
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

        private bool IsAbandonedSiteFilterChunk(V2TransportRecord record)
        {
            return record != null && record.Lane == V2ScheduleLane.Bulk && record.MessageId != null && (m_AbandonedSiteFilterTransfers.Contains(record.MessageId.Value) || m_AbandonedCorrelationTransfers.Contains(record.MessageId.Value));
        }

        private void SendSiteFilterControl(V2SiteFilterControl control)
        {
            V2EnqueueResult queued = m_Transport.EnqueueSessionControl(V2SiteFilterControlCodec.Encode(control), V2DeliveryReliability.Reliable);
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
            CancelSiteFilterJobAfterDisconnectAsync().Forget();
            CancelCorrelationJobAfterDisconnectAsync().Forget();
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
            int size = checked(V2TransportFrameCodec.HeaderLength + record.Record.PayloadLength);
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
                        if (!m_SceneOperationBulkReceiver.TryAppendNextBuffered(m_DeferredRecords, pending => pending.Record, out DeferredRecord buffered, out V2TransportRecord completedMutation))
                            break;

                        m_DeferredBytes -= V2TransportFrameCodec.HeaderLength + buffered.Record.PayloadLength;
                        if (completedMutation != null)
                            await ProcessRecordAsync(completedMutation, completedMutation.FirstReceived, completedMutation.LastReceived, stop).ConfigureAwait(false);
                        if (m_CompletedCheckpoint != null)
                            await ApplyRetainedCheckpointAsync(stop).ConfigureAwait(false);
                        if (m_AfterDeferredRecordProcessed != null)
                            await m_AfterDeferredRecordProcessed(buffered.Record, stop).ConfigureAwait(false);
                        continue;
                    }

                    if (m_DeferredRecords.Count == 0) break;
                    DeferredRecord pending = m_DeferredRecords.Dequeue();
                    m_DeferredBytes -= V2TransportFrameCodec.HeaderLength + pending.Record.PayloadLength;
                    if (IsAbandonedSiteFilterChunk(pending.Record)) continue;
                    await ProcessRecordAsync(pending.Record, pending.FirstReceived, pending.LastReceived, stop).ConfigureAwait(false);
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
            m_CompletedCheckpoint = null;
            m_CompletedCheckpointOperation = null;
        }

        private async Task ApplyTrackedMutationAsync(V2TransportRecord record, V2Mutation mutation, Func<bool> apply, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            bool measured = SyncTelemetry.Enabled && TryGetTelemetryProfile(mutation, out _);
            SyncProfile profile = measured ? GetTelemetryProfile(mutation) : default;
            SyncTelemetryIdentity identity = measured ? CreateTelemetryIdentity(record.MessageId) : default;
            var telemetry = measured ? new SyncReceiveTelemetry(firstReceived, lastReceived, V2TransportFrameCodec.HeaderLength + record.PayloadLength) : null;
            try
            {
                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
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

        private static bool TryGetTelemetryProfile(V2Mutation mutation, out SyncProfile profile)
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

        private static SyncProfile GetTelemetryProfile(V2Mutation mutation)
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
            V2PublishedSceneCheckpoint checkpoint = await Task.Run(() => V2SceneMutationCheckpointCodec.Decode(encodedCheckpoint), stop).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
            m_Boundary.ApplyCheckpoint(checkpoint.Checkpoint, operationId);
            m_Driver.AdvanceCanonicalWatermark(checkpoint.CanonicalSequence);
        }

        private void ValidateScope(V2TransportRecord record)
        {
            if (record == null || !record.SessionId.Equals(m_Identity.SessionId) || record.SceneId == null || !record.SceneId.Equals(m_Identity.SceneId) || record.IncarnationId == null || !record.IncarnationId.Equals(m_Identity.IncarnationId))
                throw new InvalidDataException("An incoming v2 record belongs to a different published scene incarnation.");
        }

        private static bool IsCheckpointBody(V2TransportRecord record)
        {
            if (record.PayloadLength < 4) return false;
            byte[] bytes = record.GetPayloadCopy();
            return bytes[0] == (byte)'H' && bytes[1] == (byte)'B' && bytes[2] == (byte)'C' && bytes[3] == (byte)'P';
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed) throw new ObjectDisposedException(nameof(QuestV2ReplicaSession));
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Driver.ProposalQueued -= OnProposalQueued;
            m_Driver.OfflineLocalEntered -= OnOfflineLocalEntered;
            m_SiteFilterRequestRegistration?.Dispose();
            m_CorrelationRequestRegistration?.Dispose();
            if (m_ActiveSiteFilterJob != null) EndSiteFilterJob(m_ActiveSiteFilterJob, new OperationCanceledException("The Quest replica session was disposed."));
            if (m_ActiveCorrelationJob != null) EndCorrelationJob(m_ActiveCorrelationJob, new OperationCanceledException("The Quest replica session was disposed."));
            m_Transport.Dispose();
            m_Driver.Dispose();
            m_Boundary.Dispose();
            m_CheckpointBulkReceiver.Reset();
            m_SceneOperationBulkReceiver.Reset();
            m_DeferredRecords.Clear();
            m_DeferredBytes = 0;
            m_CompletedCheckpoint = null;
            m_CompletedCheckpointOperation = null;
            m_DeferredDrainPending = false;
            m_VisibilityAvailable.Dispose();
        }
    }
}
