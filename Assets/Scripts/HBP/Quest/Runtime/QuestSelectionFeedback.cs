using TMPro;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Authored local aids; diagnostic volume never participates in selection.</summary>
    public sealed class QuestSelectionFeedback : MonoBehaviour
    {
        [SerializeField] private QuestSiteProbe probe;
        [SerializeField] private LineRenderer candidate, link, beam;
        [SerializeField] private LineRenderer[] volume;
        [SerializeField] private TextMeshPro label;
        [SerializeField] private Renderer cursorCore;
        [SerializeField] private LineRenderer[] cursorBack, cursorFront;
        private readonly Vector3[] circle = new Vector3[49];
        private MaterialPropertyBlock properties;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            foreach (var line in volume) Tint(line, new Color(1, .7f, .1f));
            Tint(candidate, Color.cyan);
            Tint(link, Color.cyan);
        }

        private void Tint(Renderer renderer, Color value)
        {
            properties.SetColor("_BaseColor", value);
            properties.SetColor("_Color", value);
            renderer.SetPropertyBlock(properties);
        }

        private void LateUpdate() => Refresh(Camera.main);

        public void Refresh(Camera camera)
        {
            var settings = probe != null ? probe.Settings : null;
            bool active = probe != null && probe.IsActive && settings != null;
            DrawCursor(active, settings, camera);
            var target = active ? probe.CurrentContact : null;
            beam.enabled = active && settings.Mode == QuestSelectionMode.Ray;
            if (beam.enabled)
            {
                beam.positionCount = 2;
                beam.SetPosition(0, probe.Assistance.Start);
                beam.SetPosition(1, target != null ? QuestSiteAssistance.Center(target) : probe.Assistance.End);
            }

            candidate.enabled = target != null && settings.CandidateCue;
            link.enabled = target != null && settings.LinkCue;
            label.gameObject.SetActive(target != null && settings.NameCue);
            if (settings != null) label.fontSize = 3 * settings.LabelScale;
            if (target != null)
            {
                Vector3 center = QuestSiteAssistance.Center(target);
                Vector3 right = camera != null ? camera.transform.right : Vector3.right;
                Vector3 up = camera != null ? camera.transform.up : Vector3.up;
                DrawCircle(candidate, center, right, up, QuestSiteAssistance.Radius(target) + .001f);
                link.positionCount = 2;
                link.SetPosition(0, probe.ProbePosition);
                link.SetPosition(1, center);
                label.transform.position = center + up * .012f;
                if (camera != null) label.transform.rotation = camera.transform.rotation;
                label.text = target.Site.Information.Name;
            }

            foreach (var line in volume) line.enabled = active && settings.Debug;
            if (!active || !settings.Debug || volume.Length < 6) return;
            var geometry = probe.Assistance;
            Vector3 axis = (geometry.End - geometry.Start).normalized;
            if (axis == Vector3.zero) axis = Vector3.forward;
            Vector3 u = Vector3.Cross(axis, Vector3.up).normalized;
            if (u == Vector3.zero) u = Vector3.right;
            Vector3 v = Vector3.Cross(axis, u).normalized;
            DrawCircle(volume[0], geometry.Start, u, v, geometry.StartRadius);
            DrawCircle(volume[1], geometry.End, u, v, geometry.EndRadius);
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = i == 0 ? u : i == 1 ? -u : i == 2 ? v : -v;
                volume[i + 2].positionCount = 2;
                volume[i + 2].SetPosition(0, geometry.Start + offset * geometry.StartRadius);
                volume[i + 2].SetPosition(1, geometry.End + offset * geometry.EndRadius);
            }

            // With zero segment length, show the actual spherical contact volume in three planes.
            if (geometry.Start == geometry.End)
            {
                DrawCircle(volume[2], geometry.Start, u, axis, geometry.StartRadius);
                DrawCircle(volume[3], geometry.Start, v, axis, geometry.StartRadius);
                volume[4].enabled = volume[5].enabled = false;
            }
        }

        private void DrawCursor(bool active, QuestSelectionSettings settings, Camera camera)
        {
            bool guides = active && settings.CursorStyle != QuestCursorStyle.Sphere;
            foreach (var line in cursorBack) line.enabled = guides;
            foreach (var line in cursorFront) line.enabled = guides;
            if (!guides) return;
            Color ink = settings.CursorColor == QuestCursorColor.Amber ? new Color(1, .7f, .05f) : settings.CursorColor == QuestCursorColor.Cyan ? Color.cyan : Color.white;
            Tint(cursorCore, ink);
            Vector3 center = probe.ProbePosition;
            Quaternion rotation = probe.CursorPose.rotation;
            Vector3 right = rotation * Vector3.right, up = rotation * Vector3.up, forward = rotation * Vector3.forward;
            for (int i = 0; i < 4; i++)
            {
                bool show = i == 0 || settings.CursorStyle == QuestCursorStyle.Cage && i < 3 || settings.CursorStyle == QuestCursorStyle.Stylus && i == 3;
                cursorBack[i].enabled = cursorFront[i].enabled = show;
                if (!show) continue;
                Tint(cursorBack[i], Color.black);
                Tint(cursorFront[i], ink);
                cursorBack[i].startWidth = cursorBack[i].endWidth = .0014f;
                cursorFront[i].startWidth = cursorFront[i].endWidth = .0006f;
                if (i == 3)
                {
                    DrawSegment(cursorBack[i], center - forward * .04f, center);
                    DrawSegment(cursorFront[i], center - forward * .04f, center);
                }
                else
                {
                    Vector3 u = i == 1 ? right : i == 2 ? up : settings.CursorStyle == QuestCursorStyle.Cage || camera == null ? right : camera.transform.right;
                    Vector3 v = i > 0 ? forward : settings.CursorStyle == QuestCursorStyle.Cage || camera == null ? up : camera.transform.up;
                    DrawCircle(cursorBack[i], center, u, v, settings.CursorGuideRadius);
                    DrawCircle(cursorFront[i], center, u, v, settings.CursorGuideRadius);
                }
            }
        }

        private static void DrawSegment(LineRenderer line, Vector3 from, Vector3 to)
        {
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
        }

        private void OnDisable()
        {
            foreach (var line in cursorBack) line.enabled = false;
            foreach (var line in cursorFront) line.enabled = false;
        }

        private void DrawCircle(LineRenderer line, Vector3 center, Vector3 u, Vector3 v, float radius)
        {
            for (int i = 0; i < circle.Length; i++)
            {
                float angle = 2 * Mathf.PI * i / (circle.Length - 1);
                circle[i] = center + radius * (u * Mathf.Cos(angle) + v * Mathf.Sin(angle));
            }

            line.positionCount = circle.Length;
            line.SetPositions(circle);
        }
    }
}
