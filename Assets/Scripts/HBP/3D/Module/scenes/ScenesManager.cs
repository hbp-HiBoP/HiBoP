/**
 * \file    ScenesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define ScenesManager class
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using HBP.Module3D.Cam;

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
        private SP3DScene m_SinglePatientScene = null; /**< single patient scene */
        public SP3DScene SinglePatientScene
        {
            get { return m_SinglePatientScene; }
        }
        private MP3DScene m_MultiPatientsScene = null; /**< multi patients scene */
        public MP3DScene MultiPatientsScene
        {
            get { return m_MultiPatientsScene; }
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
        private CamerasManager m_camerasManager = null;
        public CamerasManager CamerasManager
        {
            get { return m_camerasManager; }
        }


        // canvas
        public GameObject m_canvas = null;

        // UI elements
        public GameObject m_modulePanel = null;

        public Transform SPPanel {get { return m_camerasManager.SinglePatientPanel; } }
        public Transform MPPanel { get { return m_camerasManager.MultiPatientsPanel; } }
        public Transform SPCameras { get { return m_camerasManager.SinglePatientCamerasContainer; } }
        public Transform MPCameras { get { return m_camerasManager.MultiPatientsCamerasContainer; } }

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
            m_camerasManager = transform.Find("Cameras").GetComponent<CamerasManager>();            
            //  scenes
            m_SinglePatientScene = transform.Find("SP").GetComponent<SP3DScene>();
            m_MultiPatientsScene = transform.Find("MP").GetComponent<MP3DScene>();

            // add listeners     
            //  cameras
            m_MultiPatientsScene.ApplySceneCamerasToIndividualScene.AddListener(() =>
            {
                m_camerasManager.ApplyMultiPatientsCamerasSettingsToSinglePatientCameras();
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
                m_camerasManager.UpdateCamerasTarget(SceneType.SinglePatient, target);
            });

            m_MultiPatientsScene.UpdateCameraTarget.AddListener((target) =>
            {
                m_camerasManager.UpdateCamerasTarget(SceneType.MultiPatients, target);
            });


            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.ScenesManager, gameObject);
        }

        #endregion mono_behaviour

        #region Public Methods

        public void SetModuleFocusState(bool state)
        {
            m_camerasManager.SetModuleFocus(state);            
        }

        /// <summary>
        /// Define the number of columns cameras of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="nbColumns"></param>
        public void define_conditions_columns_cameras(SceneType type, int nbColumns)
        {
            m_camerasManager.DefineConditionsColumnsCameras(type, nbColumns);
        }

        /// <summary>
        /// Define the current selected column of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void define_selected_column(Base3DScene scene, int idColumn)
        {
            switch(scene.Type)
            {
                case SceneType.SinglePatient:
                    m_SinglePatientScene.update_selected_column(idColumn);
                    break;
                case SceneType.MultiPatients:
                    m_MultiPatientsScene.update_selected_column(idColumn);
                    break;
                default:
                    break;
            }
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
            m_camerasManager.AddColumnCamera(type, CameraType.fMRI);
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
            m_camerasManager.RemoveLastColumnCamera(type);
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