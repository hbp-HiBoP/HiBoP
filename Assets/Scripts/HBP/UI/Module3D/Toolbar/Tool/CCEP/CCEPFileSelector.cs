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

                ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).CurrentLatencyFile = value;
                ApplicationState.Module3D.SelectedScene.UpdateSitesRendering();
            });
        }

        public override void DefaultState()
        {
            m_Dropdown.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isCCEP = ApplicationState.Module3D.SelectedScene.IsLatencyModeEnabled;
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_Dropdown.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_Dropdown.interactable = isCCEP;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_Dropdown.interactable = isCCEP;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_Dropdown.interactable = isCCEP;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_Dropdown.interactable = isCCEP;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_Dropdown.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_Dropdown.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_Dropdown.interactable = isCCEP;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_Dropdown.interactable = false;
                    break;
                default:
                    break;
            }
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column)
            {
                m_Dropdown.options.Clear();
                foreach (HBP.Module3D.Latencies latencies in ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedImplantation.Latencies)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(latencies.Name));
                }
                m_Dropdown.value = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).CurrentLatencyFile != -1 ? ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).CurrentLatencyFile : 0;
                m_Dropdown.RefreshShownValue();
            }
        }
        #endregion
    }
}