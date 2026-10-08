using System;
using System.Collections.Generic;
using HBP.Data.Module3D;
using UnityEngine;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Quest
{
    /// <summary>Prepared identity and actual contact sphere, independent of input and publication.</summary>
    public sealed class QuestSiteTarget
    {
        public readonly Column3D Column;
        public readonly Site Site;
        public readonly SphereCollider Sphere;

        public QuestSiteTarget(Column3D column, Site site)
        {
            Column = column;
            Site = site;
            Sphere = site.GetComponent<SphereCollider>();
        }

        public bool IsEligible(Base3DScene scene) => IsSelectable(scene) && scene.Columns.Contains(Column) && Column.Sites.Contains(Site);

        public bool IsSelectable(Base3DScene scene) => scene != null && !scene.IsClosing && Column != null && Site != null && Site.gameObject.activeInHierarchy && Sphere != null && Sphere.enabled && Site.State != null && HBP.Core.Object3D.SiteAppearance.IsVisible(Site.State.IsMasked, Site.State.IsOutOfROI, Site.State.IsFiltered, Site.State.IsBlackListed, scene.ShowAllSites, scene.HideBlacklistedSites);
    }

    /// <summary>Current sphere contact, with nearest-center arbitration and stable prepared-ID ties. No Physics response.</summary>
    public sealed class QuestSiteContactPolicy
    {
        public QuestSiteTarget FindContact(Vector3 point, float radius, IReadOnlyList<QuestSiteTarget> targets)
        {
            QuestSiteTarget nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var target in targets)
            {
                var sphere = target.Sphere;
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                Vector3 scale = sphere.transform.lossyScale;
                float combined = radius + sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                float current = (point - center).sqrMagnitude;
                if (current > combined * combined) continue;
                if (current < distance || current == distance && Compare(target, nearest) < 0)
                {
                    nearest = target;
                    distance = current;
                }
            }

            return nearest;
        }

        private static int Compare(QuestSiteTarget a, QuestSiteTarget b)
        {
            if (b == null) return -1;
            int column = StringComparer.Ordinal.Compare(a.Column.ColumnData.ID, b.Column.ColumnData.ID);
            return column != 0 ? column : StringComparer.Ordinal.Compare(a.Site.Information.FullID, b.Site.Information.FullID);
        }
    }

    public static class QuestSiteSelection
    {
        public static bool TrySelect(Base3DScene scene, QuestSiteTarget target)
        {
            if (target == null || !target.IsEligible(scene) || scene.SelectedColumn == target.Column && target.Column.SelectedSite == target.Site) return false;
            scene.SelectSite(target.Column, target.Site);
            return target.Column.SelectedSite == target.Site;
        }
    }
}
