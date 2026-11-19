using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Only viewport rows are instantiated; the catalogue order is frozen until the picker closes.</summary>
    public sealed class QuestLabelList : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private QuestLabelChoice rowPrefab;
        [SerializeField] private float rowHeight = 56;
        private readonly List<QuestLabelChoice> rows = new();
        private string[] values = Array.Empty<string>();
        private Func<string, bool> selected;
        private Action<string, bool> changed;

        public void Bind(string[] labels, Func<string, bool> isSelected, Action<string, bool> callback)
        {
            values = labels;
            selected = isSelected;
            changed = callback;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, values.Length * rowHeight);
            scroll.verticalNormalizedPosition = 1;
            Refresh();
        }

        public void Include(IEnumerable<string> labels)
        {
            var additions = labels.Except(values, StringComparer.Ordinal).ToArray();
            if (additions.Length == 0)
            {
                Refresh();
                return;
            }

            values = values.Concat(additions).ToArray();
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, values.Length * rowHeight);
            Refresh();
        }

        private void Awake() => scroll.onValueChanged.AddListener(_ => Refresh());

        private void OnRectTransformDimensionsChange()
        {
            if (scroll != null && selected != null) Refresh();
        }

        public void Refresh()
        {
            int count = Math.Min(values.Length, Mathf.CeilToInt(scroll.viewport.rect.height / rowHeight) + 2);
            while (rows.Count < count) rows.Add(Instantiate(rowPrefab, scroll.content, false));
            int first = Mathf.Clamp(Mathf.FloorToInt(scroll.content.anchoredPosition.y / rowHeight), 0, Math.Max(0, values.Length - count));
            for (int i = 0; i < rows.Count; i++)
            {
                bool visible = i < count;
                rows[i].gameObject.SetActive(visible);
                if (!visible) continue;
                var rect = (RectTransform)rows[i].transform;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, 1);
                rect.anchoredPosition = new Vector2(0, -(first + i) * rowHeight);
                rect.sizeDelta = new Vector2(0, rowHeight);
                string value = values[first + i];
                rows[i].Bind(value, selected(value), changed);
            }
        }
    }
}
