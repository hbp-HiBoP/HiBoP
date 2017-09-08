using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class LoadPatient : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;

        private Site m_LastSelectedSite;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnSelectSite.AddListener((site) =>
            {
                UpdateInteractable();
            });

            m_Button.onClick.AddListener(() =>
            {
                Base3DScene scene = ApplicationState.Module3D.SelectedScene;
                ApplicationState.Module3D.OnLoadSinglePatientSceneFromMultiPatientsScene.Invoke(scene.Visualization, scene.Patients[scene.ColumnManager.SelectedColumn.SelectedPatientID]);
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isInteractable = (ApplicationState.Module3D.SelectedColumn.SelectedSite != null) && (ApplicationState.Module3D.SelectedScene.Type == SceneType.MultiPatients);
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Button.interactable = false;
                    gameObject.SetActive(false);
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Button.interactable = isInteractable;
                    gameObject.SetActive(isInteractable);
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Button.interactable = isInteractable;
                    gameObject.SetActive(isInteractable);
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Button.interactable = false;
                    gameObject.SetActive(false);
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Button.interactable = isInteractable;
                    gameObject.SetActive(isInteractable);
                    break;
                case Mode.ModesId.TriErasing:
                    m_Button.interactable = false;
                    gameObject.SetActive(false);
                    break;
                case Mode.ModesId.ROICreation:
                    m_Button.interactable = false;
                    gameObject.SetActive(false);
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Button.interactable = isInteractable;
                    gameObject.SetActive(isInteractable);
                    break;
                case Mode.ModesId.Error:
                    m_Button.interactable = false;
                    gameObject.SetActive(false);
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}