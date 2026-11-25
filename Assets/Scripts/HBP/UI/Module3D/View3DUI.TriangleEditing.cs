using HBP.Data.Module3D;
using HBP.Input;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.UI.Module3D
{
    public partial class View3DUI
    {
        private uint m_StrokeReference;
        private TriangleSelectionMode m_StrokeMode;
        private Vector2? m_PreviousStrokePointer;
        private bool m_DesktopTriangleStroke, m_BrushGuideVisible;
        private Vector3 m_BrushGuidePoint;
        private Mesh m_BrushGuideMesh;
        private Material m_BrushGuideMaterial;

        private bool CanSweepTriangles => m_Scene != null && m_View != null && !IsMinimized && m_Scene.TriangleEraser is { IsClickAvailable: true, IsSweepMode: true } && !m_Scene.ROIManager.ROICreationMode && !m_Scene.IsClosing && !m_Scene.IsSurfaceRepresentationTransitioning && !m_Scene.SceneInformation.GeometryNeedsUpdate;

        private bool IsCurrentStroke => m_DesktopTriangleStroke && CanSweepTriangles && m_Scene.TriangleEraser.HasTriangleStroke && m_Scene.TriangleEraser.EditReferenceVersion == m_StrokeReference && m_Scene.TriangleEraser.Mode == m_StrokeMode && m_View.IsSelected;

        private bool BeginDesktopTriangleStroke(Ray ray, Vector2 pointer)
        {
            if (!CanSweepTriangles || !m_Scene.TryGetDesktopTriangleTarget(ray, m_Column, out int face, out Vector3 point) || !m_Scene.TriangleEraser.BeginTriangleStroke()) return false;

            m_DesktopTriangleStroke = true;
            m_StrokeReference = m_Scene.TriangleEraser.EditReferenceVersion;
            m_StrokeMode = m_Scene.TriangleEraser.Mode;
            m_PreviousStrokePointer = pointer;
            RetouchDesktopStroke(face, point);
            return true;
        }

        private void UpdateTriangleEditing()
        {
            var pointer = DesktopInput.MousePosition;
            if (m_DesktopTriangleStroke)
            {
                if (IsCurrentStroke) SampleDesktopTriangleStroke(pointer);
                if (!IsCurrentStroke || !DesktopInput.IsLeftMouseButtonPressed)
                    EndDesktopTriangleStroke();
            }

            m_BrushGuideVisible = CanSweepTriangles && m_Scene.TriangleEraser.Mode == TriangleSelectionMode.Brush && CursorToRay(pointer, out var ray) && m_Scene.TryGetDesktopTriangleTarget(ray, m_Column, out _, out m_BrushGuidePoint);
        }

        private void SampleDesktopTriangleStroke(Vector2 pointer)
        {
            if (!IsCurrentStroke) return;
            if (!CursorToRay(pointer, out _))
            {
                m_PreviousStrokePointer = null;
                return;
            }

            var start = m_PreviousStrokePointer ?? pointer;
            if (m_PreviousStrokePointer.HasValue && start.Equals(pointer)) return;
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, pointer)));
            for (int sample = 1; sample <= samples; sample++)
            {
                var point = Vector2.Lerp(start, pointer, (float)sample / samples);
                if (CursorToRay(point, out var ray) && m_Scene.TryGetDesktopTriangleTarget(ray, m_Column, out int face, out var center))
                    RetouchDesktopStroke(face, center);
            }

            m_PreviousStrokePointer = pointer;
        }

        private void RetouchDesktopStroke(int face, Vector3 center)
        {
            var eraser = m_Scene.TriangleEraser;
            if (m_StrokeMode == TriangleSelectionMode.Brush)
                eraser.RetouchBrushStroke(m_Column, center, center, displayedUnits: true);
            else eraser.RetouchTriangleStroke(face);
        }

        private void EndDesktopTriangleStroke()
        {
            if (!m_DesktopTriangleStroke) return;
            m_DesktopTriangleStroke = false;
            m_PreviousStrokePointer = null;
            if (m_Scene != null && m_Scene.TriangleEraser.EditReferenceVersion == m_StrokeReference)
                m_Scene.TriangleEraser.EndTriangleStroke();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            EndDesktopTriangleStroke();
            m_BrushGuideVisible = false;
        }

        private void RenderBrushGuide(ScriptableRenderContext context, Camera camera)
        {
            if (!m_BrushGuideVisible || camera != m_View.Camera || camera.targetTexture != m_RenderTextureOwner.Texture || m_Column == null || !m_Column.BrainMesh) return;
            if (!m_BrushGuideMaterial)
            {
                var shader = Resources.Load<Shader>("Shaders/TriangleContactMarker");
                if (!shader) return;
                m_BrushGuideMaterial = new Material(shader);
                m_BrushGuideMaterial.SetColor("_Color", new Color(.13f, .91f, .99f));
                m_BrushGuideMesh = CreateBrushGuideMesh();
            }

            float radius = m_Scene.TriangleEraser.StrokeBrushRadiusMillimeters;
            var matrix = m_Column.BrainMesh.transform.localToWorldMatrix * Matrix4x4.TRS(m_BrushGuidePoint, Quaternion.identity, Vector3.one * radius);
            var parameters = new RenderParams(m_BrushGuideMaterial)
            {
                camera = camera,
                layer = m_Column.BrainMesh.layer,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false
            };
            Graphics.RenderMesh(parameters, m_BrushGuideMesh, 0, matrix);
        }

        private static Mesh CreateBrushGuideMesh()
        {
            const int segments = 48;
            var vertices = new Vector3[3 * segments];
            var indices = new int[vertices.Length * 2];
            for (int axis = 0; axis < 3; axis++)
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = 2f * Mathf.PI * segment / segments;
                Vector3 point = default;
                point[(axis + 1) % 3] = Mathf.Cos(angle);
                point[(axis + 2) % 3] = Mathf.Sin(angle);
                int index = axis * segments + segment;
                vertices[index] = point;
                indices[index * 2] = index;
                indices[index * 2 + 1] = axis * segments + (segment + 1) % segments;
            }

            var mesh = new Mesh { name = "Triangle brush radius", vertices = vertices };
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            return mesh;
        }
    }
}
