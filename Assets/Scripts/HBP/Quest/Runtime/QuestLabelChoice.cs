using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestLabelChoice : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Text label;
        private Action<string, bool> changed;
        private string value;
        private void Awake() => toggle.onValueChanged.AddListener(state => changed?.Invoke(value, state));

        public void Bind(string text, bool selected, Action<string, bool> callback)
        {
            value = text;
            label.text = text;
            label.supportRichText = false;
            toggle.SetIsOnWithoutNotify(selected);
            changed = callback;
        }
    }
}
