
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


namespace HBP.Module3D.Cam
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
        public void send_click_ray_to_scenes(Ray ray, Base3DScene scene, int idColumn)
        {
            switch (scene.Type)
            {
                case SceneType.SinglePatient:
                    if (m_UIManager.OverlayManager.check_if_click_on_overlay(scene.Type)) return;
                    m_scenesManager.SinglePatientScene.update_selected_column(idColumn);
                    m_scenesManager.SinglePatientScene.click_on_scene(ray);
                    break;
                case SceneType.MultiPatients:
                    if (m_UIManager.OverlayManager.check_if_click_on_overlay(scene.Type)) return;
                    m_scenesManager.MultiPatientsScene.update_selected_column(idColumn);
                    m_scenesManager.MultiPatientsScene.click_on_scene(ray);
                    break;
                default:
                    break;
            }
            m_UIManager.UpdateFocusedSceneAndColumn(scene, idColumn);
        }

        /// <summary>
        /// Send a movment ray to a scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="spScene"></param>
        /// <param name="mousePosition"></param>
        /// <param name="idColumn"></param>
        public void send_mouse_movement_to_scenes(Ray ray, SceneType type, Vector3 mousePosition, int idColumn)
        {
            if (m_UIManager.OverlayManager.check_if_click_on_overlay(type))
            {
                switch (type)
                {
                    case SceneType.SinglePatient:
                        m_scenesManager.SinglePatientScene.disable_plot_display_window(idColumn);
                        break;
                    case SceneType.MultiPatients:
                        m_scenesManager.MultiPatientsScene.disable_site_display_window(idColumn);
                        break;
                    default:
                        break;
                }
                return; // click on overlay, don't propagate to the scenes
            }
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_scenesManager.SinglePatientScene.move_mouse_on_scene(ray, mousePosition, idColumn);
                    break;
                case SceneType.MultiPatients:
                    m_scenesManager.MultiPatientsScene.move_mouse_on_scene(ray, mousePosition, idColumn);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Send a scroll action to a scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="mouseScrollDelta"></param>
        public void send_scroll_mouse_to_scenes(SceneType type, Vector2 mouseScrollDelta)
        {
            if (m_UIManager.OverlayManager.check_if_click_on_overlay(type))
                return; // click on overlay, don't propagate to the scenes

            switch (type)
            {
                case SceneType.SinglePatient:
                    m_scenesManager.SinglePatientScene.mouse_scroll_action(mouseScrollDelta);
                    break;
                case SceneType.MultiPatients:
                    m_scenesManager.MultiPatientsScene.mouse_scroll_action(mouseScrollDelta);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Send a keyboard action to the scenes
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="key"></param>
        public void send_keyboard_action_to_scenes(SceneType type, KeyCode key)
        {
            if (m_UIManager.OverlayManager.check_if_click_on_overlay(type))
                return; // click on overlay, don't propagate to the scenes

            switch (type)
            {
                case SceneType.SinglePatient:
                    m_scenesManager.SinglePatientScene.keyboard_action(key);
                    break;
                case SceneType.MultiPatients:
                    m_scenesManager.MultiPatientsScene.keyboard_action(key);
                    break;
                default:
                    break;
            }
        }

        #endregion others
    }

}