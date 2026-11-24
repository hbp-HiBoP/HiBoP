using System;
using System.Collections.Generic;
using HBP.Core.Object3D;
using UnityEngine;

namespace HBP.Data.Module3D
{
    /// <summary>Original face IDs and spatial queries for one displayed surface.</summary>
    internal sealed class TriangleSelectionGeometry
    {
        private const int LeafSize = 12;
        private const int MaxCutPlanes = 20;

        private readonly struct Node
        {
            public readonly Bounds Bounds;
            public readonly int Start, Count, Left, Right;

            public Node(Bounds bounds, int start, int count, int left, int right)
            {
                Bounds = bounds;
                Start = start;
                Count = count;
                Left = left;
                Right = right;
            }
        }

        public readonly Vector3[] Vertices;
        public readonly int[] Triangles;
        private readonly int[] m_Faces;
        private readonly List<Node> m_Nodes = new();
        private int[][] m_Neighbors;
        public int Count => m_Faces.Length;

        public TriangleSelectionGeometry(Vector3[] vertices, int[] triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
            m_Faces = new int[triangles.Length / 3];
            for (int face = 0; face < Count; face++) m_Faces[face] = face;
        }

        private Node Build(Bounds[] faceBounds, int start, int count)
        {
            Bounds bounds = faceBounds[m_Faces[start]];
            for (int i = start + 1; i < start + count; i++)
                bounds.Encapsulate(faceBounds[m_Faces[i]]);

            if (count <= LeafSize) return new Node(bounds, start, count, -1, -1);

            Vector3 size = bounds.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            float midpoint = bounds.center[axis];
            int split = start;
            int end = start + count - 1;
            while (split <= end)
            {
                if (faceBounds[m_Faces[split]].center[axis] <= midpoint)
                    split++;
                else
                {
                    (m_Faces[split], m_Faces[end]) = (m_Faces[end], m_Faces[split]);
                    end--;
                }
            }

            int leftCount = split - start;
            if (leftCount == 0 || leftCount == count) leftCount = count / 2;
            int left = m_Nodes.Count;
            m_Nodes.Add(default);
            int right = m_Nodes.Count;
            m_Nodes.Add(default);
            m_Nodes[left] = Build(faceBounds, start, leftCount);
            m_Nodes[right] = Build(faceBounds, start + leftCount, count - leftCount);
            return new Node(bounds, 0, 0, left, right);
        }

        private Bounds FaceBounds(int face)
        {
            int index = face * 3;
            var bounds = new Bounds(Vertices[Triangles[index]], Vector3.zero);
            bounds.Encapsulate(Vertices[Triangles[index + 1]]);
            bounds.Encapsulate(Vertices[Triangles[index + 2]]);
            return bounds;
        }

        public Vector3 Normal(int face)
        {
            int index = face * 3;
            Vector3 a = Vertices[Triangles[index]];
            return Vector3.Cross(Vertices[Triangles[index + 1]] - a, Vertices[Triangles[index + 2]] - a).normalized;
        }

        public int[] Neighbors(int face)
        {
            if (m_Neighbors == null)
            {
                var edges = new Dictionary<(int, int), List<int>>();
                var neighbors = new List<int>[Count];
                for (int i = 0; i < Count; i++)
                {
                    neighbors[i] = new List<int>(3);
                    for (int corner = 0; corner < 3; corner++)
                    {
                        int a = Triangles[i * 3 + corner], b = Triangles[i * 3 + (corner + 1) % 3];
                        if (a == b) continue;
                        var edge = a < b ? (a, b) : (b, a);
                        if (!edges.TryGetValue(edge, out List<int> faces)) edges.Add(edge, faces = new List<int>(2));
                        foreach (int other in faces)
                        {
                            if (other == i) continue;
                            neighbors[i].Add(other);
                            neighbors[other].Add(i);
                        }

                        faces.Add(i);
                    }
                }

                m_Neighbors = Array.ConvertAll(neighbors, faces => faces.ToArray());
            }

            return m_Neighbors[face];
        }

        public bool Raycast(Vector3 origin, Vector3 direction, int[] visible, List<Cut> cuts, bool strongCuts, ref float distance, out int face, out Vector3 weights)
        {
            face = -1;
            weights = default;
            if (Count == 0) return false;
            EnsureIndex();
            Intersect(m_Root, origin, direction, visible, cuts, strongCuts, ref distance, ref face, ref weights);
            return face >= 0;
        }

        private Node m_Root;
        private bool m_IndexReady;

        private void EnsureIndex()
        {
            if (m_IndexReady) return;
            var faceBounds = new Bounds[Count];
            for (int i = 0; i < Count; i++) faceBounds[i] = FaceBounds(i);
            m_Root = Build(faceBounds, 0, Count);
            m_IndexReady = true;
        }

