
/**
 * \file    TopMenuController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TopMenuController class
 */

// system
using System.Collections;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{ 
    /// <summary>
    /// A class for managing the top menu in the UI
    /// </summary>
    public class TopMenuController : MonoBehaviour, UICameraOverlay
    {
        #region members
        
        private bool m_currentScene;
        private Mode m_currentMode = null;

        private SP3DScene m_spScene = null; /**< SP scene */
        private MP3DScene m_mpScene = null; /**< MP scene */

        public Transform m_topPanelMenu;  /**< top panel of the menu transform */

        #endregion members

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            // retrieve scenes
            m_spScene = scenesManager.SPScene;
            m_mpScene = scenesManager.MPScene;
            // ...
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void updateCurrentSceneAndColumnName(bool spScene, int idColumn)
        {
            m_currentScene = spScene;
            m_topPanelMenu.Find("left part").Find("scene name panel").Find("Text").GetComponent<Text>().text = spScene ? "Invidivual scene" : "MNI scene";
            m_topPanelMenu.Find("left part").Find("column name panel").Find("Text").GetComponent<Text>().text = "Column " + (idColumn+1);
        }

        /// <summary>
        /// Update the UI components states with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void setUIActivity(Mode mode)
        {
            m_currentMode = mode;
            updateUI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        void updateUI()
        {
            // ...
        }

        #endregion functions
    }

}