using System;
using HBP.Data.Module3D;
using HBP.Sync.Scene;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Quest admission and targeting; ROI business state remains in the common model.</summary>
    public static class QuestROICommands
    {
        public static bool Ready(Base3DScene scene) => QuestAnatomySession.CanInteractWith(scene) && scene.IsVisible && !scene.IsActivityProjectionBusy && !scene.IsSurfaceRepresentationTransitioning && SceneReconciliationRecord.Get(scene)?.Status is not (ReconciliationStatus.Reconciling or ReconciliationStatus.OutOfSync);

        public static bool Contains(Base3DScene scene, ROI roi, Sphere sphere = null) => Ready(scene) && roi != null && scene.ROIManager.ROIs.Contains(roi) && (sphere == null || roi.Spheres.Contains(sphere));

        public static bool Change(Base3DScene scene, Action action)
        {
            if (!Ready(scene) || !scene.TryBeginSensitiveActivityOperation(out var scope)) return false;
            using (scope) action();
            return true;
        }

        public static bool Definition(Base3DScene scene, ROI roi, Sphere sphere, Vector3 position, float radius)
        {
            if (sphere == null || !Contains(scene, roi, sphere) || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) || !float.IsFinite(radius)) return false;
            return Change(scene, () => roi.SetSphereDefinition(sphere, position, radius));
        }
    }
}
