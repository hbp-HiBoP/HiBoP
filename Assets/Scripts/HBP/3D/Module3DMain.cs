using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections;
using ThirdParty.CielaSpike;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.UI.Tools;

namespace HBP.Display.Module3D
{
    /// <summary>
    /// Base class of the 3D module
    /// Used to control everything from the outside
    /// </summary>
    public class Module3DMain : MonoBehaviour
    {
        #region Properties
        private static Module3DMain m_Instance;

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
            get
            {
                return m_Instance.m_Scenes.FirstOrDefault(s => s.IsSelected);
            }
        }
        /// <summary>
        /// Currently selected column
        /// </summary>
        public static Column3D SelectedColumn
        {
            get
            {
                return SelectedScene?.SelectedColumn;
            }
        }
        /// <summary>
        /// Currently selected view
        /// </summary>
        public static View3D SelectedView
        {
            get
            {
                return SelectedColumn?.SelectedView;
            }
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

        private List<Base3DScene> m_Scenes = new List<Base3DScene>();
        /// <summary>
        /// List of open scenes
        /// </summary>
        public static ReadOnlyCollection<Base3DScene> Scenes
        {
            get
            {
                return new ReadOnlyCollection<Base3DScene>(m_Instance.m_Scenes);
            }
        }
        /// <summary>
        /// List of all the loaded visualizations
        /// </summary>
        public static ReadOnlyCollection<Visualization> Visualizations
        {
            get
            {
                return new ReadOnlyCollection<Visualization>((from scene in Scenes select scene.Visualization).ToList());
            }
        }
        
        [SerializeField] private GameObject m_SharedDirectionalLight;
        /// <summary>
        /// Shared directional light between all scenes
        /// </summary>
        public static GameObject SharedDirectionalLight { get { return m_Instance.m_SharedDirectionalLight; } }
        [SerializeField] private GameObject m_SharedSpotlight;
        /// <summary>
        /// Shared spotlight between all scenes
        /// </summary>
        public static GameObject SharedSpotlight { get { return m_Instance.m_SharedSpotlight; } }

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
        [HideInInspector] public static GenericEvent<SiteInfo> OnDisplaySiteInformation = new GenericEvent<SiteInfo>();
        /// <summary>
        /// Event called when hovering a atlas area to display its information
        /// </summary>
        [HideInInspector] public static GenericEvent<AtlasInfo> OnDisplayAtlasInformation = new GenericEvent<AtlasInfo>();
        /// <summary>
        /// Event called when a scene is added
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnAddScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when a scene is removed
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnRemoveScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called after all new scenes have been opened and initialized
        /// </summary>
        [HideInInspector] public static UnityEvent OnFinishedAddingNewScenes = new UnityEvent();
        /// <summary>
        /// Event called when changing the selected scene
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnSelectScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when minimizing a scene
        /// </summary>
        [HideInInspector] public static GenericEvent<Base3DScene> OnMinimizeScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when changing the selected column
        /// </summary>
        [HideInInspector] public static GenericEvent<Column3D> OnSelectColumn = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when changing the selected view
        /// </summary>
        [HideInInspector] public static GenericEvent<View3D> OnSelectView = new GenericEvent<View3D>();
        /// <summary>
        /// Event called when changing the index of the timeline of the selected column
        /// </summary>
        [HideInInspector] public static UnityEvent OnUpdateSelectedColumnTimeLineIndex = new UnityEvent();
        /// <summary>
        /// Event called when requesting an update in the toolbar
        /// </summary>
        [HideInInspector] public static UnityEvent OnRequestUpdateInToolbar = new UnityEvent();
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else
            {
                Destroy(this);
            }

            this.StartCoroutineAsync(c_Preload3D());
        }
        void OnDestroy()
        {
            Object3DManager.Clean();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a list of visualizations into 3D scenes
        /// </summary>
        /// <param name="visualizations">Visualizations to be loaded</param>
        public static void LoadScenes(IEnumerable<Visualization> visualizations)
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_Load(visualizations, (progress, duration, text) => onChangeProgress.Invoke(progress, duration,text)), onChangeProgress);
        }
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
            m_Instance.StartCoroutine(scene.c_Destroy());
        }
        /// <summary>
        /// Load a single patient scene extracted from a visualization
        /// </summary>
        /// <param name="visualization">Visualization from which the new visualization will be extracted</param>
        /// <param name="patient">Patient of the new visualization</param>
        public static void LoadSinglePatientSceneFromMultiPatientScene(Visualization visualization, Patient patient)
        {
            Base3DScene scene = Scenes.FirstOrDefault(s => s.Visualization == visualization);
            scene.SaveConfiguration();
            Visualization visualizationToLoad = visualization.Clone() as Visualization;
            visualizationToLoad.Name = patient.Name;
            visualizationToLoad.Patients = new List<Patient>() { patient };
            visualizationToLoad.Configuration.MeshName = ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMeshInSinglePatientVisualization;
            visualizationToLoad.Configuration.MRIName = ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization;
            visualizationToLoad.Configuration.ImplantationName = ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedImplantationInSinglePatientVisualization;
            if (scene.SelectedColumn.SelectedSite)
            {
                visualizationToLoad.Configuration.FirstSiteToSelect = scene.SelectedColumn.SelectedSite.Information.Name;
                visualizationToLoad.Configuration.FirstColumnToSelect = scene.Columns.FindIndex(c => c = scene.SelectedColumn);
            }
            if (ApplicationState.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization)
            {
                visualizationToLoad.Configuration.PreloadedMeshes = scene.MeshManager.PreloadedMeshes[patient];
                visualizationToLoad.Configuration.PreloadedMRIs = scene.MRIManager.PreloadedMRIs[patient];
            }
            visualizationToLoad.GenerateID();
            LoadScenes(new Visualization[] { visualizationToLoad });
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
        public static void ReloadScenes()
        {
            SaveConfigurations();
            List<Base3DScene> scenes = Scenes.ToList();
            foreach (Base3DScene scene in scenes)
            {
                RemoveScene(scene);
            }
            IEnumerable<string> visualizationIDs = (from scene in scenes select scene.Visualization.ID);
            LoadScenes(from visualization in ApplicationState.ProjectLoaded.Visualizations where visualizationIDs.Contains(visualization.ID) select visualization);
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
        public static IEnumerator c_Load(IEnumerable<Visualization> visualizations, Action<float,float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;

            Dictionary<Visualization, int> weightByVisualization = visualizations.ToDictionary(v => v, v => (v.CCEPColumns.Count + v.IEEGColumns.Count) * v.Patients.Count + v.AnatomicColumns.Count + v.FMRIColumns.Count + v.MEGColumns.Count);
            int totalWeight = weightByVisualization.Values.Sum();
            float progress = 0;
            const float LOADING_VISUALIZATION_PROGRESS = 0.5f;
            const float LOADING_SCENE_PROGRESS = 0.5f;
            foreach (Visualization visualization in visualizations)
            {
                float visualizationWeight = (float)weightByVisualization[visualization] / totalWeight;
                if (!visualization.IsVisualizable) throw new CanNotLoadVisualization(visualization.Name);

                yield return Ninja.JumpToUnity;
                yield return m_Instance.StartCoroutineAsync(visualization.c_Load((localProgress, duration, text) => onChangeProgress(progress + localProgress * visualizationWeight * LOADING_VISUALIZATION_PROGRESS, duration, text)), out Task visualizationLoadingTask);

                if (visualizationLoadingTask.State == TaskState.Done)
                {
                    yield return m_Instance.StartCoroutineAsync(c_LoadScene(visualization, (localProgress, duration, text) => onChangeProgress(progress + (LOADING_VISUALIZATION_PROGRESS + localProgress * LOADING_SCENE_PROGRESS) * visualizationWeight, duration, text)), out Task sceneLoadingTask);
                    if (sceneLoadingTask.State == TaskState.Error)
                    {
                        visualization.Unload();
                        throw sceneLoadingTask.Exception;
                    }
                }
                else
                {
                    throw visualizationLoadingTask.Exception;
                }
                progress += visualizationWeight;
            }
            OnFinishedAddingNewScenes.Invoke();
        }
        /// <summary>
        /// Coroutine to load a visualization asynchronously
        /// </summary>
        /// <param name="visualization">Visualization to be loaded</param>
        /// <param name="onChangeProgress">Event to update the loading circle</param>
        /// <returns></returns>
        private static IEnumerator c_LoadScene(Visualization visualization, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;

            Exception exception = null;

            yield return Ninja.JumpToUnity;
            Base3DScene scene = Instantiate(m_Instance.m_ScenePrefab, m_Instance.m_ScenesParent).GetComponent<Base3DScene>();
            scene.Initialize(visualization);
            yield return CoroutineManager.StartAsync(scene.c_Initialize(visualization, onChangeProgress, (e) => exception = e));
            if (exception == null)
            {
                try
                {
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
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    exception = e;
                }
            }
            else
            {
                throw exception;
            }
        }

        private static IEnumerator c_Preload3D()
        {
            yield return Ninja.JumpToUnity;
            // Graphic Settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;

            // Advanced Conditions
            UI.Module3D.AdvancedSiteConditionStrings.LoadConditions();

            // Objects 3D
            yield return Ninja.JumpBack;
            Object3DManager.MNI.Load();
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo64) Object3DManager.DiFuMo.Load("64");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo128) Object3DManager.DiFuMo.Load("128");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo256) Object3DManager.DiFuMo.Load("256");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo512) Object3DManager.DiFuMo.Load("512");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo1024) Object3DManager.DiFuMo.Load("1024");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadIBC) Object3DManager.IBC.Load();
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadMarsAtlas) Object3DManager.MarsAtlas.Load();
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadJuBrain) Object3DManager.JuBrain.Load();
        }
        #endregion
    }
}