using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.Data.Module3D
{
    public sealed partial class TriangleEraser
    {
        private Material m_PreviewMaterial;
        private Material[] m_PreviewMaterials;
        private MaterialPropertyBlock m_PreviewProperties;
        private Color m_ErasedTint = new Color(0f, 0f, 1f, 98f / 255);
        private Mesh m_PreviewMesh;
        private TriangleSelectionGeometry m_PreviewGeometry;
        private List<int> m_ErasedIndices;
        private readonly List<int> m_PreviewFaces = new();
        private readonly List<int> m_PreviewLines = new();
        private readonly Dictionary<(int, int), int> m_PreviewEdges = new();
        private readonly Vector4[] m_CutPoints = new Vector4[20];
        private readonly Vector4[] m_CutNormals = new Vector4[20];
        private bool HasPreview => IsEnabled && (SelectedCount != 0 || MeshHasInvisibleTriangles);

        private void UpdatePreview()
        {
            if (!HasPreview)
            {
                if (m_DisplayedObjects && m_DisplayedObjects.InvisibleBrain)
                    m_DisplayedObjects.InvisibleBrain.SetActive(false);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
                return;
            }

            if (!m_PreviewMaterial)
            {
                var shader = Resources.Load<Shader>("Shaders/TriangleSelectionPreview");
                if (!shader) return;
                m_PreviewMaterial = new Material(shader);
                m_PreviewMaterials = new[] { m_PreviewMaterial, m_PreviewMaterial, m_PreviewMaterial };
                m_PreviewMesh = new Mesh { name = "Triangle erasing preview", indexFormat = IndexFormat.UInt32 };
                m_PreviewMesh.MarkDynamic();
            }

            var mesh = m_PreviewMesh;
            var geometry = Geometry;
            if (geometry != m_PreviewGeometry)
            {
                m_PreviewGeometry = geometry;
                m_ErasedIndices = null;
                mesh.Clear();
                mesh.vertices = geometry.Vertices;
                mesh.subMeshCount = 3;
                mesh.RecalculateBounds();
            }

            if (m_ErasedIndices == null)
            {
                m_ErasedIndices = new List<int>();
                for (int face = 0; face < m_Visibility.Length; face++)
                {
                    if (m_Visibility[face] != 0) continue;
                    int index = face * 3;
                    m_ErasedIndices.Add(geometry.Triangles[index]);
                    m_ErasedIndices.Add(geometry.Triangles[index + 1]);
                    m_ErasedIndices.Add(geometry.Triangles[index + 2]);
                }

                mesh.SetIndices(m_ErasedIndices, MeshTopology.Triangles, 0, false);
            }

            m_PreviewFaces.Clear();
            m_PreviewEdges.Clear();
            m_PreviewLines.Clear();
            foreach (int face in SelectedFaces())
            {
                for (int corner = 0; corner < 3; corner++)
                {
                    int index = face * 3;
                    int a = geometry.Triangles[index + corner], b = geometry.Triangles[index + (corner + 1) % 3];
                    m_PreviewFaces.Add(a);
                    if (a == b) continue;
                    var edge = a < b ? (a, b) : (b, a);
                    m_PreviewEdges.TryGetValue(edge, out int uses);
                    m_PreviewEdges[edge] = uses + 1;
                }
            }

            foreach (var edge in m_PreviewEdges)
            {
                if (edge.Value != 1) continue;
                m_PreviewLines.Add(edge.Key.Item1);
                m_PreviewLines.Add(edge.Key.Item2);
            }

            mesh.SetIndices(m_PreviewFaces, MeshTopology.Triangles, 1, false);
            mesh.SetIndices(m_PreviewLines, MeshTopology.Lines, 2, false);
            if (m_Scene.DesktopPresentation)
            {
                if (!m_DisplayedObjects.InvisibleBrain)
                {
                    m_DisplayedObjects.InstantiateInvisibleMesh(false);
                    var material = m_DisplayedObjects.InvisibleBrain.GetComponent<MeshRenderer>().sharedMaterial;
                    if (material && material.HasProperty("_BaseColor")) m_ErasedTint = material.GetColor("_BaseColor");
                }

                m_DisplayedObjects.InvisibleBrain.SetActive(true);
                UpdateDesktopPreview();
            }

            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        private void LateUpdate()
        {
            FlushTriangleStrokePreview();
            if (m_DisplayedObjects && m_DisplayedObjects.InvisibleBrain && m_DisplayedObjects.InvisibleBrain.activeSelf)
                UpdateDesktopPreview();
        }

        private void UpdateDesktopPreview()
        {
            var overlay = m_DisplayedObjects.InvisibleBrain;
            PresentPreview(overlay.GetComponent<MeshFilter>(), overlay.GetComponent<MeshRenderer>(), true);
        }

        public void PresentPreview(MeshFilter filter, MeshRenderer renderer, bool presented)
        {
            renderer.enabled = presented && HasPreview && m_PreviewGeometry != null && !m_Scene.IsSurfaceRepresentationTransitioning && !m_Scene.SceneInformation.GeometryNeedsUpdate && m_Scene.IsVisible;
            if (!renderer.enabled) return;

            if (filter.sharedMesh != m_PreviewMesh)
            {
                filter.sharedMesh = m_PreviewMesh;
                renderer.sharedMaterials = m_PreviewMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            int count = m_Scene.MeshManager.CanClipBrainSurface ? Mathf.Min(m_CutPoints.Length, m_Scene.Cuts.Count) : 0;
            for (int i = 0; i < count; i++)
            {
                m_CutPoints[i] = m_Scene.Cuts[i].Point;
                m_CutNormals[i] = m_Scene.Cuts[i].Normal;
            }

            m_PreviewProperties ??= new MaterialPropertyBlock();
            m_PreviewProperties.SetInt("_CutCount", count);
            m_PreviewProperties.SetInt("_StrongCuts", m_Scene.StrongCuts ? 1 : 0);
            m_PreviewProperties.SetVectorArray("_CutPoints", m_CutPoints);
            m_PreviewProperties.SetVectorArray("_CutNormals", m_CutNormals);
            m_PreviewProperties.SetColor("_Tint", m_ErasedTint);
            renderer.SetPropertyBlock(m_PreviewProperties, 0);
            m_PreviewProperties.SetColor("_Tint", new Color(0.12f, 0.77f, 0.85f, 0.2f));
            renderer.SetPropertyBlock(m_PreviewProperties, 1);
            m_PreviewProperties.SetColor("_Tint", new Color(0.13f, 0.91f, 0.99f, 0.92f));
            renderer.SetPropertyBlock(m_PreviewProperties, 2);
        }

        private void OnDestroy()
        {
            if (m_PreviewMesh) Destroy(m_PreviewMesh);
            if (m_PreviewMaterial) Destroy(m_PreviewMaterial);
        }
    }
}
