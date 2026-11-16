using UnityEngine;
using HBP.Core.Object3D;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Class responsible for the display of atlases on the brain
    /// </summary>
    /// <remarks>
    /// This class stores information about how to display Mars and Jubrain Atlases (such as the color of each vertex, the transparency of the colors etc.)
    /// </remarks>
    public class AtlasManager : MonoBehaviour
    {
        #region Properties

        /// <summary>
        /// Parent scene of the manager
        /// </summary>
        [SerializeField] private Base3DScene m_Scene;

        /// <summary>
        /// Component containing references to GameObjects of the 3D scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        private Core.DLL.BrainAtlas m_CachedAtlas;
        private int m_CachedLabel = -1;
        private AtlasInfo m_CachedRegion;
        private Mesh m_InflatedHoverMesh;
        private int[] m_InflatedHoverTriangles;
        private Vector3[] m_AnatomicalHoverVertices;

        private bool m_DisplayMarsAtlas;

        /// <summary>
        /// Do we display the Mars Atlas on the brain mesh ?
        /// </summary>
        public bool DisplayMarsAtlas
        {
            get { return m_DisplayMarsAtlas; }
            set
            {
                if (value) m_Scene.EnsureAtlasCanBeUsed("mars");
                var previous = SelectedAtlas;
                m_DisplayMarsAtlas = value && Object3DManager.MarsAtlas.Loaded && m_Scene.MeshManager.SelectedMesh.SupportsMarsAtlas;
                ResetHoverOnAtlasChange(previous);
                m_Scene.BrainMaterials.SetDisplayAtlas(DisplayAtlas);
                UpdateAtlasIndices();
                m_Scene.OnSharedStateChanged.Invoke();
            }
        }

        private bool m_DisplayJuBrainAtlas;

        /// <summary>
        /// Do we display the JuBrain Atlas on the brain and on the cuts ?
        /// </summary>
        public bool DisplayJuBrainAtlas
        {
            get { return m_DisplayJuBrainAtlas; }
            set
            {
                if (value) m_Scene.EnsureAtlasCanBeUsed("jubrain");
                var previous = SelectedAtlas;
                m_DisplayJuBrainAtlas = value && Object3DManager.JuBrain.Loaded && m_Scene.MeshManager.SelectedMesh.SupportsMNIResources;
                ResetHoverOnAtlasChange(previous);
                m_Scene.BrainMaterials.SetDisplayAtlas(DisplayAtlas);
                UpdateAtlasIndices();
                m_Scene.OnSharedStateChanged.Invoke();
            }
        }

        /// <summary>
        /// Do we display an atlas ?
        /// </summary>
        public bool DisplayAtlas
        {
            get { return m_DisplayJuBrainAtlas || m_DisplayMarsAtlas; }
        }

        /// <summary>
        /// Currently selected atlas
        /// </summary>
        public Core.DLL.BrainAtlas SelectedAtlas
        {
            get
            {
                if (m_DisplayMarsAtlas)
                {
                    return Object3DManager.MarsAtlas;
                }
                else if (m_DisplayJuBrainAtlas)
                {
                    return Object3DManager.JuBrain;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// JuBrain Atlas indices for the mesh
        /// </summary>
        private int[] m_JuBrainAtlasIndices;

        /// <summary>
        /// MarsAtlas indices for the mesh
        /// </summary>
        private int[] m_MarsAtlasIndices;

        /// <summary>
        /// Transparency of the atlas on the brain and on the cuts
        /// </summary>
        private float m_AtlasAlpha = 1.0f;

        public float AtlasAlpha
        {
            get => m_AtlasAlpha;
            set
            {
                if (m_AtlasAlpha == value) return;
                m_AtlasAlpha = value;
                m_Scene.OnSharedStateChanged.Invoke();
            }
        }

        private int m_HoveredArea = -1;

        public bool CanHover => !m_Scene.IsClosing && m_Scene.MeshManager.Meshes.Count > 0 &&
            m_Scene.MeshManager.SelectedMesh.Type == HBP.Core.Enums.MeshType.MNI &&
            SelectedAtlas is { Loaded: true } && !m_Scene.FMRIManager.DisplayFMRI;

        public bool UsesInflatedHoverGeometry => CanHover &&
            m_Scene.MeshManager.SelectedMesh.Representation == SurfaceRepresentation.Inflated;

        /// <summary>
        /// Area of the atlas hovered locally.
        /// </summary>
        public int HoveredArea
        {
            get { return m_HoveredArea; }
            set
            {
                if (m_HoveredArea != value)
                {
                    m_HoveredArea = value;
                    if (m_Scene.IsClosing) return;
                    ApplySurfaceColors();
                    m_Scene.SceneInformation.InvalidateAtlasHoverCutTextures(m_Scene.DesktopPresentation != null);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the indices of all the JuBrain Atlas areas for all vertices
        /// </summary>
        public void UpdateAtlasIndices()
        {
            m_JuBrainAtlasIndices = null;
            m_MarsAtlasIndices = null;
            if (m_DisplayJuBrainAtlas && m_Scene.MeshManager.SelectedMesh.SupportsMNIResources && Object3DManager.JuBrain.Loaded)
            {
                m_JuBrainAtlasIndices = Object3DManager.JuBrain.GetSurfaceAreaLabels(m_Scene.MeshManager.ReferenceSurface);
            }

            if (m_DisplayMarsAtlas && m_Scene.MeshManager.SelectedMesh.SupportsMarsAtlas && Object3DManager.MarsAtlas.Loaded)
            {
                m_MarsAtlasIndices = Object3DManager.MarsAtlas.GetSurfaceAreaLabels(m_Scene.MeshManager.ReferenceSurface);
            }

            UpdateAtlasColors();
        }

        /// <summary>
        /// Update all colors for the atlas for all vertices
        /// </summary>
        public void UpdateAtlasColors()
        {
            ApplySurfaceColors();
            m_Scene.SceneInformation.BaseCutTexturesNeedUpdate = true;
        }

        private void ApplySurfaceColors()
        {
            if (SelectedAtlas is { Loaded: true } atlas && !m_Scene.FMRIManager.DisplayFMRI)
            {
                int[] indices = atlas is Core.DLL.MarsAtlas ? m_MarsAtlasIndices : m_JuBrainAtlasIndices;
                if (indices != null)
                {
                    Color[] colors = atlas.ConvertIndicesToColors(indices, HoveredArea);
                    m_DisplayedObjects.Brain.GetComponent<MeshFilter>().sharedMesh.colors = colors;
                    foreach (Column3D column in m_Scene.Columns)
                        column.BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors = colors;
                }
            }
        }

        /// <summary>
        /// Display the information about the atlas area under the mouse
        /// </summary>
        /// <param name="canDisplay">Can we display the atlas information ? (Did we hit a mesh or a cut with the raycast ?)</param>
        /// <param name="hitPoint">Point on the mesh or on the cut where the atlas area has to be considered</param>
        public AtlasInfo GetAtlasInformation(bool canDisplay, Vector3 hitPoint, Vector2 screenPosition = default)
        {
            if (canDisplay && TryGetHoverRegion(hitPoint, out int label, out AtlasInfo region))
            {
                HoveredArea = label;
                return new AtlasInfo(true, screenPosition, region.Type, region.Information1, region.Information2, region.Information3, region.Information4, region.Information5);
            }

            HoveredArea = -1;
            return new AtlasInfo(false, screenPosition);
        }

        public bool TryGetHoverRegion(Vector3 anatomicalPoint, out int label, out AtlasInfo region)
        {
            label = -1;
            region = null;
            if (!CanHover || !float.IsFinite(anatomicalPoint.x) || !float.IsFinite(anatomicalPoint.y) || !float.IsFinite(anatomicalPoint.z))
                return false;

            var atlas = SelectedAtlas;
            int candidate = atlas.GetClosestAreaIndex(anatomicalPoint, 1);
            if (candidate <= 0) return false;
            if (atlas != m_CachedAtlas || candidate != m_CachedLabel)
            {
                m_CachedAtlas = atlas;
                m_CachedLabel = candidate;
                m_CachedRegion = DescribeRegion(atlas is Core.DLL.MarsAtlas, candidate, atlas.GetInformation(candidate));
            }

            region = m_CachedRegion;
            if (region == null) return false;
            label = candidate;
            return true;
        }

        internal static AtlasInfo DescribeRegion(bool isMarsAtlas, int label, string[] information)
        {
            if (label <= 0 || information.Length != (isMarsAtlas ? 5 : 1) || string.IsNullOrWhiteSpace(information[0]))
                return null;

            string name = information[0] + " - ID:" + label;
            return isMarsAtlas
                ? new AtlasInfo(true, default, AtlasInfo.AtlasType.MarsAtlas, name, information[1], information[2], information[3], information[4])
                : new AtlasInfo(true, default, AtlasInfo.AtlasType.JuBrainAtlas, name);
        }

        public bool TryRaycastInflatedSurface(Column3D column, Ray ray, float distance, out RaycastHit hit)
        {
            hit = default;
            var collider = column.AtlasHoverCollider;
            if (!collider || !UsesInflatedHoverGeometry || !m_Scene.IsBrainRaycastGeometryCurrent) return false;
            if (!m_InflatedHoverMesh)
            {
                var displayed = m_DisplayedObjects.Brain.GetComponent<MeshFilter>().sharedMesh;
                m_InflatedHoverTriangles = displayed.triangles;
                m_AnatomicalHoverVertices = m_Scene.MeshManager.ReferenceSurface.CopyVertexPositions();
                // Keep hover color uploads off the mesh used by physics.
                m_InflatedHoverMesh = new Mesh
                {
                    name = "Inflated atlas hover",
                    indexFormat = displayed.indexFormat,
                    vertices = displayed.vertices,
                    triangles = m_InflatedHoverTriangles
                };
            }

            if (m_InflatedHoverTriangles.Length == 0) return false;
            if (collider.sharedMesh != m_InflatedHoverMesh) collider.sharedMesh = m_InflatedHoverMesh;
            return collider.Raycast(ray, out hit, distance);
        }

        public bool TryGetAnatomicalHoverPoint(Column3D column, RaycastHit hit, out Vector3 point)
        {
            point = default;
            if (!hit.collider) return false;
            if (hit.collider == column.AtlasHoverCollider)
                return TryInterpolateAnatomicalPoint(m_AnatomicalHoverVertices, m_InflatedHoverTriangles, hit.triangleIndex, hit.barycentricCoordinate, out point);

            point = column.BrainMesh.transform.InverseTransformPoint(hit.point);
            return true;
        }

        internal static bool TryInterpolateAnatomicalPoint(Vector3[] vertices, int[] triangles, int triangle, Vector3 weights, out Vector3 point)
        {
            point = default;
            if (triangles == null || vertices == null || (uint)triangle >= (uint)(triangles.Length / 3))
                return false;
            int index = triangle * 3;
            point = vertices[triangles[index]] * weights.x + vertices[triangles[index + 1]] * weights.y + vertices[triangles[index + 2]] * weights.z;
            return true;
        }

        private void ResetHoverOnAtlasChange(Core.DLL.BrainAtlas previous)
        {
            if (previous == SelectedAtlas) return;
            m_CachedAtlas = null;
            m_CachedRegion = null;
            m_HoveredArea = -1;
            if (!DisplayAtlas) InvalidateHoverGeometry();
        }

        internal void InvalidateHoverGeometry()
        {
            m_HoveredArea = -1;
            if (!m_InflatedHoverMesh) return;
            foreach (var column in m_Scene.Columns)
                if (column && column.AtlasHoverCollider && column.AtlasHoverCollider.sharedMesh == m_InflatedHoverMesh)
                    column.AtlasHoverCollider.sharedMesh = null;
            Destroy(m_InflatedHoverMesh);
            m_InflatedHoverMesh = null;
            m_InflatedHoverTriangles = null;
            m_AnatomicalHoverVertices = null;
        }

        private void OnDestroy()
        {
            if (m_InflatedHoverMesh) Destroy(m_InflatedHoverMesh);
        }

        public void ColorCuts(Column3D column)
        {
            column.CutTextures.ColorCutsTexturesWithBrainAtlas(SelectedAtlas, AtlasAlpha, HoveredArea);
        }

        #endregion
    }
}
