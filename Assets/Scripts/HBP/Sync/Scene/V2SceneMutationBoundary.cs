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

        internal V2SceneMutationCheckpoint(IEnumerable<SiteColorCheckpointRecord> siteColors, IEnumerable<CutDefinitionCheckpointRecord> cutDefinitions, IEnumerable<TimelineAnchorCheckpointRecord> timelineAnchors, IEnumerable<V2T09CheckpointRecord> t09Records = null, IEnumerable<V2T10CheckpointRecord> t10Records = null)
        {
            SiteColors = Array.AsReadOnly(siteColors.ToArray());
            CutDefinitions = Array.AsReadOnly(cutDefinitions.ToArray());
            TimelineAnchors = Array.AsReadOnly(timelineAnchors.ToArray());
            T09Records = Array.AsReadOnly((t09Records ?? Enumerable.Empty<V2T09CheckpointRecord>()).ToArray());
            T10Records = Array.AsReadOnly((t10Records ?? Enumerable.Empty<V2T10CheckpointRecord>()).ToArray());
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
        private readonly Dictionary<SiteState, UnityAction> m_SiteStateListeners = new();
        private readonly Dictionary<ROI, UnityAction> m_RoiSelectionListeners = new();
        private readonly Dictionary<ROI, UnityAction> m_RoiStructureListeners = new();
        private readonly Dictionary<ROI, RoiStateSnapshot> m_RoiStateSnapshots = new();
        private readonly Dictionary<Guid, V2Mutation> m_OptimisticRollbacks = new();
        private readonly Queue<Guid> m_OptimisticRollbackOrder = new();
        private readonly Dictionary<Column3D, List<(UnityEvent Event, UnityAction Listener)>> m_ColumnListeners = new();
        private readonly Dictionary<Column3D, UnityAction<Core.Object3D.Site>> m_ColumnSelectionListeners = new();
        private V2Mutation m_LastSceneStrongCuts;
        private V2Mutation m_LastSceneAutomaticCuts;
        private V2Mutation m_LastSceneHideBlacklisted;
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
        private readonly Base3DScene m_Scene;
        private readonly V2OriginDevice m_LocalOrigin;
        private readonly IMonotonicClock m_Clock;
        private readonly Action<SceneCut> m_UpdateCut;
        private readonly Func<SetTimelineAnchor, double?> m_TimelineAgeSeconds;
        private readonly Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> m_TimelineTimingEstimate;
        private readonly Dictionary<BasicTimeline, TimelineAnchorState> m_TimelineAnchorStates = new();
        private int m_PlayingTimelineCount;
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

            foreach (var target in cuts ?? throw new ArgumentNullException(nameof(cuts)))
            {
                if (target.Cut == null || target.Id == null)
                    throw new ArgumentException("Cut mutation targets require a cut and stable identity.", nameof(cuts));
                if (!m_Cuts.TryAdd(target.Id, target.Cut) || !m_CutIds.TryAdd(target.Cut, target.Id))
                    throw new ArgumentException("Cut mutation targets must have unique cut identities and objects.", nameof(cuts));
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
            V2Mutation rollback = mutation is MoveSites ? null : ReadCurrentMutation(mutation);
            using (origin == V2MutationApplicationOrigin.Remote ? V2MutationApplicationContext.EnterRemote(operationId) : V2MutationApplicationContext.EnterLocalApply(ToOriginDevice(origin), operationId))
            {
                if (!ApplyCore(mutation)) return;
            }

            if (origin != V2MutationApplicationOrigin.Remote)
            {
                if (origin == V2MutationApplicationOrigin.LocalQuest) RememberOptimisticRollback(operationId, rollback);
                MutationProposed?.Invoke(operationId, mutation, ToOriginDevice(origin));
            }
        }

        /// <summary>Restores the prepared value recorded before one rejected optimistic Quest operation.</summary>
        public bool TryRollbackOptimisticOperation(OperationId operationId)
        {
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            if (!m_OptimisticRollbacks.TryGetValue(operationId.Value, out V2Mutation rollback)) return false;
            m_OptimisticRollbacks.Remove(operationId.Value);
            Apply(rollback, V2MutationApplicationOrigin.Remote, operationId);
            return true;
        }

        public void ForgetOptimisticOperation(OperationId operationId)
        {
            if (operationId != null) m_OptimisticRollbacks.Remove(operationId.Value);
        }

        private void RememberOptimisticRollback(OperationId operationId, V2Mutation rollback)
        {
            if (m_LocalOrigin != V2OriginDevice.Quest || operationId == null || rollback == null) return;
            Guid id = operationId.Value;
            if (m_OptimisticRollbacks.ContainsKey(id)) m_OptimisticRollbacks[id] = rollback;
            else
            {
                m_OptimisticRollbacks.Add(id, rollback);
                m_OptimisticRollbackOrder.Enqueue(id);
            }

            while (m_OptimisticRollbacks.Count > V2QuestMutationDriver.MaximumRememberedOperations)
            {
                Guid oldest = m_OptimisticRollbackOrder.Dequeue();
                m_OptimisticRollbacks.Remove(oldest);
            }
        }

        /// <summary>Reads the current prepared value for the touched key of a typed mutation.</summary>
        public V2Mutation ReadCurrentMutation(V2Mutation key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
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
            foreach (KeyValuePair<SiteState, List<SiteTarget>> entry in m_Sites)
            foreach (SiteTarget target in entry.Value)
                siteRecords.Add(new SiteColorCheckpointRecord(CreateSiteColor(target, entry.Key.Color)));

            var cutRecords = new List<CutDefinitionCheckpointRecord>();
            foreach (KeyValuePair<SceneCut, CutId> entry in m_CutIds)
                cutRecords.Add(new CutDefinitionCheckpointRecord(CreateCutDefinition(entry.Key, entry.Value)));

            var timelineRecords = new List<TimelineAnchorCheckpointRecord>();
            foreach (KeyValuePair<BasicTimeline, List<TimelineTarget>> entry in m_Timelines)
            foreach (TimelineTarget target in entry.Value)
                timelineRecords.Add(new TimelineAnchorCheckpointRecord(CreateTimelineAnchor(entry.Key, target.ColumnId)));

            var t09Records = m_Scene == null ? new List<V2T09CheckpointRecord>() : CaptureT09Records();
            var t10Records = m_Scene == null ? new List<V2T10CheckpointRecord>() : CaptureT10Records();
            return new V2SceneMutationCheckpoint(siteRecords, cutRecords, timelineRecords, t09Records, t10Records);
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
                foreach (V2T09CheckpointRecord record in checkpoint.T09Records) ApplyCore(record.Value);
            }
        }

        private bool ApplyCore(V2Mutation mutation)
        {
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

            if ((ushort)mutation.Type >= (ushort)V2OperationType.CreateCut)
                return ApplyT10Mutation(mutation);

            if ((ushort)mutation.Type >= (ushort)V2OperationType.SetSelectedColumn)
                return ApplyT09Mutation(mutation);

            throw new ArgumentException("Unsupported v2 scene mutation.", nameof(mutation));
        }

        private void BindSceneTargets(Base3DScene scene)
        {
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
                }
                else if (column is Column3DDynamic dynamicColumn)
                {
                    m_LastColumnSpans.Add(column, CreateDynamicSpan(dynamicColumn));
                    AddColumnListener(dynamicColumn.DynamicParameters.OnUpdateSpanValues, () => ObserveColumnSpan(dynamicColumn), listeners);
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

                m_ColumnListeners.Add(column, listeners);
            }

            if (scene.ROIManager != null)
                foreach (ROI roi in scene.ROIManager.ROIs)
                    if (!m_RoisById.TryAdd(roi.ID, roi))
                        throw new ArgumentException("Prepared ROIs must have unique stable identities.", nameof(scene));

            foreach (KeyValuePair<SiteState, List<SiteTarget>> entry in m_Sites)
            {
                SiteState state = entry.Key;
                m_SitePresentationStates[state] = SitePresentationSnapshot.Capture(state);
                UnityAction listener = () => OnBoundSiteStateChanged(state);
                state.OnChangeState.AddListener(listener);
                m_SiteStateListeners.Add(state, listener);
            }

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

        private static void AddColumnListener(UnityEvent unityEvent, UnityAction listener, List<(UnityEvent Event, UnityAction Listener)> listeners)
        {
            unityEvent.AddListener(listener);
            listeners.Add((unityEvent, listener));
        }

        private List<V2T09CheckpointRecord> CaptureT09Records()
        {
            var records = new List<V2T09CheckpointRecord>();
            void Add(V2Mutation mutation) => records.Add(V2T09CheckpointRecord.FromMutation(mutation));
            Add(new SetSelectedColumn(m_Scene.SelectedColumn ? new ColumnId(m_Scene.SelectedColumn.ColumnData.ID) : null));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.AutomaticCutAroundSelectedSite, m_Scene.AutomaticCutAroundSelectedSite));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, m_Scene.StrongCuts));
            Add(new SetSceneBoolean(V2SceneBooleanProperty.HideBlacklistedSites, m_Scene.HideBlacklistedSites));
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

            foreach (KeyValuePair<SiteState, List<SiteTarget>> entry in m_Sites)
            foreach (SiteTarget target in entry.Value)
            {
                Add(new SetSiteHighlight(target.ColumnId, target.SiteId, entry.Key.IsHighlighted));
                Add(new SetSiteLabels(target.ColumnId, target.SiteId, entry.Key.Labels));
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
            SitePresentationSnapshot current = SitePresentationSnapshot.Capture(state);
            m_SitePresentationStates[state] = current;
            if (state.CurrentChangeKind != SiteStateChangeKind.Presentation || ShouldSuppressPublication()) return;
            foreach (SiteTarget target in targets)
            {
                if (previous.Highlighted != current.Highlighted)
                    Publish(new SetSiteHighlight(target.ColumnId, target.SiteId, current.Highlighted));
                if (!previous.Labels.SequenceEqual(current.Labels))
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
            previous = current;
            if (!ShouldSuppressPublication()) Publish(current);
        }

        private void ObserveT09<TKey>(Dictionary<TKey, V2Mutation> previousByKey, TKey key, V2Mutation current)
        {
            if (!previousByKey.TryGetValue(key, out V2Mutation previous) || previous == null)
            {
                previousByKey[key] = current;
                return;
            }

            if (V2MutationPayloadCodec.Encode(previous).SequenceEqual(V2MutationPayloadCodec.Encode(current))) return;
            previousByKey[key] = current;
            if (!ShouldSuppressPublication()) Publish(current);
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
                if (!ShouldSuppressPublication()) Publish(new DeleteRoi(new RoiId(previous.RoiId)));
                UnregisterRoi(stale);
            }

            foreach (ROI roi in current)
            {
                RegisterRoi(roi);
                RoiStateSnapshot next = RoiStateSnapshot.Capture(roi, m_Scene.ROIManager.ROIs.IndexOf(roi));
                if (!m_RoiStateSnapshots.TryGetValue(roi, out RoiStateSnapshot previous))
                {
                    if (!ShouldSuppressPublication()) Publish(CreateRoiMutation(roi));
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
            if (!ShouldSuppressPublication() && !m_Scene.AutomaticCutAroundSelectedSite) Publish(new CreateCut(m_CutIds[cut], CreateCutDefinition(cut, m_CutIds[cut]), cut.Index));
            m_LastCutOrder = m_Scene.Cuts.Select(item => new CutId(item.ID)).ToArray();
        }

        private void OnCutRemoved(SceneCut cut)
        {
            if (m_Disposed || cut == null || !m_CutIds.TryGetValue(cut, out CutId id)) return;
            if (!ShouldSuppressPublication() && !m_Scene.AutomaticCutAroundSelectedSite) Publish(new DeleteCut(id));
            m_CutIds.Remove(cut);
            m_Cuts.Remove(id);
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
            public bool Highlighted { get; }
            public string[] Labels { get; }

            private SitePresentationSnapshot(bool highlighted, string[] labels)
            {
                Highlighted = highlighted;
                Labels = labels;
            }

            public static SitePresentationSnapshot Capture(SiteState state) => new SitePresentationSnapshot(state.IsHighlighted, state.Labels.ToArray());
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
            if (m_Disposed || !m_Sites.TryGetValue(state, out List<SiteTarget> targets) || ShouldSuppressPublication()) return;
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
            if (m_Disposed || m_Scene?.AutomaticCutAroundSelectedSite == true || !m_CutIds.TryGetValue(cut, out CutId id) || ShouldSuppressPublication()) return;
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

            Publish(mutation);
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
            if (V2MutationApplicationContext.TryGetCurrent(out V2MutationApplicationOrigin origin, out _, out _, out bool suppressNested))
                return origin == V2MutationApplicationOrigin.Remote || suppressNested;
            return false;
        }

        private void Publish(V2Mutation mutation, V2Mutation rollback = null)
        {
            if (V2MutationApplicationContext.TryGetCurrent(out V2MutationApplicationOrigin origin, out V2OriginDevice? device, out OperationId operationId, out bool suppressNested))
            {
                if (origin == V2MutationApplicationOrigin.Remote || suppressNested) return;
                if (device == V2OriginDevice.Quest) RememberOptimisticRollback(operationId, rollback);
                MutationProposed?.Invoke(operationId, mutation, device.Value);
                return;
            }

            OperationId generatedOperationId = new(Guid.NewGuid());
            if (m_LocalOrigin == V2OriginDevice.Quest) RememberOptimisticRollback(generatedOperationId, rollback);
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
            m_LastRoiSpheres.Clear();
            m_ColumnsById.Clear();
            m_RoisById.Clear();
            m_Sites.Clear();
            m_SitesById.Clear();
            m_CutIds.Clear();
            m_Cuts.Clear();
            m_Timelines.Clear();
            m_ResourceCatalog = null;
        }

        private sealed class RoiStateSnapshot
        {
            public string RoiId { get; }
            public string Name { get; }
            public int Order { get; }
            public IReadOnlyDictionary<string, V2RoiSphereDefinition> Spheres { get; }

            private RoiStateSnapshot(string roiId, string name, int order, IReadOnlyDictionary<string, V2RoiSphereDefinition> spheres)
            {
                RoiId = roiId;
                Name = name;
                Order = order;
                Spheres = spheres;
            }

            public static RoiStateSnapshot Capture(ROI roi, int order)
            {
                var spheres = new Dictionary<string, V2RoiSphereDefinition>(StringComparer.Ordinal);
                foreach (RoiSphere sphere in roi.Spheres)
                    if (!spheres.TryAdd(sphere.ID, CreateSphereDefinition(sphere)))
                        throw new InvalidOperationException("ROI sphere identities must be unique.");
                return new RoiStateSnapshot(roi.ID, roi.Name, order, spheres);
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
