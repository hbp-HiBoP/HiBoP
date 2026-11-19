using UnityEngine;

namespace HBP.Quest
{
    [CreateAssetMenu(menuName = "HiBoP/Quest/Interaction policy")]
    public sealed class QuestInteractionPolicy : ScriptableObject
    {
        [Min(0.1f)] public float RayDistance = 3;
        public bool PreferNearbyAnatomy = true;
        public bool EnableDistantAnatomy = true;
        [Min(0.0001f)] public float RayWidth = 0.002f;
        public HBP.Theme.Color FeedbackColor;

        [Min(0.0001f)] public float SiteProbeRadius = 0.0005f;

        [Range(0, 1)] public float SiteHapticAmplitude = 0.2f;
        [Min(0)] public float SiteHapticDuration = 0.025f;

        [Min(.005f)] public float CutContactDistance = .035f;
        [Min(.01f)] public float CutShowDistance = .18f;
        [Min(.01f)] public float CutHideDistance = .24f;
        [Min(.005f)] public float ROIContactDistance = .035f;
    }

    public enum QuestInteractionOwner
    {
        None,
        UI,
        Anatomy,
        Cut,
        Empty,
        ROI
    }

    /// <summary>A deflection chooses one axis and must return to neutral before choosing again.</summary>
    public struct QuestThumbstickGesture
    {
        private const float PressThreshold = .65f;
        private const float NeutralThreshold = .35f;
        private bool armed;
        private Vector2Int repeating;
        private float repeatAt;

        public Vector2Int Sample(Vector2 value, bool available, float now, float horizontalRepeatInterval = 0)
        {
            if (!available)
            {
                this = default;
                return Vector2Int.zero;
            }

            float x = Mathf.Abs(value.x);
            float y = Mathf.Abs(value.y);
            float magnitude = Mathf.Max(x, y);
            if (magnitude <= NeutralThreshold)
            {
                armed = true;
                repeating = Vector2Int.zero;
            }

            if (magnitude < PressThreshold || x == y) return Vector2Int.zero;
            var direction = x > y ? new Vector2Int(value.x > 0 ? 1 : -1, 0) : new Vector2Int(0, value.y > 0 ? 1 : -1);
            if (armed)
            {
                armed = false;
                repeating = horizontalRepeatInterval > 0 && direction.x != 0 ? direction : Vector2Int.zero;
            }
            else if (repeating != direction)
            {
                repeating = Vector2Int.zero;
                return Vector2Int.zero;
            }
            else if (now < repeatAt) return Vector2Int.zero;

            repeatAt = now + horizontalRepeatInterval;
            return direction;
        }
    }

    /// <summary>One operation per hand; each interaction must release before it can capture again.</summary>
    public sealed class QuestInteractionCapture
    {
        public QuestInteractionOwner Owner { get; private set; }
        public QuestInteraction Interaction => Owner is QuestInteractionOwner.Cut or QuestInteractionOwner.ROI ? QuestInteraction.Secondary : QuestInteraction.Primary;
        private bool primaryArmed, secondaryArmed;

        public bool Sample(bool valid, bool primaryPressed, bool secondaryPressed, bool ui, bool anatomy, bool preferAnatomy, bool cut, bool roi = false)
        {
            if (!valid)
            {
                Cancel();
                return false;
            }

            bool primaryBegan = primaryArmed && primaryPressed;
            bool secondaryBegan = secondaryArmed && secondaryPressed;
            primaryArmed = !primaryPressed;
            secondaryArmed = !secondaryPressed;

            if (Owner != QuestInteractionOwner.None)
            {
                if (Interaction == QuestInteraction.Secondary ? secondaryPressed : primaryPressed) return false;
                Owner = QuestInteractionOwner.None;
                // As with a single trigger, the release frame cannot start another operation.
                return false;
            }

            if (secondaryBegan && roi) Owner = QuestInteractionOwner.ROI;
            else if (secondaryBegan && cut) Owner = QuestInteractionOwner.Cut;
            else if (primaryBegan) Owner = anatomy && (preferAnatomy || !ui) ? QuestInteractionOwner.Anatomy : ui ? QuestInteractionOwner.UI : QuestInteractionOwner.Empty;
            else return false;
            return true;
        }

        public void Cancel()
        {
            Owner = QuestInteractionOwner.None;
            primaryArmed = secondaryArmed = false;
        }
    }
}
