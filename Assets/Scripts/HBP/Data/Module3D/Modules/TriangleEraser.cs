using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Object3D;
using UnityEngine;
using UnityEngine.Events;
using AtlasRegionKey = System.ValueTuple<HBP.Data.Module3D.TriangleEraser.AtlasSource, string, int>;

namespace HBP.Data.Module3D
{
    public enum TriangleSelectionMode
    {
        Triangle,
        Angle,
        Activity,
        Atlas
    }

    public sealed partial class TriangleEraser : MonoBehaviour
    {
        [SerializeField] private Base3DScene m_Scene;
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        private const int HistoryLimit = 20;
        private readonly List<Edit> m_History = new(HistoryLimit);
        private BitArray m_Selection = new(0);
        private int[] m_Visibility = Array.Empty<int>();
        private Surface m_DisplaySurface;
        private long m_DisplayVersion;
        private TriangleSelectionGeometry m_Geometry;
        private Vector3[] m_AnatomicalVertices;
        private Surface m_AnatomicalSurface;
        private long m_AnatomicalVersion;
        private bool m_IsEnabled;
        private int m_EditRevision;

        public UnityEvent OnVisibilityMaskChanged { get; } = new();
        public bool MeshHasInvisibleTriangles { get; private set; }
        public int SelectedCount { get; private set; }
        public int SelectedErasedCount { get; private set; }
        public bool CanInvertSelection => m_Visibility.Length > 0 && !IsComputingSelection;
        public bool CanCancelLastAction => m_History.Count != 0;
        public TriangleSelectionMode Mode { get; set; }
        public bool RemoveFromSelection { get; set; }
        public int Degrees { get; set; } = 30;
        public float ActivityTolerance { get; set; } = 20;
        public float DiFuMoThreshold { get; set; } = 0.1f;
        public bool IsClickAvailable => IsEnabled && Mode != TriangleSelectionMode.Atlas;
        public int[] VisibilityMask => (int[])m_Visibility.Clone();

        public bool IsEnabled
        {
            get => m_IsEnabled;
            set
            {
                if (m_IsEnabled == value) return;
                m_IsEnabled = value;
                m_EditRevision++;
                UpdatePreview();
            }
        }

        private sealed class FaceChanges
        {
            private readonly int[] m_Sparse;
            private readonly BitArray m_Dense;

            public FaceChanges(List<int> faces, int triangleCount)
            {
                if (faces.Count <= Math.Max(1, triangleCount / 32)) m_Sparse = faces.ToArray();
                else
                {
                    m_Dense = new BitArray(triangleCount);
                    foreach (int face in faces) m_Dense[face] = true;
                }
            }

            public IEnumerable<int> Faces()
            {
                if (m_Sparse != null)
                {
                    foreach (int face in m_Sparse) yield return face;
                    yield break;
                }

                for (int face = 0; face < m_Dense.Length; face++)
                    if (m_Dense[face])
                        yield return face;
            }
        }

        private readonly struct Edit
        {
            public readonly FaceChanges Selection, Visibility;
            public readonly Dictionary<AtlasRegionKey, FaceChanges> PreviousRegions;

            public Edit(FaceChanges selection, FaceChanges visibility, Dictionary<AtlasRegionKey, FaceChanges> previousRegions)
            {
                Selection = selection;
                Visibility = visibility;
                PreviousRegions = previousRegions;
            }
        }

        internal TriangleSelectionGeometry Geometry
        {
            get
            {
                Surface surface = m_Scene.MeshManager.BrainSurface;
                if (m_Geometry == null || !ReferenceEquals(surface, m_DisplaySurface) || surface.GeometryVersion != m_DisplayVersion)
                {
                    m_DisplaySurface = surface;
                    m_DisplayVersion = surface.GeometryVersion;
                    m_Geometry = new TriangleSelectionGeometry(surface.CopyVertexPositions(), surface.CopyTriangleIndices());
                }

                return m_Geometry;
            }
        }

        internal bool TryRaycast(Column3D column, Ray ray, ref float distance, out int face, out Vector3 anatomicalPoint, bool includeErased = false)
        {
            anatomicalPoint = default;
            face = -1;
            if (m_Scene.IsClosing || m_Scene.IsSurfaceRepresentationTransitioning || m_Scene.SceneInformation.GeometryNeedsUpdate || m_Scene.MeshManager.BrainSurface == null || !column.BrainMesh || !column.BrainMesh.activeInHierarchy || !column.BrainMesh.TryGetComponent(out Renderer renderer) || !renderer.enabled || renderer.forceRenderingOff)
                return false;
            var matrix = column.BrainMesh.transform.worldToLocalMatrix;
            TriangleSelectionGeometry geometry = Geometry;
            int[] visibility = !includeErased && m_Visibility.Length == geometry.Count ? m_Visibility : null;
            if (!geometry.Raycast(matrix.MultiplyPoint3x4(ray.origin), matrix.MultiplyVector(ray.direction), visibility, m_Scene.MeshManager.CanClipBrainSurface ? m_Scene.Cuts : null, m_Scene.StrongCuts, ref distance, out face, out Vector3 weights))
                return false;

            Surface reference = m_Scene.MeshManager.ReferenceSurface;
            if (ReferenceEquals(reference, m_DisplaySurface))
                anatomicalPoint = matrix.MultiplyPoint3x4(ray.GetPoint(distance));
            else
            {
                if (!ReferenceEquals(reference, m_AnatomicalSurface) || m_AnatomicalVersion != reference.GeometryVersion)
                {
                    m_AnatomicalSurface = reference;
                    m_AnatomicalVersion = reference.GeometryVersion;
                    m_AnatomicalVertices = reference.CopyVertexPositions();
                }

                if (!AtlasManager.TryInterpolateAnatomicalPoint(m_AnatomicalVertices, geometry.Triangles, face, weights, out anatomicalPoint))
                    return false;
            }

            return true;
        }

