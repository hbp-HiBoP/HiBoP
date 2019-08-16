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
            m_Toggle.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnDynamic = SelectedColumn is HBP.Module3D.Column3DDynamic;

            m_Toggle.interactable = isColumnDynamic;
        }

        public void Set(bool isOn)
        {
            m_Toggle.isOn = isOn;
        }
        #endregion
    }
}