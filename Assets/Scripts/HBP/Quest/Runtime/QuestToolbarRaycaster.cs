using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Intersects the curved background and button faces instead of their original flat rectangles.</summary>
    public sealed class QuestToolbarRaycaster : TrackedDeviceRaycaster
    {
        [SerializeField] private QuestToolbarBackground background;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
        {
            int start = results.Count;
            base.Raycast(eventData, results);
            if (background == null) return;
            for (int i = results.Count - 1; i >= start; i--)
                if (results[i].gameObject.TryGetComponent<QuestToolbarBackground>(out _))
                    results.RemoveAt(i);
            if (!(eventData is ExtendedPointerEventData tracked) || tracked.pointerType != UIPointerType.Tracked || eventCamera == null) return;
            var ray = new Ray(tracked.trackedDevicePosition, tracked.trackedDeviceOrientation * Vector3.forward);
            var graphics = GraphicRegistry.GetGraphicsForCanvas(background.canvas);
            for (int i = 0; i < graphics.Count; i++)
                if (graphics[i] is QuestToolbarBackground surface)
                    RaycastSurface(surface, ray, results);
        }

        private void RaycastSurface(QuestToolbarBackground surface, Ray ray, List<RaycastResult> results)
        {
            if (!surface.isActiveAndEnabled || !surface.raycastTarget || surface.canvasRenderer.cull || surface.depth < 0) return;
            if (!surface.Intersect(ray, out float distance, out Vector3 point, out Vector3 normal) || distance > maxDistance) return;
            if (ignoreReversedGraphics && Vector3.Dot(ray.direction, normal) >= 0) return;
            if (checkFor3DOcclusion && Physics.Raycast(ray, distance, blockingMask)) return;
            if (checkFor2DOcclusion && Physics2D.GetRayIntersection(ray, distance, blockingMask).collider != null) return;
            Vector2 screen = eventCamera.WorldToScreenPoint(point);
            if (!surface.Raycast(screen, eventCamera)) return;
            results.Add(new RaycastResult
            {
                gameObject = surface.gameObject, module = this, distance = distance,
                index = results.Count, depth = surface.depth, worldPosition = point, worldNormal = normal, screenPosition = screen
            });
        }
    }
}
