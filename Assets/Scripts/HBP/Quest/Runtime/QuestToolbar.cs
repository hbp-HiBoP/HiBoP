using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestToolbar : MonoBehaviour
    {
        [SerializeField] private QuestWindowsManager windows;
        [SerializeField] private QuestConnectionPanel connection;
        [SerializeField] private Button connectionButton, cutsButton, displayButton, recenterButton;
        [SerializeField] private Text connectionStatus;

        private void Awake()
        {
            connectionButton.onClick.AddListener(() => windows.Open("Connection"));
            displayButton.onClick.AddListener(() => windows.Open("Display"));
            recenterButton.onClick.AddListener(windows.RecenterOpen);
            cutsButton.interactable = false;
        }

        private void Update()
        {
            if (connectionStatus != null && connection != null) connectionStatus.text = connection.Summary;
        }
    }
}
