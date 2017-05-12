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

        private ScenesManager m_scenesManager; /**< scenes manager */
        public ScenesManager ScenesManager { get { return m_scenesManager; } }
        #endregion 

        #region Private Methods
        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve managers
            m_UIManager = GameObject.Find("Brain Visualization").GetComponent<UIManager>();
            m_scenesManager = transform.Find("Scenes").GetComponent<ScenesManager>();

            // graphics settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.antiAliasing = 8;
        }

        void Start()
        {
            // initialization
            m_UIManager.Initialize(m_scenesManager);

            // define listeners
            m_scenesManager.SendModeSpecifications.AddListener((specs) =>
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

        #endregion mono_behaviour

        #region functions
        /// <summary>
        /// Reload the SP scene
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool set_SP_data(Data.Visualization.SinglePatientVisualizationData data, bool postIRM = false)
        {
            List<string> ptsFiles = new List<string>(), namePatients = new List<string>();
            ptsFiles.Add(data.Patient.Brain.PatientReferenceFrameImplantation);
            namePatients.Add(data.Patient.Place + "_" + data.Patient.Name);

            List<string> meshesFiles = new List<string>();
            meshesFiles.Add(data.Patient.Brain.RightCerebralHemisphereMesh);
            meshesFiles.Add(data.Patient.Brain.LeftCerebralHemisphereMesh);

            // set paths
            bool success = m_scenesManager.SinglePatientScene.reset(data.Patient, postIRM);
            if (!success)
            {
                Debug.LogError("-ERROR : ScenesManager::setSPData -> aborted. ");
                return false;
            }

            // retrieve column data
            List<HBP.Data.Visualization.ColumnData> columnsData = data.Columns.ToList<HBP.Data.Visualization.ColumnData>();

            // 1) define the number of cameras for the scene  
            m_scenesManager.define_conditions_columns_cameras(SceneType.SinglePatient, data.Columns.Count);

            // 2) define the number of UI overlays and UI cameras for the scene
            m_UIManager.SetiEEGColumns(SceneType.SinglePatient, columnsData);

            // 3) define the iEEG data of the scene -> create the 3D view columns
            m_scenesManager.SinglePatientScene.set_timeline_data(data.Patient, columnsData);

            // 4) define the selected plot of all the columns of the scene
            m_scenesManager.SinglePatientScene.define_selected_site(-1);

            // 5) update the current selected column of the scene
            m_scenesManager.define_selected_column(ScenesManager.SinglePatientScene, 0);

            // 6) focus on the scene
            m_UIManager.UpdateFocusedSceneAndColumn(ScenesManager.SinglePatientScene, 0);

            m_scenesManager.DisplayMessageInScene(SceneType.SinglePatient, "Individual scene loaded: " + data.Patient.Place + "_" + data.Patient.Name + "_" + data.Patient.Date, 2f, 400, 80);

            return true;
        }

        /// <summary>
        /// Reload the SP scene
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnsData"></param>
        /// <param name="idPlotSelected"></param>
        /// <param name="blacklistMasks"></param>
        /// <param name="excludedMasks"></param>
        /// /// <returns></returns>
        public bool set_SP_data(HBP.Data.Patient patient, List<HBP.Data.Visualization.ColumnData> columnsData, int idPlotSelected, List<List<bool>> blacklistMasks,
            List<List<bool>> excludedMasks, List<List<bool>> hightLightedMasks)
        {

            // set paths
            bool success = m_scenesManager.SinglePatientScene.reset(patient, false);
            if (!success)
            {
                Debug.LogError("-ERROR : ScenesManager::setSPData -> aborted. ");
                return false;
            }

            // 1) define the number of cameras for the scene  
            m_scenesManager.define_conditions_columns_cameras(SceneType.SinglePatient, columnsData.Count);

            // 2) define the number of UI overlays and UI cameras for the scene
            m_UIManager.SetiEEGColumns(SceneType.SinglePatient, columnsData);

            // 3) define the iEEG data of the scene -> create the 3D view columns
            m_scenesManager.SinglePatientScene.set_timeline_data(patient, columnsData);

            // 4) define the selected plot of all the columns of the scene
            m_scenesManager.SinglePatientScene.define_selected_site(idPlotSelected);

            // 5) define the maks of all the columns of the scene
            m_scenesManager.SinglePatientScene.set_columns_site_mask(blacklistMasks, excludedMasks, hightLightedMasks);

            // 6) update the current selected column of the scene
            m_scenesManager.define_selected_column(ScenesManager.SinglePatientScene, 0);

            // 7) focus on the scene
            m_UIManager.UpdateFocusedSceneAndColumn(ScenesManager.SinglePatientScene, 0);
            
            m_scenesManager.DisplayMessageInScene(SceneType.SinglePatient, "Individual scene loaded: " + patient.Place + "_" + patient.Name + "_" + patient.Date, 2f, 400, 80);

            return true;
        }


        /// <summary>
        /// Reload the MP scene
        /// </summary>
        /// <param name="data"></param>
        public bool set_MP_data(HBP.Data.Visualization.MultiPatientsVisualizationData data)
        {
            List<string> ptsFiles = new List<string>(data.GetImplantation().Length), namePatients = new List<string>(data.GetImplantation().Length);
            for (int ii = 0; ii < data.GetImplantation().Length; ++ii)
            {
                ptsFiles.Add(data.GetImplantation()[ii]);
                namePatients.Add(data.Patients[ii].Place + "_" + data.Patients[ii].Name);
            }

            // set paths
            bool success = m_scenesManager.MultiPatientsScene.reset(data);
            if (!success)
            {
                Debug.LogError("-ERROR : ScenesManager::setMPData -> aborted. ");
                return false;
            }

            // retrieve column data
            List<HBP.Data.Visualization.ColumnData> columnsData = data.Columns.ToList<HBP.Data.Visualization.ColumnData>();

            // 1) define the number of cameras for the scene
            m_scenesManager.define_conditions_columns_cameras(SceneType.MultiPatients, data.Columns.Count);

            // 2) define the number of UI overlays and UI cameras for the scene
            m_UIManager.SetiEEGColumns(SceneType.MultiPatients, columnsData);

            // 3) define the iEEG data of the scene -> create the 3D view columns
            m_scenesManager.MultiPatientsScene.set_timeline_data(data.Patients.ToList<HBP.Data.Patient>(), columnsData, data.GetImplantation().Cast<string>().ToList());

            // 4) update the current selected column of the scene
            m_scenesManager.define_selected_column(ScenesManager.MultiPatientsScene, 0);

            // 5) Focus on the scene
            m_UIManager.UpdateFocusedSceneAndColumn(ScenesManager.MultiPatientsScene, 0);

            m_scenesManager.DisplayMessageInScene(SceneType.MultiPatients, "MNI scene loaded. ", 2f, 150, 80);

            return true;
        }

        /// <summary>
        /// Add a fMRI column
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public bool AddfMRIColumn(SceneType type)
        {
            string fMRIPath;
            if (!m_scenesManager.LoadfMRIDialog(type, out fMRIPath))
                return false;

            string[] split = fMRIPath.Split(new Char[] { '/', '\\' });
            string fmriLabel = split[split.Length - 1];

            m_UIManager.AddfMRIColumn(type, fmriLabel);

            return m_scenesManager.AddfMRIColumn(type, fmriLabel);
        }

        /// <summary>
        /// Remove the last fmRI column
        /// </summary>
        /// <param name="spScene"></param>
        public void RemoveLastfMRIColumn(SceneType type)
        {
            if (m_scenesManager.GetNumberOffMRIColumns(type) == 0)
                return;

            m_UIManager.RemoveLastfMRIColumn(type);
            m_scenesManager.RemoveLastfMRIColumn(type);
        }

        /// <summary>
        /// Define the ratio between the two scenes
        /// </summary>
        /// <param name="ratio"></param>
        public void set_ratio_scene(float ratio)
        {
            bool isSPSceneDisplayed = (ratio > 0.2f);
            bool isMPSceneDisplayed = (ratio < 0.8f);

            m_UIManager.OverlayManager.set_overlay_scene_visibility(isSPSceneDisplayed, SceneType.SinglePatient);
            m_UIManager.OverlayManager.set_overlay_scene_visibility(isMPSceneDisplayed, SceneType.MultiPatients);

            m_scenesManager.SPPanel.transform.parent.gameObject.SetActive(isSPSceneDisplayed);
            m_scenesManager.MPPanel.transform.parent.gameObject.SetActive(isMPSceneDisplayed);

            m_scenesManager.SPCameras.gameObject.SetActive(isSPSceneDisplayed);
            m_scenesManager.MPCameras.gameObject.SetActive(isMPSceneDisplayed);

            m_scenesManager.SPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = ratio;
            m_scenesManager.MPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1f - ratio;
        }


        #endregion functions

    }
}