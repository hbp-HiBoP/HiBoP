using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HBP.Core.Data;
using HBP.Core.Object3D;
using SceneCut = HBP.Core.Object3D.Cut;

namespace HBP.Sync.Scene
{
    /// <summary>Observes neutral Core mutation events only while a telemetry capture is active.</summary>
    public sealed class SyncSetterOriginObserver : IDisposable
    {
        private readonly HashSet<SiteState> m_Sites;
        private readonly HashSet<SceneCut> m_Cuts;
        private readonly HashSet<BasicTimeline> m_Timelines;
        private SyncSetterOrigin m_SiteColor;
        private SyncSetterOrigin m_CutDefinition;
        private SyncSetterOrigin m_TimelineAnchor;
        private bool m_Disposed;

        public SyncSetterOriginObserver(IEnumerable<SiteState> sites, IEnumerable<SceneCut> cuts, IEnumerable<BasicTimeline> timelines)
        {
            m_Sites = new HashSet<SiteState>(sites ?? throw new ArgumentNullException(nameof(sites)), ReferenceComparer<SiteState>.Instance);
            m_Cuts = new HashSet<SceneCut>(cuts ?? throw new ArgumentNullException(nameof(cuts)), ReferenceComparer<SceneCut>.Instance);
            m_Timelines = new HashSet<BasicTimeline>(timelines ?? throw new ArgumentNullException(nameof(timelines)), ReferenceComparer<BasicTimeline>.Instance);
            SiteState.ColorChanging += OnSiteColorChanging;
            SceneCut.DefinitionChanging += OnCutDefinitionChanging;
            BasicTimeline.AnchorChanging += OnTimelineAnchorChanging;
        }

        public bool TryTake(SyncProfile profile, out SyncTelemetryPoint point)
        {
            return profile switch
            {
                SyncProfile.SiteColor => m_SiteColor.TryTake(out point),
                SyncProfile.CutDefinition => m_CutDefinition.TryTake(out point),
                SyncProfile.TimelineAnchor => m_TimelineAnchor.TryTake(out point),
                _ => throw new ArgumentOutOfRangeException(nameof(profile))
            };
        }

        /// <summary>Keeps cut observation bounded by the currently live scene collection.</summary>
        public void ResetCuts(IEnumerable<SceneCut> cuts)
        {
            if (cuts == null) throw new ArgumentNullException(nameof(cuts));
            m_Cuts.Clear();
            foreach (SceneCut cut in cuts)
                m_Cuts.Add(cut);
        }

        private void OnSiteColorChanging(SiteState site)
        {
            if (m_Sites.Contains(site)) m_SiteColor.CaptureFirst();
        }

        private void OnCutDefinitionChanging(SceneCut cut)
        {
            if (m_Cuts.Contains(cut)) m_CutDefinition.CaptureFirst();
        }

        private void OnTimelineAnchorChanging(BasicTimeline timeline)
        {
            if (m_Timelines.Contains(timeline)) m_TimelineAnchor.CaptureFirst();
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            SiteState.ColorChanging -= OnSiteColorChanging;
            SceneCut.DefinitionChanging -= OnCutDefinitionChanging;
            BasicTimeline.AnchorChanging -= OnTimelineAnchorChanging;
            m_Sites.Clear();
            m_Cuts.Clear();
            m_Timelines.Clear();
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            public static ReferenceComparer<T> Instance { get; } = new();
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
