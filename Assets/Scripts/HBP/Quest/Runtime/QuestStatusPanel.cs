using UnityEngine.Events;
using System;
using UnityEngine;
using UnityEngine.UI;
using HBP.Transfer.Transport;

namespace HBP.Quest
{
    /// <summary>Serialized world-space Quest connection window content.</summary>
    public sealed class QuestStatusPanel : MonoBehaviour
    {
        [SerializeField] private Text code;
        [SerializeField] private Text address;
        [SerializeField] private Text countdown;
        [SerializeField] private Image lifetimeGauge;
        [SerializeField] private GameObject timer;
        [SerializeField] private Button retry;
        [SerializeField] private Button newAssociation;
        [SerializeField] private Text retryLabel;
        [SerializeField] private Text newAssociationLabel;
        [SerializeField] private GameObject actions;
        [SerializeField] private GameObject unpairedContent;
        [SerializeField] private GameObject pairedContent;
        [SerializeField] private Text pairedDesktop;
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
            unpairedContent.SetActive(true);
            pairedContent.SetActive(false);
            bool available = state.Phase == PairingPhase.Available;
            SetDetails(available ? state.Code.Substring(0, 3) + " " + state.Code.Substring(3) : "", ip);
            timer.SetActive(available);
            int seconds = (int)Math.Ceiling(state.Remaining.TotalSeconds);
            countdown.text = $"{seconds / 60:00}:{seconds % 60:00}";
            lifetimeGauge.fillAmount = Mathf.Clamp01((float)(state.Remaining.TotalSeconds / QuestPairing.CodeLifetime.TotalSeconds));
            Actions(false, false);
        }

        private void Actions(bool canRetry, bool canReplace)
        {
            if (!canReplace) confirming = false;
            retry.gameObject.SetActive(canRetry || confirming);
            newAssociation.gameObject.SetActive(canReplace);
            retryLabel.text = confirming ? "Cancel" : "Retry";
            newAssociationLabel.text = confirming ? "Confirm" : "Forget association";
            actions.SetActive(canRetry || confirming || canReplace);
        }

        public void ShowPaired(string desktopAddress, bool canForget)
        {
            unpairedContent.SetActive(false);
            pairedContent.SetActive(true);
            pairedDesktop.text = string.IsNullOrEmpty(desktopAddress) ? "Desktop" : $"Desktop ({desktopAddress})";
            if (!canForget) confirming = false;
            Actions(false, true);
            newAssociation.interactable = canForget;
        }

        public void ShowUnavailable(string details)
        {
            unpairedContent.SetActive(true);
            pairedContent.SetActive(false);
            SetDetails("", "");
            timer.SetActive(false);
            Actions(false, false);
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

        private void SetDetails(string pairingCode, string ip)
        {
            code.text = pairingCode;
            code.gameObject.SetActive(!string.IsNullOrEmpty(pairingCode));
            address.text = ip ?? "";
            address.gameObject.SetActive(!string.IsNullOrEmpty(ip));
        }
    }
}
