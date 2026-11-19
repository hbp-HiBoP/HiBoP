using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Independent Add/Remove toggles with neither selected meaning Keep.</summary>
    public sealed class QuestSitePresetLabelRow : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Toggle add, remove;
        private string value;
        private Action<string, QuestPresetLabelAction> changed;

        private void Awake()
        {
            add.onValueChanged.AddListener(_ => Notify());
            remove.onValueChanged.AddListener(_ => Notify());
        }

        public void Bind(string name, QuestPresetLabelAction action, Action<string, QuestPresetLabelAction> callback)
        {
            value = name;
            changed = callback;
            label.supportRichText = false;
            label.text = name;
            add.SetIsOnWithoutNotify(action == QuestPresetLabelAction.Add);
            remove.SetIsOnWithoutNotify(action == QuestPresetLabelAction.Remove);
        }

        private void Notify() => changed?.Invoke(value, add.isOn ? QuestPresetLabelAction.Add : remove.isOn ? QuestPresetLabelAction.Remove : QuestPresetLabelAction.Keep);
    }
}
