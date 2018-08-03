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
            m_Button.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.LoadSinglePatientSceneFromMultiPatientScene(SelectedScene.Visualization, SelectedScene.Patients[SelectedScene.ColumnManager.SelectedColumn.SelectedPatientID]);
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isInteractable = (SelectedColumn.SelectedSite != null) && (SelectedScene.Type == Data.Enums.SceneType.MultiPatients);

            m_Button.interactable = isInteractable;
        }
        #endregion
    }
}