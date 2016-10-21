
/**
 * \file    InputsSceneManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define InputsSceneManager class
 */

// system
using System.Collections;

// unity
using UnityEngine;


namespace HBP.VISU3D.Cam
{
    /// <summary>
    /// A manager class used to retrieve mouse and keyboards inputs events from cameras and apply it to the scenes
    /// </summary>
    public class InputsSceneManager : MonoBehaviour
    {
        #region members

        private ScenesManager m_scenesManager = null;
        private UIManager m_UIManager = null;

        #endregion members

        #region mono_behaviour


        void Awake()
        {
            m_scenesManager = StaticVisuComponents.ScenesManager;
            m_UIManager = StaticVisuComponents.UIManager;
        }

        #endregion mono_behaviour


        #region others

        /// <summary>
        /// Send a click ray to a scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void sendClickRayToScenes(Ray ray, bool spScene, int idColumn)
        {
            if (m_UIManager.UIOverlayManager.checkIfClickOnOverlay(spScene))
                return; // click on overlay, don't propagate to the scenes

            m_UIManager.updateFocusedSceneAndColumn(spScene, idColumn);

            if (spScene)
            {                
                m_scenesManager.SPScene.updateSelectedColumn(idColumn);                
                m_scenesManager.SPScene.clickOnScene(ray);
            }
            else
            {
                m_scenesManager.MPScene.updateSelectedColumn(idColumn);   
                m_scenesManager.MPScene.clickOnScene(ray);
            }
        }

        /// <summary>
        /// Send a movment ray to a scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="spScene"></param>
        /// <param name="mousePosition"></param>
        /// <param name="idColumn"></param>
        public void sendMouseMovementToScenes(Ray ray, bool spScene, Vector3 mousePosition, int idColumn)
        {
            if (m_UIManager.UIOverlayManager.checkIfClickOnOverlay(spScene))
            {
                if (spScene)
                    m_scenesManager.SPScene.disablePlotDisplayWindows(idColumn);
                else
                    m_scenesManager.MPScene.disablePlotDisplayWindows(idColumn);

                return; // click on overlay, don't propagate to the scenes
            }

            if (spScene)
                m_scenesManager.SPScene.moveMouseOnScene(ray, mousePosition, idColumn);
            else
                m_scenesManager.MPScene.moveMouseOnScene(ray, mousePosition, idColumn);
        }

        /// <summary>
        /// Send a scroll action to a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="mouseScrollDelta"></param>
        public void sendScrollMouseToScenes(bool spScene, Vector2 mouseScrollDelta)
        {
            if (m_UIManager.UIOverlayManager.checkIfClickOnOverlay(spScene))
                return; // click on overlay, don't propagate to the scenes

            if (spScene)
                m_scenesManager.SPScene.mouseScrollAction(mouseScrollDelta);
            else
                m_scenesManager.MPScene.mouseScrollAction(mouseScrollDelta);
        }

        /// <summary>
        /// Send a keyboard action to the scenes
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="key"></param>
        public void sendKeyboardActionToScene(bool spScene, KeyCode key)
        {
            if (m_UIManager.UIOverlayManager.checkIfClickOnOverlay(spScene))
                return; // click on overlay, don't propagate to the scenes

            if (spScene)
                m_scenesManager.SPScene.keyboardAction(key);
            else
                m_scenesManager.MPScene.keyboardAction(key);
        }

        #endregion others
    }

}