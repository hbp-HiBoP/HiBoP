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
    }

    public enum QuestInteractionOwner
    {
        None,
        UI,
        Anatomy,
        Cut,
        Empty
    }

    /// <summary>One operation per hand; each interaction must release before it can capture again.</summary>
    public sealed class QuestInteractionCapture
    {
        public QuestInteractionOwner Owner { get; private set; }
        public QuestInteraction Interaction => Owner == QuestInteractionOwner.Cut ? QuestInteraction.Secondary : QuestInteraction.Primary;
        private bool primaryArmed, secondaryArmed;

        public bool Sample(bool valid, bool primaryPressed, bool secondaryPressed, bool ui, bool anatomy, bool preferAnatomy, bool cut)
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

            if (secondaryBegan && cut) Owner = QuestInteractionOwner.Cut;
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
