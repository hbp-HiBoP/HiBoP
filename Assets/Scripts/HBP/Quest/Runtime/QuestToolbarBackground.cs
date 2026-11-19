using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Curves a themed, sliced Image, including tangent button sections of the toolbar.</summary>
    public sealed class QuestToolbarBackground : Image
    {
        private float radius = 1300;
        private Vector3 axis = Vector3.up, radial = Vector3.forward;
        private readonly List<UIVertex> source = new();
        private Vector3[] lower, upper;
        private bool geometryDirty = true;
        private Vector2 center;
        private Vector3 centerPoint;
        private Quaternion inverseCenter = Quaternion.identity;

        public void Configure(float value, Vector3 horizontalAxis, float centerX = 0, float centerY = 0)
        {
            radius = Mathf.Max(1, value);
            axis = horizontalAxis.normalized;
            radial = Vector3.Cross(Vector3.right, axis).normalized;
            center = new Vector2(centerX, centerY);
            centerPoint = ArcPoint(centerX, centerY);
            inverseCenter = Quaternion.Inverse(SurfaceRotation(centerX));
            geometryDirty = true;
            SetVerticesDirty();
        }

        public Quaternion SurfaceRotation(float x) => Quaternion.AngleAxis(x / radius * Mathf.Rad2Deg, axis);
        private Vector3 ArcPoint(float x, float y) => SurfaceRotation(x) * (radial * radius + Vector3.up * y) - radial * radius;
        public Vector3 SurfacePoint(float x, float y) => inverseCenter * (ArcPoint(x + center.x, y + center.y) - centerPoint);

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            geometryDirty = true;
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            base.OnPopulateMesh(vertices);
            source.Clear();
            var vertex = UIVertex.simpleVert;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                source.Add(vertex);
            }

            vertices.Clear();
            // Simple and sliced Images emit independent quads in bottom-left, top-left, top-right, bottom-right order.
            for (int quad = 0; quad + 3 < source.Count; quad += 4)
            {
                var a = source[quad];
                var b = source[quad + 1];
                var c = source[quad + 2];
                var d = source[quad + 3];
                int segments = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(d.position.x - a.position.x) / 20));
                for (int segment = 0; segment < segments; segment++)
                {
                    int index = vertices.currentVertCount;
                    AddVertex(vertices, a, d, (float)segment / segments);
                    AddVertex(vertices, b, c, (float)segment / segments);
                    AddVertex(vertices, b, c, (float)(segment + 1) / segments);
                    AddVertex(vertices, a, d, (float)(segment + 1) / segments);
                    vertices.AddTriangle(index, index + 1, index + 2);
                    vertices.AddTriangle(index + 2, index + 3, index);
                }
            }
        }

        private void AddVertex(VertexHelper vertices, UIVertex left, UIVertex right, float t)
        {
            var vertex = left;
            Vector3 position = Vector3.LerpUnclamped(left.position, right.position, t);
            vertex.position = SurfacePoint(position.x, position.y);
            vertex.uv0 = Vector4.LerpUnclamped(left.uv0, right.uv0, t);
            vertex.color = Color32.Lerp(left.color, right.color, t);
            vertices.AddVert(vertex);
        }

        public bool Intersect(Ray ray, out float distance, out Vector3 point, out Vector3 normal)
        {
            if (geometryDirty)
            {
                Rect rect = rectTransform.rect;
                int segments = Mathf.Clamp(Mathf.CeilToInt(rect.width / 20), 4, 256);
                if (lower == null || lower.Length != segments + 1)
                {
                    lower = new Vector3[segments + 1];
                    upper = new Vector3[segments + 1];
                }

                for (int i = 0; i <= segments; i++)
                {
                    float x = Mathf.Lerp(rect.xMin, rect.xMax, (float)i / segments);
                    lower[i] = SurfacePoint(x, rect.yMin);
                    upper[i] = SurfacePoint(x, rect.yMax);
                }

                geometryDirty = false;
            }

            Vector3 origin = transform.InverseTransformPoint(ray.origin);
            Vector3 direction = transform.InverseTransformVector(ray.direction);
            distance = float.PositiveInfinity;
            normal = Vector3.zero;
            for (int i = 0; i < lower.Length - 1; i++)
            {
                TestTriangle(origin, direction, lower[i], upper[i], upper[i + 1], ref distance, ref normal);
                TestTriangle(origin, direction, lower[i], upper[i + 1], lower[i + 1], ref distance, ref normal);
            }

            point = float.IsPositiveInfinity(distance) ? Vector3.zero : ray.GetPoint(distance);
            normal = transform.TransformDirection(normal).normalized;
            return !float.IsPositiveInfinity(distance);
        }

        private static void TestTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, ref float nearest, ref Vector3 normal)
        {
            Vector3 edge1 = b - a, edge2 = c - a;
            Vector3 p = Vector3.Cross(direction, edge2);
            float determinant = Vector3.Dot(edge1, p);
            if (Mathf.Abs(determinant) < .000001f) return;
            Vector3 offset = origin - a;
            float u = Vector3.Dot(offset, p) / determinant;
            if (u < 0 || u > 1) return;
            Vector3 q = Vector3.Cross(offset, edge1);
            float v = Vector3.Dot(direction, q) / determinant;
            if (v < 0 || u + v > 1) return;
            float distance = Vector3.Dot(edge2, q) / determinant;
            if (distance < 0 || distance >= nearest) return;
            nearest = distance;
            normal = Vector3.Cross(edge1, edge2);
        }
    }
}
