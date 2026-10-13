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
            pairButton.Initialize(this, () => WindowsManager.Open("Quest Pairing window", null));
            sendButton.Initialize(this, () => WindowsManager.Open("Quest Send window", null));
        }

        private void Start()
        {
            connection = QuestManager.Instance;
            connection.OnChanged.AddListener(Refresh);
            Refresh();
        }

        private void Refresh()
        {
            if (sendConditions != null) sendConditions.interactable = connection != null && connection.IsPaired;
        }

        private void OnDestroy()
        {
            if (connection != null) connection.OnChanged.RemoveListener(Refresh);
        }
    }
}
