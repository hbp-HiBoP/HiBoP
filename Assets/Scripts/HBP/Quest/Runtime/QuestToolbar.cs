using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestToolbar : MonoBehaviour
    {
        [SerializeField] private QuestWindowsManager windows;
        [SerializeField] private QuestConnectionPanel connection;
        [SerializeField] private QuestCutsPanel cuts;
        [SerializeField] private QuestDisplaySettingsPanel displaySettings;
        [SerializeField] private QuestROIPanel rois;
        [SerializeField] private QuestTriangleErasingPanel triangles;
        [SerializeField] private Button trianglesButton;
        [SerializeField] private Button roiButton;
        [SerializeField] private Button connectionButton, cutsButton, displayButton, siteButton, signalsButton;
        [SerializeField] private Button displaySettingsButton;
        [SerializeField] private Text connectionStatus;
        private readonly List<(Button Button, string Key)> sceneButtons = new();

        private void Awake()
        {
            foreach (var button in GetComponentsInChildren<QuestToolbarButton>(true)) button.Configure(windows);
            Bind(connectionButton, "Connection");
            Bind(displayButton, "Display");
            Bind(displaySettingsButton, "DisplaySettings", () => displaySettings != null && displaySettings.Ready);
            Bind(siteButton, "Site", HasInteractiveScene);
            Bind(signalsButton, "Signals", HasInteractiveScene);
            Bind(cutsButton, "Cuts", HasInteractiveScene);
            Bind(roiButton, "ROI", () => rois != null && rois.Ready);
            Bind(trianglesButton, "TriangleErasing", () => triangles != null && triangles.Ready);
        }

        private bool HasInteractiveScene() => cuts != null && cuts.Ready;

        private void Bind(Button button, string key, Func<bool> canOpen = null)
        {
            if (button == null) return;
            button.onClick.AddListener(() => ToggleWindow(key));
            if (canOpen == null || windows == null) return;
            windows.SetOpenAvailability(key, canOpen);
            sceneButtons.Add((button, key));
        }

        private void ToggleWindow(string key)
        {
            var window = windows.Find(key);
            if (window != null && window.IsOpen) window.Close();
            else windows.Open(key);
        }

        private void Update()
        {
            if (connectionStatus != null && connection != null) connectionStatus.text = connection.Summary;
            foreach (var (button, key) in sceneButtons) button.interactable = windows.CanOpen(key);
        }
    }
}
