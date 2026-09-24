using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using UnityEngine;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.UI.Tools;
using HBP.Sync;
using HBP.Sync.Scene;
using System.Text;
using System.Linq;

namespace HBP.Quest
{
    public enum AnatomyReceptionState
    {
        Idle,
        Connecting,
        Receiving,
        Preparing
    }

    /// <summary>Owns delivery identity independently of connections; all publication/lifecycle changes use Unity's thread.</summary>
    public sealed class QuestAnatomySession : MonoBehaviour
    {
        public const int MaximumDeliveryHistory = 256;
        private readonly ReceivedSurfaceCache surfaceCache = new();
        [SerializeField] private QuestAnatomyView view;
        private readonly Dictionary<string, Entry> deliveries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private SynchronizationContext unityContext;
        private int mainThread;
        private CancellationTokenSource reception;
        private Entry current;
        private bool destroyed;
        private PairingContext globals;
        private SceneArchive globalArchive;
        private readonly SemaphoreSlim publicationGate = new(1, 1);
        private LiveGeometryStateAdapter replica;
        private StateSnapshot acceptedReplica;
        private int replicaGeneration;
        public ulong ReceivedRevision { get; private set; }
        public ulong AppliedRevision { get; private set; }
        public ulong VisibleRevision { get; private set; }
        public AnatomyReceptionState ReceptionState { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsReady => current != null && view != null && view.Scene != null;
        public string TransferId => current?.TransferId;
        public string ContentHash => current?.ContentHash;
        public string SessionId => current?.SessionId;
        public string LastError { get; private set; }
        public long ReceivedBytes { get; private set; }
        public long TotalBytes { get; private set; }
        private Action<float, string> loadingProgress;
        private readonly object telemetryGate = new();
        private SyncTelemetryPoint firstTransferByte;
        private SyncTelemetryPoint lastTransferByte;
        private long transferWireBytes;
        private InitialTransferTelemetry pendingInitialTelemetry;

        private sealed class InitialTransferTelemetry
        {
            public string TransferId;
            public SyncReceiveTelemetry Trace;
        }

        private readonly struct ReceiveResult
        {
            public readonly DeliveryReceipt Receipt;
            public readonly Exception Error;

            public ReceiveResult(DeliveryReceipt receipt, Exception error)
            {
                Receipt = receipt;
                Error = error;
            }
        }

        private readonly struct MeasuredProfiles
        {
            private readonly byte m_Bits;

            private MeasuredProfiles(byte bits) => m_Bits = bits;

            public int Count => (m_Bits & 1) + ((m_Bits >> 1) & 1) + ((m_Bits >> 2) & 1);

            public SyncProfile this[int index]
            {
                get
                {
                    int current = 0;
                    for (int bit = 0; bit < 3; bit++)
                    {
                        if ((m_Bits & (1 << bit)) == 0) continue;
                        if (current++ == index)
                        {
                            return bit switch
                            {
                                0 => SyncProfile.SiteColor,
                                1 => SyncProfile.CutDefinition,
                                _ => SyncProfile.TimelineAnchor
                            };
                        }
                    }

                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public MeasuredProfiles Add(SyncProfile profile)
            {
                int bit = profile switch
                {
                    SyncProfile.SiteColor => 0,
                    SyncProfile.CutDefinition => 1,
                    SyncProfile.TimelineAnchor => 2,
                    _ => throw new ArgumentOutOfRangeException(nameof(profile))
                };
                return new MeasuredProfiles((byte)(m_Bits | (1 << bit)));
            }
        }

        private sealed class Entry
        {
            public string TransferId;
            public string SessionId;
            public string ContentHash;
            public DeliveryStatus Status;
        }

        private void Awake()
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            unityContext = SynchronizationContext.Current;
        }

        /// <summary>Accept an authenticated incoming stream using the same publication and cancellation owner.</summary>
        public async Task<DeliveryReceipt> ReceiveStreamAsync(Stream stream, CancellationToken stop)
        {
            Task<DeliveryReceipt> receptionTask = await OnUnityThreadAsync(() => ReceiveWithLoadingAsync(stream, stop, false), stop).ConfigureAwait(false);
            return await receptionTask.ConfigureAwait(false);
        }

        private async Task<DeliveryReceipt> ReceiveWithLoadingAsync(Stream stream, CancellationToken stop, bool globalsTransfer)
        {
            ReceiveResult result = await LoadingManager.LoadAsync<ReceiveResult>(async update =>
            {
                try
                {
                    Task<DeliveryReceipt> work = await OnUnityThreadAsync(() =>
                    {
                        loadingProgress = (value, message) => update(value, 0, new LoadingText(message));
                        loadingProgress(0.02f, globalsTransfer ? "Receiving pairing data" : "Connecting to Desktop");
                        if (globalsTransfer)
                            return ReceiveCoreAsync(async (token, publish, progress) =>
                            {
                                using var close = token.Register(stream.Dispose);
                                return await ReceiveFileAsync(stream, token, publish, progress).ConfigureAwait(false);
                            }, stop, InstallGlobalsAsync);
                        return ReceiveCoreAsync(async (token, publish, progress) =>
                        {
                            using var closeStream = token.Register(stream.Dispose);
                            return await ReceiveFileAsync(stream, token, publish, progress, true).ConfigureAwait(false);
                        }, stop);
                    }, stop).ConfigureAwait(false);
                    DeliveryReceipt receipt = await work.ConfigureAwait(false);
                    await UniTask.SwitchToMainThread();
                    loadingProgress?.Invoke(1, globalsTransfer ? "Quest paired" : "Visualization ready");
                    return new ReceiveResult(receipt, null);
                }
                catch (Exception error)
                {
                    if (error is not OperationCanceledException && !stop.IsCancellationRequested)
                        Debug.LogError($"Quest {(globalsTransfer ? "pairing data" : "visualization")} reception failed: {error}");
                    return new ReceiveResult(null, error);
                }
                finally
                {
                    await UniTask.SwitchToMainThread();
                    loadingProgress = null;
                }
            });
            if (result.Error != null) throw result.Error;
            return result.Receipt;
        }

        public async Task<DeliveryReceipt> ReceiveGlobalsAsync(Stream stream, CancellationToken stop)
        {
            if (await OnUnityThreadAsync(() => IsReady, stop).ConfigureAwait(false))
                throw new InvalidOperationException("Close the active visualization before replacing shared dependencies.");
            Task<DeliveryReceipt> work = await OnUnityThreadAsync(() => ReceiveWithLoadingAsync(stream, stop, true), stop).ConfigureAwait(false);
            return await work.ConfigureAwait(false);
        }

        private async Task<DeliveryStatus> InstallGlobalsAsync(string file, string hash, CancellationToken stop)
        {
            await OnUnityThreadAsync(() =>
            {
                ReceptionState = AnatomyReceptionState.Preparing;
                loadingProgress?.Invoke(0.85f, "Preparing Quest data");
                return 0;
            }, stop).ConfigureAwait(false);
            string directory = await OnUnityThreadAsync(() => Path.Combine(Application.temporaryCachePath, "PairingData", Guid.NewGuid().ToString("N")), stop).ConfigureAwait(false);
            var archive = new SceneArchive(directory, true);
            bool consumed = false;
            try
            {
                var candidate = await Task.Run(() =>
                {
                    var context = new PairingContext(archive.ReadGlobalData(file, stop));
                    context.RestoreFilterPresets(archive);
                    return context;
                }, stop).ConfigureAwait(false);
                Task installation = await OnUnityThreadAsync(async () =>
                {
                    if (!HBP.Core.Preferences.PersistentDataManager.IsInitialized || !HBP.Core.Database.DatabaseManager.IsInitialized)
                        throw new InvalidOperationException("The Quest common services must be initialized before pairing.");
                    await view.ClearAsync();
                    surfaceCache.Clear();
                    stop.ThrowIfCancellationRequested();
                    if (destroyed)
                        throw new ObjectDisposedException(nameof(QuestAnatomySession));
                    HBP.Core.Preferences.PersistentDataManager.ApplySessionData(candidate.Data.Preferences, candidate.Data.Tags, candidate.Data.Aliases, candidate.FilterPresets);
                    HBP.Core.Database.DatabaseManager.Database.SetProtocols(candidate.Data.Protocols, new HBP.Core.Data.ValidationRequest(HBP.Core.Data.ValidationAspect.None));
                    HBP.Core.DLL.ActivityProjectionSettings.VolumeGridDimension = candidate.Data.Grid;
                    HBP.Core.DLL.ActivityProjectionSettings.VolumeInterpolation = candidate.Data.Interpolation;
                    var previous = globalArchive;
                    globals = candidate;
                    globalArchive = archive;
                    consumed = true;
                    current = null;
                    deliveries.Clear();
                    previous?.Dispose();
                    // Warm standard resources once when pairing, before sending a visualization.
                    stop.ThrowIfCancellationRequested();
                    await HBP.Core.Tools.StandardData.EnsureInstalledAsync();
                    await HBP.Data.Module3D.Base3DScene.PrepareStandardResourcesAsync();
                    stop.ThrowIfCancellationRequested();
                }, stop).ConfigureAwait(false);
                await installation.ConfigureAwait(false);
                return DeliveryStatus.Published;
            }
            finally
            {
                if (!consumed)
                    archive.Dispose();
            }
        }

        private async Task<DeliveryReceipt> ReceiveCoreAsync(Func<CancellationToken, Func<string, string, CancellationToken, Task<DeliveryStatus>>, Action<long>, Task<DeliveryReceipt>> receive, CancellationToken stop, Func<string, string, CancellationToken, Task<DeliveryStatus>> prepare = null)
        {
            RequireMainThread();
            if (destroyed || !isActiveAndEnabled)
                throw new InvalidOperationException("The session receiver is unavailable.");
            if (view == null || unityContext == null)
                throw new InvalidOperationException("Missing serialized view or Unity synchronization context.");
            if (reception != null)
                throw new InvalidOperationException("A delivery is already in progress.");
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stop);
            reception = attempt;
            LastError = null;
            ReceivedBytes = 0;
            TotalBytes = 0;
            lock (telemetryGate)
            {
                firstTransferByte = default;
                lastTransferByte = default;
                transferWireBytes = 0;
            }

            ReceptionState = AnatomyReceptionState.Connecting;
            loadingProgress?.Invoke(0.02f, "Connecting to Desktop");

            try
            {
                var receipt = await receive(attempt.Token, prepare ?? ((file, hash, token) => PrepareAsync(file, hash, token)), count => unityContext.Post(_ =>
                {
                    if (destroyed || reception != attempt || attempt.IsCancellationRequested)
                        return;
                    IsConnected = true;
                    ReceptionState = AnatomyReceptionState.Receiving;
                    if (TotalBytes == 0) ReceivedBytes = count;
                }, null));

                return receipt;
            }
            catch (Exception exception)
            {
                // No resource release here: ACK loss can follow a successful publication.

                LastError = exception.Message;
                throw;
            }
            finally
            {
                reception = null;
                IsConnected = false;
                ReceptionState = AnatomyReceptionState.Idle;
            }
        }

        private void ReportTransferProgress(long received, long total)
        {
            unityContext.Post(_ =>
            {
                if (destroyed || reception == null || reception.IsCancellationRequested || ReceptionState == AnatomyReceptionState.Preparing) return;
                ReceivedBytes = received;
                TotalBytes = total;
                ReceptionState = AnatomyReceptionState.Receiving;
                if (total > 0)
                    loadingProgress?.Invoke(0.10f + 0.70f * Mathf.Clamp01((float)received / total), $"Receiving: {100L * Math.Min(received, total) / total}%");
            }, null);
        }

        private async Task<DeliveryReceipt> ReceiveFileAsync(Stream stream, CancellationToken stop, Func<string, string, CancellationToken, Task<DeliveryStatus>> publish, Action<long> progress, bool allowBlocks = false)
        {
            string file = Path.Combine(Application.temporaryCachePath, "scene-" + Guid.NewGuid().ToString("N") + ".hbscene");
            try
            {
                return await PinnedTlsTransfer.ReceiveFileAsync(stream, file, stop, publish, progress, allowBlocks ? (input, token) => ReceiveBlocksAsync(input, token, progress) : null, (received, total) => ReportTransferProgress(received, total), RecordTransferWireProgress).ConfigureAwait(false);
            }
            finally
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        private async Task<DeliveryReceipt> ReceiveBlocksAsync(Stream stream, CancellationToken stop, Action<long> progress)
        {
            string directory = await OnUnityThreadAsync(() => Path.Combine(Application.temporaryCachePath, "SceneRestoration", Guid.NewGuid().ToString("N")), stop).ConfigureAwait(false);
            var archive = new SceneArchive(directory, true, globals)
            {
                SurfaceCache = surfaceCache
            };
            bool consumed = false;
            try
            {
                byte[] digest = await BlockContainer.ReceiveAsync(stream, archive, stop, progress, (received, total) => ReportTransferProgress(received, total), RecordTransferWireProgress).ConfigureAwait(false);
                string hash = new DeliveryReceipt(digest, DeliveryStatus.Published).ContentHash;
                await OnUnityThreadAsync(() =>
                {
                    ReceptionState = AnatomyReceptionState.Preparing;
                    loadingProgress?.Invoke(0.85f, "Preparing visualization");
                    return 0;
                }, stop).ConfigureAwait(false);
                ScenePayload payload = await Task.Run(() => { return archive.ReadPrepared(hash); }, stop).ConfigureAwait(false);
                Task<DeliveryStatus> publication = await OnUnityThreadAsync(() => PublishAsync(payload, archive, hash, stop), stop).ConfigureAwait(false);
                DeliveryStatus result = await publication.ConfigureAwait(false);
                consumed = result == DeliveryStatus.Published;
                // Publication owns the archive even if writing the receipt fails.
                var receipt = new byte[33];
                receipt[0] = (byte)result;
                Buffer.BlockCopy(digest, 0, receipt, 1, 32);
                await stream.WriteAsync(receipt, 0, receipt.Length, stop).ConfigureAwait(false);
                return new DeliveryReceipt(digest, result);
            }
            finally
            {
                if (!consumed)
                    archive.Dispose();
            }
        }

        private async Task<DeliveryStatus> PrepareAsync(string file, string hash, CancellationToken stop)
        {
            string directory = await OnUnityThreadAsync(() =>
            {
                ReceptionState = AnatomyReceptionState.Preparing;
                loadingProgress?.Invoke(0.85f, "Preparing visualization");
                return Path.Combine(Application.temporaryCachePath, "SceneRestoration", Guid.NewGuid().ToString("N"));
            }, stop).ConfigureAwait(false);
            var archive = new SceneArchive(directory, true, globals)
            {
                SurfaceCache = surfaceCache
            };
            bool consumed = false;
            try
            {
                ScenePayload payload = await Task.Run(() => { return archive.Read(file, stop); }, stop).ConfigureAwait(false);
                Task<DeliveryStatus> publication = await OnUnityThreadAsync(() => PublishAsync(payload, archive, hash, stop), stop).ConfigureAwait(false);
                DeliveryStatus result = await publication.ConfigureAwait(false);
                consumed = result == DeliveryStatus.Published;
                return result;
            }
            finally
            {
                if (!consumed)
                    archive.Dispose();
            }
        }

        private async Task<DeliveryStatus> PublishAsync(ScenePayload snapshot, SceneArchive archive, string hash, CancellationToken stop)
        {
            await publicationGate.WaitAsync(stop);
            try
            {
                if (deliveries.TryGetValue(snapshot.TransferId, out Entry existing))
                {
                    if (existing.ContentHash != hash) throw new InvalidDataException("Transfer identity reused with different content.");
                    return existing == current ? DeliveryStatus.AlreadyPublished : existing.Status;
                }

                // Never evict tombstones: that would allow a delayed retry to resurrect old content.
                if (deliveries.Count >= MaximumDeliveryHistory) throw new InvalidOperationException("Delivery history is full; reopen the receiver before sending new identities.");
                var next = new Entry { TransferId = snapshot.TransferId, SessionId = snapshot.SessionId, ContentHash = hash, Status = DeliveryStatus.Published };
                deliveries.Add(next.TransferId, next); // Allocate metadata before the renderer commit.
                InitialTransferTelemetry telemetry = null;
                if (SyncTelemetry.Enabled)
                {
                    lock (telemetryGate)
                    {
                        telemetry = new InitialTransferTelemetry
                        {
                            TransferId = snapshot.TransferId,
                            Trace = new SyncReceiveTelemetry(firstTransferByte, lastTransferByte, transferWireBytes)
                        };
                    }
                }

                try
                {
                    telemetry?.Trace.CaptureApplyStart();
                    await view.ApplyAsync(snapshot, archive, stop); // ACK only after complete common rendering and publication.
                    telemetry?.Trace.CaptureApplyEnd();
                    if (telemetry != null)
                    {
                        pendingInitialTelemetry = telemetry;
                        _ = RecordInitialNextVisibleAsync(telemetry, stop);
                    }
                }
                catch
                {
                    deliveries.Remove(next.TransferId);
                    throw;
                }
                finally
                {
                    // Publish captured facts even if apply or the later receipt fails; never fabricate ApplyEnd.
                    telemetry?.Trace.Publish(SyncProfile.InitialTransfer, InitialTransferIdentity(snapshot.TransferId));
                }

                if (current != null) current.Status = DeliveryStatus.Superseded;
                current = next;
                replica = null;
                acceptedReplica = null;
                ReceivedRevision = AppliedRevision = VisibleRevision = 0;
                ++replicaGeneration;
                return DeliveryStatus.Published;
            }
            finally
            {
                publicationGate.Release();
            }
        }

        /// <summary>Receive ordered scientific state on a separate authenticated control stream.</summary>
        public async Task ReceiveReplicaAsync(Stream stream, CancellationToken stop)
        {
            var resources = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            StateSnapshot checkpoint = await OnUnityThreadAsync(() => acceptedReplica, stop).ConfigureAwait(false);
            await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Checkpoint, ReplicaWire.Checkpoint(checkpoint?.CommonRevision ?? 0, checkpoint == null ? new byte[32] : ReplicaWire.Hash(checkpoint)), stop).ConfigureAwait(false);
            while (!stop.IsCancellationRequested)
            {
                SyncTelemetryPoint firstByte = default;
                SyncTelemetryPoint lastByte = default;
                Action<ReplicaReadStage> measure = null;
                if (SyncTelemetry.Enabled)
                    measure = stage =>
                    {
                        if (stage == ReplicaReadStage.FirstBytes)
                            firstByte = SyncTelemetry.CapturePoint();
                        else
                            lastByte = SyncTelemetry.CapturePoint();
                    };
                var frame = await ReplicaWire.ReadAsync(stream, stop, measure).ConfigureAwait(false);
                if (frame.Kind == ReplicaFrameKind.Resource)
                {
                    var resource = ReplicaWire.ReadResource(frame.Body);
                    if (resources.Count >= 8 && !resources.ContainsKey(resource.Reference)) throw new InvalidDataException("Too many pending replica resources.");
                    resources[resource.Reference] = resource.Data;
                    await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Applied, ReplicaWire.Revision(0), stop).ConfigureAwait(false);
                    continue;
                }

                if (frame.Body.Length < 5) throw new InvalidDataException("Truncated replica clock and state.");
                float senderClock = BitConverter.ToSingle(frame.Body, 0);
                byte[] stateBytes = new byte[frame.Body.Length - 4];
                Buffer.BlockCopy(frame.Body, 4, stateBytes, 0, stateBytes.Length);
                StateSnapshot snapshot = frame.Kind == ReplicaFrameKind.Snapshot ? SharedStateCodec.Decode(stateBytes) : null;
                ReplicaDelta delta = frame.Kind == ReplicaFrameKind.Delta ? ReplicaDelta.Decode(stateBytes) : null;
                if (snapshot == null && delta == null) throw new InvalidDataException("Unexpected Desktop replica message.");
                ulong revision = snapshot?.CommonRevision ?? delta.Revision;
                Guid epoch = snapshot?.EpochId ?? acceptedReplica?.EpochId ?? Guid.Empty;
                MeasuredProfiles profiles = SyncTelemetry.Enabled ? GetMeasuredProfiles(delta) : default;
                SyncReceiveTelemetry telemetry = profiles.Count == 0 ? null : new SyncReceiveTelemetry(firstByte, lastByte, frame.Body.Length + 5L);
                try
                {
                    await OnUnityThreadAsync(() => ReceivedRevision = revision, stop).ConfigureAwait(false);
                    await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Received, ReplicaWire.Revision(revision), stop).ConfigureAwait(false);
                    Task application = await OnUnityThreadAsync(() => ApplyReplicaAsync(snapshot, delta, senderClock, resources, telemetry, stop), stop).ConfigureAwait(false);
                    await application.ConfigureAwait(false);
                    resources.Clear();
                    await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Applied, ReplicaWire.Revision(revision), stop).ConfigureAwait(false);
                    Task visible = await OnUnityThreadAsync(() => WaitReplicaVisibleAsync(revision, stop, telemetry), stop).ConfigureAwait(false);
                    await visible.ConfigureAwait(false);
                    await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Visible, ReplicaWire.Revision(revision), stop).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is InvalidDataException || exception is InvalidOperationException || exception is ArgumentException)
                {
                    byte[] message = Encoding.UTF8.GetBytes(exception.Message);
                    await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Rejected, message, stop).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    // IOException, cancellation and even a failed rejection ACK retain the phases already reached.
                    for (int i = 0; i < profiles.Count; i++)
                        telemetry?.Publish(profiles[i], ReplicaIdentity(epoch, revision));
                }
            }
        }

        private async Task ApplyReplicaAsync(StateSnapshot snapshot, ReplicaDelta delta, float senderClock, Dictionary<string, byte[]> resources, SyncReceiveTelemetry telemetry, CancellationToken stop)
        {
            await publicationGate.WaitAsync(stop);
            try
            {
                if (!IsReady) throw new InvalidOperationException("No published scene for replica state.");
                if (delta != null)
                {
                    if (acceptedReplica == null) throw new InvalidDataException("A full replica checkpoint is required.");
                    snapshot = delta.Apply(acceptedReplica);
                }

                if (acceptedReplica != null && snapshot.CommonRevision <= acceptedReplica.CommonRevision)
                {
                    if (snapshot.CommonRevision == acceptedReplica.CommonRevision && SharedStateCodec.Encode(snapshot).AsSpan().SequenceEqual(SharedStateCodec.Encode(acceptedReplica))) return;
                    throw new InvalidDataException("Stale or divergent replica revision.");
                }

                LiveGeometryStateAdapter applying = replica;
                if (applying == null)
                {
                    if (snapshot.CommonRevision < 1) throw new InvalidDataException("Invalid initial replica revision.");
                    byte[] hash = new byte[32];
                    for (int i = 0; i < hash.Length; i++) hash[i] = Convert.ToByte(current.ContentHash.Substring(i * 2, 2), 16);
                    var binding = PreparedSceneDeliveryBinding.FromPublished(new DeliveryReceipt(hash, DeliveryStatus.Published), view.PublishedScene);
                    var candidate = new LiveGeometryStateAdapter(view.Scene, snapshot.EpochId, binding);
                    foreach (var resource in resources) candidate.PrepareCorrelationResource(resource.Key, resource.Value);
                    candidate.BindInitialState(snapshot);
                    applying = candidate;
                }
                else
                    foreach (var resource in resources)
                        applying.PrepareCorrelationResource(resource.Key, resource.Value);

                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
                deadline.CancelAfter(TimeSpan.FromSeconds(30));
                await UniTask.WaitUntil(() => view.Scene != null && view.Scene.CanApplyLegacyStateSnapshot, cancellationToken: deadline.Token);
                telemetry?.CaptureApplyStart();
                applying.Apply(ReplicaClock.ToLocalClock(snapshot, senderClock, Time.realtimeSinceStartup), delta);
                telemetry?.CaptureApplyEnd();
                replica = applying;
                acceptedReplica = snapshot;
                AppliedRevision = snapshot.CommonRevision;
            }
            finally
            {
                publicationGate.Release();
            }
        }

        private async Task WaitReplicaVisibleAsync(ulong revision, CancellationToken stop, SyncReceiveTelemetry telemetry)
        {
            int generation = replicaGeneration;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            if (view.Scene == null) throw new InvalidOperationException("The published scene is unavailable.");
            await view.Scene.PrepareRenderingAsync(deadline.Token);
            await UniTask.NextFrame(cancellationToken: deadline.Token);
            if (generation != replicaGeneration || acceptedReplica?.CommonRevision != revision) throw new InvalidDataException("Replica scene was replaced before visibility.");
            VisibleRevision = revision;
            // This is the Unity-thread CPU eligibility boundary, not the later worker continuation.
            telemetry?.CaptureNextVisible();
        }

        private async UniTask RecordInitialNextVisibleAsync(InitialTransferTelemetry telemetry, CancellationToken stop)
        {
            try
            {
                await UniTask.NextFrame(cancellationToken: stop);
                if (!ReferenceEquals(pendingInitialTelemetry, telemetry) || current?.TransferId != telemetry.TransferId || view == null || view.Scene == null) return;
                telemetry.Trace.CaptureNextVisible();
                SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, InitialTransferIdentity(telemetry.TransferId), SyncMilestone.NextVisible, telemetry.Trace.NextVisible);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            finally
            {
                if (ReferenceEquals(pendingInitialTelemetry, telemetry)) pendingInitialTelemetry = null;
            }
        }

        private void RecordTransferWireProgress(long received)
        {
            if (!SyncTelemetry.Enabled)
                return;
            SyncTelemetryPoint point = SyncTelemetry.CapturePoint();
            lock (telemetryGate)
            {
                if (!firstTransferByte.IsValid)
                    firstTransferByte = point;
                lastTransferByte = point;
                transferWireBytes = received;
            }
        }

        private static MeasuredProfiles GetMeasuredProfiles(ReplicaDelta delta)
        {
            if (delta == null)
                return default;
            MeasuredProfiles profiles = default;
            foreach (StateKey key in delta.Assignments.Keys)
                profiles = AddMeasuredProfile(profiles, key);
            foreach (StateKey key in delta.Removals)
                profiles = AddMeasuredProfile(profiles, key);
            return profiles;
        }

        private static MeasuredProfiles AddMeasuredProfile(MeasuredProfiles profiles, StateKey key)
        {
            if (key.Entity == EntityKind.Site && key.FieldId == 5)
                return profiles.Add(SyncProfile.SiteColor);
            if (key.Entity == EntityKind.Cut && key.FieldId is >= 3 and <= 6)
                return profiles.Add(SyncProfile.CutDefinition);
            if (key.Entity == EntityKind.Column && key.FieldId is >= 27 and <= 32)
                return profiles.Add(SyncProfile.TimelineAnchor);
            return profiles;
        }

        private static SyncTelemetryIdentity ReplicaIdentity(Guid epoch, ulong revision)
        {
            return SyncTelemetryIdentity.LegacyRevisionProxy(epoch.ToString("N"), unchecked((long)revision));
        }

        private static SyncTelemetryIdentity InitialTransferIdentity(string transferId)
        {
            return SyncTelemetryIdentity.Unknown(transferId);
        }

        private async Task<T> OnUnityThreadAsync<T>(Func<T> action, CancellationToken stop)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            int dispatched = 0;
            using var cancelled = stop.Register(() =>
            {
                // Once action starts, its returned task owns cancellation and cleanup. Do not
                // detach an in-flight async publication and release the reception slot early.
                if (Interlocked.CompareExchange(ref dispatched, 1, 0) == 0) completion.TrySetCanceled(stop);
            });
            unityContext.Post(_ =>
            {
                if (Interlocked.CompareExchange(ref dispatched, 1, 0) != 0) return;
                if (stop.IsCancellationRequested || destroyed)
                {
                    completion.TrySetCanceled();
                    return;
                }

                try
                {
                    completion.TrySetResult(action());
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }, null);
            return await completion.Task.ConfigureAwait(false);
        }

        public void Disconnect()
        {
            RequireMainThread();
            reception?.Cancel();
            IsConnected = false;
        }

        public void CloseSession()
        {
            RequireMainThread();
            pendingInitialTelemetry = null;
            replica = null;
            acceptedReplica = null;
            ReceivedRevision = AppliedRevision = VisibleRevision = 0;
            ++replicaGeneration;
            surfaceCache.Clear();
            Disconnect(); // Cancels queued publication before freeing resources.
            if (current != null)
                current.Status = DeliveryStatus.Closed;
            current = null;
            if (view != null)
                view.Clear();
        }

        private void RequireMainThread()
        {
            if (mainThread != Thread.CurrentThread.ManagedThreadId) throw new InvalidOperationException("Use the session on Unity's main thread.");
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Disconnect();
            // Android may kill a paused application without a quitting callback. This is explicitly a partial checkpoint.
            SyncTelemetryCapture.ExportCheckpoint("quest-pause");
        }

        private void OnDisable() => Disconnect();

        private void OnDestroy()
        {
            destroyed = true;
            CloseSession();
            deliveries.Clear();
            _ = ReleaseGlobalsAsync();
        }

        private async Task ReleaseGlobalsAsync()
        {
            try
            {
                if (!ReferenceEquals(view, null)) await view.ClearAsync();
                globalArchive?.Dispose();
                globalArchive = null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