        public bool Contact(Vector3 center, Matrix4x4 localToWorld, Matrix4x4 worldToLocal, List<Cut> cuts, bool strongCuts, ref float distance, out int face)
        {
            face = -1;
            if (Count == 0) return false;
            EnsureIndex();
            // Each row gives the maximal extent of the world sphere along one local axis.
            var extent = new Vector3(RowLength(0), RowLength(1), RowLength(2)) * distance;
            var bounds = new Bounds(worldToLocal.MultiplyPoint3x4(center), extent * 2f);
            float best = distance * distance;
            Visit(m_Root, bounds, center, localToWorld, worldToLocal, cuts, strongCuts, ref best, ref face);
            if (face < 0) return false;
            distance = Mathf.Sqrt(best);
            return true;

            float RowLength(int row) => new Vector3(worldToLocal[row, 0], worldToLocal[row, 1], worldToLocal[row, 2]).magnitude;
        }

        private void Visit(Node node, Bounds bounds, Vector3 center, Matrix4x4 toWorld, Matrix4x4 toLocal, List<Cut> cuts, bool strongCuts, ref float best, ref int face)
        {
            if (!node.Bounds.Intersects(bounds)) return;
            if (node.Left >= 0)
            {
                Visit(m_Nodes[node.Left], bounds, center, toWorld, toLocal, cuts, strongCuts, ref best, ref face);
                Visit(m_Nodes[node.Right], bounds, center, toWorld, toLocal, cuts, strongCuts, ref best, ref face);
                return;
            }

            for (int i = node.Start; i < node.Start + node.Count; i++)
            {
                int candidate = m_Faces[i];
                int offset = candidate * 3;
                var a = Vertices[Triangles[offset]];
                var b = Vertices[Triangles[offset + 1]];
                var c = Vertices[Triangles[offset + 2]];
                var closest = ClosestPoint(center, toWorld.MultiplyPoint3x4(a), toWorld.MultiplyPoint3x4(b), toWorld.MultiplyPoint3x4(c));
                float squared = (center - closest).sqrMagnitude;
                if (squared > best) continue;
                if (!InCuts(toLocal.MultiplyPoint3x4(closest), cuts, strongCuts))
                    squared = ClippedDistance(center, toWorld, a, b, c, cuts, strongCuts);
                if (squared > best || squared == best && face >= 0 && candidate >= face) continue;
                best = squared;
                face = candidate;
            }
        }

        private static float ClippedDistance(Vector3 center, Matrix4x4 toWorld, Vector3 a, Vector3 b, Vector3 c, List<Cut> cuts, bool strongCuts)
        {
            Span<Vector3> polygon = stackalloc Vector3[3 + MaxCutPlanes + 1];
            Span<Vector3> clipped = stackalloc Vector3[3 + MaxCutPlanes + 1];
            polygon[0] = a;
            polygon[1] = b;
            polygon[2] = c;
            int count = 3;
            float best = float.PositiveInfinity;
            for (int i = 0; i < Mathf.Min(MaxCutPlanes, cuts.Count); i++)
            {
                int size = ClipPolygon(polygon[..count], clipped, cuts[i]);
                if (!strongCuts)
                {
                    best = Mathf.Min(best, PolygonDistance(center, toWorld, clipped[..size]));
                    continue;
                }

                if (size == 0) return float.PositiveInfinity;
                clipped[..size].CopyTo(polygon);
                count = size;
            }

            return strongCuts ? PolygonDistance(center, toWorld, polygon[..count]) : best;
        }

        private static int ClipPolygon(ReadOnlySpan<Vector3> polygon, Span<Vector3> clipped, Cut cut)
        {
            int count = 0;
            Vector3 previous = polygon[^1];
            float before = Vector3.Dot(cut.Normal, cut.Point - previous);
            foreach (var current in polygon)
            {
                float after = Vector3.Dot(cut.Normal, cut.Point - current);
                if ((before < 0f && after > 0f) || (before > 0f && after < 0f))
                    clipped[count++] = Vector3.LerpUnclamped(previous, current, before / (before - after));
                if (after >= 0f) clipped[count++] = current;
                previous = current;
                before = after;
            }

            return count;
        }

        private static float PolygonDistance(Vector3 center, Matrix4x4 toWorld, ReadOnlySpan<Vector3> polygon)
        {
            if (polygon.Length == 0) return float.PositiveInfinity;
            var a = toWorld.MultiplyPoint3x4(polygon[0]);
            var b = toWorld.MultiplyPoint3x4(polygon[Mathf.Min(1, polygon.Length - 1)]);
            if (polygon.Length < 3) return (center - ClosestOnEdge(center, a, b)).sqrMagnitude;
            float best = float.PositiveInfinity;
            for (int i = 2; i < polygon.Length; i++)
            {
                var c = toWorld.MultiplyPoint3x4(polygon[i]);
                best = Mathf.Min(best, (center - ClosestPoint(center, a, b, c)).sqrMagnitude);
                b = c;
            }

            return best;
        }

