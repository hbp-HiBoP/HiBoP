using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Local window lifecycle, independent of connection and scientific state.</summary>
    public sealed class QuestWindow : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject content;
        [SerializeField] private Behaviour raycaster;
        [SerializeField] private Button close, pin;
        [SerializeField] private Text pinLabel;
        [SerializeField] private bool initiallyOpen = true;
        [SerializeField] private Vector3 placement = new Vector3(0, 0, 1.15f);
        private int hovered;
        public string Key => key;
        public bool IsOpen { get; private set; }
        public bool IsPinned { get; private set; }
        public bool IsManipulated { get; private set; }
        public bool IsBusy => hovered != 0 || IsManipulated;
        public Vector3 Placement => placement;
        public event Action<QuestWindow> Changed;
        public event Action<QuestWindow> Destroyed;

        private void Awake()
        {
            if (close != null) close.onClick.AddListener(Close);
            if (pin != null) pin.onClick.AddListener(TogglePin);
            SetOpen(initiallyOpen);
            RefreshPin();
        }

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

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
            Changed?.Invoke(this);
        }

        public void SetPinned(bool value)
        {
            IsPinned = value;
            RefreshPin();
            Changed?.Invoke(this);
        }

        public void TogglePin() => SetPinned(!IsPinned);

        private void RefreshPin()
        {
            if (pinLabel != null) pinLabel.text = IsPinned ? "Follow" : "Pin";
        }

        public void SetHovered(int hand, bool value)
        {
            if (value) hovered |= 1 << hand;
            else hovered &= ~(1 << hand);
        }

        public void SetManipulated(bool value)
        {
            IsManipulated = value;
            Changed?.Invoke(this);
        }

        private void OnDestroy()
        {
            Destroyed?.Invoke(this);
        }
    }
}
