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
        [SerializeField] private Button newAssociation;
        [SerializeField] private GameObject unpairedContent;
        [SerializeField] private GameObject pairedContent;
        [SerializeField] private Text pairedDesktop;
        public UnityEvent OnNewAssociationRequested { get; } = new();
        private void Awake() => newAssociation.onClick.AddListener(OnNewAssociationRequested.Invoke);

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
            newAssociation.gameObject.SetActive(false);
        }

        public void ShowPaired(string desktopAddress, bool canForget)
        {
            unpairedContent.SetActive(false);
            pairedContent.SetActive(true);
            pairedDesktop.text = string.IsNullOrEmpty(desktopAddress) ? "Desktop" : $"Desktop ({desktopAddress})";
            newAssociation.gameObject.SetActive(true);
            newAssociation.interactable = canForget;
        }

        public void ShowUnavailable(string details)
        {
            unpairedContent.SetActive(true);
            pairedContent.SetActive(false);
            SetDetails("", "");
            timer.SetActive(false);
            newAssociation.gameObject.SetActive(false);
        }

        public void Hide()
        {
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
