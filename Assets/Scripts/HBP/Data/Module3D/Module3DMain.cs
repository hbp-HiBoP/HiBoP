using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Base class of the 3D module
    /// Used to control everything from the outside
    /// </summary>
    public class Module3DMain : Singleton<Module3DMain>
    {
        #region Properties

        /// <summary>
        /// Default layer string for the visible meshes layer
        /// </summary>
        public const string DEFAULT_MESHES_LAYER = "Default";

        /// <summary>
        /// Default layer string for the invisible meshes layer
        /// </summary>
        public const string HIDDEN_MESHES_LAYER = "Hidden Meshes";

        /// <summary>
        /// Currently selected scene
        /// </summary>
        public static Base3DScene SelectedScene
        {
            get { return m_Instance.m_Scenes.FirstOrDefault(s => s.IsSelected); }
        }

        /// <summary>
        /// Currently selected column
        /// </summary>
        public static Column3D SelectedColumn
        {
            get { return SelectedScene?.SelectedColumn; }
        }

        /// <summary>
        /// Currently selected view
        /// </summary>
        public static View3D SelectedView
        {
            get { return SelectedColumn?.SelectedView; }
        }

        /// <summary>
        /// Maximum number of views a user can add to a scene
        /// </summary>
        public const int MAXIMUM_VIEW_NUMBER = 5;

        /// <summary>
        /// Space between scenes in world space
        /// </summary>
        public const int SPACE_BETWEEN_SCENES_GAME_OBJECTS = 3000;

        /// <summary>
        /// Number of scenes that have been loaded in this instance of HiBoP
        /// </summary>
        public static int NumberOfScenesLoadedSinceStart { get; set; }

        private List<Base3DScene> m_Scenes = new();

        /// <summary>
        /// List of open scenes
        /// </summary>
        public static ReadOnlyCollection<Base3DScene> Scenes
        {
            get { return new ReadOnlyCollection<Base3DScene>(m_Instance.m_Scenes); }
        }

        /// <summary>
        /// List of all the loaded visualizations
        /// </summary>
        public static ReadOnlyCollection<Visualization> Visualizations
        {
            get { return new ReadOnlyCollection<Visualization>((from scene in Scenes select scene.Visualization).ToList()); }
        }

        [SerializeField] private SharedMaterials m_SharedMaterials;

        public static SharedMaterials SharedMaterials
        {
            get { return m_Instance.m_SharedMaterials; }
        }

        [SerializeField] private GameObject m_SharedDirectionalLight;

        /// <summary>
        /// Shared directional light between all scenes
        /// </summary>
        public static GameObject SharedDirectionalLight
        {
            get { return m_Instance.m_SharedDirectionalLight; }
        }

        [SerializeField] private GameObject m_SharedSpotlight;

        /// <summary>
        /// Shared spotlight between all scenes
        /// </summary>
        public static GameObject SharedSpotlight
        {
            get { return m_Instance.m_SharedSpotlight; }
        }

        /// <summary>
        /// Parent gameobject of every scenes
        /// </summary>
        [SerializeField] private Transform m_ScenesParent;

        /// <summary>
        /// Prefab corresponding to a scene
        /// </summary>
        [SerializeField] private GameObject m_ScenePrefab;

        #endregion

        #region Events

        /// <summary>
        /// Event called when hovering a site to display its information
        /// </summary>
        [HideInInspector] public static GenericEvent<SiteInfo> OnDisplaySiteInformation = new();

        /// <summary>
        /// Event called when hovering a atlas area to display its information
        /// </summary>
        [HideInInspector] public static GenericEvent<AtlasInfo> OnDisplayAtlasInformation = new();

        /// <summary>
        /// Event called when a scene is added
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnAddScene = new();

        /// <summary>
        /// Event called when a scene is removed
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnRemoveScene = new();

        /// <summary>
        /// Event called after all new scenes have been opened and initialized
        /// </summary>
        [HideInInspector] public static UnityEvent OnFinishedAddingNewScenes = new();

        /// <summary>
        /// Event called when changing the selected scene
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnSelectScene = new();

        [HideInInspector] public static GenericEvent<Base3DScene> OnDeselectScene = new();

        /// <summary>
        /// Event called when minimizing a scene
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnMinimizeScene = new();

        /// <summary>
        /// Event called when changing the selected column
        /// </summary>
        [HideInInspector] public static GenericEvent<Column3D> OnSelectColumn = new();

        /// <summary>
        /// Event called when changing the selected view
        /// </summary>
        [HideInInspector] public static GenericEvent<View3D> OnSelectView = new();

        /// <summary>
        /// Event called when changing the index of the timeline of the selected column
        /// </summary>
        [HideInInspector] public static UnityEvent OnUpdateSelectedColumnTimeLineIndex = new();

        /// <summary>
        /// Event called when requesting an update in the toolbar
        /// </summary>
        [HideInInspector] public static UnityEvent OnRequestUpdateInToolbar = new();

        [HideInInspector] public static UnityEvent OnRequestUpdateInSiteList = new();

        #endregion

        #region Private Methods

        protected override void Initialization()
        {
            SpecificSiteLocationFilterCondition.SceneLocationEvaluator = CheckSpecificSiteLocation;
            Preload3D();
        }

        private static bool? CheckSpecificSiteLocation(SpecificSiteLocationFilterCondition condition, Core.Object3D.Site site)
        {
            Base3DScene selectedScene = SelectedScene;
            if (selectedScene == null) return false;

            switch (condition.LocationType)
            {
                case SpecificSiteLocationFilterCondition.SpecificLocationType.BrainMesh:
                    Surface mesh = condition.MeshPart switch
                    {
                        MeshPart.Both => selectedScene.MeshManager.SelectedMesh.SimplifiedBoth,
                        MeshPart.Left => selectedScene.MeshManager.SelectedMesh is LeftRightMesh3D leftRightMesh ? leftRightMesh.SimplifiedLeft : null,
                        MeshPart.Right => selectedScene.MeshManager.SelectedMesh is LeftRightMesh3D leftRightMesh ? leftRightMesh.SimplifiedRight : null,
                        _ => null
                    };
                    return mesh != null && mesh.IsPointInside(site.Information.DefaultPosition);
                case SpecificSiteLocationFilterCondition.SpecificLocationType.CutPlane:
                    var planes = selectedScene.Cuts.Select(c => (Core.DLL.Plane)c).ToList();
                    return selectedScene.ImplantationManager.SelectedImplantation.RawSiteList.IsSiteOnAnyPlane(site, planes, 1.0f);
                default:
                    return null;
            }
        }

        void OnDestroy()
        {
            Object3DManager.Reset();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Remove every scenes corresponding to a visualization
        /// </summary>
        /// <param name="visualization">Visualization corresponding to the scenes to be removed</param>
        public static void RemoveScene(Visualization visualization)
        {
            Base3DScene[] scenes = Scenes.Where(s => s.Visualization == visualization).ToArray();
            foreach (var scene in scenes)
            {
                RemoveScene(scene);
            }
        }

        /// <summary>
        /// Remove a scene
        /// </summary>
        /// <param name="scene">Scene to be removed</param>
        public static void RemoveScene(Base3DScene scene)
        {
            OnRemoveScene.Invoke(scene);
            m_Instance.m_Scenes.Remove(scene);
            scene.Clean().Forget();
        }

        /// <summary>
        /// Load a single patient scene extracted from a visualization
        /// </summary>
        /// <param name="visualization">Visualization from which the new visualization will be extracted</param>
        /// <param name="patient">Patient of the new visualization</param>
        public static Visualization PrepareSinglePatientVisualizationFromMultiPatientScene(Visualization visualization, Patient patient)
        {
            Base3DScene scene = Scenes.FirstOrDefault(s => s.Visualization == visualization);
            scene.SaveConfiguration();
            Visualization visualizationToLoad = visualization.Clone() as Visualization;
            visualizationToLoad.Name = patient.Name;
            visualizationToLoad.Patients = new List<Patient>() { patient };
            visualizationToLoad.Configuration.MeshName = PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMeshInSinglePatientVisualization;
            visualizationToLoad.Configuration.MRIName = PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization;
            visualizationToLoad.Configuration.ImplantationName = PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedImplantationInSinglePatientVisualization;
            if (scene.SelectedColumn.SelectedSite)
            {
                visualizationToLoad.Configuration.FirstSiteToSelect = scene.SelectedColumn.SelectedSite.Information.Name;
                visualizationToLoad.Configuration.FirstColumnToSelect = scene.Columns.FindIndex(c => c == scene.SelectedColumn);
            }

            if (PersistentDataManager.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization)
            {
                visualizationToLoad.Configuration.PreloadedMeshes = scene.MeshManager.PreloadedMeshes[patient];
                visualizationToLoad.Configuration.PreloadedMRIs = scene.MRIManager.PreloadedMRIs[patient];
            }

            visualizationToLoad.GenerateID();
            return visualizationToLoad;
        }

        /// <summary>
        /// Save all the configurations of the scenes
        /// </summary>
        public static void SaveConfigurations()
        {
            foreach (var scene in Scenes)
            {
                scene.SaveConfiguration();
            }
        }

        /// <summary>
        /// Reload all scenes
        /// </summary>
        public static List<Visualization> PrepareReloadScenes()
        {
            SaveConfigurations();
            List<Base3DScene> scenes = Scenes.ToList();
            foreach (Base3DScene scene in scenes)
            {
                RemoveScene(scene);
            }

            IEnumerable<string> visualizationIDs = (from scene in scenes select scene.Visualization.ID);
            return (from visualization in ApplicationState.LoadedProject.Visualizations where visualizationIDs.Contains(visualization.ID) select visualization).ToList();
        }

        /// <summary>
        /// Remove all scenes
        /// </summary>
        public static void RemoveAllScenes()
        {
            List<Base3DScene> scenes = Scenes.ToList();
            foreach (Base3DScene scene in scenes)
            {
                RemoveScene(scene);
            }
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// Coroutine used to load visualizations one by one
        /// </summary>
        /// <param name="visualizations">Visualizations to be loaded</param>
        /// <returns></returns>
        public static async UniTask LoadAsync(IEnumerable<Visualization> visualizations, Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            Visualization[] visualizationSnapshot = visualizations.ToArray();
            Project project = ApplicationState.LoadedProject;
            DataInfo[] requiredDataInfos = visualizationSnapshot.SelectMany(visualization => visualization.GetRequiredDataInfos()).Distinct().ToArray();
            Patient[] requiredPatients = visualizationSnapshot.SelectMany(visualization => visualization.Patients).Distinct().ToArray();
            ValidationRequest validationRequest = new(ValidationAspect.None);
            if (requiredDataInfos.Length > 0)
            {
                validationRequest = validationRequest.Merge(new ValidationRequest(ValidationAspect.DataInfoAll, dataInfoIDs: requiredDataInfos.Select(dataInfo => dataInfo.ID)));
            }

            if (requiredPatients.Length > 0)
            {
                validationRequest = validationRequest.Merge(new ValidationRequest(ValidationAspect.PatientAssets, patientIDs: requiredPatients.Select(patient => patient.ID)));
            }

            float validationWeight = project != null && project.RequiresValidation(validationRequest) ? 0.25f : 0;
            if (project != null && validationWeight > 0)
            {
                await project.EnsureProjectValidatedForImmediateLoadAsync(validationRequest, (progress, duration, text) => onChangeProgress(progress * validationWeight, duration, text), token);
            }

            token.ThrowIfCancellationRequested();

            Dictionary<Visualization, int> weightByVisualization = visualizationSnapshot.ToDictionary(v => v, v => (v.CCEPColumns.Count + v.IEEGColumns.Count) * v.Patients.Count + v.AnatomicColumns.Count + v.FMRIColumns.Count + v.MEGColumns.Count + v.StaticColumns.Count);
            int totalWeight = weightByVisualization.Values.Sum();
            float progress = 0;
            const float LOADING_VISUALIZATION_PROGRESS = 0.5f;
            const float LOADING_SCENE_PROGRESS = 0.5f;
            foreach (Visualization visualization in visualizationSnapshot)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    float visualizationWeight = (float)weightByVisualization[visualization] / totalWeight;
                    if (!visualization.IsVisualizable) throw new CanNotLoadVisualization(visualization.Name);
                    await visualization.LoadAsync((localProgress, duration, text) => onChangeProgress(validationWeight + (progress + localProgress * visualizationWeight * LOADING_VISUALIZATION_PROGRESS) * (1 - validationWeight), duration, text), token);
                    await LoadSceneAsync(visualization, (localProgress, duration, text) => onChangeProgress(validationWeight + (progress + (LOADING_VISUALIZATION_PROGRESS + localProgress * LOADING_SCENE_PROGRESS) * visualizationWeight) * (1 - validationWeight), duration, text), token);
                    progress += visualizationWeight;
                }
                catch (OperationCanceledException e)
                {
                    visualization.Unload();
                    throw e;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    visualization.Unload();
                    throw e;
                }
            }

            OnFinishedAddingNewScenes.Invoke();
        }

        /// <summary>
        /// Coroutine to load a visualization asynchronously
        /// </summary>
        /// <param name="visualization">Visualization to be loaded</param>
        /// <param name="onChangeProgress">Event to update the loading circle</param>
        /// <returns></returns>
        private static async UniTask LoadSceneAsync(Visualization visualization, Action<float, float, LoadingText> onChangeProgress, CancellationToken token)
        {
            await UniTask.SwitchToMainThread();
            Base3DScene scene = Instantiate(m_Instance.m_ScenePrefab, m_Instance.m_ScenesParent).GetComponent<Base3DScene>();
            scene.Initialize(visualization);
            token.ThrowIfCancellationRequested();
            await scene.InitializeAsync(visualization, onChangeProgress, token);
            // Add the listeners
            scene.OnSelect.AddListener(() =>
            {
                foreach (Base3DScene s in m_Instance.m_Scenes)
                {
                    if (s != scene)
                    {
                        s.IsSelected = false;
                    }
                }
            });
            // Add the scene to the list
            m_Instance.m_Scenes.Add(scene);
            scene.FinalizeInitialization();
            OnAddScene.Invoke(scene);
            scene.LoadConfiguration();
            IProgress<float> inflationProgress = new Progress<float>(value => onChangeProgress(value, 0.0f, new LoadingText("Inflating surface")));
            await scene.RestoreConfiguredSurfaceRepresentationAsync(inflationProgress, token, animate: false);
        }

        private static void Preload3D()
        {
            // Graphic Settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;
            QualitySettings.vSyncCount = 0;

            // Objects 3D
            Object3DManager.MNI.Load().Forget();
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadDiFuMo64) Object3DManager.DiFuMo.Load("64");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadDiFuMo128) Object3DManager.DiFuMo.Load("128");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadDiFuMo256) Object3DManager.DiFuMo.Load("256");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadDiFuMo512) Object3DManager.DiFuMo.Load("512");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadDiFuMo1024) Object3DManager.DiFuMo.Load("1024");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadIBC) Object3DManager.IBC.Load();
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadMarsAtlas) Object3DManager.MarsAtlas.Load();
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadJuBrain) Object3DManager.JuBrain.Load();
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerAUDI) Object3DManager.Localizers.TryLoad("AUDI");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerLEC1) Object3DManager.Localizers.TryLoad("LEC1");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerLEC2) Object3DManager.Localizers.TryLoad("LEC2");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerMCSE) Object3DManager.Localizers.TryLoad("MCSE");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerMOTO) Object3DManager.Localizers.TryLoad("MOTO");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerMVEB) Object3DManager.Localizers.TryLoad("MVEB");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerMVIS) Object3DManager.Localizers.TryLoad("MVIS");
            if (PersistentDataManager.UserPreferences.Data.Atlases.PreloadLocalizerVISU) Object3DManager.Localizers.TryLoad("VISU");
        }

        #endregion
    }
}
