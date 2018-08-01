using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ComputeIEEG : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Compute;
        [SerializeField]
        private Button m_Remove;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Compute.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.ResetIEEG();
                ApplicationState.Module3D.SelectedScene.UpdateGenerator();
                UpdateInteractable();
            });
            m_Remove.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.ResetIEEG();
                UpdateInteractable();
            });
        }
        public override void DefaultState()
        {
            m_Compute.interactable = false;
            m_Remove.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isCCEP = ApplicationState.Module3D.SelectedScene.IsLatencyModeEnabled;
            bool isColumnIEEG = ApplicationState.Module3D.SelectedColumn.Type == Data.Enums.ColumnType.iEEG;
            bool isGeneratorUpToDate = ApplicationState.Module3D.SelectedScene.SceneInformation.IsGeneratorUpToDate;

            m_Compute.interactable = !isCCEP && isColumnIEEG;
            m_Remove.interactable = !isCCEP && isColumnIEEG && isGeneratorUpToDate;
        }
        #endregion
    }
}