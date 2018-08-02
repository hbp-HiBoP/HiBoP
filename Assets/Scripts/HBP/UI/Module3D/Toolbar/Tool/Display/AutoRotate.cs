using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class AutoRotate : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        [SerializeField]
        private Toggle m_Toggle;
        [SerializeField]
        private Slider m_Slider;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.AutomaticRotation = isOn;
            });

            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.AutomaticRotationSpeed = value;
            });
        }

        public override void DefaultState()
        {
            m_Button.interactable = false;
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
            m_Slider.value = 30.0f;
            m_Slider.interactable = false;
        }

        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
            m_Toggle.interactable = true;
            m_Slider.interactable = true;
        }

        public override void UpdateStatus()
        {
            m_Toggle.isOn = ApplicationState.Module3D.SelectedScene.AutomaticRotation;
            m_Slider.value = ApplicationState.Module3D.SelectedScene.AutomaticRotationSpeed;
        }
        #endregion
    }
}