
/**
 * \file    ScenesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define ScenesManager class
 */

// system
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

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

        // ######## private
        // managers                
        private UIManager m_UIManager = null;
        





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

        private Transform m_buttonsRightPanel = null;
        private Slider m_scenesRatioSlider = null;


        // events
        protected Events.SendModeSpecifications m_sendModeSpecifications = new Events.SendModeSpecifications();
        public Events.SendModeSpecifications SendModeSpecifications { get { return m_sendModeSpecifications; } }
        public Events.FocusOnScene FocusOnScene = new Events.FocusOnScene();

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            int idScript = TimeExecution.getId();
            TimeExecution.startAwake(idScript, TimeExecution.ScriptsId.ScenesManager);
            

            // retrieve 
            //  managers
            m_UIManager = transform.parent.Find("UI").GetComponent<UIManager>();
            m_camerasManager = transform.Find("Cameras").GetComponent<CamerasManager>();            
            //  scenes
            m_SPScene = transform.Find("SP").GetComponent<SP3DScene>();
            m_MPScene = transform.Find("MP").GetComponent<MP3DScene>();            
            //  cameras
            m_SPCameras = transform.Find("Cameras").Find("sp_cameras").gameObject;
            m_MPCameras = transform.Find("Cameras").Find("mp_cameras").gameObject;
            //  retrieve ui elements
            m_buttonsRightPanel = m_modulePanel.transform.Find("middle").Find("right menu");
            m_scenesRatioSlider = m_modulePanel.transform.Find("middle").Find("scenes ratio slider").gameObject.GetComponent<Slider>();
            //  retrieve panels
            m_SPPanel = m_camerasManager.m_singlePatientPanel;
            m_MPPanel = m_camerasManager.m_multiPatientsPanel;

            // add listeners     
            //  cameras
            m_MPScene.ApplySceneCamerasToIndividualScene.AddListener(() =>
            {
                m_camerasManager.applyMPCamerasSettingsToSPCameras();
            });
            //  modes specs
            m_SPScene.SendModeSpecifications.AddListener((specs) =>
            {
                m_sendModeSpecifications.Invoke(specs);
            });
            m_MPScene.SendModeSpecifications.AddListener((specs) =>
            {
                m_sendModeSpecifications.Invoke(specs);
            });


            m_SPScene.UpdateCameraTarget.AddListener((target) =>
            {
                m_camerasManager.updateCamerasTarget(true, target);
            });

            m_MPScene.UpdateCameraTarget.AddListener((target) =>
            {
                m_camerasManager.updateCamerasTarget(false, target);
            });


            TimeExecution.endAwake(idScript, TimeExecution.ScriptsId.ScenesManager, gameObject);
        }

        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Set the focus state of the module
        /// </summary>
        /// <param name="state"></param>
        public void setModuleFocus(bool state)
        {            
            m_camerasManager.setModuleFocus(state);            
        }

        /// <summary>
        /// Define the number of columns cameras of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="nbColumns"></param>
        public void defineConditionColumnsCameras(bool spScene, int nbColumns)
        {
            m_camerasManager.defineConditionColumnsCameras(spScene, nbColumns);
        }

        /// <summary>
        /// Define the current selected column of the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void defineSelectedColumn(bool spScene, int idColumn)
        {
            if(spScene)
                m_SPScene.updateSelectedColumn(idColumn);
            else
                m_MPScene.updateSelectedColumn(idColumn);
        }

        /// <summary>
        /// Display a message in the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void displayMessageInScene(bool spScene, string message, float duration, int width, int height)
        {
            if(spScene)
                m_SPScene.displayScreenMessage(message, duration, width, height);
            else
                m_MPScene.displayScreenMessage(message, duration, width, height);
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
        public bool loadIRMFDialog(bool spScene, out string IRMFPath)
        {
            IRMFPath = "";

            if (spScene)
            {
                if (m_SPScene.getIRMFColumnsNb() >= 2)
                    return false;
            }
            else
            {
                if (m_MPScene.getIRMFColumnsNb() >= 2)
                    return false;
            }

            //DLL.QtGUI gui = DLL.QtGUI.Instance;
            string[] filters = new string[] { "nii", "img" };
            IRMFPath = DLL.QtGUI.getOpenFileName(filters, "Select an IRMF file");


            if (IRMFPath.Length == 0) // no path selected
                return false;

            // load IMRF
            if (spScene)
            {
                if (!m_SPScene.loadIRMFColumn(IRMFPath))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load IRMF");
                    return false;
                }
            }
            else
            {
                if (!m_MPScene.loadIRMFColumn(IRMFPath))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load IRMF");
                    return false;
                }
            }

            m_camerasManager.addColumnCameras(spScene, true);
            return true;
        }


        /// <summary>
        /// Add an IRMF column to a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool addIRMFColumn(bool spScene, string IRMFLabel)
        {
            // add column
            if (spScene)
            {
                if (!m_SPScene.addIRMFColumn(IRMFLabel))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
                    return false;
                }
            }
            else
            {
                if (!m_MPScene.addIRMFColumn(IRMFLabel))
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
                    return false;
                }
            }

            Column3DViewManager cm = spScene ? m_SPScene.CM : m_MPScene.CM;
            m_UIManager.UIOverlayManager.updateColumnName(spScene, cm.nbIEEGCol() + cm.nbIRMFCol() - 1, IRMFLabel);

            displayMessageInScene(spScene, "IRMF successfully loaded. ", 2f, 150, 80);

            return true;
        }

        /// <summary>
        /// Remove the last IRMF column of the input scene
        /// </summary>
        /// <param name="spScene"></param>
        public void removeLastIRMFColumn(bool spScene)
        {
            m_camerasManager.removeLastColumnCameras(spScene);

            if (spScene)
            {
                m_SPScene.removeLastIRMFColumn();
            }
            else
            {
                m_MPScene.removeLastIRMFColumn();
            }
        }

        /// <summary>
        /// Return the nnumber of IRMF columns of the input scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public int IRMFColumnsNumber(bool spScene)
        {
            if (spScene)
            {
                return m_SPScene.getIRMFColumnsNb();
            }

            return m_MPScene.getIRMFColumnsNb();
        }


        #endregion functions
    }
}