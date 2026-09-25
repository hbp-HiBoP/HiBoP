using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
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
        private readonly V2PreparedSceneIdentity m_Identity;
        private readonly V2SceneMutationBoundary m_Boundary;
        private readonly V2DesktopMutationAuthority m_Authority;
        private readonly V2PublicationMutationJournal m_Journal;
        private readonly V2OutgoingScheduler m_Scheduler;
        private readonly V2PersistentTransport m_Transport;
        private readonly Func<string, byte[], byte[], CancellationToken, V2PersistentTransport, Task> m_OpenReplica;
        private readonly CancellationTokenSource m_Lifetime = new CancellationTokenSource();
        private readonly CancellationTokenSource m_PublicationAbort = new CancellationTokenSource();
        private readonly TaskCompletionSource<OperationId> m_InitialApplyAcknowledged = new TaskCompletionSource<OperationId>(TaskCreationOptions.RunContinuationsAsynchronously);
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
            m_Identity = V2PreparedSceneIdentity.Create(globalContextId, scene.Visualization.ID, transferId);
            m_Boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Desktop);
            m_Authority = new V2DesktopMutationAuthority(m_Identity.SceneId, m_Identity.IncarnationId, m_Boundary);
            m_Journal = new V2PublicationMutationJournal(m_Identity.SceneId, m_Identity.IncarnationId);
            m_Scheduler = new V2OutgoingScheduler(m_Identity.SessionId, m_Identity.SceneId, m_Identity.IncarnationId, V2OriginDevice.Desktop);
            m_Transport = new V2PersistentTransport(m_Scheduler);
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
        }

        public async Task StartAfterPublicationAsync(PreparedSceneDeliveryBinding binding, string host, byte[] pin, byte[] credential, CancellationToken stop)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            ValidateBinding(binding);
            stop.ThrowIfCancellationRequested();
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
            _ = ObserveIncomingAsync(m_IncomingTask);
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
            V2EnqueueResult queued = m_Transport.EnqueueMutation(mutation.Mutation, mutation.CanonicalSequence, null, coalesciblePreview: false, operationId: mutation.OperationId);
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
                if (record.Kind != V2TransportMessageKind.Application) continue;
                byte[] payload = record.GetPayloadCopy();
                if (record.Lane == V2ScheduleLane.SessionControl)
                {
                    if (!V2PublicationControlCodec.TryDecodeAcknowledgement(payload, out OperationId barrierId))
                        throw new InvalidDataException("Unsupported v2 session-control application message.");
                    m_InitialApplyAcknowledged.TrySetResult(barrierId);
                    continue;
                }

                if (record.Lane != V2ScheduleLane.Interactive || record.OriginDevice != V2OriginDevice.Quest || !record.ObservedCanonicalSequence.HasValue || record.Mutation == null)
                    throw new InvalidDataException("Unexpected v2 Quest application record.");

                await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization, stop);
                V2DesktopProposalResult result = m_Authority.AcceptQuestProposal(record.SceneId, record.IncarnationId, record.MessageId, record.Mutation, record.ObservedCanonicalSequence.Value);
                if (result.Correction != null)
                {
                    EnqueueQuestProposalDecision(record, V2QuestProposalDecisionCodec.EncodeCorrection(result.Correction));
                }
                else if (result.CanonicalMutation == null && result.Outcome != V2ProposalOutcome.Duplicate)
                {
                    EnqueueQuestProposalDecision(record, V2QuestProposalDecisionCodec.EncodeRejection(record.MessageId, result.RejectionCode ?? "proposal_rejected"));
                }
            }
        }

        private void EnqueueQuestProposalDecision(V2TransportRecord proposal, byte[] body)
        {
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_Identity.SceneId, m_Identity.IncarnationId, proposal.Mutation);
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

        private async Task ObserveIncomingAsync(Task incoming)
        {
            try
            {
                await incoming.ConfigureAwait(false);
                if (!m_Lifetime.IsCancellationRequested) MarkConnectionClosed("The Quest v2 mutation receiver ended.");
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested)
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
        }

        private void OnAuthorityFailure(string reason)
        {
            lock (m_Gate) AbortLocked(false, "The v2 Desktop authority stopped: " + reason + ".");
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
            m_Lifetime.Cancel();
            m_PublicationAbort.Cancel();
            m_ConnectionLifetime?.Cancel();
            m_Transport.Dispose();
            m_Authority.Dispose();
            m_Boundary.Dispose();
            m_Lifetime.Dispose();
            m_PublicationAbort.Dispose();
            m_ConnectionLifetime?.Dispose();
        }

        internal sealed class V2PublicationRestartException : OperationCanceledException
        {
            public V2PublicationRestartException(string message) : base(message)
            {
            }
        }
    }
}
