using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.Events;
using SceneCut = HBP.Core.Object3D.Cut;
using RoiSphere = HBP.Data.Module3D.Sphere;

namespace HBP.Sync.Scene
{
    public enum V2MutationApplicationOrigin : byte
    {
        LocalDesktop,
        LocalQuest,
        Remote
    }

    /// <summary>Thread-scoped source identity for one synchronous business mutation.</summary>
    public static class V2MutationApplicationContext
    {
        [ThreadStatic] private static Context s_Current;

        public static IDisposable EnterLocal(V2OriginDevice device, OperationId operationId)
        {
            if (device != V2OriginDevice.Desktop && device != V2OriginDevice.Quest)
                throw new ArgumentOutOfRangeException(nameof(device));
            return Enter(device == V2OriginDevice.Desktop ? V2MutationApplicationOrigin.LocalDesktop : V2MutationApplicationOrigin.LocalQuest, device, operationId, false);
        }

        internal static IDisposable EnterLocalApply(V2OriginDevice device, OperationId operationId)
        {
            if (device != V2OriginDevice.Desktop && device != V2OriginDevice.Quest)
                throw new ArgumentOutOfRangeException(nameof(device));
            return Enter(device == V2OriginDevice.Desktop ? V2MutationApplicationOrigin.LocalDesktop : V2MutationApplicationOrigin.LocalQuest, device, operationId, true);
        }

        public static IDisposable EnterRemote(OperationId operationId) => Enter(V2MutationApplicationOrigin.Remote, null, operationId, true);

        internal static bool TryGetCurrent(out V2MutationApplicationOrigin origin, out V2OriginDevice? device, out OperationId operationId, out bool suppressNestedPublication)
        {
            if (s_Current == null)
            {
                origin = default;
                device = default;
                operationId = null;
                suppressNestedPublication = false;
                return false;
            }

            origin = s_Current.Origin;
            device = s_Current.Device;
            operationId = s_Current.OperationId;
            suppressNestedPublication = s_Current.SuppressNestedPublication;
            return true;
        }

        private static IDisposable Enter(V2MutationApplicationOrigin origin, V2OriginDevice? device, OperationId operationId, bool suppressNestedPublication)
        {
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            var previous = s_Current;
            s_Current = new Context(origin, device, operationId, suppressNestedPublication);
            return new Scope(previous);
        }

        private sealed class Context
        {
            public V2MutationApplicationOrigin Origin { get; }
            public V2OriginDevice? Device { get; }
            public OperationId OperationId { get; }
            public bool SuppressNestedPublication { get; }

            public Context(V2MutationApplicationOrigin origin, V2OriginDevice? device, OperationId operationId, bool suppressNestedPublication)
            {
                Origin = origin;
                Device = device;
                OperationId = operationId;
                SuppressNestedPublication = suppressNestedPublication;
            }
        }

        private sealed class Scope : IDisposable
        {
            private readonly Context m_Previous;
            private bool m_Disposed;

            public Scope(Context previous) => m_Previous = previous;

            public void Dispose()
            {
                if (m_Disposed) return;
                m_Disposed = true;
                s_Current = m_Previous;
            }
        }
    }

    public sealed class V2SceneMutationCheckpoint
    {
        public IReadOnlyList<SiteColorCheckpointRecord> SiteColors { get; }
        public IReadOnlyList<CutDefinitionCheckpointRecord> CutDefinitions { get; }
        public IReadOnlyList<TimelineAnchorCheckpointRecord> TimelineAnchors { get; }
        public IReadOnlyList<V2T09CheckpointRecord> T09Records { get; }
        public IReadOnlyList<V2T10CheckpointRecord> T10Records { get; }
        public IReadOnlyList<V2T11CheckpointRecord> T11Records { get; }
        public IReadOnlyList<V2SiteFilterCheckpointRecord> T12Records { get; }
        public IReadOnlyList<V2CorrelationCheckpointRecord> T13Records { get; }

        internal V2SceneMutationCheckpoint(IEnumerable<SiteColorCheckpointRecord> siteColors, IEnumerable<CutDefinitionCheckpointRecord> cutDefinitions, IEnumerable<TimelineAnchorCheckpointRecord> timelineAnchors, IEnumerable<V2T09CheckpointRecord> t09Records = null, IEnumerable<V2T10CheckpointRecord> t10Records = null, IEnumerable<V2T11CheckpointRecord> t11Records = null, IEnumerable<V2SiteFilterCheckpointRecord> t12Records = null, IEnumerable<V2CorrelationCheckpointRecord> t13Records = null)
        {
            SiteColors = Array.AsReadOnly(siteColors.ToArray());
            CutDefinitions = Array.AsReadOnly(cutDefinitions.ToArray());
            TimelineAnchors = Array.AsReadOnly(timelineAnchors.ToArray());
            T09Records = Array.AsReadOnly((t09Records ?? Enumerable.Empty<V2T09CheckpointRecord>()).ToArray());
            T10Records = Array.AsReadOnly((t10Records ?? Enumerable.Empty<V2T10CheckpointRecord>()).ToArray());
            T11Records = Array.AsReadOnly((t11Records ?? Enumerable.Empty<V2T11CheckpointRecord>()).ToArray());
            T12Records = Array.AsReadOnly((t12Records ?? Enumerable.Empty<V2SiteFilterCheckpointRecord>()).ToArray());
            T13Records = Array.AsReadOnly((t13Records ?? Enumerable.Empty<V2CorrelationCheckpointRecord>()).ToArray());
        }
    }

    /// <summary>Typed checkpoint state for the current canonical correlation matrices and provenance.</summary>
    public sealed class V2CorrelationCheckpointRecord
    {
        private const ushort RecordMagic = 0x5433;
        private const ushort LegacyRecordSchema = 1;
        private const ushort RecordSchema = 2;
        private readonly byte[] m_ResultBytes;

        public bool HasResult => m_ResultBytes != null;
        public bool DisplayCorrelations { get; }
        public byte[] ResultBytes => m_ResultBytes == null ? null : (byte[])m_ResultBytes.Clone();

        public V2CorrelationCheckpointRecord(byte[] resultBytes, bool displayCorrelations = false)
        {
            if (resultBytes == null || resultBytes.Length == 0)
            {
                m_ResultBytes = null;
            }
            else
            {
                if (resultBytes.Length > CorrelationResultResource.MaximumBytes) throw new ArgumentOutOfRangeException(nameof(resultBytes));
                CorrelationResultResource.Decode(resultBytes);
                m_ResultBytes = (byte[])resultBytes.Clone();
            }

            DisplayCorrelations = displayCorrelations;
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream((m_ResultBytes?.Length ?? 0) + 10);
            using var writer = new BinaryWriter(stream);
            writer.Write(RecordMagic);
            writer.Write(RecordSchema);
            writer.Write((byte)(HasResult ? 1 : 0));
            writer.Write((byte)(DisplayCorrelations ? 1 : 0));
            if (HasResult)
            {
                writer.Write(m_ResultBytes.Length);
                writer.Write(m_ResultBytes);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static V2CorrelationCheckpointRecord Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 6 || bytes.Length > CorrelationResultResource.MaximumBytes + 10)
                throw new InvalidDataException("Invalid T13 correlation checkpoint record length.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt16() != RecordMagic)
                throw new InvalidDataException("Unsupported T13 correlation checkpoint record signature or schema.");
            ushort schema = reader.ReadUInt16();
            if (schema != LegacyRecordSchema && schema != RecordSchema)
                throw new InvalidDataException("Unsupported T13 correlation checkpoint record signature or schema.");
            byte hasResult = reader.ReadByte();
            if (hasResult > 1) throw new InvalidDataException("Invalid T13 correlation checkpoint result flag.");
            byte displayCorrelations = reader.ReadByte();
            if ((schema == LegacyRecordSchema && displayCorrelations != 0) || (schema == RecordSchema && displayCorrelations > 1))
                throw new InvalidDataException("Invalid T13 correlation checkpoint display flag.");
            byte[] result = null;
            if (hasResult == 1)
            {
                int length = reader.ReadInt32();
                if (length <= 0 || length > CorrelationResultResource.MaximumBytes || length != stream.Length - stream.Position)
                    throw new InvalidDataException("Invalid T13 correlation checkpoint result length.");
                result = reader.ReadBytes(length);
                if (result.Length != length) throw new EndOfStreamException();
            }

            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing T13 correlation checkpoint record bytes.");
            return new V2CorrelationCheckpointRecord(result, schema == LegacyRecordSchema ? hasResult == 1 : displayCorrelations == 1);
        }
    }

    /// <summary>Observes the three typed Core setters and applies them without a transport dependency.</summary>
    public sealed class V2SceneMutationBoundary : IDisposable
    {
        private readonly Dictionary<SiteState, List<SiteTarget>> m_Sites = new();
        private readonly Dictionary<(ColumnId ColumnId, SiteId SiteId), SiteState> m_SitesById = new();
        private readonly Dictionary<SceneCut, CutId> m_CutIds = new();
        private readonly Dictionary<CutId, SceneCut> m_Cuts = new();
        private readonly Dictionary<BasicTimeline, List<TimelineTarget>> m_Timelines = new();
        private readonly Dictionary<string, Column3D> m_ColumnsById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ROI> m_RoisById = new(StringComparer.Ordinal);
        private readonly Dictionary<SiteState, SitePresentationSnapshot> m_SitePresentationStates = new();
        private readonly Dictionary<SceneCut, SetCutDefinition> m_CutDefinitionSnapshots = new();
        private readonly Dictionary<SiteState, UnityAction> m_SiteStateListeners = new();
        private readonly Dictionary<ROI, UnityAction> m_RoiSelectionListeners = new();
        private readonly Dictionary<ROI, UnityAction> m_RoiStructureListeners = new();
        private readonly Dictionary<ROI, RoiStateSnapshot> m_RoiStateSnapshots = new();
        private readonly Dictionary<Guid, V2Mutation> m_OptimisticRollbacks = new();
        private readonly Dictionary<Guid, V2Mutation> m_OptimisticForwardMutations = new();
        private readonly Dictionary<Guid, SiteConfigurationProvenanceSnapshot> m_OptimisticSiteConfigurationProvenanceRollbacks = new();
        private readonly Dictionary<Guid, V2SceneMutationCheckpoint> m_OptimisticCheckpointRollbacks = new();
        private readonly Dictionary<Guid, SiteConfigurationProvenanceSnapshot> m_OptimisticCheckpointProvenanceRollbacks = new();
        private readonly Dictionary<(string ColumnId, string SiteId), SiteConfigurationFieldOwners> m_SiteConfigurationFieldOwners = new();
        private readonly Queue<Guid> m_OptimisticRollbackOrder = new();
        private readonly Dictionary<Column3D, List<(UnityEvent Event, UnityAction Listener)>> m_ColumnListeners = new();
        private readonly Dictionary<Column3D, UnityAction<Core.Object3D.Site>> m_ColumnSelectionListeners = new();
        private V2Mutation m_LastSceneStrongCuts;
        private V2Mutation m_LastSceneAutomaticCuts;
        private V2Mutation m_LastSceneHideBlacklisted;
        private V2Mutation m_LastSceneShowAllSites;
        private V2Mutation m_LastSceneSiteGain;
        private V2Mutation m_LastSceneEdgeMode;
        private V2Mutation m_LastSceneBrainTransparent;
        private V2Mutation m_LastSceneBrainAlpha;
        private V2Mutation m_LastSceneAtlasAlpha;
        private V2Mutation m_LastSceneBrainColor;
        private V2Mutation m_LastSceneCutColor;
        private V2Mutation m_LastSceneColormap;
        private V2Mutation m_LastSceneMarsAtlas;
        private V2Mutation m_LastSceneJuBrainAtlas;
        private V2Mutation m_LastSceneIbcDifumo;
        private V2Mutation m_LastSceneLocalizer;
        private V2Mutation m_LastSceneFmriCalibration;
        private V2Mutation m_LastMeshDisplay;
        private V2Mutation m_LastSelectedMri;
        private V2Mutation m_LastMriCalibration;
        private V2Mutation m_LastImplantation;
        private V2Mutation m_LastTriangleMask;
        private V2Mutation m_LastSelectedColumn;
        private readonly Dictionary<Column3D, V2Mutation> m_LastSelectedSites = new();
        private readonly Dictionary<Column3D, V2Mutation> m_LastActivityAlphas = new();
        private readonly Dictionary<Column3D, V2Mutation> m_LastColumnSpans = new();
        private readonly Dictionary<Column3D, V2Mutation> m_LastFunctionalDisplays = new();
        private readonly Dictionary<Column3D, V2Mutation> m_LastInfluenceDistances = new();
        private readonly Dictionary<Column3D, V2Mutation> m_LastColumnResources = new();
        private readonly Dictionary<Column3D, V2Mutation> m_LastCcepSources = new();
        private readonly Base3DScene m_Scene;
        private readonly V2OriginDevice m_LocalOrigin;
        private readonly IMonotonicClock m_Clock;
        private readonly Action<SceneCut> m_UpdateCut;
        private readonly Func<SetTimelineAnchor, double?> m_TimelineAgeSeconds;
        private readonly Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> m_TimelineTimingEstimate;
        private readonly Dictionary<BasicTimeline, TimelineAnchorState> m_TimelineAnchorStates = new();
        private int m_PlayingTimelineCount;
        private ConfigurationMutationCapture m_ConfigurationMutationCapture;
        private bool m_Disposed;
        private bool m_RoiObserverInitialized;
        private string m_LastActiveRoiId;
        private CutId[] m_LastCutOrder = Array.Empty<CutId>();
        private UnityAction m_CutOrderListener;
        private PreparedSceneResourceCatalog m_ResourceCatalog;
        private string m_ResourceManifestHash;

        public event Action<OperationId, V2Mutation, V2OriginDevice> MutationProposed;
        public bool IsAnyTimelinePlaying => Volatile.Read(ref m_PlayingTimelineCount) > 0;

        /// <summary>Reserve a sensitive scene operation before applying its accepted value.</summary>
        public bool TryBeginSensitiveActivityOperation(out IDisposable operationScope)
        {
            if (m_Scene == null)
            {
                operationScope = null;
                return false;
            }

            return m_Scene.TryBeginSensitiveActivityOperation(out operationScope);
        }

        /// <summary>Bind the currently prepared scene once; per-edit target resolution uses object-keyed lookups.</summary>
        public V2SceneMutationBoundary(Base3DScene scene, V2OriginDevice localOrigin, IMonotonicClock clock = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null, Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> timelineTimingEstimate = null) : this(CreateSiteTargets(scene), CreateCutTargets(scene), CreateTimelineTargets(scene), localOrigin, clock, cut => scene.UpdateCutPlane(cut, preserveDefinitionNormal: true), timelineAgeSeconds, timelineTimingEstimate)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            m_Scene = scene;
            BindSceneTargets(scene);
        }

        /// <summary>Bind exact resources from the published delivery before enabling live resource mutations.</summary>
        public void BindPreparedResources(PreparedSceneDeliveryBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            RequireScene();
            if (m_ResourceCatalog != null)
            {
                if (StringComparer.Ordinal.Equals(m_ResourceManifestHash, binding.ManifestHash)) return;
                throw new InvalidOperationException("A prepared scene boundary cannot be rebound to another resource manifest.");
            }

            var catalog = new PreparedSceneResourceCatalog(m_Scene, binding.ManifestHash);
            catalog.AssertDeliveryManifest(binding.Manifest);
            m_ResourceCatalog = catalog;
            m_ResourceManifestHash = binding.ManifestHash;
            foreach (Column3D column in m_Scene.Columns)
                BindColumnResourceObservers(column, m_ColumnListeners[column]);
            if (m_Scene.MeshManager != null)
            {
                m_Scene.MeshManager.ResourceSelectionChanged += ObserveMeshSelection;
                m_Scene.MeshManager.DisplaySelectionChanged += ObserveMeshDisplay;
            }

            if (m_Scene.MRIManager != null) m_Scene.MRIManager.ResourceSelectionChanged += ObserveMriSelection;
            if (m_Scene.ImplantationManager != null) m_Scene.ImplantationManager.ResourceSelectionChanged += ObserveImplantationSelection;
            if (m_Scene.TriangleEraser != null) m_Scene.TriangleEraser.VisibilityMaskChanged += ObserveTriangleMask;
            ObserveMeshDisplay();
            ObserveMriSelection();
            ObserveImplantationSelection();
            ObserveMriCalibration();
            ObserveTriangleMask();
        }

        /// <summary>Bind explicit fixture targets or a prepared-scene projection of its stable IDs.</summary>
        public V2SceneMutationBoundary(IEnumerable<(SiteState State, ColumnId ColumnId, SiteId SiteId)> sites, IEnumerable<(SceneCut Cut, CutId Id)> cuts, IEnumerable<(BasicTimeline Timeline, ColumnId ColumnId)> timelines, V2OriginDevice localOrigin, IMonotonicClock clock = null, Action<SceneCut> updateCut = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null, Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> timelineTimingEstimate = null)
        {
            if (localOrigin != V2OriginDevice.Desktop && localOrigin != V2OriginDevice.Quest)
                throw new ArgumentOutOfRangeException(nameof(localOrigin));
            m_LocalOrigin = localOrigin;
            m_Clock = clock ?? StopwatchMonotonicClock.Instance;
            m_TimelineAgeSeconds = timelineAgeSeconds;
            m_TimelineTimingEstimate = timelineTimingEstimate;
            if (m_Clock.Frequency <= 0 || m_Clock.Frequency > 1000000000000L)
                throw new ArgumentOutOfRangeException(nameof(clock));
            m_UpdateCut = updateCut;

            foreach (var target in sites ?? throw new ArgumentNullException(nameof(sites)))
            {
                if (target.State == null || target.ColumnId == null || target.SiteId == null)
                    throw new ArgumentException("Site mutation targets require a state and stable identities.", nameof(sites));
                if (!m_Sites.TryGetValue(target.State, out List<SiteTarget> siteTargets))
                    m_Sites.Add(target.State, siteTargets = new List<SiteTarget>(1));
                siteTargets.Add(new SiteTarget(target.ColumnId, target.SiteId));
                if (!m_SitesById.TryAdd((target.ColumnId, target.SiteId), target.State))
                    throw new ArgumentException("Site mutation targets must have unique column and site identities.", nameof(sites));
            }

            foreach (SiteState state in m_Sites.Keys)
            {
                m_SitePresentationStates[state] = SitePresentationSnapshot.Capture(state);
                UnityAction listener = () => OnBoundSiteStateChanged(state);
                state.OnChangeState.AddListener(listener);
                m_SiteStateListeners.Add(state, listener);
            }

            foreach (var target in cuts ?? throw new ArgumentNullException(nameof(cuts)))
            {
                if (target.Cut == null || target.Id == null)
                    throw new ArgumentException("Cut mutation targets require a cut and stable identity.", nameof(cuts));
                if (!m_Cuts.TryAdd(target.Id, target.Cut) || !m_CutIds.TryAdd(target.Cut, target.Id))
                    throw new ArgumentException("Cut mutation targets must have unique cut identities and objects.", nameof(cuts));
                m_CutDefinitionSnapshots.Add(target.Cut, CreateCutDefinition(target.Cut, target.Id));
            }

            foreach (var target in timelines ?? throw new ArgumentNullException(nameof(timelines)))
            {
                if (target.Timeline == null || target.ColumnId == null || target.Timeline.Length <= 0)
                    throw new ArgumentException("Timeline mutation targets require a prepared timeline and stable column identity.", nameof(timelines));
                if (!m_Timelines.TryGetValue(target.Timeline, out List<TimelineTarget> timelineTargets))
                {
                    m_Timelines.Add(target.Timeline, timelineTargets = new List<TimelineTarget>(1));
                    m_TimelineAnchorStates.Add(target.Timeline, TimelineAnchorState.Capture(target.Timeline));
                    if (target.Timeline.IsPlaying) m_PlayingTimelineCount++;
                }

                timelineTargets.Add(new TimelineTarget(target.ColumnId));
            }

            SiteState.ColorChanged += OnSiteColorChanged;
            SceneCut.DefinitionChanged += OnCutDefinitionChanged;
            BasicTimeline.AnchorChanged += OnTimelineAnchorChanged;
        }

        public void Apply(V2Mutation mutation, V2MutationApplicationOrigin origin, OperationId operationId)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            ValidateMutation(mutation);
            V2SceneMutationCheckpoint checkpointRollback = origin == V2MutationApplicationOrigin.LocalQuest && mutation is SetConfigurationTransaction ? CaptureCheckpoint() : null;
            SiteConfigurationProvenanceSnapshot checkpointProvenanceRollback = checkpointRollback == null ? null : CapturePendingSiteConfigurationProvenanceSnapshot();
            V2Mutation rollback = origin == V2MutationApplicationOrigin.LocalQuest && mutation is not MoveSites && mutation is not SetConfigurationTransaction ? ReadCurrentMutation(mutation) : null;
            SiteConfigurationProvenanceSnapshot provenanceRollback = rollback == null ? null : CaptureSiteConfigurationProvenanceSnapshot(mutation);
            bool applied;
            using (origin == V2MutationApplicationOrigin.Remote ? V2MutationApplicationContext.EnterRemote(operationId) : V2MutationApplicationContext.EnterLocalApply(ToOriginDevice(origin), operationId))
            {
                applied = ApplyCore(mutation);
            }

            if (origin == V2MutationApplicationOrigin.Remote || applied)
                MarkSiteConfigurationWriters(mutation, operationId.Value);
            if (!applied) return;

            if (origin != V2MutationApplicationOrigin.Remote)
            {
                if (origin == V2MutationApplicationOrigin.LocalQuest)
                {
                    if (checkpointRollback != null) RememberOptimisticCheckpointRollback(operationId, checkpointRollback, checkpointProvenanceRollback);
                    else RememberOptimisticRollback(operationId, rollback, mutation, provenanceRollback);
                }

                MutationProposed?.Invoke(operationId, mutation, ToOriginDevice(origin));
            }
        }

        internal void ApplyOptimisticReplay(V2Mutation mutation, OperationId operationId)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            ValidateMutation(mutation);
            V2SceneMutationCheckpoint checkpointRollback = mutation is SetConfigurationTransaction ? CaptureCheckpoint() : null;
            SiteConfigurationProvenanceSnapshot checkpointProvenanceRollback = checkpointRollback == null ? null : CapturePendingSiteConfigurationProvenanceSnapshot();
            V2Mutation rollback = mutation is not MoveSites && mutation is not SetConfigurationTransaction ? ReadCurrentMutation(mutation) : null;
            SiteConfigurationProvenanceSnapshot provenanceRollback = rollback == null ? null : CaptureSiteConfigurationProvenanceSnapshot(mutation);
            bool applied;
            using (V2MutationApplicationContext.EnterLocalApply(V2OriginDevice.Quest, operationId))
                applied = ApplyCore(mutation);

            MarkSiteConfigurationWriters(mutation, operationId.Value);

