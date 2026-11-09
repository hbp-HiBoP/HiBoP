using System;
using System.Linq;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.Quest
{
    public sealed class QuestROIHandles : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestROIPanel panel;
        [SerializeField] private QuestPointerInput pointer;
        [SerializeField] private QuestCutHandles cuts;
        [SerializeField] private QuestInteractionPolicy policy;
        [SerializeField] private QuestCutGizmoSettings feedback;
        [SerializeField, Min(.01f)] private float minimumHandDistanceMeters = .08f;
        private readonly Drag[] drags = new Drag[2];
        private readonly Vector3[] grip = new Vector3[2], aim = new Vector3[2];
        private readonly Quaternion[] rotations = new Quaternion[2];
        private readonly bool[] tracked = new bool[2];
        public bool Active => panel != null && panel.ManipulationEnabled;

        private sealed class Drag
        {
            public QuestROISphere Target;
            public ROI ROI;
            public Sphere Sphere;
            public QuestColumnPresentation Column;
            public readonly bool[] Aim = new bool[2];
            public int Hands;
            public bool PairReady;
            public Pose AnchorHand, AnchorSphere;
            public Vector3 AnchorSpan, AnchorPosition, Position;
            public float AnchorRadius, Radius;
            public IDisposable Scope;
        }

        public bool IsCaptured(int hand) => drags[hand] != null;
        public bool IsCapturedInScene(int hand, Base3DScene scene) => drags[hand]?.Column != null && drags[hand].Column.Scene == scene;
        public bool IsColumnLocked(QuestAnatomyManipulator column) => drags.Any(value => value?.Column != null && value.Column.Manipulator == column);
        public bool IsSphereLocked(Sphere sphere) => drags.Any(value => value != null && value.Sphere == sphere);
        private bool CanContinue(Drag drag) => CanTarget(drag.Target) && drag.Target.Column == drag.Column && drag.Target.ROI == drag.ROI && drag.Target.Sphere == drag.Sphere;
        private bool CanTarget(QuestROISphere target) => Active && target != null && target.IsValid && view.CanInteractWithColumn(target.Column) && QuestROICommands.Ready(target.Column.Scene);
        public float Distance(QuestROISphere target, int hand) => Mathf.Min(target.ContactDistance(grip[hand]), target.ContactDistance(aim[hand]));

        public bool ShareCapture(int first, int second) => drags[first] != null && ReferenceEquals(drags[first], drags[second]);

        public QuestROISphere Candidate(int hand, bool allowJoining = true)
        {
            if (!Active || !tracked[hand] || drags[hand] != null) return null;
            var other = drags[1 - hand];
            // As with brain manipulation, a second fresh grip can join a held object
            // without having to squeeze both controllers against a small sphere.
            if (other != null && CanContinue(other) && (allowJoining || Distance(other.Target, hand) <= policy.ROIContactDistance)) return other.Target;
            return view.AllColumns.Where(value => view.CanInteractWithColumn(value)).SelectMany(value => value.ROISpheres).Where(value => CanTarget(value) && !IsSphereLocked(value.Sphere) && !value.Column.Manipulator.IsGrabbed && (pointer == null || !pointer.IsAnatomyCaptured(value.Column.Manipulator)) && (cuts == null || !cuts.IsColumnLocked(value.Column.Manipulator)) && Distance(value, hand) <= policy.ROIContactDistance).OrderBy(value => Distance(value, hand)).ThenBy(value => (value.Frame.TransformPoint(value.Sphere.Position) - grip[hand]).sqrMagnitude).ThenBy(value => value.Column.Column.ColumnData.ID, StringComparer.Ordinal).ThenBy(value => value.Sphere.ID, StringComparer.Ordinal).FirstOrDefault();
        }

        public void SetHand(int hand, bool valid, Vector3 gripPosition, Vector3 aimPosition) => SetHand(hand, valid, gripPosition, aimPosition, Quaternion.identity);

        public void SetHand(int hand, bool valid, Vector3 gripPosition, Vector3 aimPosition, Quaternion rotation)
        {
            tracked[hand] = valid;
            grip[hand] = gripPosition;
            aim[hand] = aimPosition;
            rotations[hand] = rotation;
            if (!valid) End(hand);
        }

        public bool Begin(int hand, QuestROISphere target)
        {
            if (!tracked[hand] || drags[hand] != null || !CanTarget(target)) return false;
            var other = drags[1 - hand];
            if (other != null && CanContinue(other) && other.Target == target)
            {
                other.Aim[hand] = UseAim(target, hand);
                other.Hands |= 1 << hand;
                drags[hand] = other;
                Anchor(other);
                return true;
            }

            if (IsSphereLocked(target.Sphere) || Distance(target, hand) > policy.ROIContactDistance || target.Column.Manipulator.IsGrabbed || pointer != null && pointer.IsAnatomyCaptured(target.Column.Manipulator) || cuts != null && cuts.IsColumnLocked(target.Column.Manipulator) || !target.Column.Scene.TryBeginSensitiveActivityOperation(out var scope)) return false;
            try
            {
                view.SelectContext(target.Column.Scene, target.Column.Column);
                target.ROI.SelectSphere(target.ROI.Spheres.IndexOf(target.Sphere));
                var drag = new Drag { Target = target, ROI = target.ROI, Sphere = target.Sphere, Column = target.Column, Hands = 1 << hand, Scope = scope };
                drag.Aim[hand] = UseAim(target, hand);
                drags[hand] = drag;
                Anchor(drag);
                return true;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        private Vector3 WorldPoint(Drag drag, int hand) => drag.Aim[hand] ? aim[hand] : grip[hand];

        // Like anatomy, prefer the physical grip whenever it can already reach the object.
        private bool UseAim(QuestROISphere target, int hand) => target.ContactDistance(grip[hand]) > policy.ROIContactDistance && target.ContactDistance(aim[hand]) <= policy.ROIContactDistance;

        private void Anchor(Drag drag)
        {
            drag.Position = drag.AnchorPosition = drag.Sphere.Position;
            drag.Radius = drag.AnchorRadius = drag.Sphere.InfluenceRadius;
            drag.AnchorSphere = new Pose(drag.Target.Frame.TransformPoint(drag.Position), drag.Target.transform.rotation);
            if (drag.Hands == 3)
            {
                var left = WorldPoint(drag, 0);
                var right = WorldPoint(drag, 1);
                drag.AnchorHand = new Pose((left + right) * .5f, Quaternion.identity);
                drag.AnchorSpan = right - left;
                drag.PairReady = drag.AnchorSpan.magnitude >= minimumHandDistanceMeters;
            }
            else
            {
                int hand = drag.Hands == 1 ? 0 : 1;
                drag.AnchorHand = new Pose(WorldPoint(drag, hand), rotations[hand]);
            }
        }

        public bool Move(int hand)
        {
            var drag = drags[hand];
            if (drag == null) return false;
            if (!tracked[hand] || !CanContinue(drag))
            {
                CancelGesture(drag);
                return false;
            }

            if (drag.Sphere.Position != drag.Position || drag.Sphere.InfluenceRadius != drag.Radius)
            {
                Anchor(drag);
                return true;
            }

            Vector3 movement;
            var offset = drag.AnchorSphere.position - drag.AnchorHand.position;
            Quaternion rotation;
            float radius = drag.AnchorRadius;
            if (drag.Hands == 3)
            {
                if (Vector3.Distance(WorldPoint(drag, 0), WorldPoint(drag, 1)) < minimumHandDistanceMeters)
                {
                    drag.PairReady = false;
                    return true;
                }

                if (!drag.PairReady)
                {
                    Anchor(drag);
                    return true;
                }

                var left = WorldPoint(drag, 0);
                var right = WorldPoint(drag, 1);
                var span = right - left;
                radius = Mathf.Clamp(drag.AnchorRadius * span.magnitude / drag.AnchorSpan.magnitude, .5f, 100f);
                rotation = Quaternion.FromToRotation(drag.AnchorSpan, span);
                movement = (left + right) * .5f - drag.AnchorHand.position + rotation * offset * (radius / drag.AnchorRadius) - offset;
            }
            else
            {
                int active = drag.Hands == 1 ? 0 : 1;
                rotation = rotations[active] * Quaternion.Inverse(drag.AnchorHand.rotation);
                movement = WorldPoint(drag, active) - drag.AnchorHand.position + rotation * offset - offset;
            }

            // Convert displacement, keeping stationary scientific coordinates exact across a reanchor.
            var position = drag.AnchorPosition + drag.Target.Frame.InverseTransformVector(movement);
            if (!QuestROICommands.Definition(drag.Column.Scene, drag.ROI, drag.Sphere, position, radius))
            {
                CancelGesture(drag);
                return false;
            }

            drag.Position = drag.Sphere.Position;
            drag.Radius = drag.Sphere.InfluenceRadius;
            // Cage orientation is presentation-only; the shared sphere has position and radius.
            drag.Target.transform.rotation = rotation * drag.AnchorSphere.rotation;
            return true;
        }

        public void End(int hand)
        {
            var drag = drags[hand];
            drags[hand] = null;
            if (drag == null) return;
            drag.Hands &= ~(1 << hand);
            if (drag.Hands == 0) drag.Scope.Dispose();
            else if (drag.Sphere != null && CanContinue(drag)) Anchor(drag);
            else CancelGesture(drag);
        }

        private void CancelGesture(Drag drag)
        {
            for (int i = 0; i < drags.Length; i++)
                if (ReferenceEquals(drags[i], drag))
                    drags[i] = null;
            drag.Hands = 0;
            drag.Scope.Dispose();
        }

        public void CancelAll()
        {
            End(0);
            End(1);
        }

        public void CancelScene(Base3DScene scene)
        {
            for (int i = 0; i < 2; i++)
                if (IsCapturedInScene(i, scene))
                    CancelGesture(drags[i]);
        }

        private void LateUpdate()
        {
            if (view == null) return;
            for (int i = 0; i < 2; i++)
                if (drags[i] != null && !CanContinue(drags[i]))
                    CancelGesture(drags[i]);
            var left = drags[0] == null ? Candidate(0) : null;
            var right = drags[1] == null ? Candidate(1) : null;
            foreach (var presentation in view.Scenes)
            {
                bool visible = panel != null && panel.GuidesEnabled;
                if (presentation.Scene.ROIManager.ROICreationMode != visible) presentation.Scene.ROIManager.ROICreationMode = visible;
            }

            foreach (var column in view.AllColumns)
            foreach (var sphere in column.ROISpheres)
            {
                if (sphere == null || !sphere.IsValid) continue;
                bool captured = drags.Any(value => value?.Target == sphere);
                sphere.Feedback(feedback, captured || sphere == left || sphere == right, IsSphereLocked(sphere.Sphere) && !captured || column.Manipulator.IsGrabbed);
            }
        }

        private void OnDisable()
        {
            CancelAll();
            if (view != null)
                foreach (var presentation in view.Scenes)
                    presentation.Scene.ROIManager.ROICreationMode = false;
        }
    }
}
