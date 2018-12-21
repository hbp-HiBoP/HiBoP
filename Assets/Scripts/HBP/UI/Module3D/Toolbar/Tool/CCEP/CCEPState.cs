using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPState : Tool
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

                SelectedScene.IsLatencyModeEnabled = isOn;
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
            bool areCCEPAvailable = SelectedScene.Type == Data.Enums.SceneType.SinglePatient && SelectedScene.ColumnManager.SelectedImplantation.AreLatenciesLoaded;

            m_Toggle.interactable = areCCEPAvailable;
        }

        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.IsLatencyModeEnabled && SelectedScene.ColumnManager.SelectedImplantation.AreLatenciesLoaded;
        }
        #endregion
    }
}