using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CRNL.HiBoP.XR.SitePrototypes
{
    public sealed class P10PrototypeComparisonTests
    {
        private const int SiteCount = 37_500;
        private const int QueryCount = 2_000;
        private const float ProximityThresholdMillimeters = 12f;
        private const float RayRadiusMillimeters = 2f;

        [Test]
        public void D3_CompareGpuSubmissionAndSpatialIndexes()
        {
            Vector3[] positions = CreateD3Positions();
            using var grid = new GridPrototype(positions, 8f);
            using var bvh = new BvhPrototype(positions);

            Measurement gridBuild = Measure(() => new GridPrototype(positions, 8f).Dispose(), 3, 8);
            Measurement bvhBuild = Measure(() => new BvhPrototype(positions).Dispose(), 3, 8);
            Measurement gridQuery = MeasureQueries(grid, positions);
            Measurement bvhQuery = MeasureQueries(bvh, positions);
            Measurement gridRay = MeasureRayQueries(grid, positions);
            Measurement bvhRay = MeasureRayQueries(bvh, positions);
            Assert.That(VerifyExactTargets(grid, positions), Is.EqualTo(QueryCount));
            Assert.That(VerifyExactTargets(bvh, positions), Is.EqualTo(QueryCount));
            Assert.That(VerifyExactRayTargets(grid, positions), Is.EqualTo(QueryCount));
            Assert.That(VerifyExactRayTargets(bvh, positions), Is.EqualTo(QueryCount));

            GpuMeasurements gpu = MeasureGpuSubmissions(positions);
            string report = $"{{\n" + $"  \"environment\": \"UnityEditor {Application.unityVersion} {SystemInfo.operatingSystem} {SystemInfo.graphicsDeviceName}\",\n" + $"  \"dataset\": \"D3 synthetic\",\n" + $"  \"siteCount\": {SiteCount},\n" + $"  \"queryCount\": {QueryCount},\n" + $"  \"gridBuildMs\": {gridBuild.Json()},\n" + $"  \"bvhBuildMs\": {bvhBuild.Json()},\n" + $"  \"gridQueryUs\": {gridQuery.Json()},\n" + $"  \"bvhQueryUs\": {bvhQuery.Json()},\n" + $"  \"gridRayUs\": {gridRay.Json()},\n" + $"  \"bvhRayUs\": {bvhRay.Json()},\n" + $"  \"matrixSubmissionMs\": {gpu.MatrixSubmission.Json()},\n" + $"  \"bufferedSubmissionMs\": {gpu.BufferedSubmission.Json()},\n" + $"  \"bufferedDirty256Ms\": {gpu.BufferedDirtySubmission.Json()},\n" + $"  \"matrixDrawCalls\": {gpu.MatrixDrawCalls},\n" + $"  \"bufferedDrawCalls\": {gpu.BufferedDrawCalls},\n" + $"  \"matrixCpuBytes\": {gpu.MatrixCpuBytes},\n" + $"  \"bufferBytes\": {gpu.BufferBytes}\n" + "}";

            string repository = Directory.GetParent(Application.dataPath)?.Parent?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
            string artifactDirectory = Path.Combine(repository, ".artifacts", "xr", "p10");
            Directory.CreateDirectory(artifactDirectory);
            File.WriteAllText(Path.Combine(artifactDirectory, "prototype-host.json"), report);
            TestContext.WriteLine("P10_PROTOTYPE " + report.Replace(Environment.NewLine, " "));
        }

        private static GpuMeasurements MeasureGpuSubmissions(Vector3[] positions)
        {
            Shader shader = Shader.Find("HiBoP XR/P10/Prototype Buffered Sites");
            Assert.That(shader, Is.Not.Null);
            var matrixMaterial = new Material(shader) { enableInstancing = true };
            var bufferedMaterial = new Material(shader) { enableInstancing = true };
            bufferedMaterial.EnableKeyword("_P10_BUFFERED");
            Mesh mesh = CreatePrototypeMesh();

            var matrices = new Matrix4x4[positions.Length];
            var positionData = new Vector4[positions.Length];
            var attributes = new Vector4[positions.Length];
            for (int index = 0; index < positions.Length; index++)
            {
                Vector3 metres = positions[index] * 0.001f;
                matrices[index] = Matrix4x4.TRS(metres, Quaternion.identity, Vector3.one * 0.001f);
                positionData[index] = positions[index];
                attributes[index] = new Vector4(1f, 0.53f, 0.15f, 0.15f);
            }

            var batches = new List<Matrix4x4[]>();
            for (int start = 0; start < matrices.Length; start += 1023)
            {
                int count = Math.Min(1023, matrices.Length - start);
                var batch = new Matrix4x4[count];
                Array.Copy(matrices, start, batch, 0, count);
                batches.Add(batch);
            }

            try
            {
                using var positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SiteCount, sizeof(float) * 4);
                using var attributesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SiteCount, sizeof(float) * 4);
                var block = new MaterialPropertyBlock();
                block.SetBuffer("_P10PrototypePositions", positionsBuffer);
                block.SetBuffer("_P10PrototypeAttributes", attributesBuffer);
                var renderParams = new RenderParams(bufferedMaterial) { matProps = block, worldBounds = new Bounds(Vector3.zero, Vector3.one * 10f) };
                positionsBuffer.SetData(positionData);

                Measurement matrix = Measure(() =>
                {
                    for (int index = 0; index < matrices.Length; index++)
                    {
                        Vector3 metres = positions[index] * 0.001f;
                        matrices[index] = Matrix4x4.TRS(metres, Quaternion.identity, Vector3.one * (attributes[index].x * 0.001f));
                    }

                    for (int start = 0, batchIndex = 0; start < matrices.Length; start += 1023, batchIndex++)
                        Array.Copy(matrices, start, batches[batchIndex], 0, batches[batchIndex].Length);
                    foreach (Matrix4x4[] batch in batches)
                        Graphics.DrawMeshInstanced(mesh, 0, matrixMaterial, batch);
                }, 10, 80);
                Measurement buffered = Measure(() =>
                {
                    attributesBuffer.SetData(attributes);
                    Graphics.RenderMeshPrimitives(renderParams, mesh, 0, SiteCount);
                }, 10, 80);
                Measurement bufferedDirty = Measure(() =>
                {
                    attributesBuffer.SetData(attributes, 0, 0, 256);
                    Graphics.RenderMeshPrimitives(renderParams, mesh, 0, SiteCount);
                }, 10, 80);

                return new GpuMeasurements(matrix, buffered, bufferedDirty, batches.Count, 1, (long)SiteCount * 64L, (long)SiteCount * 32L);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(matrixMaterial);
                UnityEngine.Object.DestroyImmediate(bufferedMaterial);
            }
        }

        private static Mesh CreatePrototypeMesh()
        {
            var mesh = new Mesh { name = "P10 disposable tetrahedron" };
            mesh.vertices = new[] { new Vector3(1f, 0f, -0.5f), new Vector3(-1f, 0f, -0.5f), new Vector3(0f, 1f, 0.5f), new Vector3(0f, -1f, 0.5f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Vector3[] CreateD3Positions()
        {
            var result = new Vector3[SiteCount];
            var random = new System.Random(10);
            for (int index = 0; index < result.Length; index++)
            {
                double longitude = random.NextDouble() * Math.PI * 2d;
                double z = random.NextDouble() * 2d - 1d;
                double radial = Math.Sqrt(1d - z * z);
                double shell = 0.55d + random.NextDouble() * 0.45d;
                result[index] = new Vector3((float)(90d * shell * radial * Math.Cos(longitude)), (float)(110d * shell * z), (float)(85d * shell * radial * Math.Sin(longitude)));
            }

            return result;
        }

        private static Measurement MeasureQueries(IPrototypeIndex index, Vector3[] positions)
        {
            var samples = new double[QueryCount];
            for (int query = 0; query < QueryCount; query++)
            {
                Vector3 point = positions[(query * 7919) % positions.Length];
                long start = Stopwatch.GetTimestamp();
                int selected = index.FindNearest(point, ProximityThresholdMillimeters);
                long end = Stopwatch.GetTimestamp();
                Assert.That(selected, Is.GreaterThanOrEqualTo(0));
                samples[query] = (end - start) * 1_000_000d / Stopwatch.Frequency;
            }

            return Measurement.From(samples);
        }

        private static int VerifyExactTargets(IPrototypeIndex index, Vector3[] positions)
        {
            int correct = 0;
            for (int query = 0; query < QueryCount; query++)
            {
                int expected = (query * 7919) % positions.Length;
                if (index.FindNearest(positions[expected], ProximityThresholdMillimeters) == expected)
                    correct++;
            }

            return correct;
        }

        private static Measurement MeasureRayQueries(IPrototypeIndex index, Vector3[] positions)
        {
            var samples = new double[QueryCount];
            for (int query = 0; query < QueryCount; query++)
            {
                int expected = (query * 7919) % positions.Length;
                Vector3 direction = positions[expected].sqrMagnitude > 0f ? positions[expected].normalized : Vector3.forward;
                Vector3 origin = positions[expected] + direction * 20f;
                long start = Stopwatch.GetTimestamp();
                int selected = index.FindRay(origin, -direction, 25f, RayRadiusMillimeters);
                long end = Stopwatch.GetTimestamp();
                Assert.That(selected, Is.GreaterThanOrEqualTo(0));
                samples[query] = (end - start) * 1_000_000d / Stopwatch.Frequency;
            }

            return Measurement.From(samples);
        }

        private static int VerifyExactRayTargets(IPrototypeIndex index, Vector3[] positions)
        {
            int correct = 0;
            for (int query = 0; query < QueryCount; query++)
            {
                int expected = (query * 7919) % positions.Length;
                Vector3 direction = positions[expected].sqrMagnitude > 0f ? positions[expected].normalized : Vector3.forward;
                Vector3 origin = positions[expected] + direction * 20f;
                int reference = FindRayBruteForce(positions, origin, -direction, 25f, RayRadiusMillimeters);
                if (index.FindRay(origin, -direction, 25f, RayRadiusMillimeters) == reference)
                    correct++;
            }

            return correct;
        }

        private static int FindRayBruteForce(Vector3[] positions, Vector3 origin, Vector3 direction, float maximumDistance, float radius)
        {
            direction.Normalize();
            int best = -1;
            float bestDistance = maximumDistance;
            for (int index = 0; index < positions.Length; index++)
            {
                Vector3 offset = positions[index] - origin;
                float along = Vector3.Dot(offset, direction);
                if (along < 0f || along > bestDistance)
                    continue;
                if ((offset - direction * along).sqrMagnitude <= radius * radius)
                {
                    bestDistance = along;
                    best = index;
                }
            }

            return best;
        }

        private static Measurement Measure(Action action, int warmup, int repetitions)
        {
            for (int index = 0; index < warmup; index++)
                action();
            var samples = new double[repetitions];
            for (int index = 0; index < repetitions; index++)
            {
                long start = Stopwatch.GetTimestamp();
                action();
                samples[index] = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
            }

            return Measurement.From(samples);
        }

        private interface IPrototypeIndex : IDisposable
        {
            int FindNearest(Vector3 point, float threshold);

            int FindRay(Vector3 origin, Vector3 direction, float maximumDistance, float radius);
        }

        private sealed class GridPrototype : IPrototypeIndex
        {
            private readonly Vector3[] m_Positions;
            private readonly float m_CellSize;
            private readonly Dictionary<Cell, List<int>> m_Cells = new();

            public GridPrototype(Vector3[] positions, float cellSize)
            {
                m_Positions = positions;
                m_CellSize = cellSize;
                for (int index = 0; index < positions.Length; index++)
                {
                    Cell cell = Cell.From(positions[index], cellSize);
                    if (!m_Cells.TryGetValue(cell, out List<int> entries))
                    {
                        entries = new List<int>();
                        m_Cells.Add(cell, entries);
                    }

                    entries.Add(index);
                }
            }

            public int FindNearest(Vector3 point, float threshold)
            {
                Cell center = Cell.From(point, m_CellSize);
                int reach = Mathf.CeilToInt(threshold / m_CellSize);
                int best = -1;
                float bestSquared = threshold * threshold;
                for (int x = center.X - reach; x <= center.X + reach; x++)
                for (int y = center.Y - reach; y <= center.Y + reach; y++)
                for (int z = center.Z - reach; z <= center.Z + reach; z++)
                {
                    if (!m_Cells.TryGetValue(new Cell(x, y, z), out List<int> entries))
                        continue;
                    foreach (int index in entries)
                    {
                        float squared = (m_Positions[index] - point).sqrMagnitude;
                        if (squared < bestSquared || (Mathf.Approximately(squared, bestSquared) && (best < 0 || index < best)))
                        {
                            bestSquared = squared;
                            best = index;
                        }
                    }
                }

                return best;
            }

            public int FindRay(Vector3 origin, Vector3 direction, float maximumDistance, float radius)
            {
                direction.Normalize();
                int best = -1;
                float bestDistance = maximumDistance;
                var visited = new HashSet<Cell>();
                var tested = new HashSet<int>();
                int reach = Mathf.CeilToInt(radius / m_CellSize);
                for (float distance = 0f; distance <= maximumDistance; distance += m_CellSize * 0.5f)
                {
                    Cell center = Cell.From(origin + direction * distance, m_CellSize);
                    for (int x = center.X - reach; x <= center.X + reach; x++)
                    for (int y = center.Y - reach; y <= center.Y + reach; y++)
                    for (int z = center.Z - reach; z <= center.Z + reach; z++)
                    {
                        Cell cell = new(x, y, z);
                        if (!visited.Add(cell) || !m_Cells.TryGetValue(cell, out List<int> entries))
                            continue;
                        foreach (int index in entries)
                        {
                            if (!tested.Add(index))
                                continue;
                            Vector3 offset = m_Positions[index] - origin;
                            float along = Vector3.Dot(offset, direction);
                            if (along < 0f || along > bestDistance)
                                continue;
                            float perpendicularSquared = (offset - direction * along).sqrMagnitude;
                            if (perpendicularSquared <= radius * radius)
                            {
                                bestDistance = along;
                                best = index;
                            }
                        }
                    }
                }

                return best;
            }

            public void Dispose()
            {
            }
        }

        private sealed class BvhPrototype : IPrototypeIndex
        {
            private const int LeafSize = 8;
            private readonly Vector3[] m_Positions;
            private readonly int[] m_Indices;
            private readonly List<Node> m_Nodes = new();

            public BvhPrototype(Vector3[] positions)
            {
                m_Positions = positions;
                m_Indices = Enumerable.Range(0, positions.Length).ToArray();
                Build(0, positions.Length);
            }

            public int FindNearest(Vector3 point, float threshold)
            {
                int best = -1;
                float bestSquared = threshold * threshold;
                Search(0, point, ref best, ref bestSquared);
                return best;
            }

            public int FindRay(Vector3 origin, Vector3 direction, float maximumDistance, float radius)
            {
                direction.Normalize();
                int best = -1;
                float bestDistance = maximumDistance;
                SearchRay(0, origin, direction, radius, ref best, ref bestDistance);
                return best;
            }

            public void Dispose()
            {
            }

            private int Build(int start, int count)
            {
                Bounds bounds = new(m_Positions[m_Indices[start]], Vector3.zero);
                for (int offset = 1; offset < count; offset++)
                    bounds.Encapsulate(m_Positions[m_Indices[start + offset]]);
                int nodeIndex = m_Nodes.Count;
                m_Nodes.Add(default);
                if (count <= LeafSize)
                {
                    m_Nodes[nodeIndex] = new Node(bounds, start, count, -1, -1);
                    return nodeIndex;
                }

                int axis = bounds.size.x >= bounds.size.y && bounds.size.x >= bounds.size.z ? 0 : bounds.size.y >= bounds.size.z ? 1 : 2;
                Array.Sort(m_Indices, start, count, Comparer<int>.Create((left, right) => m_Positions[left][axis].CompareTo(m_Positions[right][axis])));
                int leftCount = count / 2;
                int leftNode = Build(start, leftCount);
                int rightNode = Build(start + leftCount, count - leftCount);
                m_Nodes[nodeIndex] = new Node(bounds, 0, 0, leftNode, rightNode);
                return nodeIndex;
            }

            private void Search(int nodeIndex, Vector3 point, ref int best, ref float bestSquared)
            {
                Node node = m_Nodes[nodeIndex];
                if (node.Bounds.SqrDistance(point) > bestSquared)
                    return;
                if (node.Count > 0)
                {
                    for (int offset = 0; offset < node.Count; offset++)
                    {
                        int index = m_Indices[node.Start + offset];
                        float squared = (m_Positions[index] - point).sqrMagnitude;
                        if (squared < bestSquared || (Mathf.Approximately(squared, bestSquared) && (best < 0 || index < best)))
                        {
                            bestSquared = squared;
                            best = index;
                        }
                    }

                    return;
                }

                Search(node.Left, point, ref best, ref bestSquared);
                Search(node.Right, point, ref best, ref bestSquared);
            }

            private void SearchRay(int nodeIndex, Vector3 origin, Vector3 direction, float radius, ref int best, ref float bestDistance)
            {
                Node node = m_Nodes[nodeIndex];
                Bounds expanded = node.Bounds;
                expanded.Expand(radius * 2f);
                if (!expanded.IntersectRay(new Ray(origin, direction), out float entry) || entry > bestDistance)
                    return;
                if (node.Count > 0)
                {
                    for (int offsetIndex = 0; offsetIndex < node.Count; offsetIndex++)
                    {
                        int index = m_Indices[node.Start + offsetIndex];
                        Vector3 offset = m_Positions[index] - origin;
                        float along = Vector3.Dot(offset, direction);
                        if (along < 0f || along > bestDistance)
                            continue;
                        if ((offset - direction * along).sqrMagnitude <= radius * radius)
                        {
                            bestDistance = along;
                            best = index;
                        }
                    }

                    return;
                }

                SearchRay(node.Left, origin, direction, radius, ref best, ref bestDistance);
                SearchRay(node.Right, origin, direction, radius, ref best, ref bestDistance);
            }

            private readonly struct Node
            {
                public Node(Bounds bounds, int start, int count, int left, int right)
                {
                    Bounds = bounds;
                    Start = start;
                    Count = count;
                    Left = left;
                    Right = right;
                }

                public Bounds Bounds { get; }
                public int Start { get; }
                public int Count { get; }
                public int Left { get; }
                public int Right { get; }
            }
        }

        private readonly struct Cell : IEquatable<Cell>
        {
            public Cell(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public int X { get; }
            public int Y { get; }
            public int Z { get; }
            public static Cell From(Vector3 value, float size) => new(Mathf.FloorToInt(value.x / size), Mathf.FloorToInt(value.y / size), Mathf.FloorToInt(value.z / size));
            public bool Equals(Cell other) => X == other.X && Y == other.Y && Z == other.Z;
            public override bool Equals(object obj) => obj is Cell other && Equals(other);
            public override int GetHashCode() => unchecked(((X * 397) ^ Y) * 397 ^ Z);
        }

        private readonly struct Measurement
        {
            private Measurement(double p50, double p95, double maximum)
            {
                P50 = p50;
                P95 = p95;
                Maximum = maximum;
            }

            public double P50 { get; }
            public double P95 { get; }
            public double Maximum { get; }

            public static Measurement From(IEnumerable<double> values)
            {
                double[] ordered = values.OrderBy(value => value).ToArray();
                return new Measurement(Percentile(ordered, 0.50), Percentile(ordered, 0.95), ordered[^1]);
            }

            public string Json() => string.Format(CultureInfo.InvariantCulture, "{{ \"p50\": {0:F6}, \"p95\": {1:F6}, \"max\": {2:F6} }}", P50, P95, Maximum);
            private static double Percentile(double[] values, double percentile) => values[Math.Min(values.Length - 1, (int)Math.Ceiling(values.Length * percentile) - 1)];
        }

        private readonly struct GpuMeasurements
        {
            public GpuMeasurements(Measurement matrixSubmission, Measurement bufferedSubmission, Measurement bufferedDirtySubmission, int matrixDrawCalls, int bufferedDrawCalls, long matrixCpuBytes, long bufferBytes)
            {
                MatrixSubmission = matrixSubmission;
                BufferedSubmission = bufferedSubmission;
                BufferedDirtySubmission = bufferedDirtySubmission;
                MatrixDrawCalls = matrixDrawCalls;
                BufferedDrawCalls = bufferedDrawCalls;
                MatrixCpuBytes = matrixCpuBytes;
                BufferBytes = bufferBytes;
            }

            public Measurement MatrixSubmission { get; }
            public Measurement BufferedSubmission { get; }
            public Measurement BufferedDirtySubmission { get; }
            public int MatrixDrawCalls { get; }
            public int BufferedDrawCalls { get; }
            public long MatrixCpuBytes { get; }
            public long BufferBytes { get; }
        }
    }
}
