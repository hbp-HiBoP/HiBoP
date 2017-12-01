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
                ApplicationState.Module3D.LoadSinglePatientSceneFromMultiPatientScene(scene.Visualization, scene.Patients[scene.ColumnManager.SelectedColumn.SelectedPatientID]);
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isInteractable = (ApplicationState.Module3D.SelectedColumn.SelectedSite != null) && (ApplicationState.Module3D.SelectedScene.Type == SceneType.MultiPatients);

            m_Button.interactable = isInteractable;
        }
        #endregion
    }
}