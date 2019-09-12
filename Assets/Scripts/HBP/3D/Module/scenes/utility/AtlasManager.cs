using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
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

        /// <summary>
        /// Atlas indices for the splitted meshes
        /// </summary>
        private List<int[]> m_SplitAtlasIndices = new List<int[]>();
        public bool DisplayAtlas { get; set; }
        public float AtlasAlpha { get; set; } = 1.0f;
        public int AtlasSelectedArea { get; set; } = -1;
        public bool DisplayJuBrainAtlas
        {
            get
            {
                return DisplayAtlas;
            }
            set
            {
                DisplayAtlas = value;
                UpdateAtlasColors();
                m_Scene.BrainMaterial.SetInt("_Atlas", DisplayAtlas ? 1 : 0);
                m_Scene.ResetIEEG();
            }
        }
        public int SelectedAtlasArea
        {
            get
            {
                return AtlasSelectedArea;
            }
            set
            {
                if (AtlasSelectedArea != value)
                {
                    AtlasSelectedArea = value;
                    UpdateAtlasColors();
                    m_Scene.ComputeCutTextures();
                }
            }
        }
        private bool m_IsMarsAtlasEnabled;
        /// <summary>
        /// Are Mars Atlas colors displayed ?
        /// </summary>
        public bool IsMarsAtlasEnabled
        {
            get
            {
                return m_IsMarsAtlasEnabled;
            }
            set
            {
                m_IsMarsAtlasEnabled = value;
                m_Scene.BrainMaterial.SetInt("_Atlas", m_IsMarsAtlasEnabled ? 1 : 0);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the indices of all the JuBrain Atlas areas for all vertices
        /// </summary>
        public void UpdateAtlasIndices()
        {
            m_SplitAtlasIndices = new List<int[]>();
            for (int ii = 0; ii < m_Scene.MeshManager.MeshSplitNumber; ++ii)
            {
                m_SplitAtlasIndices.Add(ApplicationState.Module3D.JuBrainAtlas.GetSurfaceAreaLabels(m_Scene.MeshManager.SplittedMeshes[ii]));
            }
        }
        /// <summary>
        /// Update all colors for the atlas for all vertices
        /// </summary>
        public void UpdateAtlasColors()
        {
            for (int ii = 0; ii < m_Scene.MeshManager.MeshSplitNumber; ++ii)
            {
                Color[] colors = ApplicationState.Module3D.JuBrainAtlas.ConvertIndicesToColors(m_SplitAtlasIndices[ii], SelectedAtlasArea);
                m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.colors = colors;
                foreach (Column3D column in m_Scene.Columns)
                {
                    column.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().sharedMesh.colors = colors;
                }
            }
        }
        /// <summary>
        /// Display the information about the atlas area under the mouse
        /// </summary>
        /// <param name="canDisplay">Can we display the atlas information ? (Did we hit a mesh or a cut with the raycast ?)</param>
        /// <param name="hitPoint">Point on the mesh or on the cut where the atlas area has to be considered</param>
        public void DisplayAtlasInformation(bool canDisplay, Vector3 hitPoint)
        {
            if (canDisplay && DisplayJuBrainAtlas)
            {
                SelectedAtlasArea = ApplicationState.Module3D.JuBrainAtlas.GetClosestAreaIndex(hitPoint);
                string[] information = ApplicationState.Module3D.JuBrainAtlas.GetInformation(SelectedAtlasArea);
                if (information.Length == 5)
                {
                    ApplicationState.Module3D.OnDisplayAtlasInformation.Invoke(new AtlasInfo(true, Input.mousePosition, information[0], information[1], information[2], information[3], information[4]));
                }
                else
                {
                    ApplicationState.Module3D.OnDisplayAtlasInformation.Invoke(new AtlasInfo(false, Input.mousePosition));
                }
            }
            else
            {
                SelectedAtlasArea = -1;
                ApplicationState.Module3D.OnDisplayAtlasInformation.Invoke(new AtlasInfo(false, Input.mousePosition));
            }
        }
        #endregion
    }
}