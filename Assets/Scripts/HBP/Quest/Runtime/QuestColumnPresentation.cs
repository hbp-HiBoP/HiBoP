using HBP.Data.Module3D;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Quest
{
    public sealed class QuestColumnPresentation : MonoBehaviour
    {
        [SerializeField] private Transform scientificFrame;
        [SerializeField] private TMPro.TextMeshPro label;
        [SerializeField] private QuestAnatomyManipulator manipulator;
        [SerializeField] private MeshRenderer roiPrefab;
        [SerializeField] private QuestSiteSelectionRing selectionRing;
        private Base3DScene scene;
        public Base3DScene Scene => scene;
        private readonly List<MeshRenderer> spheres = new();
        public Column3D Column { get; private set; }
        public QuestAnatomyManipulator Manipulator => manipulator;

        public void Bind(Base3DScene source, Column3D column)
        {
            if (scene != null) scene.OnSurfaceRepresentationChanged.RemoveListener(OnRepresentationChanged);
            scene = source;
            scene.OnSurfaceRepresentationChanged.AddListener(OnRepresentationChanged);
            Column = column;
            column.transform.SetParent(scientificFrame, false);
            column.transform.localPosition = Vector3.zero;
            column.transform.localRotation = Quaternion.identity;
            column.transform.localScale = Vector3.one;
            if (label != null) label.text = $"{column.Name} ({scene.Visualization.Name})";
            manipulator.Bind(column);
            if (selectionRing != null) selectionRing.Bind(source, column);
        }

        private void OnRepresentationChanged(HBP.Core.Object3D.SurfaceRepresentation value) => manipulator.InvalidateRayTarget();

        private void OnDestroy()
        {
            if (scene != null) scene.OnSurfaceRepresentationChanged.RemoveListener(OnRepresentationChanged);
        }

        public void Hide()
        {
            if (selectionRing != null) selectionRing.SetHidden(true);
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = true;
        }

        public void Show()
        {
            if (selectionRing != null) selectionRing.SetHidden(false);
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = false;
        }

        public void SetSelectionFeedbackVisible(bool visible)
        {
            if (selectionRing != null) selectionRing.SetHidden(!visible);
        }

        public float LayoutDiameter(float scale)
        {
            var mesh = manipulator.SharedMesh;
            float diameter = mesh != null ? mesh.bounds.size.magnitude * .001f : .2f;
            if (label != null && label.havePropertiesChanged) label.ForceMeshUpdate(true);
            // Include the HoloLens title area above the anatomy in radial spacing.
            return Mathf.Max(diameter + .11f, label != null ? label.textBounds.size.magnitude * .001f : .4f) * Mathf.Abs(scale);
        }

        public void RefreshTitle(Vector3 headPosition)
        {
            if (label == null || manipulator.SharedMesh == null) return;
            var mesh = manipulator.SharedMesh;
            var frame = Column.BrainMesh.transform;
            float scale = Mathf.Abs(frame.lossyScale.x);
            string title = $"{Column.Name} ({scene.Visualization.Name})";
            if (label.text != title) label.text = title;
            label.transform.position = frame.TransformPoint(mesh.bounds.center) + Vector3.up * (Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z) * .5f + 10) * scale;
            Vector3 direction = label.transform.position - headPosition;
            if (direction.sqrMagnitude > .000001f) label.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public Bounds WorldLayoutBounds(Vector3? headPosition = null, Vector3 translation = default)
        {
            var mesh = manipulator.SharedMesh;
            var frame = Column != null && Column.BrainMesh != null ? Column.BrainMesh.transform : transform;
            var bounds = new Bounds(frame.position + translation, Vector3.zero);
            if (mesh != null)
                for (int i = 0; i < 8; i++)
                    bounds.Encapsulate(frame.TransformPoint(mesh.bounds.center + Vector3.Scale(mesh.bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))) + translation);
            if (label != null)
            {
                if (label.havePropertiesChanged) label.ForceMeshUpdate(true);
                Vector3 position = label.transform.position + translation;
                Vector3 direction = position - (headPosition ?? position - label.transform.forward);
                Quaternion rotation = direction.sqrMagnitude > .000001f ? Quaternion.LookRotation(direction, Vector3.up) : label.transform.rotation;
                var matrix = Matrix4x4.TRS(position, rotation, label.transform.lossyScale);
                var textBounds = label.textBounds;
                for (int i = 0; i < 8; i++)
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(textBounds.center + Vector3.Scale(textBounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            }

            return bounds;
        }

        private void LateUpdate()
        {
            if (Camera.main != null) RefreshTitle(Camera.main.transform.position);
            if (scene == null || scene.IsClosing) return;
            var selected = scene.ROIManager.SelectedROI;
            int count = selected == null ? 0 : selected.Spheres.Count;
            while (spheres.Count < count) spheres.Add(Instantiate(roiPrefab, scientificFrame, false));
            for (int i = 0; i < spheres.Count; i++)
            {
                var renderer = spheres[i];
                if (i >= count)
                {
                    renderer.gameObject.SetActive(false);
                    continue;
                }

                var source = selected.Spheres[i];
                renderer.gameObject.SetActive(source.gameObject.activeInHierarchy && scene.ROIManager.ROICreationMode);
                renderer.transform.localPosition = source.transform.localPosition;
                renderer.transform.localScale = source.transform.localScale;
                renderer.sharedMaterial = source.GetComponent<Renderer>().sharedMaterial;
                renderer.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            }
        }
    }
}
