using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Data.Module3D;

namespace HBP.Sync.Scene
{
    public enum V2ReconciliationStatus
    {
        Local,
        Reconciling,
        Synchronized,
        OutOfSync,
        Orphan
    }

    /// <summary>Process-local ownership survives transport disposal, never an application restart.</summary>
    public sealed class V2SceneReconciliationRecord : IDisposable
    {
        private static readonly Dictionary<Base3DScene, V2SceneReconciliationRecord> Records = new Dictionary<Base3DScene, V2SceneReconciliationRecord>();
        private readonly SortedDictionary<ulong, V2Mutation> m_Unconfirmed = new SortedDictionary<ulong, V2Mutation>();
        private readonly Dictionary<BasicTimeline, ColumnId[]> m_Timelines;
        private readonly HashSet<string> m_ChangedTimelines = new HashSet<string>(StringComparer.Ordinal);
        private V2SceneCheckpointState m_Common;
        private ulong m_CommonSequence;
        private readonly List<Dictionary<string, V2SceneCheckpointState.Cell>> m_RecentBases = new();
        private long m_HistoryBytes;
        private const int MaximumRecentBases = 128;
        private const long MaximumHistoryBytes = 64L * 1024 * 1024;
        public Base3DScene Scene { get; }
        public PreparedSceneDeliveryBinding Binding { get; private set; }
        public V2ReconciliationStatus Status { get; private set; } = V2ReconciliationStatus.Local;
        public string Message { get; private set; }
        public byte[] PendingCommit { get; internal set; }
        public V2SceneCheckpointState Common => m_Common;
        public string CommonHash => m_Common?.Hash() ?? string.Empty;
        public string[] ChangedTimelines => m_ChangedTimelines.ToArray();
        public event Action Changed;
        internal IDisposable RecoveryLock;
        internal V2SceneMutationBoundary RecoveryBoundary;

        private V2SceneReconciliationRecord(Base3DScene scene, PreparedSceneDeliveryBinding binding)
        {
            Scene = scene;
            Binding = binding;
            m_Timelines = scene.Columns.Where(column => column.NavigationTimeline != null).GroupBy(column => column.NavigationTimeline).ToDictionary(group => group.Key, group => group.Select(column => new ColumnId(column.ColumnData.ID)).ToArray());
            BasicTimeline.AnchorChanged += OnTimelineChanged;
        }

        public static V2SceneReconciliationRecord Get(Base3DScene scene) => !ReferenceEquals(scene, null) && Records.TryGetValue(scene, out var record) ? record : null;

        public static V2SceneReconciliationRecord Retain(Base3DScene scene, PreparedSceneDeliveryBinding binding)
        {
            var current = Get(scene);
            if (current != null && current.Binding.CreateV2Identity().IncarnationId.Equals(binding.CreateV2Identity().IncarnationId))
            {
                current.Binding = binding;
                return current;
            }

            current?.Dispose();
            var result = new V2SceneReconciliationRecord(scene, binding);
            Records[scene] = result;
            return result;
        }

        private void OnTimelineChanged(BasicTimeline timeline, bool automatic)
        {
            if (automatic || Status == V2ReconciliationStatus.Reconciling || !m_Timelines.TryGetValue(timeline, out var columns)) return;
            if (V2MutationApplicationContext.TryGetCurrent(out var origin, out _, out _, out _) && origin == V2MutationApplicationOrigin.Remote) return;
            foreach (var column in columns) m_ChangedTimelines.Add(column.Value);
        }

        public void InstallCommon(V2SceneMutationCheckpoint checkpoint, ulong sequence)
        {
            m_Common = V2SceneCheckpointState.FromCheckpoint(checkpoint);
            m_RecentBases.Clear();
            m_HistoryBytes = 0;
            m_CommonSequence = sequence;
            foreach (ulong key in m_Unconfirmed.Keys.Where(key => key <= sequence).ToArray()) m_Unconfirmed.Remove(key);
            m_ChangedTimelines.Clear();
        }

        public void InvalidateCommon()
        {
            m_Common = null;
            m_RecentBases.Clear();
            m_HistoryBytes = 0;
        }

        public void BeginPublication()
        {
            InvalidateCommon();
            m_CommonSequence = 0;
            m_Unconfirmed.Clear();
        }

        public void QueueCanonical(ulong sequence, V2Mutation mutation)
        {
            if (sequence <= m_CommonSequence) return;
            if (m_Unconfirmed.Count >= V2DesktopMutationAuthority.MaximumRememberedOperations)
            {
                InvalidateCommon();
                m_Unconfirmed.Clear();
            }

            m_Unconfirmed[sequence] = mutation;
        }

