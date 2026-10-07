using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>uGUI dropdown popups must carry the tracked camera and raycaster too.</summary>
    public sealed class QuestDropdown : Dropdown
    {
        private void Configure(GameObject popup)
        {
            var source = GetComponentInParent<Canvas>();
            foreach (var canvas in popup.GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = source != null ? source.worldCamera : null;
                if (canvas.GetComponent<TrackedDeviceRaycaster>() == null) canvas.gameObject.AddComponent<TrackedDeviceRaycaster>();
                var mouseRaycaster = canvas.GetComponent<GraphicRaycaster>();
                if (mouseRaycaster != null) mouseRaycaster.enabled = false;
            }
        }

        protected override GameObject CreateDropdownList(GameObject template)
        {
            var popup = base.CreateDropdownList(template);
            Configure(popup);
            return popup;
        }

        protected override GameObject CreateBlocker(Canvas rootCanvas)
        {
            var blocker = base.CreateBlocker(rootCanvas);
            Configure(blocker);
            return blocker;
        }
    }
}
