using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestToolbar : MonoBehaviour
    {
        [SerializeField] private QuestWindowsManager windows;
        [SerializeField] private QuestConnectionPanel connection;
        [SerializeField] private QuestCutsPanel cuts;
        [SerializeField] private Button connectionButton, cutsButton, displayButton, recenterButton;
        [SerializeField] private Text connectionStatus;

        private void Awake()
        {
            foreach (var button in GetComponentsInChildren<QuestToolbarButton>(true)) button.Configure(windows);
            connectionButton.onClick.AddListener(() => ToggleWindow("Connection"));
            displayButton.onClick.AddListener(() => ToggleWindow("Display"));
            recenterButton.onClick.AddListener(windows.RecenterOpen);
            cutsButton.onClick.AddListener(() =>
            {
                if (cuts != null && cuts.Ready) ToggleWindow("Cuts");
            });
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
            cutsButton.interactable = cuts != null && cuts.Ready;
        }
    }
}
