using UnityEngine;
using UnityEngine.EventSystems;

namespace HBP.Quest
{
    public sealed class QuestTimelineMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private QuestTimeline timeline;
        private string description;

        internal void Bind(QuestTimeline owner, string text)
        {
            timeline = owner;
            description = text;
        }

        public void OnPointerEnter(PointerEventData data) => timeline.ShowEvent(this, description, data.pointerId);
        public void OnPointerExit(PointerEventData data) => timeline.HideEvent(this, data.pointerId);
    }
}
