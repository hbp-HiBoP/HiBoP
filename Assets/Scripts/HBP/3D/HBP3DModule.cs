/**
 * \file    HiBoP_3DModule_API.cs
 * \author  Lance Florian and Adrien Gannerie
 * \date    01/2016 - 04/2017
 * \brief   Define the HiBoP_3DModule_API class
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.UI.Module3D;
using System.Linq;
using System.Collections;
using CielaSpike;
using Tools.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HBP.Module3D
{
    /// <summary>
    /// Interface class for controling the 3D module. Never uses other GameObject than this one from outside of the module
    /// </summary>
    public class HBP3DModule : MonoBehaviour
    {
        #region Properties
        [SerializeField, Candlelight.PropertyBackingField]
        private ScenesManager m_ScenesManager;
        /// <summary>
        /// Reference to the scene manager
        /// </summary>
        public ScenesManager ScenesManager
        {
            get { return m_ScenesManager; }
            set { m_ScenesManager = value; }
        }

        /// <summary>
        /// Current selected scene
        /// </summary>
        public Base3DScene SelectedScene
        {
            get
            {
                return m_ScenesManager.SelectedScene;
            }
        }
        /// <summary>
        /// Current selected column
        /// </summary>
        public Column3D SelectedColumn
        {
            get
            {
                if (m_ScenesManager.SelectedScene)
                {
                    return m_ScenesManager.SelectedScene.ColumnManager.SelectedColumn;
                }
                else return null;
            }
        }
        /// <summary>
        /// Current selected view
        /// </summary>
        public View3D SelectedView
        {
            get
            {
                return m_ScenesManager.SelectedScene.ColumnManager.SelectedColumn.SelectedView;
            }
        }

        public const int MAXIMUM_VIEW_NUMBER = 5;
        public const int MAXIMUM_COLUMN_NUMBER = 5;
        /// <summary>
        /// Space between scenes in world space
        /// </summary>
        public const int SPACE_BETWEEN_SCENES_AND_COLUMNS = 3000;

        /// <summary>
        /// Number of scenes that have been loaded in this instance of HiBoP (to apply a unique ID to each scene)
        /// </summary>
        public int NumberOfScenesLoadedSinceStart { get; set; }

        /// <summary>
        /// Number of columns that have been instanciated in this instance of HiBoP (to apply a unique ID to each column)
        /// </summary>
        public int NumberOfColumnsSinceStart { get; set; }

        /// <summary>
        /// List of all the loaded visualizations
        /// </summary>
        public ReadOnlyCollection<Data.Visualization.Visualization> Visualizations
        {
            get
            {
                return new ReadOnlyCollection<Data.Visualization.Visualization>((from scene in m_ScenesManager.Scenes select scene.Visualization).ToList());
            }
        }

        /// <summary>
        /// Mars atlas index (to get name of mars atlas, broadman etc)
        /// </summary>
        public DLL.MarsAtlasIndex MarsAtlasIndex;

        /// <summary>
        /// Event called when an IEGG column minimized state has changed (params : spScene, IEEGColumnsMinimizedStates)
        /// </summary>
        public GenericEvent<bool, List<bool>> OnMinimizeColumn = new GenericEvent<bool, List<bool>>();
        /// <summary>
        /// UI event for sending a plot info request to the outside UI (params : plotRequest)
        /// </summary>
        public GenericEvent<SiteRequest> OnRequestSiteInformation = new GenericEvent<SiteRequest>();
        /// <summary>
        /// Event called when hovering a site to display its information
        /// </summary>
        public GenericEvent<SiteInfo> OnDisplaySiteInformation = new GenericEvent<SiteInfo>();
        /// <summary>
        /// Event called when changing the value of the timeline of the selected column
        /// </summary>
        public UnityEvent OnUpdateSelectedColumnTimeLineID = new UnityEvent();
        /// <summary>
        /// Invoked whend we load a single patient scene from the mutli patients scene (params : id patient)
        /// </summary>   
        public GenericEvent<Data.Visualization.Visualization, Data.Patient> OnLoadSinglePatientSceneFromMultiPatientsScene = new GenericEvent<Data.Visualization.Visualization, Data.Patient>();
        /// <summary>
        /// Send the path of the saved ROI
        /// </summary>
        public GenericEvent<string> OnSaveRegionOfInterest = new GenericEvent<string>();
        /// <summary>
        /// Event called when adding or removing a ROI
        /// </summary>
        public UnityEvent OnChangeNumberOfROI = new UnityEvent();
        /// <summary>
        /// Event called when adding or removing a bubble in a ROI
        /// </summary>
        public UnityEvent OnChangeNumberOfVolumeInROI = new UnityEvent();
        /// <summary>
        /// Event called when selecting a ROI
        /// </summary>
        public UnityEvent OnSelectROI = new UnityEvent();
        /// <summary>
        /// Event called when selecting a volume of a ROI
        /// </summary>
        public UnityEvent OnSelectROIVolume = new UnityEvent();
        /// <summary>
        /// Event called when changing the radius of a volume of a ROI
        /// </summary>
        public UnityEvent OnChangeROIVolumeRadius = new UnityEvent();
        /// <summary>
        /// Event called when a scene is added
        /// </summary>
        public GenericEvent<Base3DScene> OnAddScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when a scene is removed
        /// </summary>
        public GenericEvent<Base3DScene> OnRemoveScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when changing the selected scene
        /// </summary>
        public GenericEvent<Base3DScene> OnSelectScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when changing the selected column
        /// </summary>
        public GenericEvent<Column3D> OnSelectColumn = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when changing the selected view
        /// </summary>
        public GenericEvent<View3D> OnSelectView = new GenericEvent<View3D>();
        /// <summary>
        /// Event called when changing the selected site
        /// </summary>
        public GenericEvent<Site> OnSelectSite = new GenericEvent<Site>();
        /// <summary>
        /// Event called when updating the invisible part of the brain (erasing triangles, reset ...)
        /// </summary>
        public UnityEvent OnModifyInvisiblePart = new UnityEvent();
        /// <summary>
        /// Event called when the timeline is stopped because it reached the end
        /// </summary>
        public UnityEvent OnStopTimelinePlay = new UnityEvent();
        /// <summary>
        /// Event called when requesting an update in the UI
        /// </summary>
        public UnityEvent OnRequestUpdateInUI = new UnityEvent();
        #endregion

        #region Private Methods
        void Awake()
        {
            // Scene Manager
            m_ScenesManager = transform.GetComponentInChildren<ScenesManager>();
            m_ScenesManager.OnSelectScene.AddListener((s) =>
            {
                OnSelectScene.Invoke(s);
            });

            // Graphic Settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;

            // MarsAtlas index
            string dataDirectory = Application.dataPath + "/../Data/";
            #if UNITY_EDITOR
                dataDirectory = Application.dataPath + "/Data/";
            #endif

            MarsAtlasIndex = new DLL.MarsAtlasIndex();
            if (!MarsAtlasIndex.LoadMarsAtlasIndexFile(dataDirectory + "MarsAtlas/mars_atlas_index.csv"))
            {
                UnityEngine.Debug.LogError("Can't load mars atlas index.");
            }
        }
        void OnDestroy()
        {
            ApplicationState.DLLDebugManager.clean();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a visualization
        /// </summary>
        /// <param name="visualization"></param>
        /// <returns></returns>
        public void LoadScene(Data.Visualization.Visualization visualization)
        {
            this.StartCoroutineAsync(c_Load(visualization));
        }
        /// <summary>
        /// Remove a visualization and its associated scene
        /// </summary>
        /// <param name="visualization"></param>
        public void RemoveScene(Data.Visualization.Visualization visualization)
        {
            Base3DScene scene = m_ScenesManager.Scenes.ToList().Find(s => s.Visualization == visualization);
            m_ScenesManager.RemoveScene(scene);
        }
        /// <summary>
        /// Remove a visualization and its associated scene
        /// </summary>
        /// <param name="visualization"></param>
        public void RemoveScene(Base3DScene scene)
        {
            m_ScenesManager.RemoveScene(scene);
        }
        #endregion

        #region Coroutines
        IEnumerator c_Load(Data.Visualization.Visualization visualization)
        {
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            GenericEvent<float, float, string> OnChangeLoadingProgress = new GenericEvent<float, float, string>();
            OnChangeLoadingProgress.AddListener((progress, time, message) => { loadingCircle.ChangePercentage(progress, time, message); });
            Task visualizationLoadingTask;
            yield return this.StartCoroutineAsync(visualization.c_Load(OnChangeLoadingProgress), out visualizationLoadingTask);
            switch (visualizationLoadingTask.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case TaskState.Error:
                    Exception exception = visualizationLoadingTask.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    break;
            }
            Task sceneLoadingTask;
            yield return this.StartCoroutineAsync(c_LoadScene(visualization, OnChangeLoadingProgress), out sceneLoadingTask);
            switch (sceneLoadingTask.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case TaskState.Error:
                    Exception exception = sceneLoadingTask.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    break;
            }
            loadingCircle.Close();
        }
        IEnumerator c_LoadScene(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress = null)
        {
            if (onChangeProgress == null) onChangeProgress = new GenericEvent<float, float, string>();
            
            yield return Ninja.JumpToUnity;
            switch (visualization.ReferenceFrame)
            {
                case Data.Anatomy.ReferenceFrameType.Patient:
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetSinglePatientVisualization(visualization, onChangeProgress));
                    break;
                case Data.Anatomy.ReferenceFrameType.MNI:
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetMultiPatientsVisualization(visualization, onChangeProgress));
                    break;
                default:
                    break;
            }
        }
        IEnumerator c_SetSinglePatientVisualization(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress, bool postMRI = false)
        {
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(m_ScenesManager.c_AddSinglePatientScene(visualization, onChangeProgress, postMRI));
            // Add listeners to last added scene
            SinglePatient3DScene lastAddedScene = m_ScenesManager.Scenes.Last() as SinglePatient3DScene;
            lastAddedScene.Events.OnRequestSiteInformation.AddListener((siteRequest) =>
            {
                OnRequestSiteInformation.Invoke(siteRequest);
            });
        }
        IEnumerator c_SetMultiPatientsVisualization(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(m_ScenesManager.c_AddMultiPatientsScene(visualization, onChangeProgress));
            bool result = false;
            // Add listener to last added scene
            MultiPatients3DScene lastAddedScene = m_ScenesManager.Scenes.Last() as MultiPatients3DScene;
            lastAddedScene.Events.OnRequestSiteInformation.AddListener((siteRequest) =>
            {
                OnRequestSiteInformation.Invoke(siteRequest);
            });
            lastAddedScene.OnLoadSinglePatientSceneFromMultiPatientsScene.AddListener((visu, patient) =>
            {
                OnLoadSinglePatientSceneFromMultiPatientsScene.Invoke(visu, patient);
            });
        }
        #endregion
    }

#if UNITY_EDITOR
    public class EditorMenuActions
    {
        [MenuItem("Before building/Copy data to build directory")]
        static void CoptyDataToBuildDirectory()
        {
            string buildDataPath = Module3D.DLL.QtGUI.get_existing_directory_name("Select Build directory where data will be copied");
            if (buildDataPath.Length > 0)
            {
                FileUtil.DeleteFileOrDirectory(buildDataPath + "/Data");
                FileUtil.CopyFileOrDirectory(GlobalPaths.Data, buildDataPath + "/Data");
            }            
        }

        [MenuItem("Debug test/Load patient from debug launcher")]
        static void LoadPatientFromDebugLauncher()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }
        }        

        [MenuItem("Debug test/Focus on single patient scene")]
        static void FocusOnSinglePatientScene()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }
        }

        [MenuItem("Debug test/Focus on multi-patients scene")]
        static void FocusOnMultiPatientsScene()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }
        }

        [MenuItem("Debug test/Focus on both scenes")]
        static void FocusOnBothScenes()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }
        }

        [MenuItem("Debug test/Launch hibop debug launcher editor")]
        static void OpenDebugLauncherEditor()
        {
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = Application.dataPath + "/../tools/HiBoP_Tools.exe";
            process.StartInfo.Arguments = "EditorLauncher";
            process.Start();
        }

        [MenuItem("Debug test/File dialog")]
        static void OpenDebugFileDialogEditor()
        {
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Application.dataPath + "/../tools/HiBoP_Tools.exe",
                        Arguments = "FileDialog get_existing_file_names \"message test\" \"Images files (*.jpg *.png)\"", // TODO
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
            }
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Application.dataPath + "/../tools/HiBoP_Tools.exe",
                        Arguments = "FileDialog get_existing_file_name \"message test\" \"MRI files (*.nii)\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
            }
        }
    }
#endif
}