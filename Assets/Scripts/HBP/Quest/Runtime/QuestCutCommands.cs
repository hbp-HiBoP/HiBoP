using System.Globalization;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Each control changes only its intent, reading all other fields from the live cut.</summary>
    public static class QuestCutCommands
    {
        public static bool CanEdit(Base3DScene scene, Cut cut) => scene != null && !scene.IsClosing && !scene.AutomaticCutAroundSelectedSite && scene.Cuts.Contains(cut);

        public static bool Position(Base3DScene scene, Cut cut, float value)
        {
            if (!CanEdit(scene, cut) || !float.IsFinite(value)) return false;
            scene.SetCutDefinition(cut, cut.Orientation, cut.Flip, Mathf.Clamp01(value), cut.Normal);
            return true;
        }

        public static bool Orientation(Base3DScene scene, Cut cut, CutOrientation value)
        {
            if (!CanEdit(scene, cut)) return false;
            scene.SetCutDefinition(cut, value, cut.Flip, cut.Position, cut.Normal);
            return true;
        }

        public static bool Flip(Base3DScene scene, Cut cut, bool value)
        {
            if (!CanEdit(scene, cut) || cut.Orientation == CutOrientation.Custom) return false;
            scene.SetCutDefinition(cut, cut.Orientation, value, value == cut.Flip ? cut.Position : 1 - cut.Position, cut.Normal);
            return true;
        }

        public static bool TryNormal(string x, string y, string z, out Vector3 normal)
        {
            normal = default;
            if (!Parse(x, out float nx) || !Parse(y, out float ny) || !Parse(z, out float nz)) return false;
            normal = new Vector3(nx, ny, nz);
            return normal.sqrMagnitude > 1e-12f && float.IsFinite(normal.sqrMagnitude);
        }

        private static bool Parse(string text, out float value) => float.TryParse(text?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && float.IsFinite(value);

        public static bool Normal(Base3DScene scene, Cut cut, string x, string y, string z)
        {
            if (!CanEdit(scene, cut) || cut.Orientation != CutOrientation.Custom || !TryNormal(x, y, z, out var normal)) return false;
            scene.SetCutDefinition(cut, cut.Orientation, cut.Flip, cut.Position, normal);
            return true;
        }
    }
}
