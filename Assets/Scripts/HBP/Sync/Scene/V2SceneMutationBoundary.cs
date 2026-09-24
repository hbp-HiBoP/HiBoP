using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;
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

        internal V2SceneMutationCheckpoint(IEnumerable<SiteColorCheckpointRecord> siteColors, IEnumerable<CutDefinitionCheckpointRecord> cutDefinitions, IEnumerable<TimelineAnchorCheckpointRecord> timelineAnchors)
        {
            SiteColors = Array.AsReadOnly(siteColors.ToArray());
            CutDefinitions = Array.AsReadOnly(cutDefinitions.ToArray());
            TimelineAnchors = Array.AsReadOnly(timelineAnchors.ToArray());
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
        private readonly Base3DScene m_Scene;
        private readonly V2OriginDevice m_LocalOrigin;
        private readonly IMonotonicClock m_Clock;
        private readonly Action<SceneCut> m_UpdateCut;
        private readonly Func<SetTimelineAnchor, double?> m_TimelineAgeSeconds;
        private bool m_Disposed;

        public event Action<OperationId, V2Mutation, V2OriginDevice> MutationProposed;

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
        public V2SceneMutationBoundary(Base3DScene scene, V2OriginDevice localOrigin, IMonotonicClock clock = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null) : this(CreateSiteTargets(scene), CreateCutTargets(scene), CreateTimelineTargets(scene), localOrigin, clock, cut => scene.UpdateCutPlane(cut, preserveDefinitionNormal: true), timelineAgeSeconds)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            m_Scene = scene;
        }

        /// <summary>Bind explicit fixture targets or a prepared-scene projection of its stable IDs.</summary>
        public V2SceneMutationBoundary(IEnumerable<(SiteState State, ColumnId ColumnId, SiteId SiteId)> sites, IEnumerable<(SceneCut Cut, CutId Id)> cuts, IEnumerable<(BasicTimeline Timeline, ColumnId ColumnId)> timelines, V2OriginDevice localOrigin, IMonotonicClock clock = null, Action<SceneCut> updateCut = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null)
        {
            if (localOrigin != V2OriginDevice.Desktop && localOrigin != V2OriginDevice.Quest)
                throw new ArgumentOutOfRangeException(nameof(localOrigin));
            m_LocalOrigin = localOrigin;
            m_Clock = clock ?? StopwatchMonotonicClock.Instance;
            m_TimelineAgeSeconds = timelineAgeSeconds;
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
                    m_Timelines.Add(target.Timeline, timelineTargets = new List<TimelineTarget>(1));
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

            return new V2SceneMutationCheckpoint(siteRecords, cutRecords, timelineRecords);
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

            using (V2MutationApplicationContext.EnterRemote(operationId))
            {
                foreach (SiteColorCheckpointRecord record in checkpoint.SiteColors) ApplyCore(record.Value);
                foreach (CutDefinitionCheckpointRecord record in checkpoint.CutDefinitions) ApplyCore(record.Value);
                foreach (TimelineAnchorCheckpointRecord record in checkpoint.TimelineAnchors) ApplyCore(record.Value);
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

            throw new ArgumentException("Unsupported v2 scene mutation.", nameof(mutation));
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
            if (m_Disposed || automatic || !m_Timelines.TryGetValue(timeline, out List<TimelineTarget> targets) || ShouldSuppressPublication()) return;
            foreach (TimelineTarget target in targets)
            {
                SetTimelineAnchor mutation;
                try
                {
                    mutation = CreateTimelineAnchor(timeline, target.ColumnId);
                }
                catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
                {
                    // A local value outside the v2 contract is not a publishable mutation.
                    continue;
                }

                Publish(mutation);
            }
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

        private static SetCutDefinition CreateCutDefinition(SceneCut cut, CutId id) => new SetCutDefinition(id, (V2CutOrientation)cut.Orientation, cut.Flip, checked((uint)cut.NumberOfCuts), cut.Position, cut.Normal.x, cut.Normal.y, cut.Normal.z);

        private SetTimelineAnchor CreateTimelineAnchor(BasicTimeline timeline, ColumnId columnId) => new SetTimelineAnchor(columnId, timeline.CurrentIndex, timeline.IsPlaying, timeline.IsLooping, timeline.Step, m_Clock.GetTimestamp(), checked((ulong)m_Clock.Frequency));

        private void ApplyTimelineAnchor(BasicTimeline timeline, SetTimelineAnchor anchor)
        {
            // Monotonic ticks are process-local. Until a valid clock estimate is supplied, seek and start on receipt.
            double elapsed = 0d;
            if (anchor.Playing && m_TimelineAgeSeconds != null)
            {
                double? estimatedAge = m_TimelineAgeSeconds(anchor);
                if (estimatedAge.HasValue && estimatedAge.Value >= 0d && !double.IsNaN(estimatedAge.Value) && !double.IsInfinity(estimatedAge.Value))
                    elapsed = estimatedAge.Value;
            }

            double samplePosition = elapsed * anchor.Step;
            long wholeSteps = samplePosition >= long.MaxValue ? long.MaxValue : (long)Math.Floor(samplePosition);
            int index;
            if (anchor.Looping)
            {
                long offset = wholeSteps % timeline.Length;
                index = (int)((anchor.Index + offset) % timeline.Length);
            }
            else index = wholeSteps >= timeline.Length - anchor.Index ? timeline.Length - 1 : anchor.Index + (int)wholeSteps;

            bool playing = anchor.Playing && (anchor.Looping || index < timeline.Length - 1);
            if (timeline.IsLooping != anchor.Looping) timeline.IsLooping = anchor.Looping;
            if (timeline.Step != anchor.Step) timeline.Step = anchor.Step;
            if (timeline.CurrentIndex != index) timeline.CurrentIndex = index;
            if (timeline.IsPlaying != playing) timeline.IsPlaying = playing;
            double fractionalAge = (samplePosition - Math.Floor(samplePosition)) / anchor.Step;
            float receiverAnchor = playing ? Math.Max(0f, Time.realtimeSinceStartup - (float)fractionalAge) : 0f;
            timeline.ApplySynchronizedClockAnchor(receiverAnchor);
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
