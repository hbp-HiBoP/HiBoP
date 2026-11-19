using System.Collections.Generic;
using HBP.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Routes a world-space canvas through the Quest compositor while retaining its uGUI materials.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class QuestCanvasRenderQueue : MonoBehaviour
    {
        public const int Window = HBPEdgeRendererFeature.QuestWindowRenderQueue;
        public const int Timeline = HBPEdgeRendererFeature.QuestTimelineRenderQueue;

        private readonly Dictionary<Material, Material> materials = new();
        private Canvas canvas;
        private int queue;

        internal static void Set(Canvas target, int queue)
        {
            if (!target.TryGetComponent<QuestCanvasRenderQueue>(out var routing))
                routing = target.gameObject.AddComponent<QuestCanvasRenderQueue>();
            routing.canvas = target;
            routing.queue = queue;
        }

        internal void CopyTo(Canvas target) => Set(target, queue);

        private void OnEnable() => Canvas.preWillRenderCanvases += RouteGraphics;

        private void OnDisable() => Canvas.preWillRenderCanvases -= RouteGraphics;

        private void RouteGraphics()
        {
            if (canvas == null || !canvas.isActiveAndEnabled) return;
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            for (int index = 0; index < graphics.Count; index++)
            {
                var graphic = graphics[index];
                if (graphic == null || !graphic.isActiveAndEnabled) continue;
                var source = graphic.material;
                if (source == null || source.renderQueue == queue) continue;
                if (!materials.TryGetValue(source, out var routed))
                {
                    routed = new Material(source) { hideFlags = HideFlags.HideAndDontSave, renderQueue = queue };
                    materials.Add(source, routed);
                }
                graphic.material = routed;
            }
        }

        private void OnDestroy()
        {
            foreach (var material in materials.Values) Destroy(material);
        }
    }
}
