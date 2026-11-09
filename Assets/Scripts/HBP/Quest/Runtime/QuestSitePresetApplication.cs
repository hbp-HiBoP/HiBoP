using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync;
using UnityEngine;
using UnityEngine.Events;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Quest
{
    /// <summary>One difference record per scene/column/site for the current preset definition.</summary>
    public sealed class QuestSitePresetApplication : IDisposable
    {
        private readonly Dictionary<Base3DScene, SceneHistory> histories = new();
        private Base3DScene scene;
        private Column3D column;
        private Site site;
        private string siteId;
        private QuestSitePreset definition;
        private SceneHistory applyingHistory;
        private Column3D applyingColumn;
        private SiteState applyingState;
        private bool observingRemoval;

        public bool CanRevert => TryGetUndo(out _, out _);

        public void Bind(Base3DScene nextScene, Column3D nextColumn, Site nextSite)
        {
            if (scene == nextScene && column == nextColumn && site == nextSite) return;
            scene = nextScene;
            column = nextColumn;
            site = nextSite;
            siteId = nextSite != null ? nextSite.Information.FullID : null;
        }

        public void Invalidate()
        {
            foreach (var history in histories.Values) history.Dispose();
            histories.Clear();
            definition = null;
            StopObservingRemoval();
        }

        public void ForgetScene(Base3DScene removed)
        {
            if (ReferenceEquals(removed, null) || !histories.Remove(removed, out var history)) return;
            history.Dispose();
            if (histories.Count == 0) StopObservingRemoval();
        }

        private void StopObservingRemoval()
        {
            if (!observingRemoval) return;
            Module3DMain.OnRemoveScene.RemoveListener(ForgetScene);
            observingRemoval = false;
        }

        private bool TryGetUndo(out SceneHistory history, out Difference difference)
        {
            history = null;
            difference = default;
            if (scene == null || scene.IsClosing || column == null || site == null || definition == null || !histories.TryGetValue(scene, out history) || !history.Entries.TryGetValue((column, siteId), out difference)) return false;
            if (difference.Matches(site.State, definition)) return true;
            history.Entries.Remove((column, siteId));
            if (history.Entries.Count == 0)
            {
                ForgetScene(scene);
                history = null;
            }

            return false;
        }

        private void EnsureDefinition(QuestSitePreset preset)
        {
            if (definition != null && definition.Highlight == preset.Highlight && definition.Blacklist == preset.Blacklist && definition.SetColor == preset.SetColor && definition.Color == preset.Color && definition.Labels.Count == preset.Labels.Count && definition.Labels.Zip(preset.Labels, (a, b) => a.Label == b.Label && a.Action == b.Action).All(equal => equal)) return;
            Invalidate();
            // Strings are immutable and shared; this is the only instruction copy, independent of site count.
            definition = new QuestSitePreset
            {
                Highlight = preset.Highlight, Blacklist = preset.Blacklist, SetColor = preset.SetColor, Color = preset.Color,
                Labels = preset.Labels.Select(label => new QuestSitePreset.LabelInstruction { Label = label.Label, Action = label.Action }).ToList()
            };
        }

        public bool ApplyOrRevert(QuestSitePreset preset)
        {
            if (scene == null || scene.IsClosing || scene.IsConfigurationMutationActive || column == null || site == null || scene.SelectedColumn != column || column.SelectedSite != site) return false;
            EnsureDefinition(preset);
            bool reverting = TryGetUndo(out var history, out var difference);
            var targetScene = scene;
            var targetColumn = column;
            var targetSite = site;
            string targetId = siteId;
            var state = targetSite.State;
            var currentDefinition = definition;
            if (!reverting) difference = Difference.Capture(state, currentDefinition);
            if (difference.IsEmpty) return false;
            var desired = reverting ? difference.Restore(state, currentDefinition) : currentDefinition.Configure(state);
            try
            {
                _ = new SetSiteLabels(new ColumnId(targetColumn.ColumnData.ID), new SiteId(targetId), desired.Labels);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("The resulting site labels exceed the supported limit. Shorten or remove labels in the preset.", exception);
            }

            if (history == null)
            {
                if (!histories.TryGetValue(targetScene, out history))
                {
                    history = new SceneHistory(this, targetScene);
                    histories.Add(targetScene, history);
                    if (!observingRemoval)
                    {
                        Module3DMain.OnRemoveScene.AddListener(ForgetScene);
                        observingRemoval = true;
                    }
                }
            }

            history.Observe(targetColumn);
            applyingHistory = history;
            applyingColumn = targetColumn;
            applyingState = state;
            try
            {
                targetScene.ApplySiteConfigurationBatch(new[] { new SiteConfigurationChange(targetColumn, targetId, state, desired) });
                bool applied = Matches(state, desired);
                if (!history.Disposed && ReferenceEquals(definition, currentDefinition))
                {
                    if (applied)
                    {
                        if (reverting) history.Entries.Remove((targetColumn, targetId));
                        else history.Entries[(targetColumn, targetId)] = difference;
                    }
                    else if (!reverting) history.Entries.Remove((targetColumn, targetId));
                }

                return applied;
            }
            finally
            {
                applyingHistory = null;
                applyingColumn = null;
                applyingState = null;
                // A failed rollback or authoritative correction must not leave an unsafe undo.
                if (history.Entries.TryGetValue((targetColumn, targetId), out var retained) && !retained.Matches(state, currentDefinition))
                    history.Entries.Remove((targetColumn, targetId));
                if (!history.Disposed && history.Entries.Count == 0) ForgetScene(targetScene);
            }
        }

        private static bool Matches(SiteState state, SiteConfiguration desired) => state.IsBlackListed == desired.IsBlacklisted && state.IsHighlighted == desired.IsHighlighted && state.Color == desired.Color && state.Labels.SequenceEqual(desired.Labels);

        private void OnStateChanged(SceneHistory history, Column3D ownerColumn, Site changed)
        {
            if (changed == null || history.Disposed || history == applyingHistory && ownerColumn == applyingColumn && ReferenceEquals(changed.State, applyingState)) return;
            var key = (ownerColumn, changed.Information.FullID);
            if (history.Entries.TryGetValue(key, out var difference) && !difference.Matches(changed.State, definition))
            {
                history.Entries.Remove(key);
                if (history.Entries.Count == 0 && history != applyingHistory) ForgetScene(history.Scene);
            }
        }

        public void Dispose()
        {
            Invalidate();
            Bind(null, null, null);
        }

        [Flags]
        private enum ChangedFields : byte
        {
            None = 0,
            Highlight = 1,
            Blacklist = 2,
            Color = 4
        }

        private readonly struct LabelDifference
        {
            public readonly int Instruction;
            public readonly int PreviousIndex; // -1: actually added; otherwise: removed at this position.

            public LabelDifference(int instruction, int previousIndex)
            {
                Instruction = instruction;
                PreviousIndex = previousIndex;
            }
        }

        private readonly struct Difference
        {
            private readonly SiteState state;
            private readonly ChangedFields fields;
            private readonly Color previousColor;
            private readonly LabelDifference[] labels;
            public bool IsEmpty => fields == ChangedFields.None && labels.Length == 0;

            private Difference(SiteState state, ChangedFields fields, Color previousColor, LabelDifference[] labels)
            {
                this.state = state;
                this.fields = fields;
                this.previousColor = previousColor;
                this.labels = labels;
            }

            public static Difference Capture(SiteState state, QuestSitePreset preset)
            {
                var fields = ChangedFields.None;
                if (preset.Highlight != QuestPresetBoolean.Keep && state.IsHighlighted != (preset.Highlight == QuestPresetBoolean.On)) fields |= ChangedFields.Highlight;
                if (preset.Blacklist != QuestPresetBoolean.Keep && state.IsBlackListed != (preset.Blacklist == QuestPresetBoolean.On)) fields |= ChangedFields.Blacklist;
                if (preset.SetColor && state.Color != preset.Color) fields |= ChangedFields.Color;
                List<LabelDifference> labels = null;
                for (int i = 0; i < preset.Labels.Count; i++)
                    if (preset.Labels[i].Action == QuestPresetLabelAction.Add && !state.Labels.Contains(preset.Labels[i].Label))
                        (labels ??= new()).Add(new LabelDifference(i, -1));
                // Original order, rather than instruction order, restores several removed labels correctly.
                for (int position = 0; position < state.Labels.Count; position++)
                for (int i = 0; i < preset.Labels.Count; i++)
                    if (preset.Labels[i].Action == QuestPresetLabelAction.Remove && preset.Labels[i].Label == state.Labels[position])
                    {
                        (labels ??= new()).Add(new LabelDifference(i, position));
                        break;
                    }

                return new Difference(state, fields, (fields & ChangedFields.Color) != 0 ? state.Color : default, labels?.ToArray() ?? Array.Empty<LabelDifference>());
            }

            public bool Matches(SiteState current, QuestSitePreset preset)
            {
                if (!ReferenceEquals(state, current) || preset == null) return false;
                if ((fields & ChangedFields.Highlight) != 0 && current.IsHighlighted != (preset.Highlight == QuestPresetBoolean.On) || (fields & ChangedFields.Blacklist) != 0 && current.IsBlackListed != (preset.Blacklist == QuestPresetBoolean.On) || (fields & ChangedFields.Color) != 0 && current.Color != preset.Color) return false;
                foreach (var label in labels)
                    if (current.Labels.Contains(preset.Labels[label.Instruction].Label) != (label.PreviousIndex < 0))
                        return false;
                return true;
            }

            public SiteConfiguration Restore(SiteState current, QuestSitePreset preset)
            {
                var restored = current.Labels.ToList();
                foreach (var label in labels)
                    if (label.PreviousIndex < 0)
                        restored.Remove(preset.Labels[label.Instruction].Label);
                foreach (var label in labels)
                    if (label.PreviousIndex >= 0)
                        restored.Insert(Math.Min(label.PreviousIndex, restored.Count), preset.Labels[label.Instruction].Label);
                return new SiteConfiguration((fields & ChangedFields.Blacklist) != 0 ? preset.Blacklist != QuestPresetBoolean.On : current.IsBlackListed, (fields & ChangedFields.Highlight) != 0 ? preset.Highlight != QuestPresetBoolean.On : current.IsHighlighted, (fields & ChangedFields.Color) != 0 ? previousColor : current.Color, restored);
            }
        }

        private sealed class SceneHistory : IDisposable
        {
            public readonly Dictionary<(Column3D Column, string SiteId), Difference> Entries = new();
            private readonly Dictionary<Column3D, UnityAction<Site>> listeners = new();
            private readonly QuestSitePresetApplication owner;
            private readonly Base3DScene scene;
            public Base3DScene Scene => scene;
            private readonly ImplantationManager implantation;
            public bool Disposed { get; private set; }

            public SceneHistory(QuestSitePresetApplication owner, Base3DScene scene)
            {
                this.owner = owner;
                this.scene = scene;
                implantation = scene.ImplantationManager;
                scene.OnConfigurationReplacing.AddListener(Reset);
                scene.OnSceneCompletelyLoaded.AddListener(Reset);
                scene.OnClosing.AddListener(Reset);
                if (implantation != null) implantation.OnResourceSelectionChanged.AddListener(Reset);
            }

            public void Observe(Column3D column)
            {
                if (listeners.ContainsKey(column)) return;
                UnityAction<Site> listener = changed => owner.OnStateChanged(this, column, changed);
                listeners.Add(column, listener);
                column.OnChangeSiteState.AddListener(listener);
            }

            private void Reset() => owner.ForgetScene(scene);

            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                foreach (var listener in listeners) listener.Key.OnChangeSiteState.RemoveListener(listener.Value);
                listeners.Clear();
                scene.OnConfigurationReplacing.RemoveListener(Reset);
                scene.OnSceneCompletelyLoaded.RemoveListener(Reset);
                scene.OnClosing.RemoveListener(Reset);
                if (!ReferenceEquals(implantation, null)) implantation.OnResourceSelectionChanged.RemoveListener(Reset);
                Entries.Clear();
            }
        }
    }
}
