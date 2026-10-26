using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Quest
{
    /// <summary>Routes scene controls and the existing scientific projection entry point.</summary>
    public sealed class QuestDisplayPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private Button projection;
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private Button openPatient;
        [SerializeField] private Transform rows;
        [SerializeField] private QuestSceneRow rowPrefab;
        private readonly Dictionary<QuestAnatomyView.Presentation, QuestSceneRow> controls = new();

        private void Awake()
        {
            openPatient.onClick.AddListener(() => session.RequestOpenPatient());
            projection.onClick.AddListener(() =>
            {
                if (Ready) view.RecalculateProjection();
            });
            Refresh();
        }

        private bool Ready => view != null && QuestAnatomySession.CanInteractWith(view.Scene) && view.Scene.IsVisible;
        private void Update() => Refresh();

        private void Refresh()
        {
            if (view == null)
            {
                projection.interactable = openPatient.interactable = false;
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
                    row.Bind(value, session);
                    controls.Add(value, row);
                }

            openPatient.interactable = session != null && session.CanOpenPatient;
            projection.interactable = Ready;
        }
    }
}
