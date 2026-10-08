using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Controller-accessible numeric draft; does not open a platform keyboard or mutate science.</summary>
    public sealed class QuestNumericKeypad : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private Text title, display;
        [SerializeField] private Button[] keys;
        [SerializeField] private Button apply, cancel;
        private Action<string> completed;
        private UnityEngine.Object owner;
        private string draft;
        private bool replace;
        public bool IsOpen => completed != null;

        private void Awake()
        {
            foreach (var key in keys)
            {
                string value = key.GetComponentInChildren<Text>().text;
                key.onClick.AddListener(() => Enter(value));
            }

            apply.onClick.AddListener(Apply);
            cancel.onClick.AddListener(Cancel);
            content.SetActive(false);
        }

        public void Open(UnityEngine.Object requester, string label, string value, Action<string> callback)
        {
            owner = requester;
            completed = callback;
            draft = value;
            replace = true;
            title.text = label;
            display.text = draft;
            content.SetActive(true);
            content.transform.SetAsLastSibling();
        }

        public void Enter(string value)
        {
            if (!IsOpen) return;
            if (value == "Clear") draft = "";
            else if (value == "Back") draft = draft.Length > 0 ? draft.Substring(0, draft.Length - 1) : "";
            else if (value == "+/-") draft = draft.StartsWith("-") ? draft.Substring(1) : "-" + draft;
            else if (draft.Length < 16)
            {
                if (replace) draft = "";
                if (value != "." || !draft.Contains(".")) draft += value;
            }

            replace = false;
            display.text = draft;
        }

        public void Apply()
        {
            var callback = completed;
            string result = draft;
            bool alive = owner != null;
            Cancel();
            if (alive) callback?.Invoke(result);
        }

        public void Cancel()
        {
            completed = null;
            owner = null;
            if (content != null) content.SetActive(false);
        }

        public void CancelFor(UnityEngine.Object requester)
        {
            if (owner == requester) Cancel();
        }

        private void OnDisable() => Cancel();
    }
}
