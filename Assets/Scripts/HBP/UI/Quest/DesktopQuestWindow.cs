using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        [SerializeField] private Button refreshDevices;
        [SerializeField] private Toggle automatic;
        [SerializeField] private Toggle manual;
        [SerializeField] private GameObject automaticFields;
        [SerializeField] private GameObject manualFields;
        [SerializeField] private InputField address;
        [SerializeField] private GameObject manualTransport;
        [SerializeField] private Toggle localNetwork;
        [SerializeField] private Toggle usb;
        [SerializeField] private GameObject usbInterfaceRow;
        [SerializeField] private Dropdown usbInterfaces;
        [SerializeField] private Button pair;
        [SerializeField] private Text pairLabel;
        [SerializeField] private Button disconnect;
        [SerializeField] private VisualizationList visualizations;
        [SerializeField] private Button send;

        private readonly Dictionary<Visualization, Base3DScene> scenes = new();
        private QuestManager connection;
        private bool rebuilding, requesting, sending, refreshingDevices;
        private int firstDeviceOption;
        private QuestDevice[] listedDevices = Array.Empty<QuestDevice>();
        private (NetworkInterface Network, int Index)[] interfaces = Array.Empty<(NetworkInterface, int)>();
        private string selectedInterfaceId;
        private bool explicitlySelectedInterface;
        private CancellationTokenSource interfaceProbe;
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
            localNetwork.SetIsOnWithoutNotify(!connection.ManualUsesUsb);
            usb.SetIsOnWithoutNotify(connection.ManualUsesUsb);
            selectedInterfaceId = connection.ManualInterfaceId;
            explicitlySelectedInterface = connection.ManualInterfaceWasExplicit;
            pair.onClick.AddListener(Connect);
            refreshDevices.onClick.AddListener(RefreshDevices);
            disconnect.onClick.AddListener(Disconnect);
            send.onClick.AddListener(Send);
            devices.onValueChanged.AddListener(SelectDevice);
            manual.onValueChanged.AddListener(ChangeMode);
            address.onValueChanged.AddListener(ChangeAddress);
            usb.onValueChanged.AddListener(ChangeManualTransport);
            usbInterfaces.onValueChanged.AddListener(SelectInterface);
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
            RefreshInterfaces(true);
            Refresh();
        }

        private void ChangeMode(bool isManual)
        {
            if (rebuilding) return;
            if (isManual) connection.SelectManual(address.text, usb.isOn);
            else connection.SelectAutomatic();
            RefreshInterfaces(true);
            Refresh();
        }

        private void ChangeAddress(string value)
        {
            if (manual.isOn) connection.SelectManual(value, usb.isOn);
            RefreshInterfaces(true);
            Refresh();
        }

        private void SelectDevice(int index)
        {
            if (rebuilding || manual.isOn) return;
            int candidate = index - firstDeviceOption;
            if (candidate >= 0 && candidate < listedDevices.Length) connection.Select(listedDevices[candidate]);
        }

        private async void RefreshDevices()
        {
            if (!refreshDevices.interactable) return;
            refreshingDevices = true;
            Refresh();
            var stop = destroyCancellationToken;
            try
            {
                string result = await connection.RefreshDiscoveryAsync(stop);
                if (result == null || !this || !isActiveAndEnabled) return;
                string help = Application.platform switch
                {
                    RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => "In Windows, check that the USB Ethernet adapter appears when you plug in the Quest. Ask your administrator to check incoming mDNS (UDP 5353) for HiBoP on that adapter and its active firewall profile.",
                    RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => "On macOS, open HiBoP from Finder and allow it in System Settings > Privacy & Security > Local Network.",
                    _ => "On Linux, check that the CDC NCM interface has a link-local IPv6 address (ip -6 addr). If it is filtered, allow mDNS (UDP 5353) in the firewall zone for that interface."
                };
                await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Informational, "No Quest found", result +
                    "\n\nKeep the headset awake, open HiBoP on it and approve USB networking. Check the cable or use the same local network.\n\n" + help +
                    "\n\nIf you know the Quest IP address, you can connect using Manual mode.", stop, "OK");
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            finally
            {
                refreshingDevices = false;
                if (this) Refresh();
            }
        }

        private void ChangeManualTransport(bool useUsb)
        {
            if (rebuilding || !manual.isOn) return;
            connection.SelectManual(address.text, useUsb);
            RefreshInterfaces(true);
            Refresh();
        }

        private void SelectInterface(int value)
        {
            if (rebuilding || value <= 0 || value > interfaces.Length) return;
            selectedInterfaceId = interfaces[value - 1].Network.Id;
            explicitlySelectedInterface = true;
            interfaceProbe?.Cancel();
            Refresh();
        }

        private IPAddress ManualIp()
        {
            string text = address.text.Trim().Trim('[', ']');
            int zone = text.LastIndexOf('%');
            return IPAddress.TryParse(zone < 0 ? text : text.Substring(0, zone), out var ip) ? ip : null;
        }

        private void RefreshInterfaces(bool inputChanged)
        {
            if (!manual.isOn || !usb.isOn)
            {
                interfaceProbe?.Cancel();
                return;
            }

            if (usbInterfaces.transform.Find("Dropdown List")) return;
            var ip = ManualIp();
            var next = AvailableInterfaces(ip);
            bool changed = !interfaces.Select(item => (item.Network.Id, item.Index)).SequenceEqual(next.Select(item => (item.Network.Id, item.Index)));
            if (!changed && !inputChanged) return;
            interfaces = next;
            interfaceProbe?.Cancel();
            if (!interfaces.Any(item => item.Network.Id == selectedInterfaceId)) explicitlySelectedInterface = false;
            if (explicitlySelectedInterface) return;
            selectedInterfaceId = null;
            if (ip?.IsIPv6LinkLocal == true && interfaces.Length != 0)
            {
                interfaceProbe = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                ProbeInterfacesAsync(ip, interfaces, interfaceProbe).Forget();
            }
        }

        private static (NetworkInterface Network, int Index)[] AvailableInterfaces(IPAddress ip)
        {
            var family = ip?.AddressFamily ?? AddressFamily.InterNetworkV6;
            var networks = QuestEndpoint.LocalInterfaces(family).Select(n => (Network: n, Index: QuestEndpoint.GetInterfaceIndex(n, family)));
            if (ip == null)
                networks = networks.Concat(QuestEndpoint.LocalInterfaces(AddressFamily.InterNetwork).Select(n => (Network: n, Index: QuestEndpoint.GetInterfaceIndex(n, AddressFamily.InterNetwork))))
                    .GroupBy(item => item.Network.Id).Select(group => group.First());
            return networks.Where(item => ip?.IsIPv6LinkLocal != true || item.Network.GetIPProperties().UnicastAddresses.Any(a => a.Address.IsIPv6LinkLocal))
                .OrderBy(item => item.Network.Name).ThenBy(item => item.Network.Id).ToArray();
        }

        private static QuestEndpoint ScopedRoute(IPAddress ip, QuestNetworkPath path, (NetworkInterface Network, int Index) route) =>
            new(ip, path: path, interfaceIndex: route.Index, interfaceId: route.Network.Id, interfaceName: route.Network.Name);

        private static async Task<int> FindRespondingInterfaceAsync(IPAddress ip, (NetworkInterface Network, int Index)[] candidates, CancellationToken stop)
        {
            async Task<bool> Responds((NetworkInterface Network, int Index) candidate)
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
                deadline.CancelAfter(1500);
                try
                {
                    await QuestPairing.DescribeAsync(ScopedRoute(ip, QuestNetworkPath.LocalNetwork, candidate).ConnectionAddress, deadline.Token);
                    return true;
                }
                catch (Exception error) when (error is System.IO.IOException or SocketException or TimeoutException or OperationCanceledException)
                {
                    return false;
                }
            }

            var responds = await Task.WhenAll(candidates.Select(Responds));
            stop.ThrowIfCancellationRequested();
            return responds.Count(result => result) == 1 ? Array.IndexOf(responds, true) : -1;
        }

        private async UniTaskVoid ProbeInterfacesAsync(IPAddress ip, (NetworkInterface Network, int Index)[] candidates, CancellationTokenSource probe)
        {
            var stop = probe.Token;
            try
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(300), ignoreTimeScale: true, cancellationToken: stop);
                int result = await FindRespondingInterfaceAsync(ip, candidates, stop);
                if (result < 0) return;
                selectedInterfaceId = candidates[result].Network.Id;
                Refresh();
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            finally
            {
                if (interfaceProbe == probe) interfaceProbe = null;
                probe.Dispose();
            }
        }

        private async UniTask<QuestEndpoint> ManualRouteAsync(CancellationToken stop)
        {
            if (usb.isOn)
                return ScopedRoute(ManualIp(), QuestNetworkPath.Usb, interfaces.First(item => item.Network.Id == selectedInterfaceId));

            try
            {
                return QuestEndpoint.Parse(address.text, manual: true);
            }
            catch (QuestInterfaceRequiredException required)
            {
                var ip = IPAddress.Parse(required.AddressText);
                var candidates = AvailableInterfaces(ip);
                int result = await FindRespondingInterfaceAsync(ip, candidates, stop);
                if (result >= 0) return ScopedRoute(ip, QuestNetworkPath.LocalNetwork, candidates[result]);
                throw new ArgumentException("Add the Desktop network interface after % in the link-local IPv6 address, or enter the Quest's routable network address.");
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
            RefreshInterfaces(false);
            Refresh();
        }

        private void Refresh()
        {
            if (!this || connection == null) return;
            rebuilding = true;
            bool routeAvailable = connection.Devices.Any(device => device.RouteKey == connection.SelectedRouteKey);
            if (!devices.transform.Find("Dropdown List"))
            {
                listedDevices = connection.Devices.ToArray();
                var labels = listedDevices.Select(device => device.Label).ToList();
                int index = Array.FindIndex(listedDevices, device => device.RouteKey == connection.SelectedRouteKey);
                firstDeviceOption = index < 0 ? 1 : 0;
                if (index < 0) labels.Insert(0, listedDevices.Length == 0 ? "No Quest found" : "Select a Quest");
                SetOptions(devices, labels, index + firstDeviceOption);
            }

            int selectedInterface = Array.FindIndex(interfaces, item => item.Network.Id == selectedInterfaceId);
            if (manual.isOn && usb.isOn && !usbInterfaces.transform.Find("Dropdown List"))
            {
                var labels = interfaces.Select(item => $"{item.Network.Name} ({item.Index})").Prepend(interfaces.Length == 0 ? "No interface available" : "Select an interface").ToList();
                SetOptions(usbInterfaces, labels, selectedInterface + 1);
            }
            automaticFields.SetActive(!manual.isOn);
            manualFields.SetActive(manual.isOn);
            manualTransport.SetActive(manual.isOn);
            usbInterfaceRow.SetActive(manual.isOn && usb.isOn);
            rebuilding = false;
            bool available = Interactable && !requesting && !sending && !refreshingDevices && !connection.IsBusy;
            automatic.interactable = manual.interactable = available;
            refreshDevices.interactable = available;
            localNetwork.interactable = usb.interactable = available;
            address.interactable = available;
            devices.interactable = available && connection.Devices.Count > 0;
            usbInterfaces.interactable = available && interfaces.Length > 0;
            bool canConnect = manual.isOn ? !string.IsNullOrWhiteSpace(address.text) && (!usb.isOn || selectedInterface >= 0 && ManualIp() != null) : routeAvailable;
            pair.interactable = available && (connection.IsSelectedConnected || canConnect);
            pairLabel.text = connection.IsSelectedConnected ? connection.CanRetrySceneOperations ? "Retry" : "Reconnect" : "Connect";
            disconnect.interactable = available && connection.IsPaired;
            visualizations.Interactable = available;
            var selected = SelectedScenes();
            send.interactable = available && connection.IsPaired && selected.Length > 0;
        }

        private static void SetOptions(Dropdown dropdown, List<string> options, int selected)
        {
            if (!dropdown.options.Select(option => option.text).SequenceEqual(options))
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(options);
            }

            dropdown.SetValueWithoutNotify(selected);
            dropdown.RefreshShownValue();
        }

        private Base3DScene[] SelectedScenes() => visualizations.ObjectsSelected.Where(visualization => scenes.ContainsKey(visualization)).Select(visualization => scenes[visualization]).Where(scene => scene != null && !scene.IsClosing).ToArray();

        private async void Connect()
        {
            if (!pair.interactable) return;
            requesting = true;
            Refresh();
            try
            {
                if (manual.isOn)
                {
                    var route = await ManualRouteAsync(destroyCancellationToken);
                    connection.SelectManual(address.text, usb.isOn, route, usb.isOn && explicitlySelectedInterface);
                }

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
            interfaceProbe?.Cancel();
            Module3DMain.OnAddScene.RemoveListener(AddScene);
            Module3DMain.OnRemoveScene.RemoveListener(RemoveScene);
            if (connection == null) return;
            if (requesting) connection.CancelConnectionAttempt();
            connection.OnChanged.RemoveListener(Refresh);
            connection.SetDiscoveryActive(false);
        }
    }
}
