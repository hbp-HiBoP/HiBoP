using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Data.Module3D;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestROIPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestWindow window;
        [SerializeField] private QuestDropdown selector;
        [SerializeField] private QuestTextInputField roiName;
        [SerializeField] private Button addROI, removeROI, addSphere, suspend;
        [SerializeField] private Text suspendLabel;
        [SerializeField] private QuestROISphereRow rowPrefab;
        [SerializeField] private RectTransform rows;
        [SerializeField] private Text status;
        private readonly Dictionary<Sphere, QuestROISphereRow> controls = new();
        private readonly List<ROI> options = new();
        private Base3DScene scene;
        private ROI roi, nameTarget;
        private bool nameEditing;
        private bool suspended;
        public bool Ready => view != null && scene == view.Scene && QuestROICommands.Ready(view.Scene) && view.Find(view.Scene) is { IsPlaced: true, IsTransitioning: false };
        public bool GuidesEnabled => isActiveAndEnabled && window != null && window.IsOpen && !suspended;
        public bool ManipulationEnabled => GuidesEnabled;
        public bool Suspended => suspended;
        public IReadOnlyCollection<QuestROISphereRow> Rows => controls.Values;

        private void Awake()
        {
            suspend.onClick.AddListener(() => suspended = !suspended);
            selector.onValueChanged.AddListener(value =>
            {
                if (!Ready || value < 0 || value > options.Count) return;
                var target = value == 0 ? null : options[value - 1];
                if (target != null && !scene.ROIManager.ROIs.Contains(target)) return;
                CancelDrafts();
                QuestROICommands.Change(scene, () => scene.ROIManager.SelectedROI = target);
            });
            addROI.onClick.AddListener(() =>
            {
                if (Ready) QuestROICommands.Change(scene, () => scene.ROIManager.AddROI());
            });
            removeROI.onClick.AddListener(() =>
            {
                if (!QuestROICommands.Contains(scene, roi)) return;
                var target = roi;
                CancelDrafts();
                QuestROICommands.Change(scene, () => scene.ROIManager.RemoveROI(target));
            });
            addSphere.onClick.AddListener(AddSphere);
            roiName.onValueChanged.AddListener(_ =>
            {
                if (!nameEditing) nameTarget = roi;
                nameEditing = true;
            });
            roiName.onEndEdit.AddListener(value =>
            {
                var target = nameTarget;
                nameEditing = false;
                nameTarget = null;
                if (target == roi && QuestROICommands.Contains(scene, target) && target.Name != value)
                {
                    try
                    {
                        _ = new RenameRoi(new RoiId(target.ID), value);
                        QuestROICommands.Change(scene, () => target.Name = value);
                        status.text = "";
                    }
                    catch (ArgumentException)
                    {
                        status.text = "Enter a non-empty ROI name (maximum 256 UTF-8 bytes).";
                    }
                }
            });
            roiName.KeyboardError += OnKeyboardError;
            window.OnChanged.AddListener(OnWindowChanged);
        }

        private void OnKeyboardError(string message) => status.text = message;

        private void OnWindowChanged(QuestWindow value)
        {
            if (!value.IsOpen) CancelDrafts();
        }

        private void AddSphere()
        {
            if (!Ready || !QuestROICommands.Contains(scene, roi)) return;
            var column = scene.SelectedColumn;
            var presentation = view.AllColumns.FirstOrDefault(value => value.Column == column);
            if (presentation == null || !view.CanInteractWithColumn(presentation)) return;
            var manipulator = presentation.Manipulator;
            Vector3 center = manipulator.SharedMesh != null ? manipulator.GrabCenter : presentation.ScientificFrame.position;
            Vector3 point = presentation.ScientificFrame.InverseTransformPoint(center);
            var head = Camera.main;
            if (head != null)
            {
                var direction = center - head.transform.position;
                var ray = new Ray(head.transform.position, direction);
                Physics.SyncTransforms();
                if (manipulator.Raycast(ray, direction.magnitude, out var distance))
                    point = presentation.ScientificFrame.InverseTransformPoint(ray.GetPoint(distance));
            }

            var target = roi;
            QuestROICommands.Change(scene, () => target.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere", point, 20f));
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            var current = view != null ? view.Scene : null;
            var active = current != null && !current.IsClosing ? current.ROIManager.SelectedROI : null;
            if (scene != current || roi != active)
            {
                CancelDrafts();
                foreach (var row in controls.Values)
                {
                    row.gameObject.SetActive(false);
                    Destroy(row.gameObject);
                }

                controls.Clear();
                scene = current;
                roi = active;
                status.text = "";
            }

            var available = scene != null && !scene.IsClosing ? scene.ROIManager.ROIs : new List<ROI>();
            if (!options.SequenceEqual(available) || selector.options.Count != available.Count + 1 || available.Where((value, i) => selector.options[i + 1].text != value.Name).Any())
            {
                options.Clear();
                options.AddRange(available);
                selector.ClearOptions();
                selector.AddOptions(new[] { "None" }.Concat(options.Select(value => value.Name)).ToList());
            }

            selector.SetValueWithoutNotify(roi == null ? 0 : options.IndexOf(roi) + 1);
            if (!roiName.isFocused && !nameEditing) roiName.SetTextWithoutNotify(roi != null ? roi.Name : "");
            bool ready = Ready;
            addROI.interactable = selector.interactable = ready;
            removeROI.interactable = addSphere.interactable = roiName.interactable = ready && roi != null;
            suspend.interactable = view != null && QuestAnatomySession.CanInteractWith(view.Scene) && view.Scene.IsVisible;
            suspendLabel.text = suspended ? "Show guides" : "Hide guides";
            status.gameObject.SetActive(!string.IsNullOrEmpty(status.text));
            if (!ready) CancelDrafts();
            foreach (var old in controls.Keys.Where(value => roi == null || !roi.Spheres.Contains(value)).ToArray())
            {
                controls[old].gameObject.SetActive(false);
                Destroy(controls[old].gameObject);
                controls.Remove(old);
            }

            if (roi != null)
                foreach (var sphere in roi.Spheres)
                {
                    if (!controls.TryGetValue(sphere, out var row))
                    {
                        row = Instantiate(rowPrefab, rows, false);
                        row.Bind(scene, roi, sphere);
                        controls.Add(sphere, row);
                    }

                    row.transform.SetSiblingIndex(roi.Spheres.IndexOf(sphere));
                }

            addSphere.transform.SetAsLastSibling();
        }

        private void CancelDrafts()
        {
            nameEditing = false;
            nameTarget = null;
            if (roiName != null) roiName.DeactivateInputField();
        }

        private void OnDisable() => CancelDrafts();

        private void OnDestroy()
        {
            if (window != null) window.OnChanged.RemoveListener(OnWindowChanged);
            if (roiName != null) roiName.KeyboardError -= OnKeyboardError;
        }
    }
}
