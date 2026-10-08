using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Local cut outline and central rail; contact covers the bounded plane inside the outline.</summary>
    public sealed class QuestCutGizmo : MonoBehaviour
    {
        [SerializeField] private LineRenderer outline, rail;
        private QuestCutGizmoSettings style;
        private MaterialPropertyBlock properties;
        private bool visible, geometryReady;
        private Vector3 center, axisA, axisB;
        private float radiusA, radiusB;
        private readonly Vector3[] corners = new Vector3[4];
        public Cut Cut { get; private set; }
        public Base3DScene Scene { get; private set; }
        public QuestColumnPresentation Column { get; private set; }
        public Transform Frame => Column != null && Column.Column != null && Column.Column.BrainMesh != null ? Column.Column.BrainMesh.transform : null;
        public Vector3 PlaneCenter => Frame.TransformPoint(center);
        public bool IsVisible => visible;

        public void Configure(QuestCutGizmoSettings value) => style = value;

        public void Bind(Base3DScene scene, QuestColumnPresentation column, Cut cut)
        {
            Scene = scene;
            Column = column;
            Cut = cut;
            properties = new MaterialPropertyBlock();
            Show(false, false, false);
        }

        public bool IsValid => Frame != null && QuestCutCommands.CanEdit(Scene, Cut) && Column.Column.gameObject.activeInHierarchy && Column.Manipulator.SharedMesh != null;

        public void UpdateGeometry()
        {
            geometryReady = false;
            if (!IsValid || style == null) return;
            Vector3 normal = Cut.Normal.normalized;
            // Anatomical contours keep their axes regardless of cut order or other planes.
            axisA = Vector3.Cross(normal, Mathf.Abs(normal.y) < .9f ? Vector3.up : Vector3.right).normalized;
            axisB = Vector3.Cross(normal, axisA).normalized;
            var bounds = Column.Manipulator.SharedMesh.bounds;
            radiusA = Vector3.Dot(Abs(axisA), bounds.extents);
            radiusB = Vector3.Dot(Abs(axisB), bounds.extents);
            center = Cut.Point + Vector3.ProjectOnPlane(bounds.center - Cut.Point, normal);
            corners[0] = Frame.TransformPoint(center - axisA * radiusA - axisB * radiusB);
            corners[1] = Frame.TransformPoint(center + axisA * radiusA - axisB * radiusB);
            corners[2] = Frame.TransformPoint(center + axisA * radiusA + axisB * radiusB);
            corners[3] = Frame.TransformPoint(center - axisA * radiusA + axisB * radiusB);
            outline.positionCount = 4;
            outline.loop = true;
            for (int i = 0; i < corners.Length; i++) outline.SetPosition(i, corners[i]);
            var geometry = Scene.GetCutPositionGeometry(Cut);
            Vector3 direction = Frame.TransformVector(normal * geometry.Span);
            SetLine(rail, PlaneCenter - direction * Cut.Position, PlaneCenter + direction * (1 - Cut.Position));
            foreach (var line in new[] { outline, rail })
            {
                line.useWorldSpace = true;
                line.startWidth = line.endWidth = style.LineWidth;
            }

            geometryReady = true;
        }

        public float ContactDistance(Vector3 point) => geometryReady ? Vector3.Distance(point, ClosestPoint(point)) : float.PositiveInfinity;

        private Vector3 ClosestPoint(Vector3 point)
        {
            // Project in world space so the threshold stays in metres, including under scale.
            Vector3 normal = Vector3.Cross(corners[1] - corners[0], corners[3] - corners[0]).normalized;
            Vector3 projected = point - normal * Vector3.Dot(point - corners[0], normal);
            Vector3 local = Frame.InverseTransformPoint(projected) - center;
            if (Mathf.Abs(Vector3.Dot(local, axisA)) <= radiusA && Mathf.Abs(Vector3.Dot(local, axisB)) <= radiusB) return projected;
            Vector3 closest = corners[0];
            float distance = float.PositiveInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 edge = corners[(i + 1) % corners.Length] - corners[i];
                float t = edge.sqrMagnitude > 0 ? Mathf.Clamp01(Vector3.Dot(point - corners[i], edge) / edge.sqrMagnitude) : 0;
                Vector3 candidate = corners[i] + edge * t;
                float squared = (point - candidate).sqrMagnitude;
                if (squared < distance)
                {
                    distance = squared;
                    closest = candidate;
                }
            }

            return closest;
        }

        private static Vector3 Abs(Vector3 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        private static void SetLine(LineRenderer line, Vector3 from, Vector3 to)
        {
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
        }

        public void Show(bool value, bool hovered, bool occupied)
        {
            visible = value;
            outline.enabled = rail.enabled = value;
            if (style == null) return;
            var color = occupied ? style.Occupied : hovered ? style.Hovered : style.Normal;
            if (color == null) return;
            properties ??= new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", color.Value);
            properties.SetColor("_Color", color.Value);
            foreach (var line in new[] { outline, rail })
            {
                line.startColor = line.endColor = color.Value;
                line.SetPropertyBlock(properties);
            }
        }
    }
}
