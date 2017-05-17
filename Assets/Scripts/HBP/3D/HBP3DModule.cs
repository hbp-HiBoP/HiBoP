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
        /// List of all the loaded visualizations
        /// </summary>
        public ReadOnlyCollection<Data.Visualization.Visualization> Visualizations
        {
            get
            {
                return new ReadOnlyCollection<Data.Visualization.Visualization>((from scene in ScenesManager.Scenes select scene.Visualization).ToList());
            }
        }
        
        /// <summary>
        /// Event called when an IEGG column minimized state has changed (params : spScene, IEEGColumnsMinimizedStates)
        /// </summary>
        public GenericEvent<bool, List<bool>> OnMinimizeColumn = new GenericEvent<bool, List<bool>>();
        /// <summary>
        /// UI event for sending a plot info request to the outside UI (params : plotRequest)
        /// </summary>
        public GenericEvent<SiteRequest> OnRequestSiteInformation = new GenericEvent<SiteRequest>();
        /// <summary>
        /// Invoked whend we load a single patient scene from the mutli patients scene (params : id patient)
        /// </summary>   
        public GenericEvent<Data.Visualization.MultiPatientsVisualization, Data.Patient> OnLoadSinglePatientSceneFromMultiPatientsScene = new GenericEvent<Data.Visualization.MultiPatientsVisualization, Data.Patient>();
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
        #endregion

        #region Private Methods
        void Awake()
        {
            // Scene Manager
            m_ScenesManager = transform.GetComponentInChildren<ScenesManager>();
            ScenesManager.OnAddScene.AddListener((scene) =>
            {
                OnAddVisualization.Invoke(scene.Visualization);
            });
            ScenesManager.OnRemoveScene.AddListener((scene) =>
            {
                OnRemoveVisualization.Invoke(scene.Visualization);
            });

            // Graphic Settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;
        }
        void OnDestroy()
        {
            ApplicationState.DLLDebugManager.clean();
        }
        /// <summary>
        /// Load a single patient scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataSP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        private bool SetVisualization(Data.Visualization.SinglePatientVisualization visualization)
        {
            bool result = SetSinglePatientSceneData(visualization);
            if (result)
            {
                // Add listeners to last added scene
                SinglePatient3DScene lastAddedScene = ScenesManager.Scenes.Last() as SinglePatient3DScene;
                lastAddedScene.OnRequestSiteInformation.AddListener((siteRequest) =>
                {
                    OnRequestSiteInformation.Invoke(siteRequest);
                });
            }
            return result;
        }
        /// <summary>
        /// Load a multi patients scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataMP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        private bool SetVisualization(Data.Visualization.MultiPatientsVisualization visualization)
        {
            bool result = false;
            if (MNIObjects.LoadingMutex.WaitOne(10000))
            {
                result = SetMultiPatientsSceneData(visualization);
            }

            if (!result)
            {
                UnityEngine.Debug.LogError("MNI loading data not finished.");
            }
            else
            {
                // Add listener to last added scene
                MultiPatients3DScene lastAddedScene = ScenesManager.Scenes.Last() as MultiPatients3DScene;
                lastAddedScene.OnRequestSiteInformation.AddListener((siteRequest) =>
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
        /// <summary>
        /// Load a new single patient scene
        /// </summary>
        /// <param name="visualization"></param>
        /// <returns></returns>
        private bool SetSinglePatientSceneData(Data.Visualization.SinglePatientVisualization visualization, bool postIRM = false)
        {
            bool success = m_ScenesManager.AddSinglePatientScene(visualization, postIRM);
            if (!success)
            {
                UnityEngine.Debug.LogError("-ERROR : Failed to add single patient scene");
            }
            return success;
        }
        /// <summary>
        /// Load a new multi patients scene
        /// </summary>
        /// <param name="data"></param>
        private bool SetMultiPatientsSceneData(Data.Visualization.MultiPatientsVisualization visualization)
        {
            bool success = m_ScenesManager.AddMultiPatientsScene(visualization);
            if (!success)
            {
                UnityEngine.Debug.LogError("-ERROR : Failed to add multi patients scene");
            }
            return success;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a visualization
        /// </summary>
        /// <param name="visualization"></param>
        /// <returns></returns>
        public bool AddVisualization(Data.Visualization.Visualization visualization)
        {
            bool result = false;
            if (visualization is Data.Visualization.SinglePatientVisualization)
            {
                result = SetVisualization(visualization as Data.Visualization.SinglePatientVisualization);
            }
            else if (visualization is Data.Visualization.MultiPatientsVisualization)
            {
                result = SetVisualization(visualization as Data.Visualization.MultiPatientsVisualization);
            }
            if (result)
            {
                OnAddVisualization.Invoke(visualization);
            }
            return result;
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