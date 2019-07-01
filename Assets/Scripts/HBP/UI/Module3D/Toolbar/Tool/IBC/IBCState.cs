using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IBCState : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Toggle;
        #endregion

        #region Events
        public GenericEvent<bool> OnChangeState = new GenericEvent<bool>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.FMRIManager.DisplayIBCContrasts = isOn;
                OnChangeState.Invoke(isOn);
            });
        }

        public override void DefaultState()
        {
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isIBCAvailable = ApplicationState.Module3D.IBCObjects.Loaded;

            m_Toggle.interactable = isIBCAvailable;
        }

        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.ColumnManager.FMRIManager.DisplayIBCContrasts;
        }
        #endregion
    }
}