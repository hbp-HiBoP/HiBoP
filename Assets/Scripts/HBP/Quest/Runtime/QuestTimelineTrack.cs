using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestTimelineTrack : Slider
    {
        private QuestTimeline timeline;
        private int? pointer;
        private int? lastRequested;

        internal void Bind(QuestTimeline owner) => timeline = owner;

        public override void OnPointerDown(PointerEventData data)
        {
            if (data is not QuestPointerEventData tracked || !IsInteractable() || timeline?.CanInteract != true || pointer.HasValue) return;
            pointer = data.pointerId;
            lastRequested = null;
            Seek(tracked);
        }

        public override void OnDrag(PointerEventData data)
        {
            if (data is QuestPointerEventData tracked && pointer == data.pointerId && timeline.CanInteract) Seek(tracked);
        }

        public override void OnPointerUp(PointerEventData data)
        {
            if (pointer == data.pointerId) pointer = null;
        }

        protected override void OnDisable()
        {
            pointer = null;
            base.OnDisable();
        }

        private void Seek(QuestPointerEventData tracked)
        {
            var rail = handleRect.parent as RectTransform;
            if (!TryGetIndex(rail, tracked.Ray, (int)maxValue, out int index) || index == lastRequested) return;
            lastRequested = index;
            timeline.Seek(index);
        }

        internal static bool TryGetIndex(RectTransform rail, Ray ray, int lastIndex, out int index)
        {
            var plane = new Plane(rail.forward, rail.position);
            index = 0;
            if (!plane.Raycast(ray, out float distance)) return false;
            float x = rail.InverseTransformPoint(ray.GetPoint(distance)).x;
            index = Mathf.RoundToInt(Mathf.InverseLerp(rail.rect.xMin, rail.rect.xMax, x) * lastIndex);
            return true;
        }
    }
}
