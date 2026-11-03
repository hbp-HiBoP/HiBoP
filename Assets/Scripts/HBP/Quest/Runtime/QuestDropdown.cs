using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>uGUI dropdown popups must carry the tracked camera and raycaster too.</summary>
    public sealed class QuestDropdown : Dropdown
    {
        private Canvas popupCanvas;
        private Canvas blockerCanvas;

        internal void SetWindowSortingOrder(int order)
        {
            if (popupCanvas != null) popupCanvas.sortingOrder = order + 2;
            if (blockerCanvas != null) blockerCanvas.sortingOrder = order + 1;
        }

        private void ApplyWindowSortingOrder()
        {
            var window = GetComponentInParent<QuestWindow>();
            if (window != null) SetWindowSortingOrder(window.SortingOrder);
        }

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
            popupCanvas = popup.GetComponent<Canvas>();
            ApplyWindowSortingOrder();
            return popup;
        }

        protected override GameObject CreateBlocker(Canvas rootCanvas)
        {
            var blocker = base.CreateBlocker(rootCanvas);
            Configure(blocker);
            blockerCanvas = blocker.GetComponent<Canvas>();
            ApplyWindowSortingOrder();
            return blocker;
        }
    }
}
