
/**
 * \file    ScenesRatioController.cs
 * \author  Lance Florian
 * \date    04/05/2016
 * \brief   Define ScenesRatioController class
 */

// system
using System.Collections;

// unity
using UnityEngine;
using UnityEngine.UI;
using HBP.Module3D;

namespace HBP.UI.Module3D
{ 
    /// <summary>
    /// A class for managing the scenes ratio element in the UI
    /// </summary>
    public class ScenesRatioController : MonoBehaviour, UICameraOverlay
    {
        #region members

        public Transform m_sceneRatioTransform;  /**< scene ratio transform */

        private bool m_spSceneHasBeenActivated = false;
        private bool m_mpSceneHasBeenActivated = false;

        #endregion members

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void Initialize(ScenesManager scenesManager)
        {
            //  ratio slider   
            m_sceneRatioTransform.GetComponent<Slider>().interactable = false;
            m_sceneRatioTransform.GetComponent<Slider>().onValueChanged.AddListener((value) =>
            {
                StaticComponents.HBPCommand.set_ratio_scene(value);
                foreach (Base3DScene scene in scenesManager.Scenes)
                {
                    scene.set_current_mode_specifications();
                }
            });

            scenesManager.FocusOnScene.AddListener((spScene) =>
            {
                if (spScene)
                    m_spSceneHasBeenActivated = true;
                else
                    m_mpSceneHasBeenActivated = true;

                m_sceneRatioTransform.GetComponent<Slider>().interactable = m_spSceneHasBeenActivated && m_mpSceneHasBeenActivated;
            });
        }

        public void UpdateByMode(Mode mode)
        {
            // inused
        }


        #endregion functions
    }

}