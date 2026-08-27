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
        public List<Core.Object3D.Mesh3D> LoadedMeshes
        {
            get { return (from mesh in Meshes where mesh.IsLoaded select mesh).ToList(); }
        }

        /// <summary>
        /// Whether this scene contains a patient mesh backed by persistent patient data.
        /// </summary>
        public bool HasPersistentPatientMesh => Meshes.Any(mesh => mesh.Type == MeshType.Patient && mesh is not Core.Object3D.RuntimeSingleMesh3D);

        /// <summary>
        /// Transient MRI previews owned by this scene, one for each successfully processed patient MRI.
        /// </summary>
        public List<Core.Object3D.RuntimeSingleMesh3D> RuntimePreviewMeshes => Meshes.OfType<Core.Object3D.RuntimeSingleMesh3D>().ToList();

        /// <summary>
        /// Selected Mesh3D ID
        /// </summary>
        public int SelectedMeshID { get; private set; }

        /// <summary>
        /// Selected Mesh3D
        /// </summary>
        public Core.Object3D.Mesh3D SelectedMesh
        {
            get { return Meshes[SelectedMeshID]; }
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
        /// Anatomical surface corresponding vertex-for-vertex to <see cref="BrainSurface"/>.
        /// Scientific projections and spatial associations must use this surface.
        /// </summary>
        public Core.DLL.Surface ReferenceSurface { get; private set; }

        /// <summary>
        /// Simplified anatomical mesh used by cut colliders in the scene.
        /// </summary>
        public Core.DLL.Surface SimplifiedMeshToUse { get; private set; }

        /// <summary>
        /// Simplified representation matching the displayed brain, used by its final collider.
        /// </summary>
        public Core.DLL.Surface SimplifiedBrainSurface { get; private set; }

        /// <summary>
        /// Center of the loaded mesh
        /// </summary>
        public Vector3 MeshCenter { get; private set; }

        /// <summary>
        /// Whether anatomical cut planes may clip the currently displayed brain surface.
        /// </summary>
        public bool CanClipBrainSurface => Meshes.Count > 0 && (m_Scene == null || !m_Scene.IsSurfaceRepresentationTransitioning) && SelectedMesh.Representation == Core.Object3D.SurfaceRepresentation.Anatomical;

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
                Core.Object3D.RuntimeSingleMesh3D preview = RuntimePreviewMeshes.FirstOrDefault(mesh => string.Equals(mesh.SourceMRIName, sourceMRIName, StringComparison.OrdinalIgnoreCase)) ?? RuntimePreviewMeshes.FirstOrDefault();
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
            return Meshes.FindIndex(mesh => mesh is not Core.Object3D.RuntimeSingleMesh3D && mesh.IsLoaded && string.Equals(mesh.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private int FindLoadedPersistentPatientMesh(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            return Meshes.FindIndex(mesh => mesh.Type == MeshType.Patient && mesh is not Core.Object3D.RuntimeSingleMesh3D && mesh.IsLoaded && string.Equals(mesh.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private void SelectAtIndex(int meshID)
        {
            if (meshID < 0 || meshID >= Meshes.Count)
                throw new InvalidOperationException("No mesh is available for selection.");

            SelectedMeshID = meshID;
            ApplySelectedMeshCapabilities();
            m_Scene.SceneInformation.GeometryNeedsUpdate = true;
            m_Scene.InvalidateSurfaceProjection();

            MeshPart selectedPart = SelectedMesh.SupportsHemispheres ? MeshPartToDisplay : MeshPart.Both;
            m_Scene.OnUpdateCameraTarget.Invoke(SelectedMesh.GetSurface(SelectedMesh.Representation, selectedPart).Center);
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
            m_Scene.InvalidateSurfaceProjection();
        }

        /// <summary>
        /// Selects the representation displayed for the current anatomical mesh.
        /// This does not invalidate activity projection resources because vertex indices remain identical.
        /// </summary>
        public void SelectRepresentation(Core.Object3D.SurfaceRepresentation representation)
        {
            if (SelectedMesh.Representation == representation) return;

            SelectedMesh.SelectRepresentation(representation);
            m_Scene.SceneInformation.GeometryNeedsUpdate = true;
            m_Scene.InvalidateSurfaceMesh();
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
            Mesh brainMesh = m_DisplayedObjects.Brain.GetComponent<MeshFilter>().mesh;
            BrainSurface.UpdateMeshFromDLL(brainMesh);
            brainMesh.SetUVs(3, Array.Empty<Vector3>());
            brainMesh.SetUVs(4, Array.Empty<Vector3>());
            m_Scene.BrainMaterials.SetInflationBlend(0.0f);
            foreach (Column3D column in m_Scene.Columns)
                column.UpdateColumnBrainMesh(m_DisplayedObjects.Brain);
        }

        /// <summary>
        /// Uploads the anatomical geometry and inflated target streams used by the GPU transition.
        /// </summary>
        public void PrepareRepresentationTransition()
        {
            MeshPart selectedPart = SelectedMesh.SupportsHemispheres ? MeshPartToDisplay : MeshPart.Both;
            Core.DLL.Surface anatomical = SelectedMesh.GetSurface(Core.Object3D.SurfaceRepresentation.Anatomical, selectedPart);
            Core.DLL.Surface inflated = SelectedMesh.GetSurface(Core.Object3D.SurfaceRepresentation.Inflated, selectedPart);
            Mesh brainMesh = m_DisplayedObjects.Brain.GetComponent<MeshFilter>().mesh;
            Mesh inflatedMesh = new();
            try
            {
                anatomical.UpdateMeshFromDLL(brainMesh);
                inflated.UpdateMeshFromDLL(inflatedMesh, all: false, vertices: true, normals: true, uv: false, triangles: false, colors: false);
                brainMesh.SetUVs(3, new List<Vector3>(inflatedMesh.vertices));
                brainMesh.SetUVs(4, new List<Vector3>(inflatedMesh.normals));
                Bounds transitionBounds = brainMesh.bounds;
                transitionBounds.Encapsulate(inflatedMesh.bounds.min);
                transitionBounds.Encapsulate(inflatedMesh.bounds.max);
                brainMesh.bounds = transitionBounds;

                foreach (Column3D column in m_Scene.Columns)
                    column.UpdateColumnBrainMesh(m_DisplayedObjects.Brain);
            }
            finally
            {
                if (Application.isPlaying)
                    Destroy(inflatedMesh);
                else
                    DestroyImmediate(inflatedMesh);
            }
        }

        /// <summary>
        /// Update meshes to display (fills information)
        /// </summary>
        public void UpdateMeshesInformation()
        {
            MeshPart selectedPart = SelectedMesh.SupportsHemispheres ? MeshPartToDisplay : MeshPart.Both;
            ReferenceSurface = SelectedMesh.GetSurface(Core.Object3D.SurfaceRepresentation.Anatomical, selectedPart);
            BrainSurface = SelectedMesh.GetSurface(SelectedMesh.Representation, selectedPart);
            SimplifiedMeshToUse = SelectedMesh.GetSurface(Core.Object3D.SurfaceRepresentation.Anatomical, selectedPart, simplified: true);
            SimplifiedBrainSurface = SelectedMesh.GetSurface(SelectedMesh.Representation, selectedPart, simplified: true);

            if (BrainSurface.NumberOfVertices != ReferenceSurface.NumberOfVertices || BrainSurface.NumberOfTriangles != ReferenceSurface.NumberOfTriangles)
            {
                throw new InvalidOperationException("The displayed representation must preserve the anatomical surface topology.");
            }

            // get the middle
            MeshCenter = BrainSurface.Center;
            m_Scene.BrainMaterials.SetBrainCenter(MeshCenter);
            m_Scene.OnUpdateCameraTarget.Invoke(MeshCenter);

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
