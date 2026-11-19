using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine.Events;

namespace HBP.Quest
{
    /// <summary>One initial scene scan, then bounded work on the state that actually changed.</summary>
    public sealed class QuestSceneLabelIndex : IDisposable
    {
        private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);
        private readonly Dictionary<SiteState, string[]> labels = new();
        private readonly Dictionary<Column3D, UnityAction<Site>> listeners = new();
        private readonly Base3DScene scene;
        public int Revision { get; private set; }
        public string[] Snapshot() => counts.Keys.OrderBy(s => s, StringComparer.Ordinal).ToArray();

        internal QuestSceneLabelIndex()
        {
        }

        public QuestSceneLabelIndex(Base3DScene scene) : this()
        {
            this.scene = scene;
            Rebuild();
            scene.OnSceneCompletelyLoaded.AddListener(Rebuild);
            scene.ImplantationManager.OnResourceSelectionChanged.AddListener(Rebuild);
        }

        private void Rebuild()
        {
            Detach();
            counts.Clear();
            labels.Clear();
            foreach (var state in scene.Columns.SelectMany(c => c.Sites).Select(s => s.State).Distinct()) Update(state);
            foreach (var column in scene.Columns)
            {
                UnityAction<Site> listener = site => Update(site.State);
                listeners.Add(column, listener);
                column.OnChangeSiteState.AddListener(listener);
            }

            Revision++;
        }

        private void Update(SiteState state)
        {
            string[] next = state.Labels.Distinct(StringComparer.Ordinal).ToArray();
            if (next.Length == 0 && !labels.ContainsKey(state)) return;
            if (labels.TryGetValue(state, out var previous))
            {
                if (previous.SequenceEqual(next)) return;
                foreach (string label in previous)
                {
                    if (--counts[label] == 0) counts.Remove(label);
                }
            }

            foreach (string label in next)
            {
                counts.TryGetValue(label, out int count);
                counts[label] = count + 1;
            }

            if (next.Length == 0) labels.Remove(state);
            else labels[state] = next;
            Revision++;
        }

        private void Detach()
        {
            foreach (var pair in listeners)
                if (pair.Key != null)
                    pair.Key.OnChangeSiteState.RemoveListener(pair.Value);
            listeners.Clear();
        }

        public void Dispose()
        {
            Detach();
            counts.Clear();
            labels.Clear();
            if (scene != null)
            {
                scene.OnSceneCompletelyLoaded.RemoveListener(Rebuild);
                scene.ImplantationManager.OnResourceSelectionChanged.RemoveListener(Rebuild);
            }
        }
    }
}
