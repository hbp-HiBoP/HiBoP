using System.Collections.Generic;
using System.Linq;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>All cuts remain editable independently. Window lifecycle only controls local aids.</summary>
    public sealed class QuestCutsPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestWindow window;
        [SerializeField] private QuestCutRow rowPrefab;
        [SerializeField] private RectTransform rows;
        [SerializeField] private QuestNumericKeypad keypad;
        [SerializeField] private Button add, suspend;
        [SerializeField] private Text suspendLabel, status;
        private readonly Dictionary<Cut, QuestCutRow> controls = new();
        private Base3DScene scene;
        private bool suspended;
        public bool Ready => view != null && view.Scene != null && !view.IsPreparing && !view.Scene.IsClosing;
        public bool AidsActive => Ready && window != null && window.IsOpen && !suspended && !view.Scene.AutomaticCutAroundSelectedSite;
        public bool Suspended => suspended;
        public IReadOnlyCollection<QuestCutRow> Rows => controls.Values;

        private void Awake()
        {
            add.onClick.AddListener(() =>
            {
                if (Ready && !view.Scene.AutomaticCutAroundSelectedSite) view.Scene.AddCutPlane();
            });
            suspend.onClick.AddListener(() => suspended = !suspended);
            window.Changed += OnWindowChanged;
        }

        private void OnWindowChanged(QuestWindow value)
        {
            if (!value.IsOpen) keypad.Cancel();
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            var current = Ready ? view.Scene : null;
            if (scene != current)
            {
                keypad.Cancel();
                foreach (var row in controls.Values)
                {
                    row.gameObject.SetActive(false);
                    Destroy(row.gameObject);
                }

                controls.Clear();
                scene = current;
            }

            foreach (var old in controls.Keys.Where(cut => scene == null || !scene.Cuts.Contains(cut)).ToArray())
            {
                controls[old].gameObject.SetActive(false);
                Destroy(controls[old].gameObject);
                controls.Remove(old);
            }

            if (scene != null)
                foreach (var cut in scene.Cuts)
                {
                    if (!controls.TryGetValue(cut, out var row))
                    {
                        row = Instantiate(rowPrefab, rows, false);
                        row.Bind(scene, cut, keypad);
                        controls.Add(cut, row);
                    }

                    row.transform.SetSiblingIndex(cut.Index);
                }

            add.interactable = Ready && !scene.AutomaticCutAroundSelectedSite;
            suspend.interactable = Ready;
            suspendLabel.text = suspended ? "Show guides" : "Hide guides";
            status.text = !Ready ? "Waiting for a visualization" : scene.AutomaticCutAroundSelectedSite ? "Automatic cuts: manual editing is disabled." : "Cuts affect every column in this visualization.";
        }

        private void OnDestroy()
        {
            if (window != null) window.Changed -= OnWindowChanged;
        }
    }
}
