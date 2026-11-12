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
        private Pose? layoutPose;
        internal Vector2? UserSize { get; private set; }
        internal QuestWindowsManager Manager { get; set; }
        internal bool WorkspaceReady => Manager == null || Manager.HasWorkspace;
        public string Key => key;
        public bool IsOpen { get; private set; }
        public bool IsPinned { get; private set; }
        public bool IsManipulated { get; private set; }
        public bool IsBusy => hovered != 0 || IsManipulated;
        public Vector3 Placement => placement;
        internal Pose LayoutPose => layoutPose ?? new Pose(placement, Quaternion.LookRotation(placement, Vector3.up));
        public int SortingOrder => canvas != null ? canvas.sortingOrder : 0;
        public GenericEvent<QuestWindow> OnChanged { get; } = new();
        public GenericEvent<QuestWindow> OnSelected { get; } = new();
        public GenericEvent<QuestWindow> OnDestroyed { get; } = new();
        public GenericEvent<QuestWindow> OnOpened { get; } = new();
        public GenericEvent<QuestWindow> OnMoved { get; } = new();
        public GenericEvent<QuestWindow> OnSettingsChanged { get; } = new();

        internal void SetLayoutPose(Pose pose) => layoutPose = pose;

        internal void CompleteMove() => OnMoved.Invoke(this);

        internal void CompleteResize()
        {
            UserSize = ((RectTransform)transform).rect.size;
            CompleteMove();
        }

        internal void NotifySettingsChanged() => OnSettingsChanged.Invoke(this);

        internal void RestoreState(QuestWindowLayoutStore.WindowState state)
        {
            SetLayoutPose(state.Pose);
            UserSize = state.Size;
            RestoreDimensions();
            if (state.ZoomPercent.HasValue) GetComponentInChildren<QuestWindowContentZoom>(true)?.RestoreZoomPercent(state.ZoomPercent.Value);
            if (state.Pinned.HasValue) SetPinned(state.Pinned.Value);
            if (state.Open.HasValue) SetOpen(state.Open.Value);
        }

        internal void RestoreDimensions()
        {
            if (!UserSize.HasValue) return;
            var size = UserSize.Value;
            var resizer = GetComponentInChildren<QuestWindowResizer>(true);
            if (resizer != null) size = resizer.ClampSize(size);
            var rect = (RectTransform)transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        private void Awake()
        {
            foreach (var panelCanvas in GetComponentsInChildren<Canvas>(true))
                QuestCanvasRenderQueue.Set(panelCanvas, QuestCanvasRenderQueue.Window);

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
            foreach (var button in GetComponentsInChildren<QuestToolbarButton>(true))
                button.SetWindowSortingOrder(order);
        }

        private void SetOpen(bool value)
        {
            bool changed = IsOpen != value;
            bool opened = value && !IsOpen;
            IsOpen = value;
            if (!value)
            {
                hovered = 0;
                IsManipulated = false;
            }

            RefreshPresentation();
            RefreshPin();
            OnChanged.Invoke(this);
            if (opened) OnOpened.Invoke(this);
            if (changed) NotifySettingsChanged();
        }

        internal void RefreshPresentation()
        {
            bool visible = IsOpen && WorkspaceReady;
            if (canvas != null) canvas.enabled = visible;
            if (content != null) content.SetActive(visible);
            if (raycaster != null) raycaster.enabled = visible;
        }

        public void SetPinned(bool value)
        {
            bool changed = IsPinned != value;
            IsPinned = value;
            RefreshPin();
            OnChanged.Invoke(this);
            if (changed) NotifySettingsChanged();
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
