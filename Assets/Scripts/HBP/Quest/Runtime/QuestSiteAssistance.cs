using System.Collections.Generic;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Bounded capsule/cone assistance. No dwell, swept contacts or retained target outside the active volume.</summary>
    public sealed class QuestSiteAssistance
    {
        private readonly QuestSiteContactPolicy direct = new();
        public int CandidateCount { get; private set; }
        public bool Retained { get; private set; }
        public bool DirectContact { get; private set; }
        public Vector3 Start { get; private set; }
        public Vector3 End { get; private set; }
        public float StartRadius { get; private set; }
        public float EndRadius { get; private set; }

        public QuestSiteTarget Find(Vector3 point, Vector3 viewAxis, Ray aim, QuestSelectionSettings settings, IReadOnlyList<QuestSiteTarget> targets, QuestSiteTarget previous)
        {
            CandidateCount = 0;
            Retained = false;
            DirectContact = false;
            bool ray = settings.Mode == QuestSelectionMode.Ray;
            Vector3 axis = ray ? aim.direction.normalized : viewAxis.normalized;
            if (axis == Vector3.zero) axis = Vector3.forward;
            float half = settings.Mode == QuestSelectionMode.Depth ? settings.DepthTolerance : 0;
            Start = ray ? aim.origin : point - axis * half;
            End = ray ? aim.GetPoint(settings.RayDistance) : point + axis * half;
            StartRadius = settings.ContactRadius + (settings.Attraction ? settings.AttractionRadius : 0);
            EndRadius = StartRadius + (ray && settings.Attraction ? Mathf.Tan(settings.RayAngle * Mathf.Deg2Rad) * settings.RayDistance : 0);
            var nearest = direct.FindContact(point, settings.ContactRadius, targets);
            // A direct touch is always an escape from assistance, including on the ray tool.
            QuestSiteTarget best = null;
            float score = float.PositiveInfinity, previousScore = float.PositiveInfinity;
            float length = Vector3.Distance(Start, End);
            foreach (var target in targets)
            {
                Vector3 center = Center(target);
                float along = Vector3.Dot(center - Start, axis);
                float siteRadius = Radius(target);
                if (ray && (along < 0 || along > length)) continue;
                float t = length > 0 ? Mathf.Clamp01(along / length) : 0;
                float radius = Mathf.Lerp(StartRadius, EndRadius, t) + siteRadius;
                float distance = Vector3.Distance(center, Vector3.Lerp(Start, End, t));
                if (distance > radius) continue;
                CandidateCount++;
                float current = distance / radius;
                // For a capsule, depth still breaks near-equal lateral scores predictably.
                current += .001f * Vector3.Distance(center, point);
                if (target == previous) previousScore = current;
                if (current < score || current == score && Compare(target, best) < 0)
                {
                    best = target;
                    score = current;
                }
            }

            if (nearest != null)
            {
                DirectContact = true;
                CandidateCount = Mathf.Max(1, CandidateCount);
                return nearest;
            }

            if (settings.Mode == QuestSelectionMode.Contact && !settings.Attraction) return null;
            if (settings.Stabilize && best != null && previous != null && previous != best && float.IsFinite(previousScore) && previousScore <= score + settings.SwitchMargin)
            {
                Retained = true;
                return previous;
            }

            return best;
        }

        private static int Compare(QuestSiteTarget a, QuestSiteTarget b)
        {
            if (b == null) return -1;
            int column = string.CompareOrdinal(a.Column.ColumnData.ID, b.Column.ColumnData.ID);
            return column != 0 ? column : string.CompareOrdinal(a.Site.Information.FullID, b.Site.Information.FullID);
        }

        public static Vector3 Center(QuestSiteTarget target) => target.Sphere.transform.TransformPoint(target.Sphere.center);

        public static float Radius(QuestSiteTarget target)
        {
            Vector3 s = target.Sphere.transform.lossyScale;
            return target.Sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
    }
}