        public void ResetForTopologyChange()
        {
            Surface surface = m_Scene.MeshManager.BrainSurface;
            m_EditRevision++;
            m_Visibility = Enumerable.Repeat(1, surface.NumberOfTriangles).ToArray();
            m_Selection = new BitArray(m_Visibility.Length);
            SelectedCount = 0;
            SelectedErasedCount = 0;
            ClearAtlasRegions();
            m_History.Clear();
            m_Geometry = null;
            m_AnatomicalSurface = null;
            m_AnatomicalVertices = null;
            PublishVisibility(geometryChanged: true);
            UpdatePreview();
        }

        public void ResetForRepresentationChange()
        {
            m_Geometry = null;
            UpdatePreview();
        }

        internal void ApplyVisibilityTo(Mesh mesh, Surface surface)
        {
            int count = surface.NumberOfTriangles;
            bool filtered = MeshHasInvisibleTriangles && m_Visibility.Length == count;
            if (!filtered && mesh.GetIndexCount(0) == count * 3) return;

            int[] original = m_Geometry != null && ReferenceEquals(surface, m_DisplaySurface) && surface.GeometryVersion == m_DisplayVersion ? m_Geometry.Triangles : surface.CopyTriangleIndices();
            if (!filtered)
            {
                mesh.triangles = original;
                return;
            }

            var triangles = new List<int>(original.Length);
            for (int face = 0; face < count; face++)
            {
                if (m_Visibility[face] == 0) continue;
                int index = face * 3;
                triangles.Add(original[index]);
                triangles.Add(original[index + 1]);
                triangles.Add(original[index + 2]);
            }

            mesh.SetTriangles(triangles, 0);
        }

        public void SelectTriangle(Column3D column, int face)
        {
            if (IsComputingSelection || (uint)face >= (uint)m_Visibility.Length) return;
            switch (Mode)
            {
                case TriangleSelectionMode.Triangle:
                    Retouch(new[] { face }, RemoveFromSelection);
                    break;
                case TriangleSelectionMode.Angle:
                    Vector3 normal = Geometry.Normal(face);
                    float limit = Mathf.Cos(Mathf.Clamp(Degrees, 0, 180) * Mathf.Deg2Rad);
                    Retouch(Propagate(face, candidate => candidate == face || Vector3.Dot(normal, Geometry.Normal(candidate)) >= limit - 1e-6f));
                    break;
                case TriangleSelectionMode.Activity:
                    SelectActivity(column, face);
                    break;
            }
        }

        private IEnumerable<int> Propagate(int seed, Func<int, bool> accept)
        {
            var visited = new BitArray(m_Visibility.Length);
            var pending = new Queue<int>();
            if (!accept(seed)) yield break;
            visited[seed] = true;
            pending.Enqueue(seed);
            while (pending.Count != 0)
            {
                int face = pending.Dequeue();
                yield return face;
                foreach (int next in Geometry.Neighbors(face))
                {
                    if (visited[next] || m_Visibility[next] != m_Visibility[seed]) continue;
                    visited[next] = true;
                    if (accept(next)) pending.Enqueue(next);
                }
            }
        }

        private void Retouch(IEnumerable<int> faces, bool remove = false, Dictionary<AtlasRegionKey, FaceChanges> previousRegions = null)
        {
            bool select = !remove;
            var changed = new List<int>();
            foreach (int face in faces)
            {
                if (m_Selection[face] == select) continue;
                ToggleSelection(face);
                changed.Add(face);
            }

            Record(changed, null, previousRegions);
        }

        public void ExpandSelection()
        {
            if (SelectedCount == 0) return;
            var changes = new List<int>();
            foreach (int face in SelectedFaces())
            foreach (int neighbor in Geometry.Neighbors(face))
                if (m_Visibility[neighbor] == m_Visibility[face] && !m_Selection[neighbor])
                    changes.Add(neighbor);

            var changed = new List<int>();
            foreach (int face in changes)
            {
                if (m_Selection[face]) continue;
                ToggleSelection(face);
                changed.Add(face);
            }

            Record(changed, null);
        }

        public void ClearSelection() => ChangeVisibility(null);

