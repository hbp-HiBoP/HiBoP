using UnityEngine;
using HBP.Core.Enums;
using HBP.Core.Object3D;

namespace HBP.Module3D
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

        private bool m_DisplayMarsAtlas;
        /// <summary>
        /// Do we display the Mars Atlas on the brain mesh ?
        /// </summary>
        public bool DisplayMarsAtlas
        {
            get
            {
                return m_DisplayMarsAtlas;
            }
            set
            {
                m_DisplayMarsAtlas = value;
                m_Scene.BrainMaterials.SetDisplayAtlas(m_DisplayMarsAtlas);
                if (m_Scene.MeshManager.SelectedMesh.Type == MeshType.MNI)
                {
                    UpdateAtlasIndices();
                    UpdateAtlasColors();
                }
            }
        }

        private bool m_DisplayJuBrainAtlas;
        /// <summary>
        /// Do we display the JuBrain Atlas on the brain and on the cuts ?
        /// </summary>
        public bool DisplayJuBrainAtlas
        {
            get
            {
                return m_DisplayJuBrainAtlas;
            }
            set
            {
                m_DisplayJuBrainAtlas = value;
                m_Scene.BrainMaterials.SetDisplayAtlas(m_DisplayJuBrainAtlas);
                UpdateAtlasIndices();
                UpdateAtlasColors();
            }
        }

        /// <summary>
        /// Do we display an atlas ?
        /// </summary>
        public bool DisplayAtlas
        {
            get
            {
                return m_DisplayJuBrainAtlas || (m_DisplayMarsAtlas && m_Scene.MeshManager.SelectedMesh.Type == MeshType.MNI);
            }
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
        public float AtlasAlpha { get; set; } = 1.0f;

        private int m_HoveredArea = -1;
        /// <summary>
        /// Area of the atlas hovered by the mouse
        /// </summary>
        public int HoveredArea
        {
            get
            {
                return m_HoveredArea;
            }
            set
            {
                if (m_HoveredArea != value)
                {
                    m_HoveredArea = value;
                    UpdateAtlasColors();
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
            m_JuBrainAtlasIndices = Object3DManager.JuBrain.GetSurfaceAreaLabels(m_Scene.MeshManager.BrainSurface);
            m_MarsAtlasIndices = Object3DManager.MarsAtlas.GetSurfaceAreaLabels(m_Scene.MeshManager.BrainSurface);
        }
        /// <summary>
        /// Update all colors for the atlas for all vertices
        /// </summary>
        public void UpdateAtlasColors()
        {
            if (SelectedAtlas != null)
            {
                int[] indices = SelectedAtlas is Core.DLL.MarsAtlas ? m_MarsAtlasIndices : m_JuBrainAtlasIndices;
                Color[] colors = SelectedAtlas.ConvertIndicesToColors(indices, HoveredArea);
                m_DisplayedObjects.Brain.GetComponent<MeshFilter>().mesh.colors = colors;
                foreach (Column3D column in m_Scene.Columns)
                    column.BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors = colors;
            }
            m_Scene.SceneInformation.BaseCutTexturesNeedUpdate = true;
        }
        /// <summary>
        /// Display the information about the atlas area under the mouse
        /// </summary>
        /// <param name="canDisplay">Can we display the atlas information ? (Did we hit a mesh or a cut with the raycast ?)</param>
        /// <param name="hitPoint">Point on the mesh or on the cut where the atlas area has to be considered</param>
        public void DisplayAtlasInformation(bool canDisplay, Vector3 hitPoint)
        {
            if (canDisplay && SelectedAtlas != null)
            {
                HoveredArea = SelectedAtlas.GetClosestAreaIndex(hitPoint);
                string[] information = SelectedAtlas.GetInformation(HoveredArea);
                if (information.Length == 5)
                {
                    HBP3DModule.OnDisplayAtlasInformation.Invoke(new AtlasInfo(true, Input.mousePosition, SelectedAtlas is Core.DLL.MarsAtlas ? AtlasInfo.AtlasType.MarsAtlas : AtlasInfo.AtlasType.JuBrainAtlas, information[0] + "(" + HoveredArea + ")", information[1], information[2], information[3], information[4]));
                }
                else
                {
                    HBP3DModule.OnDisplayAtlasInformation.Invoke(new AtlasInfo(false, Input.mousePosition));
                }
            }
            else
            {
                HoveredArea = -1;
                HBP3DModule.OnDisplayAtlasInformation.Invoke(new AtlasInfo(false, Input.mousePosition));
            }
        }
        public void ColorCuts(Column3D column)
        {
            column.CutTextures.ColorCutsTexturesWithBrainAtlas(SelectedAtlas, AtlasAlpha, HoveredArea);
        }
        #endregion
    }
}