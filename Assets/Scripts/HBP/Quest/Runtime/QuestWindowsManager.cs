using System;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Quest
{
    [DefaultExecutionOrder(100)]
    public sealed class QuestWindowsManager : MonoBehaviour
    {
        [Serializable]
        public struct Definition
        {
            public string Key;
            public QuestWindow Prefab;
        }

        [SerializeField] private Transform head;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private QuestWindow[] initialWindows = Array.Empty<QuestWindow>();
        [SerializeField] private Definition[] catalogue = Array.Empty<Definition>();
        private readonly Dictionary<string, QuestWindow> windows = new(StringComparer.Ordinal);
        private bool trackingPlaced;
        public event Action WindowsChanged;

        private void Awake()
        {
            foreach (var window in initialWindows) Register(window);
            RecenterOpen();
        }

        private void Update()
        {
            if (trackingPlaced || head == null) return;
            var tracker = head.GetComponent<QuestDevicePoseTracker>();
            if (tracker != null && !tracker.IsTracked) return;
            trackingPlaced = true;
            RecenterOpen();
        }

        public void Register(QuestWindow window)
        {
            if (window == null || string.IsNullOrEmpty(window.Key)) return;
            if (windows.TryGetValue(window.Key, out var existing))
            {
                if (existing == window) return;
                throw new InvalidOperationException("Duplicate Quest window: " + window.Key);
            }

            windows.Add(window.Key, window);
            window.Changed += OnChanged;
            window.Destroyed += Unregister;
            var canvas = window.GetComponent<Canvas>();
            if (canvas != null && uiCamera != null) canvas.worldCamera = uiCamera;
            var follow = window.GetComponent<QuestWindowFollower>();
            if (follow != null) follow.Configure(head, window);
            foreach (var dragger in window.GetComponentsInChildren<QuestWindowDragger>(true)) dragger.Configure(window, head);
            WindowsChanged?.Invoke();
        }

        private void OnChanged(QuestWindow window) => WindowsChanged?.Invoke();

        public void Unregister(QuestWindow window)
        {
            if (window == null || !windows.TryGetValue(window.Key, out var registered) || registered != window) return;
            window.Changed -= OnChanged;
            window.Destroyed -= Unregister;
            windows.Remove(window.Key);
            WindowsChanged?.Invoke();
        }

        public QuestWindow Find(string key) => windows.TryGetValue(key, out var window) ? window : null;

        public QuestWindow Open(string key)
        {
            var window = Find(key);
            if (window == null)
                foreach (var item in catalogue)
                    if (item.Key == key && item.Prefab != null)
                    {
                        window = Instantiate(item.Prefab, transform, false);
                        Register(window);
                        break;
                    }

            if (window == null) return null;
            window.Open();
            Recall(window);
            return window;
        }

        public void Recall(QuestWindow window)
        {
            if (window == null || !window.IsOpen || window.IsManipulated || head == null) return;
            var follow = window.GetComponent<QuestWindowFollower>();
            if (follow != null) follow.Recall();
            else
            {
                Quaternion yaw = QuestWindowFollower.HorizontalRotation(head.forward);
                window.transform.SetPositionAndRotation(head.position + yaw * window.Placement, yaw);
            }
        }

        public void RecenterOpen()
        {
            foreach (var window in windows.Values)
                if (window != null && window.IsOpen)
                    Recall(window);
        }

        private void OnDestroy()
        {
            foreach (var window in windows.Values)
                if (window != null)
                {
                    window.Changed -= OnChanged;
                    window.Destroyed -= Unregister;
                }

            windows.Clear();
        }
    }
}
