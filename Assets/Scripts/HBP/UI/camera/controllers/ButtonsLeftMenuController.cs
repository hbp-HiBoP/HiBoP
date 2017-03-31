
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
    public class LeftButtons
    {
        public enum Type : int
        {
            Global,Scene,iEEG,fMRI
        }; 
    }

    /// <summary>
    /// A class for managing the left buttons menu in the UI
    /// </summary>
    public class ButtonsLeftMenuController : MonoBehaviour
    {
        #region members       

        public Transform m_leftButtonParent = null;
        private Transform m_buttonsListParent = null;

        private Button m_globalButton = null;
        private Button m_sceneButton = null;
        private Button m_iEEGButton = null;
        private Button m_fMRIButton = null;

        private bool m_currentScene;
        private Mode m_currentMode = null;

        public SPLeftMenuController m_SPLeftMenuController = null;
        public MPLeftMenuController m_MPLeftMenuController = null;
        public GlobalMenuController m_globalMenuController = null;

        #endregion members
        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            m_buttonsListParent = m_leftButtonParent.Find("button menu list");

            // listeners
            // scene switch
            m_globalButton = m_buttonsListParent.Find("Global button").GetComponent<Button>();
            m_globalButton.onClick.AddListener(delegate
            {
                m_globalMenuController.switch_UI_Visibility();
            });
            // scene switch
            m_sceneButton = m_buttonsListParent.Find("Scene button").GetComponent<Button>();
            m_sceneButton.onClick.AddListener(delegate
            {
                if (m_currentScene)
                    m_SPLeftMenuController.menu_clicked(LeftButtons.Type.Scene);
                else
                    m_MPLeftMenuController.menu_clicked(LeftButtons.Type.Scene);
            });
            //  IEEG switch
            m_iEEGButton = m_buttonsListParent.Find("iEEG button").GetComponent<Button>();
            m_iEEGButton.onClick.AddListener(delegate
            {
                if (m_currentScene)
                    m_SPLeftMenuController.menu_clicked(LeftButtons.Type.iEEG);
                else
                    m_MPLeftMenuController.menu_clicked(LeftButtons.Type.iEEG);
            });
            //  fMRI switch
            m_fMRIButton = m_buttonsListParent.Find("fMRI button").GetComponent<Button>();
            m_fMRIButton.onClick.AddListener(delegate
            {
                if (m_currentScene)
                    m_SPLeftMenuController.menu_clicked(LeftButtons.Type.fMRI);
                else
                    m_MPLeftMenuController.menu_clicked(LeftButtons.Type.fMRI);
            });
        }

        /// <summary>
        /// Define the current scene
        /// </summary>
        /// <param name="spScene"></param>
        public void define_current_scene(bool spScene)
        {
            m_currentScene = spScene;
            update_UI();
        }

        /// <summary>
        /// Update the UI components states with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void update_UI_with_mode(Mode mode)
        {
            m_currentMode = mode;
            update_UI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        void update_UI()
        {
            bool iEEGButtonEnabled = false;
            bool IRMFButtonEnabled = false;
            
            if (m_currentMode != null)
            {
                switch (m_currentMode.m_idMode)
                {
                    case Mode.ModesId.NoPathDefined:
                        m_sceneButton.interactable = false;
                        m_globalButton.interactable = false;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = false;
                        break;
                    case Mode.ModesId.MinPathDefined:
                        m_globalButton.interactable = true;
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.AllPathDefined:
                        m_globalButton.interactable = true;
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.ComputingAmplitudes:
                        m_globalButton.interactable = true;
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.AmplitudesComputed:
                        m_globalButton.interactable = true;
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.TriErasing:
                        m_globalButton.interactable = true;
                        m_sceneButton.interactable = false;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = false;
                        break;
                    case Mode.ModesId.AmpNeedUpdate:
                        m_globalButton.interactable = true;
                        m_sceneButton.interactable = true;
                        iEEGButtonEnabled = true;
                        IRMFButtonEnabled = true;
                        break;
                    case Mode.ModesId.Error:
                        m_globalButton.interactable = false;
                        m_sceneButton.interactable = false;
                        iEEGButtonEnabled = false;
                        IRMFButtonEnabled = false;
                        break;
                }
            }

            bool isIRMF = m_currentScene ? m_SPLeftMenuController.is_current_column_fMRI() : m_MPLeftMenuController.is_current_column_fMRI();
            m_iEEGButton.interactable = !isIRMF && iEEGButtonEnabled;
            m_fMRIButton.interactable = isIRMF && IRMFButtonEnabled;

            Color orange = new Color(1f, 165 / 255f, 0f);            
            m_iEEGButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
            m_fMRIButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
            m_sceneButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
            m_globalButton.transform.Find("Text").GetComponent<Text>().color = m_currentScene ? orange : Color.green;
        }

        #endregion functions
    }
}