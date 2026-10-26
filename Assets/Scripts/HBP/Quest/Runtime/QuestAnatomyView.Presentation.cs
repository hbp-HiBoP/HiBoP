using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.Quest
{
    public enum QuestColumnDisplayMode
    {
        Single,
        Split
    }

    public sealed partial class QuestAnatomyView
    {
        internal struct ColumnPose
        {
            public Vector3 Position, Scale;
            public Quaternion Rotation;
            public static ColumnPose Read(Transform value) => new() { Position = value.position, Rotation = value.rotation, Scale = value.localScale };

            public void Apply(Transform value)
            {
                value.SetPositionAndRotation(Position, Rotation);
                value.localScale = Scale;
            }

            public static ColumnPose Interpolate(ColumnPose from, ColumnPose to, float t) =>
                new()
                {
                    Position = Vector3.Lerp(from.Position, to.Position, t),
                    Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t),
                    Scale = Vector3.Lerp(from.Scale, to.Scale, t)
                };
        }

        internal sealed class ColumnTransition
        {
            public ColumnPose[] From, To;
            public float Elapsed;
        }

        public sealed partial class Presentation
        {
            public QuestColumnDisplayMode DisplayMode { get; internal set; }
            public bool IsTransitioning => transition != null;
            internal ColumnTransition transition;
            internal ColumnPose commonPose;
            internal QuestColumnPresentation displayed;
        }

        private static QuestColumnPresentation SelectedPresentation(Presentation value) => value.columns.FirstOrDefault(column => column.Column == value.Scene.SelectedColumn) ?? value.columns.FirstOrDefault();

        public bool IsColumnPresented(QuestColumnPresentation column)
        {
            if (column == null || column.Scene == null || column.Scene.IsClosing) return false;
            var owner = Find(column.Scene);
            return owner != null && owner.IsPlaced && owner.Scene.IsVisible && (owner.IsTransitioning || owner.DisplayMode == QuestColumnDisplayMode.Split || column == SelectedPresentation(owner));
        }

        public bool CanInteractWithColumn(QuestColumnPresentation column) => IsColumnPresented(column) && QuestAnatomySession.CanInteractWith(column.Scene) && !Find(column.Scene).IsTransitioning;

        private void InvalidateInteractions(Presentation value)
        {
            foreach (var column in value.columns) column.Manipulator.CancelGrab();
            OnPresentationChanging.Invoke(value.Scene);
        }

        private void OnColumnSelected(Column3D column)
        {
            var owner = scenes.FirstOrDefault(value => value.Scene.Columns.Contains(column));
            if (owner == null) return;
            if (owner.Scene.IsVisible)
            {
                current = owner;
                foreach (var other in scenes)
                    if (other != owner && other.Scene.IsSelected)
                        other.Scene.IsSelected = false;
            }

            SynchronizeSelection(owner);
        }

        private void SynchronizeSelection(Presentation value)
        {
            var selected = SelectedPresentation(value);
            if (selected == value.displayed) return;
            if (!value.IsTransitioning && value.DisplayMode == QuestColumnDisplayMode.Single && value.displayed != null)
            {
                InvalidateInteractions(value);
                value.commonPose = ColumnPose.Read(value.displayed.transform);
                if (selected != null) value.commonPose.Apply(selected.transform);
            }

            value.displayed = selected;
        }

        public bool SetColumnDisplayMode(Base3DScene scene, QuestColumnDisplayMode mode)
        {
            var owner = Find(scene);
            if (owner == null || !owner.IsPlaced || !scene.IsVisible || !QuestAnatomySession.CanInteractWith(scene) || owner.IsTransitioning || owner.columns.Count < 2 || owner.DisplayMode == mode) return false;
            SynchronizeSelection(owner);
            InvalidateInteractions(owner);
            owner.commonPose = ColumnPose.Read(owner.displayed.transform);
            var from = owner.columns.Select(column => ColumnPose.Read(column.transform)).ToArray();
            if (owner.DisplayMode == QuestColumnDisplayMode.Single)
                for (int i = 0; i < from.Length; i++)
                    from[i] = owner.commonPose;
            owner.DisplayMode = mode;
            var to = mode == QuestColumnDisplayMode.Split ? SplitPoses(owner, owner.commonPose, Camera.main != null ? Camera.main.transform.position : owner.commonPose.Position - Vector3.forward) : Enumerable.Repeat(owner.commonPose, from.Length).ToArray();
            owner.transition = new ColumnTransition { From = from, To = to };
            AdvanceTransition(owner, 0);
            return true;
        }

        private static ColumnPose[] SplitPoses(Presentation value, ColumnPose anchor, Vector3 head)
        {
            int count = value.columns.Count;
            var poses = new ColumnPose[count];
            if (count == 0) return poses;
            Vector3 forward = Vector3.ProjectOnPlane(anchor.Position - head, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float diameter = 0;
            foreach (var column in value.columns)
                diameter = Mathf.Max(diameter, column.LayoutDiameter(anchor.Scale.x));
            float radius = count > 1 ? Mathf.Max(.15f * anchor.Scale.x, (diameter + .02f) / (2 * Mathf.Sin(Mathf.PI / count))) : 0;
            for (int i = 0; i < count; i++)
            {
                float angle = 2 * Mathf.PI * i / count;
                poses[i] = anchor;
                poses[i].Position += radius * (Vector3.up * Mathf.Cos(angle) - right * Mathf.Sin(angle));
            }

            return poses;
        }

        private static void AdvanceTransition(Presentation value, float delta)
        {
            var transition = value.transition;
            if (transition == null) return;
            transition.Elapsed = Mathf.Min(1, transition.Elapsed + delta);
            float t = Mathf.SmoothStep(0, 1, transition.Elapsed);
            for (int i = 0; i < value.columns.Count; i++)
                ColumnPose.Interpolate(transition.From[i], transition.To[i], t).Apply(value.columns[i].transform);
            if (transition.Elapsed >= 1) value.transition = null;
        }

        private static void FinishTransition(Presentation value)
        {
            if (value.transition != null) AdvanceTransition(value, 1);
        }

        private static void RestorePresentation(Presentation value, Presentation previous)
        {
            value.displayed = SelectedPresentation(value);
            if (previous == null) return;
            value.DisplayMode = previous.DisplayMode;
            value.commonPose = previous.DisplayMode == QuestColumnDisplayMode.Single && previous.displayed != null ? ColumnPose.Read(previous.displayed.transform) : previous.commonPose;
            if (!value.IsPlaced) return;
            if (value.DisplayMode == QuestColumnDisplayMode.Single)
            {
                foreach (var column in value.columns) value.commonPose.Apply(column.transform);
                return;
            }

            var poses = previous.columns.ToDictionary(column => column.Column.ColumnData.ID, column => ColumnPose.Read(column.transform));
            foreach (var column in value.columns)
                if (poses.TryGetValue(column.Column.ColumnData.ID, out var pose))
                    pose.Apply(column.transform);
            var newColumns = value.columns.Where(column => !poses.ContainsKey(column.Column.ColumnData.ID)).ToArray();
            Vector3 head = Camera.main != null ? Camera.main.transform.position : value.commonPose.Position - Vector3.forward;
            foreach (var column in value.columns) column.RefreshTitle(head);
            var occupied = value.columns.Except(newColumns).Select(column => column.WorldLayoutBounds()).ToList();
            foreach (var column in newColumns)
            {
                value.commonPose.Apply(column.transform);
                float spacing = column.LayoutDiameter(value.commonPose.Scale.x) + .02f;
                for (int step = 1;; step++)
                {
                    float angle = (step % 8) * Mathf.PI / 4;
                    column.transform.position = value.commonPose.Position + spacing * ((step - 1) / 8 + 1) * (Vector3.right * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle));
                    column.RefreshTitle(head);
                    var bounds = column.WorldLayoutBounds();
                    if (occupied.Any(other => other.Intersects(bounds))) continue;
                    occupied.Add(bounds);
                    break;
                }
            }
        }

        public void Place(Presentation value, Pose head, Camera camera, IReadOnlyList<Rect> occupied)
        {
            if (value == null || !scenes.Contains(value) || value.columns.Count == 0) return;
            FinishTransition(value);
            InvalidateInteractions(value);
            var selected = SelectedPresentation(value);
            selected.Manipulator.Recenter(head);
            var anchor = ColumnPose.Read(selected.transform);
            anchor.Position += head.position + head.rotation * Vector3.forward * QuestSceneLayout.Distance - selected.Manipulator.GrabCenter;
            var poses = value.DisplayMode == QuestColumnDisplayMode.Split ? SplitPoses(value, anchor, head.position) : Enumerable.Repeat(anchor, value.columns.Count).ToArray();
            for (int i = 0; i < poses.Length; i++) poses[i].Apply(value.columns[i].transform);
            var projection = QuestSceneLayout.Projection(head, camera);
            TryProjectedBounds(value, projection, out var bounds);
            Vector2 offset = QuestSceneLayout.FindOffset(bounds, projection.VisibleArea, occupied, candidate =>
            {
                TryProjectedBounds(value, projection, out var projected, head.rotation * new Vector3(candidate.x, candidate.y, 0));
                return projected;
            });
            Vector3 translation = head.rotation * new Vector3(offset.x, offset.y, 0);
            foreach (var column in value.columns) column.transform.position += translation;
            anchor.Position += translation;
            value.commonPose = anchor;
            CompletePlacement(value);
        }

        public Rect ProjectedBounds(Presentation value, QuestSceneLayout.ViewProjection projection)
        {
            TryProjectedBounds(value, projection, out var result);
            return result;
        }

        public bool TryProjectedBounds(Presentation value, QuestSceneLayout.ViewProjection projection, out Rect result, Vector3 translation = default)
        {
            result = default;
            bool first = true;
            foreach (var column in value.columns)
            {
                if (value.DisplayMode == QuestColumnDisplayMode.Single && column != SelectedPresentation(value)) continue;
                column.RefreshTitle(projection.Head.position);
                if (!projection.TryProject(column.WorldLayoutBounds(projection.Head.position, translation), out var rect)) continue;
                result = first ? rect : Rect.MinMaxRect(Mathf.Min(result.xMin, rect.xMin), Mathf.Min(result.yMin, rect.yMin), Mathf.Max(result.xMax, rect.xMax), Mathf.Max(result.yMax, rect.yMax));
                first = false;
            }

            return !first;
        }
    }
}
