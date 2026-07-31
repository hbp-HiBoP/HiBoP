using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HBP.Core.Enums;
using HBP.Core.Exceptions;
using HBP.Core.Data;
using HBP.Core.Preferences;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Class responsible for managing the meshes of the scene
    /// </summary>
    /// <remarks>
    /// This class can load and store meshes for the corresponding scene.
    /// It is also used to select which mesh to display on the scene.
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
        public List<Core.Object3D.Mesh3D> Meshes { get; set; } = new List<Core.Object3D.Mesh3D>();
        /// <summary>
        /// List of all the loaded meshes
        /// </summary>
        public List<Core.Object3D.Mesh3D> LoadedMeshes { get { return (from mesh in Meshes where mesh.IsLoaded select mesh).ToList(); } }
        /// <summary>
        /// Whether this scene contains a patient mesh backed by persistent patient data.
        /// </summary>
        public bool HasPersistentPatientMesh => Meshes.Any(mesh =>
            mesh.Type == MeshType.Patient && mesh is not Core.Object3D.RuntimeSingleMesh3D);
        /// <summary>
        /// Transient MRI previews owned by this scene, one for each successfully processed patient MRI.
        /// </summary>
        public List<Core.Object3D.RuntimeSingleMesh3D> RuntimePreviewMeshes =>
            Meshes.OfType<Core.Object3D.RuntimeSingleMesh3D>().ToList();
        /// <summary>
        /// Selected Mesh3D ID
        /// </summary>
        public int SelectedMeshID { get; private set; }
        /// <summary>
        /// Selected Mesh3D
        /// </summary>
        public Core.Object3D.Mesh3D SelectedMesh
        {
            get
            {
                return Meshes[SelectedMeshID];
            }
        }
        /// <summary>
        /// List of all the preloaded meshes of the scene
        /// </summary>
        public Dictionary<Patient, List<Core.Object3D.Mesh3D>> PreloadedMeshes { get; set; } = new Dictionary<Patient, List<Core.Object3D.Mesh3D>>();

        /// <summary>
        /// Mesh part to be displayed in the scene
        /// </summary>
        public MeshPart MeshPartToDisplay { get; private set; } = MeshPart.Both;
        /// <summary>
        /// Mesh being displayed in the scene
        /// </summary>
        public Core.DLL.Surface BrainSurface { get; private set; }
        /// <summary>
        /// Simplified mesh to be used in the scene
        /// </summary>
        public Core.DLL.Surface SimplifiedMeshToUse { get; private set; }
        /// <summary>
        /// Center of the loaded mesh
        /// </summary>
        public Vector3 MeshCenter { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a mesh to the mesh manager
        /// </summary>
        /// <param name="mesh">Mesh data to be converted to 3D mesh</param>
        public void Add(BaseMesh mesh)
        {
            if (mesh.IsUsable)
            {
                if (mesh is LeftRightMesh)
                {
                    Core.Object3D.LeftRightMesh3D mesh3D = new((LeftRightMesh)mesh, MeshType.Patient, PersistentDataManager.UserPreferences.Data.Anatomic.MeshPreloading);

                    if (PersistentDataManager.UserPreferences.Data.Anatomic.MeshPreloading)
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
                else if (mesh is SingleMesh)
                {
                    Core.Object3D.SingleMesh3D mesh3D = new((SingleMesh)mesh, MeshType.Patient, PersistentDataManager.UserPreferences.Data.Anatomic.MeshPreloading);

                    if (PersistentDataManager.UserPreferences.Data.Anatomic.MeshPreloading)
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
        /// Adds a loaded, scene-owned MRI preview without mutating persistent patient data or preload caches.
        /// </summary>
        public void AddRuntime(Core.Object3D.RuntimeSingleMesh3D mesh)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (!mesh.IsLoaded || mesh.Both == null || mesh.SimplifiedBoth == null)
                throw new ArgumentException("A runtime mesh must contain loaded complete and simplified surfaces.", nameof(mesh));
            if (RuntimePreviewMeshes.Any(preview => ReferenceEquals(preview.SourceMRI, mesh.SourceMRI)))
                throw new InvalidOperationException($"A runtime mesh for MRI '{mesh.SourceMRIName}' is already registered.");

            MeshPartToDisplay = MeshPart.Both;
            Meshes.Add(mesh);
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Add a mesh to the mesh manager preloaded meshes
        /// </summary>
        /// <param name="mesh">Mesh data to be converted to 3D mesh</param>
        public void AddPreloaded(BaseMesh mesh, Patient patient)
        {
            if (mesh.IsUsable)
            {
                if (!PreloadedMeshes.ContainsKey(patient)) PreloadedMeshes.Add(patient, new List<Core.Object3D.Mesh3D>());
                if (mesh is LeftRightMesh)
                    PreloadedMeshes[patient].Add(new Core.Object3D.LeftRightMesh3D((LeftRightMesh)mesh, MeshType.Patient, true));
                else if (mesh is SingleMesh)
                    PreloadedMeshes[patient].Add(new Core.Object3D.SingleMesh3D((SingleMesh)mesh, MeshType.Patient, true));
            }
        }
        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshName">Name of the mesh to be displayed</param>
        public void Select(string meshName, bool onlyIfAlreadyLoaded = false)
        {
            int meshID = Meshes.FindIndex(m => m.Name == meshName);
            if (meshID == -1 || (onlyIfAlreadyLoaded && !Meshes[meshID].IsLoaded)) meshID = 0;

            SelectAtIndex(meshID);
        }
        /// <summary>
        /// Applies the initial single-patient selection policy without ever treating a transient
        /// preview name as persistent configuration.
        /// </summary>
        public void SelectInitialMeshForScene(string configuredMeshName, string preferredMeshName, string sourceMRIName)
        {
            int meshID = FindLoadedPersistentMesh(configuredMeshName);
            if (meshID == -1 && HasPersistentPatientMesh)
            {
                meshID = FindLoadedPersistentPatientMesh(preferredMeshName);
            }
            if (meshID == -1 && !HasPersistentPatientMesh)
            {
                Core.Object3D.RuntimeSingleMesh3D preview = RuntimePreviewMeshes.FirstOrDefault(mesh =>
                    string.Equals(mesh.SourceMRIName, sourceMRIName, StringComparison.OrdinalIgnoreCase))
                    ?? RuntimePreviewMeshes.FirstOrDefault();
                if (preview != null)
                {
                    meshID = Meshes.IndexOf(preview);
                    MeshPartToDisplay = MeshPart.Both;
                }
            }
            if (meshID == -1) meshID = 0;

            SelectAtIndex(meshID);
        }

        private int FindLoadedPersistentMesh(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            return Meshes.FindIndex(mesh =>
                mesh is not Core.Object3D.RuntimeSingleMesh3D
                && mesh.IsLoaded
                && string.Equals(mesh.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private int FindLoadedPersistentPatientMesh(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            return Meshes.FindIndex(mesh =>
                mesh.Type == MeshType.Patient
                && mesh is not Core.Object3D.RuntimeSingleMesh3D
                && mesh.IsLoaded
                && string.Equals(mesh.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private void SelectAtIndex(int meshID)
        {
            if (meshID < 0 || meshID >= Meshes.Count)
                throw new InvalidOperationException("No mesh is available for selection.");

            SelectedMeshID = meshID;
            ApplySelectedMeshCapabilities();
            m_Scene.SceneInformation.GeometryNeedsUpdate = true;
            m_Scene.ResetGenerators();

            m_Scene.OnUpdateCameraTarget.Invoke(SelectedMesh.Both.Center);
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        private void ApplySelectedMeshCapabilities()
        {
            if (!SelectedMesh.SupportsHemispheres)
            {
                MeshPartToDisplay = MeshPart.Both;
            }
            if (m_Scene.AtlasManager.DisplayMarsAtlas && !SelectedMesh.SupportsMarsAtlas)
            {
                m_Scene.AtlasManager.DisplayMarsAtlas = false;
            }
            if (m_Scene.AtlasManager.DisplayJuBrainAtlas && !SelectedMesh.SupportsMNIResources)
            {
                m_Scene.AtlasManager.DisplayJuBrainAtlas = false;
            }
            if (m_Scene.FMRIManager.DisplayIBCContrasts && !SelectedMesh.SupportsMNIResources)
            {
                m_Scene.FMRIManager.DisplayIBCContrasts = false;
            }
            if (m_Scene.FMRIManager.DisplayDiFuMo && !SelectedMesh.SupportsMNIResources)
            {
                m_Scene.FMRIManager.DisplayDiFuMo = false;
            }
            if (m_Scene.FMRIManager.DisplayLocalizers && !SelectedMesh.SupportsMNIResources)
            {
                m_Scene.FMRIManager.DisplayLocalizers = false;
            }
            if (!SelectedMesh.SupportsMarsAtlas)
            {
                foreach (Column3DCCEP column in m_Scene.ColumnsCCEP.Where(column => column.Mode == Column3DCCEP.CCEPMode.MarsAtlas))
                {
                    column.Mode = Column3DCCEP.CCEPMode.Site;
                }
            }
        }
        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay">Mesh part to be displayed</param>
        public void SelectMeshPart(MeshPart meshPartToDisplay)
        {
            MeshPartToDisplay = SelectedMesh.SupportsHemispheres ? meshPartToDisplay : MeshPart.Both;
            m_Scene.SceneInformation.GeometryNeedsUpdate = true;
            m_Scene.ResetGenerators();
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
            BrainSurface.UpdateMeshFromDLL(m_DisplayedObjects.Brain.GetComponent<MeshFilter>().mesh);
            foreach (Column3D column in m_Scene.Columns)
                column.UpdateColumnBrainMesh(m_DisplayedObjects.Brain);
        }
        /// <summary>
        /// Update meshes to display (fills information)
        /// </summary>
        public void UpdateMeshesInformation()
        {
            if (SelectedMesh is Core.Object3D.LeftRightMesh3D selectedMesh)
            {
                switch (MeshPartToDisplay)
                {
                    case MeshPart.Left:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedLeft;
                        BrainSurface = selectedMesh.Left;
                        break;
                    case MeshPart.Right:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedRight;
                        BrainSurface = selectedMesh.Right;
                        break;
                    case MeshPart.Both:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedBoth;
                        BrainSurface = selectedMesh.Both;
                        break;
                    default:
                        SimplifiedMeshToUse = selectedMesh.SimplifiedBoth;
                        BrainSurface = selectedMesh.Both;
                        break;
                }
            }
            else
            {
                SimplifiedMeshToUse = SelectedMesh.SimplifiedBoth;
                BrainSurface = SelectedMesh.Both;
            }
            // get the middle
            MeshCenter = BrainSurface.Center;
            m_Scene.BrainMaterials.SetBrainCenter(MeshCenter);

            m_Scene.UpdateAllCutPlanes();
        }
        /// <summary>
        /// Initialize the meshes of the scene
        /// </summary>
        public void InitializeMeshes()
        {
            m_DisplayedObjects.InstantiateBrain();
            m_DisplayedObjects.InstantiateSimplifiedBrain();
        }
        #endregion
    }
}
