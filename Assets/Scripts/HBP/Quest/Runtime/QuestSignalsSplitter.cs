using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalsSplitter : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform available;
        [SerializeField] private LayoutElement graph, matrix, handle;
        [SerializeField] private QuestWindowContentZoom zoom;
        [SerializeField, Range(0, 1)] private float ratio = .4f;
        [SerializeField, Range(0, .2f)] private float snapThreshold = .08f;
        [SerializeField, Min(1)] private float handleHeight = 28;
        public float Ratio => ratio;

        private void LateUpdate() => Refresh();

        public void SetRatio(float value)
        {
            ratio = value <= snapThreshold ? 0 : value >= 1 - snapThreshold ? 1 : Mathf.Clamp01(value);
            Refresh();
        }

        public void Refresh()
        {
            if (available == null || graph == null || matrix == null || handle == null) return;
            graph.gameObject.SetActive(ratio > 0);
            matrix.gameObject.SetActive(ratio < 1);
            // Keep the full-width grab area usable even when the content is zoomed out.
            handle.minHeight = handle.preferredHeight = handleHeight / (zoom != null ? zoom.Factor : 1);
            var layout = available.GetComponent<VerticalLayoutGroup>();
            float spacing = layout != null ? layout.spacing * (ratio > 0 && ratio < 1 ? 2 : 1) : 0;
            float padding = layout != null ? layout.padding.vertical : 0;
            float height = Mathf.Max(0, available.rect.height - padding - handle.preferredHeight - spacing);
            graph.preferredHeight = height * ratio;
            graph.flexibleHeight = ratio == 1 ? 1 : 0;
        }

        public void OnBeginDrag(PointerEventData data) => data.useDragThreshold = false;

        public void OnDrag(PointerEventData data)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(available, data.position, data.pressEventCamera, out Vector2 local))
            {
                var layout = available.GetComponent<VerticalLayoutGroup>();
                float top = layout != null ? layout.padding.top : 0;
                float padding = layout != null ? layout.padding.vertical : 0;
                float height = handle != null ? handle.preferredHeight : 0;
                SetRatio((available.rect.yMax - top - height * .5f - local.y) / Mathf.Max(1, available.rect.height - padding - height));
            }
        }
    }
}
