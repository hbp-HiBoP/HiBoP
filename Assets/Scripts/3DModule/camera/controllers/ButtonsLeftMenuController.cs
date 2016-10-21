
/**
 * \file    ButtonsLeftMenuController.cs
 * \author  Lance Florian
 * \date    30/08/2016
 * \brief   Define ButtonsLeftMenuController class
 */

// system
using System.Collections;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for managing the left buttons menu in the UI
    /// </summary>
    public class ButtonsLeftMenuController : MonoBehaviour
    {
        #region members

        public Transform m_leftButtonParent = null;
        public Transform m_buttonsListParent = null;

        private Button m_globalButton = null;
        private Button m_sceneButton = null;
        private Button m_iEEGButton = null;
        private Button m_IRMFButton = null;

        private bool m_currentScene;
        private Mode m_currentMode = null;
        private SP3DScene m_spScene = null; /**< SP scene */
        private MP3DScene m_mpScene = null; /**< MP scene */

        public iEEGMenuController iEEGMenuController = null;
        public IRMFMenuController IRMFMenuController = null;
        public SceneMenuController generalMenUController = null;

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

            m_buttonsListParent = m_leftButtonParent.Find("button menu list");

            // listeners
            // scene switch
            m_globalButton = m_buttonsListParent.Find("Global button").GetComponent<Button>();
            m_globalButton.onClick.AddListener(delegate
            {
                //globalMenuController.switchUIVisibility();
            });
            // scene switch
            m_sceneButton = m_buttonsListParent.Find("Scene button").GetComponent<Button>();
            m_sceneButton.onClick.AddListener(delegate
            {
                generalMenUController.switchUIVisibility();
            });
            //  IEEG switch
            m_iEEGButton = m_buttonsListParent.Find("iEEG button").GetComponent<Button>();
            m_iEEGButton.onClick.AddListener(delegate
            {
                iEEGMenuController.switchUIVisibility();
            });
            //  IRMF switch
            m_IRMFButton = m_buttonsListParent.Find("IRMF button").GetComponent<Button>();
            m_IRMFButton.onClick.AddListener(delegate
            {
                IRMFMenuController.switchUIVisibility();
            });
        }

        /// <summary>
        /// Define the current scene
        /// </summary>
        /// <param name="spScene"></param>
        public void defineCurrentScene(bool spScene)
        {
            m_currentScene = spScene;
            updateUI();
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
            bool iEEGButtonEnabled = false;
            bool IRMFButtonEnabled = false;

            if (m_currentMode != null)
            {
                switch (m_currentMode.m_idMode)
                {
                    case Mode.ModesId.NoPathDefined:
                        m_sceneButton.interactable = false;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = false;
                        break;
                    case Mode.ModesId.MinPathDefined:
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.AllPathDefined:
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.ComputingAmplitudes:
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.AmplitudesComputed:
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.TriErasing:
                        m_sceneButton.interactable = false;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = false;
                        break;
                    case Mode.ModesId.AmpNeedUpdate:
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.Error:
                        m_sceneButton.interactable = false;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = false;
                        break;
                }
            }

            bool isIRMF = m_currentScene ? m_spScene.CM.isIRMFCurrentColumn() : m_mpScene.CM.isIRMFCurrentColumn();
            m_iEEGButton.interactable = !isIRMF && iEEGButtonEnabled;
            m_IRMFButton.interactable = isIRMF && IRMFButtonEnabled;

            Color orange = new Color(1f, 165 / 255f, 0f);
            
            m_iEEGButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
            m_IRMFButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
            m_sceneButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
            m_globalButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
        }

        #endregion functions
    }
}