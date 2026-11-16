using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>One column's local representation of a shared scientific sphere.</summary>
    public sealed class QuestROISphere : MonoBehaviour
    {
        private MeshRenderer cachedRenderer;
        private MaterialPropertyBlock properties;
        public QuestColumnPresentation Column { get; private set; }
        public ROI ROI { get; private set; }
        public Sphere Sphere { get; private set; }
        public Transform Frame => Column.ScientificFrame;
        public bool IsValid => Column != null && ROI != null && Sphere != null && Column.Scene.ROIManager.SelectedROI == ROI && ROI.Spheres.Contains(Sphere);

        public void Bind(QuestColumnPresentation column, ROI roi, Sphere sphere)
        {
            if (Sphere != sphere) transform.localRotation = Quaternion.identity;
            Column = column;
            ROI = roi;
            Sphere = sphere;
        }

        public float ContactDistance(Vector3 point)
        {
            if (!IsValid) return float.PositiveInfinity;
            Vector3 local = Frame.InverseTransformPoint(point) - Sphere.Position;
            if (local.sqrMagnitude <= Sphere.InfluenceRadius * Sphere.InfluenceRadius) return 0;
            return Vector3.Distance(point, Frame.TransformPoint(Sphere.Position + local.normalized * Sphere.InfluenceRadius));
        }

        internal bool Raycast(Ray ray, out float distance)
        {
            // Keep the direction's local magnitude so the roots are distances along the world ray.
            Vector3 origin = Frame.InverseTransformPoint(ray.origin) - Sphere.Position;
            Vector3 direction = Frame.InverseTransformVector(ray.direction);
            distance = 0;
            float directionSquared = direction.sqrMagnitude;
            if (directionSquared <= 0) return false;
            float closest = -Vector3.Dot(origin, direction) / directionSquared;
            float radiusSquared = Sphere.InfluenceRadius * Sphere.InfluenceRadius;
            float inside = radiusSquared - (origin + direction * closest).sqrMagnitude;
            if (inside < 0) return false;
            float offset = Mathf.Sqrt(inside / directionSquared);
            distance = closest - offset;
            if (distance < 0) distance = closest + offset;
            return distance >= 0 && float.IsFinite(distance);
        }

        public void Feedback(QuestCutGizmoSettings style, bool hovered, bool occupied)
        {
            cachedRenderer ??= GetComponent<MeshRenderer>();
            if (style == null) return;
            // Scientific selection owns its material color on both platforms.
            var color = Sphere != null && Sphere.Selected ? null : occupied ? style.Occupied : hovered ? style.Hovered : null;
            if (color == null)
            {
                cachedRenderer.SetPropertyBlock(null);
                return;
            }

            properties ??= new MaterialPropertyBlock();
            properties.SetColor("_WireColor", color.Value);
            cachedRenderer.SetPropertyBlock(properties);
        }
    }
}
