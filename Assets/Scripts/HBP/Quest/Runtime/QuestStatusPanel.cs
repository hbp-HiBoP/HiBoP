using UnityEngine.Events;
using System;
using UnityEngine;
using UnityEngine.UI;
using HBP.Transfer.Transport;
using HBP.Sync.Scene;

namespace HBP.Quest
{
    /// <summary>Serialized world-space Quest status card.</summary>
    public sealed class QuestStatusPanel : MonoBehaviour
    {
        [SerializeField] private Text heading;
        [SerializeField] private Text code;
        [SerializeField] private Text address;
        [SerializeField] private Text message;
        [SerializeField] private Text countdown;
        [SerializeField] private Image lifetimeGauge;
        [SerializeField] private Button retry;
        [SerializeField] private Button newAssociation;
        [SerializeField] private Text retryLabel;
        [SerializeField] private Text newAssociationLabel;
        public UnityEvent OnNewAssociationRequested { get; } = new();
        private bool confirming;

        private void Awake()
        {
            retry.onClick.AddListener(() => { confirming = false; });
            newAssociation.onClick.AddListener(() =>
            {
                if (confirming)
                {
                    confirming = false;
                    OnNewAssociationRequested.Invoke();
                }
                else confirming = true;
            });
        }

        public void ShowPairing(PairingStatus state, string ip)
        {
            heading.text = "Pair with Desktop";
            bool available = state.Phase == PairingPhase.Available;
            code.text = available ? state.Code.Substring(0, 3) + " " + state.Code.Substring(3) : "";
            address.text = ip ?? "";
            message.text = state.Phase switch
            {
                PairingPhase.Authenticating => "Connecting...",
                PairingPhase.AttemptsExhausted => "Five attempts used. Forget the association to pair again.",
                PairingPhase.InstallingGlobals => "Resume pairing on Desktop.",
                _ => "Enter this code on Desktop."
            };
            countdown.gameObject.SetActive(available);
            lifetimeGauge.gameObject.SetActive(available);
            int seconds = (int)Math.Ceiling(state.Remaining.TotalSeconds);
            countdown.text = $"New code in {seconds / 60:00}:{seconds % 60:00}";
            lifetimeGauge.fillAmount = Mathf.Clamp01((float)(state.Remaining.TotalSeconds / QuestPairing.CodeLifetime.TotalSeconds));
            Actions(false, state.Phase != PairingPhase.Authenticating);
        }

        private void Actions(bool canRetry, bool canReplace)
        {
            if (!canReplace) confirming = false;
            retry.gameObject.SetActive(canRetry || confirming);
            newAssociation.gameObject.SetActive(canReplace);
            retryLabel.text = confirming ? "Cancel" : "Retry";
            newAssociationLabel.text = confirming ? "Confirm" : "Forget association";
            ((RectTransform)newAssociation.transform).anchoredPosition = new Vector2(canRetry || confirming ? 130 : 0, -152);
            if (confirming) message.text = "Forget the Desktop association? You will need to enter a new code on Desktop.";
        }

        public void ShowAvailable(string ip)
        {
            heading.text = "Ready for Desktop";
            code.text = "";
            address.text = ip ?? "";
            message.text = "Start the connection from Desktop.";
            countdown.gameObject.SetActive(false);
            lifetimeGauge.gameObject.SetActive(false);
            Actions(false, true);
        }

        public void ShowWaiting()
        {
            heading.text = "Quest paired";
            code.text = "";
            address.text = "";
            message.text = "Waiting for visualization";
            countdown.gameObject.SetActive(false);
            lifetimeGauge.gameObject.SetActive(false);
            Actions(false, true);
        }

        public void ShowUnavailable(string details)
        {
            heading.text = "Quest connection unavailable";
            code.text = "";
            address.text = "";
            message.text = details;
            countdown.gameObject.SetActive(false);
            lifetimeGauge.gameObject.SetActive(false);
            Actions(false, true);
        }

        public void Hide()
        {
            confirming = false;
            GetComponent<QuestWindow>()?.Close();
        }

        public void ShowRequestedCode()
        {
            GetComponent<QuestWindow>()?.Open();
            GetComponent<QuestWindowFollower>()?.Recall();
        }

        public void ShowReconciliation(SceneReconciliationRecord record, bool desktopConnected = false)
        {
            heading.text = desktopConnected && record.Status == ReconciliationStatus.Orphan ? "Desktop connected" : record.Status switch
            {
                ReconciliationStatus.Reconciling => "Reconnecting",
                ReconciliationStatus.OutOfSync => "Synchronization incomplete",
                ReconciliationStatus.Orphan => "Local scene",
                _ => "Available offline"
            };
            code.text = address.text = "";
            message.text = record.Status == ReconciliationStatus.Orphan && desktopConnected ? "This visualization is local to Quest. Its Desktop copy was closed." : record.Message ?? "Reconnect Desktop to merge your changes.";
            countdown.gameObject.SetActive(false);
            lifetimeGauge.gameObject.SetActive(false);
            Actions(false, false);
        }

        public void ShowSession(bool connected, bool receiving)
        {
            heading.text = receiving ? "Receiving visualization" : connected ? "Desktop connected" : "Available offline";
            code.text = address.text = "";
            message.text = receiving ? "Please wait..." : connected ? "Visualization synchronized with Desktop" : "Connection interrupted. Your visualization remains available.";
            countdown.gameObject.SetActive(false);
            lifetimeGauge.gameObject.SetActive(false);
            Actions(false, false);
        }
    }
}
