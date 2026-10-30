using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestTagRow : MonoBehaviour
    {
        [SerializeField] private Text tagName, value;
        [SerializeField] private HBP.Theme.ThemeElement background;
        [SerializeField] private HBP.Theme.State alternateState;

        public void Bind(string name, string text, bool alternate)
        {
            tagName.text = name ?? "";
            value.text = text ?? "";
            background.Set(alternate ? alternateState : background.Element.DefaultState);
        }
    }
}
