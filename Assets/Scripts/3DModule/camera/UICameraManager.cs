

/**
 * \file    UICameraManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UICameraManager class
 */

// system
using System.Collections;

// unity
using UnityEngine;

// hbp
using HBP.VISU3D.Cam;

namespace HBP.VISU3D
{
    interface UICameraOverlay
    {
        void setUIActivity(Mode mode);

        void init(ScenesManager scenesManager);
    }

    /// <summary>
    /// Manager for the UI in the screen space
    /// </summary>
    public class UICameraManager : MonoBehaviour
    {
        #region members

        private Canvas m_canvas = null; /**< screen space camera canvas */
        public Canvas Canvas
        {
            get { return m_canvas; }
        }


        private RightMenuController m_rightSceneButtonsController = null;
        public RightMenuController RightSceneButtonsController {  get { return m_rightSceneButtonsController; } }

        private ButtonsLeftMenuController m_buttonsLeftMenuController = null;
        private TopMenuController m_topPanelMenuController = null;
        private SceneMenuController m_sceneMenuController = null;
        private iEEGMenuController m_iEEGMenuController = null;
        private PlotMenuController m_plotMenuController = null;
        private ROIMenuController m_ROIMenuController = null;
        private IRMFMenuController m_IRMFMenuController = null;
        private ScenesRatioController m_scenesRatioController = null;

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {            
            int idScript = TimeExecution.getId();
            TimeExecution.startAwake(idScript, TimeExecution.ScriptsId.UICameraManager);

            // retrieve controllers
            m_sceneMenuController = transform.Find("scene menu").gameObject.GetComponent<SceneMenuController>();
            m_rightSceneButtonsController = transform.Find("right scene buttons").gameObject.GetComponent<RightMenuController>();
            m_topPanelMenuController = transform.Find("top panel menu").gameObject.GetComponent<TopMenuController>();
            m_buttonsLeftMenuController = transform.Find("buttons left menu").gameObject.GetComponent<ButtonsLeftMenuController>();            
            m_iEEGMenuController = transform.Find("iEEG menu").gameObject.GetComponent<iEEGMenuController>();
            m_plotMenuController = transform.Find("Plot menu").gameObject.GetComponent<PlotMenuController>();            
            m_ROIMenuController = transform.Find("ROI menu").gameObject.GetComponent<ROIMenuController>();
            m_scenesRatioController = transform.Find("scenes ratio").gameObject.GetComponent<ScenesRatioController>();
            m_IRMFMenuController = transform.Find("IRMF menu").gameObject.GetComponent<IRMFMenuController>();

            TimeExecution.endAwake(idScript, TimeExecution.ScriptsId.UICameraManager, gameObject);
        }

        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Init all controllers
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager, Canvas camera)
        {
            // retrieve canvas
            m_canvas = camera;

            // init controllers
            m_sceneMenuController.init(scenesManager);
            m_rightSceneButtonsController.init(scenesManager);
            m_topPanelMenuController.init(scenesManager);
            m_iEEGMenuController.init(scenesManager);
            m_ROIMenuController.init(scenesManager);
            m_scenesRatioController.init(scenesManager);
            m_IRMFMenuController.init(scenesManager);
            m_plotMenuController.init(scenesManager);
            m_buttonsLeftMenuController.init(scenesManager);
        }

        /// <summary>
        /// Apply the UI state corresponding to the input mode to all the controllers
        /// </summary>
        /// <param name="mode"></param>
        public void applyUIState(Mode mode)
        {
            m_sceneMenuController.setUIActivity(mode);
            m_rightSceneButtonsController.setUIActivity(mode);
            m_topPanelMenuController.setUIActivity(mode);
            m_buttonsLeftMenuController.setUIActivity(mode);
            m_iEEGMenuController.setUIActivity(mode);
            m_ROIMenuController.setUIActivity(mode);
            m_scenesRatioController.setUIActivity(mode);            
            m_IRMFMenuController.setUIActivity(mode);
            m_plotMenuController.setUIActivity(mode);
        }

        /// <summary>
        /// Define the number of IEEG columns for the input scene for all cameras overlays
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="IEEGColumnsNb"></param>
        /// <param name="IRMFConumnsNb"></param>
        public void setIEEGColumnsNb(bool spScene, int IEEGColumnsNb)
        {
            m_iEEGMenuController.defineColumnsNb(spScene, IEEGColumnsNb);
            m_plotMenuController.defineColumnsNb(spScene, IEEGColumnsNb);

            if (!spScene)
                m_ROIMenuController.defineColumnsNb(IEEGColumnsNb);
        }

        /// <summary>
        /// Update the focused scene and column for all concerned UI
        /// </summary>
        /// <param name="scene"></param>
        public void updateFocusedSceneAndColumn(bool spScene, int columnId = -1)
        {
            m_sceneMenuController.defineCurrentMenu(spScene);
            m_iEEGMenuController.defineCurrentMenu(spScene, columnId);
            m_topPanelMenuController.updateCurrentSceneAndColumnName(spScene, columnId);
            m_buttonsLeftMenuController.defineCurrentScene(spScene);

            if (!spScene) // only for mp scene
                m_ROIMenuController.defineCurrentMenu(columnId);

            m_IRMFMenuController.defineCurrentMenu(spScene, columnId);
            m_plotMenuController.defineCurrentMenu(spScene, columnId);
        }

        /// <summary>
        /// Add an IRMF column camera ui
        /// </summary>
        /// <param name="spScene"></param>
        public void addIRMFColumn(bool spScene)
        {
            if (!spScene)
            {
                m_ROIMenuController.addMenu();
            }

            m_IRMFMenuController.addMenu(spScene);
            m_plotMenuController.addMenu(spScene);
        }


        /// <summary>
        /// Remove an IRMF column camera ui
        /// </summary>
        /// <param name="spScene"></param>
        public void removeLastIRMFColumn(bool spScene)
        {
            if (!spScene)
            {
                m_ROIMenuController.removeLastMenu();
            }

            m_IRMFMenuController.removeLastMenu(spScene);
            m_plotMenuController.removeLastMenu(spScene);
        }

        #endregion functions


    }
}