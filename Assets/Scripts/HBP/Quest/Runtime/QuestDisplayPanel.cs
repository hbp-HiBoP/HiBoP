using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Routes local visibility and the existing scientific projection entry point.</summary>
    public sealed class QuestDisplayPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestAnatomyInput input;
        [SerializeField] private Button surface, projection, recenterBrains;
        [SerializeField] private Text surfaceLabel, status;

        private void Awake()
        {
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

        private bool Ready => view != null && view.Scene != null && !view.IsPreparing;
        private void Update() => Refresh();

        private void Refresh()
        {
            surface.interactable = projection.interactable = recenterBrains.interactable = Ready;
            if (surfaceLabel != null) surfaceLabel.text = view != null && view.SurfaceHidden ? "Show brain surface" : "Hide brain surface";
            if (status != null) status.text = Ready ? "Surface visibility is local to this Quest." : "Waiting for a visualization";
        }
    }
}
