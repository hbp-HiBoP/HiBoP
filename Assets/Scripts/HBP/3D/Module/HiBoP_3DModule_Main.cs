/**
 * \file    HiBoP_3DModule_Main.cs
 * \author  Lance Florian - Adrien Gannerie 
 * \date    04/05/2016 - 04/2017
 * \brief   Define the HiBoP_3DModule_Main class
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Module3D;

namespace HBP.Module3D
{
    /// <summary>
    /// Main class of the 3D module 
    /// </summary>
    public class HiBoP_3DModule_Main : MonoBehaviour
    {
        #region Properties
        private UIManager m_UIManager = null; /**< UI manager */
        public UIManager UIManager {get { return m_UIManager; }}

        private ScenesManager m_ScenesManager; /**< scenes manager */
        public ScenesManager ScenesManager { get { return m_ScenesManager; } }
        #endregion 

        #region Private Methods
        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve managers
            m_UIManager = GameObject.Find("Brain Visualisation").GetComponent<UIManager>();
            m_ScenesManager = transform.Find("Scenes").GetComponent<ScenesManager>();

            // graphics settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;
        }

        void Start()
        {
            // initialization
            m_UIManager.Initialize(m_ScenesManager);

            // define listeners
            m_ScenesManager.SendModeSpecifications.AddListener((specs) =>
            {
                // set UI overlay specs
                UnityEngine.Profiling.Profiler.BeginSample("TEST-HiBoP_3DModule_Main setSpecificOverlayActive 1");
                for (int ii = 0; ii < specs.uiOverlayMask.Count; ++ii)
                    m_UIManager.OverlayManager.set_specific_overlay_active(specs.uiOverlayMask[ii], ii, specs.mode);
                UnityEngine.Profiling.Profiler.EndSample();

                // set UI camera specs                
                UnityEngine.Profiling.Profiler.BeginSample("TEST-HiBoP_3DModule_Main update_UI_with_mode 1");
                    m_UIManager.MenuManager.update_UI_with_mode(specs.mode);
                UnityEngine.Profiling.Profiler.EndSample();
            });

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a new single patient scene
        /// </summary>
        /// <param name="visualisation"></param>
        /// <returns></returns>
        public bool SetSinglePatientSceneData(Data.Visualisation.SinglePatientVisualisation visualisation, bool postIRM = false)
        {
            bool success = m_ScenesManager.AddSinglePatientScene(visualisation, postIRM);
            if (!success)
            {
                Debug.LogError("-ERROR : Failed to add single patient scene");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Load a new multi patients scene
        /// </summary>
        /// <param name="data"></param>
        public bool SetMultiPatientsSceneData(Data.Visualisation.MultiPatientsVisualisation visualisation)
        {
            bool success = m_ScenesManager.AddMultiPatientsScene(visualisation);
            if (!success)
            {
                Debug.LogError("-ERROR : Failed to add multi patients scene");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Add a fMRI column
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public bool AddfMRIColumnToSelectedScene()
        {
            string fMRIPath;
            if (!m_ScenesManager.LoadfMRIDialogOfSelectedScene(out fMRIPath))
                return false;

            string[] split = fMRIPath.Split(new Char[] { '/', '\\' });
            string fmriLabel = split[split.Length - 1];

            m_UIManager.AddfMRIColumn(m_ScenesManager.SelectedScene.Type, fmriLabel);

            return m_ScenesManager.AddfMRIColumnToSelectedScene(fmriLabel);
        }
        /// <summary>
        /// Remove the last fmRI column
        /// </summary>
        /// <param name="spScene"></param>
        public void RemoveLastfMRIColumnFromSelectedScene()
        {
            if (m_ScenesManager.GetNumberOffMRIColumnsOfSelectedScene() == 0)
                return;

            m_UIManager.RemoveLastfMRIColumn(m_ScenesManager.SelectedScene.Type);
            m_ScenesManager.RemoveLastfMRIColumnFromSelectedScene();
        }
        /// <summary>
        /// Define the ratio between the two scenes
        /// </summary>
        /// <param name="ratio"></param>
        public void set_ratio_scene(float ratio) // FIXME : delete
        {
            bool isSPSceneDisplayed = (ratio > 0.2f);
            bool isMPSceneDisplayed = (ratio < 0.8f);

            m_UIManager.OverlayManager.set_overlay_scene_visibility(isSPSceneDisplayed, SceneType.SinglePatient);
            m_UIManager.OverlayManager.set_overlay_scene_visibility(isMPSceneDisplayed, SceneType.MultiPatients);

            m_ScenesManager.SPPanel.transform.parent.gameObject.SetActive(isSPSceneDisplayed);
            m_ScenesManager.MPPanel.transform.parent.gameObject.SetActive(isMPSceneDisplayed);

            m_ScenesManager.SPCameras.gameObject.SetActive(isSPSceneDisplayed);
            m_ScenesManager.MPCameras.gameObject.SetActive(isMPSceneDisplayed);

            m_ScenesManager.SPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = ratio;
            m_ScenesManager.MPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1f - ratio;
        }
        #endregion

    }
}