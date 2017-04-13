
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
            m_scenesManager = StaticComponents.ScenesManager;
            m_UIManager = StaticComponents.UIManager;
        }

        #endregion mono_behaviour


        #region others

        /// <summary>
        /// Send a click ray to a scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void send_click_ray_to_scenes(Ray ray, bool spScene, int idColumn)
        {
            if (m_UIManager.UIOverlayManager.check_if_click_on_overlay(spScene))
                return; // click on overlay, don't propagate to the scenes

            m_UIManager.update_focused_scene_and_column("", spScene, idColumn);
            if (spScene)
            {                
                m_scenesManager.SPScene.update_selected_column(idColumn);                
                m_scenesManager.SPScene.click_on_scene(ray);
            }
            else
            {                
                m_scenesManager.MPScene.update_selected_column(idColumn);   
                m_scenesManager.MPScene.click_on_scene(ray);
            }
        }

        /// <summary>
        /// Send a movment ray to a scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="spScene"></param>
        /// <param name="mousePosition"></param>
        /// <param name="idColumn"></param>
        public void send_mouse_movement_to_scenes(Ray ray, bool spScene, Vector3 mousePosition, int idColumn)
        {
            if (m_UIManager.UIOverlayManager.check_if_click_on_overlay(spScene))
            {
                if (spScene)
                    m_scenesManager.SPScene.disable_plot_display_window(idColumn);
                else
                    m_scenesManager.MPScene.disable_site_display_window(idColumn);

                return; // click on overlay, don't propagate to the scenes
            }

            if (spScene)
                m_scenesManager.SPScene.move_mouse_on_scene(ray, mousePosition, idColumn);
            else
                m_scenesManager.MPScene.move_mouse_on_scene(ray, mousePosition, idColumn);
        }

        /// <summary>
        /// Send a scroll action to a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="mouseScrollDelta"></param>
        public void send_scroll_mouse_to_scenes(bool spScene, Vector2 mouseScrollDelta)
        {
            if (m_UIManager.UIOverlayManager.check_if_click_on_overlay(spScene))
                return; // click on overlay, don't propagate to the scenes

            if (spScene)
                m_scenesManager.SPScene.mouse_scroll_action(mouseScrollDelta);
            else
                m_scenesManager.MPScene.mouse_scroll_action(mouseScrollDelta);
        }

        /// <summary>
        /// Send a keyboard action to the scenes
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="key"></param>
        public void send_keyboard_action_to_scenes(bool spScene, KeyCode key)
        {
            if (m_UIManager.UIOverlayManager.check_if_click_on_overlay(spScene))
                return; // click on overlay, don't propagate to the scenes

            if (spScene)
                m_scenesManager.SPScene.keyboard_action(key);
            else
                m_scenesManager.MPScene.keyboard_action(key);
        }
        
        public bool GetCurrentFocusedScene()
        {
            return m_UIManager.CurrentFocusedScene;
        }
        #endregion others
    }

}