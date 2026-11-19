using UnityEngine;

namespace HBP.Quest
{
    [CreateAssetMenu(menuName = "Theme/Settings/Quest cut gizmo")]
    public sealed class QuestCutGizmoSettings : HBP.Theme.Settings
    {
        public HBP.Theme.Color Normal, Hovered, Occupied;
        [Min(.0001f)] public float LineWidth = .001f;

        public override void Set(GameObject target)
        {
            if (target.TryGetComponent<QuestCutGizmo>(out var gizmo)) gizmo.Configure(this);
        }
    }
}
