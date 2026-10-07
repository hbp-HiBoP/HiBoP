using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.Quest
{
    /// <summary>Optional ray-operated edge handle. Changes the layout area while keeping the opposite edge fixed.</summary>
    public sealed class QuestWindowResizer : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private QuestWindow window;

        public enum WindowEdge
        {
            Left,
            Right,
            Top,
            Bottom
        }

        [SerializeField] private WindowEdge edge = WindowEdge.Right;
        [SerializeField] private Vector2 minimumSize = new Vector2(500, 350);
        [SerializeField] private Vector2 maximumSize = new Vector2(1200, 1000);
        private RectTransform target;
        private Vector2 initialSize;
        private Vector3 initialPoint;
        private Matrix4x4 localToWorld, worldToLocal;
        private Plane plane;
        private int owner = int.MinValue;
        public WindowEdge Edge => edge;

        private void OnEnable()
        {
            if (window == null) return;
            window.Changed += WindowChanged;
        }

        private void WindowChanged(QuestWindow changed)
        {
            if (!changed.IsOpen) End();
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;

        public void OnBeginDrag(PointerEventData data)
        {
            if (!isActiveAndEnabled || window == null || !window.IsOpen || window.IsManipulated || !(data is QuestPointerEventData pointer)) return;
            target = window.transform as RectTransform;
            if (target == null) return;
            plane = new Plane(target.forward, target.position);
            if (!plane.Raycast(pointer.Ray, out float distance)) return;
            localToWorld = target.localToWorldMatrix;
            worldToLocal = target.worldToLocalMatrix;
            initialPoint = worldToLocal.MultiplyPoint3x4(pointer.Ray.GetPoint(distance));
            initialSize = target.rect.size;
            owner = data.pointerId;
            data.eligibleForClick = false;
            window.SetManipulated(true);
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != owner || window == null || !window.IsOpen || !(data is QuestPointerEventData pointer)) return;
            if (!plane.Raycast(pointer.Ray, out float distance)) return;
            // Use the captured frame, not the resizing panel's moving frame, to avoid feedback and drift.
            Vector3 delta = worldToLocal.MultiplyPoint3x4(pointer.Ray.GetPoint(distance)) - initialPoint;
            bool horizontal = edge == WindowEdge.Left || edge == WindowEdge.Right;
            bool positive = edge == WindowEdge.Right || edge == WindowEdge.Top;
            int axis = horizontal ? 0 : 1;
            float sign = positive ? 1 : -1;
            float minimum = Mathf.Max(1, minimumSize[axis]);
            float size = Mathf.Clamp(initialSize[axis] + sign * delta[axis], minimum, Mathf.Max(minimum, maximumSize[axis]));
            float movement = sign * (size - initialSize[axis]);
            Vector3 pivotOffset = Vector3.zero;
            pivotOffset[axis] = movement * (positive ? target.pivot[axis] : 1 - target.pivot[axis]);
            target.SetSizeWithCurrentAnchors(horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical, size);
            target.position = localToWorld.MultiplyPoint3x4(pivotOffset);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (data.pointerId == owner) End();
        }

        private void End()
        {
            if (owner == int.MinValue) return;
            owner = int.MinValue;
            if (window != null) window.SetManipulated(false);
        }

        private void OnDisable()
        {
            if (window != null) window.Changed -= WindowChanged;
            End();
        }
    }
}
