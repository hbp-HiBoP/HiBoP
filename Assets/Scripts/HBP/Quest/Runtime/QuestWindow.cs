using HBP.Core.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Local window lifecycle, independent of connection and scientific state.</summary>
    [DefaultExecutionOrder(10)] // Apply the pin state after the control themes initialize.
    public sealed class QuestWindow : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject content;
        [SerializeField] private Behaviour raycaster;
        [SerializeField] private Button close, pin;
        [SerializeField] private HBP.Theme.ThemeElement pinTheme;
        [SerializeField] private HBP.Theme.State pinnedState;
        [SerializeField] private bool initiallyOpen = true;
        [SerializeField] private bool initiallyPinned = true;
        [SerializeField] private Vector3 placement = new Vector3(0, 0, 1.15f);
        private Transform header;
        private QuestWindowDragger dragger;
        private int hovered;
        public string Key => key;
        public bool IsOpen { get; private set; }
        public bool IsPinned { get; private set; }
        public bool IsManipulated { get; private set; }
        public bool IsBusy => hovered != 0 || IsManipulated;
        public Vector3 Placement => placement;
        public int SortingOrder => canvas != null ? canvas.sortingOrder : 0;
        public GenericEvent<QuestWindow> OnChanged { get; } = new();
        public GenericEvent<QuestWindow> OnSelected { get; } = new();
        public GenericEvent<QuestWindow> OnDestroyed { get; } = new();

        private void Awake()
        {
            // The base prefab already provides raycast surfaces and a shared drag handler.
            header = content != null ? content.transform.Find("Header") : null;
            dragger = GetComponentInChildren<QuestWindowDragger>(true);
            if (close != null) close.onClick.AddListener(Close);
            if (pin != null) pin.onClick.AddListener(TogglePin);
            IsPinned = initiallyPinned;
            SetOpen(initiallyOpen);
        }

        public void Open()
        {
            SetOpen(true);
            Select();
        }

        public void Close() => SetOpen(false);

        public void Select()
        {
            if (IsOpen) OnSelected.Invoke(this);
        }

        internal GameObject GetBackgroundDragTarget(GameObject hit)
        {
            if (hit == null || dragger == null || !dragger.isActiveAndEnabled) return null;
            bool isDragSurface = hit == content || (header != null && hit.transform.IsChildOf(header));
            return isDragSurface && hit.GetComponentInParent<Selectable>() == null ? dragger.gameObject : null;
        }

        internal void SetSortingOrder(int order)
        {
            if (canvas == null) return;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            foreach (var dropdown in GetComponentsInChildren<QuestDropdown>(true))
                dropdown.SetWindowSortingOrder(order);
        }

        private void SetOpen(bool value)
        {
            IsOpen = value;
            if (!value)
            {
                hovered = 0;
                IsManipulated = false;
            }

            if (canvas != null) canvas.enabled = value;
            if (content != null) content.SetActive(value);
            if (raycaster != null) raycaster.enabled = value;
            RefreshPin();
            OnChanged.Invoke(this);
        }

        public void SetPinned(bool value)
        {
            IsPinned = value;
            RefreshPin();
            OnChanged.Invoke(this);
        }

        public void TogglePin() => SetPinned(!IsPinned);

        private void RefreshPin()
        {
            if (pinTheme != null) pinTheme.Set(IsPinned ? pinnedState : pinTheme.Element.DefaultState);
        }

        public void SetHovered(int hand, bool value)
        {
            if (value) hovered |= 1 << hand;
            else hovered &= ~(1 << hand);
        }

        public void SetManipulated(bool value)
        {
            IsManipulated = value;
            OnChanged.Invoke(this);
        }

        private void OnDestroy()
        {
            OnDestroyed.Invoke(this);
        }
    }
}
