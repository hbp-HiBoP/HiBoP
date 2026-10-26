using System.Collections.Generic;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Search in the headset's view plane; all distances are metres at the placement plane.</summary>
    public static class QuestSceneLayout
    {
        public const float Distance = .65f;
        public const float Grid = .05f;
        public const float Separation = .02f;

        public readonly struct ViewProjection
        {
            public readonly Pose Head;
            public readonly Rect VisibleArea;

            public ViewProjection(Pose head, Rect visibleArea)
            {
                Head = head;
                VisibleArea = visibleArea;
            }

            public bool TryProject(Bounds bounds, out Rect result)
            {
                Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
                bool inFront = false;
                Quaternion inverse = Quaternion.Inverse(Head.rotation);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 local = inverse * (corner - Head.position);
                    inFront |= local.z > 0;
                    Vector2 point = new Vector2(local.x, local.y) * (Distance / Mathf.Max(.01f, local.z));
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }

                result = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                return inFront;
            }
        }

        public static ViewProjection Projection(Pose head, Camera camera)
        {
            float halfHeight = Distance * Mathf.Tan((camera != null ? camera.fieldOfView : 60) * .5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * (camera != null ? camera.aspect : 1);
            // Five percent inset on each side of the viewport.
            return new ViewProjection(head, Rect.MinMaxRect(-halfWidth * .9f, -halfHeight * .9f, halfWidth * .9f, halfHeight * .9f));
        }

        public static Vector2 FindOffset(Rect bounds, Rect visibleArea, IReadOnlyList<Rect> occupied, System.Func<Vector2, Rect> projectCandidate = null)
        {
            var candidates = new List<Vector2>();
            int width = Mathf.CeilToInt(visibleArea.width / Grid), height = Mathf.CeilToInt(visibleArea.height / Grid);
            for (int x = -width; x <= width; x++)
            for (int y = -height; y <= height; y++)
                candidates.Add(new Vector2(x * Grid, y * Grid));
            candidates.Sort((a, b) =>
            {
                int distance = a.sqrMagnitude.CompareTo(b.sqrMagnitude);
                if (distance != 0) return distance;
                int horizontal = Mathf.Abs(a.y).CompareTo(Mathf.Abs(b.y));
                return horizontal != 0 ? horizontal : a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);
            });
            foreach (var offset in candidates)
            {
                var candidate = projectCandidate != null ? projectCandidate(offset) : new Rect(bounds.position + offset, bounds.size);
                if (candidate.xMin < visibleArea.xMin || candidate.xMax > visibleArea.xMax || candidate.yMin < visibleArea.yMin || candidate.yMax > visibleArea.yMax) continue;
                bool free = true;
                for (int i = 0; i < occupied.Count; i++)
                {
                    var other = occupied[i];
                    other.min -= Vector2.one * Separation;
                    other.max += Vector2.one * Separation;
                    if (candidate.Overlaps(other))
                    {
                        free = false;
                        break;
                    }
                }

                if (free) return offset;
            }

            return Vector2.zero;
        }
    }
}
