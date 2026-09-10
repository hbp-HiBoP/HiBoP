using HBP.Data.Module3D;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Quest
{
    public sealed class QuestColumnPresentation : MonoBehaviour
    {
        [SerializeField] private Transform scientificFrame;
        [SerializeField] private TextMesh label;
        [SerializeField] private QuestAnatomyManipulator manipulator;
        [SerializeField] private MeshRenderer roiPrefab;
        private Base3DScene scene;
        private readonly List<MeshRenderer> spheres = new();
        public Column3D Column { get; private set; }
        public QuestAnatomyManipulator Manipulator => manipulator;

        public void Bind(Base3DScene source, Column3D column)
        {
            scene = source;
            Column = column;
            column.transform.SetParent(scientificFrame, false);
            column.transform.localPosition = Vector3.zero;
            column.transform.localRotation = Quaternion.identity;
            column.transform.localScale = Vector3.one;
            label.text = column.Name;
            manipulator.Bind(column);
        }

        public void Hide()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = true;
        }

        public void Show()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = false;
        }

        private void LateUpdate()
        {
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
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
