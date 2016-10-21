
/**
 * \file    HBP_3DModule_Command.cs
 * \author  Lance Florian
 * \date    01/2016
 * \brief   Define the HBP_3DModule_Command class
 */

// system
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// unity
using UnityEngine;
using UnityEngine.Events;

using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
    using UnityEditor.Callbacks;
#endif

namespace HBP.VISU3D
{
#if UNITY_EDITOR
    public class EditorMenuActions
    {
        [MenuItem("Before building/Copy data to build dir")]
        static void copyData()
        {
            if (!EditorApplication.isPlaying)
            { 
                Debug.Log("Only in play mode.");
                return;
            }                                                      

            Debug.Log("copyData");
            string buildDataPath = DLL.QtGUI.getExistingDirectory("Select Build directory where data will be copied");

            if (buildDataPath.Length > 0)
            {
                FileUtil.DeleteFileOrDirectory(buildDataPath + "/Data");
                FileUtil.CopyFileOrDirectory(Application.dataPath + "/Data", buildDataPath + "/Data");
            }
        }

        [MenuItem("Before building/Copy dll to build dir")]
        static void copyDLL()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("Only in play mode.");
                return;
            }

            string buildDataPath = DLL.QtGUI.getExistingDirectory("Select Build directory where DLL will be copied");

            if (buildDataPath.Length > 0)
            {
                List<string> dllList = new List<string>();

                // hbp
                dllList.Add("hbp_export.dll");

                // ELAN
                dllList.Add("EEG.dll");
                dllList.Add("hdf5.dll");

                // opencv
                dllList.Add("opencv_core2413.dll");
                dllList.Add("opencv_highgui2413.dll");
                dllList.Add("opencv_imgproc2413.dll");

                // Qt
                dllList.Add("Qt5Core.dll");
                dllList.Add("Qt5Gui.dll");
                dllList.Add("Qt5Widgets.dll");
                //dllList.Add("icudt54.dll");
                //dllList.Add("icuin54.dll");
                //dllList.Add("icuuc54.dll");

                // msvc
                dllList.Add("msvcp140.dll");
                dllList.Add("vccorlib140.dll");
                dllList.Add("concrt140.dll");
                dllList.Add("vcruntime140.dll");

                // universal support
                dllList.Add("ucrtbase.dll");

                // openmp
                dllList.Add("vcomp140.dll");

                // dependencies
                dllList.Add("zlib.dll");
                dllList.Add("szip.dll");

                for (int ii = 0; ii < dllList.Count; ++ii)
                {
                    FileUtil.DeleteFileOrDirectory(buildDataPath + "/" + dllList[ii]);
                    FileUtil.CopyFileOrDirectory(Application.dataPath + "/../" + dllList[ii], buildDataPath + "/" + dllList[ii]);
                }

                FileUtil.DeleteFileOrDirectory(buildDataPath + "/platforms");
                FileUtil.CopyFileOrDirectory(Application.dataPath + "/../platforms", buildDataPath + "/platforms");
            }
        }
    }
#endif


    namespace Events
    {
        /// <summary>
        /// Event called when an IEGG column minimized state has changed (params : spScene, IEEGColumnsMinimizedStates)
        /// </summary>
        public class UpdateColumnMinimizeStateEvent : UnityEvent<bool, List<bool>> { }
    }


    /// <summary>
    /// Interface class for controling the 3D module ; loading data, setting visibilty parameters...
    /// </summary>
    public class HBP_3DModule_Command : MonoBehaviour
    {
        #region members

        private HBP_3DModule_Main m_HBP3D = null; /**< main class of the 3D module */

        // events        
        private Events.UpdateColumnMinimizeStateEvent m_updateColumnMinimizeStateEvent = new Events.UpdateColumnMinimizeStateEvent();
        public Events.UpdateColumnMinimizeStateEvent UpdateColumnMinimizeStateEvent{get { return m_updateColumnMinimizeStateEvent; }}

        protected Events.InfoPlotRequest m_plotInfoRequestEvent = new Events.InfoPlotRequest();
        public Events.InfoPlotRequest PlotInfoRequestEvent { get { return m_plotInfoRequestEvent; } }

        protected Events.LoadSpSceneFromMP m_loadSpSceneFromMP = new Events.LoadSpSceneFromMP();
        public Events.LoadSpSceneFromMP LoadSpSceneFromMP { get { return m_loadSpSceneFromMP; } }



        // TODO    
        //protected Events.FocusedColumnAndScene m_focusedColumnAndScene = new Events.FocusedColumnAndScene();
        //public Events.FocusedColumnAndScene FocusedColumnAndScene { get { return m_focusedColumnAndScene; } }
        //protected Events.LoadedSP m_loadedSPPatient = new Events.LoadedSinglePatient();
        //public Events.LoadedSP LoadedSP { get { return m_loadedSPPatient; } }



        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve
            m_HBP3D = GetComponent<HBP_3DModule_Main>();            

            // command listeners            
            MinimizeController minimizeController = m_HBP3D.UIManager.UIOverlayManager.MinimizeController;
            minimizeController.m_minimizeStateSwitchEvent.AddListener((spScene) =>
            {
                if (spScene)
                    m_updateColumnMinimizeStateEvent.Invoke(true, minimizeController.SPMinimizeStateList); 
                else
                    m_updateColumnMinimizeStateEvent.Invoke(false, minimizeController.MPMinimizeStateList);
            });
            m_HBP3D.ScenesManager.SPScene.PlotInfoRequest.AddListener((plotRequest) =>
            {
                m_plotInfoRequestEvent.Invoke(plotRequest);
            });
            m_HBP3D.ScenesManager.MPScene.PlotInfoRequest.AddListener((plotRequest) =>
            {
                m_plotInfoRequestEvent.Invoke(plotRequest);
            });

            m_HBP3D.ScenesManager.MPScene.LoadSpSceneFromMP.AddListener((idPatient) =>
            {
                m_loadSpSceneFromMP.Invoke(idPatient);
            });
        }


        void OnDestroy()
        {
            DLL.DLLDebugManager.clean();
            Debug.Log("destroy command");
        }


        #endregion mono_behaviour

        #region others

        /// <summary>
        /// Set the focus state of the module
        /// </summary>
        /// <param name="state"></param>
        public void setModuleFocusState(bool state)
        {
            m_HBP3D.ScenesManager.setModuleFocus(state);
        }

        /// <summary>
        /// Set the visibility ratio between the single patient scene and the multi patients scene, if the two parameters are true or false, the two scenes will be displayed.
        /// </summary>
        /// <param name="showSPScene"> if true display single patient scene</param>
        /// <param name="showMPScene"> if true display multi patients scene</param>
        public void setScenesVisibility(bool showSPScene = true, bool showMPScene = true)
        {
            m_HBP3D.ScenesManager.setScenesVisibility(showSPScene, showMPScene);
        }

        /// <summary>
        /// Load a single patient scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataSP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        public bool setSceneData(Data.Visualisation.SinglePatientVisualisationData visuDataSP)
        {
            return m_HBP3D.setSPData(visuDataSP);
        }

        /// <summary>
        /// Load a multi patients scene, load the UI and the data.
        /// </summary>
        /// <param name="visuDataMP"></param>
        /// <returns>false if a loading error occurs, else true </returns>
        public bool setSceneData(Data.Visualisation.MultiPatientsVisualisationData visuDataMP)
        {
            if (MNIObjects.LoadingMutex.WaitOne(10000))
            {
                return m_HBP3D.setMPData(visuDataMP);
            }

            Debug.LogError("MNI loading data not finished. ");

            return false;
        }


        #endregion others
    }
}