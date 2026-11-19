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
        public static InputAction CreateSitePresetAction() => new("Quest site preset (B)", InputActionType.Button, "<XRController>{RightHand}/secondaryButton");

        internal static InputAction CreateThumbstickAction(bool leftHand) => new("Quest thumbstick", InputActionType.Value, Controller(leftHand) + "/thumbstick", expectedControlType: "Vector2");

        public static InputAction CreateAction(string name, bool leftHand, QuestInteraction interaction)
        {
            string control = interaction switch
            {
                QuestInteraction.Primary => "triggerPressed",
                QuestInteraction.Secondary => "gripPressed",
                _ => throw new ArgumentOutOfRangeException(nameof(interaction))
            };
            return new InputAction(name, InputActionType.Button, Controller(leftHand) + "/" + control);
        }

        private static string Controller(bool leftHand) => leftHand ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";
    }
}
