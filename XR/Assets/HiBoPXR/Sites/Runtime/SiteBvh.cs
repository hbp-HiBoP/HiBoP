using System;
using System.Collections.Generic;
using System.Linq;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using UnityEngine;

namespace CRNL.HiBoP.XR.Sites
{
    internal sealed class SiteBvh
    {
        private const int LeafSize = 8;
        private const float TieEpsilonMillimeters = 0.00001f;

        private readonly Vector3[] m_Positions;
        private readonly ContractId[] m_SiteIds;
        private readonly Dictionary<ContractId, int> m_IndexById;
        private readonly int[] m_Indices;
        private readonly int[] m_LeafByIndex;
        private readonly List<Node> m_Nodes;
        private readonly List<int> m_ParentByNode;
        private readonly int[] m_QueryStack;

        public SiteBvh(SiteAsset asset)
        {
            int count = asset.SiteIds.Count;
            m_Positions = new Vector3[count];
            m_SiteIds = new ContractId[count];
            m_IndexById = new Dictionary<ContractId, int>(count);
            m_Indices = Enumerable.Range(0, count).ToArray();
            m_LeafByIndex = new int[count];
            m_Nodes = new List<Node>(count * 2);
            m_ParentByNode = new List<int>(count * 2);
            for (int index = 0; index < count; index++)
            {
                Float3 source = asset.Positions[index];
                m_Positions[index] = new Vector3(source.X, source.Y, source.Z);
                m_SiteIds[index] = asset.SiteIds[index];
                m_IndexById.Add(m_SiteIds[index], index);
            }

            Build(0, count);
            m_QueryStack = new int[m_Nodes.Count];
        }

        public int Count => m_Positions.Length;

        public int NodeCount => m_Nodes.Count;

        public Vector3 Position(int index) => m_Positions[index];

        public bool TryGetIndex(ContractId siteId, out int index) => m_IndexById.TryGetValue(siteId, out index);

        public SitePickResult Raycast(Vector3 origin, Vector3 direction, float maximumDistance, float minimumRadius, float[] radii, byte[] visibility, float[] nodeMaximumRadii)
        {
            if (direction.sqrMagnitude <= 0f)
                throw new ArgumentOutOfRangeException(nameof(direction));
            direction.Normalize();
            int best = -1;
            float bestRayDistance = maximumDistance;
            float bestAxisDistance = float.PositiveInfinity;
            int stackCount = 1;
            m_QueryStack[0] = 0;
            while (stackCount > 0)
            {
                Node node = m_Nodes[m_QueryStack[--stackCount]];
                float broadRadius = nodeMaximumRadii[node.Index];
                if (broadRadius <= 0f)
                    continue;
                Bounds expanded = node.Bounds;
                expanded.Expand(Mathf.Max(broadRadius, minimumRadius) * 2f);
                if (!expanded.IntersectRay(new Ray(origin, direction), out float entry) || entry > bestRayDistance + TieEpsilonMillimeters)
                    continue;
                if (node.Count == 0)
                {
                    m_QueryStack[stackCount++] = node.Left;
                    m_QueryStack[stackCount++] = node.Right;
                    continue;
                }

                for (int offset = 0; offset < node.Count; offset++)
                {
                    int index = m_Indices[node.Start + offset];
                    if (visibility[index] == 0 || radii[index] <= 0f)
                        continue;
                    float radius = Mathf.Max(radii[index], minimumRadius);
                    Vector3 toOrigin = origin - m_Positions[index];
                    float b = Vector3.Dot(toOrigin, direction);
                    float c = Vector3.Dot(toOrigin, toOrigin) - radius * radius;
                    float discriminant = b * b - c;
                    if (discriminant < 0f)
                        continue;
                    float squareRoot = Mathf.Sqrt(discriminant);
                    float distance = -b - squareRoot;
                    if (distance < 0f)
                        distance = -b + squareRoot;
                    if (distance < 0f || distance > maximumDistance)
                        continue;
                    Vector3 centerOffset = m_Positions[index] - origin;
                    float along = Vector3.Dot(centerOffset, direction);
                    float axisDistance = (centerOffset - direction * along).magnitude;
                    if (IsBetterRay(index, distance, axisDistance, best, bestRayDistance, bestAxisDistance))
                    {
                        best = index;
                        bestRayDistance = distance;
                        bestAxisDistance = axisDistance;
                    }
                }
            }

            if (best < 0)
                return SitePickResult.None;
            Vector3 hitPoint = origin + direction * bestRayDistance;
            return new SitePickResult(best, m_SiteIds[best], bestRayDistance, 0f, hitPoint);
        }

        public SitePickResult FindNearest(Vector3 point, float maximumSurfaceDistance, float[] radii, byte[] visibility, float[] nodeMaximumRadii)
        {
            int best = -1;
            float bestSurfaceDistance = maximumSurfaceDistance;
            float bestCenterDistance = float.PositiveInfinity;
            int stackCount = 1;
            m_QueryStack[0] = 0;
            while (stackCount > 0)
            {
                Node node = m_Nodes[m_QueryStack[--stackCount]];
                float broadRadius = nodeMaximumRadii[node.Index];
                if (broadRadius <= 0f)
                    continue;
                Bounds expanded = node.Bounds;
                expanded.Expand((broadRadius + maximumSurfaceDistance) * 2f);
                if (expanded.SqrDistance(point) > 0f)
                    continue;
                if (node.Count == 0)
                {
                    m_QueryStack[stackCount++] = node.Left;
                    m_QueryStack[stackCount++] = node.Right;
                    continue;
                }

                for (int offset = 0; offset < node.Count; offset++)
                {
                    int index = m_Indices[node.Start + offset];
                    if (visibility[index] == 0 || radii[index] <= 0f)
                        continue;
                    float centerDistance = Vector3.Distance(point, m_Positions[index]);
                    float surfaceDistance = Mathf.Max(0f, centerDistance - radii[index]);
                    if (surfaceDistance > maximumSurfaceDistance)
                        continue;
                    if (IsBetterProximity(index, surfaceDistance, centerDistance, best, bestSurfaceDistance, bestCenterDistance))
                    {
                        best = index;
                        bestSurfaceDistance = surfaceDistance;
                        bestCenterDistance = centerDistance;
                    }
                }
            }

            return best < 0 ? SitePickResult.None : new SitePickResult(best, m_SiteIds[best], 0f, bestSurfaceDistance, point);
        }

