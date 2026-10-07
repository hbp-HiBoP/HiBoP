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
    }

    public enum QuestInteractionOwner
    {
        None,
        UI,
        Anatomy,
        Empty
    }

    /// <summary>Ownership never transfers during a held trigger, including after tracking loss.</summary>
    public sealed class QuestInteractionCapture
    {
        public QuestInteractionOwner Owner { get; private set; }
        private bool armed;

        public bool Sample(bool valid, bool pressed, bool ui, bool anatomy, bool preferAnatomy)
        {
            if (!valid)
            {
                Cancel();
                return false;
            }

            if (!pressed)
            {
                Owner = QuestInteractionOwner.None;
                armed = true;
                return false;
            }

            if (!armed || Owner != QuestInteractionOwner.None) return false;
            armed = false;
            Owner = anatomy && (preferAnatomy || !ui) ? QuestInteractionOwner.Anatomy : ui ? QuestInteractionOwner.UI : QuestInteractionOwner.Empty;
            return true;
        }

        public void Cancel()
        {
            Owner = QuestInteractionOwner.None;
            armed = false;
        }
    }
}
