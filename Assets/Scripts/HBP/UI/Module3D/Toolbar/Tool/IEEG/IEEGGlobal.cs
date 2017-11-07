using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IEEGGlobal : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Toggle;

        public GenericEvent<bool> OnChangeValue = new GenericEvent<bool>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                OnChangeValue.Invoke(isOn);
            });
        }

        public override void DefaultState()
        {

        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG;
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_Toggle.interactable = isColumnIEEG;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_Toggle.interactable = isColumnIEEG;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_Toggle.interactable = isColumnIEEG;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_Toggle.interactable = isColumnIEEG;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_Toggle.interactable = false;
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}