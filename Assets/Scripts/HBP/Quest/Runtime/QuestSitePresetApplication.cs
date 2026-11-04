using System;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync;

namespace HBP.Quest
{
    /// <summary>One reversible application on the current target; never retains a per-site history.</summary>
    public sealed class QuestSitePresetApplication : IDisposable
    {
        private Base3DScene scene;
        private Column3D column;
        private HBP.Core.Object3D.Site site;
        private SiteConfiguration before, after;
        private bool applying;
        public bool CanRevert => before != null;

        public void Bind(Base3DScene nextScene, Column3D nextColumn, HBP.Core.Object3D.Site nextSite)
        {
            if (scene == nextScene && column == nextColumn && site == nextSite) return;
            if (site != null) site.State.OnChangeState.RemoveListener(OnStateChanged);
            Invalidate();
            scene = nextScene;
            column = nextColumn;
            site = nextSite;
            if (site != null) site.State.OnChangeState.AddListener(OnStateChanged);
        }

        public void Invalidate() => before = after = null;

        public bool ApplyOrRevert(QuestSitePreset preset)
        {
            if (site == null || column == null || scene == null || scene.IsClosing || scene.SelectedColumn != column || column.SelectedSite != site) return false;
            OnStateChanged();
            bool reverting = CanRevert;
            var previous = Capture(site.State);
            var desired = reverting ? Restore(site.State) : preset.Configure(site.State);
            if (Equal(previous, desired)) return false;
            // Use the same label bounds offline as the ordinary site editor and synchronized batches.
            try
            {
                _ = new SetSiteLabels(new ColumnId(column.ColumnData.ID), new SiteId(site.Information.FullID), desired.Labels);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("The resulting site labels exceed the supported limit. Shorten or remove labels in the preset.", exception);
            }

            applying = true;
            try
            {
                scene.ApplySiteConfigurationBatch(new[] { new SiteConfigurationChange(column, site.Information.FullID, site.State, desired) });
                if (reverting || !Equal(Capture(site.State), desired)) Invalidate();
                else
                {
                    before = previous;
                    after = desired;
                }

                return true;
            }
            finally
            {
                applying = false;
            }
        }

        private void OnStateChanged()
        {
            if (applying || !CanRevert) return;
            var current = site.State;
            if (before.IsHighlighted != after.IsHighlighted && current.IsHighlighted != after.IsHighlighted || before.IsBlacklisted != after.IsBlacklisted && current.IsBlackListed != after.IsBlacklisted || before.Color != after.Color && current.Color != after.Color || after.Labels.Except(before.Labels).Any(label => !current.Labels.Contains(label)) || before.Labels.Except(after.Labels).Any(label => current.Labels.Contains(label))) Invalidate();
        }

        private SiteConfiguration Restore(SiteState current)
        {
            var labels = current.Labels.ToList();
            foreach (var added in after.Labels.Except(before.Labels)) labels.Remove(added);
            for (int i = 0; i < before.Labels.Length; i++)
                if (!after.Labels.Contains(before.Labels[i]) && !labels.Contains(before.Labels[i]))
                    labels.Insert(Math.Min(i, labels.Count), before.Labels[i]);
            return new SiteConfiguration(before.IsBlacklisted != after.IsBlacklisted ? before.IsBlacklisted : current.IsBlackListed, before.IsHighlighted != after.IsHighlighted ? before.IsHighlighted : current.IsHighlighted, before.Color != after.Color ? before.Color : current.Color, labels);
        }

        private static SiteConfiguration Capture(SiteState state) => new(state.IsBlackListed, state.IsHighlighted, state.Color, state.Labels);
        private static bool Equal(SiteConfiguration a, SiteConfiguration b) => a.IsBlacklisted == b.IsBlacklisted && a.IsHighlighted == b.IsHighlighted && a.Color == b.Color && a.Labels.SequenceEqual(b.Labels);
        public void Dispose() => Bind(null, null, null);
    }
}
