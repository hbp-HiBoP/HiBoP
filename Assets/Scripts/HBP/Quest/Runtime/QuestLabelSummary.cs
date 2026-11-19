using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Fits complete label names into one line; recalculated only on labels or width changes.</summary>
    [ExecuteAlways]
    public sealed class QuestLabelSummary : MonoBehaviour
    {
        [SerializeField] private Text text;
        private string[] labels = Array.Empty<string>();
        private float measuredWidth = -1;

        public void SetLabels(IReadOnlyList<string> values)
        {
            labels = new string[values.Count];
            for (int i = 0; i < values.Count; i++) labels[i] = values[i];
            Refresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (text != null) Refresh();
        }

        private void LateUpdate()
        {
            if (text != null && measuredWidth != text.rectTransform.rect.width) Refresh();
        }

        private void Refresh()
        {
            text.supportRichText = false;
            measuredWidth = text.rectTransform.rect.width;
            var settings = text.GetGenerationSettings(Vector2.zero);
            float width = text.rectTransform.rect.width * text.pixelsPerUnit;
            text.text = Fit(labels, width, value => text.cachedTextGeneratorForLayout.GetPreferredWidth(value, settings));
        }

        internal static string Fit(IReadOnlyList<string> values, float width, Func<string, float> measure)
        {
            if (values.Count == 0) return "No labels";
            string result = "";
            for (int i = 0; i < values.Count; i++)
            {
                string candidate = result.Length == 0 ? values[i] : result + ", " + values[i];
                if (measure(candidate + (i + 1 < values.Count ? ", …" : "")) > width) return result.Length == 0 ? "…" : result + ", …";
                result = candidate;
            }

            return result;
        }
    }
}
