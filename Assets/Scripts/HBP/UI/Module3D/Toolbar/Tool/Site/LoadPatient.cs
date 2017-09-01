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
                if (!site)
                {
                    m_Button.interactable = false;
                    gameObject.SetActive(false);
                }
                else if (ApplicationState.Module3D.SelectedScene.Type == SceneType.MultiPatients)
                {
                    m_Button.interactable = true;
                    gameObject.SetActive(true);
                }
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
            bool isSiteSelected = ApplicationState.Module3D.SelectedColumn.SelectedSite != null;
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Button.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Button.interactable = isSiteSelected;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Button.interactable = isSiteSelected;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Button.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Button.interactable = isSiteSelected;
                    break;
                case Mode.ModesId.TriErasing:
                    m_Button.interactable = false;
                    break;
                case Mode.ModesId.ROICreation:
                    m_Button.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Button.interactable = isSiteSelected;
                    break;
                case Mode.ModesId.Error:
                    m_Button.interactable = false;
                    break;
                default:
                    break;
            }
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                if (ApplicationState.Module3D.SelectedScene.Type != SceneType.MultiPatients)
                {
                    gameObject.SetActive(false);
                    return;
                }

                gameObject.SetActive(true);
                Site site = ApplicationState.Module3D.SelectedColumn.SelectedSite;
                if (!site)
                {
                    m_Button.interactable = false;
                }
                else
                {
                    m_Button.interactable = true;
                }
            }
        }
        #endregion
    }
}