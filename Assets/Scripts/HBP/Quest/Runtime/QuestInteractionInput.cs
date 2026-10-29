using System;
using UnityEngine.InputSystem;

namespace HBP.Quest
{
    public enum QuestInteraction
    {
        Primary,
        Secondary
    }

    /// <summary>Shared controller bindings for primary (index trigger) and secondary (side grip) interactions.</summary>
    public static class QuestInteractionInput
    {
        public static InputAction CreateAction(string name, bool leftHand, QuestInteraction interaction)
        {
            string device = leftHand ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";
            string control = interaction switch
            {
                QuestInteraction.Primary => "triggerPressed",
                QuestInteraction.Secondary => "gripPressed",
                _ => throw new ArgumentOutOfRangeException(nameof(interaction))
            };
            return new InputAction(name, InputActionType.Button, device + "/" + control);
        }
    }
}
