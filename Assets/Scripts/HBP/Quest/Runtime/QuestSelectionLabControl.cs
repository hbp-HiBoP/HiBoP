using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Authored button binding. The label and interaction share one source of truth.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class QuestSelectionLabControl : MonoBehaviour
    {
        [SerializeField] private string command;
        [SerializeField] private int page = -1;
        public int Page => page;
        private QuestSelectionLab lab;
        private Text label;

        private void Start()
        {
            lab = GetComponentInParent<QuestSelectionLab>();
            label = GetComponentInChildren<Text>(true);
            GetComponent<Button>().onClick.AddListener(() => lab.Execute(command));
        }

        private void Update()
        {
            if (lab != null && label != null)
            {
                label.text = lab.ControlLabel(command);
                GetComponent<Button>().interactable = lab.ControlEnabled(command);
            }
        }
    }
}
