using System;
using System.Collections.Generic;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSignalGraphGraphic : MaskableGraphic
    {
        private SignalGraph graph;
        private IReadOnlyDictionary<string, float[]> resources;

        public void Bind(SignalGraph value, IReadOnlyDictionary<string, float[]> data)
        {
            int vertices = 0;
            if (value != null)
                foreach (var curve in Flatten(value.Curves, true))
                    if (curve.Resource.Length > 0)
                        vertices = checked(vertices + data[curve.Resource].Length / 3 * (curve.HasShape ? 8 : 4));
            if (vertices > 60000) throw new System.IO.InvalidDataException("This graph exceeds the Quest UI rendering capacity. Reduce the displayed curves on Desktop.");
            graph = value;
            resources = data;
            raycastTarget = false;
            SetVerticesDirty();
        }

        private static IEnumerable<SignalCurve> Flatten(IEnumerable<SignalCurve> curves, bool enabled)
        {
            foreach (var curve in curves)
            {
                if (!enabled || !curve.Enabled) continue;
                yield return curve;
                foreach (var child in Flatten(curve.Children, true)) yield return child;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (graph == null) return;
            Rect rect = GetPixelAdjustedRect();
            for (int i = 0; i <= 4; i++)
            {
                float x = Mathf.Lerp(rect.xMin, rect.xMax, i / 4f), y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
                Line(vh, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), new Color(1, 1, 1, .12f), 1);
                Line(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), new Color(1, 1, 1, .12f), 1);
            }

            foreach (var curve in Flatten(graph.Curves, true))
            {
                if (curve.Resource.Length == 0 || !resources.TryGetValue(curve.Resource, out var values)) continue;
                Color c = new(curve.Color[0], curve.Color[1], curve.Color[2], curve.Color[3]);
                for (int i = 3; i < values.Length; i += 3)
                {
                    if (!float.IsFinite(values[i]) || !float.IsFinite(values[i + 1]) || !float.IsFinite(values[i - 2])) continue;
                    Vector2 previous = Point(rect, values[i - 3], values[i - 2]), next = Point(rect, values[i], values[i + 1]);
                    if (curve.HasShape && float.IsFinite(values[i - 1]) && float.IsFinite(values[i + 2]))
                    {
                        var fill = new Color(c.r, c.g, c.b, .5f);
                        // Desktop's vertical shape renderer treats Shapes as the full band thickness.
                        Quad(vh, Point(rect, values[i - 3], values[i - 2] - values[i - 1] * .5f), Point(rect, values[i - 3], values[i - 2] + values[i - 1] * .5f), Point(rect, values[i], values[i + 1] + values[i + 2] * .5f), Point(rect, values[i], values[i + 1] - values[i + 2] * .5f), fill);
                    }

                    Line(vh, previous, next, c, Mathf.Clamp(curve.Thickness, 1, 8));
                }
            }
        }

        private Vector2 Point(Rect r, float x, float y)
        {
            float width = graph.XMax - graph.XMin, height = graph.YMax - graph.YMin;
            return new Vector2(r.xMin + (x - graph.XMin) / (width == 0 ? 1 : width) * r.width, r.yMin + (y - graph.YMin) / (height == 0 ? 1 : height) * r.height);
        }

        internal static void Line(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 perpendicular = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            Quad(vh, a - perpendicular, a + perpendicular, b + perpendicular, b - perpendicular, color);
        }

        internal static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
