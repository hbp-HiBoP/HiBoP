using System.Collections.Generic;
using System.Linq;
using HBP.Core.Object3D;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Local proximity and exclusive hand capture for cuts shared by the scene's columns.</summary>
    public sealed class QuestCutHandles : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestCutsPanel panel;
        [SerializeField] private QuestPointerInput pointer;
        [SerializeField] private QuestInteractionPolicy policy;
        [SerializeField] private QuestCutGizmo gizmoPrefab;
        private readonly List<QuestCutGizmo> gizmos = new();
        private readonly Drag[] drags = new Drag[2];
        private readonly Vector3[] grip = new Vector3[2], aim = new Vector3[2];
        private readonly bool[] tracked = new bool[2];
        private readonly QuestColumnPresentation[] nearbyColumns = new QuestColumnPresentation[2];
        public IReadOnlyList<QuestCutGizmo> Gizmos => gizmos;
        public bool Active => panel != null && panel.AidsActive;

        private sealed class Drag
        {
            public QuestCutGizmo Gizmo;
            public Vector3 LastPoint, Normal;
            public float Span;
            public bool Flip;
            public HBP.Core.Enums.CutOrientation Orientation;
        }

        public bool IsColumnLocked(QuestAnatomyManipulator column) => drags.Any(drag => drag != null && drag.Gizmo != null && drag.Gizmo.Column.Manipulator == column);
        public bool IsCutLocked(Cut cut) => drags.Any(drag => drag != null && drag.Gizmo != null && drag.Gizmo.Cut == cut);
        public bool IsCaptured(int hand) => drags[hand] != null;

        public void Refresh()
        {
            foreach (var old in gizmos.Where(g => g == null || view == null || g.Scene != view.Scene || !g.IsValid || !view.Columns.Contains(g.Column)).ToArray())
            {
                for (int i = 0; i < drags.Length; i++)
                    if (drags[i]?.Gizmo == old)
                        End(i);
                gizmos.Remove(old);
                if (old != null)
                {
                    old.Show(false, false, false);
                    Destroy(old.gameObject);
                }
            }

            if (!Active)
            {
                CancelAll();
                foreach (var gizmo in gizmos) gizmo.Show(false, false, false);
                return;
            }

            foreach (var column in view.Columns)
            foreach (var cut in view.Scene.Cuts)
                if (!gizmos.Any(g => g.Column == column && g.Cut == cut))
                {
                    var gizmo = Instantiate(gizmoPrefab, column.transform, false);
                    gizmo.Bind(view.Scene, column, cut);
                    gizmos.Add(gizmo);
                }

            foreach (var gizmo in gizmos) gizmo.UpdateGeometry();
        }

        public void SetHand(int hand, bool valid, Vector3 gripPosition, Vector3 aimPosition)
        {
            tracked[hand] = valid;
            grip[hand] = gripPosition;
            aim[hand] = aimPosition;
            if (!valid) End(hand);
            UpdateNearbyColumn(hand);
        }

        private float Distance(QuestCutGizmo gizmo, int hand) => Mathf.Min(gizmo.ContactDistance(grip[hand]), gizmo.ContactDistance(aim[hand]));
        private float PlaneCenterDistance(QuestCutGizmo gizmo, int hand) => Mathf.Min((grip[hand] - gizmo.PlaneCenter).sqrMagnitude, (aim[hand] - gizmo.PlaneCenter).sqrMagnitude);

        public QuestCutGizmo Candidate(int hand)
        {
            if (!Active || !tracked[hand]) return null;
            return gizmos.Where(g => g.IsValid && g.Column == nearbyColumns[hand] && Distance(g, hand) <= policy.CutContactDistance).OrderBy(g => Distance(g, hand)).ThenBy(g => PlaneCenterDistance(g, hand)).ThenBy(g => g.Cut.ID, System.StringComparer.Ordinal).FirstOrDefault();
        }

        private void UpdateNearbyColumn(int hand)
        {
            if (!Active || !tracked[hand])
            {
                nearbyColumns[hand] = null;
                return;
            }

            if (drags[hand] != null)
            {
                nearbyColumns[hand] = drags[hand].Gizmo.Column;
                return;
            }

            // Contact with a bounded plane wins over a neighbouring brain's bounds.
            var contact = gizmos.Where(g => g.IsValid && Distance(g, hand) <= policy.CutContactDistance).OrderBy(g => Distance(g, hand)).ThenBy(g => PlaneCenterDistance(g, hand)).ThenBy(g => CenterDistance(g.Column, hand)).ThenBy(g => g.Column.Column.ColumnData.ID, System.StringComparer.Ordinal).ThenBy(g => g.Cut.ID, System.StringComparer.Ordinal).FirstOrDefault();
            if (contact != null)
            {
                nearbyColumns[hand] = contact.Column;
                return;
            }

            var nearest = gizmos.Where(g => g.IsValid).Select(g => g.Column).Distinct().Where(column => BrainDistance(column, hand) <= (column == nearbyColumns[hand] ? policy.CutHideDistance : policy.CutShowDistance)).OrderBy(column => BrainDistance(column, hand)).ThenBy(column => CenterDistance(column, hand)).ThenBy(column => column.Column.ColumnData.ID, System.StringComparer.Ordinal).FirstOrDefault();
            nearbyColumns[hand] = nearest;
        }

        private float CenterDistance(QuestColumnPresentation column, int hand) => Mathf.Min((grip[hand] - column.Manipulator.GrabCenter).sqrMagnitude, (aim[hand] - column.Manipulator.GrabCenter).sqrMagnitude);
        private float BrainDistance(QuestColumnPresentation column, int hand) => Mathf.Min(Vector3.Distance(grip[hand], column.Manipulator.ClosestGrabPoint(grip[hand])), Vector3.Distance(aim[hand], column.Manipulator.ClosestGrabPoint(aim[hand])));

        public bool Begin(int hand, QuestCutGizmo gizmo)
        {
            if (!Active || !tracked[hand] || gizmo == null || !gizmo.IsValid || Distance(gizmo, hand) > policy.CutContactDistance || IsCutLocked(gizmo.Cut) || gizmo.Column.Manipulator.IsGrabbed || (pointer != null && pointer.IsAnatomyCaptured(gizmo.Column.Manipulator))) return false;
            float span = gizmo.Scene.GetCutPositionGeometry(gizmo.Cut).Span;
            if (!float.IsFinite(span) || span <= 0) return false;
            Vector3 point = gizmo.ContactDistance(grip[hand]) <= gizmo.ContactDistance(aim[hand]) ? grip[hand] : aim[hand];
            drags[hand] = new Drag { Gizmo = gizmo, LastPoint = gizmo.Frame.InverseTransformPoint(point), Normal = gizmo.Cut.Normal.normalized, Span = span, Flip = gizmo.Cut.Flip, Orientation = gizmo.Cut.Orientation };
            // Continue with the same controller reference (grip or aim) for the whole capture.
            useAim[hand] = point == aim[hand];
            return true;
        }

        private readonly bool[] useAim = new bool[2];

        public bool Move(int hand)
        {
            var drag = drags[hand];
            if (drag == null) return false;
            var gizmo = drag.Gizmo;
            if (!Active || !tracked[hand] || gizmo == null || !gizmo.IsValid || gizmo.Scene != view.Scene || !view.Columns.Contains(gizmo.Column))
            {
                End(hand);
                return false;
            }

            var cut = gizmo.Cut;
            Vector3 point = gizmo.Frame.InverseTransformPoint(useAim[hand] ? aim[hand] : grip[hand]);
            float span = gizmo.Scene.GetCutPositionGeometry(cut).Span;
            Vector3 normal = cut.Normal.normalized;
            // A remote orientation/flip change reanchors instead of turning a held gesture into a jump.
            bool sameAxis = normal == drag.Normal && span == drag.Span && cut.Orientation == drag.Orientation && cut.Flip == drag.Flip;
            float delta = sameAxis ? Vector3.Dot(point - drag.LastPoint, normal) / span : 0;
            drag.LastPoint = point;
            drag.Normal = normal;
            drag.Span = span;
            drag.Orientation = cut.Orientation;
            drag.Flip = cut.Flip;
            if (float.IsFinite(delta) && Mathf.Abs(delta) > 1e-7f) QuestCutCommands.Position(gizmo.Scene, cut, cut.Position + delta);
            return true;
        }

        public void End(int hand)
        {
            // Every applied pose already went through the reliable, replaceable SetCutDefinition lane.
            // Do not republish a stale snapshot on release or after a remote deletion/correction.
            drags[hand] = null;
        }

        public void CancelAll()
        {
            for (int i = 0; i < drags.Length; i++) End(i);
        }

        private void LateUpdate()
        {
            Refresh();
            for (int i = 0; i < tracked.Length; i++) UpdateNearbyColumn(i);
            var leftCandidate = drags[0] == null ? Candidate(0) : null;
            var rightCandidate = drags[1] == null ? Candidate(1) : null;
            foreach (var gizmo in gizmos)
            {
                bool captured = drags.Any(d => d?.Gizmo == gizmo);
                // Show one coherent set of cuts per nearby hand/column, rather than isolated
                // handles from several columns according to each plane's attachment point.
                bool visible = Active && (captured || nearbyColumns.Contains(gizmo.Column));
                bool hovered = leftCandidate == gizmo || rightCandidate == gizmo;
                gizmo.Show(visible, captured || hovered, IsCutLocked(gizmo.Cut) && !captured || gizmo.Column.Manipulator.IsGrabbed);
            }
        }

        private void OnDisable()
        {
            CancelAll();
            foreach (var gizmo in gizmos)
                if (gizmo != null)
                    gizmo.Show(false, false, false);
        }
    }
}
