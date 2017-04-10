/**
 * \file    HiBoP_3DModule_API.cs
 * \author  Lance Florian and Adrien Gannerie
 * \date    01/2016 - 04/2017
 * \brief   Define the HiBoP_3DModule_API class
 */

using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HBP.VISU3D
{
    namespace Events
    {
        /// <summary>
        /// Event called when an IEGG column minimized state has changed (params : spScene, IEEGColumnsMinimizedStates)
        /// </summary>
        [System.Serializable]
        public class UpdateColumnMinimizeStateEvent : UnityEvent<bool, List<bool>> { }
    }

    /// <summary>
    /// Interface class for controling the 3D module. Never uses other GameObject than this one from outside of the module
    /// </summary>
    public class HiBoP_3DModule_API : MonoBehaviour
    {
        #region Properties
        private HiBoP_3DModule_Main m_HBP3D = null; /**< main class of the 3D module */

        [Header("Module camera")]
        [SerializeField]
        Camera m_backgroundCamera = null;
        public Camera BackgroundCamera { get { return m_backgroundCamera; } }

        [Header("API events")]
        public Events.UpdateColumnMinimizeStateEvent UpdateColumnMinimizedState = new Events.UpdateColumnMinimizeStateEvent();
        public Events.InfoPlotRequest SiteInfoRequest = new Events.InfoPlotRequest();        
        public Events.LoadSPSceneFromMP LoadSPSceneFromMP = new Events.LoadSPSceneFromMP();
        public Events.ROISavedEvent ROISavedEvent = new Events.ROISavedEvent();
        #endregion 

        #region Private Methods

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve
            m_HBP3D = GetComponent<HiBoP_3DModule_Main>();

            // command listeners            
            MinimizeController minimizeController = m_HBP3D.UIManager.UIOverlayManager.MinimizeController;
            minimizeController.m_minimizeStateSwitchEvent.AddListener((spScene) =>
            {
                if (spScene)
                    UpdateColumnMinimizedState.Invoke(true, minimizeController.SPMinimizeStateList);
                else
                    UpdateColumnMinimizedState.Invoke(false, minimizeController.MPMinimizeStateList);
            });
            m_HBP3D.ScenesManager.SPScene.PlotInfoRequest.AddListener((siteRequest) =>
            {
                SiteInfoRequest.Invoke(siteRequest);
            });
            m_HBP3D.ScenesManager.MPScene.PlotInfoRequest.AddListener((siteRequest) =>
            {
                SiteInfoRequest.Invoke(siteRequest);
            });
            m_HBP3D.ScenesManager.MPScene.LoadSPSceneFromMP.AddListener((idPatient) =>
            {
                LoadSPSceneFromMP.Invoke(idPatient);
            });
            m_HBP3D.UIManager.UICameraManager.transform.Find("mp left menues").Find("ROI").GetComponent<ROIMenuController>().ROISavedEvent.AddListener((pathROI) =>
            {
                ROISavedEvent.Invoke(pathROI);
                UnityEngine.Debug.Log("pathROI : " + pathROI);
            });
        }
        void OnDestroy()
        {
            StaticComponents.DLLDebugManager.clean();
        }
        #endregion

        #region Public Methods
        // ################################################### API public functions
        /// <summary>
        /// Set the focus state of the module
        /// </summary>
        /// <param name="state"></param>
        public void SetModuleFocus(bool state)
        {
            m_HBP3D.ScenesManager.SetModuleFocusState(state);
        }

        /// <summary>
        /// Set the visibility ratio between the single patient scene and the multi patients scene, if the two parameters are true or false, the two scenes will be displayed.
        /// </summary>
        /// <param name="showSPScene"> if true display single patient scene</param>
        /// <param name="showMPScene"> if true display multi patients scene</param>
        public void SetScenesVisibility(bool showSPScene = true, bool showMPScene = true)
        {
            m_HBP3D.ScenesManager.setScenesVisibility(showSPScene, showMPScene);
        }

        /// <summary>
        /// Load a single patient scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataSP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        public bool LoadData(Data.Visualisation.SinglePatientVisualisationData visuDataSP)
        {
            bool result = m_HBP3D.set_SP_data(visuDataSP);
            return result;
        }

        /// <summary>
        /// Load a multi patients scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataMP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        public bool LoadData(Data.Visualisation.MultiPatientsVisualisationData visuDataMP)
        {
            if (MNIObjects.LoadingMutex.WaitOne(10000))
                return m_HBP3D.set_MP_data(visuDataMP);

            UnityEngine.Debug.LogError("MNI loading data not finished. ");

            return false;
        }
        #endregion
    }

#if UNITY_EDITOR
    public class EditorMenuActions
    {
        [MenuItem("Before building/Copy data to build directory")]
        static void CoptyDataToBuildDirectory()
        {
            string buildDataPath = VISU3D.DLL.QtGUI.get_existing_directory_name("Select Build directory where data will be copied");
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

            GameObject.Find("HiBoP 3DModule API").GetComponent<HiBoP_3DModule_API>().SetScenesVisibility(true, false);
        }

        [MenuItem("Debug test/Focus on multi-patients scene")]
        static void FocusOnMultiPatientsScene()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }

            GameObject.Find("HiBoP 3DModule API").GetComponent<HiBoP_3DModule_API>().SetScenesVisibility(false, true);
        }

        [MenuItem("Debug test/Focus on both scenes")]
        static void FocusOnBothScenes()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.Log("Only in play mode.");
                return;
            }

            GameObject.Find("HiBoP 3DModule API").GetComponent<HiBoP_3DModule_API>().SetScenesVisibility(true, true);
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