using System.Collections.Generic;
using HBP.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestTagTable : MonoBehaviour
    {
        [SerializeField] private Text heading, empty;
        [SerializeField] private GameObject columnHeaders;
        [SerializeField] private Transform content;
        [SerializeField] private QuestTagRow rowTemplate;
        private readonly List<QuestTagRow> rows = new();

        public void Bind(string title, IReadOnlyList<BaseTagValue> tags)
        {
            gameObject.SetActive(tags != null);
            if (tags == null) return;
            heading.text = title;
            empty.gameObject.SetActive(tags.Count == 0);
            columnHeaders.SetActive(tags.Count > 0);
            while (rows.Count < tags.Count) rows.Add(Instantiate(rowTemplate, content, false));
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(i < tags.Count);
                if (i < tags.Count) rows[i].Bind(tags[i].Tag.Name, tags[i].DisplayableValue, i % 2 == 0);
            }
        }
    }
}
