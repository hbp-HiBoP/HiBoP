/**
 * \file    ScenesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define ScenesManager class
 */
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.Cam;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Module3D
{
    namespace Events
    {
        public class FocusOnScene : UnityEvent<bool> { }
        public class OnChangeSelectedScene : UnityEvent<Base3DScene> { }
    }

    /// <summary>
    /// Manager for the single patient and the multi patients scenes
    /// </summary>
    public class ScenesManager : MonoBehaviour
    {
        #region members

        // ######## public
        // scenes
        private SinglePatient3DScene m_SinglePatientScene = null; /**< single patient scene */
        public SinglePatient3DScene SinglePatientScene
        {
            get { return m_SinglePatientScene; }
        }
        private MultiPatients3DScene m_MultiPatientsScene = null; /**< multi patients scene */
        public MultiPatients3DScene MultiPatientsScene
        {
            get { return m_MultiPatientsScene; }
        }

        private List<Base3DScene> m_Scenes = null;
        public ReadOnlyCollection<Base3DScene> Scenes
        {
            get { return new ReadOnlyCollection<Base3DScene>(m_Scenes); }
        }

        private Base3DScene m_SelectedScene = null;
        public Base3DScene SelectedScene
        {
            set
            {
                m_SelectedScene.ModesManager.OnChangeMode.RemoveAllListeners();
                m_SelectedScene = value;
                OnChangeSelectedScene.Invoke(value);
            }
            get { return m_SelectedScene; }
        }
        public Events.OnChangeSelectedScene OnChangeSelectedScene = new Events.OnChangeSelectedScene();

        // managers
        private CamerasManager m_CamerasManager = null;
        public CamerasManager CamerasManager
        {
            get { return m_CamerasManager; }
        }

        // Scene prefabs
        public GameObject SinglePatientScenePrefab;
        public GameObject MultiPatientsScenePrefab;

        // canvas
        public GameObject m_canvas = null;

        // UI elements
        public GameObject m_modulePanel = null;

        public Transform SPPanel {get { return m_CamerasManager.SinglePatientPanel; } }
        public Transform MPPanel { get { return m_CamerasManager.MultiPatientsPanel; } }
        public Transform SPCameras { get { return m_CamerasManager.SinglePatientCamerasContainer; } }
        public Transform MPCameras { get { return m_CamerasManager.MultiPatientsCamerasContainer; } }

        // events
        public Events.SendModeSpecifications SendModeSpecifications = new Events.SendModeSpecifications();
        public Events.FocusOnScene FocusOnScene = new Events.FocusOnScene();

        #endregion members

        #region mono_behaviour

        void Awake()
        {
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.ScenesManager);
            
            // retrieve 
            //  managers
            m_CamerasManager = transform.Find("Cameras").GetComponent<CamerasManager>();            
            //  scenes
            m_SinglePatientScene = transform.Find("SP").GetComponent<SinglePatient3DScene>();
            m_MultiPatientsScene = transform.Find("MP").GetComponent<MultiPatients3DScene>();

            // add listeners     
            //  cameras
            m_MultiPatientsScene.ApplySceneCamerasToIndividualScene.AddListener(() =>
            {
                m_CamerasManager.ApplyMultiPatientsCamerasSettingsToSinglePatientCameras();
            });
            //  modes specs
            m_SinglePatientScene.SendModeSpecifications.AddListener((UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));
            m_MultiPatientsScene.SendModeSpecifications.AddListener((UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));


            m_SinglePatientScene.UpdateCameraTarget.AddListener((target) =>
            {
                m_CamerasManager.UpdateCamerasTarget(SceneType.SinglePatient, target);
            });

            m_MultiPatientsScene.UpdateCameraTarget.AddListener((target) =>
            {
                m_CamerasManager.UpdateCamerasTarget(SceneType.MultiPatients, target);
            });


            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.ScenesManager, gameObject);
        }

        #endregion mono_behaviour

        #region Public Methods
        public bool AddSinglePatientScene(Data.Visualisation.SinglePatientVisualisation visualisation, bool postIRM)
        {
            // Create new scene
            GameObject newSinglePatientScene = Instantiate(SinglePatientScenePrefab, transform);
            SinglePatient3DScene scene = newSinglePatientScene.GetComponent<SinglePatient3DScene>();
            bool success = scene.Initialize(visualisation, postIRM);
            if (!success)
            {
                Debug.LogError("-ERROR : Could not initialize new single patient scene.");
                return false;
            }
            // Set up the cameras

            // Set up the UI

            // Set Timeline data
            // Done in Initialize
            // Set selected site to none
            // Done in Initialize
            // Set selected column to the first column
            scene.update_selected_column(0);
            // Focus on the scene

            scene.display_sceen_message("Single Patient Scene loaded : " + visualisation.Patient.Place + "_" + visualisation.Patient.Name + "_" + visualisation.Patient.Date, 2.0f, 400, 80);

            m_Scenes.Add(scene);
            return true;
        }

        public bool AddMultiPatientsScene(Data.Visualisation.MultiPatientsVisualisation visualisation)
        {
            // Create new scene
            GameObject newMultiPatientsScene = Instantiate(SinglePatientScenePrefab, transform);
            MultiPatients3DScene scene = newMultiPatientsScene.GetComponent<MultiPatients3DScene>();
            bool success = scene.Initialize(visualisation);
            if (!success)
            {
                Debug.LogError("-ERROR : Could not initialize new multi patients scene.");
                return false;
            }
            // Set up the cameras

            // Set up the UI

            // Set Timeline data
            // Done in Initialize
            // Set selected column to the first column
            scene.update_selected_column(0);
            // Focus on the scene

            scene.display_sceen_message("Multi Patients Scene loaded", 2.0f, 400, 80);

            m_Scenes.Add(scene);
            return true;
        }

        public void RemoveSinglePatientScene()
        {

        }

        public void RemoveMultiPatientsScene()
        {

        }

        public void SetModuleFocusState(bool state)
        {
            m_CamerasManager.SetModuleFocus(state);            
        }

        /// <summary>
        /// Display a message in the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void DisplayMessageInScene(SceneType type, string message, float duration, int width, int height)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_SinglePatientScene.display_sceen_message(message, duration, width, height);
                    break;
                case SceneType.MultiPatients:
                    m_MultiPatientsScene.display_sceen_message(message, duration, width, height);
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Define the visibility of the scenes
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="mpScene"></param>
        public void setScenesVisibility(bool spScene, bool mpScene)
        {
            if (spScene)
                FocusOnScene.Invoke(true);
            if(mpScene)
                FocusOnScene.Invoke(false);

            float value;

            if (spScene && mpScene)
            {
                value = 0.5f;
            }
            else if (spScene)
            {
                value = 1f;
            }
            else if (mpScene)
            {
                value = 0f;
            }
            else
            {
                value = 0.5f;
            }

            //if (m_scenesRatioSlider.value == value)
            //{
            //    if (m_scenesRatioSlider.value != 1f)
            //    {
            //        m_scenesRatioSlider.value = 1f; // force callback
            //    }
            //    else
            //    {
            //        m_scenesRatioSlider.value = 0f; // force callback
            //    }
            //}

            //m_scenesRatioSlider.value = value;
        }



        /// <summary>
        /// Load IRMF dialog
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool LoadfMRIDialog(SceneType type, out string path)
        {
            bool loaded = true;
            string[] filters = new string[] { "nii", "img" };
            path = "";
            path = DLL.QtGUI.GetExistingFileName(filters, "Select an fMRI file");
    
            if (!string.IsNullOrEmpty(path))
            {
                switch (type)
                {
                    case SceneType.SinglePatient:
                        if (!m_SinglePatientScene.load_FMRI_file(path))
                        {
                            Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load IRMF");
                            loaded = false;
                        }
                        break;
                    case SceneType.MultiPatients:
                        if (!m_MultiPatientsScene.load_FMRI_file(path))
                        {
                            Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load IRMF");
                            loaded = false;
                        }
                        break;
                    default:
                        loaded = false;
                        break;
                }
            }
            else
            {
                loaded = false;
            }
            m_CamerasManager.AddColumnCamera(type, CameraType.fMRI);
            return loaded;
        }


        /// <summary>
        /// Add an IRMF column to a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool AddfMRIColumn(SceneType type, string fmriLabel)
        {
            bool result = true;
            switch (type)
            {
                case SceneType.SinglePatient:
                    if (!m_SinglePatientScene.AddfMRIColumn(fmriLabel))
                    {
                        Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
                        result = false;
                    }
                    break;
                case SceneType.MultiPatients:
                    if (!m_MultiPatientsScene.AddfMRIColumn(fmriLabel))
                    {
                        Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
                        result =  false;
                    }
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }
        public void RemoveLastfMRIColumn(SceneType type)
        {
            m_CamerasManager.RemoveLastColumnCamera(type);
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_SinglePatientScene.RemoveLastFMRIColumn();
                    break;
                case SceneType.MultiPatients:
                    m_MultiPatientsScene.RemoveLastFMRIColumn();
                    break;
                default:
                    break;
            }
        }
        public int GetNumberOffMRIColumns(SceneType type)
        {
            int result;
            switch (type)
            {
                case SceneType.SinglePatient:
                    result = m_SinglePatientScene.GetNumberOffMRIColumns();
                    break;
                case SceneType.MultiPatients:
                    result = m_MultiPatientsScene.GetNumberOffMRIColumns();
                    break;
                default:
                    result = -1;
                    break;
            }
            return result;
        }
        #endregion
    }
}