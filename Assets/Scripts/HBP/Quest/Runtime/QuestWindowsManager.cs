using System;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;

namespace HBP.Quest
{
    [DefaultExecutionOrder(100)]
    public sealed class QuestWindowsManager : MonoBehaviour
    {
        // Each window reserves two extra orders for its dropdown blocker and popup.
        // The loading canvas starts at 30000, followed by the dialog canvas at 31000.
        private const int FirstWindowSortingOrder = 10;
        private const int LastWindowSortingOrder = 29997;
        private const int WindowSortingOrderStep = 3;

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
        private readonly List<QuestWindow> windowOrder = new();
        private bool trackingPlaced;
        private readonly QuestWorkspaceInitialization initialization = new();
        private QuestWindowLayoutStore layout;
        private Pose workspace;
        private bool focused = true, paused;
        public bool HasWorkspace => trackingPlaced;

        private void Awake()
        {
            layout = new QuestWindowLayoutStore(QuestWindowLayoutStore.DefaultPath);
            foreach (var window in initialWindows) Register(window);
            foreach (var definition in catalogue)
                if (definition.Prefab != null && Find(definition.Key) == null && layout.TryGetState(definition.Key, out var state) && state.Open == true)
                {
                    var window = Instantiate(definition.Prefab, transform, false);
                    Register(window);
                }
        }

        private void Update()
        {
            if (trackingPlaced || head == null) return;
            var tracker = head.GetComponent<QuestDevicePoseTracker>();
            bool worn = InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(CommonUsages.userPresence, out bool present) && present;
            bool ready = focused && !paused && worn && tracker != null && tracker.IsTracked;
            if (initialization.Step(Time.unscaledDeltaTime, ready, new Pose(head.position, head.rotation))) RecenterOpen();
        }

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value) initialization.Reset();
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value) initialization.Reset();
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
            window.Manager = this;
            window.RefreshPresentation();
            int order = int.MaxValue;
            if (layout.TryGetState(window.Key, out var state))
            {
                window.RestoreState(state);
                order = state.Order ?? int.MaxValue;
            }

            int insertion = windowOrder.FindIndex(other => layout.TryGetState(other.Key, out var otherState) && otherState.Order > order);
            if (insertion < 0) insertion = windowOrder.Count;
            windowOrder.Insert(insertion, window);
            UpdateSortingOrder(insertion);
            window.OnSelected.AddListener(BringToFront);
            window.OnDestroyed.AddListener(Unregister);
            window.OnOpened.AddListener(Recall);
            window.OnMoved.AddListener(SavePlacement);
            window.OnSettingsChanged.AddListener(SaveSettings);
            var canvas = window.GetComponent<Canvas>();
            if (canvas != null && uiCamera != null) canvas.worldCamera = uiCamera;
            var follow = window.GetComponent<QuestWindowFollower>();
            if (follow != null) follow.Configure(head, window);
            foreach (var dragger in window.GetComponentsInChildren<QuestWindowDragger>(true)) dragger.Configure(window, head);
            Recall(window);
        }

        public void Unregister(QuestWindow window)
        {
            if (window == null || !windows.TryGetValue(window.Key, out var registered) || registered != window) return;
            window.OnSelected.RemoveListener(BringToFront);
            window.OnDestroyed.RemoveListener(Unregister);
            window.OnOpened.RemoveListener(Recall);
            window.OnMoved.RemoveListener(SavePlacement);
            window.OnSettingsChanged.RemoveListener(SaveSettings);
            window.Manager = null;
            windows.Remove(window.Key);
            int index = windowOrder.IndexOf(window);
            windowOrder.RemoveAt(index);
            UpdateSortingOrder(index);
        }

        private void BringToFront(QuestWindow window)
        {
            int index = windowOrder.IndexOf(window);
            if (index < 0 || index == windowOrder.Count - 1) return;
            windowOrder.RemoveAt(index);
            windowOrder.Add(window);
            UpdateSortingOrder(index);
            SaveSettings(window);
        }

        private void UpdateSortingOrder(int start)
        {
            for (int i = start; i < windowOrder.Count; i++)
                windowOrder[i].SetSortingOrder(Mathf.Min(FirstWindowSortingOrder + i * WindowSortingOrderStep, LastWindowSortingOrder));
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
            return window;
        }

        public void Recall(QuestWindow window)
        {
            if (!trackingPlaced || window == null || !window.IsOpen || window.IsManipulated || head == null) return;
            window.RestoreDimensions();
            var pose = window.LayoutPose;
            var follow = window.GetComponent<QuestWindowFollower>();
            if (follow != null) follow.Place(workspace, pose);
            else window.transform.SetPositionAndRotation(workspace.position + workspace.rotation * pose.position, workspace.rotation * pose.rotation);
        }

        public void RecenterOpen()
        {
            if (head == null) return;
            workspace = new Pose(head.position, QuestWindowFollower.HorizontalRotation(head.forward));
            trackingPlaced = true;
            // Revealing content may register another window through its OnEnable lifecycle.
            foreach (var window in windowOrder.ToArray())
                if (window != null)
                {
                    Recall(window);
                    window.RefreshPresentation();
                }
        }

        private void SavePlacement(QuestWindow window)
        {
            if (!trackingPlaced || !focused || paused || head == null) return;
            var tracker = head.GetComponent<QuestDevicePoseTracker>();
            if (tracker != null && !tracker.IsTracked) return;
            Quaternion inverse = Quaternion.Inverse(workspace.rotation);
            var pose = new Pose(inverse * (window.transform.position - workspace.position), inverse * window.transform.rotation);
            window.SetLayoutPose(pose);
            // Reset following to the newly chosen placement without recording subsequent automatic motion.
            var follow = window.GetComponent<QuestWindowFollower>();
            if (follow != null) follow.Place(workspace, pose);
            SaveSettings(window);
        }

        private void StoreSettings(QuestWindow window)
        {
            var zoom = window.GetComponentInChildren<QuestWindowContentZoom>(true);
            layout.SetState(window.Key, new QuestWindowLayoutStore.WindowState(window.LayoutPose, window.UserSize, zoom != null ? zoom.ZoomPercent : null, window.IsPinned, window.IsOpen, windowOrder.IndexOf(window)));
        }

        private void SaveSettings(QuestWindow window)
        {
            // Snapshot all ranks together, retaining saved local poses rather than transient automatic following.
            foreach (var item in windowOrder) StoreSettings(item);
            SaveLayout();
        }

        private void SaveLayout()
        {
            try
            {
                layout.Save();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnDestroy()
        {
            foreach (var window in windows.Values)
                if (window != null)
                {
                    window.OnSelected.RemoveListener(BringToFront);
                    window.OnDestroyed.RemoveListener(Unregister);
                    window.OnOpened.RemoveListener(Recall);
                    window.OnMoved.RemoveListener(SavePlacement);
                    window.OnSettingsChanged.RemoveListener(SaveSettings);
                    window.Manager = null;
                }

            windows.Clear();
            windowOrder.Clear();
        }
    }
}
