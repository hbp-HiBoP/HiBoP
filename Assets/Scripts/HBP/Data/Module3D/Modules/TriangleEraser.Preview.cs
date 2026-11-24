using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.Data.Module3D
{
    public sealed partial class TriangleEraser
    {
        private Material m_PreviewMaterial;
        private MaterialPropertyBlock m_PreviewProperties;
        private Color m_ErasedTint = new Color(0f, 0f, 1f, 98f / 255);
        private Mesh m_PreviewMesh;
        private TriangleSelectionGeometry m_PreviewGeometry;
        private List<int> m_ErasedIndices;
        private readonly Vector4[] m_CutPoints = new Vector4[20];
        private readonly Vector4[] m_CutNormals = new Vector4[20];

        private void UpdatePreview()
        {
            if (!IsEnabled || SelectedCount == 0 && !MeshHasInvisibleTriangles || !m_Scene.DesktopPresentation)
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
            }

            if (!m_DisplayedObjects.InvisibleBrain)
                m_DisplayedObjects.InstantiateInvisibleMesh(false);

            var overlay = m_DisplayedObjects.InvisibleBrain;
            var mesh = overlay.GetComponent<MeshFilter>().sharedMesh;
            var renderer = overlay.GetComponent<MeshRenderer>();
            var geometry = Geometry;
            bool updateGeometry = mesh != m_PreviewMesh || geometry != m_PreviewGeometry;
            if (updateGeometry)
            {
                if (mesh != m_PreviewMesh && renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_BaseColor"))
                    m_ErasedTint = renderer.sharedMaterial.GetColor("_BaseColor");
                m_PreviewMesh = mesh;
                m_PreviewGeometry = geometry;
                m_ErasedIndices = null;
                mesh.Clear();
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.vertices = geometry.Vertices;
                mesh.subMeshCount = 3;
                mesh.RecalculateBounds();
                renderer.sharedMaterials = new[] { m_PreviewMaterial, m_PreviewMaterial, m_PreviewMaterial };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
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

            var faces = new List<int>(SelectedCount * 3);
            var edges = new Dictionary<(int, int), int>();
            foreach (int face in SelectedFaces())
            {
                for (int corner = 0; corner < 3; corner++)
                {
                    int index = face * 3;
                    int a = geometry.Triangles[index + corner], b = geometry.Triangles[index + (corner + 1) % 3];
                    faces.Add(a);
                    if (a == b) continue;
                    var edge = a < b ? (a, b) : (b, a);
                    edges.TryGetValue(edge, out int uses);
                    edges[edge] = uses + 1;
                }
            }

            var lines = new List<int>();
            foreach (var edge in edges)
            {
                if (edge.Value != 1) continue;
                lines.Add(edge.Key.Item1);
                lines.Add(edge.Key.Item2);
            }

            mesh.SetIndices(faces, MeshTopology.Triangles, 1, false);
            mesh.SetIndices(lines, MeshTopology.Lines, 2, false);
            overlay.SetActive(true);
            UpdatePreviewProperties();
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        private void LateUpdate()
        {
            if (IsEnabled && m_DisplayedObjects && m_DisplayedObjects.InvisibleBrain && m_DisplayedObjects.InvisibleBrain.activeSelf)
                UpdatePreviewProperties();
        }

        private void UpdatePreviewProperties()
        {
            var renderer = m_DisplayedObjects.InvisibleBrain.GetComponent<MeshRenderer>();
            renderer.enabled = !m_Scene.IsSurfaceRepresentationTransitioning && !m_Scene.SceneInformation.GeometryNeedsUpdate && m_Scene.IsVisible;
            if (!renderer.enabled) return;

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
            if (m_PreviewMaterial) Destroy(m_PreviewMaterial);
        }
    }
}