        public void ConfirmThrough(ulong sequence)
        {
            if (m_Common == null) return;
            foreach (var entry in m_Unconfirmed.Where(entry => entry.Key <= sequence).ToArray())
            {
                var undo = new Dictionary<string, V2SceneCheckpointState.Cell>(StringComparer.Ordinal);
                m_Common.BeforeChange = (key, previous) =>
                {
                    if (!undo.ContainsKey(key)) undo.Add(key, previous);
                };
                try
                {
                    m_Common.ApplyCanonical(entry.Value);
                }
                finally
                {
                    m_Common.BeforeChange = null;
                }

                m_RecentBases.Add(undo);
                m_HistoryBytes += UndoBytes(undo);
                while (m_RecentBases.Count > MaximumRecentBases || m_HistoryBytes > MaximumHistoryBytes)
                {
                    m_HistoryBytes -= UndoBytes(m_RecentBases[0]);
                    m_RecentBases.RemoveAt(0);
                }

                m_CommonSequence = entry.Key;
                m_Unconfirmed.Remove(entry.Key);
                if (entry.Value is SetTimelineAnchor anchor) m_ChangedTimelines.Remove(anchor.ColumnId.Value);
            }
        }

        public void ApplyCanonical(ulong sequence, V2Mutation mutation)
        {
            QueueCanonical(sequence, mutation);
            ConfirmThrough(sequence);
        }

        private static long UndoBytes(Dictionary<string, V2SceneCheckpointState.Cell> undo) => undo.Sum(entry => (long)entry.Key.Length * 2 + (entry.Value?.Bytes.Length ?? 0) + 64);

        public string[] CommonCandidates()
        {
            if (m_Common == null) return Array.Empty<string>();
            var candidate = m_Common.Clone();
            var hashes = new List<string> { candidate.Hash() };
            for (int i = m_RecentBases.Count - 1; i >= 0; i--)
            {
                foreach (var entry in m_RecentBases[i]) candidate.SetCell(entry.Key, entry.Value);
                hashes.Add(candidate.Hash());
            }

            return hashes.Distinct().ToArray();
        }

        public V2SceneCheckpointState FindCommon(string hash)
        {
            if (m_Common == null) return null;
            var candidate = m_Common.Clone();
            if (candidate.Hash() == hash) return candidate;
            for (int i = m_RecentBases.Count - 1; i >= 0; i--)
            {
                foreach (var entry in m_RecentBases[i]) candidate.SetCell(entry.Key, entry.Value);
                if (candidate.Hash() == hash) return candidate;
            }

            return null;
        }

        public V2SceneCheckpointState Capture(V2SceneMutationBoundary boundary)
        {
            var result = V2SceneCheckpointState.FromCheckpoint(boundary.CaptureCheckpoint());
            NormalizeTimelines(result, m_Common, m_ChangedTimelines);
            return result;
        }

        internal static void NormalizeTimelines(V2SceneCheckpointState current, V2SceneCheckpointState baseline, IEnumerable<string> changedColumns, V2SceneCheckpointState latestCommon = null)
        {
            if (baseline == null) return;
            var changed = new HashSet<string>(changedColumns, StringComparer.Ordinal);
            foreach (var entry in current.Cells.Where(entry => entry.Value.Mutation is SetTimelineAnchor).ToArray())
            {
                var anchor = (SetTimelineAnchor)entry.Value.Mutation;
                if (changed.Contains(anchor.ColumnId.Value) || !baseline.Cells.TryGetValue(entry.Key, out var original)) continue;
                if (latestCommon != null && latestCommon.Cells.TryGetValue(entry.Key, out var latest) && !latest.Bytes.SequenceEqual(original.Bytes)) continue;
                var before = original.Mutation as SetTimelineAnchor;
                if (before != null && anchor.Playing == before.Playing && anchor.Looping == before.Looping && anchor.Step == before.Step && (anchor.Playing || anchor.Index == before.Index))
                    current.SetCell(entry.Key, new V2SceneCheckpointState.Cell(entry.Value.Section, original.Bytes, entry.Value.Mutation));
            }
        }

        public void ResetCommon(V2SceneMutationCheckpoint checkpoint)
        {
            m_Unconfirmed.Clear();
            m_CommonSequence = 0;
            InstallCommon(checkpoint, 0);
        }

        public void SetStatus(V2ReconciliationStatus status, string message = null)
        {
            Status = status;
            Message = message;
            Changed?.Invoke();
        }

        public void ReleaseReconciliationGuard() => ReleaseRecoveryLock();

        internal void ReleaseRecoveryLock()
        {
            RecoveryLock?.Dispose();
            RecoveryLock = null;
            RecoveryBoundary?.Dispose();
            RecoveryBoundary = null;
        }

        public void Dispose()
        {
            BasicTimeline.AnchorChanged -= OnTimelineChanged;
            ReleaseRecoveryLock();
            if (Records.TryGetValue(Scene, out var current) && ReferenceEquals(current, this)) Records.Remove(Scene);
        }
    }
}
