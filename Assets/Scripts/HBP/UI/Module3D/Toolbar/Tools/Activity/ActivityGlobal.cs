﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class ActivityGlobal : Tool
    {
        #region Properties
        /// <summary>
        /// Toggle global mode
        /// </summary>
        [SerializeField] private Toggle m_Toggle;
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing the global mode
        /// </summary>
        public GenericEvent<bool> OnChangeValue = new GenericEvent<bool>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                OnChangeValue.Invoke(isOn);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Toggle.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Toggle.interactable = true;
        }
        /// <summary>
        /// Set the global mode
        /// </summary>
        /// <param name="isOn">Global mode activated</param>
        public void Set(bool isOn)
        {
            m_Toggle.isOn = isOn;
        }
        #endregion
    }
}