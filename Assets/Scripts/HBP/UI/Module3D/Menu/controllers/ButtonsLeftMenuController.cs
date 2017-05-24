/**
 * \file    ButtonsLeftMenuController.cs
 * \author  Lance Florian
 * \date    30/08/2016
 * \brief   Define ButtonsLeftMenuController class
 */
using UnityEngine;
using UnityEngine.UI;
using HBP.Module3D;

namespace HBP.UI.Module3D
{    
    public class LeftButtons
    {
        public enum Type
        {
            Global,Scene,iEEG,fMRI
        }; 
    }

    /// <summary>
    /// A class for managing the left buttons menu in the UI
    /// </summary>
    public class ButtonsLeftMenuController : MonoBehaviour
    {
        #region Properties       
        Button m_globalButton = null;
        Button m_sceneButton = null;
        Button m_iEEGButton = null;
        Button m_fMRIButton = null;

        [SerializeField, Candlelight.PropertyBackingField]
        SPLeftMenuController m_SinglePatientLeftMenuController = null;
        public SPLeftMenuController SinglePatientLeftMenuController { get { return m_SinglePatientLeftMenuController; } }
        [SerializeField, Candlelight.PropertyBackingField]
        MPLeftMenuController m_MultiPatientsLeftMenuController = null;
        public MPLeftMenuController MultiPatientsLeftMenuController { get { return m_MultiPatientsLeftMenuController; } }
        [SerializeField, Candlelight.PropertyBackingField]
        GlobalMenuController m_GlobalMenuController = null;
        public GlobalMenuController GlobalMenuController { get { return m_GlobalMenuController; } }

        #endregion

        #region Public Methods
        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void Initialize(ScenesManager scenesManager)
        {
            FindButtons();
            scenesManager.OnChangeSelectedScene.AddListener((scene) => OnChangeScene(scene));
            m_globalButton.onClick.AddListener(() => m_GlobalMenuController.switch_UI_Visibility());
            OnChangeScene(scenesManager.SelectedScene);
        }
        #endregion

        #region Private Methods
        void OnChangeScene(Base3DScene scene)
        {
            scene.ModesManager.OnChangeMode.AddListener((mode) => OnChangeMode(mode));

            m_sceneButton.onClick.RemoveAllListeners();
            m_sceneButton.onClick.AddListener(delegate
            {
                switch (scene.Type)
                {
                    case SceneType.SinglePatient:
                        m_SinglePatientLeftMenuController.menu_clicked(LeftButtons.Type.Scene);
                        break;
                    case SceneType.MultiPatients:
                        m_MultiPatientsLeftMenuController.menu_clicked(LeftButtons.Type.Scene);
                        break;
                    default:
                        break;
                }
            });

            m_iEEGButton.onClick.RemoveAllListeners();
            m_iEEGButton.onClick.AddListener(delegate
            {
                switch (scene.Type)
                {
                    case SceneType.SinglePatient:
                        m_SinglePatientLeftMenuController.menu_clicked(LeftButtons.Type.iEEG);
                        break;
                    case SceneType.MultiPatients:
                        m_MultiPatientsLeftMenuController.menu_clicked(LeftButtons.Type.iEEG);
                        break;
                    default:
                        break;
                }
            });

            m_fMRIButton.onClick.RemoveAllListeners();
            m_fMRIButton.onClick.AddListener(delegate
            {
                switch (scene.Type)
                {
                    case SceneType.SinglePatient:
                        m_SinglePatientLeftMenuController.menu_clicked(LeftButtons.Type.fMRI);
                        break;
                    case SceneType.MultiPatients:
                        m_MultiPatientsLeftMenuController.menu_clicked(LeftButtons.Type.fMRI);
                        break;
                    default:
                        break;
                }
            });

            m_iEEGButton.interactable = scene.Column3DViewManager.SelectedColumn.Type == Column3DView.ColumnType.IEEG;
            m_fMRIButton.interactable = scene.Column3DViewManager.SelectedColumn.Type == Column3DView.ColumnType.FMRI;

            OnChangeMode(scene.ModesManager.CurrentMode);
        }
        void OnChangeMode(Mode mode)
        {
            bool iEEGButtonEnabled = false;
            bool IRMFButtonEnabled = false;

            if (mode != null)
            {
                switch (mode.ID)
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
            m_iEEGButton.interactable &= iEEGButtonEnabled;
            m_fMRIButton.interactable &= IRMFButtonEnabled;
        }
        void FindButtons()
        {
            m_globalButton = transform.Find("Global button").GetComponent<Button>();
            m_sceneButton = transform.Find("Scene button").GetComponent<Button>();
            m_iEEGButton = transform.Find("iEEG button").GetComponent<Button>();
            m_fMRIButton = transform.Find("fMRI button").GetComponent<Button>();
        }
        #endregion
    }
}