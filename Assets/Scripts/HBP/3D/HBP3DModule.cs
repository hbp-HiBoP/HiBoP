using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections;
using CielaSpike;
using Tools.Unity;

namespace HBP.Module3D
{
    /// <summary>
    /// Base class of the 3D module
    /// Used to control everything from the outside
    /// </summary>
    public class HBP3DModule : MonoBehaviour
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
        public Base3DScene SelectedScene
        {
            get
            {
                return m_Scenes.FirstOrDefault(s => s.IsSelected);
            }
        }
        /// <summary>
        /// Currently selected column
        /// </summary>
        public Column3D SelectedColumn
        {
            get
            {
                Base3DScene scene = SelectedScene;
                if (scene)
                {
                    return scene.ColumnManager.SelectedColumn;
                }
                return null;
            }
        }
        /// <summary>
        /// Currently selected view
        /// </summary>
        public View3D SelectedView
        {
            get
            {
                Column3D column = SelectedColumn;
                if (column)
                {
                    return column.SelectedView;
                }
                return null;
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
        public int NumberOfScenesLoadedSinceStart { get; set; }

        private List<Base3DScene> m_Scenes = new List<Base3DScene>();
        /// <summary>
        /// List of loaded scenes
        /// </summary>
        public ReadOnlyCollection<Base3DScene> Scenes
        {
            get
            {
                return new ReadOnlyCollection<Base3DScene>(m_Scenes);
            }
        }
        /// <summary>
        /// List of all the loaded visualizations
        /// </summary>
        public ReadOnlyCollection<Data.Visualization.Visualization> Visualizations
        {
            get
            {
                return new ReadOnlyCollection<Data.Visualization.Visualization>((from scene in Scenes select scene.Visualization).ToList());
            }
        }

        /// <summary>
        /// Mars atlas index (to get name of mars atlas, broadman etc)
        /// </summary>
        public DLL.MarsAtlasIndex MarsAtlasIndex { get; private set; }

        /// <summary>
        /// MNI Objects (Mesh and MRI)
        /// </summary>
        public MNIObjects MNIObjects;
        
        /// <summary>
        /// Shared directional light between all scenes
        /// </summary>
        public GameObject SharedDirectionalLight;
        /// <summary>
        /// Shared spotlight between all scenes
        /// </summary>
        public GameObject SharedSpotlight;

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
        [HideInInspector] public GenericEvent<SiteInfo> OnDisplaySiteInformation = new GenericEvent<SiteInfo>();
        /// <summary>
        /// Event called when a scene is added
        /// </summary>
        [HideInInspector] public GenericEvent<Base3DScene> OnAddScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when a scene is removed
        /// </summary>
        [HideInInspector] public GenericEvent<Base3DScene> OnRemoveScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called after all new scenes have been opened and initialized
        /// </summary>
        [HideInInspector] public UnityEvent OnFinishedAddingNewScenes = new UnityEvent();
        /// <summary>
        /// Event called when changing the selected scene
        /// </summary>
        [HideInInspector] public GenericEvent<Base3DScene> OnSelectScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when minimizing a scene
        /// </summary>
        [HideInInspector] public GenericEvent<Base3DScene> OnMinimizeScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when changing the selected column
        /// </summary>
        [HideInInspector] public GenericEvent<Column3D> OnSelectColumn = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when changing the selected view
        /// </summary>
        [HideInInspector] public GenericEvent<View3D> OnSelectView = new GenericEvent<View3D>();
        /// <summary>
        /// Event called when changing the value of the timeline of the selected column
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateSelectedColumnTimeLineID = new UnityEvent();
        /// <summary>
        /// Event called when the timeline is stopped because it reached the end
        /// </summary>
        [HideInInspector] public UnityEvent OnStopTimelinePlay = new UnityEvent();
        /// <summary>
        /// Event called when requesting an update in the UI
        /// </summary>
        [HideInInspector] public UnityEvent OnRequestUpdateInToolbar = new UnityEvent();
        #endregion

        #region Private Methods
        void Awake()
        {
            // Graphic Settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;

            // MarsAtlas index
            string dataDirectory = Application.dataPath + "/../Data/";
            #if UNITY_EDITOR
                dataDirectory = Application.dataPath + "/Data/";
            #endif
            MarsAtlasIndex = new DLL.MarsAtlasIndex(dataDirectory + "MarsAtlas/mars_atlas_index.csv");
        }
        void OnDestroy()
        {
            MarsAtlasIndex?.Dispose();
            ApplicationState.DLLDebugManager.clean();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a list of visualizations into 3D scenes
        /// </summary>
        /// <param name="visualizations">Visualizations to be loaded</param>
        public void LoadScenes(IEnumerable<Data.Visualization.Visualization> visualizations)
        {
            this.StartCoroutineAsync(c_Load(visualizations));
        }
        /// <summary>
        /// Remove every scenes corresponding to a visualization
        /// </summary>
        /// <param name="visualization">Visualization corresponding to the scenes to be removed</param>
        public void RemoveScene(Data.Visualization.Visualization visualization)
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
        public void RemoveScene(Base3DScene scene)
        {
            OnRemoveScene.Invoke(scene);
            m_Scenes.Remove(scene);
            this.StartCoroutine(scene.c_Destroy());
        }
        /// <summary>
        /// Load a single patient scene extracted from a visualization
        /// </summary>
        /// <param name="visualization">Visualization from which the new visualization will be extracted</param>
        /// <param name="patient">Patient of the new visualization</param>
        public void LoadSinglePatientSceneFromMultiPatientScene(Data.Visualization.Visualization visualization, Data.Patient patient)
        {
            Scenes.FirstOrDefault(s => s.Visualization == visualization).SaveConfiguration();
            Data.Visualization.Visualization visualizationToLoad = visualization.Clone() as Data.Visualization.Visualization;
            visualizationToLoad.Name = patient.Name;
            visualizationToLoad.RemoveAllPatients();
            visualizationToLoad.AddPatient(patient);
            if (patient.Brain.Meshes.FirstOrDefault(m => m.Name == ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMeshInSinglePatientVisualization) != null)
            {
                visualizationToLoad.Configuration.MeshName = ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMeshInSinglePatientVisualization;
            }
            else if (patient.Brain.Meshes.Count > 0)
            {
                visualizationToLoad.Configuration.MeshName = patient.Brain.Meshes.First().Name;
            }
            if (patient.Brain.MRIs.FirstOrDefault(m => m.Name == ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization) != null)
            {
                visualizationToLoad.Configuration.MRIName = ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization;
            }
            else if (patient.Brain.MRIs.Count > 0)
            {
                visualizationToLoad.Configuration.MRIName = patient.Brain.MRIs.First().Name;
            }
            if (patient.Brain.Implantations.FirstOrDefault(m => m.Name == ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedImplantationInSinglePatientVisualization) != null)
            {
                visualizationToLoad.Configuration.ImplantationName = ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedImplantationInSinglePatientVisualization;
            }
            else if (patient.Brain.Implantations.Count > 0)
            {
                visualizationToLoad.Configuration.ImplantationName = patient.Brain.Implantations.First().Name;
            }
            LoadScenes(new Data.Visualization.Visualization[] { visualizationToLoad });
        }
        /// <summary>
        /// Save all the configurations of the scenes
        /// </summary>
        public void SaveConfigurations()
        {
            foreach (var scene in Scenes)
            {
                scene.SaveConfiguration();
            }
        }
        /// <summary>
        /// Reload all scenes
        /// </summary>
        public void ReloadScenes()
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
        public void RemoveAllScenes()
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
        public IEnumerator c_Load(IEnumerable<Data.Visualization.Visualization> visualizations)
        {
            foreach (Data.Visualization.Visualization visualization in visualizations)
            {
                if (!visualization.IsVisualizable) throw new CanNotLoadVisualization(visualization.Name);

                yield return Ninja.JumpToUnity;
                LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
                GenericEvent<float, float, LoadingText> OnChangeLoadingProgress = new GenericEvent<float, float, LoadingText>();
                OnChangeLoadingProgress.AddListener((progress, time, message) => { loadingCircle.ChangePercentage(progress / 2.0f, time, message); });
                Task visualizationLoadingTask;
                yield return this.StartCoroutineAsync(visualization.c_Load(OnChangeLoadingProgress), out visualizationLoadingTask);
                switch (visualizationLoadingTask.State)
                {
                    case TaskState.Done:
                        break;
                    case TaskState.Error:
                        visualization.Unload();
                        Exception exception = visualizationLoadingTask.Exception;
                        ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                        break;
                }
                if (visualizationLoadingTask.State == TaskState.Done)
                {
                    Task sceneLoadingTask;
                    yield return this.StartCoroutineAsync(c_LoadScene(visualization, OnChangeLoadingProgress), out sceneLoadingTask);
                    switch (sceneLoadingTask.State)
                    {
                        case TaskState.Done:
                            yield return new WaitForSeconds(0.5f);
                            break;
                        case TaskState.Error:
                            visualization.Unload();
                            Exception exception = sceneLoadingTask.Exception;
                            ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                            break;
                    }
                }
                loadingCircle.Close();
            }
            OnFinishedAddingNewScenes.Invoke();
        }
        /// <summary>
        /// Coroutine to load a visualization asynchronously
        /// </summary>
        /// <param name="visualization">Visualization to be loaded</param>
        /// <param name="onChangeProgress">Event to update the loading circle</param>
        /// <returns></returns>
        IEnumerator c_LoadScene(Data.Visualization.Visualization visualization, GenericEvent<float, float, LoadingText> onChangeProgress = null)
        {
            if (onChangeProgress == null) onChangeProgress = new GenericEvent<float, float, LoadingText>();

            Exception exception = null;

            yield return Ninja.JumpToUnity;
            Base3DScene scene = Instantiate(m_ScenePrefab, m_ScenesParent).GetComponent<Base3DScene>();
            scene.Initialize(visualization);
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(scene.c_Initialize(visualization, onChangeProgress, (e) => exception = e));
            if (exception == null)
            {
                try
                {
                    // Add the listeners
                    scene.OnChangeSelectedState.AddListener((selected) =>
                    {
                        if (selected)
                        {
                            foreach (Base3DScene s in m_Scenes)
                            {
                                if (s != scene)
                                {
                                    s.IsSelected = false;
                                }
                            }
                        }
                    });
                    // Add the scene to the list
                    m_Scenes.Add(scene);
                    scene.FinalizeInitialization();
                    ApplicationState.Module3D.OnAddScene.Invoke(scene);
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
        #endregion
    }
}