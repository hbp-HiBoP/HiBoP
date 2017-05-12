﻿

/**
 * \file    SPLeftMenuController.cs
 * \author  Lance Florian
 * \date    23/01/2017
 * \brief   Define SPLeftMenuController class
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Controller for managing the left menu of the SP scene
    /// </summary>
    public class SPLeftMenuController : MonoBehaviour
    {
        #region Properties

        private Base3DScene m_scene = null;

        private SceneMenuController m_sceneMenuController = null;
        private iEEGMenuController m_iEEGMenuController = null;
        private SiteMenuController m_siteMenuController = null;
        private FMRIMenuController m_FMRIMenuController = null;

        public Transform m_menuesListTransorm = null; /**< scene menu list transform */

        #endregion

        #region Private Methods

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve controllers
            m_sceneMenuController = transform.Find("scene").gameObject.GetComponent<SceneMenuController>();
            m_iEEGMenuController = transform.Find("iEEG").gameObject.GetComponent<iEEGMenuController>();
            m_siteMenuController = transform.Find("site").gameObject.GetComponent<SiteMenuController>();
            m_FMRIMenuController = transform.Find("fMRI").gameObject.GetComponent<FMRIMenuController>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Init all controllers
        /// </summary>
        /// <param name="scenesManager"></param>
        public void Initialize(ScenesManager scenesManager)
        {
            // init controllers
            //m_scene = (Base3DScene)scenesManager.SinglePatientScene;
            //m_sceneMenuController.init(m_scene, scenesManager.CamerasManager);
            m_iEEGMenuController.init(m_scene);
            m_FMRIMenuController.init(m_scene);
            m_siteMenuController.init(m_scene);            
        }

        /// <summary>
        /// Apply the UI state corresponding to the input mode to all the controllers
        /// </summary>
        /// <param name="mode"></param>
        public void update_UI_with_mode(Mode mode)
        {
            m_sceneMenuController.UpdateByMode(mode);
            m_iEEGMenuController.UpdateByMode(mode);
            m_FMRIMenuController.UpdateByMode(mode);
            m_siteMenuController.UpdateByMode(mode);
        }

        /// <summary>
        /// Define the number of IEEG columns for the input scene for all cameras overlays
        /// </summary>
        /// <param name="columnsData"></param>
        public void set_iEEG_columns_nb(int iEEGColumnsNb)
        {
            m_iEEGMenuController.define_columns_nb(iEEGColumnsNb);
            m_siteMenuController.define_columns_nb(iEEGColumnsNb);
        }

        /// <summary>
        /// Update the focused scene and column for all concerned UI
        /// </summary>
        /// <param name="columnId"></param>
        public void update_focused_column(int columnId = -1)
        {
            m_iEEGMenuController.define_current_column(columnId);
            m_FMRIMenuController.define_current_column(columnId);
            m_siteMenuController.define_current_column(columnId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visibility"></param>
        public void SetMenuesVisibility(bool visibility)
        {
            m_menuesListTransorm.gameObject.SetActive(visibility);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="button"></param>
        public void menu_clicked(LeftButtons.Type button)
        {
            switch(button)
            {
                case LeftButtons.Type.Scene:
                    m_sceneMenuController.switch_UI_visibility();
                    break;
                case LeftButtons.Type.iEEG:
                    m_iEEGMenuController.switch_UI_visibility();
                    break;
                case LeftButtons.Type.fMRI:
                    m_FMRIMenuController.switch_UI_visibility();
                    break;
            }
        }


        /// <summary>
        /// Add an IRMF column camera ui
        /// </summary>
        public void add_fMRI_column()
        {
            m_FMRIMenuController.add_menu();
            m_siteMenuController.add_menu();
        }


        /// <summary>
        /// Remove an IRMF column camera ui
        /// </summary>
        public void RemoveLastfMRIColumn()
        {
            m_FMRIMenuController.remove_last_menu();
            m_siteMenuController.remove_last_menu();
        }

        #endregion


    }
}