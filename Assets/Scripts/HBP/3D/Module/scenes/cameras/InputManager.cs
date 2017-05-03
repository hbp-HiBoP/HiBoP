
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
using HBP.UI.Module3D;


namespace HBP.Interaction
{
    /// <summary>
    /// A manager class used to retrieve mouse and keyboards inputs events from cameras and apply it to the scenes
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Properties
        private Module3D.ScenesManager m_scenesManager = null;
        private UIManager m_UIManager = null;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_scenesManager = Module3D.StaticComponents.ScenesManager;
            m_UIManager = Module3D.StaticComponents.UIManager;
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Send a click ray to a scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void SendClickRayToScenes(Ray ray, Module3D.Base3DScene scene, int idColumn)
        {
            if (m_UIManager.OverlayManager.check_if_click_on_overlay(scene.Type)) return;
            scene.UpdateSelectedColumn(idColumn);
            scene.ClickOnScene(ray);
            m_UIManager.UpdateFocusedSceneAndColumn(scene, idColumn);
        }
        /// <summary>
        /// Send a movment ray to a scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="spScene"></param>
        /// <param name="mousePosition"></param>
        /// <param name="idColumn"></param>
        public void SendMouseMovementToScenes(Ray ray, Module3D.Base3DScene scene, Vector3 mousePosition, int idColumn)
        {
            if (m_UIManager.OverlayManager.check_if_click_on_overlay(scene.Type))
            {
                scene.DisableSiteDisplayWindow(idColumn);
                return;
            }
            else
            {
                scene.MoveMouseOnScene(ray, mousePosition, idColumn);
            }
        }
        /// <summary>
        /// Send a scroll action to a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="mouseScrollDelta"></param>
        public void SendScrollMouseToScenes(Module3D.Base3DScene scene, Vector2 mouseScrollDelta)
        {
            if (m_UIManager.OverlayManager.check_if_click_on_overlay(scene.Type))
                return; // click on overlay, don't propagate to the scenes
            scene.MouseScrollAction(mouseScrollDelta);
        }
        /// <summary>
        /// Send a keyboard action to the scenes
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="key"></param>
        public void SendKeyboardActionToScenes(Module3D.Base3DScene scene, KeyCode key)
        {
            if (m_UIManager.OverlayManager.check_if_click_on_overlay(scene.Type))
                return; // click on overlay, don't propagate to the scenes
            scene.KeyboardAction(key);
        }
        #endregion
    }
}