            if (checkpointRollback != null) RememberOptimisticCheckpointRollback(operationId, checkpointRollback, checkpointProvenanceRollback);
            else if (rollback != null) RememberOptimisticRollback(operationId, rollback, mutation, provenanceRollback);
        }

        /// <summary>Restores the prepared value recorded before one rejected optimistic Quest operation.</summary>
        public bool TryRollbackOptimisticOperation(OperationId operationId)
        {
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            if (m_OptimisticCheckpointRollbacks.TryGetValue(operationId.Value, out V2SceneMutationCheckpoint checkpoint))
            {
                m_OptimisticCheckpointProvenanceRollbacks.TryGetValue(operationId.Value, out SiteConfigurationProvenanceSnapshot checkpointProvenance);
                ApplyCheckpointRollback(checkpoint, operationId, checkpointProvenance);
                m_OptimisticCheckpointRollbacks.Remove(operationId.Value);
                m_OptimisticCheckpointProvenanceRollbacks.Remove(operationId.Value);
                return true;
            }

            if (!m_OptimisticRollbacks.TryGetValue(operationId.Value, out V2Mutation rollback)) return false;
            m_OptimisticForwardMutations.TryGetValue(operationId.Value, out V2Mutation forward);
            m_OptimisticSiteConfigurationProvenanceRollbacks.TryGetValue(operationId.Value, out SiteConfigurationProvenanceSnapshot rollbackProvenance);
            m_OptimisticRollbacks.Remove(operationId.Value);
            m_OptimisticForwardMutations.Remove(operationId.Value);
            m_OptimisticSiteConfigurationProvenanceRollbacks.Remove(operationId.Value);
            if (forward != null && TryCreateConditionalSiteConfigurationRestoration(operationId, forward, rollback, rollbackProvenance, out SetSiteConfigurationBatch conditionalRestoration, out Dictionary<(string ColumnId, string SiteId), SiteConfigurationFields> restoredFields))
            {
                ApplySiteConfigurationRestoration(conditionalRestoration, operationId, rollbackProvenance, restoredFields);
                RebasePendingSiteConfigurationRollbacks(operationId, forward, rollback, rollbackProvenance);
            }
            else
            {
                Apply(rollback, V2MutationApplicationOrigin.Remote, operationId);
                if (forward != null) RebasePendingSiteConfigurationRollbacks(operationId, forward, rollback, rollbackProvenance);
            }

            return true;
        }

        private void RebasePendingSiteConfigurationRollbacks(OperationId rejectedOperationId, V2Mutation rejectedForward, V2Mutation restoration, SiteConfigurationProvenanceSnapshot rejectedProvenance)
        {
            if (restoration is not SetSiteConfigurationBatch && restoration is not SetSiteBlacklist && restoration is not SetSiteHighlight && restoration is not SetSiteColor && restoration is not SetSiteLabels)
                return;

            Guid rejectedId = rejectedOperationId.Value;
            // A checkpoint captured before this operation cannot contain its optimistic values.
            Guid[] operationOrder = m_OptimisticRollbackOrder.ToArray();
            int rejectedOrder = Array.IndexOf(operationOrder, rejectedId);
            if (rejectedOrder < 0) return;

            foreach (Guid operationId in m_OptimisticRollbacks.Keys.ToArray())
            {
                if (Array.IndexOf(operationOrder, operationId) <= rejectedOrder) continue;
                V2Mutation rollback = m_OptimisticRollbacks[operationId];
                m_OptimisticSiteConfigurationProvenanceRollbacks.TryGetValue(operationId, out SiteConfigurationProvenanceSnapshot provenance);
                if (TryRebaseTargetedSiteConfigurationRollback(rollback, rejectedForward, restoration, rejectedId, rejectedProvenance, provenance, out V2Mutation rebased, out SiteConfigurationProvenanceSnapshot rebasedProvenance))
                {
                    m_OptimisticRollbacks[operationId] = rebased;
                    m_OptimisticSiteConfigurationProvenanceRollbacks[operationId] = rebasedProvenance;
                }
            }

            foreach (Guid operationId in m_OptimisticCheckpointRollbacks.Keys.ToArray())
            {
                if (Array.IndexOf(operationOrder, operationId) <= rejectedOrder) continue;
                V2SceneMutationCheckpoint checkpoint = m_OptimisticCheckpointRollbacks[operationId];
                m_OptimisticCheckpointProvenanceRollbacks.TryGetValue(operationId, out SiteConfigurationProvenanceSnapshot provenance);
                if (TryRebaseCheckpointSiteConfiguration(checkpoint, rejectedForward, restoration, rejectedId, rejectedProvenance, provenance, out V2SceneMutationCheckpoint rebased, out SiteConfigurationProvenanceSnapshot rebasedProvenance))
                {
                    m_OptimisticCheckpointRollbacks[operationId] = rebased;
                    m_OptimisticCheckpointProvenanceRollbacks[operationId] = rebasedProvenance;
                }
            }
        }

        private bool TryCreateConditionalSiteConfigurationRestoration(OperationId rejectedOperationId, V2Mutation forward, V2Mutation restoration, SiteConfigurationProvenanceSnapshot rollbackProvenance, out SetSiteConfigurationBatch conditionalRestoration, out Dictionary<(string ColumnId, string SiteId), SiteConfigurationFields> restoredFields)
        {
            conditionalRestoration = null;
            restoredFields = new Dictionary<(string ColumnId, string SiteId), SiteConfigurationFields>();
            Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue> forwardValues = GetSiteConfigurationValues(forward);
            Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue> restorationValues = GetSiteConfigurationValues(restoration);
            if (forwardValues.Count == 0 || restorationValues.Count == 0) return false;

            var assignments = new List<V2SiteConfigurationAssignment>();
            foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationMutationValue> entry in forwardValues)
            {
                if (!restorationValues.TryGetValue(entry.Key, out SiteConfigurationMutationValue previous)) continue;
                SiteState state = ResolveSite(new ColumnId(entry.Key.ColumnId), new SiteId(entry.Key.SiteId));
                V2SiteConfigurationAssignment current = CaptureSiteConfigurationAssignment(new ColumnId(entry.Key.ColumnId), new SiteId(entry.Key.SiteId), state);
                SiteConfigurationFields fields = entry.Value.Fields & previous.Fields;
                SiteConfigurationFieldOwners owners = GetSiteConfigurationFieldOwners(entry.Key);
                SiteConfigurationFields stillOwned = GetFieldsOwnedBy(owners, rejectedOperationId.Value, fields);
                if (stillOwned == SiteConfigurationFields.None) continue;
                V2SiteConfigurationAssignment restored = CopySiteConfigurationFields(current, previous.Assignment, stillOwned);
                if (!SiteConfigurationAssignmentsEqual(current, restored)) assignments.Add(restored);
                restoredFields[entry.Key] = stillOwned;
            }

            if (assignments.Count > 0) conditionalRestoration = new SetSiteConfigurationBatch(assignments);
            return true;
        }

        private void ApplySiteConfigurationRestoration(SetSiteConfigurationBatch restoration, OperationId operationId, SiteConfigurationProvenanceSnapshot rollbackProvenance, IReadOnlyDictionary<(string ColumnId, string SiteId), SiteConfigurationFields> restoredFields)
        {
            if (restoration != null)
            {
                ValidateMutation(restoration);
                using (V2MutationApplicationContext.EnterRemote(operationId))
                    ApplyCore(restoration);
            }

            foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationFields> entry in restoredFields)
            {
                SiteConfigurationFieldOwners previousOwners = rollbackProvenance != null && rollbackProvenance.TryGet(entry.Key, out SiteConfigurationProvenanceEntry previous) ? previous.Owners : default;
                SetSiteConfigurationFieldOwners(entry.Key, previousOwners, entry.Value);
            }
        }

        private static bool TryRebaseTargetedSiteConfigurationRollback(V2Mutation rollback, V2Mutation rejectedForward, V2Mutation restoration, Guid rejectedOperationId, SiteConfigurationProvenanceSnapshot rejectedProvenance, SiteConfigurationProvenanceSnapshot rollbackProvenance, out V2Mutation rebased, out SiteConfigurationProvenanceSnapshot rebasedProvenance)
        {
            rebased = rollback;
            rebasedProvenance = rollbackProvenance;
            if (rejectedProvenance == null || rollbackProvenance == null) return false;
            Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue> rollbackValues = GetSiteConfigurationValues(rollback);
            Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue> forwardValues = GetSiteConfigurationValues(rejectedForward);
            Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue> restorationValues = GetSiteConfigurationValues(restoration);
            if (rollbackValues.Count == 0 || forwardValues.Count == 0 || restorationValues.Count == 0) return false;

            var assignments = new List<V2SiteConfigurationAssignment>(rollbackValues.Count);
            bool changed = false;
            SiteConfigurationProvenanceSnapshot updatedProvenance = rollbackProvenance.Clone();
            foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationMutationValue> entry in rollbackValues)
            {
                if (!forwardValues.TryGetValue(entry.Key, out SiteConfigurationMutationValue forwardValue) || !restorationValues.TryGetValue(entry.Key, out SiteConfigurationMutationValue restoreValue))
                {
                    assignments.Add(entry.Value.Assignment);
                    continue;
                }

                SiteConfigurationFields fields = entry.Value.Fields & forwardValue.Fields & restoreValue.Fields;
                if (!rollbackProvenance.TryGet(entry.Key, out SiteConfigurationProvenanceEntry rollbackOwners) || !rejectedProvenance.TryGet(entry.Key, out SiteConfigurationProvenanceEntry rejectedOwners))
                {
                    assignments.Add(entry.Value.Assignment);
                    continue;
                }

                SiteConfigurationFields dependentFields = GetFieldsOwnedBy(rollbackOwners.Owners, rejectedOperationId, fields);
                V2SiteConfigurationAssignment merged = CopySiteConfigurationFields(entry.Value.Assignment, restoreValue.Assignment, dependentFields);
                assignments.Add(merged);
                if (dependentFields != SiteConfigurationFields.None)
                {
                    updatedProvenance.SetOwners(entry.Key, dependentFields, rejectedOwners.Owners);
                    changed = true;
                }
            }

            if (!changed) return false;
            rebased = CreateSiteConfigurationMutationLike(rollback, assignments);
            rebasedProvenance = updatedProvenance;
            return true;
        }

        private static bool TryRebaseCheckpointSiteConfiguration(V2SceneMutationCheckpoint checkpoint, V2Mutation rejectedForward, V2Mutation restoration, Guid rejectedOperationId, SiteConfigurationProvenanceSnapshot rejectedProvenance, SiteConfigurationProvenanceSnapshot checkpointProvenance, out V2SceneMutationCheckpoint rebased, out SiteConfigurationProvenanceSnapshot rebasedProvenance)
        {
            rebased = checkpoint;
            rebasedProvenance = checkpointProvenance;
            int batchIndex = -1;
            SetSiteConfigurationBatch currentBatch = null;
            for (int i = 0; i < checkpoint.T11Records.Count; i++)
            {
                if (checkpoint.T11Records[i].Value is not SetSiteConfigurationBatch batch) continue;
                batchIndex = i;
                currentBatch = batch;
                break;
            }

            if (currentBatch == null) return false;
            if (!TryRebaseTargetedSiteConfigurationRollback(currentBatch, rejectedForward, restoration, rejectedOperationId, rejectedProvenance, checkpointProvenance, out V2Mutation rebasedMutation, out rebasedProvenance)) return false;
            var records = checkpoint.T11Records.ToArray();
            records[batchIndex] = new V2T11CheckpointRecord(rebasedMutation);
            rebased = new V2SceneMutationCheckpoint(checkpoint.SiteColors, checkpoint.CutDefinitions, checkpoint.TimelineAnchors, checkpoint.T09Records, checkpoint.T10Records, records, checkpoint.T12Records, checkpoint.T13Records);
            return true;
        }

        private static Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue> GetSiteConfigurationValues(V2Mutation mutation)
        {
            var values = new Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue>();
            if (mutation is SetSiteConfigurationBatch batch)
            {
                foreach (V2SiteConfigurationAssignment assignment in batch.Assignments)
                    values.Add((assignment.ColumnId.Value, assignment.SiteId.Value), new SiteConfigurationMutationValue(assignment, SiteConfigurationFields.All));
                return values;
            }

            ColumnId columnId;
            SiteId siteId;
            SiteConfigurationFields fields;
            bool blacklisted = false;
            bool highlighted = false;
            float red = 0f, green = 0f, blue = 0f, alpha = 1f;
            IEnumerable<string> labels = Array.Empty<string>();
            switch (mutation)
            {
                case SetSiteBlacklist value:
                    columnId = value.ColumnId;
                    siteId = value.SiteId;
                    blacklisted = value.Blacklisted;
                    fields = SiteConfigurationFields.Blacklist;
                    break;
                case SetSiteHighlight value:
                    columnId = value.ColumnId;
                    siteId = value.SiteId;
                    highlighted = value.Highlighted;
                    fields = SiteConfigurationFields.Highlight;
                    break;
                case SetSiteColor value:
                    columnId = value.ColumnId;
                    siteId = value.FullSiteId;
                    red = value.Red;
                    green = value.Green;
                    blue = value.Blue;
                    alpha = value.Alpha;
                    fields = SiteConfigurationFields.Color;
                    break;
                case SetSiteLabels value:
                    columnId = value.ColumnId;
                    siteId = value.SiteId;
                    labels = value.Labels;
                    fields = SiteConfigurationFields.Labels;
                    break;
                default:
                    return values;
            }

            var siteAssignment = new V2SiteConfigurationAssignment(columnId, siteId, blacklisted, highlighted, red, green, blue, alpha, labels);
            values.Add((columnId.Value, siteId.Value), new SiteConfigurationMutationValue(siteAssignment, fields));
            return values;
        }

        private static V2SiteConfigurationAssignment CopySiteConfigurationFields(V2SiteConfigurationAssignment current, V2SiteConfigurationAssignment source, SiteConfigurationFields fields)
        {
            bool blacklisted = (fields & SiteConfigurationFields.Blacklist) != 0 ? source.Blacklisted : current.Blacklisted;
            bool highlighted = (fields & SiteConfigurationFields.Highlight) != 0 ? source.Highlighted : current.Highlighted;
            float red = (fields & SiteConfigurationFields.Color) != 0 ? source.Red : current.Red;
            float green = (fields & SiteConfigurationFields.Color) != 0 ? source.Green : current.Green;
            float blue = (fields & SiteConfigurationFields.Color) != 0 ? source.Blue : current.Blue;
            float alpha = (fields & SiteConfigurationFields.Color) != 0 ? source.Alpha : current.Alpha;
            IEnumerable<string> labels = (fields & SiteConfigurationFields.Labels) != 0 ? source.Labels : current.Labels;
            return new V2SiteConfigurationAssignment(current.ColumnId, current.SiteId, blacklisted, highlighted, red, green, blue, alpha, labels);
        }

        private static V2SiteConfigurationAssignment CaptureSiteConfigurationAssignment(ColumnId columnId, SiteId siteId, SiteState state) => new(columnId, siteId, state.IsBlackListed, state.IsHighlighted, state.Color.r, state.Color.g, state.Color.b, state.Color.a, state.Labels);

        private static V2Mutation CreateSiteConfigurationMutationLike(V2Mutation original, IEnumerable<V2SiteConfigurationAssignment> assignments)
        {
            if (original is SetSiteConfigurationBatch) return new SetSiteConfigurationBatch(assignments);
            V2SiteConfigurationAssignment value = assignments.Single();
            return original switch
            {
                SetSiteBlacklist => new SetSiteBlacklist(value.ColumnId, value.SiteId, value.Blacklisted),
                SetSiteHighlight => new SetSiteHighlight(value.ColumnId, value.SiteId, value.Highlighted),
                SetSiteColor => new SetSiteColor(value.ColumnId, value.SiteId, value.Red, value.Green, value.Blue, value.Alpha),
                SetSiteLabels => new SetSiteLabels(value.ColumnId, value.SiteId, value.Labels),
                _ => throw new ArgumentException("Unsupported site configuration mutation.", nameof(original))
            };
        }

        [Flags]
        private enum SiteConfigurationFields : byte
        {
            None = 0,
            Blacklist = 1 << 0,
            Highlight = 1 << 1,
            Color = 1 << 2,
            Labels = 1 << 3,
            All = Blacklist | Highlight | Color | Labels
        }

        private readonly struct SiteConfigurationMutationValue
        {
            public V2SiteConfigurationAssignment Assignment { get; }
            public SiteConfigurationFields Fields { get; }

            public SiteConfigurationMutationValue(V2SiteConfigurationAssignment assignment, SiteConfigurationFields fields)
            {
                Assignment = assignment;
                Fields = fields;
            }
        }

        private readonly struct SiteConfigurationFieldOwners
        {
            public Guid BlacklistWriter { get; }
            public Guid HighlightWriter { get; }
            public Guid ColorWriter { get; }
            public Guid LabelsWriter { get; }

            public SiteConfigurationFieldOwners(Guid blacklistWriter, Guid highlightWriter, Guid colorWriter, Guid labelsWriter)
            {
                BlacklistWriter = blacklistWriter;
                HighlightWriter = highlightWriter;
                ColorWriter = colorWriter;
                LabelsWriter = labelsWriter;
            }

            public Guid GetWriter(SiteConfigurationFields field) =>
                field switch
                {
                    SiteConfigurationFields.Blacklist => BlacklistWriter,
                    SiteConfigurationFields.Highlight => HighlightWriter,
                    SiteConfigurationFields.Color => ColorWriter,
                    SiteConfigurationFields.Labels => LabelsWriter,
                    _ => Guid.Empty
                };

            public SiteConfigurationFieldOwners WithOwners(SiteConfigurationFields fields, SiteConfigurationFieldOwners source) => new((fields & SiteConfigurationFields.Blacklist) != 0 ? source.BlacklistWriter : BlacklistWriter, (fields & SiteConfigurationFields.Highlight) != 0 ? source.HighlightWriter : HighlightWriter, (fields & SiteConfigurationFields.Color) != 0 ? source.ColorWriter : ColorWriter, (fields & SiteConfigurationFields.Labels) != 0 ? source.LabelsWriter : LabelsWriter);

            public static SiteConfigurationFieldOwners ForWriter(Guid writer) => new(writer, writer, writer, writer);
        }

        private readonly struct SiteConfigurationProvenanceEntry
        {
            public SiteConfigurationFields Fields { get; }
            public SiteConfigurationFieldOwners Owners { get; }

            public SiteConfigurationProvenanceEntry(SiteConfigurationFields fields, SiteConfigurationFieldOwners owners)
            {
                Fields = fields;
                Owners = owners;
            }
        }

        private sealed class SiteConfigurationProvenanceSnapshot
        {
            private readonly Dictionary<(string ColumnId, string SiteId), SiteConfigurationProvenanceEntry> m_Entries = new();

            public IEnumerable<KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationProvenanceEntry>> Entries => m_Entries;

            public bool TryGet((string ColumnId, string SiteId) key, out SiteConfigurationProvenanceEntry entry) => m_Entries.TryGetValue(key, out entry);

            public void SetOwners((string ColumnId, string SiteId) key, SiteConfigurationFields fields, SiteConfigurationFieldOwners owners)
            {
                m_Entries.TryGetValue(key, out SiteConfigurationProvenanceEntry existing);
                m_Entries[key] = new SiteConfigurationProvenanceEntry(existing.Fields | fields, existing.Owners.WithOwners(fields, owners));
            }

            public SiteConfigurationProvenanceSnapshot Clone()
            {
                var clone = new SiteConfigurationProvenanceSnapshot();
                foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationProvenanceEntry> entry in m_Entries)
                    clone.m_Entries.Add(entry.Key, entry.Value);
                return clone;
            }
        }

        private SiteConfigurationFieldOwners GetSiteConfigurationFieldOwners((string ColumnId, string SiteId) key) => m_SiteConfigurationFieldOwners.TryGetValue(key, out SiteConfigurationFieldOwners owners) ? owners : default;

        private void SetSiteConfigurationFieldOwners((string ColumnId, string SiteId) key, SiteConfigurationFieldOwners owners, SiteConfigurationFields fields)
        {
            SiteConfigurationFieldOwners current = GetSiteConfigurationFieldOwners(key);
            m_SiteConfigurationFieldOwners[key] = current.WithOwners(fields, owners);
        }

        private SiteConfigurationProvenanceSnapshot CaptureSiteConfigurationProvenanceSnapshot(V2Mutation mutation) => CaptureSiteConfigurationProvenanceSnapshot(GetSiteConfigurationValues(mutation));

        private SiteConfigurationProvenanceSnapshot CapturePendingSiteConfigurationProvenanceSnapshot()
        {
            var pendingValues = new Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue>();
            foreach (V2Mutation mutation in m_OptimisticForwardMutations.Values)
            foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationMutationValue> value in GetSiteConfigurationValues(mutation))
            {
                if (pendingValues.TryGetValue(value.Key, out SiteConfigurationMutationValue existing))
                    pendingValues[value.Key] = new SiteConfigurationMutationValue(existing.Assignment, existing.Fields | value.Value.Fields);
                else
                    pendingValues.Add(value.Key, value.Value);
            }

            return CaptureSiteConfigurationProvenanceSnapshot(pendingValues);
        }

        private SiteConfigurationProvenanceSnapshot CaptureSiteConfigurationProvenanceSnapshot(Dictionary<(string ColumnId, string SiteId), SiteConfigurationMutationValue> values)
        {
            var snapshot = new SiteConfigurationProvenanceSnapshot();
            foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationMutationValue> value in values)
                snapshot.SetOwners(value.Key, value.Value.Fields, GetSiteConfigurationFieldOwners(value.Key));
            return snapshot;
        }

        private void MarkSiteConfigurationWriters(V2Mutation mutation, Guid writer)
        {
            if (mutation is SetConfigurationTransaction transaction)
            {
                foreach (V2Mutation child in transaction.Mutations)
                    MarkSiteConfigurationWriters(child, writer);
                return;
            }

            foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationMutationValue> value in GetSiteConfigurationValues(mutation))
                SetSiteConfigurationFieldOwners(value.Key, SiteConfigurationFieldOwners.ForWriter(writer), value.Value.Fields);
        }

        private void RestoreSiteConfigurationProvenance(SiteConfigurationProvenanceSnapshot snapshot)
        {
            if (snapshot == null) return;
            foreach (KeyValuePair<(string ColumnId, string SiteId), SiteConfigurationProvenanceEntry> entry in snapshot.Entries)
                SetSiteConfigurationFieldOwners(entry.Key, entry.Value.Owners, entry.Value.Fields);
        }

        private void ClearCheckpointSiteConfigurationProvenance(V2SceneMutationCheckpoint checkpoint)
        {
            foreach (V2SiteConfigurationAssignment assignment in checkpoint.T11Records.Select(record => record.Value).OfType<SetSiteConfigurationBatch>().SelectMany(batch => batch.Assignments))
                SetSiteConfigurationFieldOwners((assignment.ColumnId.Value, assignment.SiteId.Value), default, SiteConfigurationFields.All);
        }

        private void MarkCheckpointSiteConfigurationWriters(V2SceneMutationCheckpoint checkpoint, Guid writer)
        {
            foreach (V2SiteConfigurationAssignment assignment in checkpoint.T11Records.Select(record => record.Value).OfType<SetSiteConfigurationBatch>().SelectMany(batch => batch.Assignments))
                SetSiteConfigurationFieldOwners((assignment.ColumnId.Value, assignment.SiteId.Value), SiteConfigurationFieldOwners.ForWriter(writer), SiteConfigurationFields.All);
        }

        private static SiteConfigurationFields GetFieldsOwnedBy(SiteConfigurationFieldOwners owners, Guid writer, SiteConfigurationFields fields)
        {
            SiteConfigurationFields owned = SiteConfigurationFields.None;
            foreach (SiteConfigurationFields field in new[] { SiteConfigurationFields.Blacklist, SiteConfigurationFields.Highlight, SiteConfigurationFields.Color, SiteConfigurationFields.Labels })
                if ((fields & field) != 0 && owners.GetWriter(field) == writer)
                    owned |= field;
            return owned;
        }

        public void ForgetOptimisticOperation(OperationId operationId)
        {
            if (operationId == null) return;
            m_OptimisticRollbacks.Remove(operationId.Value);
            m_OptimisticForwardMutations.Remove(operationId.Value);
            m_OptimisticSiteConfigurationProvenanceRollbacks.Remove(operationId.Value);
            m_OptimisticCheckpointRollbacks.Remove(operationId.Value);
            m_OptimisticCheckpointProvenanceRollbacks.Remove(operationId.Value);
        }

        private void RememberOptimisticRollback(OperationId operationId, V2Mutation rollback, V2Mutation forward = null, SiteConfigurationProvenanceSnapshot provenanceRollback = null)
        {
            if (m_LocalOrigin != V2OriginDevice.Quest || operationId == null || rollback == null) return;
            Guid id = operationId.Value;
            if (m_OptimisticRollbacks.ContainsKey(id)) m_OptimisticRollbacks[id] = rollback;
            else
            {
                m_OptimisticRollbacks.Add(id, rollback);
                m_OptimisticRollbackOrder.Enqueue(id);
            }

            if (forward != null) m_OptimisticForwardMutations[id] = forward;
            if (provenanceRollback != null) m_OptimisticSiteConfigurationProvenanceRollbacks[id] = provenanceRollback;

            while (m_OptimisticRollbacks.Count + m_OptimisticCheckpointRollbacks.Count > V2QuestMutationDriver.MaximumRememberedOperations)
            {
                Guid oldest = m_OptimisticRollbackOrder.Dequeue();
                m_OptimisticRollbacks.Remove(oldest);
                m_OptimisticForwardMutations.Remove(oldest);
                m_OptimisticSiteConfigurationProvenanceRollbacks.Remove(oldest);
                m_OptimisticCheckpointRollbacks.Remove(oldest);
                m_OptimisticCheckpointProvenanceRollbacks.Remove(oldest);
            }
        }

        private void RememberOptimisticCheckpointRollback(OperationId operationId, V2SceneMutationCheckpoint checkpoint, SiteConfigurationProvenanceSnapshot provenanceRollback = null)
        {
            if (m_LocalOrigin != V2OriginDevice.Quest || operationId == null || checkpoint == null) return;
            Guid id = operationId.Value;
            if (!m_OptimisticRollbacks.ContainsKey(id) && !m_OptimisticCheckpointRollbacks.ContainsKey(id))
                m_OptimisticRollbackOrder.Enqueue(id);
            m_OptimisticRollbacks.Remove(id);
            m_OptimisticForwardMutations.Remove(id);
            m_OptimisticSiteConfigurationProvenanceRollbacks.Remove(id);
            m_OptimisticCheckpointRollbacks[id] = checkpoint;
            if (provenanceRollback != null) m_OptimisticCheckpointProvenanceRollbacks[id] = provenanceRollback;
            while (m_OptimisticRollbacks.Count + m_OptimisticCheckpointRollbacks.Count > V2QuestMutationDriver.MaximumRememberedOperations)
            {
                Guid oldest = m_OptimisticRollbackOrder.Dequeue();
                m_OptimisticRollbacks.Remove(oldest);
                m_OptimisticForwardMutations.Remove(oldest);
                m_OptimisticSiteConfigurationProvenanceRollbacks.Remove(oldest);
                m_OptimisticCheckpointRollbacks.Remove(oldest);
                m_OptimisticCheckpointProvenanceRollbacks.Remove(oldest);
            }
        }

        /// <summary>Reads the current prepared value for the touched key of a typed mutation.</summary>
        public V2Mutation ReadCurrentMutation(V2Mutation key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key is SetSiteFilterResult) throw new InvalidOperationException("Site-filter job results are scene-wide and cannot be optimistically corrected.");
            if (key is SetCorrelationResult) throw new InvalidOperationException("Correlation job results are scene-wide and cannot be optimistically corrected.");
            if (key is SetSiteColor siteColor)
            {
                SiteState state = ResolveSite(siteColor.ColumnId, siteColor.FullSiteId);
                return CreateSiteColor(new SiteTarget(siteColor.ColumnId, siteColor.FullSiteId), state.Color);
            }

            if (key is SetCutDefinition cutDefinition)
                return CreateCutDefinition(ResolveCut(cutDefinition.CutId), cutDefinition.CutId);

            if (key is SetTimelineAnchor timelineAnchor)
            {
                (BasicTimeline timeline, ColumnId columnId) = ResolveTimeline(timelineAnchor.ColumnId);
                return CreateTimelineAnchor(timeline, columnId);
            }

            if ((ushort)key.Type >= (ushort)V2OperationType.SetSiteBlacklist)
                return ReadCurrentT11Mutation(key);

            if ((ushort)key.Type >= (ushort)V2OperationType.CreateCut)
                return ReadCurrentT10Mutation(key);

            if ((ushort)key.Type >= (ushort)V2OperationType.SetSelectedColumn)
                return ReadCurrentT09Mutation(key);

            throw new ArgumentException("Unsupported v2 scene mutation.", nameof(key));
        }

        /// <summary>Checks prepared targets and typed values that could fail before an authority commits a sequence.</summary>
        internal void ValidateMutation(V2Mutation mutation)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            if (mutation is SetSiteFilterResult filterResult)
            {
                ValidateSiteFilterResult(filterResult);
                return;
            }

            if (mutation is SetCorrelationResult) return;

            if (mutation is SetSiteColor siteColor)
            {
                ResolveSite(siteColor.ColumnId, siteColor.FullSiteId);
                return;
            }

            if (mutation is SetCutDefinition cutDefinition)
            {
                ResolveCut(cutDefinition.CutId);
                if (cutDefinition.NumberOfCuts > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(mutation), "Cut count exceeds the prepared scene's supported range.");
                return;
            }

            if (mutation is SetTimelineAnchor timelineAnchor)
            {
                (BasicTimeline timeline, _) = ResolveTimeline(timelineAnchor.ColumnId);
                if (timelineAnchor.Index >= timeline.Length)
                    throw new ArgumentOutOfRangeException(nameof(mutation), "Timeline index exceeds the prepared timeline.");
                return;
            }

            if ((ushort)mutation.Type >= (ushort)V2OperationType.SetSiteBlacklist)
            {
                ValidateT11Mutation(mutation);
                return;
            }

            if ((ushort)mutation.Type >= (ushort)V2OperationType.CreateCut)
            {
                ValidateT10Mutation(mutation);
                return;
            }

            if ((ushort)mutation.Type >= (ushort)V2OperationType.SetSelectedColumn)
            {
                ValidateT09Mutation(mutation);
                return;
            }

            throw new ArgumentException("Unsupported v2 scene mutation.", nameof(mutation));
        }

        public V2SceneMutationCheckpoint CaptureCheckpoint()
        {
            var siteRecords = new List<SiteColorCheckpointRecord>();

            var cutRecords = new List<CutDefinitionCheckpointRecord>();
            foreach (KeyValuePair<SceneCut, CutId> entry in m_CutIds)
                cutRecords.Add(new CutDefinitionCheckpointRecord(CreateCutDefinition(entry.Key, entry.Value)));

            var timelineRecords = new List<TimelineAnchorCheckpointRecord>();
            foreach (KeyValuePair<BasicTimeline, List<TimelineTarget>> entry in m_Timelines)
            foreach (TimelineTarget target in entry.Value)
                timelineRecords.Add(new TimelineAnchorCheckpointRecord(CreateTimelineAnchor(entry.Key, target.ColumnId)));

            var t09Records = m_Scene == null ? new List<V2T09CheckpointRecord>() : CaptureT09Records();
            var t10Records = m_Scene == null ? new List<V2T10CheckpointRecord>() : CaptureT10Records();
            var t11Records = CaptureT11Records();
            var t12Records = new List<V2SiteFilterCheckpointRecord>();
            if (CaptureSiteFilterCheckpoint() is V2SiteFilterCheckpointRecord filterRecord) t12Records.Add(filterRecord);
            CorrelationResultResource correlationResource = m_Scene == null ? null : CorrelationResultResource.Capture(m_Scene);
            var t13Records = new[] { new V2CorrelationCheckpointRecord(correlationResource?.Encode(), m_Scene != null && m_Scene.DisplayCorrelations) };
            return new V2SceneMutationCheckpoint(siteRecords, cutRecords, timelineRecords, t09Records, t10Records, t11Records, t12Records, t13Records);
        }

        public SetSiteFilterResult CreateSiteFilterResult(OperationId jobId, ulong generation, bool[] included)
        {
            if (included == null) throw new ArgumentNullException(nameof(included));
            IReadOnlyList<SiteFilterTarget> roster = CreateSiteFilterRoster();
            if (included.Length != roster.Count) throw new ArgumentException("The inclusion mask does not match the prepared site roster.", nameof(included));
            return CreateSiteFilterResult(jobId, generation, included, CreateSiteFilterRosterHash(roster));
        }

        public SetCorrelationResult CreateCorrelationResult(OperationId jobId, ulong generation, byte[] resultBytes)
        {
            var result = new SetCorrelationResult(jobId, generation, resultBytes);
            ValidateCorrelationResult(result);
            return result;
        }

        public SetSiteFilterResult CreateSiteFilterResult(OperationId jobId, ulong generation, bool[] included, byte[] expectedRosterHash)
        {
            if (included == null) throw new ArgumentNullException(nameof(included));
            if (expectedRosterHash == null || expectedRosterHash.Length != 32) throw new ArgumentException("A prepared site-roster identity requires a SHA-256 hash.", nameof(expectedRosterHash));
            IReadOnlyList<SiteFilterTarget> roster = CreateSiteFilterRoster();
            if (included.Length != roster.Count) throw new InvalidOperationException("The prepared site roster changed while the filter was being evaluated.");
            byte[] currentHash = CreateSiteFilterRosterHash(roster);
            int difference = 0;
            for (int i = 0; i < currentHash.Length; i++) difference |= currentHash[i] ^ expectedRosterHash[i];
            if (difference != 0) throw new InvalidOperationException("The prepared site roster changed while the filter was being evaluated.");
            return new SetSiteFilterResult(jobId, generation, currentHash, roster.Count, V2SiteFilterMaskCodec.EncodeBits(included));
        }

        public (int SiteCount, byte[] RosterHash) CaptureSiteFilterRosterIdentity()
        {
            IReadOnlyList<SiteFilterTarget> roster = CreateSiteFilterRoster();
            return (roster.Count, CreateSiteFilterRosterHash(roster));
        }

        public (IReadOnlyList<(SiteState State, ColumnId ColumnId, SiteId SiteId)> Targets, byte[] RosterHash) CaptureSiteFilterRoster()
        {
            IReadOnlyList<SiteFilterTarget> roster = CreateSiteFilterRoster();
            var targets = roster.Select(target => (target.State, target.ColumnId, target.SiteId)).ToArray();
            return (Array.AsReadOnly(targets), CreateSiteFilterRosterHash(roster));
        }

        private V2SiteFilterCheckpointRecord CaptureSiteFilterCheckpoint()
        {
            var roster = CreateSiteFilterRoster();
            if (roster.Count == 0) return null;
            bool[] included = roster.Select(target => target.State.IsFiltered).ToArray();
            return new V2SiteFilterCheckpointRecord(CreateSiteFilterRosterHash(roster), roster.Count, V2SiteFilterMaskCodec.EncodeBits(included));
        }

        private void ValidateSiteFilterCheckpoint(IReadOnlyList<V2SiteFilterCheckpointRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (records.Count > 1) throw new InvalidDataException("A checkpoint can contain at most one T12 site-filter result.");
            if (records.Count == 0) return;
            ValidateSiteFilterMask(records[0].RosterHash, records[0].SiteCount);
        }

        private void ValidateCorrelationCheckpoint(IReadOnlyList<V2CorrelationCheckpointRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (records.Count > 1 || records.Any(record => record == null)) throw new InvalidDataException("A checkpoint can contain at most one T13 correlation result.");
            if (records.Count == 0 || !records[0].HasResult) return;
            CorrelationResultResource.Decode(records[0].ResultBytes).ValidateFor(m_Scene, m_Scene.Columns.SelectMany(column => column.Sites).Select(site => site.Information.FullID).ToArray());
        }

        private void ValidateSiteFilterResult(SetSiteFilterResult result)
        {
            ValidateSiteFilterMask(result.RosterHash, result.SiteCount);
        }

        private void ValidateCorrelationResult(SetCorrelationResult result)
        {
            if (m_Scene == null) throw new InvalidOperationException("Correlation results require a prepared scene.");
            CorrelationResultResource resource = CorrelationResultResource.Decode(result.ResultBytes);
            resource.ValidateFor(m_Scene, m_Scene.Columns.SelectMany(column => column.Sites).Select(site => site.Information.FullID).ToArray());
        }

        private void ValidateSiteFilterMask(byte[] rosterHash, int siteCount)
        {
            IReadOnlyList<SiteFilterTarget> roster = CreateSiteFilterRoster();
            if (roster.Count != siteCount)
                throw new InvalidDataException("The site-filter result does not match the prepared site roster size.");
            byte[] expectedHash = CreateSiteFilterRosterHash(roster);
            int hashDifference = 0;
            for (int i = 0; i < expectedHash.Length; i++) hashDifference |= expectedHash[i] ^ rosterHash[i];
            if (hashDifference != 0)
                throw new InvalidDataException("The site-filter result belongs to a different prepared site roster.");
        }

        private bool ApplySiteFilterResult(SetSiteFilterResult result)
        {
            ValidateSiteFilterResult(result);
            IReadOnlyList<SiteFilterTarget> roster = CreateSiteFilterRoster();
            bool changed = false;
            for (int i = 0; i < roster.Count; i++)
                changed |= roster[i].State.IsFiltered != result.IsIncluded(i);
            if (changed)
            {
                Action apply = () => { SiteState.ApplyFilteredStateBatch(CreateFilterAssignments(roster, result.IsIncluded)); };
                if (m_Scene != null) m_Scene.ApplySiteStateBatch(apply);
                else apply();
                Module3DMain.OnRequestUpdateInSiteList.Invoke();
            }

            return true;
        }

        private bool ApplyCorrelationResult(SetCorrelationResult result)
        {
            CorrelationResultResource.Decode(result.ResultBytes).Apply(m_Scene);
            m_Scene.DisplayCorrelations = true;
            return true;
        }

        private void ApplySiteFilterCheckpoint(V2SiteFilterCheckpointRecord record)
        {
            IReadOnlyList<SiteFilterTarget> roster = CreateSiteFilterRoster();
            Action apply = () => { SiteState.ApplyFilteredStateBatch(CreateFilterAssignments(roster, record.IsIncluded)); };
            if (m_Scene != null) m_Scene.ApplySiteStateBatch(apply);
            else apply();
            Module3DMain.OnRequestUpdateInSiteList.Invoke();
        }

        private static IEnumerable<(SiteState State, bool Included)> CreateFilterAssignments(IReadOnlyList<SiteFilterTarget> roster, Func<int, bool> includedAt)
        {
            var assignments = new Dictionary<SiteState, bool>();
            for (int i = 0; i < roster.Count; i++)
            {
                SiteState state = roster[i].State;
                bool included = includedAt(i);
                if (assignments.TryGetValue(state, out bool previous) && previous != included)
                    throw new InvalidDataException("The site-filter result assigns conflicting values to an aliased site state.");
                assignments[state] = included;
            }

            return assignments.Select(assignment => (assignment.Key, assignment.Value));
        }

        private IReadOnlyList<SiteFilterTarget> CreateSiteFilterRoster()
        {
            return m_SitesById.OrderBy(entry => entry.Key.ColumnId.Value, StringComparer.Ordinal).ThenBy(entry => entry.Key.SiteId.Value, StringComparer.Ordinal).Select(entry => new SiteFilterTarget(entry.Key.ColumnId, entry.Key.SiteId, entry.Value)).ToArray();
        }

        private static byte[] CreateSiteFilterRosterHash(IReadOnlyList<SiteFilterTarget> roster)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(roster.Count);
                foreach (SiteFilterTarget target in roster)
                {
                    WriteRosterIdentity(writer, target.ColumnId.Value);
                    WriteRosterIdentity(writer, target.SiteId.Value);
                }
            }

            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(stream.ToArray());
        }

        private static void WriteRosterIdentity(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        private readonly struct SiteFilterTarget
        {
            public ColumnId ColumnId { get; }
            public SiteId SiteId { get; }
            public SiteState State { get; }

            public SiteFilterTarget(ColumnId columnId, SiteId siteId, SiteState state)
            {
                ColumnId = columnId;
                SiteId = siteId;
                State = state;
            }
        }

        private List<V2T10CheckpointRecord> CaptureT10Records()
        {
            var records = new List<V2T10CheckpointRecord>();
            void Add(V2Mutation mutation) => records.Add(new V2T10CheckpointRecord(mutation));

            foreach (SceneCut cut in m_Scene.Cuts)
            {
                CutId id = m_CutIds[cut];
                Add(new CreateCut(id, CreateCutDefinition(cut, id), cut.Index));
            }

            Add(new SetCutOrder(m_Scene.Cuts.Select(cut => m_CutIds[cut])));

            if (m_Scene.ROIManager != null)
            {
                foreach (ROI roi in m_Scene.ROIManager.ROIs) Add(CreateRoiMutation(roi));
                Add(new SetActiveRoi(m_Scene.ROIManager.SelectedROI ? new RoiId(m_Scene.ROIManager.SelectedROI.ID) : null));
            }

            if (m_Scene.MRIManager != null)
                Add(new SetMriCalibration(m_Scene.MRIManager.MRICalMinFactor, m_Scene.MRIManager.MRICalMaxFactor));

            if (m_ResourceCatalog != null)
            {
                if (m_Scene.MeshManager?.SelectedMesh != null) Add(CreateMeshDisplayMutation());
                if (m_Scene.MRIManager?.SelectedMRI != null) Add(CreateSelectedMriMutation());
                if (m_Scene.ImplantationManager?.SelectedImplantation != null) Add(CreateImplantationMutation());
                if (m_Scene.MeshManager?.SelectedMesh != null && m_Scene.TriangleEraser != null) Add(CreateTriangleMaskMutation());
            }

            return records;
        }

        private void ValidateCheckpointT10Records(IReadOnlyList<V2T10CheckpointRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (records.Count == 0) return;
            var touched = new HashSet<V2TouchedKey>();
            var stagedCuts = new HashSet<CutId>();
            var stagedRois = new HashSet<string>(StringComparer.Ordinal);
            var stagedSpheres = new HashSet<string>(StringComparer.Ordinal);
            SetCutOrder cutOrder = null;
            SetActiveRoi activeRoiRoster = null;
            SetMeshDisplay meshDisplay = null;
            ApplyTriangleMask masks = null;

            var validationScene = new SceneId(Guid.Parse("70000000-0000-0000-0000-000000000001"));
            var validationIncarnation = new IncarnationId(Guid.Parse("70000000-0000-0000-0000-000000000002"));
            foreach (V2T10CheckpointRecord record in records)
            {
                if (record?.Value == null || !IsCheckpointT10Mutation(record.Value))
                    throw new ArgumentException("Checkpoint contains an unsupported T10 record.", nameof(records));
                V2MutationDescriptor descriptor = V2MutationDescriptor.Create(validationScene, validationIncarnation, record.Value);
                foreach (V2TouchedKey key in descriptor.TouchedKeys)
                    if (!touched.Add(key))
                        throw new ArgumentException("Checkpoint contains a duplicate T10 record key.", nameof(records));

                switch (record.Value)
                {
                    case CreateCut value:
                        if (!stagedCuts.Add(value.CutId)) throw new ArgumentException("Checkpoint contains a duplicate cut identity.", nameof(records));
                        break;
                    case SetCutOrder value:
                        if (cutOrder != null) throw new ArgumentException("Checkpoint contains more than one cut order.", nameof(records));
                        cutOrder = value;
                        break;
                    case CreateRoi value:
                        RequireRoiManager();
                        if (!stagedRois.Add(value.RoiId.Value)) throw new ArgumentException("Checkpoint contains a duplicate ROI identity.", nameof(records));
                        break;
                    case SetActiveRoi value:
                        RequireRoiManager();
                        if (activeRoiRoster != null) throw new ArgumentException("Checkpoint contains more than one active ROI record.", nameof(records));
                        activeRoiRoster = value;
                        break;
                    case SetMeshDisplay value:
                        ValidateT10Mutation(value);
                        meshDisplay = value;
                        break;
                    case ApplyTriangleMask value:
                        masks = value;
                        break;
                    default:
                        ValidateT10Mutation(record.Value);
                        break;
                }
            }

            if (cutOrder != null)
            {
                if (cutOrder.CutIds.Count != stagedCuts.Count || cutOrder.CutIds.Any(id => !stagedCuts.Contains(id)))
                    throw new InvalidDataException("Checkpoint cut order must name every cut identity exactly once.");
                foreach (CreateCut cut in records.Select(record => record.Value).OfType<CreateCut>())
                    if (cut.Order >= stagedCuts.Count)
                        throw new ArgumentOutOfRangeException(nameof(records), "Checkpoint cut order exceeds the prepared scene.");
            }
            else if (stagedCuts.Count > 0 || m_Cuts.Count > 0)
            {
                throw new InvalidDataException("Checkpoint cut roster is missing its typed order record.");
            }

            var availableRois = new HashSet<string>(stagedRois, StringComparer.Ordinal);
            if (activeRoiRoster == null)
                foreach (string id in m_RoisById.Keys)
                    availableRois.Add(id);
            if (activeRoiRoster?.RoiId != null && !availableRois.Contains(activeRoiRoster.RoiId.Value))
                throw new KeyNotFoundException("Checkpoint active ROI is not present in the authoritative ROI roster.");

            foreach (CreateRoi roi in records.Select(record => record.Value).OfType<CreateRoi>())
            {
                int roiRosterSize = activeRoiRoster == null ? availableRois.Count : stagedRois.Count;
                if (roi.Order >= roiRosterSize) throw new ArgumentOutOfRangeException(nameof(records), "Checkpoint ROI order exceeds the prepared scene.");
                foreach (V2RoiSphereDefinition sphere in roi.Spheres)
                    if (!stagedSpheres.Add(sphere.SphereId.Value))
                        throw new InvalidDataException("Checkpoint contains duplicate sphere identities across ROIs.");
            }

            foreach (CreateRoi roiRecord in records.Select(record => record.Value).OfType<CreateRoi>())
            {
                foreach (V2RoiSphereDefinition sphere in roiRecord.Spheres)
                {
                    if (m_RoisById.Values.Any(existingRoi => (activeRoiRoster == null || stagedRois.Contains(existingRoi.ID)) && !StringComparer.Ordinal.Equals(existingRoi.ID, roiRecord.RoiId.Value) && existingRoi.Spheres.Any(existingSphere => existingSphere.ID == sphere.SphereId.Value)))
                        throw new InvalidDataException("Checkpoint ROI sphere identity is already owned by another ROI.");
                }
            }

            if (masks != null)
            {
                if (meshDisplay == null)
                {
                    ValidateTriangleMask(masks);
                }
                else
                {
                    Mesh3D mesh = m_ResourceCatalog.ResolveMesh(meshDisplay.MeshId.Value);
                    MeshPart part = meshDisplay.Part switch { V2MeshPart.Left => MeshPart.Left, V2MeshPart.Right => MeshPart.Right, _ => MeshPart.Both };
                    Core.Object3D.SurfaceRepresentation representation = meshDisplay.Representation == V2SurfaceRepresentation.Inflated ? Core.Object3D.SurfaceRepresentation.Inflated : Core.Object3D.SurfaceRepresentation.Anatomical;
                    string resource = m_ResourceCatalog.MeshReference(mesh);
                    TopologyId expectedComplete = new("surface:" + resource + ":" + part.ToString().ToLowerInvariant() + ":complete");
                    TopologyId expectedSimplified = new("surface:" + resource + ":" + part.ToString().ToLowerInvariant() + ":simplified");
                    int completeCount = mesh.GetSurface(representation, part, simplified: false).NumberOfTriangles;
                    int simplifiedCount = mesh.GetSurface(representation, part, simplified: true).NumberOfTriangles;
                    if (!masks.Masks[0].TopologyId.Equals(expectedComplete) || masks.Masks[0].TriangleCount != completeCount || !masks.Masks[1].TopologyId.Equals(expectedSimplified) || masks.Masks[1].TriangleCount != simplifiedCount)
                        throw new InvalidDataException("Checkpoint triangle masks do not match the selected prepared original topology.");
                }
            }
        }

        private static bool IsCheckpointT10Mutation(V2Mutation mutation) => mutation.Type is V2OperationType.CreateCut or V2OperationType.SetCutOrder or V2OperationType.CreateRoi or V2OperationType.SetActiveRoi or V2OperationType.SetMeshDisplay or V2OperationType.SetSelectedMri or V2OperationType.SetMriCalibration or V2OperationType.SetImplantation or V2OperationType.ApplyTriangleMask;

        private void ApplyCheckpointT10Records(IReadOnlyList<V2T10CheckpointRecord> records)
        {
            foreach (V2T10CheckpointRecord record in records.OrderBy(record => GetCheckpointApplyOrder(record.Value)).ThenBy(record => record.Value is CreateCut cut ? cut.Order : record.Value is CreateRoi roi ? roi.Order : 0))
                ApplyCheckpointT10Mutation(record.Value);
        }

        private static int GetCheckpointApplyOrder(V2Mutation mutation) =>
            mutation.Type switch
            {
                V2OperationType.CreateCut => 0,
                V2OperationType.SetCutOrder => 1,
                V2OperationType.CreateRoi => 2,
                V2OperationType.SetActiveRoi => 3,
                V2OperationType.SetMeshDisplay => 4,
                V2OperationType.SetSelectedMri => 5,
                V2OperationType.SetMriCalibration => 6,
                V2OperationType.SetImplantation => 7,
                V2OperationType.ApplyTriangleMask => 8,
                _ => throw new ArgumentException("Unsupported T10 checkpoint mutation.", nameof(mutation))
            };

        private void ApplyCheckpointT10Mutation(V2Mutation mutation)
        {
            switch (mutation)
            {
                case CreateCut value:
                    if (!m_Cuts.TryGetValue(value.CutId, out SceneCut cut))
                    {
                        cut = m_Scene.AddCutPlane(value.CutId.Value);
                        RegisterCut(cut);
                    }

                    ApplyCore(value.Definition);
                    MoveCutToOrder(value.CutId, value.Order);
                    break;
                case SetCutOrder value:
                    if (!m_Scene.Cuts.Select(cut => m_CutIds[cut]).SequenceEqual(value.CutIds)) ApplyCutOrder(value);
                    break;
                case CreateRoi value:
                    ApplyCheckpointRoi(value);
                    break;
                case SetActiveRoi value:
                    ApplyCore(value);
                    break;
                default:
                    ApplyT10Mutation(mutation);
                    break;
            }
        }

        private void ValidateCheckpointT11Records(IReadOnlyList<V2T11CheckpointRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            var touched = new HashSet<V2TouchedKey>();
            var validationScene = new SceneId(Guid.Parse("70000000-0000-0000-0000-000000000001"));
            var validationIncarnation = new IncarnationId(Guid.Parse("70000000-0000-0000-0000-000000000002"));
            foreach (V2T11CheckpointRecord record in records)
            {
                if (record?.Value == null || record.Value is SetConfigurationTransaction)
                    throw new ArgumentException("Checkpoint contains an unsupported T11 record.", nameof(records));
                V2MutationDescriptor descriptor = V2MutationDescriptor.Create(validationScene, validationIncarnation, record.Value);
                foreach (V2TouchedKey key in descriptor.TouchedKeys)
                    if (!touched.Add(key))
                        throw new ArgumentException("Checkpoint contains a duplicate T11 record key.", nameof(records));
                ValidateMutation(record.Value);
            }
        }

        private void ApplyCheckpointT11Records(IReadOnlyList<V2T11CheckpointRecord> records)
        {
            foreach (V2T11CheckpointRecord record in records.OrderBy(record => GetCheckpointT11ApplyOrder(record.Value)))
                ApplyCore(record.Value);
        }

        private static int GetCheckpointT11ApplyOrder(V2Mutation mutation) =>
            mutation.Type switch
            {
                V2OperationType.SetInfluenceDistance => 0,
                V2OperationType.SetColumnResource => 1,
                V2OperationType.SetCcepSource => 2,
                V2OperationType.SetSiteBlacklist => 3,
                V2OperationType.SetSiteConfigurationBatch => 4,
                _ => throw new ArgumentException("Unsupported T11 checkpoint mutation.", nameof(mutation))
            };

        private void ApplyCheckpointRoi(CreateRoi value)
        {
            if (!m_RoisById.TryGetValue(value.RoiId.Value, out ROI roi))
            {
                roi = m_Scene.ROIManager.AddROI(value.Name);
                string generatedId = roi.ID;
                m_RoisById.Remove(generatedId);
                roi.ID = value.RoiId.Value;
                RegisterRoi(roi);
            }
            else if (!StringComparer.Ordinal.Equals(roi.Name, value.Name))
            {
                roi.Name = value.Name;
            }

            var desired = value.Spheres.ToDictionary(sphere => sphere.SphereId.Value, StringComparer.Ordinal);
            for (int i = roi.Spheres.Count - 1; i >= 0; i--)
                if (!desired.ContainsKey(roi.Spheres[i].ID))
                    roi.RemoveSphere(i);

            for (int i = 0; i < value.Spheres.Count; i++)
            {
                V2RoiSphereDefinition definition = value.Spheres[i];
                int currentIndex = roi.Spheres.FindIndex(sphere => StringComparer.Ordinal.Equals(sphere.ID, definition.SphereId.Value));
                if (currentIndex < 0)
                {
                    AddSphere(roi, definition, roi.Spheres.Count);
                    currentIndex = roi.Spheres.Count - 1;
                }
                else
                {
                    RoiSphere existing = roi.Spheres[currentIndex];
                    existing.Position = new Vector3(definition.X, definition.Y, definition.Z);
                    existing.SetInfluenceRadius(definition.InfluenceRadius);
                    if (currentIndex != i)
                    {
                        roi.Spheres.RemoveAt(currentIndex);
                        roi.Spheres.Insert(i, existing);
                    }
                }
            }

            MoveRoiToOrder(roi, value.Order);
            m_RoiStateSnapshots[roi] = RoiStateSnapshot.Capture(roi, value.Order);
        }

        private void ReconcileCheckpointRosters(IReadOnlyList<V2T10CheckpointRecord> records)
        {
            SetCutOrder cutOrder = records.Select(record => record.Value).OfType<SetCutOrder>().SingleOrDefault();
            if (cutOrder != null)
            {
                var desiredCuts = new HashSet<CutId>(cutOrder.CutIds);
                foreach (SceneCut cut in m_Scene.Cuts.Where(cut => !desiredCuts.Contains(m_CutIds[cut])).ToArray())
                    m_Scene.RemoveCutPlane(cut);
            }

            bool hasRoiRoster = records.Any(record => record.Value is SetActiveRoi);
            if (!hasRoiRoster || m_Scene.ROIManager == null) return;
            var desiredRois = new HashSet<string>(records.Select(record => record.Value).OfType<CreateRoi>().Select(roi => roi.RoiId.Value), StringComparer.Ordinal);
            foreach (ROI roi in m_Scene.ROIManager.ROIs.Where(roi => !desiredRois.Contains(roi.ID)).ToArray())
                m_Scene.ROIManager.RemoveROI(roi);
        }

        private static void ValidateCheckpointT09Records(IReadOnlyList<V2T09CheckpointRecord> records, IReadOnlyList<V2T10CheckpointRecord> t10Records)
        {
            var stagedRois = t10Records.Select(record => record.Value).OfType<CreateRoi>().ToDictionary(roi => roi.RoiId.Value, StringComparer.Ordinal);
            bool hasRoiRoster = t10Records.Any(record => record.Value is SetActiveRoi);
            foreach (V2T09CheckpointRecord record in records)
            {
                if (record.Value is SetSelectedRoiSphere selection && (hasRoiRoster || stagedRois.ContainsKey(selection.RoiId)))
                {
                    if (!stagedRois.TryGetValue(selection.RoiId, out CreateRoi roi))
                        throw new KeyNotFoundException("Checkpoint selected sphere references an ROI outside the authoritative ROI roster.");
                    if (selection.SphereId.Length > 0 && !roi.Spheres.Any(sphere => StringComparer.Ordinal.Equals(sphere.SphereId.Value, selection.SphereId)))
                        throw new KeyNotFoundException("Checkpoint selected sphere is absent from its staged ROI.");
                    continue;
                }

                // T09 validation is completed by the bound boundary after it has checked staged T10 identities.
                if (record?.Value == null) throw new ArgumentException("Checkpoint contains an empty T09 record.", nameof(records));
            }
        }

        public void ApplyCheckpoint(V2SceneMutationCheckpoint checkpoint, OperationId operationId)
        {
            ApplyCheckpointCore(checkpoint, operationId, null, restoreOptimisticProvenance: false, updateProvenance: true);
        }

        private void ApplyCheckpointRollback(V2SceneMutationCheckpoint checkpoint, OperationId operationId, SiteConfigurationProvenanceSnapshot provenance)
        {
            ApplyCheckpointCore(checkpoint, operationId, provenance, restoreOptimisticProvenance: true, updateProvenance: true);
        }

        private void ApplyCheckpointWithoutChangingProvenance(V2SceneMutationCheckpoint checkpoint, OperationId operationId)
        {
            ApplyCheckpointCore(checkpoint, operationId, null, restoreOptimisticProvenance: false, updateProvenance: false);
        }

        private void ApplyCheckpointCore(V2SceneMutationCheckpoint checkpoint, OperationId operationId, SiteConfigurationProvenanceSnapshot provenance, bool restoreOptimisticProvenance, bool updateProvenance)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));

            var siteKeys = new HashSet<(ColumnId, SiteId)>();
            foreach (SiteColorCheckpointRecord record in checkpoint.SiteColors)
            {
                SetSiteColor value = record.Value;
                ResolveSite(value.ColumnId, value.FullSiteId);
                if (!siteKeys.Add((value.ColumnId, value.FullSiteId))) throw new ArgumentException("Checkpoint contains a duplicate site-color key.", nameof(checkpoint));
            }

            var cutKeys = new HashSet<CutId>();
            var stagedCuts = new HashSet<CutId>(checkpoint.T10Records.Select(record => record.Value).OfType<CreateCut>().Select(cut => cut.CutId));
            bool hasCutRoster = checkpoint.T10Records.Any(record => record.Value is SetCutOrder);
            foreach (CutDefinitionCheckpointRecord record in checkpoint.CutDefinitions)
            {
                if (hasCutRoster && !stagedCuts.Contains(record.Value.CutId))
                    throw new KeyNotFoundException("Checkpoint cut definition is outside the authoritative cut roster.");
                if (!stagedCuts.Contains(record.Value.CutId)) ResolveCut(record.Value.CutId);
                if (!cutKeys.Add(record.Value.CutId)) throw new ArgumentException("Checkpoint contains a duplicate cut key.", nameof(checkpoint));
            }

            var timelineKeys = new HashSet<ColumnId>();
            foreach (TimelineAnchorCheckpointRecord record in checkpoint.TimelineAnchors)
            {
                (BasicTimeline timeline, _) = ResolveTimeline(record.Value.ColumnId);
                if (record.Value.Index >= timeline.Length) throw new ArgumentOutOfRangeException(nameof(checkpoint), "Checkpoint timeline index exceeds the prepared timeline.");
                if (!timelineKeys.Add(record.Value.ColumnId)) throw new ArgumentException("Checkpoint contains a duplicate timeline key.", nameof(checkpoint));
            }

            ValidateCheckpointT10Records(checkpoint.T10Records);
            ValidateCheckpointT11Records(checkpoint.T11Records);
            ValidateSiteFilterCheckpoint(checkpoint.T12Records);
            ValidateCorrelationCheckpoint(checkpoint.T13Records);
            ValidateCheckpointT09Records(checkpoint.T09Records, checkpoint.T10Records);
            var stagedRoiIds = new HashSet<string>(checkpoint.T10Records.Select(record => record.Value).OfType<CreateRoi>().Select(roi => roi.RoiId.Value), StringComparer.Ordinal);
            bool hasRoiRoster = checkpoint.T10Records.Any(record => record.Value is SetActiveRoi);
            foreach (V2T09CheckpointRecord record in checkpoint.T09Records)
            {
                if (record?.Value == null) throw new ArgumentException("Checkpoint contains an empty T09 record.", nameof(checkpoint));
                if (record.Value is SetSelectedRoiSphere selectedSphere && (hasRoiRoster || stagedRoiIds.Contains(selectedSphere.RoiId)))
                    continue;
                ValidateMutation(record.Value);
            }

            using (V2MutationApplicationContext.EnterRemote(operationId))
            {
                ReconcileCheckpointRosters(checkpoint.T10Records);
                ApplyCheckpointT10Records(checkpoint.T10Records);
                foreach (SiteColorCheckpointRecord record in checkpoint.SiteColors) ApplyCore(record.Value);
                foreach (CutDefinitionCheckpointRecord record in checkpoint.CutDefinitions) ApplyCore(record.Value);
                foreach (TimelineAnchorCheckpointRecord record in checkpoint.TimelineAnchors) ApplyCore(record.Value);
                ApplyCheckpointT11Records(checkpoint.T11Records);
                foreach (V2T09CheckpointRecord record in checkpoint.T09Records) ApplyCore(record.Value);
                if (checkpoint.T12Records.Count == 1) ApplySiteFilterCheckpoint(checkpoint.T12Records[0]);
                if (checkpoint.T13Records.Count == 1)
                {
                    V2CorrelationCheckpointRecord correlationRecord = checkpoint.T13Records[0];
                    if (correlationRecord.HasResult)
                        CorrelationResultResource.Decode(correlationRecord.ResultBytes).Apply(m_Scene);
                    else if (m_Scene != null)
                        m_Scene.ResetCorrelations();
                    if (m_Scene != null) m_Scene.DisplayCorrelations = correlationRecord.DisplayCorrelations;
                }
            }

            if (updateProvenance)
            {
                m_SiteConfigurationFieldOwners.Clear();
                if (restoreOptimisticProvenance)
                    RestoreSiteConfigurationProvenance(provenance);
                else
                    MarkCheckpointSiteConfigurationWriters(checkpoint, operationId.Value);
            }
        }

        private bool ApplyCore(V2Mutation mutation)
        {
            if (mutation is SetSiteFilterResult filterResult)
                return ApplySiteFilterResult(filterResult);

            if (mutation is SetCorrelationResult correlationResult)
                return ApplyCorrelationResult(correlationResult);

            if (mutation is SetSiteColor siteColor)
            {
                SiteState state = ResolveSite(siteColor.ColumnId, siteColor.FullSiteId);
                Color color = new Color(siteColor.Red, siteColor.Green, siteColor.Blue, siteColor.Alpha);
                if (state.Color == color) return false;
                state.Color = color;
                return true;
            }

            if (mutation is SetCutDefinition cutDefinition)
            {
                SceneCut cut = ResolveCut(cutDefinition.CutId);
                Vector3 normal = new Vector3(cutDefinition.NormalX, cutDefinition.NormalY, cutDefinition.NormalZ);
                if (cut.Orientation == (CutOrientation)cutDefinition.Orientation && cut.Flip == cutDefinition.Flip && cut.NumberOfCuts == cutDefinition.NumberOfCuts && cut.Position == cutDefinition.Position && cut.Normal == normal)
                    return false;
                cut.Orientation = (CutOrientation)cutDefinition.Orientation;
                cut.Flip = cutDefinition.Flip;
                cut.NumberOfCuts = checked((int)cutDefinition.NumberOfCuts);
                cut.Normal = normal;
                cut.Position = cutDefinition.Position;
                m_UpdateCut?.Invoke(cut);
                return true;
            }

            if (mutation is SetTimelineAnchor timelineAnchor)
            {
                (BasicTimeline timeline, _) = ResolveTimeline(timelineAnchor.ColumnId);
                if (timelineAnchor.Index >= timeline.Length) throw new ArgumentOutOfRangeException(nameof(mutation), "Timeline index exceeds the prepared timeline.");
                ApplyTimelineAnchor(timeline, timelineAnchor);
                return true;
            }

            if ((ushort)mutation.Type >= (ushort)V2OperationType.SetSiteBlacklist)
                return ApplyT11Mutation(mutation);

            if ((ushort)mutation.Type >= (ushort)V2OperationType.CreateCut)
                return ApplyT10Mutation(mutation);

            if ((ushort)mutation.Type >= (ushort)V2OperationType.SetSelectedColumn)
                return ApplyT09Mutation(mutation);

            throw new ArgumentException("Unsupported v2 scene mutation.", nameof(mutation));
        }

        private void BindSceneTargets(Base3DScene scene)
        {
            scene.ConfigurationMutationStarted += BeginConfigurationMutation;
            scene.ConfigurationMutationCompleted += CompleteConfigurationMutation;
            scene.SiteConfigurationBatchRouter = ApplySiteConfigurationBatch;
            foreach (Column3D column in scene.Columns)
            {
                string id = column.ColumnData.ID;
                if (!m_ColumnsById.TryAdd(id, column)) throw new ArgumentException("Prepared columns must have unique stable identities.", nameof(scene));
                var columnId = new ColumnId(id);
                m_LastSelectedSites.Add(column, ReadCurrentT09Mutation(new SetSelectedSite(columnId, null)));
                m_LastActivityAlphas.Add(column, new SetActivityAlpha(columnId, column.ActivityAlpha));

                UnityAction<Core.Object3D.Site> selectionListener = site => OnColumnSiteSelectionChanged(column, site);
                column.OnSelectSite.AddListener(selectionListener);
                m_ColumnSelectionListeners.Add(column, selectionListener);
                var listeners = new List<(UnityEvent, UnityAction)>();
                AddColumnListener(column.OnUpdateActivityAlpha, () => ObserveColumnActivityAlpha(column), listeners);
                if (column is Column3DStatic staticColumn)
                {
                    m_LastColumnSpans.Add(column, CreateStaticSpan(staticColumn));
                    AddColumnListener(staticColumn.StaticParameters.OnUpdateSpanValues, () => ObserveColumnSpan(staticColumn), listeners);
                    m_LastInfluenceDistances.Add(column, CreateInfluenceDistance(column));
                    AddColumnListener(staticColumn.StaticParameters.OnUpdateInfluenceDistance, () => ObserveInfluenceDistance(column), listeners);
                }
                else if (column is Column3DDynamic dynamicColumn)
                {
                    m_LastColumnSpans.Add(column, CreateDynamicSpan(dynamicColumn));
                    AddColumnListener(dynamicColumn.DynamicParameters.OnUpdateSpanValues, () => ObserveColumnSpan(dynamicColumn), listeners);
                    m_LastInfluenceDistances.Add(column, CreateInfluenceDistance(column));
                    AddColumnListener(dynamicColumn.DynamicParameters.OnUpdateInfluenceDistance, () => ObserveInfluenceDistance(column), listeners);
                }
                else if (column is Column3DAnatomy anatomyColumn)
                {
                    m_LastInfluenceDistances.Add(column, CreateInfluenceDistance(column));
                    AddColumnListener(anatomyColumn.AnatomyParameters.OnUpdateInfluenceDistance, () => ObserveInfluenceDistance(column), listeners);
                }

                if (column is Column3DFMRI fmriColumn)
                {
                    m_LastFunctionalDisplays.Add(column, CreateFunctionalDisplay(fmriColumn));
                    AddColumnListener(fmriColumn.FMRIParameters.OnUpdateCalValues, () => ObserveFunctionalDisplay(fmriColumn), listeners);
                    AddColumnListener(fmriColumn.FMRIParameters.OnUpdateHideValues, () => ObserveFunctionalDisplay(fmriColumn), listeners);
                }
                else if (column is Column3DMEG megColumn)
                {
                    m_LastFunctionalDisplays.Add(column, CreateFunctionalDisplay(megColumn));
                    AddColumnListener(megColumn.MEGParameters.OnUpdateCalValues, () => ObserveFunctionalDisplay(megColumn), listeners);
                    AddColumnListener(megColumn.MEGParameters.OnUpdateHideValues, () => ObserveFunctionalDisplay(megColumn), listeners);
                }

                if (column is Column3DCCEP ccepColumn)
                {
                    m_LastCcepSources.Add(column, CreateCcepSource(ccepColumn));
                    AddColumnListener(ccepColumn.OnSelectSource, () => ObserveCcepSource(ccepColumn), listeners);
                }

                BindColumnResourceObservers(column, listeners);
                m_ColumnListeners.Add(column, listeners);
            }

            if (scene.ROIManager != null)
                foreach (ROI roi in scene.ROIManager.ROIs)
                    if (!m_RoisById.TryAdd(roi.ID, roi))
                        throw new ArgumentException("Prepared ROIs must have unique stable identities.", nameof(scene));

            foreach (ROI roi in scene.ROIManager != null ? scene.ROIManager.ROIs : Enumerable.Empty<ROI>())
            {
                m_LastRoiSpheres.Add(roi, new SetSelectedRoiSphere(roi.ID, roi.SelectedSphere ? roi.SelectedSphere.ID : string.Empty));
                UnityAction listener = () => OnRoiSphereSelectionChanged(roi);
                roi.OnChangeSphereSelectionState.AddListener(listener);
                m_RoiSelectionListeners.Add(roi, listener);
            }

            m_LastSelectedColumn = new SetSelectedColumn(scene.SelectedColumn ? new ColumnId(scene.SelectedColumn.ColumnData.ID) : null);
            m_LastSceneAutomaticCuts = new SetSceneBoolean(V2SceneBooleanProperty.AutomaticCutAroundSelectedSite, scene.AutomaticCutAroundSelectedSite);
            m_LastActiveRoiId = scene.ROIManager?.SelectedROI?.ID;
            m_LastCutOrder = scene.Cuts.Select(cut => new CutId(cut.ID)).ToArray();
            scene.OnAddCut.AddListener(OnCutAdded);
            scene.OnRemoveCut.AddListener(OnCutRemoved);
            m_CutOrderListener = ObserveCutOrder;
            scene.OnModifyPlanesCuts.AddListener(m_CutOrderListener);
            scene.OnChangeAutomaticCutAroundSelectedSite.AddListener(OnAutomaticCutPolicyChanged);
            scene.SitePositionCommandExecuted += OnSitePositionCommandExecuted;
            scene.OnSharedStateChanged.AddListener(ObserveScenePresentation);
            if (scene.BrainMaterials != null) scene.BrainMaterials.AlphaChanged += OnBrainAlphaChanged;
            if (scene.FMRIManager != null) scene.FMRIManager.PresentationChanged += ObserveFmriPresentation;
            Module3DMain.OnSelectColumn.AddListener(OnModuleColumnSelected);
            scene.OnUpdateROI.AddListener(RefreshRoiTargets);
            if (scene.ROIManager != null)
            {
                scene.ROIManager.RoiAdded += OnRoiAdded;
                scene.ROIManager.RoiRemoved += OnRoiRemoved;
                scene.ROIManager.ActiveRoiChanged += OnActiveRoiChanged;
            }

            RefreshRoiTargets();
            ObserveScenePresentation();
            if (scene.FMRIManager != null) ObserveFmriPresentation();
        }

        private void BeginConfigurationMutation()
        {
            if (m_ConfigurationMutationCapture != null)
                throw new InvalidOperationException("A configuration mutation is already being captured by this scene boundary.");
            m_ConfigurationMutationCapture = new ConfigurationMutationCapture(CaptureCheckpoint(), CaptureConfigurationRoster(), CapturePendingSiteConfigurationProvenanceSnapshot());
        }

        private void CompleteConfigurationMutation(Exception actionError)
        {
            ConfigurationMutationCapture capture = m_ConfigurationMutationCapture;
            if (capture == null) return;
            m_ConfigurationMutationCapture = null;

            if (actionError != null)
            {
                ApplyCheckpointRollback(capture.InitialCheckpoint, new OperationId(Guid.NewGuid()), capture.InitialSiteConfigurationProvenance);
                return;
            }

            try
            {
                SetConfigurationTransaction transaction = BuildConfigurationTransaction(capture);
                if (transaction == null) return;
                ValidateConfigurationTransaction(transaction, capture.InitialRoster);

                OperationId operationId = new(Guid.NewGuid());
                MarkSiteConfigurationWriters(transaction, operationId.Value);
                if (m_LocalOrigin == V2OriginDevice.Quest)
                    RememberOptimisticCheckpointRollback(operationId, capture.InitialCheckpoint, capture.InitialSiteConfigurationProvenance);
                try
                {
                    MutationProposed?.Invoke(operationId, transaction, m_LocalOrigin);
                }
                catch
                {
                    ForgetOptimisticOperation(operationId);
                    throw;
                }
            }
            catch (Exception failure)
            {
                try
                {
                    ApplyCheckpointRollback(capture.InitialCheckpoint, new OperationId(Guid.NewGuid()), capture.InitialSiteConfigurationProvenance);
                }
                catch (Exception rollbackFailure)
                {
                    throw new AggregateException("Configuration transaction failed and restoring its previous scene state also failed.", failure, rollbackFailure);
                }

                throw;
            }
        }

        private bool ApplySiteConfigurationBatch(IReadOnlyList<SiteConfigurationChange> changes, Action apply)
        {
            if (changes == null) throw new ArgumentNullException(nameof(changes));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            SiteConfigurationChange[] previous = changes.Select(change => new SiteConfigurationChange(change.Column, change.SiteId, change.State, new Core.Data.SiteConfiguration(change.State.IsBlackListed, change.State.IsHighlighted, change.State.Color, change.State.Labels))).ToArray();
            var assignments = new List<V2SiteConfigurationAssignment>();
            foreach (SiteConfigurationChange change in changes)
            {
                if (change.Column?.ColumnData?.ID == null)
                    throw new InvalidOperationException("Site configuration column has no stable prepared identity.");
                var key = (new ColumnId(change.Column.ColumnData.ID), new SiteId(change.SiteId));
                if (!m_SitesById.TryGetValue(key, out SiteState preparedState)) continue;
                if (!ReferenceEquals(preparedState, change.State))
                    throw new InvalidOperationException("Site configuration target does not reference the prepared site state.");
                Core.Data.SiteConfiguration configuration = change.Configuration;
                if (preparedState.IsBlackListed == configuration.IsBlacklisted && preparedState.IsHighlighted == configuration.IsHighlighted && preparedState.Color == configuration.Color && preparedState.Labels.SequenceEqual(configuration.Labels)) continue;
                assignments.Add(new V2SiteConfigurationAssignment(key.Item1, key.Item2, configuration.IsBlacklisted, configuration.IsHighlighted, configuration.Color.r, configuration.Color.g, configuration.Color.b, configuration.Color.a, configuration.Labels));
            }

            if (assignments.Count == 0)
            {
                OperationId localOperationId = new(Guid.NewGuid());
                try
                {
                    using (V2MutationApplicationContext.EnterLocalApply(m_LocalOrigin, localOperationId)) apply();
                }
                catch (Exception failure)
                {
                    try
                    {
                        RestoreSiteConfigurationChanges(previous, localOperationId);
                    }
                    catch (Exception rollbackFailure)
                    {
                        throw new AggregateException("Site configuration import failed and restoring its previous values also failed.", failure, rollbackFailure);
                    }

                    throw;
                }

                return false;
            }

            var mutation = new SetSiteConfigurationBatch(assignments);
            ValidateMutation(mutation);
            V2Mutation rollback = ReadCurrentMutation(mutation);
            SiteConfigurationProvenanceSnapshot provenanceRollback = CaptureSiteConfigurationProvenanceSnapshot(mutation);
            OperationId operationId = new(Guid.NewGuid());
            try
            {
                using (V2MutationApplicationContext.EnterLocalApply(m_LocalOrigin, operationId)) apply();
            }
            catch (Exception failure)
            {
                try
                {
                    RestoreSiteConfigurationChanges(previous, operationId);
                }
                catch (Exception rollbackFailure)
                {
                    throw new AggregateException("Site configuration batch failed and restoring its previous values also failed.", failure, rollbackFailure);
                }

                throw;
            }

            MarkSiteConfigurationWriters(mutation, operationId.Value);
            if (m_LocalOrigin == V2OriginDevice.Quest) RememberOptimisticRollback(operationId, rollback, mutation, provenanceRollback);
            try
            {
                MutationProposed?.Invoke(operationId, mutation, m_LocalOrigin);
            }
            catch (Exception failure)
            {
                ForgetOptimisticOperation(operationId);
                try
                {
                    RestoreSiteConfigurationChanges(previous, operationId);
                }
                catch (Exception rollbackFailure)
                {
                    throw new AggregateException("Site configuration publication failed and restoring its previous values also failed.", failure, rollbackFailure);
                }

                RestoreSiteConfigurationProvenance(provenanceRollback);

                throw;
            }

            return true;
        }

        private void RestoreSiteConfigurationChanges(IEnumerable<SiteConfigurationChange> previous, OperationId operationId)
        {
            using (V2MutationApplicationContext.EnterRemote(operationId))
                m_Scene.ApplySiteStateBatch(() =>
                {
                    foreach (SiteConfigurationChange change in previous)
                    {
                        change.State.ApplyState(change.Configuration.IsBlacklisted, change.Configuration.IsHighlighted, change.Configuration.Color, change.Configuration.Labels);
                        change.Column.SiteStateBySiteID[change.SiteId] = change.State;
                    }
                });
        }

        private void RecordConfigurationSiteBefore(SiteState state, SitePresentationSnapshot previous)
        {
            ConfigurationMutationCapture capture = m_ConfigurationMutationCapture;
            if (capture == null || !m_Sites.TryGetValue(state, out List<SiteTarget> targets)) return;
            foreach (SiteTarget target in targets)
            {
                var key = (target.ColumnId.Value, target.SiteId.Value);
                capture.SiteBefore.TryAdd(key, new V2SiteConfigurationAssignment(target.ColumnId, target.SiteId, previous.Blacklisted, previous.Highlighted, previous.Color.r, previous.Color.g, previous.Color.b, previous.Color.a, previous.Labels));
            }
        }

        private SetConfigurationTransaction BuildConfigurationTransaction(ConfigurationMutationCapture capture)
        {
            var children = new List<ConfigurationMutationChild>();
            var coalescedIndexes = new Dictionary<V2TouchedKey, int>();
            SceneId validationScene = new(Guid.Parse("70000000-0000-0000-0000-000000000001"));
            IncarnationId validationIncarnation = new(Guid.Parse("70000000-0000-0000-0000-000000000002"));
            foreach ((V2Mutation mutation, V2Mutation rollback) in capture.Children)
            {
                if (IsSiteConfigurationMutation(mutation)) continue;
                if (mutation is MoveSites or SetTimelineAnchor)
                    throw new InvalidOperationException("Configuration transactions only support reversible configuration mutations.");

                V2MutationDescriptor descriptor = V2MutationDescriptor.Create(validationScene, validationIncarnation, mutation);
                if (IsCoalescibleConfigurationMutation(mutation) && descriptor.TouchedKeys.Count == 1)
                {
                    V2TouchedKey key = descriptor.TouchedKeys[0];
                    if (coalescedIndexes.TryGetValue(key, out int previousIndex))
                    {
                        ConfigurationMutationChild previous = children[previousIndex];
                        children[previousIndex] = null;
                        coalescedIndexes[key] = children.Count;
                        children.Add(new ConfigurationMutationChild(mutation, previous.Rollback));
                    }
                    else
                    {
                        coalescedIndexes.Add(key, children.Count);
                        children.Add(new ConfigurationMutationChild(mutation, rollback));
                    }
                }
                else
                {
                    children.Add(new ConfigurationMutationChild(mutation, rollback));
                }
            }

            SetSiteConfigurationBatch currentSiteBatch = CreateChangedConfigurationBatch(capture.SiteBefore, out V2Mutation previousSiteBatch);
            if (currentSiteBatch != null)
                children.Add(new ConfigurationMutationChild(currentSiteBatch, previousSiteBatch));

            ConfigurationMutationChild[] prepared = children.Where(child => child != null).ToArray();
            if (prepared.Length == 0) return null;
            if (prepared.Length > 64)
                throw new InvalidOperationException("Scene configuration exceeds the 64-child atomic transaction bound.");
            return new SetConfigurationTransaction(prepared.Select(child => child.Mutation));
        }

        private SetSiteConfigurationBatch CreateChangedConfigurationBatch(IReadOnlyDictionary<(string ColumnId, string SiteId), V2SiteConfigurationAssignment> previous, out V2Mutation rollback)
        {
            rollback = null;
            if (previous.Count == 0) return null;
            var current = new List<V2SiteConfigurationAssignment>(previous.Count);
            var old = new List<V2SiteConfigurationAssignment>(previous.Count);
            foreach (KeyValuePair<(string ColumnId, string SiteId), V2SiteConfigurationAssignment> entry in previous)
            {
                var key = (new ColumnId(entry.Key.ColumnId), new SiteId(entry.Key.SiteId));
                SiteState state = ResolveSite(key.Item1, key.Item2);
                V2SiteConfigurationAssignment before = entry.Value;
                var after = new V2SiteConfigurationAssignment(key.Item1, key.Item2, state.IsBlackListed, state.IsHighlighted, state.Color.r, state.Color.g, state.Color.b, state.Color.a, state.Labels);
                if (SiteConfigurationAssignmentsEqual(before, after)) continue;
                old.Add(before);
                current.Add(after);
            }

            if (current.Count == 0) return null;
            rollback = new SetSiteConfigurationBatch(old);
            return new SetSiteConfigurationBatch(current);
        }

        private static bool SiteConfigurationAssignmentsEqual(V2SiteConfigurationAssignment left, V2SiteConfigurationAssignment right) => left.Blacklisted == right.Blacklisted && left.Highlighted == right.Highlighted && left.Red == right.Red && left.Green == right.Green && left.Blue == right.Blue && left.Alpha == right.Alpha && left.Labels.SequenceEqual(right.Labels);

        private static bool IsSiteConfigurationMutation(V2Mutation mutation) => mutation is SetSiteBlacklist or SetSiteColor or SetSiteHighlight or SetSiteLabels or SetSiteConfigurationBatch;

        private static bool IsCoalescibleConfigurationMutation(V2Mutation mutation) => mutation is SetSiteBlacklist or SetInfluenceDistance or SetColumnResource or SetCcepSource or SetSceneBoolean or SetSceneFloat or SetSceneColor or SetSelectedColumn or SetSelectedSite or SetActivityAlpha or SetColumnSpan or SetFunctionalDisplay or SetIbcDifumoDisplay or SetLocalizerDisplay or SetFmriAtlasCalibration or SetSelectedRoiSphere or SetCutDefinition or SetCutOrder or RenameRoi or SetActiveRoi or SetRoiSphereDefinition or SetMeshDisplay or SetSelectedMri or SetMriCalibration or SetImplantation or ApplyTriangleMask;

        private StagedConfigurationRoster CaptureConfigurationRoster()
        {
            var cuts = m_Scene.Cuts.Select(cut => m_CutIds[cut]).ToList();
            var rois = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var roiOrder = new List<string>();
            if (m_Scene.ROIManager != null)
                foreach (ROI roi in m_Scene.ROIManager.ROIs)
                {
                    rois.Add(roi.ID, roi.Spheres.Select(sphere => sphere.ID).ToHashSet(StringComparer.Ordinal));
                    roiOrder.Add(roi.ID);
                }

            return new StagedConfigurationRoster(cuts, rois, roiOrder, m_Scene.ROIManager?.SelectedROI?.ID);
        }

        private static void AddColumnListener(UnityEvent unityEvent, UnityAction listener, List<(UnityEvent Event, UnityAction Listener)> listeners)
        {
            unityEvent.AddListener(listener);
            listeners.Add((unityEvent, listener));
        }

        private void BindColumnResourceObservers(Column3D column, List<(UnityEvent Event, UnityAction Listener)> listeners)
        {
            if (m_ResourceCatalog == null || m_LastColumnResources.ContainsKey(column)) return;
            if (column is Column3DStatic staticColumn)
            {
                m_LastColumnResources.Add(column, CreateColumnResource(column, V2ColumnResourceKind.StaticLabel));
                AddColumnListener(staticColumn.OnUpdateSelectedLabel, () => ObserveColumnResource(staticColumn, V2ColumnResourceKind.StaticLabel), listeners);
            }
            else if (column is Column3DFMRI fmriColumn)
            {
                m_LastColumnResources.Add(column, CreateColumnResource(column, V2ColumnResourceKind.FmriResource));
                AddColumnListener(fmriColumn.OnChangeSelectedFMRI, () => ObserveColumnResource(fmriColumn, V2ColumnResourceKind.FmriResource), listeners);
            }
            else if (column is Column3DMEG megColumn)
            {
                m_LastColumnResources.Add(column, CreateColumnResource(column, V2ColumnResourceKind.MegResource));
                AddColumnListener(megColumn.OnChangeSelectedMEG, () => ObserveColumnResource(megColumn, V2ColumnResourceKind.MegResource), listeners);
            }
        }

        private List<V2T09CheckpointRecord> CaptureT09Records()
        {
            var records = new List<V2T09CheckpointRecord>();
            void Add(V2Mutation mutation) => records.Add(V2T09CheckpointRecord.FromMutation(mutation));
            Add(new SetSelectedColumn(m_Scene.SelectedColumn ? new ColumnId(m_Scene.SelectedColumn.ColumnData.ID) : null));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.AutomaticCutAroundSelectedSite, m_Scene.AutomaticCutAroundSelectedSite));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, m_Scene.StrongCuts));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.HideBlacklistedSites, m_Scene.HideBlacklistedSites));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, m_Scene.ShowAllSites));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.EdgeMode, m_Scene.EdgeMode));
            Add(new SetSceneFloat(V2SceneFloatProperty.SiteGain, m_Scene.SiteGain));
            if (m_Scene.BrainMaterials != null)
            {
                Add(new SetSceneBoolean(V2SceneBooleanProperty.BrainTransparent, m_Scene.IsBrainTransparent));
                Add(new SetSceneFloat(V2SceneFloatProperty.BrainAlpha, m_Scene.BrainMaterials.Alpha));
                Add(new SetSceneColor(V2SceneColorProperty.Brain, (int)m_Scene.BrainColor));
                Add(new SetSceneColor(V2SceneColorProperty.Cut, (int)m_Scene.CutColor));
                Add(new SetSceneColor(V2SceneColorProperty.Colormap, (int)m_Scene.Colormap));
            }

            if (m_Scene.AtlasManager != null)
            {
                Add(new SetSceneBoolean(V2SceneBooleanProperty.DisplayMarsAtlas, m_Scene.AtlasManager.DisplayMarsAtlas));
                Add(new SetSceneBoolean(V2SceneBooleanProperty.DisplayJuBrainAtlas, m_Scene.AtlasManager.DisplayJuBrainAtlas));
                Add(new SetSceneFloat(V2SceneFloatProperty.AtlasAlpha, m_Scene.AtlasManager.AtlasAlpha));
            }

            foreach (Column3D column in m_Scene.Columns)
            {
                var columnId = new ColumnId(column.ColumnData.ID);
                Add(new SetSelectedSite(columnId, column.SelectedSite ? new SiteId(column.SelectedSite.Information.FullID) : null));
                Add(new SetActivityAlpha(columnId, column.ActivityAlpha));
                if (column is Column3DStatic staticColumn) Add(CreateStaticSpan(staticColumn));
                if (column is Column3DDynamic dynamicColumn) Add(CreateDynamicSpan(dynamicColumn));
                if (column is Column3DFMRI fmriColumn) Add(CreateFunctionalDisplay(fmriColumn));
                if (column is Column3DMEG megColumn) Add(CreateFunctionalDisplay(megColumn));
            }

            if (m_Scene.FMRIManager != null)
            {
                Add(CreateIbcDifumoDisplay());
                Add(CreateLocalizerDisplay());
                Add(CreateFmriAtlasCalibration());
            }

            if (m_Scene.ROIManager != null)
                foreach (ROI roi in m_Scene.ROIManager.ROIs)
                    Add(new SetSelectedRoiSphere(roi.ID, roi.SelectedSphere ? roi.SelectedSphere.ID : string.Empty));
            return records;
        }

        private List<V2T11CheckpointRecord> CaptureT11Records()
        {
            var records = new List<V2T11CheckpointRecord>();
            void Add(V2Mutation mutation) => records.Add(new V2T11CheckpointRecord(mutation));

            if (m_Scene != null)
            {
                foreach (Column3D column in m_Scene.Columns)
                {
                    if (m_LastInfluenceDistances.ContainsKey(column)) Add(CreateInfluenceDistance(column));
                    if (m_LastColumnResources.TryGetValue(column, out V2Mutation resource)) Add(resource is SetColumnResource selected ? CreateColumnResource(column, selected.Kind) : resource);
                    if (column is Column3DCCEP ccepColumn) Add(CreateCcepSource(ccepColumn));
                }
            }

            if (m_SitesById.Count > 0)
                Add(CreateSiteConfigurationBatch(m_SitesById.Keys.Select(key => (key.ColumnId, key.SiteId))));
            return records;
        }

        private void OnModuleColumnSelected(Column3D column)
        {
            if (!m_ColumnsById.TryGetValue(column?.ColumnData?.ID, out Column3D ownedColumn) || !ReferenceEquals(column, ownedColumn)) return;
            ObserveT09(ref m_LastSelectedColumn, new SetSelectedColumn(new ColumnId(column.ColumnData.ID)));
        }

        private void OnColumnSiteSelectionChanged(Column3D column, Core.Object3D.Site site)
        {
            if (m_Disposed || !m_ColumnsById.TryGetValue(column.ColumnData.ID, out Column3D ownedColumn) || !ReferenceEquals(column, ownedColumn)) return;
            ObserveT09(m_LastSelectedSites, column, new SetSelectedSite(new ColumnId(column.ColumnData.ID), site ? new SiteId(site.Information.FullID) : null));
        }

        private void OnBoundSiteStateChanged(SiteState state)
        {
            if (!m_Sites.TryGetValue(state, out List<SiteTarget> targets)) return;
            SitePresentationSnapshot previous = m_SitePresentationStates[state];
            RecordConfigurationSiteBefore(state, previous);
            SitePresentationSnapshot current = SitePresentationSnapshot.Capture(state);
            m_SitePresentationStates[state] = current;
            if (ShouldSuppressPublication()) return;
            foreach (SiteTarget target in targets)
            {
                if (state.CurrentChangeKind == SiteStateChangeKind.ScientificMask && previous.Blacklisted != current.Blacklisted)
                    Publish(new SetSiteBlacklist(target.ColumnId, target.SiteId, current.Blacklisted));
                if (state.CurrentChangeKind == SiteStateChangeKind.Presentation && previous.Highlighted != current.Highlighted)
                    Publish(new SetSiteHighlight(target.ColumnId, target.SiteId, current.Highlighted));
                if (state.CurrentChangeKind == SiteStateChangeKind.Presentation && !previous.Labels.SequenceEqual(current.Labels))
                {
                    try
                    {
                        Publish(new SetSiteLabels(target.ColumnId, target.SiteId, current.Labels));
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
        }

        private void OnBrainAlphaChanged(float alpha)
        {
            ObserveT09(ref m_LastSceneBrainAlpha, new SetSceneFloat(V2SceneFloatProperty.BrainAlpha, alpha));
        }

        private void ObserveScenePresentation()
        {
            if (m_Scene == null || m_Disposed) return;
            ObserveT09(ref m_LastSceneStrongCuts, new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, m_Scene.StrongCuts));
            ObserveT09(ref m_LastSceneAutomaticCuts, new SetSceneBoolean(V2SceneBooleanProperty.AutomaticCutAroundSelectedSite, m_Scene.AutomaticCutAroundSelectedSite));
            ObserveT09(ref m_LastSceneHideBlacklisted, new SetSceneBoolean(V2SceneBooleanProperty.HideBlacklistedSites, m_Scene.HideBlacklistedSites));
            ObserveT09(ref m_LastSceneShowAllSites, new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, m_Scene.ShowAllSites));
            ObserveT09(ref m_LastSceneSiteGain, new SetSceneFloat(V2SceneFloatProperty.SiteGain, m_Scene.SiteGain));
            ObserveMriCalibration();
            ObserveT09(ref m_LastSceneEdgeMode, new SetSceneBoolean(V2SceneBooleanProperty.EdgeMode, m_Scene.EdgeMode));
            if (m_Scene.BrainMaterials != null)
            {
                ObserveT09(ref m_LastSceneBrainTransparent, new SetSceneBoolean(V2SceneBooleanProperty.BrainTransparent, m_Scene.IsBrainTransparent));
                ObserveT09(ref m_LastSceneBrainAlpha, new SetSceneFloat(V2SceneFloatProperty.BrainAlpha, m_Scene.BrainMaterials.Alpha));
                ObserveT09(ref m_LastSceneBrainColor, new SetSceneColor(V2SceneColorProperty.Brain, (int)m_Scene.BrainColor));
                ObserveT09(ref m_LastSceneCutColor, new SetSceneColor(V2SceneColorProperty.Cut, (int)m_Scene.CutColor));
                ObserveT09(ref m_LastSceneColormap, new SetSceneColor(V2SceneColorProperty.Colormap, (int)m_Scene.Colormap));
            }

            if (m_Scene.AtlasManager != null)
            {
                ObserveT09(ref m_LastSceneAtlasAlpha, new SetSceneFloat(V2SceneFloatProperty.AtlasAlpha, m_Scene.AtlasManager.AtlasAlpha));
                ObserveT09(ref m_LastSceneMarsAtlas, new SetSceneBoolean(V2SceneBooleanProperty.DisplayMarsAtlas, m_Scene.AtlasManager.DisplayMarsAtlas));
                ObserveT09(ref m_LastSceneJuBrainAtlas, new SetSceneBoolean(V2SceneBooleanProperty.DisplayJuBrainAtlas, m_Scene.AtlasManager.DisplayJuBrainAtlas));
            }
        }

        private void ObserveFmriPresentation()
        {
            if (m_Scene == null || m_Scene.FMRIManager == null || m_Disposed) return;
            ObserveT09(ref m_LastSceneIbcDifumo, CreateIbcDifumoDisplay());
            ObserveT09(ref m_LastSceneLocalizer, CreateLocalizerDisplay());
            ObserveT09(ref m_LastSceneFmriCalibration, CreateFmriAtlasCalibration());
        }

        private void ObserveColumnActivityAlpha(Column3D column)
        {
            ObserveT09(m_LastActivityAlphas, column, new SetActivityAlpha(new ColumnId(column.ColumnData.ID), column.ActivityAlpha));
        }

        private void ObserveColumnSpan(Column3D column)
        {
            ObserveT09(m_LastColumnSpans, column, column is Column3DStatic staticColumn ? CreateStaticSpan(staticColumn) : CreateDynamicSpan((Column3DDynamic)column));
        }

        private void ObserveFunctionalDisplay(Column3D column)
        {
            ObserveT09(m_LastFunctionalDisplays, column, column is Column3DFMRI fmriColumn ? CreateFunctionalDisplay(fmriColumn) : CreateFunctionalDisplay((Column3DMEG)column));
        }

        private void ObserveInfluenceDistance(Column3D column) => ObserveT11(m_LastInfluenceDistances, column, CreateInfluenceDistance(column));

        private void ObserveColumnResource(Column3D column, V2ColumnResourceKind kind) => ObserveT11(m_LastColumnResources, column, CreateColumnResource(column, kind));

        private void ObserveCcepSource(Column3DCCEP column) => ObserveT11(m_LastCcepSources, column, CreateCcepSource(column));

        private void OnRoiSphereSelectionChanged(ROI roi)
        {
            if (!m_RoisById.TryGetValue(roi.ID, out ROI ownedRoi) || !ReferenceEquals(ownedRoi, roi)) return;
            ObserveT09(m_LastRoiSpheres, roi, new SetSelectedRoiSphere(roi.ID, roi.SelectedSphere ? roi.SelectedSphere.ID : string.Empty));
        }

        private readonly Dictionary<ROI, V2Mutation> m_LastRoiSpheres = new();

        private void ObserveT09(ref V2Mutation previous, V2Mutation current)
        {
            if (previous == null)
            {
                previous = current;
                return;
            }

            if (V2MutationPayloadCodec.Encode(previous).SequenceEqual(V2MutationPayloadCodec.Encode(current))) return;
            V2Mutation rollback = previous;
            previous = current;
            if (!ShouldSuppressPublication()) Publish(current, rollback);
        }

        private void ObserveT09<TKey>(Dictionary<TKey, V2Mutation> previousByKey, TKey key, V2Mutation current)
        {
            if (!previousByKey.TryGetValue(key, out V2Mutation previous) || previous == null)
            {
                previousByKey[key] = current;
                return;
            }

            if (V2MutationPayloadCodec.Encode(previous).SequenceEqual(V2MutationPayloadCodec.Encode(current))) return;
            V2Mutation rollback = previous;
            previousByKey[key] = current;
            if (!ShouldSuppressPublication()) Publish(current, rollback);
        }

        private void ObserveT10(ref V2Mutation previous, V2Mutation current)
        {
            if (previous == null)
            {
                previous = current;
                return;
            }

            if (V2MutationPayloadCodec.Encode(previous).SequenceEqual(V2MutationPayloadCodec.Encode(current))) return;
            V2Mutation rollback = previous;
            previous = current;
            if (!ShouldSuppressPublication()) Publish(current, rollback);
        }

        private void ObserveT11<TKey>(Dictionary<TKey, V2Mutation> previousByKey, TKey key, V2Mutation current)
        {
            if (!previousByKey.TryGetValue(key, out V2Mutation previous) || previous == null)
            {
                previousByKey[key] = current;
                return;
            }

            if (V2MutationPayloadCodec.Encode(previous).SequenceEqual(V2MutationPayloadCodec.Encode(current))) return;
            previousByKey[key] = current;
            if (!ShouldSuppressPublication()) Publish(current, previous);
        }

        private void ObserveMeshSelection() => ObserveMeshDisplay();

        private void ObserveMeshDisplay()
        {
            if (m_Disposed || m_ResourceCatalog == null) return;
            MeshManager manager = m_Scene.MeshManager;
            if (manager == null || manager.Meshes == null || manager.SelectedMeshID < 0 || manager.SelectedMeshID >= manager.Meshes.Count) return;
            try
            {
                ObserveT10(ref m_LastMeshDisplay, CreateMeshDisplayMutation());
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is System.IO.InvalidDataException)
            {
            }
        }

        private void ObserveMriSelection()
        {
            if (m_Disposed || m_ResourceCatalog == null || m_Scene.MRIManager == null) return;
            try
            {
                ObserveT10(ref m_LastSelectedMri, CreateSelectedMriMutation());
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is System.IO.InvalidDataException)
            {
            }
        }

        private void ObserveMriCalibration()
        {
            if (m_Disposed || m_Scene.MRIManager == null) return;
            ObserveT10(ref m_LastMriCalibration, new SetMriCalibration(m_Scene.MRIManager.MRICalMinFactor, m_Scene.MRIManager.MRICalMaxFactor));
        }

        private void ObserveImplantationSelection()
        {
            if (m_Disposed || m_ResourceCatalog == null || m_Scene.ImplantationManager?.SelectedImplantation == null) return;
            try
            {
                ObserveT10(ref m_LastImplantation, CreateImplantationMutation());
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is System.IO.InvalidDataException)
            {
            }
        }

        private void ObserveTriangleMask()
        {
            if (m_Disposed || m_ResourceCatalog == null || m_Scene.TriangleEraser == null) return;
            try
            {
                ObserveT10(ref m_LastTriangleMask, CreateTriangleMaskMutation());
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is System.IO.InvalidDataException)
            {
            }
        }

        private void RefreshRoiTargets()
        {
            if (m_Scene == null || m_Scene.ROIManager == null || m_Disposed) return;
            ROI[] current = m_Scene.ROIManager.ROIs.ToArray();
            var currentSet = new HashSet<ROI>(current);
            if (!m_RoiObserverInitialized)
            {
                foreach (ROI roi in current)
                {
                    RegisterRoi(roi);
                    m_RoiStateSnapshots[roi] = RoiStateSnapshot.Capture(roi, m_Scene.ROIManager.ROIs.IndexOf(roi));
                }

                m_LastActiveRoiId = m_Scene.ROIManager.SelectedROI ? m_Scene.ROIManager.SelectedROI.ID : null;
                m_RoiObserverInitialized = true;
                return;
            }

            foreach (ROI stale in m_RoiStateSnapshots.Keys.Where(roi => !currentSet.Contains(roi)).ToArray())
            {
                RoiStateSnapshot previous = m_RoiStateSnapshots[stale];
                if (!ShouldSuppressPublication()) Publish(new DeleteRoi(new RoiId(previous.RoiId)), CreateRoiMutation(previous));
                UnregisterRoi(stale);
            }

            foreach (ROI roi in current)
            {
                RegisterRoi(roi);
                RoiStateSnapshot next = RoiStateSnapshot.Capture(roi, m_Scene.ROIManager.ROIs.IndexOf(roi));
                if (!m_RoiStateSnapshots.TryGetValue(roi, out RoiStateSnapshot previous))
                {
                    if (!ShouldSuppressPublication()) Publish(CreateRoiMutation(roi), new DeleteRoi(new RoiId(roi.ID)));
                    m_RoiStateSnapshots[roi] = next;
                    continue;
                }

                if (!StringComparer.Ordinal.Equals(previous.RoiId, next.RoiId))
                {
                    if (!ShouldSuppressPublication())
                    {
                        Publish(new DeleteRoi(new RoiId(previous.RoiId)));
                        Publish(CreateRoiMutation(roi));
                    }
                }
                else
                {
                    if (!StringComparer.Ordinal.Equals(previous.Name, next.Name) && !ShouldSuppressPublication())
                        Publish(new RenameRoi(new RoiId(next.RoiId), next.Name));

                    foreach (string oldSphereId in previous.Spheres.Keys.Except(next.Spheres.Keys, StringComparer.Ordinal))
                        if (!ShouldSuppressPublication())
                            Publish(new DeleteRoiSphere(new RoiId(next.RoiId), new SphereId(oldSphereId)));
                    foreach (KeyValuePair<string, V2RoiSphereDefinition> sphere in next.Spheres)
                    {
                        if (!previous.Spheres.TryGetValue(sphere.Key, out V2RoiSphereDefinition oldSphere))
                        {
                            if (!ShouldSuppressPublication()) Publish(new CreateRoiSphere(new RoiId(next.RoiId), sphere.Value, roi.Spheres.FindIndex(item => item.ID == sphere.Key)));
                        }
                        else if (!SphereDefinitionEquals(oldSphere, sphere.Value) && !ShouldSuppressPublication())
                            Publish(new SetRoiSphereDefinition(new RoiId(next.RoiId), sphere.Value));
                    }
                }

                m_RoiStateSnapshots[roi] = next;
            }

            string activeRoiId = m_Scene.ROIManager.SelectedROI ? m_Scene.ROIManager.SelectedROI.ID : null;
            if (!StringComparer.Ordinal.Equals(m_LastActiveRoiId, activeRoiId) && !ShouldSuppressPublication())
                Publish(new SetActiveRoi(activeRoiId == null ? null : new RoiId(activeRoiId)), new SetActiveRoi(m_LastActiveRoiId == null ? null : new RoiId(m_LastActiveRoiId)));
            m_LastActiveRoiId = activeRoiId;
        }

        private void OnRoiAdded(ROI _) => RefreshRoiTargets();

        private void OnRoiRemoved(ROI _) => RefreshRoiTargets();

        private void OnActiveRoiChanged(ROI roi)
        {
            if (roi && !m_RoiStateSnapshots.ContainsKey(roi)) RefreshRoiTargets();
            string activeRoiId = roi ? roi.ID : null;
            if (StringComparer.Ordinal.Equals(m_LastActiveRoiId, activeRoiId)) return;
            string previousActiveRoiId = m_LastActiveRoiId;
            m_LastActiveRoiId = activeRoiId;
            if (!ShouldSuppressPublication())
                Publish(new SetActiveRoi(activeRoiId == null ? null : new RoiId(activeRoiId)), new SetActiveRoi(previousActiveRoiId == null ? null : new RoiId(previousActiveRoiId)));
        }

        private static bool SphereDefinitionEquals(V2RoiSphereDefinition left, V2RoiSphereDefinition right) => left.SphereId.Equals(right.SphereId) && left.X == right.X && left.Y == right.Y && left.Z == right.Z && left.InfluenceRadius == right.InfluenceRadius;

        private void OnCutAdded(SceneCut cut)
        {
            if (m_Disposed || cut == null) return;
            RegisterCut(cut);
            m_CutDefinitionSnapshots[cut] = CreateCutDefinition(cut, m_CutIds[cut]);
            if (!ShouldSuppressPublication() && !m_Scene.AutomaticCutAroundSelectedSite)
                Publish(new CreateCut(m_CutIds[cut], m_CutDefinitionSnapshots[cut], cut.Index), new DeleteCut(m_CutIds[cut]));
            m_LastCutOrder = m_Scene.Cuts.Select(item => new CutId(item.ID)).ToArray();
        }

        private void OnCutRemoved(SceneCut cut)
        {
            if (m_Disposed || cut == null || !m_CutIds.TryGetValue(cut, out CutId id)) return;
            int previousIndex = Array.FindIndex(m_LastCutOrder, candidate => candidate.Equals(id));
            V2Mutation rollback = m_CutDefinitionSnapshots.TryGetValue(cut, out SetCutDefinition definition) && previousIndex >= 0 ? new CreateCut(id, definition, previousIndex) : null;
            if (!ShouldSuppressPublication() && !m_Scene.AutomaticCutAroundSelectedSite) Publish(new DeleteCut(id), rollback);
            m_CutIds.Remove(cut);
            m_Cuts.Remove(id);
            m_CutDefinitionSnapshots.Remove(cut);
            m_LastCutOrder = m_Scene.Cuts.Select(item => new CutId(item.ID)).ToArray();
        }

        private void RegisterCut(SceneCut cut)
        {
            CutId id = new(cut.ID);
            if (m_CutIds.TryGetValue(cut, out CutId existingId))
            {
                if (existingId.Equals(id)) return;
                m_Cuts.Remove(existingId);
                m_CutIds.Remove(cut);
            }

            if (!m_Cuts.TryAdd(id, cut) || !m_CutIds.TryAdd(cut, id))
                throw new InvalidOperationException("Cut identities must remain unique in the prepared scene.");
            m_CutDefinitionSnapshots[cut] = CreateCutDefinition(cut, id);
        }

        private void ObserveCutOrder()
        {
            if (m_Disposed || m_Scene == null) return;
            foreach (SceneCut cut in m_Scene.Cuts) RegisterCut(cut);
            CutId[] current = m_Scene.Cuts.Select(cut => new CutId(cut.ID)).ToArray();
            if (current.SequenceEqual(m_LastCutOrder)) return;
            CutId[] previous = m_LastCutOrder;
            m_LastCutOrder = current;
            if (!ShouldSuppressPublication() && !m_Scene.AutomaticCutAroundSelectedSite)
                Publish(new SetCutOrder(current), new SetCutOrder(previous));
        }

        private void OnAutomaticCutPolicyChanged(bool _) => ObserveT09(ref m_LastSceneAutomaticCuts, new SetSceneBoolean(V2SceneBooleanProperty.AutomaticCutAroundSelectedSite, m_Scene.AutomaticCutAroundSelectedSite));

        private void OnSitePositionCommandExecuted(SitePositionCommand command)
        {
            V2SiteMoveCommand mapped = command switch
            {
                SitePositionCommand.MoveLeft => V2SiteMoveCommand.Left,
                SitePositionCommand.MoveRight => V2SiteMoveCommand.Right,
                SitePositionCommand.Reset => V2SiteMoveCommand.Reset,
                _ => throw new ArgumentOutOfRangeException(nameof(command))
            };
            if (!ShouldSuppressPublication()) Publish(new MoveSites(mapped));
        }

        private static SetColumnSpan CreateStaticSpan(Column3DStatic column) => new SetColumnSpan(new ColumnId(column.ColumnData.ID), V2ColumnSpanKind.Static, column.StaticParameters.SpanMin, column.StaticParameters.Middle, column.StaticParameters.SpanMax);
        private static SetColumnSpan CreateDynamicSpan(Column3DDynamic column) => new SetColumnSpan(new ColumnId(column.ColumnData.ID), V2ColumnSpanKind.Dynamic, column.DynamicParameters.SpanMin, column.DynamicParameters.Middle, column.DynamicParameters.SpanMax);
        private static SetFunctionalDisplay CreateFunctionalDisplay(Column3DFMRI column) => new SetFunctionalDisplay(new ColumnId(column.ColumnData.ID), V2FunctionalModality.Fmri, column.FMRIParameters.FMRINegativeCalMinFactor, column.FMRIParameters.FMRINegativeCalMaxFactor, column.FMRIParameters.FMRIPositiveCalMinFactor, column.FMRIParameters.FMRIPositiveCalMaxFactor, column.FMRIParameters.HideLowerValues, column.FMRIParameters.HideMiddleValues, column.FMRIParameters.HideHigherValues);
        private static SetFunctionalDisplay CreateFunctionalDisplay(Column3DMEG column) => new SetFunctionalDisplay(new ColumnId(column.ColumnData.ID), V2FunctionalModality.Meg, column.MEGParameters.FMRINegativeCalMinFactor, column.MEGParameters.FMRINegativeCalMaxFactor, column.MEGParameters.FMRIPositiveCalMinFactor, column.MEGParameters.FMRIPositiveCalMaxFactor, column.MEGParameters.HideLowerValues, column.MEGParameters.HideMiddleValues, column.MEGParameters.HideHigherValues);

        private sealed class SitePresentationSnapshot
        {
            public bool Blacklisted { get; }
            public bool Highlighted { get; }
            public Color Color { get; }
            public string[] Labels { get; }

            private SitePresentationSnapshot(bool blacklisted, bool highlighted, Color color, string[] labels)
            {
                Blacklisted = blacklisted;
                Highlighted = highlighted;
                Color = color;
                Labels = labels;
            }

            public static SitePresentationSnapshot Capture(SiteState state) => new SitePresentationSnapshot(state.IsBlackListed, state.IsHighlighted, state.Color, state.Labels.ToArray());
        }

        private V2Mutation ReadCurrentT09Mutation(V2Mutation key)
        {
            RequireScene();
            return key switch
            {
                SetSelectedColumn => new SetSelectedColumn(m_Scene.SelectedColumn ? new ColumnId(m_Scene.SelectedColumn.ColumnData.ID) : null),
                SetSelectedSite selected => CurrentSelectedSite(selected.ColumnId),
                SetSceneBoolean value => new SetSceneBoolean(value.Property, ReadSceneBoolean(value.Property)),
                SetSceneFloat value => new SetSceneFloat(value.Property, ReadSceneFloat(value.Property)),
                SetSceneColor value => new SetSceneColor(value.Property, ReadSceneColor(value.Property)),
                SetSiteHighlight value => new SetSiteHighlight(value.ColumnId, value.SiteId, ResolveSite(value.ColumnId, value.SiteId).IsHighlighted),
                SetSiteLabels value => new SetSiteLabels(value.ColumnId, value.SiteId, ResolveSite(value.ColumnId, value.SiteId).Labels),
                SetActivityAlpha value => new SetActivityAlpha(value.ColumnId, ResolveColumn(value.ColumnId).ActivityAlpha),
                SetColumnSpan value => value.Kind == V2ColumnSpanKind.Static ? CreateStaticSpan(ResolveStaticColumn(value.ColumnId)) : CreateDynamicSpan(ResolveDynamicColumn(value.ColumnId)),
                SetFunctionalDisplay value => value.Modality == V2FunctionalModality.Fmri ? CreateFunctionalDisplay(ResolveFmriColumn(value.ColumnId)) : CreateFunctionalDisplay(ResolveMegColumn(value.ColumnId)),
                SetIbcDifumoDisplay => CreateIbcDifumoDisplay(),
                SetLocalizerDisplay => CreateLocalizerDisplay(),
                SetFmriAtlasCalibration => CreateFmriAtlasCalibration(),
                SetSelectedRoiSphere value => CurrentSelectedRoiSphere(ResolveRoi(value.RoiId)),
                _ => throw new ArgumentException("Unsupported T09 scene mutation.", nameof(key))
            };
        }

        private V2Mutation ReadCurrentT11Mutation(V2Mutation key)
        {
            return key switch
            {
                SetSiteBlacklist value => new SetSiteBlacklist(value.ColumnId, value.SiteId, ResolveSite(value.ColumnId, value.SiteId).IsBlackListed),
                SetInfluenceDistance value => CreateInfluenceDistance(RequireSceneAndResolveColumn(value.ColumnId)),
                SetColumnResource value => CreateColumnResource(RequireSceneAndResolveColumn(value.ColumnId), value.Kind),
                SetCcepSource value => CreateCcepSource(RequireSceneAndResolveCcepColumn(value.ColumnId)),
                SetSiteConfigurationBatch value => CreateSiteConfigurationBatch(value.Assignments.Select(assignment => (assignment.ColumnId, assignment.SiteId))),
                SetConfigurationTransaction value => new SetConfigurationTransaction(RequireSceneAndReadCurrentMutations(value.Mutations)),
                _ => throw new ArgumentException("Unsupported T11 scene mutation.", nameof(key))
            };
        }

        private Column3D RequireSceneAndResolveColumn(ColumnId columnId)
        {
            RequireScene();
            return ResolveColumn(columnId);
        }

        private Column3DCCEP RequireSceneAndResolveCcepColumn(ColumnId columnId)
        {
            RequireScene();
            return ResolveCcepColumn(columnId);
        }

        private IEnumerable<V2Mutation> RequireSceneAndReadCurrentMutations(IEnumerable<V2Mutation> mutations)
        {
            RequireScene();
            return mutations.Select(ReadCurrentMutation);
        }

        private V2Mutation ReadCurrentT10Mutation(V2Mutation key)
        {
            RequireScene();
            return key switch
            {
                CreateCut value => m_Cuts.TryGetValue(value.CutId, out SceneCut cut) ? new CreateCut(value.CutId, CreateCutDefinition(cut, value.CutId), m_Scene.Cuts.IndexOf(cut)) : new DeleteCut(value.CutId),
                DeleteCut value => m_Cuts.TryGetValue(value.CutId, out SceneCut cut) ? new CreateCut(value.CutId, CreateCutDefinition(cut, value.CutId), m_Scene.Cuts.IndexOf(cut)) : new DeleteCut(value.CutId),
                SetCutOrder => new SetCutOrder(m_Scene.Cuts.Select(cut => new CutId(cut.ID))),
                CreateRoi value => m_RoisById.TryGetValue(value.RoiId.Value, out ROI roi) ? CreateRoiMutation(roi) : new DeleteRoi(value.RoiId),
                RenameRoi value => new RenameRoi(value.RoiId, ResolveRoi(value.RoiId.Value).Name),
                DeleteRoi value => m_RoisById.TryGetValue(value.RoiId.Value, out ROI roi) ? CreateRoiMutation(roi, CaptureRoiSelectionSnapshot(roi)) : new DeleteRoi(value.RoiId),
                SetActiveRoi => new SetActiveRoi(m_Scene.ROIManager.SelectedROI ? new RoiId(m_Scene.ROIManager.SelectedROI.ID) : null),
                CreateRoiSphere value => CurrentSphereMutation(value.RoiId, value.Definition.SphereId),
                DeleteRoiSphere value => CurrentSphereCreationMutation(value.RoiId, value.SphereId),
                SetRoiSphereDefinition value => CurrentSphereMutation(value.RoiId, value.Definition.SphereId),
                MoveSites value => value,
                SetMeshDisplay => CreateMeshDisplayMutation(),
                SetSelectedMri => CreateSelectedMriMutation(),
                SetMriCalibration => new SetMriCalibration(m_Scene.MRIManager.MRICalMinFactor, m_Scene.MRIManager.MRICalMaxFactor),
                SetImplantation => CreateImplantationMutation(),
                ApplyTriangleMask => CreateTriangleMaskMutation(),
                _ => throw new ArgumentException("Unsupported T10 scene mutation.", nameof(key))
            };
        }

        private void ValidateT11Mutation(V2Mutation mutation)
        {
            switch (mutation)
            {
                case SetSiteBlacklist value:
                    ResolveSite(value.ColumnId, value.SiteId);
                    break;
                case SetInfluenceDistance value:
                    RequireScene();
                    ResolveInfluenceColumn(value.ColumnId);
                    break;
                case SetColumnResource value:
                    RequireScene();
                    ValidateColumnResource(value);
                    break;
                case SetCcepSource value:
                    RequireScene();
                    ValidateCcepSource(value);
                    break;
                case SetSiteConfigurationBatch value:
                    foreach (V2SiteConfigurationAssignment assignment in value.Assignments)
                        ResolveSite(assignment.ColumnId, assignment.SiteId);
                    break;
                case SetConfigurationTransaction value:
                    RequireScene();
                    ValidateConfigurationTransaction(value, CaptureConfigurationRoster());
                    break;
                default:
                    throw new ArgumentException("Unsupported T11 scene mutation.", nameof(mutation));
            }
        }

        private void ValidateConfigurationTransaction(SetConfigurationTransaction transaction, StagedConfigurationRoster initialRoster)
        {
            RequireScene();
            StagedConfigurationRoster staged = initialRoster.Clone();
            var touched = new HashSet<V2TouchedKey>();
            var validationScene = new SceneId(Guid.Parse("70000000-0000-0000-0000-000000000001"));
            var validationIncarnation = new IncarnationId(Guid.Parse("70000000-0000-0000-0000-000000000002"));
            foreach (V2Mutation child in transaction.Mutations)
            {
                ValidateConfigurationChild(child, staged);
                foreach (V2TouchedKey key in V2MutationDescriptor.Create(validationScene, validationIncarnation, child).TouchedKeys)
                    touched.Add(key);
            }

            if (touched.Count > 128)
                throw new ArgumentOutOfRangeException(nameof(transaction), "Configuration transaction exceeds the touched-key bound.");
        }

        private void ValidateConfigurationChild(V2Mutation child, StagedConfigurationRoster staged)
        {
            switch (child)
            {
                case MoveSites:
                case SetTimelineAnchor:
                    throw new InvalidOperationException("Configuration transactions only support reversible configuration mutations.");
                case CreateCut value:
                    if (staged.Cuts.Contains(value.CutId) || value.Order > staged.Cuts.Count)
                        throw new InvalidOperationException("Cut identity or insertion order is not valid in the staged configuration.");
                    if (!value.CutId.Equals(value.Definition.CutId) || value.Definition.NumberOfCuts > int.MaxValue)
                        throw new InvalidOperationException("Staged cut definition does not match its identity or supported range.");
                    staged.Cuts.Insert(value.Order, value.CutId);
                    return;
                case DeleteCut value:
                    if (!staged.Cuts.Remove(value.CutId))
                        throw new KeyNotFoundException("Staged cut deletion references an absent cut.");
                    return;
                case SetCutOrder value:
                    if (value.CutIds.Count != staged.Cuts.Count || value.CutIds.Count != value.CutIds.Distinct().Count() || value.CutIds.Any(id => !staged.Cuts.Contains(id)))
                        throw new InvalidOperationException("Staged cut order must be a complete permutation of the current cut identities.");
                    staged.Cuts.Clear();
                    staged.Cuts.AddRange(value.CutIds);
                    return;
                case SetCutDefinition value:
                    if (!staged.Cuts.Contains(value.CutId))
                        throw new KeyNotFoundException("Staged cut definition references an absent cut.");
                    if (value.NumberOfCuts > int.MaxValue)
                        throw new ArgumentOutOfRangeException(nameof(child), "Cut count exceeds the prepared scene's supported range.");
                    return;
                case CreateRoi value:
                    RequireRoiManager();
                    if (staged.Rois.ContainsKey(value.RoiId.Value) || value.Order > staged.RoiOrder.Count)
                        throw new InvalidOperationException("ROI identity or insertion order is not valid in the staged configuration.");
                    var sphereIds = value.Spheres.Select(sphere => sphere.SphereId.Value).ToHashSet(StringComparer.Ordinal);
                    if (sphereIds.Count != value.Spheres.Count || sphereIds.Any(id => staged.Rois.Values.Any(existing => existing.Contains(id))))
                        throw new InvalidOperationException("Staged ROI sphere identities must be globally unique.");
                    ValidateRoiSelectionSnapshot(value.SelectionSnapshot, value.RoiId, value.Spheres.Select(sphere => sphere.SphereId));
                    staged.Rois.Add(value.RoiId.Value, sphereIds);
                    staged.RoiOrder.Insert(value.Order, value.RoiId.Value);
                    return;
                case RenameRoi value:
                    RequireStagedRoi(staged, value.RoiId.Value);
                    return;
                case DeleteRoi value:
                    RequireStagedRoi(staged, value.RoiId.Value);
                    staged.Rois.Remove(value.RoiId.Value);
                    staged.RoiOrder.Remove(value.RoiId.Value);
                    if (StringComparer.Ordinal.Equals(staged.ActiveRoiId, value.RoiId.Value)) staged.ActiveRoiId = null;
                    return;
                case SetActiveRoi value:
                    RequireRoiManager();
                    if (value.RoiId != null) RequireStagedRoi(staged, value.RoiId.Value);
                    staged.ActiveRoiId = value.RoiId?.Value;
                    return;
                case CreateRoiSphere value:
                    HashSet<string> createSphereOwner = RequireStagedRoi(staged, value.RoiId.Value);
                    if (value.Order > createSphereOwner.Count || staged.Rois.Values.Any(roi => roi.Contains(value.Definition.SphereId.Value)))
                        throw new InvalidOperationException("ROI sphere identity or insertion order is not valid in the staged configuration.");
                    createSphereOwner.Add(value.Definition.SphereId.Value);
                    ValidateRoiSelectionSnapshot(value.SelectionSnapshot, value.RoiId, createSphereOwner.Select(id => new SphereId(id)));
                    return;
                case DeleteRoiSphere value:
                    if (!RequireStagedRoi(staged, value.RoiId.Value).Remove(value.SphereId.Value))
                        throw new KeyNotFoundException("Staged ROI sphere deletion references an absent sphere.");
                    return;
                case SetRoiSphereDefinition value:
                    if (!RequireStagedRoi(staged, value.RoiId.Value).Contains(value.Definition.SphereId.Value))
                        throw new KeyNotFoundException("Staged ROI sphere definition references an absent sphere.");
                    return;
                case SetSelectedRoiSphere value:
                    if (!staged.Rois.TryGetValue(value.RoiId, out HashSet<string> selectedSphereRoster) || value.SphereId.Length > 0 && !selectedSphereRoster.Contains(value.SphereId))
                        throw new KeyNotFoundException("Selected ROI sphere is absent from the staged ROI roster.");
                    return;
                default:
                    ValidateMutation(child);
                    return;
            }
        }

        private static HashSet<string> RequireStagedRoi(StagedConfigurationRoster staged, string roiId)
        {
            if (staged.Rois.TryGetValue(roiId, out HashSet<string> spheres)) return spheres;
            throw new KeyNotFoundException("Staged ROI operation references an absent ROI.");
        }

        private void ValidateColumnResource(SetColumnResource value)
        {
            RequireResourceCatalog();
            Column3D column = value.Kind switch
            {
                V2ColumnResourceKind.StaticLabel => ResolveStaticColumn(value.ColumnId),
                V2ColumnResourceKind.FmriResource => ResolveFmriColumn(value.ColumnId),
                V2ColumnResourceKind.MegResource => ResolveMegColumn(value.ColumnId),
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };
            int index = m_ResourceCatalog.ResolveColumnIndex(column, value.ResourceReference);
            if (index < 0 && value.Kind != V2ColumnResourceKind.StaticLabel)
                throw new InvalidOperationException("Functional column resource selection cannot be empty.");
        }

        private void ValidateCcepSource(SetCcepSource value)
        {
            Column3DCCEP column = ResolveCcepColumn(value.ColumnId);
            if (value.Mode == V2CcepSourceMode.Site)
            {
                if (value.SourceSiteId != null && (!column.Sources.Any(site => StringComparer.Ordinal.Equals(site.Information.FullID, value.SourceSiteId.Value)) || !column.ColumnCCEPData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.ContainsKey(value.SourceSiteId.Value)))
                    throw new KeyNotFoundException("CCEP source site is not available in the prepared column.");
                return;
            }

            if (value.MarsAtlasLabel < 0) return;
            Mesh3D selectedMesh = m_Scene.MeshManager?.SelectedMesh;
            bool implantationHasMarsAtlas = m_Scene.ImplantationManager?.SelectedImplantation?.SiteInfos.Any(info => info.SiteData?.Tags?.Any(tagValue => tagValue.Tag is StringTag tag && tag.Name == "MarsAtlas") == true) == true;
            if (selectedMesh == null || !selectedMesh.SupportsMarsAtlas || !Object3DManager.MarsAtlas.Loaded || !Object3DManager.MarsAtlas.Labels().Contains(value.MarsAtlasLabel) || !implantationHasMarsAtlas)
                throw new InvalidOperationException("CCEP Mars atlas source is not prepared for the selected mesh and implantation.");
        }

        private bool ApplyT11Mutation(V2Mutation mutation)
        {
            ValidateT11Mutation(mutation);
            switch (mutation)
            {
                case SetSiteBlacklist value:
                    {
                        SiteState state = ResolveSite(value.ColumnId, value.SiteId);
                        if (state.IsBlackListed == value.Blacklisted) return false;
                        state.IsBlackListed = value.Blacklisted;
                        return true;
                    }
                case SetInfluenceDistance value:
                    {
                        Column3D column = ResolveInfluenceColumn(value.ColumnId);
                        if (column is Column3DAnatomy anatomy)
                        {
                            if (anatomy.AnatomyParameters.InfluenceDistance == value.Distance) return false;
                            anatomy.AnatomyParameters.InfluenceDistance = value.Distance;
                        }
                        else if (column is Column3DStatic staticColumn)
                        {
                            if (staticColumn.StaticParameters.InfluenceDistance == value.Distance) return false;
                            staticColumn.StaticParameters.InfluenceDistance = value.Distance;
                        }
                        else
                        {
                            Column3DDynamic dynamicColumn = (Column3DDynamic)column;
                            if (dynamicColumn.DynamicParameters.InfluenceDistance == value.Distance) return false;
                            dynamicColumn.DynamicParameters.InfluenceDistance = value.Distance;
                        }

                        return true;
                    }
                case SetColumnResource value:
                    {
                        Column3D column = ResolveColumn(value.ColumnId);
                        if (StringComparer.Ordinal.Equals(m_ResourceCatalog.ColumnReference(column), value.ResourceReference)) return false;
                        int index = m_ResourceCatalog.ResolveColumnIndex(column, value.ResourceReference);
                        if (index >= 0)
                        {
                            if (column is Column3DStatic staticColumn) staticColumn.SelectedLabelIndex = index;
                            else if (column is Column3DFMRI fmriColumn) fmriColumn.SelectedFMRIIndex = index;
                            else ((Column3DMEG)column).SelectedMEGIndex = index;
                        }

                        return true;
                    }
                case SetCcepSource value:
                    {
                        Column3DCCEP column = ResolveCcepColumn(value.ColumnId);
                        Core.Object3D.Site source = value.SourceSiteId == null ? null : column.Sources.Single(site => StringComparer.Ordinal.Equals(site.Information.FullID, value.SourceSiteId.Value));
                        column.ApplySynchronizedSource((Column3DCCEP.CCEPMode)value.Mode, source, value.MarsAtlasLabel);
                        return true;
                    }
                case SetSiteConfigurationBatch value:
                    {
                        bool changed = false;
                        Action apply = () =>
                        {
                            foreach (V2SiteConfigurationAssignment assignment in value.Assignments)
                            {
                                SiteState state = ResolveSite(assignment.ColumnId, assignment.SiteId);
                                Color color = new Color(assignment.Red, assignment.Green, assignment.Blue, assignment.Alpha);
                                if (state.IsBlackListed == assignment.Blacklisted && state.IsHighlighted == assignment.Highlighted && state.Color == color && state.Labels.SequenceEqual(assignment.Labels)) continue;
                                changed = true;
                                state.ApplySynchronizedState(state.IsFiltered, assignment.Blacklisted, assignment.Highlighted, color, assignment.Labels);
                            }
                        };
                        if (m_Scene != null) m_Scene.ApplySiteStateBatch(apply);
                        else apply();
                        return changed;
                    }
                case SetConfigurationTransaction value:
                    {
                        V2SceneMutationCheckpoint rollback = CaptureCheckpoint();
                        bool changed = false;
                        try
                        {
                            foreach (V2Mutation child in value.Mutations) changed |= ApplyCore(child);
                        }
                        catch (Exception failure)
                        {
                            try
                            {
                                ApplyCheckpointWithoutChangingProvenance(rollback, new OperationId(Guid.NewGuid()));
                            }
                            catch (Exception rollbackFailure)
                            {
                                throw new AggregateException("Configuration transaction failed and restoring its previous scene state also failed.", failure, rollbackFailure);
                            }

                            throw;
                        }

                        return changed;
                    }
                default:
                    throw new ArgumentException("Unsupported T11 scene mutation.", nameof(mutation));
            }
        }

        private Column3D ResolveInfluenceColumn(ColumnId columnId) => RequireColumnType(columnId, column => column is Column3DAnatomy or Column3DStatic or Column3DDynamic);
        private Column3DCCEP ResolveCcepColumn(ColumnId columnId) => RequireColumnType(columnId, column => column is Column3DCCEP) as Column3DCCEP;

        private static SetInfluenceDistance CreateInfluenceDistance(Column3D column) =>
            new SetInfluenceDistance(new ColumnId(column.ColumnData.ID), column switch
            {
                Column3DAnatomy anatomy => anatomy.AnatomyParameters.InfluenceDistance,
                Column3DStatic staticColumn => staticColumn.StaticParameters.InfluenceDistance,
                Column3DDynamic dynamicColumn => dynamicColumn.DynamicParameters.InfluenceDistance,
                _ => throw new InvalidOperationException("Column does not support influence distance.")
            });

        private SetColumnResource CreateColumnResource(Column3D column, V2ColumnResourceKind kind)
        {
            RequireResourceCatalog();
            return new SetColumnResource(new ColumnId(column.ColumnData.ID), kind, m_ResourceCatalog.ColumnReference(column));
        }

        private static SetCcepSource CreateCcepSource(Column3DCCEP column) => new SetCcepSource(new ColumnId(column.ColumnData.ID), (V2CcepSourceMode)column.Mode, column.SelectedSourceSite == null ? null : new SiteId(column.SelectedSourceSite.Information.FullID), column.Mode == Column3DCCEP.CCEPMode.Site ? -1 : column.SelectedSourceMarsAtlasLabel);

        private SetSiteConfigurationBatch CreateSiteConfigurationBatch(IEnumerable<(ColumnId ColumnId, SiteId SiteId)> targets)
        {
            var assignments = new List<V2SiteConfigurationAssignment>();
            foreach ((ColumnId columnId, SiteId siteId) in targets)
            {
                SiteState state = ResolveSite(columnId, siteId);
                assignments.Add(new V2SiteConfigurationAssignment(columnId, siteId, state.IsBlackListed, state.IsHighlighted, state.Color.r, state.Color.g, state.Color.b, state.Color.a, state.Labels));
            }

            return new SetSiteConfigurationBatch(assignments);
        }

        private void ValidateT10Mutation(V2Mutation mutation)
        {
            RequireScene();
            switch (mutation)
            {
                case CreateCut value:
                    if (m_Cuts.ContainsKey(value.CutId) || value.Order > m_Scene.Cuts.Count) throw new InvalidOperationException("Cut identity or insertion order is not valid for the prepared scene.");
                    break;
                case DeleteCut value: ResolveCut(value.CutId); break;
                case SetCutOrder value:
                    if (value.CutIds.Count != m_Scene.Cuts.Count || value.CutIds.Any(id => !m_Cuts.ContainsKey(id)) || value.CutIds.Count != value.CutIds.Distinct().Count())
                        throw new InvalidOperationException("Cut order must be a complete permutation of the prepared cut identities.");
                    break;
                case CreateRoi value:
                    RequireRoiManager();
                    if (m_RoisById.ContainsKey(value.RoiId.Value) || value.Order > m_Scene.ROIManager.ROIs.Count) throw new InvalidOperationException("ROI identity or insertion order is not valid for the prepared scene.");
                    foreach (V2RoiSphereDefinition sphere in value.Spheres)
                        if (m_RoisById.Values.Any(roi => roi.Spheres.Any(existing => existing.ID == sphere.SphereId.Value)))
                            throw new InvalidOperationException("ROI sphere identity already exists in the prepared scene.");
                    ValidateRoiSelectionSnapshot(value.SelectionSnapshot, value.RoiId, value.Spheres.Select(sphere => sphere.SphereId));
                    break;
                case RenameRoi value: ResolveRoi(value.RoiId.Value); break;
                case DeleteRoi value: ResolveRoi(value.RoiId.Value); break;
                case SetActiveRoi value:
                    RequireRoiManager();
                    if (value.RoiId != null) ResolveRoi(value.RoiId.Value);
                    break;
                case CreateRoiSphere value:
                    ROI createOwner = ResolveRoi(value.RoiId.Value);
                    if (value.Order > createOwner.Spheres.Count || m_RoisById.Values.Any(roi => roi.Spheres.Any(sphere => sphere.ID == value.Definition.SphereId.Value))) throw new InvalidOperationException("ROI sphere identity or insertion order is not valid for the prepared ROI.");
                    ValidateRoiSelectionSnapshot(value.SelectionSnapshot, value.RoiId, createOwner.Spheres.Select(sphere => new SphereId(sphere.ID)).Append(value.Definition.SphereId));
                    break;
                case DeleteRoiSphere value: ResolveSphere(value.RoiId, value.SphereId); break;
                case SetRoiSphereDefinition value: ResolveSphere(value.RoiId, value.Definition.SphereId); break;
                case MoveSites: break;
                case SetMeshDisplay value:
                    RequireResourceCatalog();
                    Mesh3D mesh = m_ResourceCatalog.ResolveMesh(value.MeshId.Value);
                    if (value.Part != V2MeshPart.Both && !mesh.SupportsHemispheres) throw new InvalidOperationException("The prepared mesh does not support hemisphere selection.");
                    if (value.Representation == V2SurfaceRepresentation.Inflated && !mesh.HasInflatedRepresentation) throw new InvalidOperationException("The requested surface representation is not prepared.");
                    break;
                case SetSelectedMri value:
                    RequireResourceCatalog();
                    m_ResourceCatalog.ResolveMri(value.ResourceId.Value);
                    break;
                case SetMriCalibration:
                    if (m_Scene.MRIManager == null) throw new InvalidOperationException("MRI calibration is unavailable in the prepared scene.");
                    break;
                case SetImplantation value:
                    RequireResourceCatalog();
                    Implantation3D implantation = m_ResourceCatalog.ResolveImplantation(value.ResourceId.Value);
                    if (!StringComparer.Ordinal.Equals(ComputeMembershipHash(implantation), value.MembershipHash)) throw new InvalidOperationException("Implantation membership does not match the prepared resource.");
                    break;
                case ApplyTriangleMask value: ValidateTriangleMask(value); break;
                default: throw new ArgumentException("Unsupported T10 scene mutation.", nameof(mutation));
            }
        }

        private bool ApplyT10Mutation(V2Mutation mutation)
        {
            ValidateT10Mutation(mutation);
            switch (mutation)
            {
                case CreateCut value:
                    {
                        SceneCut cut = m_Scene.AddCutPlane(value.CutId.Value);
                        RegisterCut(cut);
                        ApplyCore(value.Definition);
                        MoveCutToOrder(value.CutId, value.Order);
                        return true;
                    }
                case DeleteCut value:
                    m_Scene.RemoveCutPlane(ResolveCut(value.CutId));
                    return true;
                case SetCutOrder value:
                    ApplyCutOrder(value);
                    return true;
                case CreateRoi value:
                    {
                        ROI roi = m_Scene.ROIManager.AddROI(value.Name);
                        string generatedId = roi.ID;
                        m_RoisById.Remove(generatedId);
                        roi.ID = value.RoiId.Value;
                        RegisterRoi(roi);
                        foreach (V2RoiSphereDefinition sphere in value.Spheres)
                            AddSphere(roi, sphere, roi.Spheres.Count);
                        MoveRoiToOrder(roi, value.Order);
                        m_RoiStateSnapshots[roi] = RoiStateSnapshot.Capture(roi, value.Order);
                        m_LastRoiSpheres[roi] = CurrentSelectedRoiSphere(roi);
                        m_LastActiveRoiId = m_Scene.ROIManager.SelectedROI ? m_Scene.ROIManager.SelectedROI.ID : null;
                        if (value.SelectionSnapshot != null) ApplyRoiSelectionSnapshot(roi, value.SelectionSnapshot);
                        return true;
                    }
                case RenameRoi value:
                    {
                        ROI roi = ResolveRoi(value.RoiId.Value);
                        if (roi.Name == value.Name) return false;
                        roi.Name = value.Name;
                        return true;
                    }
                case DeleteRoi value:
                    m_Scene.ROIManager.RemoveROI(ResolveRoi(value.RoiId.Value));
                    return true;
                case SetActiveRoi value:
                    {
                        ROI roi = value.RoiId == null ? null : ResolveRoi(value.RoiId.Value);
                        if (ReferenceEquals(m_Scene.ROIManager.SelectedROI, roi)) return false;
                        m_Scene.ROIManager.SelectedROI = roi;
                        return true;
                    }
                case CreateRoiSphere value:
                    {
                        ROI roi = ResolveRoi(value.RoiId.Value);
                        AddSphere(roi, value.Definition, value.Order);
                        if (value.SelectionSnapshot != null) ApplyRoiSelectionSnapshot(roi, value.SelectionSnapshot);
                    }
                    return true;
                case DeleteRoiSphere value:
                    {
                        (ROI roi, RoiSphere sphere) = ResolveSphere(value.RoiId, value.SphereId);
                        roi.RemoveSphere(roi.Spheres.IndexOf(sphere));
                        return true;
                    }
                case SetRoiSphereDefinition value:
                    {
                        (ROI roi, RoiSphere sphere) = ResolveSphere(value.RoiId, value.Definition.SphereId);
                        Vector3 position = new Vector3(value.Definition.X, value.Definition.Y, value.Definition.Z);
                        if (sphere.Position == position && sphere.InfluenceRadius == value.Definition.InfluenceRadius) return false;
                        sphere.Position = position;
                        sphere.SetInfluenceRadius(value.Definition.InfluenceRadius);
                        roi.OnChangeSphereParameters.Invoke();
                        return true;
                    }
                case MoveSites value:
                    if (value.Command == V2SiteMoveCommand.Left) m_Scene.MoveSitesToHemisphere(false);
                    else if (value.Command == V2SiteMoveCommand.Right) m_Scene.MoveSitesToHemisphere(true);
                    else m_Scene.ResetSitesPositions();
                    return true;
                case SetMeshDisplay value:
                    ApplyMeshDisplay(value);
                    return true;
                case SetSelectedMri value:
                    m_Scene.MRIManager.SelectPrepared(m_ResourceCatalog.ResolveMri(value.ResourceId.Value));
                    return true;
                case SetMriCalibration value:
                    m_Scene.MRIManager.SetCalValues(value.Minimum, value.Maximum);
                    return true;
                case SetImplantation value:
                    m_Scene.ImplantationManager.SelectPrepared(m_ResourceCatalog.ResolveImplantation(value.ResourceId.Value));
                    return true;
                case ApplyTriangleMask value:
                    m_Scene.TriangleEraser.CurrentMasks = value.Masks.Select(mask => mask.ToVisibilityMask()).ToList();
                    return true;
                default: throw new ArgumentException("Unsupported T10 scene mutation.", nameof(mutation));
            }
        }

        private CreateRoi CreateRoiMutation(ROI roi, V2RoiSelectionSnapshot selectionSnapshot = null) => new CreateRoi(new RoiId(roi.ID), roi.Name, roi.Spheres.Select((sphere, index) => CreateSphereDefinition(sphere)), m_Scene.ROIManager.ROIs.IndexOf(roi), selectionSnapshot);

        private static CreateRoi CreateRoiMutation(RoiStateSnapshot snapshot) => new CreateRoi(new RoiId(snapshot.RoiId), snapshot.Name, snapshot.SphereOrder, snapshot.Order);

        private static V2RoiSphereDefinition CreateSphereDefinition(RoiSphere sphere) => new V2RoiSphereDefinition(new SphereId(sphere.ID), sphere.Position.x, sphere.Position.y, sphere.Position.z, sphere.InfluenceRadius);

        private V2Mutation CurrentSphereMutation(RoiId roiId, SphereId sphereId)
        {
            try
            {
                (_, RoiSphere sphere) = ResolveSphere(roiId, sphereId);
                return new SetRoiSphereDefinition(roiId, CreateSphereDefinition(sphere));
            }
            catch (KeyNotFoundException)
            {
                return new DeleteRoiSphere(roiId, sphereId);
            }
        }

        private V2Mutation CurrentSphereCreationMutation(RoiId roiId, SphereId sphereId)
        {
            try
            {
                (ROI roi, RoiSphere sphere) = ResolveSphere(roiId, sphereId);
                return new CreateRoiSphere(roiId, CreateSphereDefinition(sphere), roi.Spheres.IndexOf(sphere), CaptureRoiSelectionSnapshot(roi));
            }
            catch (KeyNotFoundException)
            {
                return new DeleteRoiSphere(roiId, sphereId);
            }
        }

        private V2RoiSelectionSnapshot CaptureRoiSelectionSnapshot(ROI roi)
        {
            int selectedSphereIndex = roi.SelectedSphereID;
            SphereId selectedSphereId = selectedSphereIndex >= 0 && selectedSphereIndex < roi.Spheres.Count ? new SphereId(roi.Spheres[selectedSphereIndex].ID) : null;
            return new V2RoiSelectionSnapshot(m_Scene.ROIManager.SelectedROI ? new RoiId(m_Scene.ROIManager.SelectedROI.ID) : null, selectedSphereId);
        }

        private void ValidateRoiSelectionSnapshot(V2RoiSelectionSnapshot selectionSnapshot, RoiId restoredRoiId, IEnumerable<SphereId> restoredSphereIds)
        {
            if (selectionSnapshot == null) return;
            if (selectionSnapshot.ActiveRoiId != null && !selectionSnapshot.ActiveRoiId.Equals(restoredRoiId) && !m_RoisById.ContainsKey(selectionSnapshot.ActiveRoiId.Value))
                throw new InvalidOperationException("ROI selection snapshot refers to an unavailable active ROI.");
            if (selectionSnapshot.SelectedSphereId != null && !restoredSphereIds.Contains(selectionSnapshot.SelectedSphereId))
                throw new InvalidOperationException("ROI selection snapshot refers to an unavailable selected sphere.");
        }

        private void ApplyRoiSelectionSnapshot(ROI roi, V2RoiSelectionSnapshot selectionSnapshot)
        {
            ApplyT09Mutation(new SetSelectedRoiSphere(roi.ID, selectionSnapshot.SelectedSphereId?.Value ?? string.Empty));
            ApplyT10Mutation(new SetActiveRoi(selectionSnapshot.ActiveRoiId));
        }

        private void RequireRoiManager()
        {
            if (m_Scene.ROIManager == null) throw new InvalidOperationException("ROI editing is unavailable in the prepared scene.");
        }

        private void RegisterRoi(ROI roi)
        {
            foreach (string staleId in m_RoisById.Where(entry => ReferenceEquals(entry.Value, roi) && !StringComparer.Ordinal.Equals(entry.Key, roi.ID)).Select(entry => entry.Key).ToArray())
                m_RoisById.Remove(staleId);
            if (!m_RoisById.TryAdd(roi.ID, roi) && (!m_RoisById.TryGetValue(roi.ID, out ROI existing) || !ReferenceEquals(existing, roi)))
                throw new InvalidOperationException("ROI identities must remain unique in the prepared scene.");
            int selectedSphereIndex = roi.SelectedSphereID;
            string selectedSphereId = selectedSphereIndex >= 0 && selectedSphereIndex < roi.Spheres.Count ? roi.Spheres[selectedSphereIndex].ID : string.Empty;
            m_LastRoiSpheres[roi] = new SetSelectedRoiSphere(roi.ID, selectedSphereId);
            if (!m_RoiSelectionListeners.ContainsKey(roi))
            {
                UnityAction selectionListener = () => OnRoiSphereSelectionChanged(roi);
                roi.OnChangeSphereSelectionState.AddListener(selectionListener);
                m_RoiSelectionListeners.Add(roi, selectionListener);
            }

            if (!m_RoiStructureListeners.ContainsKey(roi))
            {
                UnityAction structureListener = () => RefreshRoiTargets();
                roi.OnUpdateROIName.AddListener(structureListener);
                roi.OnChangeNumberOfSpheres.AddListener(structureListener);
                roi.OnChangeSphereParameters.AddListener(structureListener);
                m_RoiStructureListeners.Add(roi, structureListener);
            }
        }

        private void UnregisterRoi(ROI roi)
        {
            if (m_RoiSelectionListeners.TryGetValue(roi, out UnityAction selectionListener)) roi.OnChangeSphereSelectionState.RemoveListener(selectionListener);
            if (m_RoiStructureListeners.TryGetValue(roi, out UnityAction structureListener))
            {
                roi.OnUpdateROIName.RemoveListener(structureListener);
                roi.OnChangeNumberOfSpheres.RemoveListener(structureListener);
                roi.OnChangeSphereParameters.RemoveListener(structureListener);
            }

            m_RoiSelectionListeners.Remove(roi);
            m_RoiStructureListeners.Remove(roi);
            m_LastRoiSpheres.Remove(roi);
            m_RoiStateSnapshots.Remove(roi);
            foreach (string id in m_RoisById.Where(entry => ReferenceEquals(entry.Value, roi)).Select(entry => entry.Key).ToArray())
                m_RoisById.Remove(id);
        }

        private void AddSphere(ROI roi, V2RoiSphereDefinition definition, int order)
        {
            roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere", new Vector3(definition.X, definition.Y, definition.Z), definition.InfluenceRadius);
            RoiSphere sphere = roi.Spheres[roi.Spheres.Count - 1];
            sphere.ID = definition.SphereId.Value;
            if (order != roi.Spheres.Count - 1)
            {
                roi.Spheres.RemoveAt(roi.Spheres.Count - 1);
                roi.Spheres.Insert(order, sphere);
                roi.SelectSphere(order);
            }

            sphere.Position = new Vector3(definition.X, definition.Y, definition.Z);
            sphere.SetInfluenceRadius(definition.InfluenceRadius);
            m_LastRoiSpheres[roi] = CurrentSelectedRoiSphere(roi);
            if (m_RoiStateSnapshots.ContainsKey(roi))
                m_RoiStateSnapshots[roi] = RoiStateSnapshot.Capture(roi, m_Scene.ROIManager.ROIs.IndexOf(roi));
        }

        private void MoveRoiToOrder(ROI roi, int order)
        {
            List<ROI> rois = m_Scene.ROIManager.ROIs;
            int current = rois.IndexOf(roi);
            if (current < 0) throw new InvalidOperationException("ROI is not owned by the prepared scene.");
            if (current == order) return;
            rois.RemoveAt(current);
            rois.Insert(order, roi);
            m_Scene.ROIManager.UpdateROIMasks();
        }

        private void MoveCutToOrder(CutId cutId, int order)
        {
            List<SceneCut> cuts = m_Scene.Cuts;
            SceneCut cut = ResolveCut(cutId);
            int current = cuts.IndexOf(cut);
            cuts.RemoveAt(current);
            cuts.Insert(order, cut);
            for (int i = 0; i < cuts.Count; i++) cuts[i].Index = i;
            m_Scene.SceneInformation.CutsNeedUpdate = true;
            m_Scene.OnModifyPlanesCuts.Invoke();
        }

        private void ApplyCutOrder(SetCutOrder value)
        {
            SceneCut[] ordered = value.CutIds.Select(ResolveCut).ToArray();
            m_Scene.Cuts.Clear();
            m_Scene.Cuts.AddRange(ordered);
            for (int i = 0; i < ordered.Length; i++) ordered[i].Index = i;
            m_Scene.SceneInformation.CutsNeedUpdate = true;
            m_LastCutOrder = value.CutIds.ToArray();
            m_Scene.OnModifyPlanesCuts.Invoke();
        }

        private void ApplyMeshDisplay(SetMeshDisplay value)
        {
            Mesh3D mesh = m_ResourceCatalog.ResolveMesh(value.MeshId.Value);
            m_Scene.MeshManager.SelectPrepared(mesh);
            MeshPart part = value.Part switch { V2MeshPart.Left => MeshPart.Left, V2MeshPart.Right => MeshPart.Right, _ => MeshPart.Both };
            m_Scene.MeshManager.SelectMeshPart(part);
            m_Scene.MeshManager.SelectRepresentation(value.Representation == V2SurfaceRepresentation.Inflated ? Core.Object3D.SurfaceRepresentation.Inflated : Core.Object3D.SurfaceRepresentation.Anatomical);
        }

        private SetMeshDisplay CreateMeshDisplayMutation()
        {
            RequireResourceCatalog();
            Mesh3D mesh = m_Scene.MeshManager.SelectedMesh;
            V2MeshPart part = m_Scene.MeshManager.MeshPartToDisplay switch { MeshPart.Left => V2MeshPart.Left, MeshPart.Right => V2MeshPart.Right, _ => V2MeshPart.Both };
            V2SurfaceRepresentation representation = mesh.Representation == Core.Object3D.SurfaceRepresentation.Inflated ? V2SurfaceRepresentation.Inflated : V2SurfaceRepresentation.Anatomical;
            return new SetMeshDisplay(new ResourceId(m_ResourceCatalog.MeshReference(mesh)), part, representation);
        }

        private SetSelectedMri CreateSelectedMriMutation()
        {
            RequireResourceCatalog();
            return new SetSelectedMri(new ResourceId(m_ResourceCatalog.MriReference(m_Scene.MRIManager.SelectedMRI)));
        }

        private SetImplantation CreateImplantationMutation()
        {
            RequireResourceCatalog();
            Implantation3D implantation = m_Scene.ImplantationManager.SelectedImplantation;
            if (implantation == null) throw new InvalidOperationException("No prepared implantation is selected.");
            return new SetImplantation(new ResourceId(m_ResourceCatalog.ImplantationReference(implantation)), ComputeMembershipHash(implantation));
        }

        private static string ComputeMembershipHash(Implantation3D implantation) => ApplyTriangleMask.HashMembership(implantation.SiteInfos.Select(site => (site.Patient?.ID ?? string.Empty) + "_" + site.Name));

        private void RequireResourceCatalog()
        {
            if (m_ResourceCatalog == null) throw new InvalidOperationException("Prepared resource identities are unavailable before the scene publication is bound.");
            m_ResourceCatalog.AssertPreparedRoster();
        }

        private (TopologyId Complete, TopologyId Simplified) CurrentTopologyIds()
        {
            RequireResourceCatalog();
            Mesh3D mesh = m_Scene.MeshManager.SelectedMesh;
            string resource = m_ResourceCatalog.MeshReference(mesh);
            string part = m_Scene.MeshManager.MeshPartToDisplay.ToString().ToLowerInvariant();
            return (new TopologyId("surface:" + resource + ":" + part + ":complete"), new TopologyId("surface:" + resource + ":" + part + ":simplified"));
        }

        private ApplyTriangleMask CreateTriangleMaskMutation()
        {
            if (m_Scene.TriangleEraser == null) throw new InvalidOperationException("Triangle visibility is unavailable in the prepared scene.");
            (TopologyId complete, TopologyId simplified) = CurrentTopologyIds();
            List<int[]> masks = m_Scene.TriangleEraser.CurrentMasks;
            if (masks.Count != 2) throw new InvalidOperationException("Both original topology masks are required.");
            return new ApplyTriangleMask(new[]
            {
                V2TriangleMask.FromVisibilityMask(complete, masks[0]),
                V2TriangleMask.FromVisibilityMask(simplified, masks[1])
            });
        }

        private void ValidateTriangleMask(ApplyTriangleMask mutation)
        {
            if (m_Scene.TriangleEraser == null || m_Scene.MeshManager?.SelectedMesh == null) throw new InvalidOperationException("Triangle visibility is unavailable in the prepared scene.");
            (TopologyId complete, TopologyId simplified) = CurrentTopologyIds();
            List<int[]> current = m_Scene.TriangleEraser.CurrentMasks;
            TopologyId[] expectedIds = { complete, simplified };
            if (current.Count != 2 || mutation.Masks.Count != 2) throw new InvalidOperationException("Both original topology masks are required.");
            for (int i = 0; i < 2; i++)
                if (!mutation.Masks[i].TopologyId.Equals(expectedIds[i]) || mutation.Masks[i].TriangleCount != current[i].Length)
                    throw new InvalidOperationException("Triangle mask topology does not match the prepared original surface.");
        }

        private void ValidateT09Mutation(V2Mutation mutation)
        {
            RequireScene();
            switch (mutation)
            {
                case SetSelectedColumn selected:
                    if (selected.ColumnId != null) ResolveColumn(selected.ColumnId);
                    break;
                case SetSelectedSite selected:
                    {
                        Column3D column = ResolveColumn(selected.ColumnId);
                        if (selected.SiteId != null)
                        {
                            SiteState site = ResolveSite(selected.ColumnId, selected.SiteId);
                            if (site.IsEffectivelyMasked(m_Scene.ROIManager != null && m_Scene.ROIManager.SelectedROI != null)) throw new InvalidOperationException("A site masked in the prepared scene cannot become the shared selection.");
                        }

                        if (column == null) throw new KeyNotFoundException("Selected column is absent.");
                        break;
                    }
                case SetSceneBoolean value:
                    if (value.Property == V2SceneBooleanProperty.BrainTransparent && m_Scene.BrainMaterials == null) throw new InvalidOperationException("Brain material presentation is unavailable in the prepared scene.");
                    ValidateSceneBoolean(value);
                    break;
                case SetSceneFloat value:
                    if (value.Property == V2SceneFloatProperty.BrainAlpha && m_Scene.BrainMaterials == null) throw new InvalidOperationException("Brain material presentation is unavailable in the prepared scene.");
                    if (value.Property == V2SceneFloatProperty.AtlasAlpha && m_Scene.AtlasManager == null) throw new InvalidOperationException("Atlas display is unavailable in the prepared scene.");
                    break;
                case SetSceneColor value:
                    if (m_Scene.BrainMaterials == null) throw new InvalidOperationException("Brain material presentation is unavailable in the prepared scene.");
                    if (!Enum.IsDefined(typeof(ColorType), value.Value)) throw new ArgumentOutOfRangeException(nameof(mutation), "Unknown scene color preset.");
                    break;
                case SetSiteHighlight value: ResolveSite(value.ColumnId, value.SiteId); break;
                case SetSiteLabels value: ResolveSite(value.ColumnId, value.SiteId); break;
                case SetActivityAlpha value: RequireColumnType(value.ColumnId, column => true); break;
                case SetColumnSpan value:
                    if (value.Kind == V2ColumnSpanKind.Static) ResolveStaticColumn(value.ColumnId);
                    else ResolveDynamicColumn(value.ColumnId);
                    break;
                case SetFunctionalDisplay value:
                    if (value.Modality == V2FunctionalModality.Fmri) ResolveFmriColumn(value.ColumnId);
                    else ResolveMegColumn(value.ColumnId);
                    break;
                case SetIbcDifumoDisplay value: ValidateIbcDifumoDisplay(value); break;
                case SetLocalizerDisplay value: ValidateLocalizerDisplay(value); break;
                case SetFmriAtlasCalibration: RequireFmriManager(); break;
                case SetSelectedRoiSphere value:
                    {
                        ROI roi = ResolveRoi(value.RoiId);
                        if (value.SphereId.Length > 0 && !roi.Spheres.Any(sphere => sphere.ID == value.SphereId)) throw new KeyNotFoundException("Selected ROI sphere is absent from the prepared ROI.");
                        break;
                    }
                default: throw new ArgumentException("Unsupported T09 scene mutation.", nameof(mutation));
            }
        }

        private bool ApplyT09Mutation(V2Mutation mutation)
        {
            ValidateT09Mutation(mutation);
            if (mutation is not SetSelectedColumn && V2MutationPayloadCodec.Encode(ReadCurrentT09Mutation(mutation)).SequenceEqual(V2MutationPayloadCodec.Encode(mutation))) return false;
            switch (mutation)
            {
                case SetSelectedColumn selected:
                    Column3D selectedColumn = selected.ColumnId == null ? null : ResolveColumn(selected.ColumnId);
                    bool changed = m_Scene.Columns.Any(column => column.IsSelected != ReferenceEquals(column, selectedColumn));
                    if (!changed) return false;
                    foreach (Column3D column in m_Scene.Columns) column.IsSelected = ReferenceEquals(column, selectedColumn);
                    return true;
                case SetSelectedSite selected:
                    {
                        Column3D column = ResolveColumn(selected.ColumnId);
                        if (selected.SiteId == null)
                        {
                            column.UnselectSite();
                            return true;
                        }

                        Core.Object3D.Site site = column.Sites.Single(candidate => candidate.Information.FullID == selected.SiteId.Value);
                        m_Scene.SelectSiteForSynchronization(column, site);
                        return true;
                    }
                case SetSceneBoolean value:
                    WriteSceneBoolean(value);
                    return true;
                case SetSceneFloat value:
                    WriteSceneFloat(value);
                    return true;
                case SetSceneColor value:
                    WriteSceneColor(value);
                    return true;
                case SetSiteHighlight value:
                    {
                        SiteState state = ResolveSite(value.ColumnId, value.SiteId);
                        if (state.IsHighlighted == value.Highlighted) return false;
                        state.IsHighlighted = value.Highlighted;
                        return true;
                    }
                case SetSiteLabels value:
                    {
                        SiteState state = ResolveSite(value.ColumnId, value.SiteId);
                        if (state.Labels.SequenceEqual(value.Labels)) return false;
                        state.ApplySpecificState(false, state.IsHighlighted, false, state.IsBlackListed, false, state.Color, true, value.Labels);
                        return true;
                    }
                case SetActivityAlpha value:
                    {
                        Column3D column = ResolveColumn(value.ColumnId);
                        if (column.ActivityAlpha == value.Alpha) return false;
                        column.ActivityAlpha = value.Alpha;
                        return true;
                    }
                case SetColumnSpan value:
                    if (value.Kind == V2ColumnSpanKind.Static) ResolveStaticColumn(value.ColumnId).StaticParameters.ApplySynchronizedSpanValues(value.Minimum, value.Middle, value.Maximum);
                    else ResolveDynamicColumn(value.ColumnId).DynamicParameters.ApplySynchronizedSpanValues(value.Minimum, value.Middle, value.Maximum);
                    return true;
                case SetFunctionalDisplay value:
                    ApplyFunctionalDisplay(value);
                    return true;
                case SetIbcDifumoDisplay value:
                    ApplyIbcDifumoDisplay(value);
                    return true;
                case SetLocalizerDisplay value:
                    ApplyLocalizerDisplay(value);
                    return true;
                case SetFmriAtlasCalibration value:
                    m_Scene.FMRIManager.ApplySynchronizedAtlasCalibration(value.Alpha, value.NegativeMinimum, value.NegativeMaximum, value.PositiveMinimum, value.PositiveMaximum);
                    return true;
                case SetSelectedRoiSphere value:
                    {
                        ROI roi = ResolveRoi(value.RoiId);
                        int targetIndex = value.SphereId.Length == 0 ? -1 : roi.Spheres.FindIndex(sphere => sphere.ID == value.SphereId);
                        if (roi.SelectedSphereID == targetIndex) return false;
                        roi.SelectSphere(targetIndex);
                        return true;
                    }
                default: throw new ArgumentException("Unsupported T09 scene mutation.", nameof(mutation));
            }
        }

        private void ValidateSceneBoolean(SetSceneBoolean value)
        {
            if (value.Property == V2SceneBooleanProperty.DisplayMarsAtlas)
            {
                if (m_Scene.AtlasManager == null) throw new InvalidOperationException("Atlas display is unavailable in the prepared scene.");
                Mesh3D selectedMesh = GetSelectedMeshOrNull();
                if (value.Value && (selectedMesh == null || !Object3DManager.MarsAtlas.Loaded || !selectedMesh.SupportsMarsAtlas)) throw new InvalidOperationException("Mars atlas is not prepared for the selected mesh.");
            }

            if (value.Property == V2SceneBooleanProperty.DisplayJuBrainAtlas)
            {
                if (m_Scene.AtlasManager == null) throw new InvalidOperationException("Atlas display is unavailable in the prepared scene.");
                Mesh3D selectedMesh = GetSelectedMeshOrNull();
                if (value.Value && (selectedMesh == null || !Object3DManager.JuBrain.Loaded || !selectedMesh.SupportsMNIResources)) throw new InvalidOperationException("JuBrain atlas is not prepared for the selected mesh.");
            }
        }

        private void ValidateIbcDifumoDisplay(SetIbcDifumoDisplay value)
        {
            RequireFmriManager();
            Mesh3D selectedMesh = GetSelectedMeshOrNull();
            bool hasIbcReference = value.IbcContrastReference.Length > 0;
            int contrast = 0;
            if (hasIbcReference && (!int.TryParse(value.IbcContrastReference, NumberStyles.None, CultureInfo.InvariantCulture, out contrast) || contrast < 0))
                throw new InvalidOperationException("IBC contrast reference must identify a prepared nonnegative contrast.");
            if (value.IbcEnabled)
            {
                bool contrastPrepared = hasIbcReference && Object3DManager.IBC.Loaded && contrast < Object3DManager.IBC.FMRI.Volumes.Count;
                if (!contrastPrepared || selectedMesh == null || !selectedMesh.SupportsMNIResources)
                    throw new InvalidOperationException("IBC contrast is not present in the prepared resources for this mesh.");
            }

            if (value.DifumoEnabled)
            {
                bool atlasPrepared = Object3DManager.DiFuMo.IsLoaded(value.DifumoAtlasReference) && Object3DManager.DiFuMo.FMRIs.TryGetValue(value.DifumoAtlasReference, out var atlas) && value.DifumoArea < atlas.Volumes.Count;
                if (!atlasPrepared || selectedMesh == null || !selectedMesh.SupportsMNIResources)
                    throw new InvalidOperationException("DiFuMo atlas area is not present in the prepared resources for this mesh.");
            }
        }

        private void ValidateLocalizerDisplay(SetLocalizerDisplay value)
        {
            RequireFmriManager();
            if (!value.Enabled) return;
            Mesh3D selectedMesh = GetSelectedMeshOrNull();
            if (selectedMesh == null || !selectedMesh.SupportsMNIResources) throw new InvalidOperationException("Localizers are not supported by the selected mesh.");
            var fmri = Object3DManager.Localizers.GetCurrentFMRI(value.ProtocolReference, value.DataReference, value.BlocReference);
            if (fmri == null || !fmri.Loaded || value.TimelineIndex >= fmri.Volumes.Count) throw new InvalidOperationException("Localizer source or timeline index is absent from the prepared resources.");
        }

        private void ApplyFunctionalDisplay(SetFunctionalDisplay value)
        {
            if (value.Modality == V2FunctionalModality.Fmri)
            {
                FMRIDataParameters parameters = ResolveFmriColumn(value.ColumnId).FMRIParameters;
                parameters.ApplySynchronizedCalibration(value.NegativeMinimum, value.NegativeMaximum, value.PositiveMinimum, value.PositiveMaximum);
                if (parameters.HideLowerValues != value.HideLower || parameters.HideMiddleValues != value.HideMiddle || parameters.HideHigherValues != value.HideHigher)
                    parameters.SetHideValues(value.HideLower, value.HideMiddle, value.HideHigher);
            }
            else
            {
                MEGDataParameters parameters = ResolveMegColumn(value.ColumnId).MEGParameters;
                parameters.ApplySynchronizedCalibration(value.NegativeMinimum, value.NegativeMaximum, value.PositiveMinimum, value.PositiveMaximum);
                if (parameters.HideLowerValues != value.HideLower || parameters.HideMiddleValues != value.HideMiddle || parameters.HideHigherValues != value.HideHigher)
                    parameters.SetHideValues(value.HideLower, value.HideMiddle, value.HideHigher);
            }
        }

        private void ApplyIbcDifumoDisplay(SetIbcDifumoDisplay value)
        {
            int contrast = value.IbcContrastReference.Length == 0 ? m_Scene.FMRIManager.SelectedIBCContrastID : int.Parse(value.IbcContrastReference, CultureInfo.InvariantCulture);
            string atlas = value.DifumoAtlasReference.Length == 0 && !value.DifumoEnabled ? string.Empty : value.DifumoAtlasReference;
            m_Scene.FMRIManager.ApplySynchronizedAtlasSources(value.IbcEnabled, contrast, value.DifumoEnabled, atlas, value.DifumoArea);
        }

        private void ApplyLocalizerDisplay(SetLocalizerDisplay value)
        {
            m_Scene.FMRIManager.ApplySynchronizedLocalizer(value.Enabled, value.ProtocolReference, value.DataReference, value.BlocReference, value.TimelineIndex, value.Minimum, value.Middle, value.Maximum);
        }

        private void WriteSceneBoolean(SetSceneBoolean value)
        {
            switch (value.Property)
            {
                case V2SceneBooleanProperty.StrongCuts: m_Scene.StrongCuts = value.Value; break;
                case V2SceneBooleanProperty.AutomaticCutAroundSelectedSite: m_Scene.AutomaticCutAroundSelectedSite = value.Value; break;
                case V2SceneBooleanProperty.HideBlacklistedSites: m_Scene.HideBlacklistedSites = value.Value; break;
                case V2SceneBooleanProperty.ShowAllSites: m_Scene.ShowAllSites = value.Value; break;
                case V2SceneBooleanProperty.EdgeMode: m_Scene.EdgeMode = value.Value; break;
                case V2SceneBooleanProperty.BrainTransparent: m_Scene.IsBrainTransparent = value.Value; break;
                case V2SceneBooleanProperty.DisplayMarsAtlas: m_Scene.AtlasManager.DisplayMarsAtlas = value.Value; break;
                case V2SceneBooleanProperty.DisplayJuBrainAtlas: m_Scene.AtlasManager.DisplayJuBrainAtlas = value.Value; break;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private void WriteSceneFloat(SetSceneFloat value)
        {
            switch (value.Property)
            {
                case V2SceneFloatProperty.SiteGain: m_Scene.SiteGain = value.Value; break;
                case V2SceneFloatProperty.BrainAlpha: m_Scene.BrainMaterials.SetAlpha(value.Value); break;
                case V2SceneFloatProperty.AtlasAlpha: m_Scene.AtlasManager.AtlasAlpha = value.Value; break;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private void WriteSceneColor(SetSceneColor value)
        {
            switch (value.Property)
            {
                case V2SceneColorProperty.Brain: m_Scene.BrainColor = (ColorType)value.Value; break;
                case V2SceneColorProperty.Cut: m_Scene.CutColor = (ColorType)value.Value; break;
                case V2SceneColorProperty.Colormap: m_Scene.Colormap = (ColorType)value.Value; break;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private bool ReadSceneBoolean(V2SceneBooleanProperty property) =>
            property switch
            {
                V2SceneBooleanProperty.StrongCuts => m_Scene.StrongCuts,
                V2SceneBooleanProperty.AutomaticCutAroundSelectedSite => m_Scene.AutomaticCutAroundSelectedSite,
                V2SceneBooleanProperty.HideBlacklistedSites => m_Scene.HideBlacklistedSites,
                V2SceneBooleanProperty.ShowAllSites => m_Scene.ShowAllSites,
                V2SceneBooleanProperty.EdgeMode => m_Scene.EdgeMode,
                V2SceneBooleanProperty.BrainTransparent => m_Scene.IsBrainTransparent,
                V2SceneBooleanProperty.DisplayMarsAtlas => m_Scene.AtlasManager.DisplayMarsAtlas,
                V2SceneBooleanProperty.DisplayJuBrainAtlas => m_Scene.AtlasManager.DisplayJuBrainAtlas,
                _ => throw new ArgumentOutOfRangeException(nameof(property))
            };

        private float ReadSceneFloat(V2SceneFloatProperty property) =>
            property switch
            {
                V2SceneFloatProperty.SiteGain => m_Scene.SiteGain,
                V2SceneFloatProperty.BrainAlpha => m_Scene.BrainMaterials.Alpha,
                V2SceneFloatProperty.AtlasAlpha => m_Scene.AtlasManager.AtlasAlpha,
                _ => throw new ArgumentOutOfRangeException(nameof(property))
            };

        private int ReadSceneColor(V2SceneColorProperty property) =>
            property switch
            {
                V2SceneColorProperty.Brain => (int)m_Scene.BrainColor,
                V2SceneColorProperty.Cut => (int)m_Scene.CutColor,
                V2SceneColorProperty.Colormap => (int)m_Scene.Colormap,
                _ => throw new ArgumentOutOfRangeException(nameof(property))
            };

        private SetSelectedSite CurrentSelectedSite(ColumnId columnId)
        {
            Column3D column = ResolveColumn(columnId);
            return new SetSelectedSite(columnId, column.SelectedSite ? new SiteId(column.SelectedSite.Information.FullID) : null);
        }

        private SetIbcDifumoDisplay CreateIbcDifumoDisplay()
        {
            FMRIManager manager = RequireFmriManager();
            string ibcReference = Object3DManager.IBC.Loaded ? manager.SelectedIBCContrastID.ToString(CultureInfo.InvariantCulture) : string.Empty;
            string selectedDiFuMoAtlas = manager.SelectedDiFuMoAtlas;
            string difumoReference = !string.IsNullOrWhiteSpace(selectedDiFuMoAtlas) && Object3DManager.DiFuMo.IsLoaded(selectedDiFuMoAtlas) ? selectedDiFuMoAtlas : string.Empty;
            return new SetIbcDifumoDisplay(manager.DisplayIBCContrasts, ibcReference, manager.DisplayDiFuMo, difumoReference, Math.Max(0, manager.SelectedDiFuMoArea));
        }

        private SetLocalizerDisplay CreateLocalizerDisplay()
        {
            FMRIManager manager = RequireFmriManager();
            return new SetLocalizerDisplay(manager.DisplayLocalizers, manager.SelectedLocalizersProtocol ?? string.Empty, manager.SelectedLocalizersData ?? string.Empty, manager.SelectedLocalizersBloc ?? string.Empty, Math.Max(0, manager.SelectedLocalizersTimelineIndex), manager.LocalizersMin, manager.LocalizersMiddle, manager.LocalizersMax);
        }

        private SetFmriAtlasCalibration CreateFmriAtlasCalibration()
        {
            FMRIManager manager = RequireFmriManager();
            return new SetFmriAtlasCalibration(manager.FMRIAlpha, manager.FMRINegativeCalMinFactor, manager.FMRINegativeCalMaxFactor, manager.FMRIPositiveCalMinFactor, manager.FMRIPositiveCalMaxFactor);
        }

        private FMRIManager RequireFmriManager() => m_Scene.FMRIManager != null ? m_Scene.FMRIManager : throw new InvalidOperationException("FMRI presentation is unavailable in the prepared scene.");

        private Mesh3D GetSelectedMeshOrNull()
        {
            MeshManager manager = m_Scene.MeshManager;
            if (manager == null || manager.Meshes == null || manager.SelectedMeshID < 0 || manager.SelectedMeshID >= manager.Meshes.Count)
                return null;
            return manager.Meshes[manager.SelectedMeshID];
        }

        private SetSelectedRoiSphere CurrentSelectedRoiSphere(ROI roi) => new SetSelectedRoiSphere(roi.ID, roi.SelectedSphere ? roi.SelectedSphere.ID : string.Empty);

        private Column3D ResolveColumn(ColumnId columnId)
        {
            if (columnId != null && m_ColumnsById.TryGetValue(columnId.Value, out Column3D column)) return column;
            throw new KeyNotFoundException("Column target is not part of the prepared scene.");
        }

        private Column3D RequireColumnType(ColumnId columnId, Func<Column3D, bool> predicate)
        {
            Column3D column = ResolveColumn(columnId);
            if (!predicate(column)) throw new InvalidOperationException("Column modality does not support this mutation.");
            return column;
        }

        private Column3DStatic ResolveStaticColumn(ColumnId columnId) => RequireColumnType(columnId, column => column is Column3DStatic) as Column3DStatic;
        private Column3DDynamic ResolveDynamicColumn(ColumnId columnId) => RequireColumnType(columnId, column => column is Column3DDynamic) as Column3DDynamic;
        private Column3DFMRI ResolveFmriColumn(ColumnId columnId) => RequireColumnType(columnId, column => column is Column3DFMRI) as Column3DFMRI;
        private Column3DMEG ResolveMegColumn(ColumnId columnId) => RequireColumnType(columnId, column => column is Column3DMEG) as Column3DMEG;

        private ROI ResolveRoi(string roiId)
        {
            if (m_RoisById.TryGetValue(roiId, out ROI roi) && roi) return roi;
            throw new KeyNotFoundException("ROI target is not part of the prepared scene.");
        }

        private (ROI Roi, RoiSphere Sphere) ResolveSphere(RoiId roiId, SphereId sphereId)
        {
            ROI roi = ResolveRoi(roiId.Value);
            RoiSphere sphere = roi.Spheres.FirstOrDefault(item => item && StringComparer.Ordinal.Equals(item.ID, sphereId.Value));
            if (!sphere) throw new KeyNotFoundException("ROI sphere target is not part of the prepared ROI.");
            return (roi, sphere);
        }

        private void RequireScene()
        {
            if (m_Scene == null) throw new InvalidOperationException("This T09 mutation requires a bound prepared scene.");
        }

        private void OnSiteColorChanged(SiteState state)
        {
            if (m_Disposed || !m_Sites.TryGetValue(state, out List<SiteTarget> targets)) return;
            if (m_SitePresentationStates.TryGetValue(state, out SitePresentationSnapshot previous))
                RecordConfigurationSiteBefore(state, previous);
            m_SitePresentationStates[state] = SitePresentationSnapshot.Capture(state);
            if (ShouldSuppressPublication()) return;
            Color color = state.Color;
            foreach (SiteTarget target in targets)
            {
                SetSiteColor mutation;
                try
                {
                    mutation = CreateSiteColor(target, color);
                }
                catch (ArgumentException)
                {
                    // A locally requested Core value outside the v2 contract is not a publishable mutation.
                    continue;
                }

                Publish(mutation);
            }
        }

        private void OnCutDefinitionChanged(SceneCut cut)
        {
            if (m_Disposed || !m_CutIds.TryGetValue(cut, out CutId id)) return;
            SetCutDefinition mutation;
            try
            {
                mutation = CreateCutDefinition(cut, id);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
            {
                // A locally requested Core value outside the v2 contract is not a publishable mutation.
                return;
            }

            m_CutDefinitionSnapshots.TryGetValue(cut, out SetCutDefinition previous);
            m_CutDefinitionSnapshots[cut] = mutation;
            if (m_Scene?.AutomaticCutAroundSelectedSite == true || ShouldSuppressPublication()) return;
            Publish(mutation, previous);
        }

        private void OnTimelineAnchorChanged(BasicTimeline timeline, bool automatic)
        {
            if (m_Disposed || !m_Timelines.TryGetValue(timeline, out List<TimelineTarget> targets)) return;
            if (!m_TimelineAnchorStates.TryGetValue(timeline, out TimelineAnchorState previous)) return;
            TimelineAnchorState current = TimelineAnchorState.Capture(timeline);
            m_TimelineAnchorStates[timeline] = current;
            if (previous.Playing != current.Playing)
                Interlocked.Add(ref m_PlayingTimelineCount, current.Playing ? 1 : -1);
            if (automatic || ShouldSuppressPublication()) return;

            V2TimelineAnchorIntent intent = GetTimelineAnchorIntent(previous, current);
            foreach (TimelineTarget target in targets)
            {
                SetTimelineAnchor mutation;
                try
                {
                    mutation = CreateTimelineAnchor(timeline, target.ColumnId, intent);
                }
                catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
                {
                    // A local value outside the v2 contract is not a publishable mutation.
                    continue;
                }

                Publish(mutation);
            }
        }

        private static V2TimelineAnchorIntent GetTimelineAnchorIntent(TimelineAnchorState previous, TimelineAnchorState current)
        {
            if (previous.Playing != current.Playing)
                return current.Playing ? V2TimelineAnchorIntent.Play : V2TimelineAnchorIntent.Pause;
            if (previous.Index != current.Index)
                return V2TimelineAnchorIntent.Seek;
            if (previous.Step != current.Step)
                return V2TimelineAnchorIntent.Step;
            if (previous.Looping != current.Looping)
                return V2TimelineAnchorIntent.Loop;
            throw new InvalidOperationException("A timeline anchor event did not change an anchor component.");
        }

        private readonly struct TimelineAnchorState
        {
            public int Index { get; }
            public bool Playing { get; }
            public bool Looping { get; }
            public int Step { get; }

            private TimelineAnchorState(BasicTimeline timeline)
            {
                Index = timeline.CurrentIndex;
                Playing = timeline.IsPlaying;
                Looping = timeline.IsLooping;
                Step = timeline.Step;
            }

            public static TimelineAnchorState Capture(BasicTimeline timeline) => new TimelineAnchorState(timeline);
        }

        private bool ShouldSuppressPublication()
        {
            if (BasicTimeline.IsAutomaticPlaybackUpdateInProgress) return true;
            if (m_ConfigurationMutationCapture != null) return false;
            if (V2MutationApplicationContext.TryGetCurrent(out V2MutationApplicationOrigin origin, out _, out _, out bool suppressNested))
                return origin == V2MutationApplicationOrigin.Remote || suppressNested;
            return false;
        }

        private void Publish(V2Mutation mutation, V2Mutation rollback = null)
        {
            if (m_ConfigurationMutationCapture != null)
            {
                m_ConfigurationMutationCapture.Children.Add((mutation, rollback));
                return;
            }

            if (V2MutationApplicationContext.TryGetCurrent(out V2MutationApplicationOrigin origin, out V2OriginDevice? device, out OperationId operationId, out bool suppressNested))
            {
                if (origin == V2MutationApplicationOrigin.Remote || suppressNested) return;
                SiteConfigurationProvenanceSnapshot provenanceRollback = CaptureSiteConfigurationProvenanceSnapshot(mutation);
                MarkSiteConfigurationWriters(mutation, operationId.Value);
                if (device == V2OriginDevice.Quest) RememberOptimisticRollback(operationId, rollback, mutation, provenanceRollback);
                MutationProposed?.Invoke(operationId, mutation, device.Value);
                return;
            }

            OperationId generatedOperationId = new(Guid.NewGuid());
            SiteConfigurationProvenanceSnapshot generatedProvenanceRollback = CaptureSiteConfigurationProvenanceSnapshot(mutation);
            MarkSiteConfigurationWriters(mutation, generatedOperationId.Value);
            if (m_LocalOrigin == V2OriginDevice.Quest) RememberOptimisticRollback(generatedOperationId, rollback, mutation, generatedProvenanceRollback);
            MutationProposed?.Invoke(generatedOperationId, mutation, m_LocalOrigin);
        }

        private SiteState ResolveSite(ColumnId columnId, SiteId siteId)
        {
            if (m_SitesById.TryGetValue((columnId, siteId), out SiteState state)) return state;
            throw new KeyNotFoundException("Site-color target is not part of the prepared scene.");
        }

        private SceneCut ResolveCut(CutId cutId)
        {
            if (m_Cuts.TryGetValue(cutId, out SceneCut cut)) return cut;
            throw new KeyNotFoundException("Cut target is not part of the prepared scene.");
        }

        private (BasicTimeline Timeline, ColumnId ColumnId) ResolveTimeline(ColumnId columnId)
        {
            foreach (KeyValuePair<BasicTimeline, List<TimelineTarget>> entry in m_Timelines)
            {
                TimelineTarget target = entry.Value.FirstOrDefault(value => value.ColumnId.Equals(columnId));
                if (target != null) return (entry.Key, target.ColumnId);
            }

            throw new KeyNotFoundException("Timeline target is not part of the prepared scene.");
        }

        private SetSiteColor CreateSiteColor(SiteTarget target, Color color) => new SetSiteColor(target.ColumnId, target.SiteId, color.r, color.g, color.b, color.a);

        private static SetCutDefinition CreateCutDefinition(SceneCut cut, CutId id) => new SetCutDefinition(id, (V2CutOrientation)cut.Orientation, cut.Flip, checked((uint)cut.NumberOfCuts), CanonicalZero(cut.Position), CanonicalZero(cut.Normal.x), CanonicalZero(cut.Normal.y), CanonicalZero(cut.Normal.z));

        // Native MRI orientation math can produce -0; the v2 wire contract requires canonical +0.
        private static float CanonicalZero(float value) => value == 0f ? 0f : value;

        private SetTimelineAnchor CreateTimelineAnchor(BasicTimeline timeline, ColumnId columnId, V2TimelineAnchorIntent? intent = null)
        {
            V2TimelineAnchorIntent resolvedIntent = intent ?? (timeline.IsPlaying ? V2TimelineAnchorIntent.Play : V2TimelineAnchorIntent.Pause);
            return new SetTimelineAnchor(columnId, timeline.CurrentIndex, timeline.IsPlaying, timeline.IsLooping, timeline.Step, m_Clock.GetTimestamp(), checked((ulong)m_Clock.Frequency), resolvedIntent);
        }

        private void ApplyTimelineAnchor(BasicTimeline timeline, SetTimelineAnchor anchor)
        {
            if (anchor.Intent != V2TimelineAnchorIntent.Play)
            {
                if (timeline.IsLooping != anchor.Looping) timeline.IsLooping = anchor.Looping;
                if (timeline.Step != anchor.Step) timeline.Step = anchor.Step;
                if (timeline.CurrentIndex != anchor.Index) timeline.CurrentIndex = anchor.Index;
                if (timeline.IsPlaying != anchor.Playing) timeline.IsPlaying = anchor.Playing;
                timeline.ApplySynchronizedClockAnchor(anchor.Playing ? Time.realtimeSinceStartup : 0f);
                return;
            }

            // An invalid estimate deliberately falls back to the transmitted index and starts playback on receipt.
            V2TimelineAnchorTimingEstimate? timingEstimate = null;
            if (m_TimelineTimingEstimate != null)
            {
                V2TimelineAnchorTimingEstimate? candidate = m_TimelineTimingEstimate(anchor);
                if (candidate.HasValue && candidate.Value.UncertaintySeconds <= V2TimelineClockEstimator.MaximumUncertaintyInSamples / anchor.Step)
                    timingEstimate = candidate;
            }
            else if (m_TimelineAgeSeconds != null)
            {
                double? estimatedAge = m_TimelineAgeSeconds(anchor);
                if (estimatedAge.HasValue && estimatedAge.Value >= 0d && !double.IsNaN(estimatedAge.Value) && !double.IsInfinity(estimatedAge.Value))
                    timingEstimate = new V2TimelineAnchorTimingEstimate(estimatedAge.Value, 0d);
            }

            double elapsed = timingEstimate?.AgeSeconds ?? 0d;
            double samplePosition = elapsed * anchor.Step;
            long wholeSteps = samplePosition >= long.MaxValue ? long.MaxValue : (long)Math.Floor(samplePosition);
            int index;
            if (anchor.Looping)
            {
                long offset = wholeSteps % timeline.Length;
                index = (int)(((long)anchor.Index + offset) % timeline.Length);
            }
            else index = wholeSteps >= timeline.Length - anchor.Index ? timeline.Length - 1 : anchor.Index + (int)wholeSteps;

            bool playing = timingEstimate.HasValue ? anchor.Looping || index < timeline.Length - 1 : anchor.Playing;
            int previousIndex = timeline.CurrentIndex;
            bool wasPlaying = timeline.IsPlaying;
            if (timeline.IsLooping != anchor.Looping) timeline.IsLooping = anchor.Looping;
            if (timeline.Step != anchor.Step) timeline.Step = anchor.Step;
            double uncertaintyInSamples = timingEstimate.HasValue ? timingEstimate.Value.UncertaintySeconds * anchor.Step : 0d;
            if (!wasPlaying || !timingEstimate.HasValue || IndexDrift(previousIndex, index, timeline.Length, anchor.Looping) > 1d + uncertaintyInSamples)
                if (timeline.CurrentIndex != index)
                    timeline.CurrentIndex = index;
            if (timeline.IsPlaying != playing) timeline.IsPlaying = playing;
            double fractionalAge = (samplePosition - Math.Floor(samplePosition)) / anchor.Step;
            float receiverAnchor = timingEstimate.HasValue && playing ? Math.Max(0f, Time.realtimeSinceStartup - (float)fractionalAge) : playing ? Time.realtimeSinceStartup : 0f;
            timeline.ApplySynchronizedClockAnchor(receiverAnchor);
        }

        private static double IndexDrift(int currentIndex, int targetIndex, int length, bool looping)
        {
            int difference = Math.Abs(currentIndex - targetIndex);
            return looping ? Math.Min(difference, length - difference) : difference;
        }

        private static V2OriginDevice ToOriginDevice(V2MutationApplicationOrigin origin) =>
            origin switch
            {
                V2MutationApplicationOrigin.LocalDesktop => V2OriginDevice.Desktop,
                V2MutationApplicationOrigin.LocalQuest => V2OriginDevice.Quest,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

        private static IEnumerable<(SiteState State, ColumnId ColumnId, SiteId SiteId)> CreateSiteTargets(Base3DScene scene)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            return scene.Columns.SelectMany(column => column.Sites.Select(site => (site.State, new ColumnId(column.ColumnData.ID), new SiteId(site.Information.FullID)))).ToArray();
        }

        private static IEnumerable<(SceneCut Cut, CutId Id)> CreateCutTargets(Base3DScene scene)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            return scene.Cuts.Select(cut => (cut, new CutId(cut.ID))).ToArray();
        }

        private static IEnumerable<(BasicTimeline Timeline, ColumnId ColumnId)> CreateTimelineTargets(Base3DScene scene)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            return scene.Columns.Where(column => column.NavigationTimeline != null && column.NavigationTimeline.Length > 0).Select(column => (column.NavigationTimeline, new ColumnId(column.ColumnData.ID))).ToArray();
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            SiteState.ColorChanged -= OnSiteColorChanged;
            SceneCut.DefinitionChanged -= OnCutDefinitionChanged;
            BasicTimeline.AnchorChanged -= OnTimelineAnchorChanged;
            if (m_Scene != null)
            {
                m_Scene.ConfigurationMutationStarted -= BeginConfigurationMutation;
                m_Scene.ConfigurationMutationCompleted -= CompleteConfigurationMutation;
                if (m_Scene.SiteConfigurationBatchRouter?.Target == this)
                    m_Scene.SiteConfigurationBatchRouter = null;
                m_Scene.OnAddCut.RemoveListener(OnCutAdded);
                m_Scene.OnRemoveCut.RemoveListener(OnCutRemoved);
                if (m_CutOrderListener != null) m_Scene.OnModifyPlanesCuts.RemoveListener(m_CutOrderListener);
                m_Scene.OnChangeAutomaticCutAroundSelectedSite.RemoveListener(OnAutomaticCutPolicyChanged);
                m_Scene.SitePositionCommandExecuted -= OnSitePositionCommandExecuted;
                m_Scene.OnSharedStateChanged.RemoveListener(ObserveScenePresentation);
                m_Scene.OnUpdateROI.RemoveListener(RefreshRoiTargets);
                if (m_Scene.ROIManager != null)
                {
                    m_Scene.ROIManager.RoiAdded -= OnRoiAdded;
                    m_Scene.ROIManager.RoiRemoved -= OnRoiRemoved;
                    m_Scene.ROIManager.ActiveRoiChanged -= OnActiveRoiChanged;
                }

                if (m_Scene.BrainMaterials != null) m_Scene.BrainMaterials.AlphaChanged -= OnBrainAlphaChanged;
                if (m_Scene.FMRIManager != null) m_Scene.FMRIManager.PresentationChanged -= ObserveFmriPresentation;
                if (m_Scene.MeshManager != null)
                {
                    m_Scene.MeshManager.ResourceSelectionChanged -= ObserveMeshSelection;
                    m_Scene.MeshManager.DisplaySelectionChanged -= ObserveMeshDisplay;
                }

                if (m_Scene.MRIManager != null) m_Scene.MRIManager.ResourceSelectionChanged -= ObserveMriSelection;
                if (m_Scene.ImplantationManager != null) m_Scene.ImplantationManager.ResourceSelectionChanged -= ObserveImplantationSelection;
                if (m_Scene.TriangleEraser != null) m_Scene.TriangleEraser.VisibilityMaskChanged -= ObserveTriangleMask;
                Module3DMain.OnSelectColumn.RemoveListener(OnModuleColumnSelected);
            }

            foreach (KeyValuePair<SiteState, UnityAction> entry in m_SiteStateListeners)
                entry.Key.OnChangeState.RemoveListener(entry.Value);
            foreach (KeyValuePair<ROI, UnityAction> entry in m_RoiSelectionListeners)
                entry.Key.OnChangeSphereSelectionState.RemoveListener(entry.Value);
            foreach (KeyValuePair<ROI, UnityAction> entry in m_RoiStructureListeners)
            {
                entry.Key.OnUpdateROIName.RemoveListener(entry.Value);
                entry.Key.OnChangeNumberOfSpheres.RemoveListener(entry.Value);
                entry.Key.OnChangeSphereParameters.RemoveListener(entry.Value);
            }

            foreach (KeyValuePair<Column3D, UnityAction<Core.Object3D.Site>> entry in m_ColumnSelectionListeners)
                entry.Key.OnSelectSite.RemoveListener(entry.Value);
            foreach (KeyValuePair<Column3D, List<(UnityEvent Event, UnityAction Listener)>> entry in m_ColumnListeners)
            foreach ((UnityEvent unityEvent, UnityAction listener) in entry.Value)
                unityEvent.RemoveListener(listener);

            m_TimelineAnchorStates.Clear();
            Interlocked.Exchange(ref m_PlayingTimelineCount, 0);
            m_SiteStateListeners.Clear();
            m_RoiSelectionListeners.Clear();
            m_RoiStructureListeners.Clear();
            m_RoiStateSnapshots.Clear();
            m_ColumnSelectionListeners.Clear();
            m_ColumnListeners.Clear();
            m_SitePresentationStates.Clear();
            m_LastSelectedSites.Clear();
            m_LastActivityAlphas.Clear();
            m_LastColumnSpans.Clear();
            m_LastFunctionalDisplays.Clear();
            m_LastInfluenceDistances.Clear();
            m_LastColumnResources.Clear();
            m_LastCcepSources.Clear();
            m_LastRoiSpheres.Clear();
            m_ColumnsById.Clear();
            m_RoisById.Clear();
            m_Sites.Clear();
            m_SitesById.Clear();
            m_CutIds.Clear();
            m_Cuts.Clear();
            m_CutDefinitionSnapshots.Clear();
            m_Timelines.Clear();
            m_ResourceCatalog = null;
        }

        private sealed class ConfigurationMutationCapture
        {
            public V2SceneMutationCheckpoint InitialCheckpoint { get; }
            public StagedConfigurationRoster InitialRoster { get; }
            public SiteConfigurationProvenanceSnapshot InitialSiteConfigurationProvenance { get; }
            public List<(V2Mutation Mutation, V2Mutation Rollback)> Children { get; } = new();
            public Dictionary<(string ColumnId, string SiteId), V2SiteConfigurationAssignment> SiteBefore { get; } = new();

            public ConfigurationMutationCapture(V2SceneMutationCheckpoint initialCheckpoint, StagedConfigurationRoster initialRoster, SiteConfigurationProvenanceSnapshot initialSiteConfigurationProvenance)
            {
                InitialCheckpoint = initialCheckpoint;
                InitialRoster = initialRoster;
                InitialSiteConfigurationProvenance = initialSiteConfigurationProvenance;
            }
        }

        private sealed class ConfigurationMutationChild
        {
            public V2Mutation Mutation { get; }
            public V2Mutation Rollback { get; }

            public ConfigurationMutationChild(V2Mutation mutation, V2Mutation rollback)
            {
                Mutation = mutation;
                Rollback = rollback;
            }
        }

        private sealed class StagedConfigurationRoster
        {
            public List<CutId> Cuts { get; }
            public Dictionary<string, HashSet<string>> Rois { get; }
            public List<string> RoiOrder { get; }
            public string ActiveRoiId { get; set; }

            public StagedConfigurationRoster(List<CutId> cuts, Dictionary<string, HashSet<string>> rois, List<string> roiOrder, string activeRoiId)
            {
                Cuts = cuts;
                Rois = rois;
                RoiOrder = roiOrder;
                ActiveRoiId = activeRoiId;
            }

            public StagedConfigurationRoster Clone() => new StagedConfigurationRoster(new List<CutId>(Cuts), Rois.ToDictionary(entry => entry.Key, entry => new HashSet<string>(entry.Value, StringComparer.Ordinal), StringComparer.Ordinal), new List<string>(RoiOrder), ActiveRoiId);
        }

        private sealed class RoiStateSnapshot
        {
            public string RoiId { get; }
            public string Name { get; }
            public int Order { get; }
            public IReadOnlyDictionary<string, V2RoiSphereDefinition> Spheres { get; }
            public IReadOnlyList<V2RoiSphereDefinition> SphereOrder { get; }

            private RoiStateSnapshot(string roiId, string name, int order, IReadOnlyDictionary<string, V2RoiSphereDefinition> spheres, IReadOnlyList<V2RoiSphereDefinition> sphereOrder)
            {
                RoiId = roiId;
                Name = name;
                Order = order;
                Spheres = spheres;
                SphereOrder = sphereOrder;
            }

            public static RoiStateSnapshot Capture(ROI roi, int order)
            {
                var spheres = new Dictionary<string, V2RoiSphereDefinition>(StringComparer.Ordinal);
                var sphereOrder = new List<V2RoiSphereDefinition>(roi.Spheres.Count);
                foreach (RoiSphere sphere in roi.Spheres)
                {
                    V2RoiSphereDefinition definition = CreateSphereDefinition(sphere);
                    if (!spheres.TryAdd(sphere.ID, definition))
                        throw new InvalidOperationException("ROI sphere identities must be unique.");
                    sphereOrder.Add(definition);
                }

                return new RoiStateSnapshot(roi.ID, roi.Name, order, spheres, sphereOrder.AsReadOnly());
            }
        }

        private sealed class SiteTarget
        {
            public ColumnId ColumnId { get; }
            public SiteId SiteId { get; }

            public SiteTarget(ColumnId columnId, SiteId siteId)
            {
                ColumnId = columnId;
                SiteId = siteId;
            }
        }

        private sealed class TimelineTarget
        {
            public ColumnId ColumnId { get; }
            public TimelineTarget(ColumnId columnId) => ColumnId = columnId;
        }
    }
}
