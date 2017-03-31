
/**
 * \file    BaseController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UIOverlay, BaseOverlayController,IndividualSceneOverlayController,BothScenesOverlayController classes
 */

// unity
using UnityEngine;

// hbp
using HBP.VISU3D.Cam;


namespace HBP.VISU3D
{
    /// <summary>
    /// UI overlay activity manager
    /// </summary>
    public class UIOverlay
    {
        public void setActivity(bool activity)
        {
            mainUITransform.gameObject.SetActive(activity);
        }

        public Transform mainUITransform;
    }

    /// <summary>
    /// Base for all overlays controllers
    /// </summary>
    abstract public class BaseOverlayController : MonoBehaviour
    {
        #region members

        protected Camera m_backGroundCamera;
        protected CamerasManager m_camerasManager;
        public Transform m_canvasOverlayParent = null;

        #endregion members

        #region functions

        public abstract void update_UI_position();

        public abstract void update_UI();



        #endregion functions
    }


    /// <summary>
    /// Base for individual scene overlay controllers
    /// </summary>
    abstract public class IndividualSceneOverlayController : BaseOverlayController
    {
        #region members

        protected bool m_isSPScene;
        protected bool isVisibleFromScene = true;
        protected bool isEnoughtRoom = true;
        protected bool currentActivity = false;
        protected Mode currentMode = null;
        protected Base3DScene m_scene = null;

        #endregion members

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="camerasManager"></param>
        public void init(Base3DScene scene, CamerasManager camerasManager)
        {
            m_scene = scene;
            m_backGroundCamera = camerasManager.background_camera();
            m_camerasManager = camerasManager;

            m_isSPScene = (m_scene.name == "SP");
        }

        /// <summary>
        /// Set if the UI is visibile in the current scene
        /// </summary>
        /// <param name="visible"></param>
        public void set_UI_visibility_from_scene(bool visible)
        {
            isVisibleFromScene = visible;
            update_UI();
        }

        /// <summary>
        /// Update UI with activity and the current mode
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="mode"></param>
        public void set_UI_activity(bool activity, Mode mode)
        {
            currentActivity = activity;
            currentMode = mode;
            update_UI();
        }


        #endregion functions
    }

    /// <summary>
    /// Base for both scenes overlay controllers
    /// </summary>
    abstract public class BothScenesOverlayController : BaseOverlayController
    {
        protected bool isVisibleFromSPScene = true;
        protected bool isVisibleFromMPScene = true;
        protected bool isEnoughtRoomSPScene = true;
        protected bool isEnoughtRoomMPScene = true;

        protected bool currentSPActivity = false;
        protected bool currentMPActivity = false;
        protected Mode currentSPMode = null;
        protected Mode currentMPMode = null;

        protected SP3DScene m_spScene;
        protected MP3DScene m_mpScene;        

        public void init(ScenesManager scenesManager)
        {
            m_spScene = scenesManager.SPScene;
            m_mpScene = scenesManager.MPScene;
            m_camerasManager = scenesManager.CamerasManager;
            m_backGroundCamera = m_camerasManager.background_camera();            
        }

        /// <summary>
        /// Update UI with activity and the current mode
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="mode"></param>
        public void set_UI_activity(bool activity, Mode mode)
        {
            if(mode.m_sceneSp)
            {
                currentSPActivity = activity;
                currentSPMode = mode;
            }
            else
            {
                currentMPActivity = activity;
                currentMPMode = mode;
            }

            update_UI();
        }


        /// <summary>
        /// Set if the UI is visibile in the current scene
        /// </summary>
        /// <param name="visible"></param>
        public void set_UI_visibility_from_scene(bool spScene, bool visible)
        {
            if(spScene)
                isVisibleFromSPScene = visible;
            else
                isVisibleFromMPScene = visible;

            update_UI();
        }
        
    }
}