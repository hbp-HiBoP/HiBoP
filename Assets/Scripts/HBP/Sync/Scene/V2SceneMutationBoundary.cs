using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.Events;
using SceneCut = HBP.Core.Object3D.Cut;

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

        internal V2SceneMutationCheckpoint(IEnumerable<SiteColorCheckpointRecord> siteColors, IEnumerable<CutDefinitionCheckpointRecord> cutDefinitions, IEnumerable<TimelineAnchorCheckpointRecord> timelineAnchors, IEnumerable<V2T09CheckpointRecord> t09Records = null)
        {
            SiteColors = Array.AsReadOnly(siteColors.ToArray());
            CutDefinitions = Array.AsReadOnly(cutDefinitions.ToArray());
            TimelineAnchors = Array.AsReadOnly(timelineAnchors.ToArray());
            T09Records = Array.AsReadOnly((t09Records ?? Enumerable.Empty<V2T09CheckpointRecord>()).ToArray());
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
        private readonly Dictionary<Column3D, List<(UnityEvent Event, UnityAction Listener)>> m_ColumnListeners = new();
        private readonly Dictionary<Column3D, UnityAction<Core.Object3D.Site>> m_ColumnSelectionListeners = new();
        private V2Mutation m_LastSceneStrongCuts;
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
            using (origin == V2MutationApplicationOrigin.Remote ? V2MutationApplicationContext.EnterRemote(operationId) : V2MutationApplicationContext.EnterLocalApply(ToOriginDevice(origin), operationId))
            {
                if (!ApplyCore(mutation)) return;
            }

            if (origin != V2MutationApplicationOrigin.Remote)
                MutationProposed?.Invoke(operationId, mutation, ToOriginDevice(origin));
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
            return new V2SceneMutationCheckpoint(siteRecords, cutRecords, timelineRecords, t09Records);
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
            foreach (CutDefinitionCheckpointRecord record in checkpoint.CutDefinitions)
            {
                ResolveCut(record.Value.CutId);
                if (!cutKeys.Add(record.Value.CutId)) throw new ArgumentException("Checkpoint contains a duplicate cut key.", nameof(checkpoint));
            }

            var timelineKeys = new HashSet<ColumnId>();
            foreach (TimelineAnchorCheckpointRecord record in checkpoint.TimelineAnchors)
            {
                (BasicTimeline timeline, _) = ResolveTimeline(record.Value.ColumnId);
                if (record.Value.Index >= timeline.Length) throw new ArgumentOutOfRangeException(nameof(checkpoint), "Checkpoint timeline index exceeds the prepared timeline.");
                if (!timelineKeys.Add(record.Value.ColumnId)) throw new ArgumentException("Checkpoint contains a duplicate timeline key.", nameof(checkpoint));
            }

            foreach (V2T09CheckpointRecord record in checkpoint.T09Records)
                ValidateMutation(record.Value);

            using (V2MutationApplicationContext.EnterRemote(operationId))
            {
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
            scene.OnSharedStateChanged.AddListener(ObserveScenePresentation);
            if (scene.BrainMaterials != null) scene.BrainMaterials.AlphaChanged += OnBrainAlphaChanged;
            if (scene.FMRIManager != null) scene.FMRIManager.PresentationChanged += ObserveFmriPresentation;
            Module3DMain.OnSelectColumn.AddListener(OnModuleColumnSelected);
            scene.OnUpdateROI.AddListener(RefreshRoiTargets);
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
            ObserveT09(ref m_LastSceneHideBlacklisted, new SetSceneBoolean(V2SceneBooleanProperty.HideBlacklistedSites, m_Scene.HideBlacklistedSites));
            ObserveT09(ref m_LastSceneSiteGain, new SetSceneFloat(V2SceneFloatProperty.SiteGain, m_Scene.SiteGain));
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

        private void RefreshRoiTargets()
        {
            if (m_Scene == null || m_Scene.ROIManager == null || m_Disposed) return;
            var current = new HashSet<ROI>(m_Scene.ROIManager.ROIs);
            foreach (ROI stale in m_RoisById.Values.Where(roi => !current.Contains(roi)).ToArray())
            {
                if (m_RoiSelectionListeners.TryGetValue(stale, out UnityAction listener)) stale.OnChangeSphereSelectionState.RemoveListener(listener);
                m_RoiSelectionListeners.Remove(stale);
                m_LastRoiSpheres.Remove(stale);
                m_RoisById.Remove(stale.ID);
            }

            foreach (ROI roi in current)
            {
                if (m_RoisById.TryGetValue(roi.ID, out ROI existing) && ReferenceEquals(existing, roi)) continue;
                if (!m_RoisById.TryAdd(roi.ID, roi)) throw new InvalidOperationException("Prepared ROIs must have unique stable identities.");
                m_LastRoiSpheres.Add(roi, new SetSelectedRoiSphere(roi.ID, roi.SelectedSphere ? roi.SelectedSphere.ID : string.Empty));
                UnityAction listener = () => OnRoiSphereSelectionChanged(roi);
                roi.OnChangeSphereSelectionState.AddListener(listener);
                m_RoiSelectionListeners.Add(roi, listener);
            }
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
            if (m_Disposed || !m_CutIds.TryGetValue(cut, out CutId id) || ShouldSuppressPublication()) return;
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

        private void Publish(V2Mutation mutation)
        {
            if (V2MutationApplicationContext.TryGetCurrent(out V2MutationApplicationOrigin origin, out V2OriginDevice? device, out OperationId operationId, out bool suppressNested))
            {
                if (origin == V2MutationApplicationOrigin.Remote || suppressNested) return;
                MutationProposed?.Invoke(operationId, mutation, device.Value);
                return;
            }

            MutationProposed?.Invoke(new OperationId(Guid.NewGuid()), mutation, m_LocalOrigin);
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
                m_Scene.OnSharedStateChanged.RemoveListener(ObserveScenePresentation);
                m_Scene.OnUpdateROI.RemoveListener(RefreshRoiTargets);
                if (m_Scene.BrainMaterials != null) m_Scene.BrainMaterials.AlphaChanged -= OnBrainAlphaChanged;
                if (m_Scene.FMRIManager != null) m_Scene.FMRIManager.PresentationChanged -= ObserveFmriPresentation;
                Module3DMain.OnSelectColumn.RemoveListener(OnModuleColumnSelected);
            }

            foreach (KeyValuePair<SiteState, UnityAction> entry in m_SiteStateListeners)
                entry.Key.OnChangeState.RemoveListener(entry.Value);
            foreach (KeyValuePair<ROI, UnityAction> entry in m_RoiSelectionListeners)
                entry.Key.OnChangeSphereSelectionState.RemoveListener(entry.Value);
            foreach (KeyValuePair<Column3D, UnityAction<Core.Object3D.Site>> entry in m_ColumnSelectionListeners)
                entry.Key.OnSelectSite.RemoveListener(entry.Value);
            foreach (KeyValuePair<Column3D, List<(UnityEvent Event, UnityAction Listener)>> entry in m_ColumnListeners)
            foreach ((UnityEvent unityEvent, UnityAction listener) in entry.Value)
                unityEvent.RemoveListener(listener);

            m_TimelineAnchorStates.Clear();
            Interlocked.Exchange(ref m_PlayingTimelineCount, 0);
            m_SiteStateListeners.Clear();
            m_RoiSelectionListeners.Clear();
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
