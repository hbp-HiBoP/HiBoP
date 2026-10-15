using System.Collections.Generic;
using HBP.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Text = UnityEngine.UI.Text;

namespace HBP.Quest
{
    /// <summary>Reflects the window state and shows an authored, non-blocking tooltip for either hand.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class QuestToolbarButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string windowKey;
        [SerializeField] private ThemeElement theme;
        [SerializeField] private State activated;
        [SerializeField] private RectTransform tooltip;
        [SerializeField] private Text title;
        [SerializeField, Min(0)] private float tooltipDelay = .2f;
        private readonly HashSet<int> pointers = new();
        private QuestWindowsManager windows;
        private float hoverTime;

        public void Configure(QuestWindowsManager manager) => windows = manager;

        public void PlaceTooltip(QuestToolbarBackground surface, float x, float gap)
        {
            Quaternion rotation = surface.SurfaceRotation(x);
            float y = surface.rectTransform.rect.yMax + gap + tooltip.rect.height * .5f;
            Vector3 position = surface.SurfacePoint(x, y) + rotation * Vector3.back * 6;
            tooltip.SetPositionAndRotation(surface.transform.TransformPoint(position), surface.transform.rotation * rotation);
        }

        private void LateUpdate()
        {
            bool open = !string.IsNullOrEmpty(windowKey) && windows != null && windows.Find(windowKey) is { IsOpen: true };
            State state = open ? activated : theme.Element.DefaultState;
            if (theme.ActualState != state) theme.Set(state);
            if (pointers.Count == 0 || tooltip.gameObject.activeSelf) return;
            hoverTime += Time.unscaledDeltaTime;
            if (hoverTime < tooltipDelay) return;
            tooltip.gameObject.SetActive(true);
            tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp(title.preferredWidth + 24, 80, 240));
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (!isActiveAndEnabled) return;
            if (pointers.Add(data.pointerId) && pointers.Count == 1) hoverTime = 0;
        }

        public void OnPointerExit(PointerEventData data)
        {
            pointers.Remove(data.pointerId);
            if (pointers.Count == 0) HideTooltip();
            // Selectable has one hover flag; keep it set while the other tracked ray remains inside.
            else GetComponent<Button>().OnPointerEnter(data);
        }

        private void OnEnable() => HideTooltip();
        private void OnDisable() => HideTooltip();

        private void HideTooltip()
        {
            pointers.Clear();
            hoverTime = 0;
            if (tooltip != null) tooltip.gameObject.SetActive(false);
        }
    }
}
