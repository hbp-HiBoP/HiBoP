using System;
using System.Collections.Generic;
using System.IO;
using HBP.Core.Tools;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Quest
{
    /// <summary>Headset-local window state, independent of projects and Desktop preferences.</summary>
    public sealed class QuestWindowLayoutStore
    {
        public static string DefaultPath = Path.Combine(Application.persistentDataPath, "QuestWindowLayout.json");
        private readonly string path;
        private readonly Dictionary<string, WindowState> states = new(StringComparer.Ordinal);
        private bool writable = true;

        public readonly struct WindowState
        {
            public readonly Pose Pose;
            public readonly Vector2? Size;
            public readonly int? ZoomPercent;
            public readonly bool? Pinned, Open;
            public readonly int? Order;

            public WindowState(Pose pose, Vector2? size = null, int? zoomPercent = null, bool? pinned = null, bool? open = null, int? order = null)
            {
                Pose = pose;
                Size = size;
                ZoomPercent = zoomPercent;
                Pinned = pinned;
                Open = open;
                Order = order;
            }
        }

        [JsonObject(MemberSerialization.OptIn), Preserve]
        public sealed class Document
        {
            [JsonProperty, Preserve] public int Version;
            [JsonProperty, Preserve] public Dictionary<string, Entry> Windows;
        }

        [JsonObject(MemberSerialization.OptIn), Preserve]
        public sealed class Entry
        {
            [JsonProperty, Preserve] public float[] Position;
            [JsonProperty, Preserve] public float[] Rotation;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore), Preserve] public float[] Size;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore), Preserve] public int? ZoomPercent;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore), Preserve] public bool? Pinned;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore), Preserve] public bool? Open;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore), Preserve] public int? Order;
        }

        public QuestWindowLayoutStore(string path)
        {
            this.path = path;
            if (!File.Exists(path)) return;
            try
            {
                var document = ClassLoaderSaver.LoadFromJson<Document>(path);
                if (document != null && document.Version > 2) writable = false;
                if (document == null || document.Version < 1 || document.Version > 2 || document.Windows == null)
                    throw new InvalidDataException("Unsupported or incomplete Quest window layout.");
                foreach (var item in document.Windows)
                {
                    if (string.IsNullOrWhiteSpace(item.Key) || !TryRead(item.Value, out var state))
                    {
                        Debug.LogWarning($"Ignoring invalid Quest window placement '{item.Key}'.");
                        continue;
                    }

                    states.Add(item.Key, state);
                }
            }
            catch (Exception exception)
            {
                // Loading never writes. A newer format stays read-only; malformed files can be replaced by a deliberate move.
                states.Clear();
                Debug.LogWarning($"Unable to load Quest window layout '{path}': {exception.Message}");
            }
        }

        public bool TryGet(string key, out Pose pose)
        {
            bool found = states.TryGetValue(key, out var state);
            pose = state.Pose;
            return found;
        }

        public bool TryGetState(string key, out WindowState state) => states.TryGetValue(key, out state);

        public void Set(string key, Pose pose)
        {
            states.TryGetValue(key, out var previous);
            SetState(key, new WindowState(pose, previous.Size, previous.ZoomPercent, previous.Pinned, previous.Open, previous.Order));
        }

        public void SetState(string key, WindowState state)
        {
            if (string.IsNullOrWhiteSpace(key) || !Finite(state.Pose.position) || !Valid(state.Pose.rotation) || (state.Size.HasValue && !ValidSize(state.Size.Value)) || (state.ZoomPercent.HasValue && !ValidZoom(state.ZoomPercent.Value)) || state.Order < 0)
                throw new ArgumentException("A window state requires a stable key, a finite pose and valid window settings.");
            states[key] = new WindowState(new Pose(state.Pose.position, state.Pose.rotation.normalized), state.Size, state.ZoomPercent, state.Pinned, state.Open, state.Order);
        }

        public void Save()
        {
            if (!writable) return;
            var document = new Document { Version = 2, Windows = new Dictionary<string, Entry>(StringComparer.Ordinal) };
            foreach (var item in states)
            {
                var state = item.Value;
                var p = state.Pose.position;
                var q = state.Pose.rotation;
                document.Windows.Add(item.Key, new Entry
                {
                    Position = new[] { p.x, p.y, p.z }, Rotation = new[] { q.x, q.y, q.z, q.w },
                    Size = state.Size.HasValue ? new[] { state.Size.Value.x, state.Size.Value.y } : null,
                    ZoomPercent = state.ZoomPercent, Pinned = state.Pinned, Open = state.Open, Order = state.Order
                });
            }

            ClassLoaderSaver.SaveToJsonAtomicOrThrow(document, path);
        }

        private static bool TryRead(Entry entry, out WindowState state)
        {
            state = default;
            if (entry?.Position?.Length != 3 || entry.Rotation?.Length != 4) return false;
            var p = new Vector3(entry.Position[0], entry.Position[1], entry.Position[2]);
            var q = new Quaternion(entry.Rotation[0], entry.Rotation[1], entry.Rotation[2], entry.Rotation[3]);
            if (!Finite(p) || !Valid(q)) return false;
            Vector2? size = entry.Size?.Length == 2 && ValidSize(new Vector2(entry.Size[0], entry.Size[1])) ? new Vector2(entry.Size[0], entry.Size[1]) : null;
            int? zoom = entry.ZoomPercent.HasValue && ValidZoom(entry.ZoomPercent.Value) ? entry.ZoomPercent : null;
            int? order = entry.Order >= 0 ? entry.Order : null;
            state = new WindowState(new Pose(p, q.normalized), size, zoom, entry.Pinned, entry.Open, order);
            return true;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        private static bool ValidSize(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && value.x > 0 && value.y > 0;
        private static bool ValidZoom(int value) => value >= QuestWindowContentZoom.MinimumPercent && value <= QuestWindowContentZoom.MaximumPercent && value % QuestWindowContentZoom.StepPercent == 0;

        private static bool Valid(Quaternion value)
        {
            float norm = Quaternion.Dot(value, value);
            return float.IsFinite(norm) && norm > 0.000001f;
        }
    }
}
