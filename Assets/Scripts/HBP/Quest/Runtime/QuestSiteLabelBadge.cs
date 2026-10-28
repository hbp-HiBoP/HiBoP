using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSiteLabelBadge : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Button remove;
        private Action action;
        private void Awake() => remove.onClick.AddListener(() => action?.Invoke());

        public void Bind(string text, Action callback)
        {
            label.supportRichText = false;
            label.text = text + "   ×";
            action = callback;
        }
    }
}
