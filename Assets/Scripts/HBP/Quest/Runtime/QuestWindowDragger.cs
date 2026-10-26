using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.Quest
{
    public sealed class QuestWindowDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        [SerializeField] private QuestWindow window;
        private int owner = int.MinValue;
        private float distance;
        private Vector3 offset;
        private Transform head;

        public void Configure(QuestWindow ownerWindow, Transform viewer)
        {
            if (isActiveAndEnabled && window != null) window.OnChanged.RemoveListener(WindowChanged);
            End();
            window = ownerWindow;
            head = viewer;
            if (isActiveAndEnabled && window != null) window.OnChanged.AddListener(WindowChanged);
        }

        private void OnEnable()
        {
            if (window != null) window.OnChanged.AddListener(WindowChanged);
        }

        private void WindowChanged(QuestWindow changed)
        {
            if (!changed.IsOpen) End();
        }

        public void OnInitializePotentialDrag(PointerEventData data)
        {
            data.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!isActiveAndEnabled || window == null || !window.IsOpen || window.IsManipulated || !(data is QuestPointerEventData pointer)) return;
            owner = data.pointerId;
            distance = Vector3.Distance(pointer.Ray.origin, data.pointerPressRaycast.worldPosition);
            offset = window.transform.position - pointer.Ray.GetPoint(distance);
            window.SetManipulated(true);
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId == owner && window != null && window.IsOpen && data is QuestPointerEventData pointer)
            {
                window.transform.position = pointer.Ray.GetPoint(distance) + offset;
                if (head != null) window.transform.rotation = QuestWindowFollower.HorizontalRotation(window.transform.position - head.position);
            }
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
            if (window != null) window.OnChanged.RemoveListener(WindowChanged);
            End();
        }
    }
}
