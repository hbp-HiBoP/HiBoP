using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Quest.Desktop;
using HBP.Transfer.Transport;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.Quest
{
    public sealed class QuestPairingWindow : DialogWindow
    {
        #region Properties

        [SerializeField] private Dropdown devices;
        [SerializeField] private Toggle manual;
        [SerializeField] private InputField address;
        [SerializeField] private Text status;
        [SerializeField] private Button pair;

        private QuestManager connection;
        private bool rebuilding;
        private bool missingSelection;
        private int selectedOption;

        #endregion

        #region Private Methods

        protected override void Initialize()
        {
            base.Initialize();

            connection = QuestManager.Instance;
            connection.SetDiscoveryActive(true);
            devices.onValueChanged.AddListener(index =>
            {
                if (!rebuilding && !manual.isOn) Select(index);
            });
            manual.onValueChanged.AddListener(_ => SelectMode());
            address.onEndEdit.AddListener(_ =>
            {
                if (manual.isOn) connection.SelectManual(address.text);
            });
            connection.Changed += Refresh;
            Refresh();
        }

        private void SelectMode()
        {
            if (manual.isOn) connection.SelectManual(address.text);
            else Select(devices.value);
            Refresh();
        }

        private void Select(int index)
        {
            int candidate = index - (missingSelection ? 1 : 0);
            if (candidate >= 0 && candidate < connection.Devices.Count) connection.Select(connection.Devices[candidate]);
            else
            {
                devices.SetValueWithoutNotify(selectedOption);
                devices.RefreshShownValue();
            }
        }

        private async UniTask PairAsync()
        {
            if (manual.isOn) connection.SelectManual(address.text);
            await connection.PairAsync(token => DialogBoxManager.OpenInputAsync("Pair with Quest", "Enter the six-digit code displayed in your Quest.", "• • • • • •", "Pair", "Cancel", token));
        }

        private void Refresh()
        {
            if (!this || connection == null) return;
            rebuilding = true;
            devices.ClearOptions();
            int index = connection.Devices.ToList().FindIndex(x => x.Id == connection.SelectedId);
            missingSelection = !manual.isOn && connection.SelectedId != null && index < 0;
            var labels = connection.Devices.Select(x => x.Label).ToList();
            if (missingSelection) labels.Insert(0, "Selected Quest unavailable");
            labels.AddRange(connection.DiscoveryMessages);
            devices.AddOptions(labels);
            selectedOption = missingSelection ? 0 : Math.Max(0, index);
            devices.SetValueWithoutNotify(selectedOption);
            devices.RefreshShownValue();
            rebuilding = false;
            devices.interactable = !connection.IsBusy && !manual.isOn && connection.Devices.Count > 0;
            address.interactable = !connection.IsBusy;
            manual.interactable = !connection.IsBusy;
            pair.interactable = !connection.IsBusy && !connection.IsPaired && (manual.isOn ? !string.IsNullOrWhiteSpace(address.text) : connection.Devices.Count > 0 && !missingSelection);
            status.text = connection.Status;
        }

        private void OnDestroy()
        {
            if (connection == null) return;
            connection.Changed -= Refresh;
            connection.SetDiscoveryActive(false);
        }

        #endregion

        #region Public Methods

        public override async void OK()
        {
            await PairAsync();
            bool paired = connection.IsPaired;
            string result = connection.Status;
            base.OK();
            await UniTask.NextFrame();
            await DialogBoxManager.OpenAsync(paired ? DialogBoxType.Informational : DialogBoxType.Error, paired ? "Quest paired" : "Quest pairing failed", paired ? "Your Quest is paired and ready." : result);
        }

        #endregion
    }
}