        public void ApplySelection(bool keep)
        {
            if (SelectedCount == 0) return;
            if (keep)
                ChangeVisibility(face => m_Selection[face] ? 1 : 0);
            else
                ChangeVisibility(face => m_Selection[face] ? 0 : m_Visibility[face]);
        }

        public void RestoreSelection()
        {
            if (SelectedErasedCount > 0)
                ChangeVisibility(face => m_Selection[face] ? 1 : m_Visibility[face]);
        }

        public void InvertSelection()
        {
            if (!CanInvertSelection) return;
            var changed = new List<int>(m_Visibility.Length);
            for (int face = 0; face < m_Visibility.Length; face++)
            {
                ToggleSelection(face);
                changed.Add(face);
            }

            Record(changed, null, ClearAtlasRegions());
        }

        public void ResetEraser() => ChangeVisibility(_ => 1);

        public void ImportVisibility(TextReader reader) => ImportVisibility(ReadTrimask(reader));

        internal static int[] ReadTrimask(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            string line = reader.ReadLine() ?? string.Empty;
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Select(token => int.Parse(token, CultureInfo.InvariantCulture)).ToArray();
        }

        public void ImportVisibility(int[] mask)
        {
            ValidateMask(mask);
            ChangeVisibility(face => mask[face]);
        }

        public void ApplyExternalVisibility(int[] mask)
        {
            ValidateMask(mask);
            if (m_Visibility.SequenceEqual(mask)) return;
            m_EditRevision++;
            m_Visibility = (int[])mask.Clone();
            m_History.Clear();
            m_Selection.SetAll(false);
            SelectedCount = 0;
            SelectedErasedCount = 0;
            ClearAtlasRegions();
            PublishVisibility();
            UpdatePreview();
        }

        private void ValidateMask(int[] mask)
        {
            if (mask == null || mask.Length != m_Visibility.Length || Array.Exists(mask, value => value != 0 && value != 1))
                throw new ArgumentException("The visibility mask does not match the complete surface.", nameof(mask));
        }

        private void ChangeVisibility(Func<int, int> getVisibility)
        {
            var visibility = new List<int>();
            if (getVisibility != null)
            {
                for (int face = 0; face < m_Visibility.Length; face++)
                {
                    if (m_Visibility[face] == getVisibility(face)) continue;
                    m_Visibility[face] ^= 1;
                    visibility.Add(face);
                }
            }

            var selection = SelectedFaces().ToList();
            m_Selection.SetAll(false);
            SelectedCount = 0;
            SelectedErasedCount = 0;
            Record(selection, visibility, ClearAtlasRegions());
        }

        private IEnumerable<int> SelectedFaces()
        {
            if (SelectedCount == 0) yield break;
            for (int face = 0; face < m_Selection.Length; face++)
                if (m_Selection[face])
                    yield return face;
        }

        private void ToggleSelection(int face)
        {
            m_Selection[face] = !m_Selection[face];
            int delta = m_Selection[face] ? 1 : -1;
            SelectedCount += delta;
            if (m_Visibility[face] == 0) SelectedErasedCount += delta;
        }

        private void Record(List<int> selection, List<int> visibility, Dictionary<AtlasRegionKey, FaceChanges> previousRegions = null)
        {
            bool selectionChanged = selection.Count != 0;
            bool visibilityChanged = visibility is { Count: > 0 };
            if (!selectionChanged && !visibilityChanged && previousRegions == null) return;
            m_EditRevision++;
            if (m_History.Count == HistoryLimit) m_History.RemoveAt(0);
            m_History.Add(new Edit(selectionChanged ? new FaceChanges(selection, m_Visibility.Length) : null, visibilityChanged ? new FaceChanges(visibility, m_Visibility.Length) : null, previousRegions));
            if (visibilityChanged) PublishVisibility();
            UpdatePreview();
        }

        public void CancelLastAction()
        {
            if (m_History.Count == 0) return;
            m_EditRevision++;
            Edit edit = m_History[^1];
            m_History.RemoveAt(m_History.Count - 1);
            if (edit.Visibility != null)
            {
                foreach (int face in edit.Visibility.Faces()) m_Visibility[face] ^= 1;
                PublishVisibility();
            }

            if (edit.Selection != null)
                foreach (int face in edit.Selection.Faces())
                    ToggleSelection(face);
            if (edit.PreviousRegions != null) m_AtlasRegions = edit.PreviousRegions;

            UpdatePreview();
        }

        private void PublishVisibility(bool geometryChanged = false)
        {
            MeshHasInvisibleTriangles = Array.IndexOf(m_Visibility, 0) >= 0;
            m_ErasedIndices = null;
            m_Scene.MeshManager.UpdateMeshesFromDLL();
            m_Scene.InvalidateSurfaceMesh();
            m_Scene.SceneInformation.CollidersNeedUpdate = true;
            if (!geometryChanged)
            {
                m_Scene.FMRIManager.UpdateSurfaceFMRIColors();
                m_Scene.AtlasManager.UpdateAtlasColors();
            }

            OnVisibilityMaskChanged.Invoke();
        }
    }
}
