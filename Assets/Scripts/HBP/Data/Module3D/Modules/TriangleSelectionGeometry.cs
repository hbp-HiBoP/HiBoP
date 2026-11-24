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
            if (!m_IndexReady)
            {
                var faceBounds = new Bounds[Count];
                for (int i = 0; i < Count; i++) faceBounds[i] = FaceBounds(i);
                m_Root = Build(faceBounds, 0, Count);
                m_IndexReady = true;
            }

            Intersect(m_Root, origin, direction, visible, cuts, strongCuts, ref distance, ref face, ref weights);
            return face >= 0;
        }

        private Node m_Root;
        private bool m_IndexReady;

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
