using UnityEngine;
using UnityEngine.UI;
using System;
using HBP.Transfer.Transport;

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
        public event Action RetryRequested;
        public event Action NewAssociationRequested;
        private bool confirming;

        private void Awake()
        {
            retry.onClick.AddListener(() =>
            {
                if (confirming) confirming = false;
                else RetryRequested?.Invoke();
            });
            newAssociation.onClick.AddListener(() =>
            {
                if (confirming) { confirming = false; NewAssociationRequested?.Invoke(); }
                else confirming = true;
            });
        }

        public void ShowPairing(PairingStatus state, string ip)
        {
            gameObject.SetActive(true);
            heading.text = "Pair with Desktop";
            bool available = state.Phase == PairingPhase.Available;
            code.text = available ? state.Code.Substring(0, 3) + " " + state.Code.Substring(3) : "";
            address.text = ip ?? "";
            message.text = state.Phase switch
            {
                PairingPhase.Authenticating => "Connecting...",
                PairingPhase.AttemptsExhausted => "Five attempts used. Select Retry.",
                PairingPhase.InstallingGlobals => "Resume pairing on Desktop.",
                _ => "Enter this code on Desktop."
            };
            countdown.gameObject.SetActive(available);
            lifetimeGauge.gameObject.SetActive(available);
            int seconds = (int)Math.Ceiling(state.Remaining.TotalSeconds);
            countdown.text = $"New code in {seconds / 60:00}:{seconds % 60:00}";
            lifetimeGauge.fillAmount = Mathf.Clamp01((float)(state.Remaining.TotalSeconds / QuestPairing.CodeLifetime.TotalSeconds));
            Actions(state.Phase == PairingPhase.AttemptsExhausted, state.Phase != PairingPhase.Authenticating);
        }

        private void Actions(bool canRetry, bool canReplace)
        {
            if (!canReplace) confirming = false;
            retry.gameObject.SetActive(canRetry || confirming);
            newAssociation.gameObject.SetActive(canReplace);
            retryLabel.text = confirming ? "Cancel" : "Retry";
            newAssociationLabel.text = confirming ? "Confirm" : "New association";
            ((RectTransform)newAssociation.transform).anchoredPosition = new Vector2(canRetry || confirming ? 130 : 0, -152);
            if (confirming) message.text = "Replace the Desktop association?";
        }

        public void ShowWaiting()
        {
            gameObject.SetActive(true);
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
            gameObject.SetActive(true);
            heading.text = "Quest connection unavailable";
            code.text = "";
            address.text = "";
            message.text = details;
            countdown.gameObject.SetActive(false);
            lifetimeGauge.gameObject.SetActive(false);
            Actions(true, false);
        }

        public void Hide() { confirming = false; gameObject.SetActive(false); }
    }
}
