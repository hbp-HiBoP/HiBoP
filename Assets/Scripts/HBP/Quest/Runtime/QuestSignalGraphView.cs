using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HBP.Data.Informations.Graphs;
using HBP.Sync;
using HBP.UI.Informations.Graphs;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Read-only adapter around the Desktop graph, including its axes and nested legends.</summary>
    public sealed class QuestSignalGraphView : MonoBehaviour
    {
        [SerializeField] private Graph graph;
        private readonly Dictionary<string, CurveData> curves = new();

        public void Bind(SignalGraph value, IReadOnlyDictionary<string, float[]> resources)
        {
            if (value == null)
            {
                Clear();
                return;
            }

            graph.gameObject.SetActive(true);
            var used = new HashSet<string>();

            Graph.Curve Convert(SignalCurve curve, string path)
            {
                var color = new Color(curve.Color[0], curve.Color[1], curve.Color[2], curve.Color[3]);
                CurveData data = null;
                if (curve.Resource.Length > 0)
                {
                    string style = curve.Thickness.ToString("R", CultureInfo.InvariantCulture) + "|" + string.Join(",", curve.Color.Select(component => component.ToString("R", CultureInfo.InvariantCulture)));
                    string key = path + "|" + curve.Resource + "|" + curve.HasShape + "|" + style;
                    used.Add(key);
                    if (!curves.TryGetValue(key, out data))
                    {
                        var samples = resources[curve.Resource];
                        var points = new Vector2[samples.Length / 3];
                        for (int i = 0; i < points.Length; i++) points[i] = new Vector2(samples[i * 3], samples[i * 3 + 1]);
                        if (curve.HasShape)
                        {
                            var shape = new float[points.Length];
                            for (int i = 0; i < shape.Length; i++) shape[i] = samples[i * 3 + 2];
                            var shaped = ScriptableObject.CreateInstance<ShapedCurveData>();
                            // The Desktop already applied the SEM preference; never consult a local preference here.
                            shaped.Init(points, shape, color, curve.Thickness);
                            data = shaped;
                        }
                        else data = CurveData.CreateInstance(points, color, curve.Thickness);

                        curves.Add(key, data);
                    }

                    data.Color = color;
                    data.Thickness = curve.Thickness;
                }

                return new Graph.Curve(curve.Name, data, curve.Enabled, curve.Id, curve.Children.Select((child, index) => Convert(child, path + "/" + index)).ToArray(), color);
            }

            var roots = value.Curves.Select((curve, index) => Convert(curve, index.ToString())).ToArray();
            graph.DefaultAbscissaDisplayRange = new Vector2(value.XMin, value.XMax);
            graph.DefaultOrdinateDisplayRange = new Vector2(value.YMin, value.YMax);
            graph.AbscissaDisplayRange = new Vector2(value.XMin, value.XMax);
            graph.OrdinateDisplayRange = new Vector2(value.YMin, value.YMax);
            graph.AbscissaLabel = value.XLabel;
            graph.AbscissaUnit = value.XUnit;
            graph.OrdinateLabel = value.YLabel;
            graph.OrdinateUnit = value.YUnit;
            graph.SetCurves(roots);
            foreach (string key in curves.Keys.Where(key => !used.Contains(key)).ToArray())
            {
                Release(curves[key]);
                curves.Remove(key);
            }
        }

        public void SetTime(float value, bool visible)
        {
            graph.DisplayCurrentTime = visible;
            if (visible) graph.CurrentTime = value;
        }

        public void Clear()
        {
            if (graph != null)
            {
                graph.ClearCurves();
                graph.DisplayCurrentTime = false;
                graph.gameObject.SetActive(false);
            }

            foreach (var data in curves.Values) Release(data);
            curves.Clear();
        }

        private static void Release(CurveData data)
        {
            if (Application.isPlaying) Destroy(data);
            else DestroyImmediate(data);
        }

        private void OnDestroy()
        {
            foreach (var data in curves.Values) Release(data);
            curves.Clear();
        }
    }
}
