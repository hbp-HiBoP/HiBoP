using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Quest
{
    /// <summary>Routes local visibility and the existing scientific projection entry point.</summary>
    public sealed class QuestDisplayPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestAnatomyInput input;
        [SerializeField] private Button surface, projection, recenterBrains;
        [SerializeField] private Text surfaceLabel, status;
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private Button openPatient;
        [SerializeField] private Transform rows;
        [SerializeField] private QuestSceneRow rowPrefab;
        private readonly Dictionary<QuestAnatomyView.Presentation, QuestSceneRow> controls = new();

        private void Awake()
        {
            openPatient.onClick.AddListener(() => session.RequestOpenPatient());
            surface.onClick.AddListener(() =>
            {
                if (Ready) view.ToggleSurface();
            });
            projection.onClick.AddListener(() =>
            {
                if (Ready) view.RecalculateProjection();
            });
            recenterBrains.onClick.AddListener(() =>
            {
                if (Ready) input.RecenterBrains();
            });
            Refresh();
        }

        private bool Ready => view != null && view.Scene != null && view.Scene.IsVisible && !view.Scene.IsClosing;
        private void Update() => Refresh();

        private void Refresh()
        {
            if (view == null)
            {
                surface.interactable = projection.interactable = recenterBrains.interactable = openPatient.interactable = false;
                if (status != null) status.text = "Waiting for a visualization";
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
            surface.interactable = projection.interactable = recenterBrains.interactable = Ready;
            if (surfaceLabel != null) surfaceLabel.text = view != null && view.SurfaceHidden ? "Show brain surface" : "Hide brain surface";
            if (status != null) status.text = session?.ActionStatus ?? (Ready ? "Interact with a column to select its visualization." : "Interact with a visible visualization to select it.");
        }
    }
}
