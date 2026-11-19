using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Virtualized three-state label instructions, with stable ordering while open.</summary>
    public sealed class QuestSitePresetLabelList : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private QuestSitePresetLabelRow rowPrefab;
        [SerializeField] private float rowHeight = 56;
        private readonly List<QuestSitePresetLabelRow> rows = new();
        private string[] values = Array.Empty<string>();
        private Func<string, QuestPresetLabelAction> action;
        private Action<string, QuestPresetLabelAction> changed;

        private void Awake() => scroll.onValueChanged.AddListener(_ => Refresh());

        public void Bind(IEnumerable<string> labels, Func<string, QuestPresetLabelAction> getAction, Action<string, QuestPresetLabelAction> callback)
        {
            values = labels.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            action = getAction;
            changed = callback;
            scroll.verticalNormalizedPosition = 1;
            Resize();
        }

        public void Include(IEnumerable<string> labels)
        {
            values = values.Concat(labels.Except(values, StringComparer.Ordinal)).ToArray();
            Resize();
        }

        private void Resize()
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, values.Length * rowHeight);
            Refresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (scroll != null && action != null) Refresh();
        }

        public void Refresh()
        {
            if (action == null) return;
            int count = Math.Min(values.Length, Mathf.CeilToInt(scroll.viewport.rect.height / rowHeight) + 2);
            while (rows.Count < count) rows.Add(Instantiate(rowPrefab, scroll.content, false));
            int first = Mathf.Clamp(Mathf.FloorToInt(scroll.content.anchoredPosition.y / rowHeight), 0, Math.Max(0, values.Length - count));
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                row.gameObject.SetActive(i < count);
                if (i >= count) continue;
                var rect = (RectTransform)row.transform;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, 1);
                rect.anchoredPosition = new Vector2(0, -(first + i) * rowHeight);
                rect.sizeDelta = new Vector2(0, rowHeight);
                string label = values[first + i];
                row.Bind(label, action(label), changed);
            }
        }
    }
}