        private static Vector3 ClosestPoint(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a);
            float areaSquared = normal.sqrMagnitude;
            if (areaSquared > 0f)
            {
                var projected = point - normal * (Vector3.Dot(point - a, normal) / areaSquared);
                if (Vector3.Dot(normal, Vector3.Cross(b - a, projected - a)) >= 0f && Vector3.Dot(normal, Vector3.Cross(c - b, projected - b)) >= 0f && Vector3.Dot(normal, Vector3.Cross(a - c, projected - c)) >= 0f)
                    return projected;
            }

            var ab = ClosestOnEdge(point, a, b);
            var bc = ClosestOnEdge(point, b, c);
            var ca = ClosestOnEdge(point, c, a);
            var nearest = (point - ab).sqrMagnitude <= (point - bc).sqrMagnitude ? ab : bc;
            return (point - nearest).sqrMagnitude <= (point - ca).sqrMagnitude ? nearest : ca;
        }

        private static Vector3 ClosestOnEdge(Vector3 point, Vector3 a, Vector3 b)
        {
            var edge = b - a;
            float length = edge.sqrMagnitude;
            return length > 0f ? a + edge * Mathf.Clamp01(Vector3.Dot(point - a, edge) / length) : a;
        }

        private void Intersect(Node node, Vector3 origin, Vector3 direction, int[] visible, List<Cut> cuts, bool strongCuts, ref float distance, ref int face, ref Vector3 weights)
        {
            if (!IntersectsBounds(node.Bounds, origin, direction, distance)) return;
            if (node.Left >= 0)
            {
                Intersect(m_Nodes[node.Left], origin, direction, visible, cuts, strongCuts, ref distance, ref face, ref weights);
                Intersect(m_Nodes[node.Right], origin, direction, visible, cuts, strongCuts, ref distance, ref face, ref weights);
                return;
            }

            for (int i = node.Start; i < node.Start + node.Count; i++)
            {
                int candidate = m_Faces[i];
                if (visible != null && visible[candidate] == 0) continue;
                int offset = candidate * 3;
                Vector3 a = Vertices[Triangles[offset]];
                Vector3 ab = Vertices[Triangles[offset + 1]] - a;
                Vector3 ac = Vertices[Triangles[offset + 2]] - a;
                Vector3 p = Vector3.Cross(direction, ac);
                float determinant = Vector3.Dot(ab, p);
                if (determinant <= 0f) continue;

                Vector3 fromA = origin - a;
                float u = Vector3.Dot(fromA, p) / determinant;
                if (u < 0f || u > 1f) continue;
                Vector3 q = Vector3.Cross(fromA, ab);
                float v = Vector3.Dot(direction, q) / determinant;
                if (v < 0f || u + v > 1f) continue;

                float hit = Vector3.Dot(ac, q) / determinant;
                if (hit < 0f || hit > distance || (hit == distance && face >= 0 && candidate >= face)) continue;
                if (!InCuts(origin + direction * hit, cuts, strongCuts)) continue;
                distance = hit;
                face = candidate;
                weights = new Vector3(1f - u - v, u, v);
            }
        }

        private static bool IntersectsBounds(Bounds bounds, Vector3 origin, Vector3 direction, float maximum)
        {
            float minimum = 0f;
            for (int axis = 0; axis < 3; axis++)
            {
                float low = bounds.min[axis] - origin[axis], high = bounds.max[axis] - origin[axis];
                float delta = direction[axis];
                if (delta == 0f)
                {
                    if (low > 0f || high < 0f) return false;
                    continue;
                }

                float entry = low / delta, exit = high / delta;
                if (entry > exit) (entry, exit) = (exit, entry);
                minimum = Mathf.Max(minimum, entry);
                maximum = Mathf.Min(maximum, exit);
                if (minimum > maximum) return false;
            }

            return true;
        }

        private static bool InCuts(Vector3 point, List<Cut> cuts, bool strongCuts)
        {
            if (cuts == null || cuts.Count == 0) return true;
            int count = Mathf.Min(MaxCutPlanes, cuts.Count);
            for (int i = 0; i < count; i++)
            {
                bool inside = Vector3.Dot(cuts[i].Normal, cuts[i].Point - point) >= 0f;
                if (inside != strongCuts) return inside;
            }

            return strongCuts;
        }
    }
}
