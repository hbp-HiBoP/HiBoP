using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Transport;
using UnityEngine;

namespace HBP.Quest.Desktop
{
    /// <summary>Owns the one sent Desktop scene; no instance exists before its delivery is acknowledged.</summary>
    internal sealed class DesktopReplicaSession : IDisposable
    {
        private readonly object gate = new();
        private readonly Base3DScene scene;
        private readonly LiveGeometryStateAdapter adapter;
        private readonly string host;
        private readonly byte[] pin, credential;
        private readonly CancellationTokenSource lifetime = new();
        private readonly Dictionary<string, byte[]> resources = new(StringComparer.Ordinal);
        private readonly StateSnapshot initial;
        private readonly float initialClockTime;
        private StateSnapshot accepted, pending;
        private StateSnapshot lastSent;
        private float pendingClockTime;
        private bool cutsDirty, timelinesDirty, stateDirty;
        private readonly BasicTimeline[] timelines;
        private bool disposed;
        public bool IsClosed => disposed;
        public Base3DScene SourceScene => scene;
        public bool IsConnected { get; private set; }
        public string RejectionReason { get; private set; }
        public ulong VisibleRevision { get; private set; }

        public DesktopReplicaSession(Base3DScene scene, PreparedSceneDeliveryBinding binding, string host, byte[] pin, byte[] credential)
        {
            this.scene = scene ? scene : throw new ArgumentNullException(nameof(scene));
            this.host = host;
            this.pin = (byte[])pin.Clone();
            this.credential = (byte[])credential.Clone();
            adapter = new LiveGeometryStateAdapter(scene, Guid.NewGuid(), binding);
            initial = adapter.Capture(1);
            initialClockTime = Time.realtimeSinceStartup;
            pending = initial;
            pendingClockTime = initialClockTime;
            adapter.BindInitialState(initial);
            RememberResource(initial);
            timelines = scene.Columns.Select(column => column.NavigationTimeline).Where(timeline => timeline is { Length: > 0 }).Distinct().ToArray();
            Module3DMain.OnRemoveScene.AddListener(OnSceneRemoved);
            scene.OnModifyPlanesCuts.AddListener(OnCutsChanged);
            scene.OnUpdateCuts.AddListener(OnCutsChanged);
            scene.OnSharedStateChanged.AddListener(OnStateChanged);
            scene.OnSelect.AddListener(OnStateChanged);
            scene.OnSelectSite.AddListener(OnSiteChanged);
            scene.OnChangeAutomaticCutAroundSelectedSite.AddListener(OnBoolChanged);
            scene.OnSelectCCEPSource.AddListener(OnStateChanged);
            scene.OnSurfaceRepresentationChanged.AddListener(OnRepresentationChanged);
            scene.OnUpdateGeneratorState.AddListener(OnBoolChanged);
            scene.OnUpdateROI.AddListener(OnStateChanged);
            scene.OnChangeDisplayCorrelations.AddListener(OnStateChanged);
            Module3DMain.OnRequestUpdateInToolbar.AddListener(OnStateChanged);
            foreach (var column in scene.Columns)
            {
                column.OnSelect.AddListener(OnStateChanged);
                column.OnSelectSite.AddListener(OnSiteChanged);
                column.OnChangeSiteState.AddListener(OnSiteChanged);
                column.OnUpdateActivityAlpha.AddListener(OnStateChanged);
                if (column is Column3DAnatomy anatomy)
                    anatomy.AnatomyParameters.OnUpdateInfluenceDistance.AddListener(OnStateChanged);
                if (column is Column3DStatic staticColumn)
                {
                    staticColumn.OnUpdateSelectedLabel.AddListener(OnStateChanged);
                    staticColumn.StaticParameters.OnUpdateSpanValues.AddListener(OnStateChanged);
                    staticColumn.StaticParameters.OnUpdateInfluenceDistance.AddListener(OnStateChanged);
                }

                if (column is Column3DDynamic dynamicColumn)
                {
                    dynamicColumn.DynamicParameters.OnUpdateSpanValues.AddListener(OnStateChanged);
                    dynamicColumn.DynamicParameters.OnUpdateInfluenceDistance.AddListener(OnStateChanged);
                }

                if (column is Column3DFMRI fmri)
                {
                    fmri.OnChangeSelectedFMRI.AddListener(OnStateChanged);
                    fmri.FMRIParameters.OnUpdateCalValues.AddListener(OnStateChanged);
                    fmri.FMRIParameters.OnUpdateHideValues.AddListener(OnStateChanged);
                }

                if (column is Column3DMEG meg)
                {
                    meg.OnChangeSelectedMEG.AddListener(OnStateChanged);
                    meg.MEGParameters.OnUpdateCalValues.AddListener(OnStateChanged);
                    meg.MEGParameters.OnUpdateHideValues.AddListener(OnStateChanged);
                }
            }

            foreach (var timeline in timelines) timeline.OnUpdateCurrentIndex.AddListener(OnTimelineChanged);
            _ = RunAsync(lifetime.Token);
        }