        public float[] CreateDynamicRadiusBounds(float[] radii, byte[] visibility)
        {
            var result = new float[m_Nodes.Count];
            for (int nodeIndex = m_Nodes.Count - 1; nodeIndex >= 0; nodeIndex--)
            {
                Node node = m_Nodes[nodeIndex];
                result[nodeIndex] = node.Count > 0 ? MaximumLeafRadius(node, radii, visibility) : Mathf.Max(result[node.Left], result[node.Right]);
            }

            return result;
        }

        public void UpdateDynamicRadiusBounds(float[] nodeMaximumRadii, float[] radii, byte[] visibility, int start, int count)
        {
            int end = checked(start + count);
            for (int index = start; index < end; index++)
            {
                int nodeIndex = m_LeafByIndex[index];
                Node leaf = m_Nodes[nodeIndex];
                nodeMaximumRadii[nodeIndex] = MaximumLeafRadius(leaf, radii, visibility);
                for (int parent = m_ParentByNode[nodeIndex]; parent >= 0; parent = m_ParentByNode[parent])
                {
                    Node parentNode = m_Nodes[parent];
                    nodeMaximumRadii[parent] = Mathf.Max(nodeMaximumRadii[parentNode.Left], nodeMaximumRadii[parentNode.Right]);
                }
            }
        }

        private int Build(int start, int count)
        {
            Bounds bounds = new(m_Positions[m_Indices[start]], Vector3.zero);
            for (int offset = 1; offset < count; offset++)
                bounds.Encapsulate(m_Positions[m_Indices[start + offset]]);
            int nodeIndex = m_Nodes.Count;
            m_Nodes.Add(default);
            m_ParentByNode.Add(-1);
            if (count <= LeafSize)
            {
                m_Nodes[nodeIndex] = new Node(nodeIndex, bounds, start, count, -1, -1);
                for (int offset = 0; offset < count; offset++)
                    m_LeafByIndex[m_Indices[start + offset]] = nodeIndex;
                return nodeIndex;
            }

            int axis = bounds.size.x >= bounds.size.y && bounds.size.x >= bounds.size.z ? 0 : bounds.size.y >= bounds.size.z ? 1 : 2;
            Array.Sort(m_Indices, start, count, Comparer<int>.Create((left, right) => ComparePosition(left, right, axis)));
            int leftCount = count / 2;
            int left = Build(start, leftCount);
            int right = Build(start + leftCount, count - leftCount);
            m_ParentByNode[left] = nodeIndex;
            m_ParentByNode[right] = nodeIndex;
            m_Nodes[nodeIndex] = new Node(nodeIndex, bounds, 0, 0, left, right);
            return nodeIndex;
        }

        private float MaximumLeafRadius(Node node, float[] radii, byte[] visibility)
        {
            float maximum = 0f;
            for (int offset = 0; offset < node.Count; offset++)
            {
                int index = m_Indices[node.Start + offset];
                if (visibility[index] != 0 && radii[index] > 0f)
                    maximum = Mathf.Max(maximum, radii[index]);
            }

            return maximum;
        }

        private int ComparePosition(int left, int right, int axis)
        {
            int comparison = m_Positions[left][axis].CompareTo(m_Positions[right][axis]);
            return comparison != 0 ? comparison : m_SiteIds[left].CompareTo(m_SiteIds[right]);
        }

        private bool IsBetterRay(int index, float distance, float axisDistance, int best, float bestDistance, float bestAxisDistance)
        {
            if (best < 0 || distance < bestDistance - TieEpsilonMillimeters)
                return true;
            if (Mathf.Abs(distance - bestDistance) > TieEpsilonMillimeters)
                return false;
            if (axisDistance < bestAxisDistance - TieEpsilonMillimeters)
                return true;
            return Mathf.Abs(axisDistance - bestAxisDistance) <= TieEpsilonMillimeters && m_SiteIds[index] < m_SiteIds[best];
        }

        private bool IsBetterProximity(int index, float surfaceDistance, float centerDistance, int best, float bestSurfaceDistance, float bestCenterDistance)
        {
            if (best < 0 || surfaceDistance < bestSurfaceDistance - TieEpsilonMillimeters)
                return true;
            if (Mathf.Abs(surfaceDistance - bestSurfaceDistance) > TieEpsilonMillimeters)
                return false;
            if (centerDistance < bestCenterDistance - TieEpsilonMillimeters)
                return true;
            return Mathf.Abs(centerDistance - bestCenterDistance) <= TieEpsilonMillimeters && m_SiteIds[index] < m_SiteIds[best];
        }

        private readonly struct Node
        {
            public Node(int index, Bounds bounds, int start, int count, int left, int right)
            {
                Index = index;
                Bounds = bounds;
                Start = start;
                Count = count;
                Left = left;
                Right = right;
            }

            public int Index { get; }

            public Bounds Bounds { get; }

            public int Start { get; }

            public int Count { get; }

            public int Left { get; }

            public int Right { get; }
        }
    }
}
