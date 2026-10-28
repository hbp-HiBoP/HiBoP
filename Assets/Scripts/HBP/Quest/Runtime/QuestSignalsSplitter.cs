using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalsSplitter : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform available;
        [SerializeField] private LayoutElement graph;
        [SerializeField, Range(.2f, .8f)] private float ratio = .4f;
        private void LateUpdate() => graph.preferredHeight = Mathf.Max(100, available.rect.height * ratio);
        public void OnBeginDrag(PointerEventData data) => data.useDragThreshold = false;

        public void OnDrag(PointerEventData data)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(available, data.position, data.pressEventCamera, out Vector2 local))
                ratio = Mathf.Clamp((available.rect.yMax - local.y) / Mathf.Max(1, available.rect.height), .2f, .8f);
        }
    }
}
