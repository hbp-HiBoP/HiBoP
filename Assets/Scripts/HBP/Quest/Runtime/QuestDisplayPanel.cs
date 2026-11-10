using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Quest
{
    /// <summary>Manages the available visualizations and their local presentation.</summary>
    public sealed class QuestDisplayPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private Transform rows;
        [SerializeField] private QuestSceneRow rowPrefab;
        private readonly Dictionary<QuestAnatomyView.Presentation, QuestSceneRow> controls = new();

        private void Awake()
        {
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (view == null)
            {
                return;
            }

            foreach (var old in controls.Keys.Where(value => !view.Scenes.Contains(value)).ToArray())
            {
                controls[old].gameObject.SetActive(false);
                Destroy(controls[old].gameObject);
                controls.Remove(old);
            }

            foreach (var value in view.Scenes)
                if (!controls.ContainsKey(value))
                {
                    var row = Instantiate(rowPrefab, rows, false);
                    row.Bind(value, session, view);
                    controls.Add(value, row);
                }
        }
    }
}
