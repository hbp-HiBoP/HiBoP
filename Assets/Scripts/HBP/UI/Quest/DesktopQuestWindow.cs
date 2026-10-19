using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using HBP.Quest.Desktop;
using HBP.Transfer.Transport;
using HBP.UI.Main;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Quest
{
    public sealed class DesktopQuestWindow : DialogWindow
    {
        [SerializeField] private Dropdown devices;
        [SerializeField] private Toggle automatic;
        [SerializeField] private Toggle manual;
        [SerializeField] private GameObject automaticFields;
        [SerializeField] private GameObject manualFields;
        [SerializeField] private InputField address;
        [SerializeField] private Button pair;
        [SerializeField] private Text pairLabel;
        [SerializeField] private Button disconnect;
        [SerializeField] private VisualizationList visualizations;
        [SerializeField] private Button send;

        private readonly Dictionary<Visualization, Base3DScene> scenes = new();
        private QuestManager connection;
        private bool rebuilding, requesting, sending, missingSelection;
        private int selectedOption;
        private float nextRefresh;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                Refresh();
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            connection = QuestManager.Instance;
            manual.SetIsOnWithoutNotify(connection.IsManualSelection);
            automatic.SetIsOnWithoutNotify(!connection.IsManualSelection);
            address.SetTextWithoutNotify(connection.SelectedAddress ?? string.Empty);
            pair.onClick.AddListener(Connect);
            disconnect.onClick.AddListener(Disconnect);
            send.onClick.AddListener(Send);
            devices.onValueChanged.AddListener(SelectDevice);
            manual.onValueChanged.AddListener(ChangeMode);
            address.onValueChanged.AddListener(ChangeAddress);
            visualizations.Actionable = false;
            visualizations.OnSelect.AddListener(SelectionChanged);
            visualizations.OnDeselect.AddListener(SelectionChanged);
            Module3DMain.OnAddScene.AddListener(AddScene);
            Module3DMain.OnRemoveScene.AddListener(RemoveScene);
            connection.OnChanged.AddListener(Refresh);
            if (Module3DMain.IsInitialized)
            {
                foreach (var scene in Module3DMain.Scenes) AddScene(scene);
                if (Module3DMain.SelectedScene != null) visualizations.Select(Module3DMain.SelectedScene.Visualization);
            }

            connection.SetDiscoveryActive(true);
            Refresh();
        }

        private void ChangeMode(bool isManual)
        {
            if (rebuilding) return;
            if (isManual) connection.SelectManual(address.text);
            else connection.SelectAutomatic();
            Refresh();
        }

        private void ChangeAddress(string value)
        {
            if (manual.isOn) connection.SelectManual(value);
            Refresh();
        }

        private void SelectDevice(int index)
        {
            if (rebuilding || manual.isOn) return;
            int candidate = index - (missingSelection ? 1 : 0);
            if (candidate >= 0 && candidate < connection.Devices.Count) connection.Select(connection.Devices[candidate]);
            else
            {
                devices.SetValueWithoutNotify(selectedOption);
                devices.RefreshShownValue();
            }
        }

        private void AddScene(Base3DScene scene)
        {
            if (scene == null || scene.IsClosing || scenes.ContainsKey(scene.Visualization)) return;
            scenes.Add(scene.Visualization, scene);
            visualizations.Add(scene.Visualization);
            Refresh();
        }

        private void RemoveScene(Base3DScene scene)
        {
            if (!scenes.TryGetValue(scene.Visualization, out var displayed) || displayed != scene) return;
            scenes.Remove(scene.Visualization);
            visualizations.Remove(scene.Visualization);
            Refresh();
        }

        private void SelectionChanged(Visualization _) => Refresh();

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh || connection == null) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            foreach (var entry in scenes.Where(entry => entry.Value == null || entry.Value.IsClosing).ToArray())
            {
                scenes.Remove(entry.Key);
                visualizations.Remove(entry.Key);
            }

            visualizations.Refresh();
            Refresh();
        }

        private void Refresh()
        {
            if (!this || connection == null) return;
            rebuilding = true;
            var labels = connection.Devices.Select(device => device.Label).ToList();
            int index = connection.Devices.ToList().FindIndex(device => device.Id == connection.SelectedId);
            missingSelection = !manual.isOn && connection.SelectedId != null && index < 0;
            if (missingSelection) labels.Insert(0, "Selected Quest unavailable");
            if (labels.Count == 0) labels.Add("No Quest found");
            if (!devices.options.Select(option => option.text).SequenceEqual(labels))
            {
                devices.ClearOptions();
                devices.AddOptions(labels);
            }

            selectedOption = missingSelection ? 0 : Math.Max(0, index);
            devices.SetValueWithoutNotify(selectedOption);
            devices.RefreshShownValue();
            rebuilding = false;
            automaticFields.SetActive(!manual.isOn);
            manualFields.SetActive(manual.isOn);
            bool available = Interactable && !requesting && !sending && !connection.IsBusy;
            automatic.interactable = manual.interactable = available;
            address.interactable = available;
            devices.interactable = available && connection.Devices.Count > 0;
            pair.interactable = available && (connection.IsSelectedConnected || connection.CanRetrySceneOperations || (manual.isOn ? !string.IsNullOrWhiteSpace(address.text) : connection.Devices.Count > 0 && !missingSelection));
            pairLabel.text = connection.IsSelectedConnected ? connection.CanRetrySceneOperations ? "Retry" : "Reconnect" : "Connect";
            disconnect.interactable = available && connection.IsPaired;
            visualizations.Interactable = available;
            var selected = SelectedScenes();
            send.interactable = available && connection.IsPaired && selected.Length > 0;
        }

        private Base3DScene[] SelectedScenes() => visualizations.ObjectsSelected.Where(visualization => scenes.ContainsKey(visualization)).Select(visualization => scenes[visualization]).Where(scene => scene != null && !scene.IsClosing).ToArray();

        private async void Connect()
        {
            if (!pair.interactable) return;
            requesting = true;
            Refresh();
            try
            {
                await connection.PairAsync(token => DialogBoxManager.OpenInputAsync("Pair with Quest", "Enter the six-digit code displayed in your Quest.", "• • • • • •", "Pair", "Cancel", token));
                if (!this) return;
                if (!connection.LastOperationCancelled && connection.BlockingError != null && !ReferenceEquals(connection.BlockingError, ApplicationManager.DatabaseInitializationPresentedError))
                    await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Quest pairing", TransferError.Describe(connection.BlockingError));
            }
            catch (Exception error)
            {
                if (this) await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Quest pairing", TransferError.Describe(error));
            }
            finally
            {
                requesting = false;
                if (this) Refresh();
            }
        }

        private void Disconnect()
        {
            if (disconnect.interactable) connection.Disconnect();
        }

        private async void Send()
        {
            if (!send.interactable) return;
            var selected = SelectedScenes();
            sending = true;
            Refresh();
            try
            {
                await connection.RetryDisplayScenesAsync(selected, CancellationToken.None);
            }
            catch (Exception error)
            {
                if (this) await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Quest transfer", TransferError.Describe(error));
            }
            finally
            {
                sending = false;
                if (this) Refresh();
            }
        }

        private void OnDestroy()
        {
            Module3DMain.OnAddScene.RemoveListener(AddScene);
            Module3DMain.OnRemoveScene.RemoveListener(RemoveScene);
            if (connection == null) return;
            if (requesting) connection.CancelConnectionAttempt();
            connection.OnChanged.RemoveListener(Refresh);
            connection.SetDiscoveryActive(false);
        }
    }
}
