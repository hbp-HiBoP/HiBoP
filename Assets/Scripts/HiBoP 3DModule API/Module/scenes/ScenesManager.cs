
/**
 * \file    ScenesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define ScenesManager class
 */

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// hbp
using HBP.VISU3D.Cam;

namespace HBP.VISU3D
{
    namespace Events
    {
        public class FocusOnScene : UnityEvent<bool> { }
    }

    /// <summary>
    /// Manager for the single patient and the multi patients scenes
    /// </summary>
    public class ScenesManager : MonoBehaviour
    {
        #region members

        // ######## public
        // scenes
        private SP3DScene m_SPScene = null; /**< single patient scene */
        public SP3DScene SPScene
        {
            get { return m_SPScene; }
        }
        private MP3DScene m_MPScene = null; /**< multi patients scene */
        public MP3DScene MPScene
        {
            get { return m_MPScene; }
        }
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

        private GameObject m_SPPanel = null;
        public GameObject SPPanel{get { return m_SPPanel; }}

        private GameObject m_MPPanel = null;
        public GameObject MPPanel { get { return m_MPPanel; } }

        private GameObject m_SPCameras = null;
        public GameObject SPCameras { get { return m_SPCameras; } }

        private GameObject m_MPCameras = null;
        public GameObject MPCameras { get { return m_MPCameras; } }

        private Slider m_scenesRatioSlider = null;

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
            m_SPScene = transform.Find("SP").GetComponent<SP3DScene>();
            m_MPScene = transform.Find("MP").GetComponent<MP3DScene>();            
            //  cameras
            m_SPCameras = transform.Find("Cameras").Find("sp_cameras").gameObject;
            m_MPCameras = transform.Find("Cameras").Find("mp_cameras").gameObject;
            //  retrieve ui elements
            m_scenesRatioSlider = m_modulePanel.transform.Find("middle").Find("scenes ratio slider").gameObject.GetComponent<Slider>();
            //  retrieve panels
            m_SPPanel = m_camerasManager.m_singlePatientPanel;
            m_MPPanel = m_camerasManager.m_multiPatientsPanel;

            // add listeners     
            //  cameras
            m_MPScene.ApplySceneCamerasToIndividualScene.AddListener(() =>
            {
                m_camerasManager.apply_MP_cameras_settings_to_SP_cameras();
            });
            //  modes specs
            m_SPScene.SendModeSpecifications.AddListener((UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));
            m_MPScene.SendModeSpecifications.AddListener((UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));


            m_SPScene.UpdateCameraTarget.AddListener((target) =>
            {
                m_camerasManager.UpdateCamerasTarget(true, target);
            });

            m_MPScene.UpdateCameraTarget.AddListener((target) =>
            {
                m_camerasManager.UpdateCamerasTarget(false, target);
            });


            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.ScenesManager, gameObject);
        }

        #endregion mono_behaviour

        #region functions

        public void SetModuleFocusState(bool state)
        {
            m_camerasManager.set_module_focus(state);            
        }

        /// <summary>
        /// Define the number of columns cameras of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="nbColumns"></param>
        public void define_conditions_columns_cameras(bool spScene, int nbColumns)
        {
            m_camerasManager.define_conditions_columns_cameras(spScene, nbColumns);
        }

        /// <summary>
        /// Define the current selected column of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void define_selected_column(bool spScene, int idColumn)
        {
            if(spScene)
                m_SPScene.update_selected_column(idColumn);
            else
                m_MPScene.update_selected_column(idColumn);
        }

        /// <summary>
        /// Display a message in the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void DisplayMessageInScene(bool spScene, string message, float duration, int width, int height)
        {
            if(spScene)
                m_SPScene.display_sceen_message(message, duration, width, height);
            else
                m_MPScene.display_sceen_message(message, duration, width, height);
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

            if (m_scenesRatioSlider.value == value)
            {
                if (m_scenesRatioSlider.value != 1f)
                {
                    m_scenesRatioSlider.value = 1f; // force callback
                }
                else
                {
                    m_scenesRatioSlider.value = 0f; // force callback
                }
            }

            m_scenesRatioSlider.value = value;
        }



        /// <summary>
        /// Load IRMF dialog
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool load_fMRI_dialog(bool spScene, out string IRMFPath)
        {
            IRMFPath = "";
            string[] filters = new string[] { "nii", "img" };
            IRMFPath = VISU3D.DLL.QtGUI.get_existing_file_name(filters, "Select an fMRI file");
    
            if (IRMFPath.Length == 0) // no path selected
                return false;

            // load IMRF
            if (spScene)
            {
                if (!m_SPScene.load_FMRI_file(IRMFPath))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load IRMF");
                    return false;
                }
            }
            else
            {
                if (!m_MPScene.load_FMRI_file(IRMFPath))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load IRMF");
                    return false;
                }
            }

            m_camerasManager.add_column_cameras(spScene, true);
            return true;
        }


        /// <summary>
        /// Add an IRMF column to a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool add_fMRI_column(bool spScene, string fmriLabel)
        {
            // add column
            if (spScene)
            {
                if (!m_SPScene.add_FMRI_column(fmriLabel))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
                    return false;
                }
            }
            else
            {
                if (!m_MPScene.add_FMRI_column(fmriLabel))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
                    return false;
                }
            }
            
            return true;
        }

        public void remove_last_fMRI_column(bool spScene)
        {
            m_camerasManager.remove_last_column_cameras(spScene);

            if (spScene)
            {
                m_SPScene.remove_last_FMRI_column();
            }
            else
            {
                m_MPScene.remove_last_FMRI_column();
            }
        }

        public int fMRI_columns_nb(bool spScene)
        {
            return spScene ? m_SPScene.GetNumberOffMRIColumns() : m_MPScene.GetNumberOffMRIColumns();
        }


        #endregion functions
    }
}