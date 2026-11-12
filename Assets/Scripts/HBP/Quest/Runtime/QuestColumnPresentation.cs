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
        [SerializeField] private QuestTimeline timeline;
        private QuestAnatomyView view;
        private QuestAnatomyView.Presentation presentation;
        private Base3DScene scene;
        public Base3DScene Scene => scene;
        private readonly List<QuestROISphere> spheres = new();
        public IReadOnlyList<QuestROISphere> ROISpheres => spheres;
        public Transform ScientificFrame => scientificFrame;
        public Column3D Column { get; private set; }
        public QuestAnatomyManipulator Manipulator => manipulator;
        internal bool HasProjectedTimeline => scene != null && !scene.IsClosing && Column?.NavigationTimeline is { Length: > 1 } && scene.HasSurfaceActivityProjection;
        internal bool HasTimeline => presentation is { ShowTimeline: true, SurfaceHidden: false } && HasProjectedTimeline;

        internal void BindTimeline(QuestAnatomyView owner, QuestAnatomyView.Presentation state)
        {
            view = owner;
            presentation = state;
            if (timeline != null) timeline.Bind(owner, state, this);
        }

        internal void RefreshTimeline(Camera camera)
        {
            if (timeline == null || view == null) return;
            bool visible = camera != null && view.IsColumnPresented(this) && !presentation.IsTransitioning && HasTimeline && manipulator.SharedMesh != null;
            if (visible) PlaceTimeline(camera.transform.position);
            if (timeline.gameObject.activeSelf != visible) timeline.gameObject.SetActive(visible);
            if (visible) timeline.Refresh(camera);
        }

        private void PlaceTimeline(Vector3 headPosition)
        {
            var mesh = manipulator.SharedMesh;
            var frame = Column.BrainMesh.transform;
            var rect = timeline.Rect;
            var extents = mesh.bounds.extents;
            float scale = Mathf.Abs(frame.lossyScale.x);
            float height = Mathf.Abs(frame.TransformVector(Vector3.right * extents.x).y) + Mathf.Abs(frame.TransformVector(Vector3.up * extents.y).y) + Mathf.Abs(frame.TransformVector(Vector3.forward * extents.z).y);
            float clearance = Mathf.Max(height, Mathf.Max(extents.x, extents.y, extents.z) * scale) + 10 * scale;
            Vector3 position = frame.TransformPoint(mesh.bounds.center) - Vector3.up * (clearance + rect.rect.height * .5f * Mathf.Abs(rect.lossyScale.y));
            Vector3 direction = position - headPosition;
            rect.position = position;
            if (direction.sqrMagnitude > .000001f) rect.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

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
            float size = Mathf.Max(diameter + .11f, label != null ? label.textBounds.size.magnitude * .001f : .4f);
            if (timeline != null && HasTimeline)
            {
                var rect = timeline.Rect;
                size = Mathf.Max(size + rect.rect.height * Mathf.Abs(rect.localScale.y), rect.rect.width * Mathf.Abs(rect.localScale.x));
            }

            return size * Mathf.Abs(scale);
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

            if (timeline != null && mesh != null && HasTimeline)
            {
                var rect = timeline.Rect;
                PlaceTimeline(headPosition ?? rect.position - rect.forward);
                Vector3 position = rect.position + translation;
                Vector3 direction = position - (headPosition ?? position - rect.forward);
                var rotation = direction.sqrMagnitude > .000001f ? Quaternion.LookRotation(direction, Vector3.up) : rect.rotation;
                var matrix = Matrix4x4.TRS(position, rotation, rect.lossyScale);
                var area = rect.rect;
                bounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(area.xMin, area.yMin)));
                bounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(area.xMin, area.yMax)));
                bounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(area.xMax, area.yMin)));
                bounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(area.xMax, area.yMax)));
            }

            return bounds;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null) RefreshTitle(camera.transform.position);
            RefreshTimeline(camera);
            if (scene == null || scene.IsClosing) return;
            var selected = scene.ROIManager.SelectedROI;
            int count = selected == null ? 0 : selected.Spheres.Count;
            while (spheres.Count < count) spheres.Add(Instantiate(roiPrefab, scientificFrame, false).GetComponent<QuestROISphere>());
            for (int i = 0; i < spheres.Count; i++)
            {
                var proxy = spheres[i];
                var renderer = proxy.GetComponent<MeshRenderer>();
                if (i >= count)
                {
                    renderer.gameObject.SetActive(false);
                    continue;
                }

                var source = selected.Spheres[i];
                proxy.Bind(this, selected, source);
                renderer.gameObject.SetActive(source.gameObject.activeInHierarchy && scene.ROIManager.ROICreationMode);
                renderer.transform.localPosition = source.transform.localPosition;
                renderer.transform.localScale = source.transform.localScale;
                renderer.sharedMaterial = source.GetComponent<Renderer>().sharedMaterial;
                renderer.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            }
        }
    }
}
