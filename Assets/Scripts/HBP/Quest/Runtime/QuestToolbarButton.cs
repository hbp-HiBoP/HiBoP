using System.Collections.Generic;
using HBP.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;

namespace HBP.Quest
{
    /// <summary>Reflects the window state and shows an authored, non-blocking tooltip for either hand.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class QuestToolbarButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const double HoldFeedbackSeconds = .5;
        private const double BringIntoViewSeconds = 2;

        [SerializeField] private string windowKey;
        [SerializeField] private ThemeElement theme;
        [SerializeField] private State activated;
        [SerializeField] private RectTransform tooltip;
        [SerializeField] private Text title;
        [SerializeField] private Image holdProgress;
        [SerializeField, Min(0)] private float tooltipDelay = .2f;
        private readonly HashSet<int> pointers = new();
        private QuestWindowsManager windows;
        private Button button;
        private QuestPointerEventData holdPointer;
        private double holdStarted;
        private float hoverTime;

        private void Awake() => button = GetComponent<Button>();

        public void Configure(QuestWindowsManager manager) => windows = manager;

        internal void SetWindowSortingOrder(int order)
        {
            var canvas = tooltip.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = order + 1;
        }

        public void PlaceTooltip(QuestToolbarBackground surface, float x, float gap)
        {
            Quaternion rotation = surface.SurfaceRotation(x);
            float y = surface.rectTransform.rect.yMax + gap + tooltip.rect.height * .5f;
            Vector3 position = surface.SurfacePoint(x, y) + rotation * Vector3.back * 6;
            tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp(title.preferredWidth + 24, 80, 240));
            // Tangent tooltips on the inclined arc can dip below the middle of the bar.
            // Keep the entire rectangle above its top edge, including both lower corners.
            float bottom = position.y - Mathf.Abs((rotation * Vector3.right).y) * tooltip.rect.width * .5f - Mathf.Abs((rotation * Vector3.up).y) * tooltip.rect.height * .5f;
            position.y += Mathf.Max(0, surface.rectTransform.rect.yMax + gap - bottom);
            tooltip.SetPositionAndRotation(surface.transform.TransformPoint(position), surface.transform.rotation * rotation);
        }

        private void LateUpdate()
        {
            bool open = WindowIsOpen;
            State state = open ? activated : theme.Element.DefaultState;
            if (theme.ActualState != state) theme.Set(state);
            if (holdPointer != null) UpdateHold(open);
            if (pointers.Count == 0 || tooltip.gameObject.activeSelf) return;
            hoverTime += Time.unscaledDeltaTime;
            if (hoverTime < tooltipDelay) return;
            tooltip.gameObject.SetActive(true);
            tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp(title.preferredWidth + 24, 80, 240));
        }

        private bool WindowIsOpen => !string.IsNullOrEmpty(windowKey) && windows != null && windows.Find(windowKey) is { IsOpen: true };

        public void OnPointerDown(PointerEventData data)
        {
            if (!(data is QuestPointerEventData pointer) || data.button != PointerEventData.InputButton.Left ||
                !isActiveAndEnabled || !button.IsInteractable() || holdProgress == null || !WindowIsOpen) return;
            if (holdPointer != null)
            {
                data.eligibleForClick = false;
                return;
            }

            holdPointer = pointer;
            holdStarted = Time.unscaledTimeAsDouble;
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (holdPointer == null || !ReferenceEquals(data, holdPointer)) return;
            if (!holdPointer.Cancelled) UpdateHold(WindowIsOpen);
            EndHold(false);
        }

        private void UpdateHold(bool open)
        {
            if (!open || !button.IsInteractable())
            {
                EndHold(true);
                return;
            }

            double elapsed = Time.unscaledTimeAsDouble - holdStarted;
            if (elapsed < HoldFeedbackSeconds) return;
            holdPointer.eligibleForClick = false;
            if (elapsed >= BringIntoViewSeconds)
            {
                EndHold(true);
                windows.BringIntoView(windowKey);
                return;
            }

            holdProgress.transform.parent.gameObject.SetActive(true);
            holdProgress.fillAmount = (float)((elapsed - HoldFeedbackSeconds) / (BringIntoViewSeconds - HoldFeedbackSeconds));
        }

        private void EndHold(bool consumeClick)
        {
            if (consumeClick && holdPointer != null) holdPointer.eligibleForClick = false;
            holdPointer = null;
            if (holdProgress != null) holdProgress.transform.parent.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (!isActiveAndEnabled) return;
            if (pointers.Add(data.pointerId) && pointers.Count == 1) hoverTime = 0;
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (ReferenceEquals(data, holdPointer)) EndHold(true);
            pointers.Remove(data.pointerId);
            if (pointers.Count == 0) HideTooltip();
            // Selectable has one hover flag; keep it set while the other tracked ray remains inside.
            else GetComponent<Button>().OnPointerEnter(data);
        }

        private void OnEnable() => HideTooltip();
        private void OnDisable()
        {
            EndHold(true);
            HideTooltip();
        }

        private void HideTooltip()
        {
            pointers.Clear();
            hoverTime = 0;
            if (tooltip != null) tooltip.gameObject.SetActive(false);
        }
    }
}
