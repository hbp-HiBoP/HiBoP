/**
 * \file    UICameraManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UICameraManager class
 */

using System.Collections.Generic;
using UnityEngine;

namespace HBP.VISU3D
{
    interface UICameraOverlay
    {
        void UpdateByMode(Mode mode);
    }

    /// <summary>
    /// Manager for the UI in the screen space
    /// </summary>
    public class UICameraManager : MonoBehaviour
    {
        #region members

        private ScenesRatioController m_scenesRatioController = null;
        private ToolsMenu m_topPanelMenuController = null;
        private ButtonsLeftMenuController m_buttonsLeftMenuController = null;
        private SPLeftMenuController m_SPLeftMenuController = null;
        private MPLeftMenuController m_MPLeftMenuController = null;
        private GlobalMenuController m_globalMenuController = null;

        #endregion members

        #region mono_behaviour

        void Awake()
        {            
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.UICameraManager);

            // retrieve controllers
            m_globalMenuController = transform.Find("global menu").gameObject.GetComponent<GlobalMenuController>();
            m_scenesRatioController = transform.Find("scenes ratio").gameObject.GetComponent<ScenesRatioController>();
            m_topPanelMenuController = GameObject.Find("Tools Menu").GetComponent<ToolsMenu>();
            m_buttonsLeftMenuController = transform.Find("buttons left menu").gameObject.GetComponent<ButtonsLeftMenuController>();
            m_SPLeftMenuController = transform.Find("sp left menues").gameObject.GetComponent<SPLeftMenuController>();
            m_MPLeftMenuController = transform.Find("mp left menues").gameObject.GetComponent<MPLeftMenuController>();            
            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.UICameraManager, gameObject);
        }

        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Init all controllers
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            // init controllers
            m_globalMenuController.init(scenesManager);
            m_SPLeftMenuController.init(scenesManager);
            m_MPLeftMenuController.init(scenesManager);
            m_topPanelMenuController.Init(scenesManager);
            m_scenesRatioController.init(scenesManager);
            m_buttonsLeftMenuController.init(scenesManager);            
        }

        /// <summary>
        /// Apply the UI state corresponding to the input mode to all the controllers
        /// </summary>
        /// <param name="mode"></param>
        public void update_UI_with_mode(Mode mode)
        {
            if(mode.m_sceneSp)
                m_SPLeftMenuController.update_UI_with_mode(mode);
            else
                m_MPLeftMenuController.update_UI_with_mode(mode);

            m_topPanelMenuController.UpdateByMode(mode);
            m_buttonsLeftMenuController.update_UI_with_mode(mode);
            m_scenesRatioController.UpdateByMode(mode);            
        }

        /// <summary>
        /// Define the number of IEEG columns for the input scene for all cameras overlays
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IEEGColumnsNb"></param>
        public void set_iEEG_columns_nb(bool spScene, List<HBP.Data.Visualisation.ColumnData> columnsData)
        {
            if (spScene)
                m_SPLeftMenuController.set_iEEG_columns_nb(columnsData.Count);
            else
                m_MPLeftMenuController.set_iEEG_columns_nb(columnsData.Count);

            m_topPanelMenuController.GenerateColumnsNames(spScene, columnsData);
        }

        /// <summary>
        /// Update the focused scene and column for all concerned UI
        /// </summary>
        /// <param name="scene"></param>
        public void UpdateFocusedSceneAndColumn(string nameScene, bool spScene, int columnId = -1)
        {
            m_topPanelMenuController.UpdateCurrentSceneInformations(nameScene, spScene, columnId);
            m_buttonsLeftMenuController.define_current_scene(spScene);

            if (spScene)
                m_SPLeftMenuController.update_focused_column(columnId);
            else
                m_MPLeftMenuController.update_focused_column(columnId);

            m_SPLeftMenuController.set_menues_visibility(spScene);
            m_MPLeftMenuController.set_menues_visibility(!spScene);
        }

        /// <summary>
        /// Add an IRMF column camera UI
        /// </summary>
        /// <param name="spScene"></param>
        public void add_fMRI_column(bool spScene, string label)
        {
            if(spScene)
                m_SPLeftMenuController.add_fMRI_column();
            else
                m_MPLeftMenuController.add_fMRI_column();

            m_topPanelMenuController.AddColumnName(spScene, label);
        }


        /// <summary>
        /// Remove an IRMF column camera ui
        /// </summary>
        /// <param name="spScene"></param>
        public void remove_last_fMRI_column(bool spScene)
        {
            if (spScene)
                m_SPLeftMenuController.remove_last_fMRI_column();
            else
                m_MPLeftMenuController.remove_last_fMRI_column();

            m_topPanelMenuController.RemoveLastColumnName(spScene);
        }

        #endregion functions


    }
}