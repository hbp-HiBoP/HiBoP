using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace HBP.Sync.Scene
{
    /// <summary>Authenticated, bounded, staged recovery of one retained scene incarnation.</summary>
    public static class V2SceneReconciliationProtocol
    {
        public const string Magic = "HBR3";
        private const int MaximumPacket = V2SceneMutationCheckpointCodec.MaximumCheckpointBytes + 65536;

        private enum Kind : byte
        {
            Hello = 1,
            Snapshot,
            Stage,
            Prepared,
            Commit,
            Applied,
            Finalize,
            Finished,
            Orphan
        }

        public static async Task ReconcileDesktopAsync(Stream stream, V2SceneReconciliationRecord record, Func<V2CheckpointConflict, CancellationToken, Task<V2ConflictChoice>> choose, CancellationToken stop, Func<Func<CancellationToken, Task>, CancellationToken, Task> runApplication = null)
        {
            using var close = stop.Register(stream.Dispose);
            await UniTask.SwitchToMainThread(stop);
            bool sourceClosed = !record.Scene || record.Scene.IsClosing;
            V2SceneMutationBoundary boundary = null;
            IDisposable guard = null;
            bool committing = record.PendingCommit != null, finished = false;
            try
            {
                if (!sourceClosed) (boundary, guard) = await OpenAsync(record, V2OriginDevice.Desktop, stop);
                record.SetStatus(V2ReconciliationStatus.Reconciling, "Comparing Desktop and Quest changes.");
                await stream.WriteAsync(Encoding.ASCII.GetBytes(Magic), 0, 4, stop).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(stop);
                await WriteAsync(stream, Kind.Hello, Snapshot(record, boundary, sourceClosed), stop).ConfigureAwait(false);
                var response = await ReadAsync(stream, stop).ConfigureAwait(false);
                if (sourceClosed)
                {
                    Require(response, Kind.Orphan);
                    await UniTask.SwitchToMainThread(stop);
                    record.SetStatus(V2ReconciliationStatus.Orphan, "The scene was closed on Desktop; the Quest copy remains local.");
                    finished = true;
                    return;
                }

                Require(response, Kind.Snapshot);
                await UniTask.SwitchToMainThread(stop);
                var remote = DecodeSnapshot(response.Body, record);
                V2SceneMutationCheckpoint candidate;
                if (record.PendingCommit != null)
                    candidate = V2SceneMutationCheckpointCodec.Decode(record.PendingCommit).Checkpoint;
                else
                {
                    var sharedHash = record.CommonCandidates().FirstOrDefault(hash => remote.CommonCandidates.Contains(hash));
                    var baseline = sharedHash == null ? null : record.FindCommon(sharedHash);
                    V2SceneReconciliationRecord.NormalizeTimelines(remote.State, baseline, remote.ChangedTimelines, remote.TimelineCommon);
                    var local = V2SceneCheckpointState.FromCheckpoint(boundary.CaptureCheckpoint());
                    V2SceneReconciliationRecord.NormalizeTimelines(local, baseline, record.ChangedTimelines, record.Common);
                    var merge = new V2SceneCheckpointMerge(baseline, local, remote.State);
                    var choices = new List<V2ConflictChoice>();
                    foreach (var conflict in merge.Conflicts)
                    {
                        if (choose == null) throw new InvalidOperationException("A reconciliation conflict requires a user choice.");
                        choices.Add(await choose(conflict, stop));
                        await UniTask.SwitchToMainThread(stop);
                    }

                    candidate = merge.Resolve(choices).ToCheckpoint();
                }

                async Task Apply(CancellationToken applicationStop)
                {
                    using var closeApplication = applicationStop.Register(stream.Dispose);
                    await boundary.PrepareCheckpointAsync(candidate, applicationStop);
                    byte[] bytes = V2SceneMutationCheckpointCodec.Encode(0, candidate);
                    string hash = Hash(bytes);
                    await WriteAsync(stream, Kind.Stage, bytes, applicationStop).ConfigureAwait(false);
                    RequireHash(await ReadAsync(stream, applicationStop).ConfigureAwait(false), Kind.Prepared, hash);
                    await UniTask.SwitchToMainThread(applicationStop);
                    record.PendingCommit = bytes;
                    committing = true;
                    await WriteAsync(stream, Kind.Commit, Encoding.UTF8.GetBytes(hash), applicationStop).ConfigureAwait(false);
                    RequireHash(await ReadAsync(stream, applicationStop).ConfigureAwait(false), Kind.Applied, hash);
                    await UniTask.SwitchToMainThread(applicationStop);
                    boundary.ApplyCheckpoint(candidate, new OperationId(Guid.NewGuid()));
                    await record.Scene.PrepareRenderingAsync(applicationStop);
                    await WriteAsync(stream, Kind.Finalize, Encoding.UTF8.GetBytes(hash), applicationStop).ConfigureAwait(false);
                    RequireHash(await ReadAsync(stream, applicationStop).ConfigureAwait(false), Kind.Finished, hash);
                    await UniTask.SwitchToMainThread(applicationStop);
                    record.ResetCommon(candidate);
                    record.PendingCommit = null;
                    // A fresh live session must still acknowledge the initial application barrier.
                    record.SetStatus(V2ReconciliationStatus.Local, "Changes merged; resuming synchronization.");
                    finished = true;
                }

                if (runApplication == null) await Apply(stop);
                else await runApplication(Apply, stop);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                Finish(record, boundary, guard, committing, finished);
            }
        }

        /// <summary>The caller has consumed Magic on the authenticated replica stream.</summary>
        public static async Task ReceiveQuestAsync(Stream stream, V2SceneReconciliationRecord record, CancellationToken stop)
        {
            using var close = stop.Register(stream.Dispose);
            V2SceneMutationBoundary boundary = null;
            IDisposable guard = null;
            bool committing = record.PendingCommit != null, finished = false;
            try
            {
                var hello = await ReadAsync(stream, stop).ConfigureAwait(false);
                Require(hello, Kind.Hello);
                await UniTask.SwitchToMainThread(stop);
                var remote = DecodeSnapshot(hello.Body, record);
                if (remote.Closed)
                {
                    record.ReleaseRecoveryLock();
                    record.PendingCommit = null;
                    record.SetStatus(V2ReconciliationStatus.Orphan, "The scene was closed on Desktop. You can continue using it locally.");
                    await WriteAsync(stream, Kind.Orphan, Array.Empty<byte>(), stop).ConfigureAwait(false);
                    finished = true;
                    return;
                }

                (boundary, guard) = await OpenAsync(record, V2OriginDevice.Quest, stop);
                record.SetStatus(V2ReconciliationStatus.Reconciling, "Reconnecting: resolving changes on Desktop.");
                await WriteAsync(stream, Kind.Snapshot, Snapshot(record, boundary, false), stop).ConfigureAwait(false);
                var stage = await ReadAsync(stream, stop).ConfigureAwait(false);
                Require(stage, Kind.Stage);
                var candidate = V2SceneMutationCheckpointCodec.Decode(stage.Body).Checkpoint;
                string hash = Hash(stage.Body);
                await boundary.PrepareCheckpointAsync(candidate, stop);
                await WriteAsync(stream, Kind.Prepared, Encoding.UTF8.GetBytes(hash), stop).ConfigureAwait(false);
                RequireHash(await ReadAsync(stream, stop).ConfigureAwait(false), Kind.Commit, hash);
                await UniTask.SwitchToMainThread(stop);
                record.PendingCommit = stage.Body;
                committing = true;
                boundary.ApplyCheckpoint(candidate, new OperationId(Guid.NewGuid()));
                await record.Scene.PrepareRenderingAsync(stop);
                await WriteAsync(stream, Kind.Applied, Encoding.UTF8.GetBytes(hash), stop).ConfigureAwait(false);
                RequireHash(await ReadAsync(stream, stop).ConfigureAwait(false), Kind.Finalize, hash);
                await UniTask.SwitchToMainThread(stop);
                record.ResetCommon(candidate);
                await WriteAsync(stream, Kind.Finished, Encoding.UTF8.GetBytes(hash), stop).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(stop);
                record.PendingCommit = null;
                record.SetStatus(V2ReconciliationStatus.Local, "Changes merged; waiting for Desktop.");
                finished = true;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                Finish(record, boundary, guard, committing, finished);
            }
        }

        private static async Task<(V2SceneMutationBoundary, IDisposable)> OpenAsync(V2SceneReconciliationRecord record, V2OriginDevice device, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(stop);
            if (record.RecoveryBoundary != null)
            {
                Open(record, device, out var retainedBoundary, out var retainedGuard);
                return (retainedBoundary, retainedGuard);
            }

            record.Scene.CancelPendingSiteFilters();
            return await record.Scene.CapturePreparedAsync(() =>
            {
                Open(record, device, out var boundary, out var guard);
                return (boundary, guard);
            }, stop);
        }

        private static void Open(V2SceneReconciliationRecord record, V2OriginDevice device, out V2SceneMutationBoundary boundary, out IDisposable guard)
        {
            if (record.RecoveryBoundary != null)
            {
                boundary = record.RecoveryBoundary;
                guard = record.RecoveryLock;
                record.RecoveryBoundary = null;
                record.RecoveryLock = null;
                return;
            }

            boundary = new V2SceneMutationBoundary(record.Scene, device);
            try
            {
                boundary.BindPreparedResources(record.Binding);
                guard = boundary.LockForReconciliation();
            }
            catch
            {
                boundary.Dispose();
                throw;
            }
        }

        private static void Finish(V2SceneReconciliationRecord record, V2SceneMutationBoundary boundary, IDisposable guard, bool committing, bool finished)
        {
            if (!finished && committing)
            {
                if (boundary != null)
                {
                    record.RecoveryBoundary = boundary;
                    record.RecoveryLock = guard;
                }

                record.SetStatus(V2ReconciliationStatus.OutOfSync, "Applying changes was interrupted. Retry the connection on Desktop to finish; shared edits are paused.");
            }
            else
            {
                if (finished && boundary != null)
                {
                    record.RecoveryBoundary = boundary;
                    record.RecoveryLock = guard;
                    record.SetStatus(V2ReconciliationStatus.Reconciling, "Changes merged; resuming the Desktop connection.");
                }
                else
                {
                    guard?.Dispose();
                    boundary?.Dispose();
                }

                if (!finished) record.SetStatus(V2ReconciliationStatus.Local, "Reconnection interrupted; local changes are preserved.");
            }
        }

        private sealed class RemoteSnapshot
        {
            internal string[] CommonCandidates;
            internal V2SceneCheckpointState TimelineCommon;
            internal string[] ChangedTimelines;
            internal V2SceneCheckpointState State;
            internal bool Closed;
        }

        private static byte[] Snapshot(V2SceneReconciliationRecord record, V2SceneMutationBoundary boundary, bool closed)
        {
            using var buffer = new MemoryStream();
            using var writer = new BinaryWriter(buffer, Encoding.UTF8, true);
            var identity = record.Binding.CreateV2Identity();
            writer.Write(identity.SessionId.ToByteArray());
            writer.Write(identity.SceneId.ToByteArray());
            writer.Write(identity.IncarnationId.ToByteArray());
            writer.Write(record.Binding.ManifestHash);
            writer.Write(closed);
            var bases = record.CommonCandidates();
            writer.Write(bases.Length);
            foreach (string hash in bases) writer.Write(hash);
            var anchors = record.Common?.Cells.Values.Select(cell => cell.Mutation).OfType<SetTimelineAnchor>().ToArray() ?? Array.Empty<SetTimelineAnchor>();
            writer.Write(anchors.Length);
            foreach (var anchor in anchors)
            {
                var encoded = V2MutationPayloadCodec.Encode(anchor);
                writer.Write(encoded.Length);
                writer.Write(encoded);
            }

            var changed = record.ChangedTimelines;
            writer.Write(changed.Length);
            foreach (string id in changed) writer.Write(id);
            if (!closed)
            {
                byte[] checkpoint = V2SceneMutationCheckpointCodec.Encode(0, boundary.CaptureCheckpoint());
                writer.Write(checkpoint.Length);
                writer.Write(checkpoint);
            }

            return buffer.ToArray();
        }

        private static RemoteSnapshot DecodeSnapshot(byte[] bytes, V2SceneReconciliationRecord record)
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var identity = record.Binding.CreateV2Identity();
            if (!reader.ReadBytes(16).SequenceEqual(identity.SessionId.ToByteArray()) || !reader.ReadBytes(16).SequenceEqual(identity.SceneId.ToByteArray()) || !reader.ReadBytes(16).SequenceEqual(identity.IncarnationId.ToByteArray()) || reader.ReadString() != record.Binding.ManifestHash)
                throw new InvalidDataException("Reconciliation belongs to another prepared scene incarnation.");
            var result = new RemoteSnapshot { Closed = reader.ReadBoolean() };
            int baseCount = reader.ReadInt32();
            if (baseCount < 0 || baseCount > 129) throw new InvalidDataException("Invalid reconciliation base window.");
            result.CommonCandidates = new string[baseCount];
            for (int i = 0; i < baseCount; i++) result.CommonCandidates[i] = reader.ReadString();
            int anchorCount = reader.ReadInt32();
            if (anchorCount < 0 || anchorCount > 256) throw new InvalidDataException("Invalid reconciliation anchor count.");
            var anchors = new List<TimelineAnchorCheckpointRecord>();
            for (int i = 0; i < anchorCount; i++)
            {
                int size = reader.ReadInt32();
                if (size <= 0 || size > 1024) throw new InvalidDataException("Invalid reconciliation anchor length.");
                if (V2MutationPayloadCodec.Decode(reader.ReadBytes(size)) is not SetTimelineAnchor anchor) throw new InvalidDataException("Invalid reconciliation anchor.");
                anchors.Add(new TimelineAnchorCheckpointRecord(anchor));
            }

            result.TimelineCommon = V2SceneCheckpointState.FromCheckpoint(new V2SceneMutationCheckpoint(Array.Empty<SiteColorCheckpointRecord>(), Array.Empty<CutDefinitionCheckpointRecord>(), anchors));
            int count = reader.ReadInt32();
            if (count < 0 || count > 256) throw new InvalidDataException("Invalid reconciliation timeline count.");
            result.ChangedTimelines = new string[count];
            for (int i = 0; i < count; i++) result.ChangedTimelines[i] = reader.ReadString();
            if (!result.Closed)
            {
                int length = reader.ReadInt32();
                if (length <= 0 || length > V2SceneMutationCheckpointCodec.MaximumCheckpointBytes || length != stream.Length - stream.Position) throw new InvalidDataException("Invalid reconciliation snapshot length.");
                result.State = V2SceneCheckpointState.FromCheckpoint(V2SceneMutationCheckpointCodec.Decode(reader.ReadBytes(length)).Checkpoint);
            }

            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing reconciliation snapshot bytes.");
            return result;
        }

        private readonly struct Packet
        {
            internal readonly Kind Kind;
            internal readonly byte[] Body;

            internal Packet(Kind kind, byte[] body)
            {
                Kind = kind;
                Body = body;
            }
        }

        private static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        private static void Require(Packet packet, Kind expected)
        {
            if (packet.Kind != expected) throw new InvalidDataException("Unexpected reconciliation stage: " + packet.Kind + ".");
        }

        private static void RequireHash(Packet packet, Kind expected, string hash)
        {
            Require(packet, expected);
            if (Encoding.UTF8.GetString(packet.Body) != hash) throw new InvalidDataException("Reconciliation candidate hash does not match.");
        }

        private static async Task WriteAsync(Stream stream, Kind kind, byte[] body, CancellationToken stop)
        {
            if (body.Length > MaximumPacket) throw new InvalidDataException("Reconciliation packet exceeds its bound.");
            var header = new byte[] { (byte)kind, (byte)body.Length, (byte)(body.Length >> 8), (byte)(body.Length >> 16), (byte)(body.Length >> 24) };
            await stream.WriteAsync(header, 0, header.Length, stop).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, stop).ConfigureAwait(false);
            await stream.FlushAsync(stop).ConfigureAwait(false);
        }

        private static async Task<Packet> ReadAsync(Stream stream, CancellationToken stop)
        {
            var header = new byte[5];
            await ReadExactlyAsync(stream, header, stop).ConfigureAwait(false);
            int length = header[1] | header[2] << 8 | header[3] << 16 | header[4] << 24;
            if (length < 0 || length > MaximumPacket || !Enum.IsDefined(typeof(Kind), header[0])) throw new InvalidDataException("Invalid reconciliation packet header.");
            var body = new byte[length];
            await ReadExactlyAsync(stream, body, stop).ConfigureAwait(false);
            return new Packet((Kind)header[0], body);
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] bytes, CancellationToken stop)
        {
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = await stream.ReadAsync(bytes, offset, bytes.Length - offset, stop).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException("Reconciliation connection interrupted.");
                offset += read;
            }
        }
    }
}
