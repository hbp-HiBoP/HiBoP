using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPFileSelector : Tool
    {
        #region Properties
        [SerializeField]
        private Dropdown m_Dropdown;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedColumn.CurrentLatencyFile = value;
            });
        }

        public override void DefaultState()
        {
            m_Dropdown.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isCCEP = ApplicationState.Module3D.SelectedScene.IsLatencyModeEnabled && ApplicationState.Module3D.SelectedScene.Type == SceneType.SinglePatient;

            m_Dropdown.interactable = isCCEP;
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column)
            {
                m_Dropdown.options.Clear();
                if (ApplicationState.Module3D.SelectedScene.Type == SceneType.SinglePatient)
                {
                    foreach (HBP.Module3D.Latencies latencies in ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedImplantation.Latencies)
                    {
                        m_Dropdown.options.Add(new Dropdown.OptionData(latencies.Name));
                    }
                    m_Dropdown.value = ApplicationState.Module3D.SelectedColumn.CurrentLatencyFile != -1 ? ApplicationState.Module3D.SelectedColumn.CurrentLatencyFile : 0;
                }
                m_Dropdown.RefreshShownValue();
            }
        }
        #endregion
    }
}