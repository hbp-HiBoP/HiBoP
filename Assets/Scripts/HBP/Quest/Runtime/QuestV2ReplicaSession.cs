using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;

namespace HBP.Quest
{
    /// <summary>Owns one published Quest scene's v2 mutation boundary and duplex transport.</summary>
    internal sealed class QuestV2ReplicaSession : IDisposable
    {
        private const int MaximumDeferredRecords = 512;
        private const int MaximumDeferredBytes = 4 * 1024 * 1024;
        private const int MaximumPendingVisibilityTelemetry = 1024;

        private readonly V2PreparedSceneIdentity m_Identity;
        private readonly V2SceneMutationBoundary m_Boundary;
        private readonly V2OutgoingScheduler m_Scheduler;
        private readonly V2PersistentTransport m_Transport;
        private readonly V2QuestMutationDriver m_Driver;
        private readonly V2PublicationCheckpointBulkReceiver m_CheckpointBulkReceiver = new V2PublicationCheckpointBulkReceiver();
        private readonly Queue<DeferredRecord> m_DeferredRecords = new Queue<DeferredRecord>();
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
            TransferId = binding.TransferId;
            ManifestHash = binding.ManifestHash;
            m_Boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Quest);
            m_Scheduler = new V2OutgoingScheduler(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, V2OriginDevice.Quest);
            m_Transport = new V2PersistentTransport(m_Scheduler);
            m_Driver = new V2QuestMutationDriver(m_Identity.SceneId, m_Identity.IncarnationId, m_Boundary, m_Scheduler);
            m_BeforeCheckpointApply = beforeCheckpointApply;
            m_AfterDeferredRecordProcessed = afterDeferredRecordProcessed;
            m_Driver.ProposalQueued += OnProposalQueued;
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

                await ProcessRecordAsync(record, received.FirstReceived, received.LastReceived, stop).ConfigureAwait(false);
            }
        }

        private async Task ProcessRecordAsync(V2TransportRecord record, SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, CancellationToken stop)
        {
            ValidateScope(record);
            if (record.Kind != V2TransportMessageKind.Application)
                throw new InvalidDataException("Unexpected non-application v2 record in the scene mutation stream.");

            if (m_CheckpointBulkReceiver.IsCheckpointDescriptor(record))
            {
                m_CheckpointBulkReceiver.Begin(record, V2OriginDevice.Desktop);
                return;
            }

            if (record.Lane == V2ScheduleLane.SceneControl && record.BodySchema == V2PublicationCheckpointBulkReceiver.BodySchema && IsCheckpointBody(record))
            {
                RetainCompletedCheckpoint(record.GetPayloadCopy(), record.MessageId);
                await ResumeCompletedCheckpointAndDrainAsync(stop).ConfigureAwait(false);
                return;
            }

            byte[] payload = record.GetPayloadCopy();
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

        private async Task ResumeCompletedCheckpointAndDrainAsync(CancellationToken stop)
        {
            if (m_CompletedCheckpoint != null)
            {
                if (m_BeforeCheckpointApply != null)
                    await m_BeforeCheckpointApply(stop).ConfigureAwait(false);
                await ApplyCheckpointAsync(m_CompletedCheckpoint, m_CompletedCheckpointOperation, stop).ConfigureAwait(false);
                m_CompletedCheckpoint = null;
                m_CompletedCheckpointOperation = null;
            }

            if (!m_DeferredDrainPending) return;
            while (m_DeferredRecords.Count > 0)
            {
                DeferredRecord pending = m_DeferredRecords.Peek();
                await ProcessRecordAsync(pending.Record, pending.FirstReceived, pending.LastReceived, stop).ConfigureAwait(false);
                m_DeferredRecords.Dequeue();
                m_DeferredBytes -= V2TransportFrameCodec.HeaderLength + pending.Record.PayloadLength;
                if (m_AfterDeferredRecordProcessed != null)
                    await m_AfterDeferredRecordProcessed(pending.Record, stop).ConfigureAwait(false);
            }

            m_DeferredDrainPending = false;
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
            m_Transport.Dispose();
            m_Driver.Dispose();
            m_Boundary.Dispose();
            m_CheckpointBulkReceiver.Reset();
            m_DeferredRecords.Clear();
            m_DeferredBytes = 0;
            m_CompletedCheckpoint = null;
            m_CompletedCheckpointOperation = null;
            m_DeferredDrainPending = false;
            m_VisibilityAvailable.Dispose();
        }
    }
}
