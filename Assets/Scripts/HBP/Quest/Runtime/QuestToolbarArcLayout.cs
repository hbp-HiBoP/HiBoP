using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Sizes the toolbar from its active buttons and places them on a gentle horizontal arc.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class QuestToolbarArcLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private QuestWindow window;
        [SerializeField] private QuestToolbarBackground background;
        [SerializeField, Min(1)] private float buttonWidth = 76, buttonHeight = 72;
        [SerializeField, Min(0)] private float spacing = 4, padding = 4;
        [SerializeField] private float rowHeight;
        [SerializeField, Min(0)] private float tooltipGap = 10;
        [SerializeField, Min(1)] private float radiusMultiplier = 3;
        [SerializeField, Range(5, 90)] private float maximumArc = 60;
        private readonly List<RectTransform> buttons = new();
        private int previousHash;
        private float previousRadius;
        private Vector3 previousAxis;
        private bool dirty = true;

        private void OnEnable() => RebuildLayout();
        private void OnValidate() => dirty = true;
        private void LateUpdate() => Refresh(false);
        public void RebuildLayout() => Refresh(true);

        private void Refresh(bool force)
        {
            if (content == null || window == null || background == null) return;
            int hash = 17, count = 0;
            foreach (Transform child in content)
                if (child.gameObject.activeSelf && child.TryGetComponent<Button>(out _))
                {
                    hash = unchecked(hash * 31 + child.GetEntityId().GetHashCode());
                    count++;
                }

            float width = Mathf.Max(buttonWidth + 2 * padding, count * buttonWidth + Mathf.Max(0, count - 1) * spacing + 2 * padding);
            float scale = Mathf.Max(.0001f, Mathf.Abs(transform.lossyScale.x));
            float radius = Mathf.Max(window.Placement.z * radiusMultiplier / scale, width / (maximumArc * Mathf.Deg2Rad));
            Vector3 axis = transform.InverseTransformDirection(Vector3.up).normalized;
            if (!force && !dirty && hash == previousHash && Mathf.Abs(radius - previousRadius) < .001f && (axis - previousAxis).sqrMagnitude < .000001f) return;
            previousHash = hash;
            previousRadius = radius;
            previousAxis = axis;
            dirty = false;

            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight + 2 * padding);
            background.Configure(radius, axis);
            buttons.Clear();
            foreach (Transform child in content)
                if (child.gameObject.activeSelf && child.TryGetComponent<Button>(out _))
                    buttons.Add((RectTransform)child);
            for (int i = 0; i < buttons.Count; i++)
            {
                var rect = buttons[i];
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                float x = (i - (buttons.Count - 1) * .5f) * (buttonWidth + spacing);
                rect.localPosition = background.SurfacePoint(x, rowHeight);
                rect.localRotation = background.SurfaceRotation(x);
                if (rect.TryGetComponent<QuestToolbarBackground>(out var face)) face.Configure(radius, axis, x, rowHeight);
                if (rect.TryGetComponent<QuestToolbarButton>(out var button)) button.PlaceTooltip(background, x, tooltipGap);
            }
        }
    }
}
