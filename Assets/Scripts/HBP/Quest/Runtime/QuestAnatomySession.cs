using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
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
        private SessionPreferencesReceiver sessionPreferences;
        private SessionAtlasPreparation atlasPreparation;
        private Task standardInstallation = Task.CompletedTask;
        private Task standardPreparation = Task.CompletedTask;

        private long controlGeneration;
        private readonly Dictionary<Guid, (byte[] Bytes, Task<SessionControlResponse> Work)> sceneControls = new();
        private readonly SemaphoreSlim receptionGate = new(1, 1);
        private readonly SemaphoreSlim loadingGate = new(1, 1);
        public bool IsSessionConnected { get; private set; }
        public string ActionStatus { get; private set; }

        public async Task<SessionControlResponse> ReceiveSessionControlAsync(SessionControlRequest request, CancellationToken token)
        {
            if ((byte)request.Kind < (byte)SessionControlKind.SceneInventory)
            {
                var response = await sessionPreferences.HandleAsync(request, token);
                await UniTask.SwitchToMainThread(token);
                if (request.Kind == SessionControlKind.Open && response.Applied)
                {
                    controlGeneration = request.Generation;
                    IsSessionConnected = true;
                }

                return response;
            }

            await UniTask.SwitchToMainThread(token);
            if (globals == null || request.ContextId != Guid.ParseExact(globals.Id, "N") || request.Generation != controlGeneration)
                return new(request.OperationId, SessionControlStatus.Stale, "The Quest connection generation has changed.");
            var bytes = SessionControlCodec.Encode(request);
            if (sceneControls.TryGetValue(request.OperationId, out var previous))
                return bytes.SequenceEqual(previous.Bytes) ? await previous.Work : new(request.OperationId, SessionControlStatus.Rejected, "Scene operation identity reused.");
            if (sceneControls.Count >= 256)
            {
                foreach (var key in sceneControls.Where(value => value.Value.Work.IsCompleted).Select(value => value.Key).Take(128).ToArray()) sceneControls.Remove(key);
                if (sceneControls.Count >= 256) return new(request.OperationId, SessionControlStatus.Rejected, "Too many pending scene operations.");
            }

            var work = HandleSceneControlAsync(request, token);
            sceneControls.Add(request.OperationId, (bytes, work));
            return await work;
        }

        private async Task<SessionControlResponse> HandleSceneControlAsync(SessionControlRequest request, CancellationToken token)
        {
            try
            {
                if (request.Kind == SessionControlKind.SceneInventory)
                {
                    var inventory = view.Scenes.Select(value => new SceneSessionEntry { VisualizationId = value.Scene.Visualization.ID, TransferId = value.TransferId, Orphan = SceneReconciliationRecord.Get(value.Scene)?.Status == ReconciliationStatus.Orphan }).ToArray();
                    return new(request.OperationId, SessionControlStatus.Applied, hasSharedScene: inventory.Any(value => !value.Orphan), body: SceneSessionState.Encode(inventory));
                }

                var close = SceneSessionState.Decode<SceneSessionClose>(request.GetBody());
                if (!Guid.TryParseExact(close.TransferId, "N", out _)) throw new InvalidDataException("Invalid closing incarnation.");
                if (deliveries.TryGetValue(close.TransferId, out var entry))
                {
                    if (entry.VisualizationId != close.VisualizationId) throw new InvalidDataException("Wrong closing scene.");
                    entry.Shared = false;
                    entry.FinalDiscardSequence = close.FinalSequence;
                    entry.FinalDiscardBulk = close.BulkThrough;
                    entry.Grace?.Cancel();
                    entry.Grace?.Dispose();
                    entry.Grace = null;
                    entry.Replica?.Dispose();
                    entry.Replica = null;
                }
                else
                {
                    if (deliveries.Count >= MaximumDeliveryHistory) throw new InvalidOperationException("Scene history is full.");
                    entry = new Entry { TransferId = close.TransferId, VisualizationId = close.VisualizationId, Shared = false, Status = DeliveryStatus.Closed };
                    entry.Lifetime.Cancel();
                    deliveries.Add(close.TransferId, entry);
                }

                if (request.Kind == SessionControlKind.CloseScene)
                {
                    // This is the atomic discard boundary. No delayed frame can find this incarnation.
                    entry.Status = DeliveryStatus.Closed;
                    entry.Lifetime.Cancel();
                    entry.CloseWork ??= CloseIncarnationAsync(entry);
                    await entry.CloseWork;
                }
                else if (request.Kind == SessionControlKind.DetachScene)
                {
                    // A completed publication may be reconciling. Stop that scene's wire work
                    // and release its recovery lock before leaving an editable local copy.
                    if (entry.Preparation?.Task.IsCompleted != false) entry.Lifetime.Cancel();
                    await entry.Gate.WaitAsync(token);
                    try
                    {
                        var presentation = view.Find(close.TransferId);
                        var record = presentation == null ? null : SceneReconciliationRecord.Get(presentation.Scene);
                        record?.ReleaseReconciliationGuard();
                        record?.SetStatus(ReconciliationStatus.Orphan, "Desktop connected; this visualization is now local to Quest.");
                    }
                    finally
                    {
                        entry.Gate.Release();
                    }
                }
                else throw new InvalidDataException("Unsupported scene lifecycle request.");

                return new(request.OperationId, SessionControlStatus.Applied, hasSharedScene: view.Scenes.Any(value => SceneReconciliationRecord.Get(value.Scene)?.Status != ReconciliationStatus.Orphan));
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                return new(request.OperationId, SessionControlStatus.Failed, error.Message);
            }
        }

        private async Task CloseIncarnationAsync(Entry entry)
        {
            if (entry.Preparation != null) await entry.Preparation.Task;
            await entry.Gate.WaitAsync();
            try
            {
                await view.CloseAsync(entry.TransferId);
            }
            finally
            {
                entry.Gate.Release();
            }

            entry.Closed?.TrySetResult(true);
            if (ReferenceEquals(current, entry)) current = null;
        }

        private SceneArchive globalArchive;
        private readonly SemaphoreSlim publicationGate = new(1, 1);
        public AnatomyReceptionState ReceptionState { get; private set; }
        public bool IsConnected { get; private set; }
        public SceneReconciliationRecord Reconciliation => view != null && view.Scene != null ? SceneReconciliationRecord.Get(view.Scene) : null;
        public bool IsSynchronizationConnected => deliveries.Values.Any(value => value.Shared && value.Replica?.TransportState == PersistentTransportState.Connected);
        public bool IsSceneConnected(HBP.Data.Module3D.Base3DScene scene) => CanRequest(scene);
        public bool IsReady => view != null && view.Scenes.Count != 0;
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


        private sealed class Entry
        {
            public string TransferId;
            public string SessionId;
            public string ContentHash;
            public DeliveryStatus Status;
            public string VisualizationId;
            public PreparedSceneDeliveryBinding Binding;
            public QuestReplicaSession Replica;
            public CancellationTokenSource Grace;
            public readonly SemaphoreSlim Gate = new(1, 1);
            public bool Shared = true;
            public readonly CancellationTokenSource Lifetime = new();
            public TaskCompletionSource<bool> Closed;
            public TaskCompletionSource<bool> Preparation;
            public Task CloseWork;
            public ulong FinalDiscardSequence, FinalDiscardBulk;
        }

        private void Awake()
        {
            sessionPreferences = new SessionPreferencesReceiver(() => globals == null ? Guid.Empty : Guid.ParseExact(globals.Id, "N"), () => HBP.Core.Preferences.PersistentDataManager.UserPreferences, () => IsReady || ReceptionState == AnatomyReceptionState.Preparing, () => atlasPreparation?.Snapshot() ?? new SessionAtlasInventory());
            mainThread = Thread.CurrentThread.ManagedThreadId;
            unityContext = SynchronizationContext.Current;
            if (view != null) view.OnContextSelected.AddListener(OnContextSelected);
        }

        private void Start()
        {
            standardInstallation = InstallStandardAsync();
            standardPreparation = PrepareStandardAsync();
        }

        private async Task InstallStandardAsync()
        {
            using var timing = PairingTiming.Measure("quest.standard-installation");
            await StandardData.EnsureInstalledAsync();
        }

        private async Task PrepareStandardAsync()
        {
            using var timing = PairingTiming.Measure("quest.mni-preparation");
            try
            {
                await standardInstallation;
                if (!destroyed) await HBP.Data.Module3D.Base3DScene.PrepareStandardResourcesAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Quest background resources: " + exception.Message);
            }
        }

        /// <summary>Accept an authenticated incoming stream using the same publication and cancellation owner.</summary>
        public async Task<DeliveryReceipt> ReceiveStreamAsync(Stream stream, CancellationToken stop)
        {
            Task<DeliveryReceipt> receptionTask = await OnUnityThreadAsync(() => ReceiveWithLoadingAsync(stream, stop, false), stop).ConfigureAwait(false);
            return await receptionTask.ConfigureAwait(false);
        }

        private async Task<DeliveryReceipt> ReceiveWithLoadingAsync(Stream stream, CancellationToken stop, bool globalsTransfer)
        {
            await loadingGate.WaitAsync(stop);
            try
            {
                return await ReceiveWithLoadingCoreAsync(stream, stop, globalsTransfer);
            }
            finally
            {
                loadingGate.Release();
            }
        }

        private async Task<DeliveryReceipt> ReceiveWithLoadingCoreAsync(Stream stream, CancellationToken stop, bool globalsTransfer)
        {
            using var timing = PairingTiming.Measure(globalsTransfer ? "quest.globals-reception" : "quest.scene-reception-and-preparation");
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

        public Task<string> ActiveGlobalContextAsync(CancellationToken stop) => OnUnityThreadAsync(() => IsReady ? globals?.Id ?? string.Empty : null, stop);

        private async Task<DeliveryStatus> InstallGlobalsAsync(string file, string hash, CancellationToken stop)
        {
            using var timing = PairingTiming.Measure("quest.globals-application");
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
                    var context = new PairingContext(archive.ReadGlobalData(file, stop, inertPreferences: true));
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
                    if (sessionPreferences != null) await sessionPreferences.CloseAsync();
                    if (atlasPreparation != null) await atlasPreparation.CloseAsync();
                    candidate.Data.Preferences = archive.MaterializeReceivedPreferences();
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
                    sceneControls.Clear();
                    previous?.Dispose();
                    atlasPreparation = new SessionAtlasPreparation(candidate.Data.Preferences.Data.Atlases, () => standardInstallation.AsUniTask());
                    atlasPreparation.Start();
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
            await receptionGate.WaitAsync(stop);
            try
            {
                await UniTask.SwitchToMainThread(stop);
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
            finally
            {
                receptionGate.Release();
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
                    if (existing.ContentHash != null && existing.ContentHash != hash) throw new InvalidDataException("Transfer identity reused with different content.");
                    return view.Find(snapshot.TransferId) != null && existing.Status == DeliveryStatus.Published ? DeliveryStatus.AlreadyPublished : existing.Status;
                }

                // Never evict tombstones: that would allow a delayed retry to resurrect old content.
                if (deliveries.Count >= MaximumDeliveryHistory) throw new InvalidOperationException("Delivery history is full; reopen the receiver before sending new identities.");
                var next = new Entry { VisualizationId = snapshot.Visualization.ID, TransferId = snapshot.TransferId, SessionId = snapshot.SessionId, ContentHash = hash, Status = DeliveryStatus.Published };
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

                next.Preparation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var preparationStop = CancellationTokenSource.CreateLinkedTokenSource(stop, next.Lifetime.Token);
                try
                {
                    var replaced = view.Scenes.FirstOrDefault(value => value.Scene.Visualization.ID == snapshot.Visualization.ID);
                    if (replaced != null && deliveries.TryGetValue(replaced.TransferId, out var old))
                    {
                        old.Shared = false;
                        old.Status = DeliveryStatus.Superseded;
                        old.Lifetime.Cancel();
                        old.Grace?.Cancel();
                        old.Grace?.Dispose();
                        old.Grace = null;
                        old.Replica?.Dispose();
                        old.Replica = null;
                        SceneReconciliationRecord.Get(replaced.Scene)?.Dispose();
                    }

                    telemetry?.Trace.CaptureApplyStart();
                    await view.ApplyAsync(snapshot, archive, preparationStop.Token); // ACK only after complete common rendering and publication.
                    telemetry?.Trace.CaptureApplyEnd();
                    if (telemetry != null)
                    {
                        pendingInitialTelemetry = telemetry;
                        _ = RecordInitialNextVisibleAsync(telemetry, stop);
                    }
                }
                catch
                {
                    if (next.Shared && next.Status != DeliveryStatus.Closed) deliveries.Remove(next.TransferId);
                    else
                    {
                        next.Status = DeliveryStatus.Closed;
                        next.Lifetime.Cancel();
                    }

                    throw;
                }
                finally
                {
                    next.Preparation.TrySetResult(true);
                    // Publish captured facts even if apply or the later receipt fails; never fabricate ApplyEnd.
                    telemetry?.Trace.Publish(SyncProfile.InitialTransfer, InitialTransferIdentity(snapshot.TransferId));
                }

                if (next.Status == DeliveryStatus.Closed)
                {
                    await view.CloseAsync(next.TransferId);
                    return DeliveryStatus.Closed;
                }

                var published = view.Find(next.TransferId);
                next.Binding = PreparedSceneDeliveryBinding.FromPublished(new DeliveryReceipt(ParseContentHash(hash), DeliveryStatus.Published), published.Restored);
                var record = SceneReconciliationRecord.Retain(published.Scene, next.Binding);
                if (!next.Shared) record.SetStatus(ReconciliationStatus.Orphan, "This visualization is now local to Quest.");
                current = next;
                return DeliveryStatus.Published;
            }
            finally
            {
                publicationGate.Release();
            }
        }

        /// <summary>Route before entering any scene's unchanged replica or reconciliation protocol.</summary>
        public async Task ReceiveReplicaAsync(Stream stream, CancellationToken stop)
        {
            byte[] prefix = await ReadPrefixAsync(stream, 4, stop).ConfigureAwait(false);
            Entry entry;
            if (HasTransportMagic(prefix, ReplicaRoute.Magic))
            {
                var route = await ReplicaRoute.ReadBodyAsync(stream, stop).ConfigureAwait(false);
                entry = await OnUnityThreadAsync(() =>
                {
                    if (globals == null || route.Session.Value != Guid.ParseExact(globals.Id, "N") || !deliveries.TryGetValue(route.Incarnation.Value.ToString("N"), out var found) || !found.Shared || found.Status != DeliveryStatus.Published || found.Binding == null || !found.Binding.CreateIdentity().SceneId.Equals(route.Scene))
                        throw new InvalidDataException("The routed scene incarnation is closed or unavailable.");
                    return found;
                }, stop).ConfigureAwait(false);
                prefix = await ReadPrefixAsync(stream, 4, stop).ConfigureAwait(false);
            }
            else entry = await OnUnityThreadAsync(() => current, stop).ConfigureAwait(false);

            if (entry == null || entry.Binding == null || !entry.Shared) throw new InvalidOperationException("No shared scene is available.");
            using var sceneStop = CancellationTokenSource.CreateLinkedTokenSource(stop, entry.Lifetime.Token);
            stop = sceneStop.Token;
            var presentation = await OnUnityThreadAsync(() => view.Find(entry.TransferId), stop).ConfigureAwait(false);
            if (presentation == null) throw new InvalidDataException("The routed scene was closed.");
            if (HasTransportMagic(prefix, SceneReconciliationProtocol.Magic))
            {
                await entry.Gate.WaitAsync(stop).ConfigureAwait(false);
                try
                {
                    var record = await OnUnityThreadAsync(() =>
                    {
                        if (!entry.Shared || entry.Status != DeliveryStatus.Published) throw new InvalidOperationException("The scene was closed.");
                        entry.Grace?.Cancel();
                        entry.Grace?.Dispose();
                        entry.Grace = null;
                        entry.Replica?.Dispose();
                        entry.Replica = null;
                        return SceneReconciliationRecord.Retain(presentation.Scene, entry.Binding);
                    }, stop).ConfigureAwait(false);
                    await SceneReconciliationProtocol.ReceiveQuestAsync(stream, record, stop).ConfigureAwait(false);
                }
                finally
                {
                    entry.Gate.Release();
                }

                return;
            }

            var replay = new PrefixReadStream(prefix, stream);
            if (!HasTransportMagic(prefix, "HBT2") && !HasTransportMagic(prefix, "HBS2"))
            {
                throw new InvalidDataException("Unsupported synchronization protocol.");
            }

            var session = await OnUnityThreadAsync(() =>
            {
                if (!entry.Shared || entry.Status != DeliveryStatus.Published) throw new InvalidOperationException("The scene was closed.");
                entry.Grace?.Cancel();
                entry.Grace?.Dispose();
                entry.Grace = null;
                if (entry.Replica == null)
                {
                    entry.Replica = new QuestReplicaSession(presentation.Scene, entry.Binding);
                    entry.Replica.OnActionReceived.AddListener(action => OnSceneAction(entry, action));
                }

                return entry.Replica;
            }, stop).ConfigureAwait(false);
            try
            {
                await session.RunConnectionAsync(replay, stop).ConfigureAwait(false);
            }
            catch (Exception error) when (!stop.IsCancellationRequested && IsTransientReplicaInterruption(error))
            {
                await OnUnityThreadAsync(() =>
                {
                    StartGrace(entry, session);
                    return 0;
                }, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                if (stop.IsCancellationRequested)
                    await OnUnityThreadAsync(() =>
                    {
                        if (entry.Replica == session)
                        {
                            session.Dispose();
                            entry.Replica = null;
                        }

                        return 0;
                    }, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private static bool IsTransientReplicaInterruption(Exception error) => error is IOException or SocketException or ObjectDisposedException or InvalidOperationException;

        private void StartGrace(Entry entry, QuestReplicaSession session)
        {
            entry.Grace?.Cancel();
            entry.Grace?.Dispose();
            entry.Grace = new CancellationTokenSource();
            ExpireGraceAsync(entry, session, entry.Grace).Forget();
        }

        private async UniTask ExpireGraceAsync(Entry entry, QuestReplicaSession session, CancellationTokenSource grace)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(OutgoingScheduler.ReconnectGraceMilliseconds), DelayType.Realtime, cancellationToken: grace.Token);
                await UniTask.SwitchToMainThread();
                if (entry.Replica != session || entry.Grace != grace) return;
                session.Dispose();
                entry.Replica = null;
                entry.Grace = null;
                var presentation = view.Find(entry.TransferId);
                if (presentation != null) SceneReconciliationRecord.Get(presentation.Scene)?.SetStatus(ReconciliationStatus.Local, "Scene offline; reconnect Desktop to merge changes.");
                grace.Dispose();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelReplicaGraceExpiry()
        {
            foreach (var entry in deliveries.Values)
            {
                entry.Grace?.Cancel();
                entry.Grace?.Dispose();
                entry.Grace = null;
            }
        }

        private void OnContextSelected(HBP.Data.Module3D.Base3DScene scene, HBP.Data.Module3D.Column3D column)
        {
            var presentation = view.Find(scene);
            if (presentation == null || !deliveries.TryGetValue(presentation.TransferId, out var entry) || entry.Replica?.TransportState != PersistentTransportState.Connected || !entry.Shared) return;
            var identity = entry.Binding.CreateIdentity();
            try
            {
                entry.Replica.SendAction(new SceneAction(identity.SceneId, identity.IncarnationId, SceneActionKind.SelectContext, column?.ColumnData.ID));
            }
            catch (IOException error)
            {
                ActionStatus = error.Message;
            }
        }

        private void OnSceneAction(Entry entry, SceneAction action)
        {
            var presentation = view.Find(entry.TransferId);
            if (presentation == null || !entry.Shared) return;
            if (action.Kind == SceneActionKind.SelectContext)
            {
                var column = presentation.Scene.Columns.FirstOrDefault(value => value.ColumnData.ID == action.Column);
                if (column != null) view.SelectContext(presentation.Scene, column, false);
            }
            else if (action.Kind == SceneActionKind.Result) ActionStatus = action.Message;
        }

        public bool CanOpenPatient => view.Scene != null && view.Scene.IsVisible && view.Scene.Type == HBP.Core.Enums.SceneType.MultiPatients && view.Scene.SelectedColumn?.SelectedSite != null && CanRequest(view.Scene);
        private bool CanRequest(HBP.Data.Module3D.Base3DScene scene) => view.Find(scene) is { } presentation && deliveries.TryGetValue(presentation.TransferId, out var entry) && entry.Shared && entry.Replica?.TransportState == PersistentTransportState.Connected && SceneReconciliationRecord.Get(scene)?.Status == ReconciliationStatus.Synchronized;

        public void RequestOpenPatient()
        {
            if (!CanOpenPatient) return;
            var scene = view.Scene;
            var entry = deliveries[view.Find(scene).TransferId];
            var identity = entry.Binding.CreateIdentity();
            try
            {
                entry.Replica.SendAction(new SceneAction(identity.SceneId, identity.IncarnationId, SceneActionKind.OpenPatient, scene.SelectedColumn.ColumnData.ID, scene.SelectedColumn.SelectedSite.Information.FullID));
                ActionStatus = "Opening patient on Desktop...";
            }
            catch (IOException error)
            {
                ActionStatus = error.Message;
            }
        }

        public async Task RequestCloseAsync(HBP.Data.Module3D.Base3DScene scene)
        {
            var presentation = view.Find(scene);
            if (presentation == null) return;
            if (CanRequest(scene))
            {
                var entry = deliveries[presentation.TransferId];
                var identity = entry.Binding.CreateIdentity();
                entry.Closed ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                entry.Replica.SendAction(new SceneAction(identity.SceneId, identity.IncarnationId, SceneActionKind.Close));
                ActionStatus = "Closing visualization...";
                if (await Task.WhenAny(entry.Closed.Task, Task.Delay(TimeSpan.FromSeconds(15))) != entry.Closed.Task) throw new IOException("Desktop did not acknowledge the close. Retry, or close locally after disconnection.");
                await entry.Closed.Task;
            }
            else
            {
                if (deliveries.TryGetValue(presentation.TransferId, out var entry))
                {
                    entry.Shared = false;
                    entry.Status = DeliveryStatus.Closed;
                    entry.Lifetime.Cancel();
                    entry.Replica?.Dispose();
                    entry.Replica = null;
                }

                await view.CloseAsync(presentation.TransferId);
            }
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
            CancelReplicaGraceExpiry();
            foreach (var entry in deliveries.Values)
            {
                entry.Replica?.Dispose();
                entry.Replica = null;
            }

            IsSessionConnected = false;
            IsConnected = false;
        }

        public void CloseSession()
        {
            RequireMainThread();
            pendingInitialTelemetry = null;
            Reconciliation?.Dispose();
            surfaceCache.Clear();
            Disconnect(); // Cancels queued publication before freeing resources.
            foreach (var entry in deliveries.Values)
            {
                entry.Shared = false;
                entry.Status = DeliveryStatus.Closed;
                entry.Lifetime.Cancel();
                entry.Replica?.Dispose();
                entry.Replica = null;
            }

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
            if (view != null) view.OnContextSelected.RemoveListener(OnContextSelected);
            CloseSession();
            deliveries.Clear();
            _ = ReleaseGlobalsAsync();
        }

        private static async Task<byte[]> ReadPrefixAsync(Stream stream, int count, CancellationToken stop)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var prefix = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(prefix, offset, count - offset, stop).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException("The authenticated Quest replica stream ended before its protocol header.");
                offset += read;
            }

            return prefix;
        }

        private static bool HasTransportMagic(byte[] prefix, string expected)
        {
            if (prefix == null || prefix.Length < 4) return false;
            for (int i = 0; i < 4; i++)
                if (prefix[i] != (byte)expected[i])
                    return false;
            return true;
        }

        private static byte[] ParseContentHash(string hash)
        {
            if (hash == null || hash.Length != 64) throw new InvalidDataException("The published scene content hash is invalid.");
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hash.Substring(i * 2, 2), 16);
            return bytes;
        }

        private sealed class PrefixReadStream : Stream
        {
            private readonly byte[] m_Prefix;
            private readonly Stream m_Inner;
            private int m_Offset;

            public PrefixReadStream(byte[] prefix, Stream inner)
            {
                m_Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
                m_Inner = inner ?? throw new ArgumentNullException(nameof(inner));
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

            public override int Read(byte[] buffer, int offset, int count)
            {
                int copied = CopyPrefix(buffer, offset, count);
                return copied > 0 ? copied : m_Inner.Read(buffer, offset, count);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                int copied = CopyPrefix(buffer, offset, count);
                return copied > 0 ? copied : await m_Inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }

            private int CopyPrefix(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException();
                int remaining = m_Prefix.Length - m_Offset;
                if (remaining == 0 || count == 0) return 0;
                int copied = Math.Min(remaining, count);
                Buffer.BlockCopy(m_Prefix, m_Offset, buffer, offset, copied);
                m_Offset += copied;
                return copied;
            }

            public override void Flush() => m_Inner.Flush();
            public override void Write(byte[] buffer, int offset, int count) => m_Inner.Write(buffer, offset, count);
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => m_Inner.WriteAsync(buffer, offset, count, cancellationToken);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) m_Inner.Dispose();
                base.Dispose(disposing);
            }
        }

        private void Update() => sessionPreferences?.Tick();

        private async Task ReleaseGlobalsAsync()
        {
            try
            {
                if (sessionPreferences != null) await sessionPreferences.CloseAsync();
                if (atlasPreparation != null) await atlasPreparation.CloseAsync();
                await standardPreparation;
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
