using System;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Quest.Desktop;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public sealed class QuestMenu : Menu
    {
        [SerializeField] private MenuButton pairButton;
        [SerializeField] private MenuButton sendButton;
        [SerializeField] private InteractableConditions sendConditions;
        private QuestManager connection;

        protected override void Awake()
        {
            base.Awake();
            pairButton.Initialize(this, () => OpenPairingWindow().Forget());
            sendButton.Initialize(this, () => WindowsManager.Open("Quest Send window", null));
        }

        private void Start()
        {
            connection = QuestManager.Instance;
            connection.Changed += Refresh;
            Refresh();
        }

        private async UniTask OpenPairingWindow()
        {
            if (connection.IsPaired)
            {
                int choice = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Quest already paired", "A Quest is already paired. Continuing will disconnect it before opening the pairing window.", "Continue", "Cancel");
                if (choice != 0) return;
                try
                {
                    if (connection.IsPaired) connection.Disconnect();
                }
                catch (Exception exception)
                {
                    await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Could not disconnect Quest", exception.Message);
                    return;
                }
            }

            WindowsManager.Open("Quest Pairing window", null);
        }

        private void Refresh()
        {
            if (sendConditions != null) sendConditions.interactable = connection != null && connection.IsPaired;
        }

        private void OnDestroy()
        {
            if (connection != null) connection.Changed -= Refresh;
        }
    }
}
