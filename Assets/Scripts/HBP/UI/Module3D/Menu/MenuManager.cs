/**
 * \file    UICameraManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UICameraManager class
 */

using System.Collections.Generic;
using UnityEngine;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    interface UICameraOverlay
    {
        void UpdateByMode(Mode mode);
    }

    /// <summary>
    /// Manager for the UI in the screen space
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        #region members

        private ScenesRatioController m_scenesRatioController = null;
        private ToolsMenu m_topPanelMenuController = null;
        private ButtonsLeftMenuController m_buttonsLeftMenuController = null;
        #endregion members

        #region mono_behaviour

        void Awake()
        {            
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.UICameraManager);
            m_topPanelMenuController = GameObject.Find("Tools Menu").GetComponent<ToolsMenu>();
            m_buttonsLeftMenuController = transform.FindChild("Left").Find("button menu parent").gameObject.GetComponent<ButtonsLeftMenuController>();        
            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.UICameraManager, gameObject);
        }

        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Init all controllers
        /// </summary>
        /// <param name="scenesManager"></param>
        public void Initialize(ScenesManager scenesManager)
        {
            // init controllers
            m_buttonsLeftMenuController.GlobalMenuController.Initialize(scenesManager);
            m_buttonsLeftMenuController.SinglePatientLeftMenuController.Initialize(scenesManager);
            m_buttonsLeftMenuController.MultiPatientsLeftMenuController.Initialize(scenesManager);
            m_topPanelMenuController.Initialize(scenesManager);
            m_scenesRatioController.Initialize(scenesManager);
            m_buttonsLeftMenuController.Initialize(scenesManager);            
        }

        /// <summary>
        /// Apply the UI state corresponding to the input mode to all the controllers
        /// </summary>
        /// <param name="mode"></param>
        public void update_UI_with_mode(Mode mode)
        {
            switch (mode.m_Type)
            {
                case SceneType.SinglePatient:
                    m_buttonsLeftMenuController.SinglePatientLeftMenuController.update_UI_with_mode(mode);
                    break;
                case SceneType.MultiPatients:
                    m_buttonsLeftMenuController.MultiPatientsLeftMenuController.update_UI_with_mode(mode);
                    break;
                default:
                    break;
            }
            m_topPanelMenuController.UpdateByMode(mode);
            m_scenesRatioController.UpdateByMode(mode);            
        }

        /// <summary>
        /// Define the number of IEEG columns for the input scene for all cameras overlays
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IEEGColumnsNb"></param>
        public void SetiEEGColumnsNb(SceneType type, List<HBP.Data.Visualization.ColumnData> columnsData)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_buttonsLeftMenuController.SinglePatientLeftMenuController.set_iEEG_columns_nb(columnsData.Count);
                    break;
                case SceneType.MultiPatients:
                    m_buttonsLeftMenuController.MultiPatientsLeftMenuController.set_iEEG_columns_nb(columnsData.Count);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Update the focused scene and column for all concerned UI
        /// </summary>
        /// <param name="scene"></param>
        public void UpdateFocusedSceneAndColumn(Base3DScene scene, int column = -1)
        {
            switch (scene.Type)
            {
                case SceneType.SinglePatient:
                    m_buttonsLeftMenuController.SinglePatientLeftMenuController.update_focused_column(column);
                    break;
                case SceneType.MultiPatients:
                    m_buttonsLeftMenuController.MultiPatientsLeftMenuController.update_focused_column(column);
                    break;
                default:
                    break;
            }
            m_buttonsLeftMenuController.SinglePatientLeftMenuController.SetMenuesVisibility(scene.Type == SceneType.SinglePatient);
            m_buttonsLeftMenuController.MultiPatientsLeftMenuController.SetMenuesVisibility(scene.Type == SceneType.MultiPatients);
        }

        /// <summary>
        /// Add an IRMF column camera UI
        /// </summary>
        /// <param name="spScene"></param>
        public void AddfMRIColumn(SceneType type, string label)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_buttonsLeftMenuController.SinglePatientLeftMenuController.add_fMRI_column();
                    break;
                case SceneType.MultiPatients:
                    m_buttonsLeftMenuController.MultiPatientsLeftMenuController.add_fMRI_column();
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Remove an IRMF column camera ui
        /// </summary>
        /// <param name="spScene"></param>
        public void RemoveLastfMRIColumn(SceneType type)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_buttonsLeftMenuController.SinglePatientLeftMenuController.RemoveLastfMRIColumn();
                    break;
                case SceneType.MultiPatients:
                    m_buttonsLeftMenuController.MultiPatientsLeftMenuController.RemoveLastfMRIColumn();
                    break;
                default:
                    break;
            }
        }

        #endregion functions


    }
}