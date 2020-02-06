using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Class responsible for managing the meshes of the scene
    /// </summary>
    /// <remarks>
    /// This class can load and store meshes for the corresponding scene.
    /// It is also used to select which mesh to display on the scene, and in charge of splitting the mesh into smaller meshes.
    /// It also handles information about the JuBrain Atlas concerning the selected mesh.
    /// </remarks>
    public class MeshManager : MonoBehaviour
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
        /// List of all the meshes of the scene
        /// </summary>
        public List<Mesh3D> Meshes = new List<Mesh3D>();
        /// <summary>
        /// List of all the loaded meshes
        /// </summary>
        public List<Mesh3D> LoadedMeshes { get { return (from mesh in Meshes where mesh.IsLoaded select mesh).ToList(); } }
        /// <summary>
        /// Number of splits for the smaller meshes
        /// </summary>
        public int MeshSplitNumber { get; set; }
        /// <summary>
        /// Selected Mesh3D ID
        /// </summary>
        public int SelectedMeshID { get; private set; }
        /// <summary>
        /// Selected Mesh3D
        /// </summary>
        public Mesh3D SelectedMesh
        {
            get
            {
                return Meshes[SelectedMeshID];
            }
        }
        /// <summary>
        /// List of splitted meshes
        /// </summary>
        public List<DLL.Surface> SplittedMeshes = new List<DLL.Surface>();

        /// <summary>
        /// Mesh part to be displayed in the scene
        /// </summary>
        public Data.Enums.MeshPart MeshPartToDisplay { get; private set; } = Data.Enums.MeshPart.Both;
        /// <summary>
        /// Mesh being displayed in the scene
        /// </summary>
        public DLL.Surface MeshToDisplay { get; private set; }
        /// <summary>
        /// Simplified mesh to be used in the scene
        /// </summary>
        public DLL.Surface SimplifiedMeshToUse { get; private set; }
        /// <summary>
        /// Center of the loaded mesh
        /// </summary>
        public Vector3 MeshCenter { get; private set; }
        #endregion

        #region Private Methods
        private void OnDestroy()
        {
            foreach (var mesh in Meshes)
            {
                if (!mesh.HasBeenLoadedOutside)
                {
                    mesh.Clean();
                }
            }
            foreach (var mesh in SplittedMeshes)
            {
                mesh?.Dispose();
            }
        }
        /// <summary>
        /// Reset the number of splits of the brain mesh
        /// </summary>
        /// <param name="nbSplits">Number of splits</param>
        private void ResetSplitsNumber(int nbSplits)
        {
            if (MeshSplitNumber == nbSplits) return;

            MeshSplitNumber = nbSplits;
            m_Scene.DLLCommonBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>(MeshSplitNumber);
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
                m_Scene.DLLCommonBrainTextureGeneratorList.Add(new DLL.MRIBrainGenerator());

            m_DisplayedObjects.InstantiateSplits(nbSplits);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a mesh to the mesh manager
        /// </summary>
        /// <param name="mesh">Mesh data to be converted to 3D mesh</param>
        public void Add(Data.BaseMesh mesh)
        {
            if (mesh.IsUsable)
            {
                if (mesh is Data.LeftRightMesh)
                {
                    LeftRightMesh3D mesh3D = new LeftRightMesh3D((Data.LeftRightMesh)mesh, Data.Enums.MeshType.Patient);

                    if (ApplicationState.UserPreferences.Data.Anatomic.MeshPreloading)
                    {
                        if (mesh3D.IsLoaded)
                        {
                            Meshes.Add(mesh3D);
                        }
                        else
                        {
                            throw new CanNotLoadGIIFile(mesh.Name);
                        }
                    }
                    else
                    {
                        string name = !string.IsNullOrEmpty(m_Scene.Visualization.Configuration.MeshName) ? m_Scene.Visualization.Configuration.MeshName : "Grey matter";
                        if (mesh3D.Name == name) mesh3D.Load();
                        Meshes.Add(mesh3D);
                    }
                }
                else if (mesh is Data.SingleMesh)
                {
                    SingleMesh3D mesh3D = new SingleMesh3D((Data.SingleMesh)mesh, Data.Enums.MeshType.Patient);

                    if (ApplicationState.UserPreferences.Data.Anatomic.MeshPreloading)
                    {
                        if (mesh3D.IsLoaded)
                        {
                            Meshes.Add(mesh3D);
                        }
                        else
                        {
                            throw new CanNotLoadGIIFile(mesh.Name);
                        }
                    }
                    else
                    {
                        string name = !string.IsNullOrEmpty(m_Scene.Visualization.Configuration.MeshName) ? m_Scene.Visualization.Configuration.MeshName : "Grey matter";
                        if (mesh3D.Name == name) mesh3D.Load();
                        Meshes.Add(mesh3D);
                    }
                }
                else
                {
                    Debug.LogError("Mesh not handled.");
                }
            }
        }
        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshName">Name of the mesh to be displayed</param>
        public void Select(string meshName)
        {
            int meshID = Meshes.FindIndex(m => m.Name == meshName);
            if (meshID == -1) meshID = 0;

            SelectedMeshID = meshID;
            if (m_Scene.AtlasManager.DisplayMarsAtlas && (!SelectedMesh.IsMarsAtlasLoaded || SelectedMesh.Type != Data.Enums.MeshType.MNI))
            {
                m_Scene.AtlasManager.DisplayMarsAtlas = false;
            }
            if (m_Scene.AtlasManager.DisplayJuBrainAtlas && SelectedMesh.Type != Data.Enums.MeshType.MNI)
            {
                m_Scene.AtlasManager.DisplayJuBrainAtlas = false;
            }
            if (m_Scene.FMRIManager.DisplayIBCContrasts && SelectedMesh.Type != Data.Enums.MeshType.MNI)
            {
                m_Scene.FMRIManager.DisplayIBCContrasts = false;
            }
            m_Scene.MeshGeometryNeedsUpdate = true;
            m_Scene.ResetIEEG();
            foreach (Column3D column in m_Scene.Columns)
            {
                column.IsRenderingUpToDate = false;
            }

            m_Scene.OnUpdateCameraTarget.Invoke(SelectedMesh.Both.Center);
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay">Mesh part to be displayed</param>
        public void SelectMeshPart(Data.Enums.MeshPart meshPartToDisplay)
        {
            MeshPartToDisplay = meshPartToDisplay;
            m_Scene.MeshGeometryNeedsUpdate = true;
            m_Scene.ResetIEEG();
            foreach (Column3D column in m_Scene.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
        }
        /// <summary>
        /// Load every mesh that has not been loaded yet
        /// </summary>
        public void LoadMissing()
        {
            foreach (var mesh in Meshes)
            {
                if (!mesh.IsLoaded) mesh.Load();
            }
        }
        /// <summary>
        /// Update the surface meshes from the DLL
        /// </summary>
        public void UpdateMeshesFromDLL()
        {
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
            {
                SplittedMeshes[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }
            UnityEngine.Profiling.Profiler.BeginSample("Update Columns Meshes");
            foreach (Column3D column in m_Scene.Columns)
            {
                column.UpdateColumnMeshes(m_DisplayedObjects.BrainSurfaceMeshes);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update meshes to display (fills information and splits the mesh)
        /// </summary>
        public void UpdateMeshesInformation()
        {
            if (SelectedMesh is LeftRightMesh3D selectedMesh)
            {
                switch (MeshPartToDisplay)
                {
                    case Data.Enums.MeshPart.Left:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedLeft;
                        MeshToDisplay = selectedMesh.Left;
                        break;
                    case Data.Enums.MeshPart.Right:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedRight;
                        MeshToDisplay = selectedMesh.Right;
                        break;
                    case Data.Enums.MeshPart.Both:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedBoth;
                        MeshToDisplay = selectedMesh.Both;
                        break;
                    default:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedBoth;
                        MeshToDisplay = selectedMesh.Both;
                        break;
                }
            }
            else
            {
                SimplifiedMeshToUse = SelectedMesh.SimplifiedBoth;
                MeshToDisplay = SelectedMesh.Both;
            }
            // get the middle
            MeshCenter = MeshToDisplay.Center;
            m_Scene.BrainMaterial.SetVector("_Center", MeshCenter);

            SplittedMeshes = MeshToDisplay.SplitToSurfaces(MeshSplitNumber);

            m_Scene.UpdateAllCutPlanes();
        }
        /// <summary>
        /// Generate the split number regarding all meshes
        /// </summary>
        /// <param name="auto">If true, the number of splits will be automatically generated. Otherwise, 10 splits will be used.</param>
        public void GenerateSplits(bool auto)
        {
            int splits = 0;
            if (auto)
            {
                int maxVertices = (from mesh in Meshes select mesh.Both.NumberOfVertices).Max();
                splits = (maxVertices / 65000) + (((maxVertices % 60000) != 0) ? 3 : 2);
            }
            else
            {
                splits = 10;
            }
            ResetSplitsNumber(splits);
        }
        #endregion
    }
}