/**
 * \file    HBP_3DModule_Main.cs
 * \author  Lance Florian
 * \date    04/05/2016
 * \brief   Define the HBP_3DModule_Main class
 */

// system
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;


// unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace HBP.VISU3D
{
    /// <summary>
    /// Main class of the 3D module 
    /// </summary>
    public class HBP_3DModule_Main : MonoBehaviour
    {
        #region members

        private UIManager m_UIManager = null; /**< UI manager */
        public UIManager UIManager{get { return m_UIManager; }}

        private ScenesManager m_scenesManager; /**< scenes manager */
        public ScenesManager ScenesManager { get { return m_scenesManager; } }

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve managers
            m_UIManager = transform.Find("UI").GetComponent<UIManager>();
            m_scenesManager = transform.Find("Scene").GetComponent<ScenesManager>();



            // graphics settings
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.antiAliasing = 0;
            // ...
        }

        void Start()
        {
            // initialize
            m_UIManager.init(m_scenesManager);

            // listeners
            m_scenesManager.SendModeSpecifications.AddListener((specs) =>
            {
                // set UI overlay specs
                for (int ii = 0; ii < specs.uiOverlayMask.Count; ++ii)
                {
                    m_UIManager.UIOverlayManager.setSpecificOverlayActive(specs.uiOverlayMask[ii], ii, specs.mode);
                }

                // set UI camera specs
                m_UIManager.UICameraManager.applyUIState(specs.mode);
            });

        }

        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Reload the SP scene
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool setSPData(Data.Visualisation.SinglePatientVisualisationData data, bool postIRM = false)
        {
            List<string> ptsFiles = new List<string>(), namePatients = new List<string>();
            ptsFiles.Add(data.Patient.Brain.PatientBasedImplantation);
            namePatients.Add(data.Patient.Place + "_" + data.Patient.Name);

            List<string> meshesFiles = new List<string>();
            meshesFiles.Add(data.Patient.Brain.RightMesh);
            meshesFiles.Add(data.Patient.Brain.LeftMesh);

            // set paths
            bool success = m_scenesManager.SPScene.reset(data.Patient, postIRM);
            if (!success)
            {
                Debug.LogError("-ERROR : ScenesManager::setSPData -> aborted. ");
                return false;
            }

            // retrieve column data
            List<HBP.Data.Visualisation.ColumnData> columnsData = data.Columns.ToList<HBP.Data.Visualisation.ColumnData>();

            // 1) define the number of cameras for the scene  
            m_scenesManager.defineConditionColumnsCameras(true, data.Columns.Count);

            // 2) define the number of UI overlays and UI cameras for the scene
            m_UIManager.setIEEGColumns(true, columnsData);
       
            // 3) define the iEEG data of the scene -> create the 3D view columns
            m_scenesManager.SPScene.setTimelinesData(data.Patient, columnsData);

            // 4) define the selected plot of all the columns of the scene
            m_scenesManager.SPScene.defineSelectedPlot(-1);

            // 5) update the current selected column of the scene
            m_scenesManager.defineSelectedColumn(true, 0);

            m_UIManager.updateFocusedSceneAndColumn(true, 0);

            m_scenesManager.displayMessageInScene(true, "Individual scene loaded. ", 2f, 150, 80);

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
        public bool setSPData(HBP.Data.Patient.Patient patient, List<HBP.Data.Visualisation.ColumnData> columnsData, int idPlotSelected, List<List<bool>> blacklistMasks, List<List<bool>> excludedMasks, List<List<bool>> hightLightedMasks)
        {
            // set paths
            bool success = m_scenesManager.SPScene.reset(patient, false);
            if (!success)
            {
                Debug.LogError("-ERROR : ScenesManager::setSPData -> aborted. ");
                return false;
            }

            // 1) define the number of cameras for the scene  
            m_scenesManager.defineConditionColumnsCameras(true, columnsData.Count);

            // 2) define the number of UI overlays and UI cameras for the scene
            m_UIManager.setIEEGColumns(true, columnsData);

            // 3) define the iEEG data of the scene -> create the 3D view columns
            m_scenesManager.SPScene.setTimelinesData(patient, columnsData);

            // 3.1) define the selected plot of all the columns of the scene
            m_scenesManager.SPScene.defineSelectedPlot(idPlotSelected);

            // 3.2) define the maks of all the columns of the scene
            m_scenesManager.SPScene.setColumnsPlotsMasks(blacklistMasks, excludedMasks, hightLightedMasks);

            // 4) update the current selected column of the scene
            m_scenesManager.defineSelectedColumn(true, 0);

            m_UIManager.updateFocusedSceneAndColumn(true, 0);

            m_scenesManager.displayMessageInScene(true, "Individual scene loaded. ", 2f, 150, 80);


            return true;
        }


        /// <summary>
        /// Reload the MP scene
        /// </summary>
        /// <param name="data"></param>
        public bool setMPData(HBP.Data.Visualisation.MultiPatientsVisualisationData data)
        {
            List<string> ptsFiles = new List<string>(data.GetImplantation().Length), namePatients = new List<string>(data.GetImplantation().Length);
            for (int ii = 0; ii < data.GetImplantation().Length; ++ii)
            {
                ptsFiles.Add(data.GetImplantation()[ii]);
                namePatients.Add(data.Patients[ii].Place + "_" + data.Patients[ii].Name);
            }

            // set paths
            bool success = m_scenesManager.MPScene.reset(data);
            if (!success)
            {
                Debug.LogError("-ERROR : ScenesManager::setMPData -> aborted. ");
                return false;
            }


            // retrieve column data
            List<HBP.Data.Visualisation.ColumnData> columnsData = data.Columns.ToList<HBP.Data.Visualisation.ColumnData>();

            // 1) define the number of cameras for the scene
            m_scenesManager.defineConditionColumnsCameras(false, data.Columns.Count);

            // 2) define the number of UI overlays and UI cameras for the scene
            m_UIManager.setIEEGColumns(false, columnsData);

            // 3) define the iEEG data of the scene -> create the 3D view columns
            m_scenesManager.MPScene.setTimelinesData(data.Patients.ToList<HBP.Data.Patient.Patient>(), columnsData, data.GetImplantation().Cast<string>().ToList());

            // 4) update the current selected column of the scene
            m_scenesManager.defineSelectedColumn(false, 0);

            m_UIManager.updateFocusedSceneAndColumn(false, 0);

            m_scenesManager.displayMessageInScene(false, "MNI scene loaded. ", 2f, 150, 80);

            return true;
        }

        /// <summary>
        /// Add an IRMF column
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public bool addIRMF(bool spScene)
        {
            string IRMFPath;
            if (!m_scenesManager.loadIRMFDialog(spScene, out IRMFPath))
                return false;

            m_UIManager.addIRMFColumn(spScene);

            string[] split = IRMFPath.Split(new Char[] { '/', '\\' });
            string IMRFLabel = split[split.Length - 1];

            return m_scenesManager.addIRMFColumn(spScene, IMRFLabel);
        }

        /// <summary>
        /// Remove the last IRMF column
        /// </summary>
        /// <param name="spScene"></param>
        public void removeLastIRMF(bool spScene)
        {
            if (m_scenesManager.IRMFColumnsNumber(spScene) == 0)
                return;

            m_UIManager.removeLastIRMFColumn(spScene);
            m_scenesManager.removeLastIRMFColumn(spScene);
        }


        public void setSceneRatio(float ratio)
        {
            bool isSPSceneDisplayed = (ratio > 0.2f);
            bool isMPSceneDisplayed = (ratio < 0.8f);

            m_UIManager.UIOverlayManager.setOverlaySceneVisibility(isSPSceneDisplayed, true);
            m_UIManager.UIOverlayManager.setOverlaySceneVisibility(isMPSceneDisplayed, false);

            m_scenesManager.SPPanel.transform.parent.gameObject.SetActive(isSPSceneDisplayed);
            m_scenesManager.MPPanel.transform.parent.gameObject.SetActive(isMPSceneDisplayed);

            m_UIManager.UICameraManager.RightSceneButtonsController.SPButtons.gameObject.SetActive(isSPSceneDisplayed);
            m_UIManager.UICameraManager.RightSceneButtonsController.MPButtons.gameObject.SetActive(isMPSceneDisplayed);

            m_scenesManager.SPCameras.SetActive(isSPSceneDisplayed);
            m_scenesManager.MPCameras.SetActive(isMPSceneDisplayed);


            m_scenesManager.SPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = ratio;
            m_scenesManager.MPPanel.transform.parent.gameObject.GetComponent<LayoutElement>().flexibleHeight = 1f - ratio;
        }


        #endregion functions

    }
}