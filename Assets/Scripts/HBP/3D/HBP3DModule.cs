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
                return m_ScenesManager.SelectedScene.ColumnManager.SelectedColumn;
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
        /// Invoked whend we load a single patient scene from the mutli patients scene (params : id patient)
        /// </summary>   
        public GenericEvent<Data.Visualization.Visualization, Data.Patient> OnLoadSinglePatientSceneFromMultiPatientsScene = new GenericEvent<Data.Visualization.Visualization, Data.Patient>();
        /// <summary>
        /// Send the path of the saved ROI
        /// </summary>
        public GenericEvent<string> OnSaveRegionOfInterest = new GenericEvent<string>();
        /// <summary>
        /// Event called when a visualization is added
        /// </summary>
        public GenericEvent<Data.Visualization.Visualization> OnAddVisualization = new GenericEvent<Data.Visualization.Visualization>();
        /// <summary>
        /// Event called when a visualization is removed
        /// </summary>
        public GenericEvent<Data.Visualization.Visualization> OnRemoveVisualization = new GenericEvent<Data.Visualization.Visualization>();
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
        #endregion

        #region Private Methods
        void Awake()
        {
            // Scene Manager
            m_ScenesManager = transform.GetComponentInChildren<ScenesManager>();
            OnAddScene.AddListener((scene) =>
            {
                OnAddVisualization.Invoke(scene.Visualization);
            });
            OnRemoveScene.AddListener((scene) =>
            {
                OnRemoveVisualization.Invoke(scene.Visualization);
            });
            m_ScenesManager.OnSelectScene.AddListener((s) =>
            {
                UnityEngine.Debug.Log("OnSelectScene (Module3D)");
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
        /// <summary>
        /// Load a multi patients scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataMP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        private bool SetMultiPatientsVisualization(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress)
        {
            bool result = false;
            if (MNIObjects.LoadingMutex.WaitOne(10000))
            {
                result = m_ScenesManager.AddMultiPatientsScene(visualization, onChangeProgress);
            }

            if (!result)
            {
                UnityEngine.Debug.LogError("MNI loading data not finished.");
            }
            else
            {
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

            return result;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a visualization
        /// </summary>
        /// <param name="visualization"></param>
        /// <returns></returns>
        public void LoadVisualization(Data.Visualization.Visualization visualization)
        {
            this.StartCoroutineAsync(c_Load(visualization));
        }
        /// <summary>
        /// Remove a visualization and its associated scene
        /// </summary>
        /// <param name="visualization"></param>
        public void RemoveVisualization(Data.Visualization.Visualization visualization)
        {
            Base3DScene scene = m_ScenesManager.Scenes.ToList().Find(s => s.Visualization == visualization);
            m_ScenesManager.RemoveScene(scene);
            OnRemoveVisualization.Invoke(visualization);
        }
        #endregion

        #region Coroutines
        IEnumerator c_Load(Data.Visualization.Visualization visualization)
        {
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            GenericEvent<float, float, string> OnChangeLoadingProgress = new GenericEvent<float, float, string>();
            OnChangeLoadingProgress.AddListener((progress, time, message) => { loadingCircle.ChangePercentage(progress, time, message); UnityEngine.Debug.Log(progress + " " + message); });
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
            
            bool result = false;
            switch (visualization.ReferenceFrame)
            {
                case Data.Anatomy.ReferenceFrameType.Patient:
                    yield return Ninja.JumpToUnity;
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetSinglePatientVisualization(visualization, onChangeProgress));
                    yield return Ninja.JumpBack;
                    break;
                case Data.Anatomy.ReferenceFrameType.MNI:
                    result = SetMultiPatientsVisualization(visualization, onChangeProgress);
                    break;
                default:
                    break;
            }
            yield return Ninja.JumpToUnity;
            if (result)
            {
                OnAddVisualization.Invoke(visualization);
            }
        }
        IEnumerator c_SetSinglePatientVisualization(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress, bool postMRI = false)
        {
            yield return Ninja.JumpToUnity;
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(m_ScenesManager.c_AddSinglePatientScene(visualization, onChangeProgress, postMRI));
            yield return Ninja.JumpBack;
            // Add listeners to last added scene
            SinglePatient3DScene lastAddedScene = m_ScenesManager.Scenes.Last() as SinglePatient3DScene;
            lastAddedScene.Events.OnRequestSiteInformation.AddListener((siteRequest) =>
            {
                OnRequestSiteInformation.Invoke(siteRequest);
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