        private void OnCutsChanged() => cutsDirty = true;
        private void OnTimelineChanged() => timelinesDirty = true;
        private void OnSiteChanged(HBP.Core.Object3D.Site site) => stateDirty = true;
        private void OnBoolChanged(bool value) => stateDirty = true;
        private void OnRepresentationChanged(SurfaceRepresentation value) => stateDirty = true;
        private void OnStateChanged() => stateDirty = true;

        /// <summary>Capture only after a scene operation, coalescing changes until the next frame.</summary>
        public void Tick(float now)
        {
            if (disposed || RejectionReason != null) return;
            if (!scene || scene.IsClosing)
            {
                Dispose();
                return;
            }

            if ((!cutsDirty && !timelinesDirty && !stateDirty) || !scene.CanApplyPreparedState || scene.SceneInformation.GeometryNeedsUpdate) return;
            try
            {
                StateSnapshot baseline = pending ?? accepted ?? initial;
                StateSnapshot captured = stateDirty ? adapter.Capture(1) : baseline;
                if (!stateDirty && cutsDirty) captured = adapter.CaptureCuts(captured);
                if (!stateDirty && timelinesDirty) captured = adapter.CaptureTimelines(captured);
                lock (gate)
                {
                    cutsDirty = timelinesDirty = stateDirty = false;
                    if (SameFields(pending ?? accepted, captured)) return;
                    RememberResource(captured);
                    pending = captured;
                    pendingClockTime = Time.realtimeSinceStartup;
                }
            }
            catch (InvalidOperationException)
            {
                // Keep the dirty operation until prepared geometry has settled.
            }
            catch (InvalidDataException exception)
            {
                RejectionReason = "Quest sync cannot capture this operation: " + exception.Message;
                Debug.LogError(RejectionReason);
            }
        }

        private void RememberResource(StateSnapshot snapshot)
        {
            string reference = CorrelationReference(snapshot);
            if (reference.Length == 0 || resources.ContainsKey(reference)) return;
            lock (gate) resources[reference] = adapter.ExportCorrelationResource(reference);
        }

        private async Task RunAsync(CancellationToken stop)
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    try
                    {
                        await QuestPairing.OpenReplicaAsync(host, pin, credential, stop, ExchangeAsync);
                    }
                    catch (Exception exception) when (!stop.IsCancellationRequested && (exception is IOException || exception is InvalidDataException || exception is InvalidOperationException || exception is System.Net.Sockets.SocketException || exception is System.Security.Authentication.AuthenticationException || exception is OperationCanceledException))
                    {
                        if (exception is InvalidDataException && exception.Message.StartsWith("Quest rejected replica: ", StringComparison.Ordinal))
                        {
                            RejectionReason = exception.Message;
                            Debug.LogError(RejectionReason);
                            break;
                        }

                        Debug.LogWarning("Quest replica disconnected: " + exception.Message);
                    }
                    finally
                    {
                        IsConnected = false;
                    }

