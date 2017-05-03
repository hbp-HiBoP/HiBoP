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
    namespace Events
    {
        /// <summary>
        /// Event called when an IEGG column minimized state has changed (params : spScene, IEEGColumnsMinimizedStates)
        /// </summary>
        [System.Serializable]
        public class UpdateColumnMinimizeStateEvent : UnityEvent<bool, List<bool>> { }
        public class OnAddVisualisation : UnityEvent<Data.Visualisation.Visualisation> { }
        public class OnRemoveVisualisation : UnityEvent<Data.Visualisation.Visualisation> { }
    }

    /// <summary>
    /// Interface class for controling the 3D module. Never uses other GameObject than this one from outside of the module
    /// </summary>
    public class HBP3DModule : MonoBehaviour
    {
        #region Properties
        private ScenesManager m_ScenesManager; /**< scenes manager */
        public ScenesManager ScenesManager { get { return m_ScenesManager; } }

        public ReadOnlyCollection<Data.Visualisation.Visualisation> Visualisations
        {
            get
            {
                return new ReadOnlyCollection<Data.Visualisation.Visualisation>((from scene in ScenesManager.Scenes select scene.Visualisation).ToList());
            }
        }

        [Header("Module camera")]
        [SerializeField]
        Camera m_backgroundCamera = null;
        public Camera BackgroundCamera { get { return m_backgroundCamera; } }

        [Header("API events")]
        public Events.UpdateColumnMinimizeStateEvent UpdateColumnMinimizedState = new Events.UpdateColumnMinimizeStateEvent();
        public Events.SiteInfoRequest SiteInfoRequest = new Events.SiteInfoRequest();        
        public Events.LoadSPSceneFromMP LoadSPSceneFromMP = new Events.LoadSPSceneFromMP();
        public Events.ROISavedEvent ROISavedEvent = new Events.ROISavedEvent();
        public Events.OnAddVisualisation OnAddVisualisation = new Events.OnAddVisualisation();
        public Events.OnRemoveVisualisation OnRemoveVisualisation = new Events.OnRemoveVisualisation();
        #endregion

        #region Private Methods
        void Awake()
        {
            // command listeners
            //MinimizeController minimizeController =  UIManager.OverlayManager.MinimizeController;
            //minimizeController.m_minimizeStateSwitchEvent.AddListener((spScene) =>
            //{
            //    if (spScene)
            //        UpdateColumnMinimizedState.Invoke(true, minimizeController.SPMinimizeStateList);
            //    else
            //        UpdateColumnMinimizedState.Invoke(false, minimizeController.MPMinimizeStateList);
            //});
            //UIManager.MenuManager.transform.FindChild("Left").FindChild("mp left menu list").FindChild("ROI").GetComponent<ROIMenuController>().ROISavedEvent.AddListener((pathROI) =>
            //{
            //    ROISavedEvent.Invoke(pathROI);
            //    UnityEngine.Debug.Log("pathROI : " + pathROI);
            //});
            ScenesManager.OnAddScene.AddListener((scene) =>
            {
                OnAddVisualisation.Invoke(scene.Visualisation);
            });
            ScenesManager.OnRemoveScene.AddListener((scene) =>
            {
                OnRemoveVisualisation.Invoke(scene.Visualisation);
            });

            // retrieve managers
            //m_UIManager = GameObject.Find("Brain Visualisation").GetComponent<UIManager>();
            m_ScenesManager = transform.Find("Scenes").GetComponent<ScenesManager>();

            // graphics settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;
        }
        void Start()
        {
            // initialization
            //m_UIManager.Initialize(m_ScenesManager);

            // define listeners
            m_ScenesManager.SendModeSpecifications.AddListener((specs) =>
            {
                // set UI overlay specs
                UnityEngine.Profiling.Profiler.BeginSample("TEST-HiBoP_3DModule_Main setSpecificOverlayActive 1");
                for (int ii = 0; ii < specs.uiOverlayMask.Count; ++ii) { }
                    //m_UIManager.OverlayManager.set_specific_overlay_active(specs.uiOverlayMask[ii], ii, specs.mode);
                UnityEngine.Profiling.Profiler.EndSample();

                // set UI camera specs                
                UnityEngine.Profiling.Profiler.BeginSample("TEST-HiBoP_3DModule_Main update_UI_with_mode 1");
                //m_UIManager.MenuManager.update_UI_with_mode(specs.mode);
                UnityEngine.Profiling.Profiler.EndSample();
            });

        }
        void OnDestroy()
        {
            StaticComponents.DLLDebugManager.clean();
        }
        /// <summary>
        /// Load a new single patient scene
        /// </summary>
        /// <param name="visualisation"></param>
        /// <returns></returns>
        private bool SetSinglePatientSceneData(Data.Visualisation.SinglePatientVisualisation visualisation, bool postIRM = false)
        {
            bool success = m_ScenesManager.AddSinglePatientScene(visualisation, postIRM);
            if (!success)
            {
                UnityEngine.Debug.LogError("-ERROR : Failed to add single patient scene");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Load a new multi patients scene
        /// </summary>
        /// <param name="data"></param>
        private bool SetMultiPatientsSceneData(Data.Visualisation.MultiPatientsVisualisation visualisation)
        {
            bool success = m_ScenesManager.AddMultiPatientsScene(visualisation);
            if (!success)
            {
                UnityEngine.Debug.LogError("-ERROR : Failed to add multi patients scene");
                return false;
            }
            return true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the focus state of the module
        /// </summary>
        /// <param name="state"></param>
        public void SetModuleFocus(bool state)
        {
            ScenesManager.SetModuleFocusState(state);
        }
        /// <summary>
        /// Set the visibility ratio between the single patient scene and the multi patients scene, if the two parameters are true or false, the two scenes will be displayed.
        /// </summary>
        /// <param name="showSPScene"> if true display single patient scene</param>
        /// <param name="showMPScene"> if true display multi patients scene</param>
        public void SetScenesVisibility(bool showSPScene = true, bool showMPScene = true) // DELETEME maybe
        {
            ScenesManager.SetScenesVisibility(showSPScene, showMPScene);
        }
        /// <summary>
        /// Load a single patient scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataSP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        public bool AddVisualisation(Data.Visualisation.SinglePatientVisualisation visualisation)
        {
            bool result = SetSinglePatientSceneData(visualisation);
            // Add listeners to last added scene
            SinglePatient3DScene lastAddedScene = ScenesManager.Scenes.Last() as SinglePatient3DScene;
            lastAddedScene.SiteInfoRequest.AddListener((siteRequest) =>
            {
                SiteInfoRequest.Invoke(siteRequest);
            });
            return result;
        }
        /// <summary>
        /// Load a multi patients scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataMP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        public bool AddVisualisation(Data.Visualisation.MultiPatientsVisualisation visualisation)
        {
            bool result = false;
            if (MNIObjects.LoadingMutex.WaitOne(10000))
                result = SetMultiPatientsSceneData(visualisation);

            if (!result)
            {
                UnityEngine.Debug.LogError("MNI loading data not finished. ");
                return false;
            }
            // Add listener to last added scene
            MultiPatients3DScene lastAddedScene = ScenesManager.Scenes.Last() as MultiPatients3DScene;
            lastAddedScene.SiteInfoRequest.AddListener((siteRequest) =>
            {
                SiteInfoRequest.Invoke(siteRequest);
            });
            lastAddedScene.LoadSPSceneFromMP.AddListener((idPatient) =>
            {
                LoadSPSceneFromMP.Invoke(idPatient);
            });
            
            return true;
        }
        /// <summary>
        /// Remove a visualisation and its associated scene
        /// </summary>
        /// <param name="visualisation"></param>
        public void RemoveVisualisation(Data.Visualisation.Visualisation visualisation)
        {
            Base3DScene scene = m_ScenesManager.Scenes.ToList().Find(s => s.Visualisation == visualisation);
            m_ScenesManager.RemoveScene(scene);
        }
        /// <summary>
        /// Define the ratio between the two scenes
        /// </summary>
        /// <param name="ratio"></param>
        public void SetSceneRatio(float ratio) // DELETEME
        {
            bool isSPSceneDisplayed = (ratio > 0.2f);
            bool isMPSceneDisplayed = (ratio < 0.8f);

            //m_UIManager.OverlayManager.set_overlay_scene_visibility(isSPSceneDisplayed, SceneType.SinglePatient);
            //m_UIManager.OverlayManager.set_overlay_scene_visibility(isMPSceneDisplayed, SceneType.MultiPatients);
            /*
            m_ScenesManager.SPPanel.transform.parent.gameObject.SetActive(isSPSceneDisplayed);
            m_ScenesManager.MPPanel.transform.parent.gameObject.SetActive(isMPSceneDisplayed);

            m_ScenesManager.SPCameras.gameObject.SetActive(isSPSceneDisplayed);
            m_ScenesManager.MPCameras.gameObject.SetActive(isMPSceneDisplayed);

            m_ScenesManager.SPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = ratio;
            m_ScenesManager.MPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1f - ratio;
            */
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

            GameObject.Find("HiBoP 3DModule API").transform.Find("Module").gameObject.GetComponent<HiBoP_PatientLoader_Debug>().load_debug_config_file(Application.dataPath + "/../tools/config_debug_load.txt");
        }        

        [MenuItem("Debug test/Focus on single patient scene")]
        static void FocusOnSinglePatientScene()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }

            GameObject.Find("HiBoP 3DModule API").GetComponent<HBP3DModule>().SetScenesVisibility(true, false);
        }

        [MenuItem("Debug test/Focus on multi-patients scene")]
        static void FocusOnMultiPatientsScene()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }

            GameObject.Find("HiBoP 3DModule API").GetComponent<HBP3DModule>().SetScenesVisibility(false, true);
        }

        [MenuItem("Debug test/Focus on both scenes")]
        static void FocusOnBothScenes()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }

            GameObject.Find("HiBoP 3DModule API").GetComponent<HBP3DModule>().SetScenesVisibility(true, true);
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