                    if (!stop.IsCancellationRequested) await Task.Delay(2000, stop);
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
        }

        private async Task ExchangeAsync(Stream stream, CancellationToken stop)
        {
            IsConnected = true;
            var checkpointFrame = await ReplicaWire.ReadAsync(stream, stop).ConfigureAwait(false);
            if (checkpointFrame.Kind != ReplicaFrameKind.Checkpoint) throw new InvalidDataException("Quest did not provide a replica checkpoint.");
            var checkpoint = ReplicaWire.ReadCheckpoint(checkpointFrame.Body);
            lock (gate)
            {
                StateSnapshot known = checkpoint.Revision == 0 ? null : accepted?.CommonRevision == checkpoint.Revision ? accepted : lastSent?.CommonRevision == checkpoint.Revision ? lastSent : null;
                if (checkpoint.Revision > 0 && (known == null || !ReplicaWire.Hash(known).SequenceEqual(checkpoint.Hash)))
                    throw new InvalidDataException("Quest replica checkpoint differs from this Desktop epoch.");
                if (known != null && (accepted == null || known.CommonRevision > accepted.CommonRevision)) accepted = known;
            }

            var sentResources = new HashSet<string>(StringComparer.Ordinal);
            bool first = true;
            while (!stop.IsCancellationRequested)
            {
                StateSnapshot baseState, candidate;
                float clock;
                lock (gate)
                {
                    baseState = accepted;
                    candidate = accepted == null ? initial : pending ?? accepted;
                    clock = accepted == null ? initialClockTime : pendingClockTime;
                }

                if (candidate == null) throw new InvalidOperationException("Replica has no checkpoint.");
                if (!first && (candidate == baseState || SameFields(baseState, candidate)))
                {
                    await Task.Delay(100, stop).ConfigureAwait(false);
                    continue;
                }

                ulong revision = baseState == null ? 1UL : baseState.CommonRevision + (SameFields(baseState, candidate) ? 0UL : 1UL);
                StateSnapshot next = candidate.WithFields(candidate.Fields, revision);
                string reference = CorrelationReference(next);
                if (reference.Length != 0 && sentResources.Add(reference))
                {
                    byte[] data;
                    lock (gate) data = resources[reference];
                    await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Resource, ReplicaWire.Resource(reference, data), stop).ConfigureAwait(false);
                    await RequireAckAsync(stream, ReplicaFrameKind.Applied, 0, stop).ConfigureAwait(false);
                }

                bool full = first || baseState == null;
                byte[] encoded = full ? SharedStateCodec.Encode(next) : ReplicaDelta.Between(baseState, next).Encode();
                byte[] body = new byte[4 + encoded.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(clock), 0, body, 0, 4);
                Buffer.BlockCopy(encoded, 0, body, 4, encoded.Length);
                await ReplicaWire.WriteAsync(stream, full ? ReplicaFrameKind.Snapshot : ReplicaFrameKind.Delta, body, stop).ConfigureAwait(false);
                lock (gate) lastSent = next;
                await RequireAckAsync(stream, ReplicaFrameKind.Received, revision, stop).ConfigureAwait(false);
                await RequireAckAsync(stream, ReplicaFrameKind.Applied, revision, stop).ConfigureAwait(false);
                await RequireAckAsync(stream, ReplicaFrameKind.Visible, revision, stop).ConfigureAwait(false);
                lock (gate)
                {
                    accepted = next;
                    VisibleRevision = revision;
                    if (ReferenceEquals(pending, candidate)) pending = null;
                }

                first = false;
            }
        }

        private static async Task RequireAckAsync(Stream stream, ReplicaFrameKind expected, ulong revision, CancellationToken stop)
        {
            var reply = await ReplicaWire.ReadAsync(stream, stop).ConfigureAwait(false);
            if (reply.Kind == ReplicaFrameKind.Rejected) throw new InvalidDataException("Quest rejected replica: " + Encoding.UTF8.GetString(reply.Body));
            if (reply.Kind != expected || ReplicaWire.ReadRevision(reply.Body) != revision) throw new InvalidDataException("Unexpected Quest replica acknowledgement.");
        }

        private static string CorrelationReference(StateSnapshot state)
        {
            return state.Fields.TryGetValue(new StateKey(EntityKind.Scene, "", "", 9), out byte[] value) ? Encoding.UTF8.GetString(value) : "";
        }

        private static bool SameFields(StateSnapshot left, StateSnapshot right)
        {
            if (left == null || right == null) return false;
            var a = left.Fields;
            var b = right.Fields;
            return a.Count == b.Count && a.All(field => b.TryGetValue(field.Key, out byte[] value) && field.Value.SequenceEqual(value));
        }

        private void OnSceneRemoved(Base3DScene removed)
        {
            if (removed == scene) Dispose();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Module3DMain.OnRemoveScene.RemoveListener(OnSceneRemoved);
            scene.OnModifyPlanesCuts.RemoveListener(OnCutsChanged);
            scene.OnUpdateCuts.RemoveListener(OnCutsChanged);
            scene.OnSharedStateChanged.RemoveListener(OnStateChanged);
            scene.OnSelect.RemoveListener(OnStateChanged);
            scene.OnSelectSite.RemoveListener(OnSiteChanged);
            scene.OnChangeAutomaticCutAroundSelectedSite.RemoveListener(OnBoolChanged);
            scene.OnSelectCCEPSource.RemoveListener(OnStateChanged);
            scene.OnSurfaceRepresentationChanged.RemoveListener(OnRepresentationChanged);
            scene.OnUpdateGeneratorState.RemoveListener(OnBoolChanged);
            scene.OnUpdateROI.RemoveListener(OnStateChanged);
            scene.OnChangeDisplayCorrelations.RemoveListener(OnStateChanged);
            Module3DMain.OnRequestUpdateInToolbar.RemoveListener(OnStateChanged);
            foreach (var column in scene.Columns)
            {
                column.OnSelect.RemoveListener(OnStateChanged);
                column.OnSelectSite.RemoveListener(OnSiteChanged);
                column.OnChangeSiteState.RemoveListener(OnSiteChanged);
                column.OnUpdateActivityAlpha.RemoveListener(OnStateChanged);
                if (column is Column3DAnatomy anatomy)
                    anatomy.AnatomyParameters.OnUpdateInfluenceDistance.RemoveListener(OnStateChanged);
                if (column is Column3DStatic staticColumn)
                {
                    staticColumn.OnUpdateSelectedLabel.RemoveListener(OnStateChanged);
                    staticColumn.StaticParameters.OnUpdateSpanValues.RemoveListener(OnStateChanged);
                    staticColumn.StaticParameters.OnUpdateInfluenceDistance.RemoveListener(OnStateChanged);
                }

                if (column is Column3DDynamic dynamicColumn)
                {
                    dynamicColumn.DynamicParameters.OnUpdateSpanValues.RemoveListener(OnStateChanged);
                    dynamicColumn.DynamicParameters.OnUpdateInfluenceDistance.RemoveListener(OnStateChanged);
                }

                if (column is Column3DFMRI fmri)
                {
                    fmri.OnChangeSelectedFMRI.RemoveListener(OnStateChanged);
                    fmri.FMRIParameters.OnUpdateCalValues.RemoveListener(OnStateChanged);
                    fmri.FMRIParameters.OnUpdateHideValues.RemoveListener(OnStateChanged);
                }

                if (column is Column3DMEG meg)
                {
                    meg.OnChangeSelectedMEG.RemoveListener(OnStateChanged);
                    meg.MEGParameters.OnUpdateCalValues.RemoveListener(OnStateChanged);
                    meg.MEGParameters.OnUpdateHideValues.RemoveListener(OnStateChanged);
                }
            }

            foreach (var timeline in timelines) timeline.OnUpdateCurrentIndex.RemoveListener(OnTimelineChanged);
            lifetime.Cancel();
            Array.Clear(pin, 0, pin.Length);
            Array.Clear(credential, 0, credential.Length);
        }
    }
}
